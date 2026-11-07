using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Interfaces;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.WinchesterDriveConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

/// <summary>Relais de signaux ST-412 ; le module emprunte les images et possède seulement ses curseurs et impulsions.</summary>
internal sealed partial class WinchesterTrackSignals
{
    private readonly IAcb4000 chip;
    private readonly Func<int, WinchesterStorage?> storage;
    private readonly uint ownerClock;
    private readonly uint[] rotation = new uint[C.DriveCount];
    private readonly TrackCursor[] cache = [new(), new()];
    private readonly List<uint> writtenPulses = [];
    private ulong ownerFraction;
    private ulong time;
    private IEmulationDiskImage? writingImage;
    private int writingCylinder;
    private int writingHead;
    private int writingUnit;
    private uint writingPhase;
    private ulong writeStart;
    private bool reading;
    private int readUnit;
    private int selectedUnit;
    private int selectedHead;
    private int selectedCylinder;
    private int selectedWriteHead;
    private bool readGate;
    private bool writeGate;
    private bool activityChanged = true;

    internal bool CanAdvanceIdleClock => chip.IsIdle && writingImage is null
        && drives[0].Settle == 0 && drives[1].Settle == 0;

    internal void AdvanceIdleTicks(uint ticks)
    {
        if (!CanAdvanceIdleClock) throw new InvalidOperationException();
        chip.AdvanceIdleTicks(ticks);
        ownerFraction += (ulong)ticks * C.ReadClock;
        AdvanceIdleCells();
    }

    private void AdvanceIdleCells()
    {
        var cells = ownerFraction / ownerClock;
        ownerFraction %= ownerClock;
        for (var unit = 0; unit < C.DriveCount; unit++)
            rotation[unit] = (uint)(((ulong)rotation[unit] + cells * C.RevolutionsPerSecond) % C.ReadClock);
        time += cells * C.CellNanoseconds;
        reading = false;
    }

    internal WinchesterTrackSignals(IAcb4000 chip, Func<int, WinchesterStorage?> storage, int ticksPerSecond = C.OwnerClock)
    {
        ArgumentNullException.ThrowIfNull(chip);
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ticksPerSecond);
        this.chip = chip;
        this.storage = storage;
        ownerClock = (uint)ticksPerSecond;
        chip.DriveStepped += ReceiveDriveStep;
        chip.DriveSelectionChanged += ReceiveDriveSelection;
        chip.DriveGatesChanged += ReceiveDriveGates;
        ReceiveDriveSelection(chip.Unit, chip.SelectedHead, chip.FormatCylinder, chip.FormatHead);
        ReceiveDriveGates(chip.ReadGate, chip.WriteGate);
    }

    internal void Tick()
    {
        AdvanceMechanics();
        chip.Tick();
        ApplyDriveSteps();
        ownerFraction += C.ReadClock;
        if (!chip.Bus.Busy && writingImage is null)
        {
            // No sequencer can consume or emit cells while the bus is free. Preserve the
            // exact spindle phase and sub-tick fraction without executing empty cell loops.
            AdvanceIdleCells();
            UpdateActivity();
            return;
        }
        while (ownerFraction >= ownerClock)
        {
            ownerFraction -= ownerClock;
            ClockCell();
        }
        UpdateActivity();
    }

    private uint Phase(int unit) => (uint)((ulong)rotation[unit] * C.NanosecondsPerSecond
        / ((ulong)C.ReadClock * C.RevolutionsPerSecond));

    private void ClockCell()
    {
        var unit = selectedUnit;
        var selected = unit < C.DriveCount ? drives[unit].Storage : null;
        var phase = selected is null ? 0 : Phase(unit);
        var cylinder = selectedCylinder;
        var head = selectedWriteHead;
        chip.SetIndex(selected is not null && phase < C.IndexPulseNanoseconds);
        var pulse = chip.ClockWriteCell(selected is not null && phase < C.IndexPulseNanoseconds);
        if (writeGate && selected is not null)
        {
            if (writingImage is not null && (writingUnit != unit || writingCylinder != cylinder
                || writingHead != head || !ReferenceEquals(writingImage, selected.Image))) PublishWindow();
            if (writingImage is null)
            {
                writingImage = selected.Image;
                writingUnit = unit;
                writingCylinder = cylinder;
                writingHead = head;
                writingPhase = phase;
                writeStart = time;
                writtenPulses.Clear();
            }
            if (pulse) writtenPulses.Add(checked((uint)(time - writeStart)));
        }
        else PublishWindow();

        if (readGate && selected is not null)
        {
            var cursor = cache[unit];
            cursor.Select(selected.Image, selected.Cylinder, selectedHead);
            if (!reading || readUnit != unit) cursor.Restart();
            chip.ClockReadCell(cursor.Sample(phase));
            reading = true;
            readUnit = unit;
        }
        else reading = false;
        for (var drive = 0; drive < C.DriveCount; drive++)
        {
            rotation[drive] += C.RevolutionsPerSecond;
            if (rotation[drive] >= C.ReadClock) rotation[drive] -= C.ReadClock;
        }
        time += C.CellNanoseconds;
    }

    /// <summary>Publie aussi une écriture interrompue ; ne détruit aucun média ou cœur emprunté.</summary>
    internal void Reset()
    {
        try { PublishWindow(); }
        finally
        {
            writingImage = null;
            writtenPulses.Clear();
            reading = false;
            foreach (var cursor in cache) cursor.Clear();
            StopActivity();
        }
    }

    private void PublishWindow()
    {
        if (writingImage is not { } image) return;
        var duration = checked((uint)(time - writeStart));
        if (duration != 0)
            image.WriteTrackWindow(writingCylinder, writingHead, C.TrackEncoding,
                C.CellNanoseconds, C.RevolutionNanoseconds, writingPhase,
                duration, writtenPulses, includeEndingInstant: false);
        // On failure, keep the pending pulses for the owner; never report a lost write as published.
        cache[writingUnit].Clear();
        writingImage = null;
        writtenPulses.Clear();
    }

    private sealed class TrackCursor
    {
        private IEmulationDiskImage? image;
        private int cylinder;
        private int head;
        private uint[] pulses = [];
        private int next;
        private uint previousPhase;

        internal void Select(IEmulationDiskImage source, int track, int side)
        {
            if (ReferenceEquals(source, image) && track == cylinder && side == head) return;
            var revolution = source.ReadTrack(track, side, C.TrackEncoding,
                C.CellNanoseconds, C.RevolutionNanoseconds).Revolutions[0];
            if (revolution.IndexTimeNanoseconds != C.RevolutionNanoseconds) throw new ArgumentException(nameof(source));
            var positions = new uint[revolution.FluxIntervalsNanoseconds.Count];
            uint position = 0;
            for (var index = 0; index < positions.Length; index++)
            {
                position = checked(position + revolution.FluxIntervalsNanoseconds[index]);
                if (position > revolution.IndexTimeNanoseconds) throw new ArgumentException(nameof(source));
                positions[index] = position;
            }
            image = source;
            cylinder = track;
            head = side;
            pulses = positions;
            Restart();
        }

        internal bool Sample(uint phase)
        {
            if (phase < previousPhase) next = 0;
            previousPhase = phase;
            var lower = (long)phase - C.HalfCellNanoseconds;
            var upper = (long)phase + C.HalfCellNanoseconds;
            while (next < pulses.Length && pulses[next] <= lower) next++;
            var value = false;
            while (next < pulses.Length && pulses[next] <= upper) { value = true; next++; }
            // The sampling window crosses INDEX; intervals encode a transition at index as the period's end.
            if (pulses.Length != 0 && lower < 0 && pulses[^1] > C.RevolutionNanoseconds + lower) value = true;
            if (pulses.Length != 0 && upper > C.RevolutionNanoseconds && pulses[0] <= upper - C.RevolutionNanoseconds) value = true;
            return value;
        }

        internal void Restart() { next = 0; previousPhase = 0; }
        internal void Clear() { image = null; pulses = []; Restart(); }
    }
}

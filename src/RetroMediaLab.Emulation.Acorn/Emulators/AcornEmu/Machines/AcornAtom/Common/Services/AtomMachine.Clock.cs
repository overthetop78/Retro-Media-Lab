using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Constants.AtomConstants;
using V = RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Constants.AtomVideoConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Services;

internal sealed partial class AtomMachine
{
    // Témoin de comparaison du transport seulement ; aucune option ni variante matérielle.
    internal bool GroupVideoClocks { get; set; } = true;

    private long _diskClock;
    private long _nextDiskClock = long.MaxValue;
    private bool _groupDiskClocks = true;
    private bool _canGroupDiskClocks = true;

    internal bool GroupDiskClocks
    {
        get => _groupDiskClocks;
        set
        {
            if (value == _groupDiskClocks) return;
            _fdcDrive?.AdvanceTo(_diskClock);
            _groupDiskClocks = value;
            DiskClockGroupingChanged(_canGroupDiskClocks);
        }
    }

    private void WireDiskClock()
    {
        if (_fdcDrive is null) return;
        _fdcDrive.ClockGroupingChanged += DiskClockGroupingChanged;
        DiskClockGroupingChanged(_fdcDrive.CanGroupClocks);
    }

    private void DiskClockGroupingChanged(bool allowed)
    {
        _canGroupDiskClocks = allowed;
        if (_fdcDrive is null) return;
        _fdcDrive.NextClockEventChanged -= DiskClockEventChanged;
        _nextDiskClock = long.MaxValue;
        if (!_groupDiskClocks || !allowed) return;
        _nextDiskClock = _fdcDrive.NextClockEvent ?? long.MaxValue;
        _fdcDrive.NextClockEventChanged += DiskClockEventChanged;
    }

    private void DiskClockEventChanged(long? next) => _nextDiskClock = next ?? long.MaxValue;

    private void ClockDisk()
    {
        if (_fdcDrive is null) return;
        ++_diskClock;
        if (!_groupDiskClocks || !_canGroupDiskClocks) _fdcDrive.Tick();
        else if (_diskClock >= _nextDiskClock) _fdcDrive.AdvanceTo(_diskClock);
    }

    private void SynchronizeDiskClock()
    {
        if (_fdcDrive is null) return;
        _fdcDrive.AdvanceTo(_diskClock);
        _fdcDrive.SynchronizeClock();
    }

    private void ClockVideo()
    {
        // Tick précède le transfert CPU : la RAM et les entrées de contrôle restent
        // stables pendant ces CLK fournis, avec les vraies prises DD/FS du récepteur.
        _videoPhase += V.CharacterClock;
        var clocks = _videoPhase / C.CpuClock;
        _videoPhase -= clocks * C.CpuClock;
        if (GroupVideoClocks) _video.AdvanceClocks(clocks);
        else for (var clock = 0; clock < clocks; clock++) _video.Tick();
    }

    private void WireCpuClock()
    {
        // Schéma202.000/C : PH2 du6502(IC22/39) vers6522(IC1/25).
        // Le CPU termine le transfert de bus avant de notifier son descendant.
        _cpu.PhaseOutputsChanged = (_, high) =>
        {
            if (high) _via.Phi2LeadingEdge();
            else _via.Phi2TrailingEdge();
        };
        // Niveau initial PH2 bas, avant les vrais fronts des cycles RESET.
        _via.Phi2TrailingEdge();
    }
}

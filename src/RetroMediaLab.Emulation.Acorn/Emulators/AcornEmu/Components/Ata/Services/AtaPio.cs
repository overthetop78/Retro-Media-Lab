using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Ata.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Enums;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Ata.Constants.AtaConstants;
using S = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.StorageConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Ata.Services;

/// <summary>ATA command block and PIO channel. The board supplies its clock and borrowed images.</summary>
/// <remarks>Seagate 36111-001 rev. C, sections 4–5. No file, thread, or image is owned here.</remarks>
internal sealed partial class AtaPio
{
    private sealed class Drive
    {
        internal readonly byte[] Buffer = new byte[C.SectorBytes * C.MaximumMultiple];
        internal IEmulationDiskImage? Image;
        internal byte Error = C.DiagnosticPassed;
        internal byte Features;
        internal byte Count = 1;
        internal byte Sector = 1;
        internal byte CylinderLow;
        internal byte CylinderHigh;
        internal byte Head;
        internal byte Status = C.IdleStatus;
        internal int Heads;
        internal int Sectors;
        internal int Multiple;
        internal int Track;
        internal bool EightBit;
        internal bool PreserveSettings;
        internal bool Interrupt;
        internal int ReadLatency = S.DefaultHardDiskLatencyMilliseconds;
        internal int WriteLatency = S.DefaultHardDiskLatencyMilliseconds;
        internal int Speed = S.DefaultSpeedPercent;
        internal long Reads;
        internal long Writes;
        internal long Seeks;
    }

    private readonly Func<int, IEmulationDiskImage?> _disk;
    private readonly int _clock;
    private readonly Drive[] _drives = [new(), new()];
    private AtaPhase _phase;
    private AtaCommand _command;
    private byte _control;
    private int _unit;
    private int _remaining;
    private int _offset;
    private int _length;
    private int _committed;
    private long _delay;
    private byte _transferError;
    private bool _dataOnly;
    private bool _firstWrite;
    private bool _irq;
    private bool _addressWrapped;
    private Drive Current => _drives[_unit];

    internal AtaPio(Func<int, IEmulationDiskImage?> disk, int ticksPerSecond = C.DefaultClockHz)
    {
        ArgumentNullException.ThrowIfNull(disk);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ticksPerSecond);
        _disk = disk;
        _clock = ticksPerSecond;
        Reset();
    }

    internal Action<bool>? InterruptChanged { get; set; }
    internal bool Irq => _irq;
    internal bool Busy => (Current.Status & C.Busy) != 0;
    internal bool Drq => (Current.Status & C.DataRequest) != 0;
    internal int Unit => _unit;
    internal long ReadBlocks(int unit) => _drives[CheckedUnit(unit)].Reads;
    internal long WrittenBlocks(int unit) => _drives[CheckedUnit(unit)].Writes;
    internal long SeekCount(int unit) => _drives[CheckedUnit(unit)].Seeks;
    internal bool Active(int unit) => CheckedUnit(unit) == _unit && _phase is not (AtaPhase.Idle or AtaPhase.Reset);
    internal bool Writing => Active(_unit) && _command is AtaCommand.WriteSectors or AtaCommand.WriteSectorsNoRetry or AtaCommand.WriteMultiple;

    internal void Configure(int unit, int readLatency, int writeLatency, int speedPercent)
    {
        var drive = _drives[CheckedUnit(unit)];
        ArgumentOutOfRangeException.ThrowIfNegative(readLatency);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(readLatency, S.MaximumLatencyMilliseconds);
        ArgumentOutOfRangeException.ThrowIfNegative(writeLatency);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(writeLatency, S.MaximumLatencyMilliseconds);
        ArgumentOutOfRangeException.ThrowIfNegative(speedPercent);
        drive.ReadLatency = readLatency;
        drive.WriteLatency = writeLatency;
        drive.Speed = speedPercent;
    }

    internal void Reset()
    {
        _control = 0;
        _unit = 0;
        BeginReset(hardware: true);
    }

    internal ushort ReadCommand(int register)
    {
        ObserveImageChange();
        if (_disk(_unit) is null) return C.OpenBusWord;
        switch ((AtaRegister)(register & C.RegisterMask))
        {
            case AtaRegister.Data: return ReadData();
            case AtaRegister.ErrorFeatures: return Current.Error;
            case AtaRegister.SectorCount: return Current.Count;
            case AtaRegister.SectorNumber: return Current.Sector;
            case AtaRegister.CylinderLow: return Current.CylinderLow;
            case AtaRegister.CylinderHigh: return Current.CylinderHigh;
            case AtaRegister.DriveHead: return Current.Head;
            default:
                var status = Current.Status;
                Current.Interrupt = false;
                UpdateInterrupt();
                return status;
        }
    }

    internal void WriteCommand(int register, ushort value)
    {
        ObserveImageChange();
        var selectedRegister = (AtaRegister)(register & C.RegisterMask);
        if (selectedRegister == AtaRegister.Data) { WriteData(value); return; }
        if (_phase != AtaPhase.Idle) return;
        if (selectedRegister == AtaRegister.DriveHead)
        {
            _unit = (value & C.DriveSelect) == 0 ? 0 : 1;
            ObserveImageChange();
            Current.Head = (byte)value;
            UpdateInterrupt();
            return;
        }
        switch (selectedRegister)
        {
            case AtaRegister.ErrorFeatures: Current.Features = (byte)value; break;
            case AtaRegister.SectorCount: Current.Count = (byte)value; break;
            case AtaRegister.SectorNumber: Current.Sector = (byte)value; break;
            case AtaRegister.CylinderLow: Current.CylinderLow = (byte)value; break;
            case AtaRegister.CylinderHigh: Current.CylinderHigh = (byte)value; break;
            case AtaRegister.StatusCommand: StartCommand((byte)value); break;
        }
    }

    internal byte ReadControl(int register)
    {
        ObserveImageChange();
        if (_disk(_unit) is null) return C.OpenBusByte;
        return register switch
        {
            C.ControlRegister => Current.Status,
            C.AddressRegister => (byte)((_unit == 0 ? C.MasterSelected : C.SlaveSelected)
                | (~(Current.Head << C.HeadAddressShift) & C.HeadAddressMask)
                | (Writing ? C.AddressWriteGate : 0)),
            _ => C.OpenBusByte
        };
    }

    internal void WriteControl(int register, byte value)
    {
        if (register != C.ControlRegister) return;
        var wasReset = (_control & C.SoftwareReset) != 0;
        _control = value;
        if ((value & C.SoftwareReset) != 0)
        {
            _phase = AtaPhase.Reset;
            _delay = 0;
            foreach (var drive in _drives)
            {
                drive.Status = C.Busy;
                drive.Interrupt = false;
                drive.Image?.Mechanics.SetActivity(false, false);
            }
        }
        else if (wasReset) BeginReset(hardware: false);
        UpdateInterrupt();
    }

    internal void Advance(int ticks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ticks);
        ObserveImageChange();
        while (ticks > 0 && _delay > 0)
        {
            var elapsed = Math.Min(ticks, _delay);
            ticks -= (int)elapsed;
            _delay -= elapsed;
            if (_delay == 0) ExecuteEvent();
        }
    }

    private void BeginReset(bool hardware)
    {
        _phase = AtaPhase.Reset;
        _unit = 0;
        _remaining = _offset = _length = 0;
        for (var unit = 0; unit < _drives.Length; unit++)
        {
            var drive = _drives[unit];
            drive.Image = _disk(unit);
            drive.Image?.Mechanics.SetActivity(false, false);
            ResetRegisters(drive);
            DefaultGeometry(drive);
            if (hardware || !drive.PreserveSettings)
            {
                drive.Multiple = 0;
                drive.EightBit = drive.PreserveSettings = false;
            }
            drive.Status = C.Busy;
        }
        ScheduleDelay(Current.ReadLatency);
        UpdateInterrupt();
    }

    private void ObserveImageChange()
    {
        var image = _disk(_unit);
        if (ReferenceEquals(Current.Image, image)) return;
        var interrupted = _phase is not (AtaPhase.Idle or AtaPhase.Reset);
        var head = Current.Head;
        Current.Image = image;
        ResetRegisters(Current);
        Current.Head = head;
        DefaultGeometry(Current);
        Current.Multiple = 0;
        Current.EightBit = Current.PreserveSettings = false;
        if (interrupted) Finish(C.AbortError);
        else
        {
            Current.Status = _phase == AtaPhase.Reset ? C.Busy : C.IdleStatus;
            UpdateInterrupt();
        }
    }

    private static void ResetRegisters(Drive drive)
    {
        drive.Error = C.DiagnosticPassed;
        drive.Count = drive.Sector = 1;
        drive.CylinderLow = drive.CylinderHigh = drive.Head = drive.Features = 0;
        drive.Interrupt = false;
    }

    private static void DefaultGeometry(Drive drive)
    {
        var image = drive.Image;
        drive.Heads = image is { Heads: > 0 and <= C.MaximumHeads } ? image.Heads : 0;
        var trackBytes = image is null ? 0 : (long)image.SectorsPerTrack * image.SectorSize;
        drive.Sectors = trackBytes > 0 && trackBytes % C.SectorBytes == 0
            && trackBytes / C.SectorBytes <= C.MaximumSectorsPerTrack ? (int)(trackBytes / C.SectorBytes) : 0;
    }

    private void Schedule(AtaPhase phase, bool writing = false)
    {
        _phase = phase;
        Current.Status = C.Busy;
        ScheduleDelay(writing ? Current.WriteLatency : Current.ReadLatency);
    }

    private void ScheduleDelay(int milliseconds) => _delay = Current.Speed == 0 ? 1
        : Math.Max(1, (long)_clock * milliseconds * S.DefaultSpeedPercent
            / C.MillisecondsPerSecond / Current.Speed);

    private void Finish(byte error = 0, bool interrupt = true, bool writeFault = false)
    {
        _phase = AtaPhase.Idle;
        _delay = 0;
        Current.Error = error;
        Current.Status = (byte)(C.IdleStatus | (error != 0 ? C.Error : 0) | (writeFault ? C.WriteFault : 0));
        Current.Image?.Mechanics.SetActivity(false, false);
        if (interrupt) Current.Interrupt = true;
        UpdateInterrupt();
    }

    private void UpdateInterrupt()
    {
        var irq = (_control & (C.DisableInterrupt | C.SoftwareReset)) == 0
            && _disk(_unit) is not null && Current.Interrupt;
        if (irq == _irq) return;
        _irq = irq;
        InterruptChanged?.Invoke(irq);
    }

    private static int CheckedUnit(int unit)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(unit);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(unit, C.DriveCount);
        return unit;
    }
}

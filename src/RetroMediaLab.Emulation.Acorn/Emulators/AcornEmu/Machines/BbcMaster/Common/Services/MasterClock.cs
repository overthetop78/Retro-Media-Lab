using R = RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Constants.Hd146818Constants;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Common.Constants.MasterConstants;
using RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Services;
using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Common.Services;

/// <summary>Câblage CMOS du BBC Master sur le VIA et son bus lent.</summary>
internal sealed class MasterClock
{
    private readonly Hd146818 _clock = new(BbcMicroModelConstants.CpuFrequencyHz);
    private readonly Func<long> _referenceClock;
    private byte _portA;
    private byte _portB;
    private byte _latch;
    private bool _enabled;
    internal bool Irq => _clock.Irq;
    internal long? NextClockEvent => _clock.NextClockEvent;
    internal Action<bool>? IrqChanged { set => _clock.IrqChanged = value; }
    internal Action<long?>? NextClockEventChanged { set => _clock.NextClockEventChanged = value; }
    internal void AdvanceTo(long clock) => _clock.AdvanceTo(clock);
    private static ReadOnlySpan<byte> Configuration => [0, 0xfe, 0, 0xeb, 0, 0xc9, 0xff, 0xff, 0x12, 0, 0x17, 0xc8, 0x1e, 5, 0, 0x35, 0xa6];
    internal MasterClock(byte station = 0, byte filingSystem = 9, Func<long>? referenceClock = null)
    {
        _referenceClock = referenceClock ?? (() => 0);
        _clock.SetPowerSense(true);
        _clock.WriteRegister(R.StatusA, C.CmosOscillatorSelection);
        _clock.WriteRegister(R.StatusB, R.TwentyFourHour);
        var registers = _clock.Export();
        Configuration.CopyTo(registers.AsSpan(R.ConfigurationStart));
        registers[R.ConfigurationStart] = station;
        registers[R.ConfigurationStart + 5] = (byte)(0xc0 | filingSystem);
        _clock.Import(registers);
        InitializeCalendar();
    }
    internal void Address(byte portB, byte portA)
    {
        Synchronize();
        _portA = portA;
        _portB = portB;
        _enabled = (portB & C.CmosEnable) != 0;
        DriveBus();
    }
    internal void Update(byte latch, byte portA)
    {
        Synchronize();
        _portA = portA;
        _latch = latch;
        DriveBus();
    }
    private void DriveBus() => _clock.SetBus(_portA, _enabled, (_portB & C.CmosAddressStrobe) != 0,
        (_latch & C.CmosDataStrobe) != 0, (_latch & C.CmosRead) != 0);
    internal byte ReadBus() => _clock.DrivingDataBus ? _clock.DataBus : byte.MaxValue;
    internal byte ReadRegister(int address) { Synchronize(); return _clock.ReadRegister(address); }
    internal void WriteRegister(int address, byte value) { Synchronize(); _clock.WriteRegister(address, value); }
    internal byte[] Export() { Synchronize(); return _clock.Export(); }
    internal void Import(ReadOnlySpan<byte> bytes)
    {
        Synchronize();
        _clock.Import(bytes);
        // The battery-backed clock is seeded by its owner, never by the reusable chip.
        // Older CMOS files contain configuration, not a timestamp of when the machine was stopped.
        _clock.WriteRegister(R.StatusA, (byte)((_clock.ReadRegister(R.StatusA) & R.LowNibble) | C.CmosOscillatorSelection));
        InitializeCalendar();
    }

    private void Synchronize() => _clock.AdvanceTo(_referenceClock());

    private void InitializeCalendar()
    {
        var now = DateTime.Now;
        var control = _clock.ReadRegister(R.StatusB);
        _clock.WriteRegister(R.StatusB, (byte)(control | R.StopUpdates));
        byte Encode(int value) => (control & R.BinaryData) != 0 ? (byte)value : (byte)((value / 10 << 4) | value % 10);
        _clock.WriteRegister(R.Seconds, Encode(now.Second));
        _clock.WriteRegister(R.Minutes, Encode(now.Minute));
        var hours = (control & R.TwentyFourHour) != 0 ? Encode(now.Hour)
            : (byte)(Encode((now.Hour + R.HoursPerHalfDay - 1) % R.HoursPerHalfDay + 1)
                | (now.Hour >= R.HoursPerHalfDay ? R.Afternoon : 0));
        _clock.WriteRegister(R.Hours, hours);
        _clock.WriteRegister(R.DayOfWeek, Encode((int)now.DayOfWeek + 1));
        _clock.WriteRegister(R.Day, Encode(now.Day));
        _clock.WriteRegister(R.Month, Encode(now.Month));
        _clock.WriteRegister(R.Year, Encode(now.Year % R.CenturyYears));
        _clock.WriteRegister(R.StatusB, control);
    }
}

using R = RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Constants.MasterClockConstants;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Common.Constants.MasterConstants;
using RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Services;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Common.Services;

/// <summary>Câblage CMOS du BBC Master sur le VIA et son bus lent.</summary>
internal sealed class MasterClock
{
    private readonly Hd146818 _clock = new();
    private int _address;
    private byte _latch;
    private byte _data;
    private bool _enabled;
    private static ReadOnlySpan<byte> Configuration => [0, 0xfe, 0, 0xeb, 0, 0xc9, 0xff, 0xff, 0x12, 0, 0x17, 0xc8, 0x1e, 5, 0, 0x35, 0xa6];
    internal MasterClock(byte station = 0, byte filingSystem = 9)
    {
        var registers = _clock.Export();
        Configuration.CopyTo(registers.AsSpan(R.ConfigurationStart));
        registers[R.ConfigurationStart] = station;
        registers[R.ConfigurationStart + 5] = (byte)(0xc0 | filingSystem);
        _clock.Import(registers);
    }
    internal void Address(byte portB, byte portA)
    {
        if ((portB & C.CmosAddressStrobe) != 0) _address = portA & (C.CmosBytes - 1);
        _enabled = (portB & C.CmosEnable) != 0;
    }
    internal void Update(byte latch, byte portA)
    {
        var changed = (latch ^ _latch) & C.CmosDataStrobe;
        _latch = latch;
        if (!_enabled || changed == 0) return;
        if ((latch & C.CmosRead) == 0 && (latch & C.CmosDataStrobe) == 0) _clock.WriteRegister(_address, portA);
        if ((latch & C.CmosRead) != 0 && (latch & C.CmosDataStrobe) != 0) _data = _clock.ReadRegister(_address);
    }
    internal byte ReadBus() => _enabled && (_latch & (C.CmosRead | C.CmosDataStrobe)) == (C.CmosRead | C.CmosDataStrobe) ? _data : byte.MaxValue;
    internal byte ReadRegister(int address) => _clock.ReadRegister(address);
    internal void WriteRegister(int address, byte value) => _clock.WriteRegister(address, value);
    internal byte[] Export() => _clock.Export();
    internal void Import(ReadOnlySpan<byte> bytes) => _clock.Import(bytes);
}

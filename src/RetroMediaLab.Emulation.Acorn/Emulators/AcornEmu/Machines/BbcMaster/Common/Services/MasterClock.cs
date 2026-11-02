using R = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Common.Constants.MasterClockConstants;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Common.Constants.MasterConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Common.Services;

/// <summary>HD146818 : registre d'adresse sur PA et PB7, autorisation PB6, données sur le bus lent IC32.</summary>
internal sealed class MasterClock
{
    private readonly byte[] _registers = new byte[C.CmosBytes];
    private int _address;
    private byte _latch;
    private byte _data;
    private bool _enabled;
    private DateTime _frozen;
    private TimeSpan _offset;
    internal MasterClock(byte station = 0, byte filingSystem = 9)
    {
        R.Configuration.CopyTo(_registers.AsSpan(R.ConfigurationStart));
        _registers[R.ConfigurationStart] = station;
        _registers[R.ConfigurationStart + 5] = (byte)(0xc0 | filingSystem);
        _registers[R.StatusB] = R.TwentyFourHour;
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
        if ((latch & C.CmosRead) == 0 && (latch & C.CmosDataStrobe) == 0) WriteRegister(_address, portA);
        if ((latch & C.CmosRead) != 0 && (latch & C.CmosDataStrobe) != 0) _data = ReadRegister(_address);
    }
    internal byte ReadBus() => _enabled && (_latch & (C.CmosRead | C.CmosDataStrobe)) == (C.CmosRead | C.CmosDataStrobe) ? _data : byte.MaxValue;
    private byte Encode(int value) => (_registers[R.StatusB] & R.BinaryData) != 0 ? (byte)value : (byte)((value / 10 << 4) | value % 10);
    private int Decode(byte value) => (_registers[R.StatusB] & R.BinaryData) != 0 ? value : (value >> 4) * 10 + (value & 15);
    internal byte ReadRegister(int address)
    {
        var now = (_registers[R.StatusB] & R.StopUpdates) != 0 ? _frozen : DateTime.Now + _offset;
        return address switch
        {
            R.Seconds => Encode(now.Second), R.Minutes => Encode(now.Minute),
            R.Hours => (_registers[R.StatusB] & R.TwentyFourHour) != 0 ? Encode(now.Hour) : (byte)(Encode((now.Hour + R.HoursPerHalfDay - 1) % R.HoursPerHalfDay + 1) | (now.Hour >= R.HoursPerHalfDay ? R.Afternoon : 0)),
            R.DayOfWeek => Encode((int)now.DayOfWeek + 1), R.Day => Encode(now.Day), R.Month => Encode(now.Month), R.Year => Encode(now.Year % R.CenturyYears),
            R.StatusA => (byte)(_registers[R.StatusA] & R.SevenBits), R.StatusC => 0, R.StatusD => R.ValidRam, _ => _registers[address]
        };
    }
    internal void WriteRegister(int address, byte value)
    {
        if (address == R.StatusB && (value & R.StopUpdates) != 0 && (_registers[R.StatusB] & R.StopUpdates) == 0)
        {
            _frozen = DateTime.Now + _offset;
            foreach (var field in R.TimeFields) _registers[field] = ReadRegister(field);
        }
        var previous = _registers[address];
        _registers[address] = value;
        if (address == R.StatusB && (previous & R.StopUpdates) != 0 && (value & R.StopUpdates) == 0)
        {
            var year = Decode(_registers[R.Year]);
            var hour = Decode((byte)(_registers[R.Hours] & R.SevenBits));
            if ((value & R.TwentyFourHour) == 0) hour = hour % R.HoursPerHalfDay + ((_registers[R.Hours] & R.StopUpdates) != 0 ? R.HoursPerHalfDay : 0);
            if (DateTime.TryParseExact($"{(year < R.YearCenturyThreshold ? R.NewCentury : R.OldCentury) + year:D4}-{Decode(_registers[R.Month]):D2}-{Decode(_registers[R.Day]):D2} {hour:D2}:{Decode(_registers[R.Minutes]):D2}:{Decode(_registers[R.Seconds]):D2}", R.TimestampFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var set)) _offset = set - DateTime.Now;
        }
    }
    internal byte[] Export() => _registers.ToArray();
    internal void Import(ReadOnlySpan<byte> bytes) { if (bytes.Length != C.CmosBytes) throw new ArgumentException(nameof(bytes)); bytes.CopyTo(_registers); }
}

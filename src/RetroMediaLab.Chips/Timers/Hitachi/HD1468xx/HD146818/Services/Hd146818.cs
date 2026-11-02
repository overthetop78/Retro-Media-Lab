using R = RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Constants.MasterClockConstants;

namespace RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Services;

/// <summary>Registres du HD146818 ; les valeurs initiales et le câblage appartiennent à la carte.</summary>
public sealed class Hd146818
:RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Interfaces.IHd146818{
    private readonly byte[] _registers = new byte[Hd146818Constants.RegisterCount];
    private DateTime _frozen;
    private TimeSpan _offset;

    public Hd146818() => _registers[R.StatusB] = R.TwentyFourHour;
    private byte Encode(int value) => (_registers[R.StatusB] & R.BinaryData) != 0 ? (byte)value : (byte)((value / 10 << 4) | value % 10);
    private int Decode(byte value) => (_registers[R.StatusB] & R.BinaryData) != 0 ? value : (value >> 4) * 10 + (value & 15);
    public byte ReadRegister(int address)
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
    public void WriteRegister(int address, byte value)
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
    public byte[] Export() => _registers.ToArray();
    public void Import(ReadOnlySpan<byte> bytes) { if (bytes.Length != Hd146818Constants.RegisterCount) throw new ArgumentException(nameof(bytes)); bytes.CopyTo(_registers); }
}

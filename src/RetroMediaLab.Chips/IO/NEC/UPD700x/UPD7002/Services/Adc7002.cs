using C = RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Constants.Adc7002Constants;

namespace RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Services;

/// <summary>Conversion analogique à quatre canaux ; échantillonnage au départ et fin de conversion sur CB1.</summary>
public sealed class Adc7002(Action<bool> endOfConversion, int clockHz = C.DefaultClockHz)
:RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Interfaces.IAdc7002{
    private readonly ushort[] _channels = new ushort[C.ChannelCount];
    private int _remaining;
    private ushort _sample;
    private ushort _result;
    private byte _status;

    public void Reset()
    {
        _remaining = 0;
        _sample = _result = 0;
        _status = 0;
        endOfConversion(false);
    }

    public void SetChannel(int channel, ushort value) => _channels[channel] = value;

    public byte Read(int register) => (register & C.RegisterMask) switch
    {
        C.StatusRegister => _status,
        C.HighRegister => (byte)(_result >> C.HighShift),
        _ => (byte)(_result & C.LowMask)
    };

    public void Write(int register, byte value)
    {
        if ((register & C.RegisterMask) != C.StatusRegister) return;
        _status = (byte)((value & C.CommandMask) | C.Busy);
        _sample = _channels[value & C.ChannelMask];
        _remaining = (value & C.TwelveBitMode) != 0 ? clockHz * C.TwelveBitMilliseconds / C.MillisecondsPerSecond : clockHz * C.EightBitMilliseconds / C.MillisecondsPerSecond;
        endOfConversion(true);
    }

    public void Tick()
    {
        if (_remaining == 0 || --_remaining != 0) return;
        _result = _sample;
        _status = (byte)((_status & C.CommandMask) | C.Complete | ((_result & C.ResultStatusMask) >> C.ResultStatusShift));
        endOfConversion(false);
    }
}

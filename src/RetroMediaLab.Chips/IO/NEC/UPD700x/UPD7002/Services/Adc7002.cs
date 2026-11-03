using C = RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Constants.Adc7002Constants;
using RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Interfaces;
using RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Enums;

namespace RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Services;

/// <summary>Interface numérique du convertisseur intégrateur µPD7002 ; EOC actif bas.</summary>
public sealed partial class Adc7002 : IAdc7002
{
    private readonly Action<bool> _endOfConversion;
    private readonly long _eightBitTicks;
    private readonly long _twelveBitTicks;
    private readonly long _longIntegrationTicks;
    private readonly bool _externalClock;
    private readonly ushort[] _channels = new ushort[C.ChannelCount];
    private long _remaining;
    private ushort _sample;
    private ushort _result;
    private byte _status;
    private byte _command;

    /// <param name="clockHz">Fréquence de l'appelant Tick/Advance, indépendante de celle du convertisseur.</param>
    /// <param name="conversionClockHz">Horloge X0/X1 ; zéro permet de la fournir explicitement avec Clock.</param>
    public Adc7002(Action<bool> endOfConversion, int clockHz = C.DefaultClockHz,
        int conversionClockHz = C.DefaultConversionClockHz)
    {
        ArgumentNullException.ThrowIfNull(endOfConversion);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(clockHz);
        ArgumentOutOfRangeException.ThrowIfNegative(conversionClockHz);
        _endOfConversion = endOfConversion;
        _externalClock = conversionClockHz == 0;
        _eightBitTicks = ConversionTicks(C.EightBitConversionClocks, clockHz, conversionClockHz);
        _twelveBitTicks = ConversionTicks(C.TwelveBitConversionClocks, clockHz, conversionClockHz);
        _longIntegrationTicks = ConversionTicks(C.LongIntegrationClocks, clockHz, conversionClockHz);
    }

    public bool EndOfConversionHigh => (_status & C.Busy) != 0;
    public bool Flag => (_status & C.FlagMask) != 0;
    public bool Converting => _remaining != 0;
    public bool TestMode { get; private set; }

    public void Reset()
    {
        _remaining = 0;
        _sample = _result = 0;
        _status = 0;
        _command = 0;
        _phase = Adc7002Phase.Idle;
        _charge = _integratedTicks = 0;
        TestMode = false;
        _writeActive = false;
        _endOfConversion(false);
    }

    public void SetChannel(int channel, ushort value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(channel);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(channel, C.ChannelCount);
        if (channel == (_command & C.ChannelMask) && _phase == Adc7002Phase.Integration)
            AccumulateInput();
        _channels[channel] = value;
    }

    public byte Read(int register) => (register & C.RegisterMask) switch
    {
        C.StatusRegister => _status,
        C.HighRegister => ReadHigh(),
        _ => (byte)(_result & C.LowMask)
    };

    public void Write(int register, byte value)
    {
        register &= C.RegisterMask;
        if (register == C.TestRegister)
        {
            // NEC ne spécifie pas les données de conversion après cet accès de fabrication.
            TestMode = true;
            return;
        }
        if (register != C.StatusRegister) return;
        TestMode = false;
        var previousEoc = EndOfConversionHigh;
        PrepareCommand(value);
        StartConversion();
        if (!previousEoc) _endOfConversion(true);
    }

    public void Tick()
    {
        if (_remaining <= 0 || --_remaining != 0) return;
        EndPhase();
    }

    public void Advance(int cycles)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(cycles);
        if (_remaining <= 0 || cycles == 0) return;
        while (_remaining != 0 && cycles >= _remaining)
        {
            cycles -= (int)_remaining;
            _remaining = 0;
            EndPhase();
        }
        if (_remaining != 0) _remaining -= cycles;
    }

    public void Clock()
    {
        if (_remaining >= 0 || ++_remaining != 0) return;
        EndPhase();
    }

    private void Complete()
    {
        _phase = Adc7002Phase.Idle;
        _result = _sample;
        _status = (byte)(_command | C.Complete | ((_result & C.ResultStatusMask) >> C.ResultStatusShift));
        _endOfConversion(false);
    }

    private byte ReadHigh()
    {
        var value = (byte)(_result >> C.HighShift);
        if (!EndOfConversionHigh)
        {
            _status |= C.Busy;
            _endOfConversion(true);
        }
        return value;
    }

    private void PrepareCommand(byte value)
    {
        _remaining = 0;
        _phase = Adc7002Phase.Idle;
        _charge = _integratedTicks = 0;
        _command = (byte)(value & C.CommandMask);
        _status = (byte)((_status & C.PreviousConversionStatusMask) | C.Busy);
        TestMode = false;
    }

    private static long ConversionTicks(int clocks, int hostHz, int conversionHz) => conversionHz == 0
        ? clocks
        : ((long)clocks * hostHz + conversionHz - 1) / conversionHz;
}

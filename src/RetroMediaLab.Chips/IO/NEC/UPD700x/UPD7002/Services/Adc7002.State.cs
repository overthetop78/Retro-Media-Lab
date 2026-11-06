using RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Contracts;
using C = RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Constants.Adc7002Constants;

namespace RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Services;

public sealed partial class Adc7002
{
    /// <summary>Conserve les échéances absolues, le front WR suspendu et la conversion partiellement intégrée.</summary>
    public Adc7002State CaptureState() => new()
    {
        FormatVersion = C.StateFormatVersion,
        InputClockFrequency = InputClockFrequency,
        ConversionClockFrequency = ConversionClockFrequency,
        Channels = _channels.ToArray(),
        TestMode = TestMode,
        WriteActive = _writeActive,
        WriteRegister = _writeRegister,
        WriteData = _writeData,
        InputClockCount = _inputClockCount,
        PublishedClockEvent = _publishedClockEvent,
        Phase = _phase,
        Charge = _charge,
        IntegratedTicks = _integratedTicks,
        Remaining = _remaining,
        Sample = _sample,
        Result = _result,
        Status = _status,
        Command = _command,
    };

    /// <summary>Restaure sans déplacer l'horloge ni émettre d'EOC artificiel ; le câblage est conservé.</summary>
    public void RestoreState(Adc7002State state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.FormatVersion != C.StateFormatVersion || state.InputClockFrequency != InputClockFrequency
            || state.ConversionClockFrequency != ConversionClockFrequency || state.Channels is not { Length: C.ChannelCount }
            || !Enum.IsDefined(state.Phase)) throw new ArgumentException(nameof(state));
        state.Channels.CopyTo(_channels, 0);
        TestMode = state.TestMode;
        _writeActive = state.WriteActive;
        _writeRegister = state.WriteRegister;
        _writeData = state.WriteData;
        _inputClockCount = state.InputClockCount;
        _publishedClockEvent = state.PublishedClockEvent;
        _phase = state.Phase;
        _charge = state.Charge;
        _integratedTicks = state.IntegratedTicks;
        _remaining = state.Remaining;
        _sample = state.Sample;
        _result = state.Result;
        _status = state.Status;
        _command = state.Command;
    }
}

using RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Contracts;
using C = RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Constants.Acia6850Constants;

namespace RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Services;

public sealed partial class Acia6850
{
    /// <summary>Conserve les bits partiellement transférés et les acquittements modem différés.</summary>
    public Acia6850State CaptureState() => new()
    {
        FormatVersion = C.StateFormatVersion,
        TicksPerSecond = _ticksPerSecond,
        SerialClock = _serialClock,
        RtsHigh = RtsHigh,
        WordFormat = WordFormat,
        TransmitControl = TransmitControl,
        Control = _control,
        Data = _data,
        Status = _status,
        MasterReset = _masterReset,
        FirstMasterReset = _firstMasterReset,
        DcdInput = _dcdInput,
        DcdObserved = _dcdObserved,
        DcdPending = _dcdPending,
        DcdStatusRead = _dcdStatusRead,
        CtsBlocked = _ctsBlocked,
        OverrunPending = _overrunPending,
        ReceiveIrqEnabled = _receiveIrqEnabled,
        TransmitIrqEnabled = _transmitIrqEnabled,
        Break = _break,
        Irq = _irq,
        Divider = _divider,
        DataBits = _dataBits,
        HasParity = _hasParity,
        OddParity = _oddParity,
        StopBits = _stopBits,
        BaudRateOverride = _baudRateOverride,
        BaudRate = _baudRate,
        InputClockRate = _inputClockRate,
        ClockPhase = _clockPhase,
        ReceiveStep = _receiveStep,
        ReceiveValue = _receiveValue,
        ReceiveClockCounter = _receiveClockCounter,
        ReceiveParity = _receiveParity,
        ReceiveFraming = _receiveFraming,
        ReceiveDiscard = _receiveDiscard,
        ReceiveWaitMark = _receiveWaitMark,
        TransmitState = _transmitState,
        TransmitData = _transmitData,
        TransmitShift = _transmitShift,
        TransmitPending = _transmitPending,
        TransmitLine = _transmitLine,
        TransmitParity = _transmitParity,
        TransmitStep = _transmitStep,
        TransmitStopCount = _transmitStopCount,
        TransmitClockCounter = _transmitClockCounter,
    };

    /// <summary>Restaure sans horloge supplémentaire ni notification ; les récepteurs existants restent raccordés.</summary>
    public void RestoreState(Acia6850State state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.FormatVersion != C.StateFormatVersion || state.TicksPerSecond != _ticksPerSecond || state.SerialClock != _serialClock
            || !Enum.IsDefined(state.WordFormat) || !Enum.IsDefined(state.TransmitControl) || !Enum.IsDefined(state.TransmitState))
            throw new ArgumentException(nameof(state));
        RtsHigh = state.RtsHigh;
        WordFormat = state.WordFormat;
        TransmitControl = state.TransmitControl;
        _control = state.Control;
        _data = state.Data;
        _status = state.Status;
        _masterReset = state.MasterReset;
        _firstMasterReset = state.FirstMasterReset;
        _dcdInput = state.DcdInput;
        _dcdObserved = state.DcdObserved;
        _dcdPending = state.DcdPending;
        _dcdStatusRead = state.DcdStatusRead;
        _ctsBlocked = state.CtsBlocked;
        _overrunPending = state.OverrunPending;
        _receiveIrqEnabled = state.ReceiveIrqEnabled;
        _transmitIrqEnabled = state.TransmitIrqEnabled;
        _break = state.Break;
        _irq = state.Irq;
        _divider = state.Divider;
        _dataBits = state.DataBits;
        _hasParity = state.HasParity;
        _oddParity = state.OddParity;
        _stopBits = state.StopBits;
        _baudRateOverride = state.BaudRateOverride;
        _baudRate = state.BaudRate;
        _inputClockRate = state.InputClockRate;
        _clockPhase = state.ClockPhase;
        _receiveStep = state.ReceiveStep;
        _receiveValue = state.ReceiveValue;
        _receiveClockCounter = state.ReceiveClockCounter;
        _receiveParity = state.ReceiveParity;
        _receiveFraming = state.ReceiveFraming;
        _receiveDiscard = state.ReceiveDiscard;
        _receiveWaitMark = state.ReceiveWaitMark;
        _transmitState = state.TransmitState;
        _transmitData = state.TransmitData;
        _transmitShift = state.TransmitShift;
        _transmitPending = state.TransmitPending;
        _transmitLine = state.TransmitLine;
        _transmitParity = state.TransmitParity;
        _transmitStep = state.TransmitStep;
        _transmitStopCount = state.TransmitStopCount;
        _transmitClockCounter = state.TransmitClockCounter;
    }
}

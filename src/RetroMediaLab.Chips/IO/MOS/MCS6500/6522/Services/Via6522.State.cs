using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Contracts;
using C = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Constants.Via6522Constants;

namespace RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;

public sealed partial class Via6522
{
    /// <summary>Copie les mémoires et les événements différés, y compris entre les deux fronts PHI2.</summary>
    public Via6522State CaptureState() => new()
    {
        FormatVersion = C.StateFormatVersion,
        Registers = _registers.ToArray(),
        Phi2High = _phi2High,
        Timer1 = _timer1,
        Timer2 = _timer2,
        Latch1 = _latch1,
        Timer1Running = _timer1Running,
        Timer2Running = _timer2Running,
        Timer2Armed = _timer2Armed,
        InterruptFlags = _interruptFlags,
        InterruptEnabled = _interruptEnabled,
        Irq = _irq,
        Ca1 = _ca1,
        Ca2 = _ca2,
        Cb1 = _cb1,
        Cb2 = _cb2,
        Ca2Output = _ca2Output,
        Cb2Output = _cb2Output,
        Ca2Pulse = _ca2Pulse,
        Cb2Pulse = _cb2Pulse,
        InputLatchA = _inputLatchA,
        InputLatchB = _inputLatchB,
        Pb6Input = _pb6Input,
        LastPb6 = _lastPb6,
        ControlModeA = _controlModeA,
        ControlModeB = _controlModeB,
        ShiftActive = _shiftActive,
        ShiftArmed = _shiftArmed,
        ShiftClock = _shiftClock,
        ShiftClockPin = _shiftClockPin,
        ShiftOutputComplete = _shiftOutputComplete,
        ShiftFinishOutputPending = _shiftFinishOutputPending,
        ShiftBits = _shiftBits,
        ShiftClockDelay = _shiftClockDelay,
        ExternalShiftPending = _externalShiftPending,
        InternalShiftPending = _internalShiftPending,
        InternalShiftValue = _internalShiftValue,
        ShiftInterruptPending = _shiftInterruptPending,
        ShiftLeadingPending = _shiftLeadingPending,
        ShiftMode = _shiftMode,
        ShiftOutput = _shiftOutput,
        ShiftUsesTimer = _shiftUsesTimer,
        ShiftInternalClock = _shiftInternalClock,
        ShiftSystemClock = _shiftSystemClock,
        Timer1Armed = _timer1Armed,
        Timer1Reload = _timer1Reload,
        Timer1LoadDelay = _timer1LoadDelay,
        Timer2LoadDelay = _timer2LoadDelay,
        Timer2Reload = _timer2Reload,
        TimerPb7 = _timerPb7,
        Timer1JustFired = _timer1JustFired,
        Timer2JustFired = _timer2JustFired,
        TimerInterruptsPending = _timerInterruptsPending,
        Timer2PulseSampled = _timer2PulseSampled,
        Timer1Continuous = _timer1Continuous,
        Timer1Output = _timer1Output,
        Timer2PulseMode = _timer2PulseMode,
    };

    /// <summary>Restaure sans front ni notification artificiels ; les récepteurs raccordés sont conservés.</summary>
    public void RestoreState(Via6522State state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.FormatVersion != C.StateFormatVersion || state.Registers is not { Length: C.RegisterCount }
            || !Enum.IsDefined(state.ShiftMode) || !Enum.IsDefined(state.ControlModeA) || !Enum.IsDefined(state.ControlModeB))
            throw new ArgumentException(nameof(state));
        state.Registers.CopyTo(_registers, 0);
        _phi2High = state.Phi2High;
        _timer1 = state.Timer1;
        _timer2 = state.Timer2;
        _latch1 = state.Latch1;
        _timer1Running = state.Timer1Running;
        _timer2Running = state.Timer2Running;
        _timer2Armed = state.Timer2Armed;
        _interruptFlags = state.InterruptFlags;
        _interruptEnabled = state.InterruptEnabled;
        _irq = state.Irq;
        _ca1 = state.Ca1;
        _ca2 = state.Ca2;
        _cb1 = state.Cb1;
        _cb2 = state.Cb2;
        _ca2Output = state.Ca2Output;
        _cb2Output = state.Cb2Output;
        _ca2Pulse = state.Ca2Pulse;
        _cb2Pulse = state.Cb2Pulse;
        _inputLatchA = state.InputLatchA;
        _inputLatchB = state.InputLatchB;
        _pb6Input = state.Pb6Input;
        _lastPb6 = state.LastPb6;
        _controlModeA = state.ControlModeA;
        _controlModeB = state.ControlModeB;
        _shiftActive = state.ShiftActive;
        _shiftArmed = state.ShiftArmed;
        _shiftClock = state.ShiftClock;
        _shiftClockPin = state.ShiftClockPin;
        _shiftOutputComplete = state.ShiftOutputComplete;
        _shiftFinishOutputPending = state.ShiftFinishOutputPending;
        _shiftBits = state.ShiftBits;
        _shiftClockDelay = state.ShiftClockDelay;
        _externalShiftPending = state.ExternalShiftPending;
        _internalShiftPending = state.InternalShiftPending;
        _internalShiftValue = state.InternalShiftValue;
        _shiftInterruptPending = state.ShiftInterruptPending;
        _shiftLeadingPending = state.ShiftLeadingPending;
        _shiftMode = state.ShiftMode;
        _shiftOutput = state.ShiftOutput;
        _shiftUsesTimer = state.ShiftUsesTimer;
        _shiftInternalClock = state.ShiftInternalClock;
        _shiftSystemClock = state.ShiftSystemClock;
        _timer1Armed = state.Timer1Armed;
        _timer1Reload = state.Timer1Reload;
        _timer1LoadDelay = state.Timer1LoadDelay;
        _timer2LoadDelay = state.Timer2LoadDelay;
        _timer2Reload = state.Timer2Reload;
        _timerPb7 = state.TimerPb7;
        _timer1JustFired = state.Timer1JustFired;
        _timer2JustFired = state.Timer2JustFired;
        _timerInterruptsPending = state.TimerInterruptsPending;
        _timer2PulseSampled = state.Timer2PulseSampled;
        _timer1Continuous = state.Timer1Continuous;
        _timer1Output = state.Timer1Output;
        _timer2PulseMode = state.Timer2PulseMode;
    }
}

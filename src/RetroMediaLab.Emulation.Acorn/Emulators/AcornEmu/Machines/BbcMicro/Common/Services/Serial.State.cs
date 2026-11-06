using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Contracts;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

internal sealed partial class Serial
{
    internal SerialState CaptureState() => new()
    {
        Acia = _acia.CaptureState(),
        Ula = _ula.CaptureState(),
        OverridePhase = _overridePhase,
        BaudRateOverride = _baudRateOverride,
        ModemPinsKnown = _modemPinsKnown,
        CtsHigh = _ctsHigh,
        DcdHigh = _dcdHigh,
        ReceiveData = _receiveData,
        Rs423Selected = _rs423Selected,
        ClockDivider = _clockDivider,
        OverrideClockStep = _overrideClockStep
    };

    internal void RestoreState(SerialState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _acia.RestoreState(state.Acia);
        _ula.RestoreState(state.Ula);
        _overridePhase = state.OverridePhase;
        _baudRateOverride = state.BaudRateOverride;
        _modemPinsKnown = state.ModemPinsKnown;
        _ctsHigh = state.CtsHigh;
        _dcdHigh = state.DcdHigh;
        _receiveData = state.ReceiveData;
        _rs423Selected = state.Rs423Selected;
        _clockDivider = state.ClockDivider;
        _overrideClockStep = state.OverrideClockStep;
    }
}

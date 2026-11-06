using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Contracts;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

internal sealed partial class Econet
{
    internal EconetState CaptureState() => new()
    {
        Enabled = _enabled,
        Station = _station,
        Controller = _controller.CaptureState(),
        NmiEnabled = _nmiEnabled,
        Driving = _driving,
        ClockPresent = _clockPresent,
        ClearToSend = _clearToSend,
        Idle = _idle,
        ControllerIrq = _controllerIrq,
        Nmi = _nmi,
        Rts = _rts,
        Transmitting = _transmitting,
    };

    internal void RestoreState(EconetState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Enabled != _enabled || state.Station != _station) throw new ArgumentException(nameof(state));
        _controller.RestoreState(state.Controller);
        _nmiEnabled = state.NmiEnabled;
        _driving = state.Driving;
        _clockPresent = state.ClockPresent;
        _clearToSend = state.ClearToSend;
        _idle = state.Idle;
        _controllerIrq = state.ControllerIrq;
        _nmi = state.Nmi;
        _rts = state.Rts;
        _transmitting = state.Transmitting;
    }
}
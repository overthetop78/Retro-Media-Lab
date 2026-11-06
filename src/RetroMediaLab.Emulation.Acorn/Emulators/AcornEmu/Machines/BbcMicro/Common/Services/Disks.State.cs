using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Contracts;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

internal sealed partial class Disks
{
    internal DisksState CaptureState() => new(_enabled, _wd1770, _control, _nmi, (int[])_speeds.Clone(),
        _intelDrive?.CaptureState(), _wd?.CaptureState());

    internal void RestoreState(DisksState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Enabled != _enabled || state.Wd1770 != _wd1770 || state.Speeds?.Length != _speeds.Length
            || (_intelDrive is null) != (state.IntelDrive is null) || (_wd is null) != (state.WdDrive is null))
            throw new ArgumentException(nameof(state));
        if (_intelDrive is not null) _intelDrive.RestoreState(state.IntelDrive!);
        if (_wd is not null) _wd.RestoreState(state.WdDrive!);
        state.Speeds.CopyTo(_speeds, 0);
        _control = state.Control;
        _nmi = state.Nmi;
    }
}

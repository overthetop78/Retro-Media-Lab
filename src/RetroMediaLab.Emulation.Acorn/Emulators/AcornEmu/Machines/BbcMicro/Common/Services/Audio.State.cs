using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Contracts;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.AudioConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

internal sealed partial class Audio
{
    internal AudioState CaptureState() => new()
    {
        FormatVersion = C.StateFormatVersion,
        Chip = _chip.CaptureState(),
        Samples = _samples.ToArray(),
        Phase = _phase,
        Level = _level,
        PendingTicks = _pendingTicks,
        ClockCountdown = _clockCountdown,
        Area = _area,
        PreviousInput = _previousInput,
        PreviousOutput = _previousOutput
    };

    internal void RestoreState(AudioState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.FormatVersion != C.StateFormatVersion || state.Samples is null
            || (uint)state.Phase >= C.CpuClock || state.PendingTicks < 0 || state.ClockCountdown < 1
            || !double.IsFinite(state.PreviousInput) || !double.IsFinite(state.PreviousOutput))
            throw new ArgumentException(nameof(state));
        _chip.RestoreState(state.Chip);
        _samples.Clear();
        _samples.AddRange(state.Samples);
        _phase = state.Phase;
        _level = state.Level;
        _pendingTicks = state.PendingTicks;
        _clockCountdown = state.ClockCountdown;
        _area = state.Area;
        _previousInput = state.PreviousInput;
        _previousOutput = state.PreviousOutput;
    }
}

using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Contracts;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

internal sealed partial class Cassette
{
    internal CassetteState CaptureState() => new()
    {
        Signal = CopySignal(_signal),
        Position = _position,
        SamplePhase = _samplePhase,
        InputLevel = _inputLevel,
        InputPresented = _inputPresented,
        PresentedPosition = _presentedPosition,
        SampleIncrement = _sampleIncrement,
        Recorded = _recorded?.ToArray(),
        Writable = _writable,
        Settings = _settings,
        SeekTarget = _seekTarget,
        Operation = _operation,
        LatencyTicks = _latencyTicks,
        ClockRequired = _clockRequired,
        PublishedActive = _publishedActivity.Active,
        PublishedOperation = _publishedActivity.Operation,
        PublishedState = _publishedActivity.State,
        HasChanges = HasChanges,
        State = _state
    };

    internal void RestoreState(CassetteState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Position < 0 || state.SamplePhase < 0 || state.LatencyTicks < 0
            || state.Settings is null || state.Signal is { SampleRate: <= 0 }
            || !Enum.IsDefined(state.State) || !Enum.IsDefined(state.PublishedState))
            throw new ArgumentException(nameof(state));
        _signal = CopySignal(state.Signal);
        _position = state.Position;
        _samplePhase = state.SamplePhase;
        _inputLevel = state.InputLevel;
        _inputPresented = state.InputPresented;
        _presentedPosition = state.PresentedPosition;
        _sampleIncrement = state.SampleIncrement;
        _recorded = state.Recorded?.ToList();
        _writable = state.Writable;
        _settings = state.Settings;
        _seekTarget = state.SeekTarget;
        _operation = state.Operation;
        _latencyTicks = state.LatencyTicks;
        _clockRequired = state.ClockRequired;
        _publishedActivity = (state.PublishedActive, state.PublishedOperation, state.PublishedState);
        HasChanges = state.HasChanges;
        _state = state.State;
    }

    private static EmulationCassetteSignal? CopySignal(EmulationCassetteSignal? signal) => signal is null ? null : new(
        signal.Samples.ToArray(), signal.SampleRate, signal.BlockPositions?.ToArray(),
        signal.Blocks?.Select(block => new EmulationCassetteBlock(block.SamplePosition, block.Data.ToArray(),
            block.IntegrityValid, new Dictionary<string, string>(block.Metadata))).ToArray());
}

using RetroMediaLab.Emulation;

namespace RetroMediaLab.Emulation.Atari.Common.Services;

internal sealed class TapeInputController
{
    private readonly object _gate = new();
    private readonly Queue<EmulationKey> _pulses = new();
    private readonly IReadOnlyDictionary<EmulationTapeCommand, IReadOnlyList<EmulationKey>> _keyCommands;
    private EmulationInputSnapshot _physicalInput = EmulationInputSnapshot.Empty;
    private int _automaticReturnFrames;

    internal IReadOnlySet<EmulationTapeCommand> AvailableCommands => _keyCommands.Keys.ToHashSet();

    internal TapeInputController(MachineConfiguration configuration, IEmulatorRuntimePolicy? runtimePolicy = null)
    {
        var policy = runtimePolicy ?? new Engine().SelectedAdapter(configuration);
        _keyCommands = policy?.GetTapeKeyCommands(configuration) ?? new Dictionary<EmulationTapeCommand, IReadOnlyList<EmulationKey>>();
        _automaticReturnFrames = _keyCommands.ContainsKey(EmulationTapeCommand.Play) ? policy?.GetTapeBootReturnDelayFrames(configuration) ?? -1 : -1;
    }

    internal void SetPhysicalInput(EmulationInputSnapshot snapshot)
    {
        lock (_gate) _physicalInput = snapshot;
    }

    internal void Execute(EmulationTapeCommand command)
    {
        lock (_gate)
        {
            if (!_keyCommands.TryGetValue(command, out var keys)) throw new NotSupportedException(command.ToString());
            foreach (var key in keys) _pulses.Enqueue(key);
        }
    }

    internal EmulationInputSnapshot NextFrame()
    {
        lock (_gate)
        {
            if (_automaticReturnFrames > 0) _automaticReturnFrames--;
            else if (_automaticReturnFrames == 0)
            {
                Execute(EmulationTapeCommand.Play);
                _automaticReturnFrames = -1;
            }

            if (_pulses.Count == 0) return _physicalInput;
            var keys = _physicalInput.Keys.ToHashSet();
            keys.Add(_pulses.Dequeue());
            return _physicalInput with { Keys = keys };
        }
    }
}
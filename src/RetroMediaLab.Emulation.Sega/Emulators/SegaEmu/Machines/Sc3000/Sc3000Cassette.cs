using RetroMediaLab.MediaEngine.Contracts;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.Sc3000;

internal sealed class Sc3000Cassette
{
    private static readonly IReadOnlySet<EmulationTapeCommand> s_stoppedCommands =
        new HashSet<EmulationTapeCommand> { EmulationTapeCommand.Play, EmulationTapeCommand.Rewind };
    private static readonly IReadOnlySet<EmulationTapeCommand> s_playingCommands =
        new HashSet<EmulationTapeCommand> { EmulationTapeCommand.Stop, EmulationTapeCommand.Pause, EmulationTapeCommand.Rewind };
    private static readonly IReadOnlySet<EmulationTapeCommand> s_pausedCommands =
        new HashSet<EmulationTapeCommand> { EmulationTapeCommand.Play, EmulationTapeCommand.Stop, EmulationTapeCommand.Rewind };
    internal static IReadOnlySet<EmulationTapeCommand> EmptyCommands { get; } = new HashSet<EmulationTapeCommand>();
    private static readonly IReadOnlySet<EmulationTapeCommand> s_endCommands =
        new HashSet<EmulationTapeCommand> { EmulationTapeCommand.Rewind };

    private MediaTapeSignal? _signal;
    private volatile EmulationTapeState _state;
    private ulong _frameStart;
    private ulong _playStart;
    private int _position;

    internal EmulationTapeState State => _state;
    internal IReadOnlySet<EmulationTapeCommand> Commands => _state switch
    {
        EmulationTapeState.Stopped => s_stoppedCommands,
        EmulationTapeState.Paused => s_pausedCommands,
        EmulationTapeState.Playing => s_playingCommands,
        EmulationTapeState.EndOfTape => s_endCommands,
        _ => EmptyCommands,
    };

    internal void Insert(MediaTapeSignal signal)
    {
        if (signal.SampleRate <= 0)
            throw new ArgumentOutOfRangeException(nameof(signal));
        _signal = signal;
        _position = 0;
        _state = signal.Samples.IsEmpty ? EmulationTapeState.EndOfTape : EmulationTapeState.Stopped;
    }

    internal void Eject()
    {
        _signal = null;
        _position = 0;
        _state = EmulationTapeState.Empty;
    }

    internal void Execute(EmulationTapeCommand command, uint clock)
    {
        if (!Commands.Contains(command))
            throw new NotSupportedException(command.ToString());
        switch (command)
        {
            case EmulationTapeCommand.Play:
                _playStart = _frameStart;
                _state = EmulationTapeState.Playing;
                break;
            case EmulationTapeCommand.Stop:
            case EmulationTapeCommand.Pause:
                if (_state == EmulationTapeState.Playing)
                    _position = PositionAt(_frameStart, clock);
                _state = command == EmulationTapeCommand.Pause ? EmulationTapeState.Paused : EmulationTapeState.Stopped;
                break;
            case EmulationTapeCommand.Rewind:
                _position = 0;
                _state = _signal!.Samples.IsEmpty ? EmulationTapeState.EndOfTape : EmulationTapeState.Stopped;
                break;
        }
    }

    internal bool ReadInput(uint cycles, uint clock)
    {
        if (_state != EmulationTapeState.Playing)
            return true;
        int position = PositionAt(_frameStart + cycles, clock);
        return position == _signal!.Samples.Length || _signal.Samples.Span[position] >= 0;
    }

    internal void EndFrame(uint cycles, uint clock)
    {
        _frameStart += cycles;
        if (_state == EmulationTapeState.Playing && PositionAt(_frameStart, clock) == _signal!.Samples.Length)
        {
            _position = _signal.Samples.Length;
            _state = EmulationTapeState.EndOfTape;
        }
    }

    private int PositionAt(ulong ticks, uint clock)
    {
        MediaTapeSignal signal = _signal!;
        ulong elapsed = ticks - _playStart;
        ulong samples = elapsed / clock * (uint)signal.SampleRate
            + elapsed % clock * (uint)signal.SampleRate / clock;
        return (int)Math.Min((ulong)signal.Samples.Length, (ulong)_position + samples);
    }
}

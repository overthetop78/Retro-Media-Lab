using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Enums;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.AnalogTapeConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

/// <summary>Magnétophone analogique, avec broches directes ou modem FSK 1 200/2 400 Hz.</summary>
/// <remarks>La ROM effectue elle-même la sérialisation. Ce composant présente les
/// bits électriques et conserve le signal ; il ne charge pas directement un programme.</remarks>
internal sealed class AnalogTape(int clock, TapeSignalMode signalMode = TapeSignalMode.FrequencyShiftKeying) : IEmulatorTapeTransport, RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces.IElectronTapePins
{
    private EmulationTapeSignal? _signal;
    private EmulationTapeSettings _settings = new();
    private List<short>? _recorded;
    private int _position;
    private long _samplePhase;
    private int _edgeTicks;
    private bool _inputLevel;
    private bool _hasLevel;
    private bool _outputBit;
    private bool _outputLevel;
    private int _outputPhase;
    private bool _writable;
    private int? _seekTarget;
    private long _latency;
    private EmulationTapeCommand? _operation;
    private EmulationTapeState _state = EmulationTapeState.Empty;
    public event Action? ActivityChanged;

    internal bool InputBit { get; private set; } = true;
    internal bool InputPresent => _latency == 0 && _state == EmulationTapeState.Playing && _hasLevel;
    internal bool InputHigh => InputPresent && _inputLevel;
    internal bool HasChanges { get; private set; }
    internal int Position => _position;
    internal short MonitorSample => _latency > 0 ? (short)0 : _state == EmulationTapeState.Recording
        ? (_outputLevel ? C.RecordingAmplitude : (short)-C.RecordingAmplitude)
        : _state == EmulationTapeState.Playing && _position < Length
            ? _recorded is null ? _signal!.Samples.Span[_position] : _recorded[_position]
            : (short)0;
    internal EmulationTapeSignal RecordedSignal => new(
        _recorded is null ? _signal?.Samples ?? ReadOnlyMemory<short>.Empty : _recorded.ToArray(),
        _signal?.SampleRate ?? C.RecordingSampleRate);
    public EmulationTapeState TapeState => _state;
    public bool TapeActive => _state != EmulationTapeState.Paused &&
        (_seekTarget is not null || _state is EmulationTapeState.Playing or EmulationTapeState.Recording);
    public EmulationTapeCommand? TapeOperation => _operation;
    public IReadOnlySet<EmulationTapeCommand> TapeCommands => _signal is null ? C.EmptyCommands
        : _writable && !_settings.WriteProtected ? C.RecordingCommands : C.Commands;

    internal void Insert(EmulationTapeSignal signal, bool writable)
    {
        if (signal.SampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(signal));
        _signal = signal;
        _recorded = null;
        _position = 0;
        _samplePhase = 0;
        _seekTarget = null;
        _writable = writable;
        HasChanges = false;
        _state = EmulationTapeState.Stopped;
        _operation = null;
        ResetHardware();
    }

    internal void Eject()
    {
        _signal = null;
        _recorded = null;
        _position = 0;
        _samplePhase = 0;
        _seekTarget = null;
        _writable = false;
        HasChanges = false;
        _state = EmulationTapeState.Empty;
        _operation = null;
        ResetHardware();
    }

    internal void Configure(EmulationTapeSettings settings)
    {
        _settings = settings;
        if (settings.WriteProtected && _state == EmulationTapeState.Recording) Stop();
        if (settings.Mode != EmulationTapePlaybackMode.RealTime && _seekTarget is { } target)
        {
            _position = target;
            Stop();
        }
    }

    internal void RefreshIndex(EmulationTapeSignal signal)
    {
        _signal = signal;
        _recorded = null;
        _position = Math.Min(_position, signal.Samples.Length);
    }

    internal void MarkSaved() => HasChanges = false;

    internal void ResetHardware()
    {
        _edgeTicks = 0;
        _hasLevel = false;
        _outputPhase = 0;
        _outputBit = false;
        _outputLevel = false;
        InputBit = true;
        ActivityChanged?.Invoke();
    }

    internal void SetOutputBit(bool value)
    {
        if (_outputBit == value) return;
        _outputBit = value;
        _outputPhase = 0;
        _outputLevel = false;
    }

    internal void SetOutputLevel(bool value) => _outputLevel = value;

    public void ExecuteTape(EmulationTapeCommand command)
    {
        if (!TapeCommands.Contains(command)) throw new NotSupportedException(command.ToString());
        switch (command)
        {
            case EmulationTapeCommand.Play:
                _seekTarget = null;
                _state = _position < Length ? EmulationTapeState.Playing : EmulationTapeState.EndOfTape;
                _operation = _state == EmulationTapeState.Playing ? command : null;
                _latency = (long)clock * _settings.ReadLatencyMilliseconds / C.MillisecondsPerSecond;
                break;
            case EmulationTapeCommand.Record:
                _seekTarget = null;
                _state = EmulationTapeState.Recording;
                _operation = command;
                _latency = (long)clock * _settings.WriteLatencyMilliseconds / C.MillisecondsPerSecond;
                break;
            case EmulationTapeCommand.Stop:
                Stop();
                break;
            case EmulationTapeCommand.Pause:
                if (_state == EmulationTapeState.Paused)
                    _state = _seekTarget is not null ? EmulationTapeState.Stopped
                        : _operation == EmulationTapeCommand.Record ? EmulationTapeState.Recording
                        : _operation == EmulationTapeCommand.Play ? EmulationTapeState.Playing : EmulationTapeState.Stopped;
                else if (TapeActive) _state = EmulationTapeState.Paused;
                break;
            case EmulationTapeCommand.Rewind:
                Seek((_signal!.BlockPositions ?? []).Where(value => value < _position).DefaultIfEmpty(0).Max(), command);
                break;
            case EmulationTapeCommand.FastForward:
                Seek((_signal!.BlockPositions ?? []).Where(value => value > _position).DefaultIfEmpty(Length).Min(), command);
                break;
        }
        ActivityChanged?.Invoke();
    }

    internal void Tick(bool motorEnabled = true)
    {
        if (!TapeActive || _signal is null) return;
        if (!motorEnabled && _seekTarget is null) return;
        if (_latency > 0) { _latency--; return; }
        if (_seekTarget is { } target)
        {
            AdvancePhase(_settings.WindMultiplier);
            while (_samplePhase >= clock && _position != target)
            {
                _samplePhase -= clock;
                _position += Math.Sign(target - _position);
            }
            if (_position == target) Stop();
            return;
        }
        if (_state == EmulationTapeState.Recording)
        {
            Record();
            return;
        }
        if (_position >= Length)
        {
            Stop();
            _state = EmulationTapeState.EndOfTape;
            return;
        }
        _edgeTicks++;
        var sample = _recorded is null ? _signal.Samples.Span[_position] : _recorded[_position];
        var level = sample > 0;
        if (_hasLevel && sample != 0 && level != _inputLevel)
        {
            InputBit = _edgeTicks < (long)clock * C.HalfWaveThresholdNumerator / C.HalfWaveThresholdDenominator;
            _edgeTicks = 0;
        }
        if (!_hasLevel && sample != 0) _edgeTicks = 0;
        _hasLevel = sample != 0;
        _inputLevel = level;
        if (_edgeTicks > clock / C.SilenceTimeoutFrequency) InputBit = true;
        AdvancePhase(1);
        while (_samplePhase >= clock) { _samplePhase -= clock; _position++; }
    }

    private int Length => _recorded?.Count ?? _signal?.Samples.Length ?? 0;

    private void AdvancePhase(int multiplier) => _samplePhase +=
        (long)(_signal?.SampleRate ?? C.RecordingSampleRate) * _settings.SpeedPercent * multiplier / C.Percent;

    private void Record()
    {
        if (signalMode == TapeSignalMode.FrequencyShiftKeying)
        {
            _outputPhase += C.HalfWavesPerCycle * (_outputBit ? C.MarkFrequency : C.SpaceFrequency);
            if (_outputPhase >= clock) { _outputPhase -= clock; _outputLevel = !_outputLevel; }
        }
        AdvancePhase(1);
        while (_samplePhase >= clock)
        {
            _samplePhase -= clock;
            _recorded ??= _signal!.Samples.ToArray().ToList();
            var sample = _outputLevel ? C.RecordingAmplitude : (short)-C.RecordingAmplitude;
            if (_position < _recorded.Count) _recorded[_position] = sample;
            else _recorded.Add(sample);
            _position++;
            HasChanges = true;
        }
    }

    private void Seek(int position, EmulationTapeCommand operation)
    {
        _seekTarget = Math.Clamp(position, 0, Length);
        _operation = operation;
        _state = EmulationTapeState.Stopped;
        _samplePhase = 0;
        _latency = 0;
        if (_settings.Mode != EmulationTapePlaybackMode.RealTime || _seekTarget == _position)
        {
            _position = _seekTarget.Value;
            Stop();
        }
    }

    private void Stop()
    {
        _state = _signal is null ? EmulationTapeState.Empty : EmulationTapeState.Stopped;
        _operation = null;
        _seekTarget = null;
        _samplePhase = 0;
        _latency = 0;
        _edgeTicks = 0;
        _hasLevel = false;
        InputBit = true;
        ActivityChanged?.Invoke();
    }
    bool RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces.IElectronTapePins.InputPresent =>
        _latency == 0 && _state == EmulationTapeState.Playing;
    bool RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces.IElectronTapePins.InputLevel => _inputLevel;
    void RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces.IElectronTapePins.SetOutputLevel(bool value) => SetOutputLevel(value);
    void RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces.IElectronTapePins.Tick(bool motor) => Tick(motor);
}

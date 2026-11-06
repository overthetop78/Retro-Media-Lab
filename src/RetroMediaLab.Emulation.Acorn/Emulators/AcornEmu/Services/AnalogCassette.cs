using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Enums;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.AnalogCassetteConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

/// <summary>Magnétophone analogique, avec broches directes ou modem FSK 1 200/2 400 Hz.</summary>
/// <remarks>La ROM effectue elle-même la sérialisation. Ce composant présente les
/// bits électriques et conserve le signal ; il ne charge pas directement un programme.</remarks>
internal sealed class AnalogCassette(int clock, CassetteSignalMode signalMode = CassetteSignalMode.FrequencyShiftKeying) : IEmulatorCassetteTransport, RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces.IElectronCassettePins
{
    private EmulationCassetteSignal? _signal;
    private EmulationCassetteSettings _settings = new();
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
    private EmulationCassetteCommand? _operation;
    private EmulationCassetteState _state = EmulationCassetteState.Empty;
    public event Action? ActivityChanged;

    internal bool InputBit { get; private set; } = true;
    internal bool InputPresent => _latency == 0 && _state == EmulationCassetteState.Playing && _hasLevel;
    internal bool InputHigh => InputPresent && _inputLevel;
    internal bool HasChanges { get; private set; }
    internal int Position => _position;
    internal short MonitorSample => _latency > 0 ? (short)0 : _state == EmulationCassetteState.Recording
        ? (_outputLevel ? C.RecordingAmplitude : (short)-C.RecordingAmplitude)
        : _state == EmulationCassetteState.Playing && _position < Length
            ? _recorded is null ? _signal!.Samples.Span[_position] : _recorded[_position]
            : (short)0;
    internal EmulationCassetteSignal RecordedSignal => new(
        _recorded is null ? _signal?.Samples ?? ReadOnlyMemory<short>.Empty : _recorded.ToArray(),
        _signal?.SampleRate ?? C.RecordingSampleRate);
    public EmulationCassetteState CassetteState => _state;
    public bool CassetteActive => _state != EmulationCassetteState.Paused &&
        (_seekTarget is not null || _state is EmulationCassetteState.Playing or EmulationCassetteState.Recording);
    public EmulationCassetteCommand? CassetteOperation => _operation;
    public IReadOnlySet<EmulationCassetteCommand> CassetteCommands => _signal is null ? C.EmptyCommands
        : _writable && !_settings.WriteProtected ? C.RecordingCommands : C.Commands;

    internal void Insert(EmulationCassetteSignal signal, bool writable)
    {
        if (signal.SampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(signal));
        _signal = signal;
        _recorded = null;
        _position = 0;
        _samplePhase = 0;
        _seekTarget = null;
        _writable = writable;
        HasChanges = false;
        _state = EmulationCassetteState.Stopped;
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
        _state = EmulationCassetteState.Empty;
        _operation = null;
        ResetHardware();
    }

    internal void Configure(EmulationCassetteSettings settings)
    {
        _settings = settings;
        if (settings.WriteProtected && _state == EmulationCassetteState.Recording) Stop();
        if (settings.Mode != EmulationCassettePlaybackMode.RealTime && _seekTarget is { } target)
        {
            _position = target;
            Stop();
        }
    }

    internal void RefreshIndex(EmulationCassetteSignal signal)
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

    public void ExecuteCassette(EmulationCassetteCommand command)
    {
        if (!CassetteCommands.Contains(command)) throw new NotSupportedException(command.ToString());
        switch (command)
        {
            case EmulationCassetteCommand.Play:
                _seekTarget = null;
                _state = _position < Length ? EmulationCassetteState.Playing : EmulationCassetteState.EndOfTape;
                _operation = _state == EmulationCassetteState.Playing ? command : null;
                _latency = (long)clock * _settings.ReadLatencyMilliseconds / C.MillisecondsPerSecond;
                break;
            case EmulationCassetteCommand.Record:
                _seekTarget = null;
                _state = EmulationCassetteState.Recording;
                _operation = command;
                _latency = (long)clock * _settings.WriteLatencyMilliseconds / C.MillisecondsPerSecond;
                break;
            case EmulationCassetteCommand.Stop:
                Stop();
                break;
            case EmulationCassetteCommand.Pause:
                if (_state == EmulationCassetteState.Paused)
                    _state = _seekTarget is not null ? EmulationCassetteState.Stopped
                        : _operation == EmulationCassetteCommand.Record ? EmulationCassetteState.Recording
                        : _operation == EmulationCassetteCommand.Play ? EmulationCassetteState.Playing : EmulationCassetteState.Stopped;
                else if (CassetteActive) _state = EmulationCassetteState.Paused;
                break;
            case EmulationCassetteCommand.Rewind:
                Seek((_signal!.BlockPositions ?? []).Where(value => value < _position).DefaultIfEmpty(0).Max(), command);
                break;
            case EmulationCassetteCommand.FastForward:
                Seek((_signal!.BlockPositions ?? []).Where(value => value > _position).DefaultIfEmpty(Length).Min(), command);
                break;
        }
        ActivityChanged?.Invoke();
    }

    internal void Tick(bool motorEnabled = true)
    {
        if (!CassetteActive || _signal is null) return;
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
        if (_state == EmulationCassetteState.Recording)
        {
            Record();
            return;
        }
        if (_position >= Length)
        {
            Stop();
            _state = EmulationCassetteState.EndOfTape;
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
        if (signalMode == CassetteSignalMode.FrequencyShiftKeying)
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

    private void Seek(int position, EmulationCassetteCommand operation)
    {
        _seekTarget = Math.Clamp(position, 0, Length);
        _operation = operation;
        _state = EmulationCassetteState.Stopped;
        _samplePhase = 0;
        _latency = 0;
        if (_settings.Mode != EmulationCassettePlaybackMode.RealTime || _seekTarget == _position)
        {
            _position = _seekTarget.Value;
            Stop();
        }
    }

    private void Stop()
    {
        _state = _signal is null ? EmulationCassetteState.Empty : EmulationCassetteState.Stopped;
        _operation = null;
        _seekTarget = null;
        _samplePhase = 0;
        _latency = 0;
        _edgeTicks = 0;
        _hasLevel = false;
        InputBit = true;
        ActivityChanged?.Invoke();
    }
    bool RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces.IElectronCassettePins.InputPresent =>
        _latency == 0 && _state == EmulationCassetteState.Playing;
    bool RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces.IElectronCassettePins.InputLevel => _inputLevel;
    void RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces.IElectronCassettePins.SetOutputLevel(bool value) => SetOutputLevel(value);
    void RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces.IElectronCassettePins.Tick(bool motor) => Tick(motor);
}

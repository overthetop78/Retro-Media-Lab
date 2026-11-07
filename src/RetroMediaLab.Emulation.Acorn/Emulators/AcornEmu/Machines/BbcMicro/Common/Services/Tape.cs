using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.TapeConstants;
using UlaConstants = RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Constants.BbcSerialUlaConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

/// <summary>Magnétophone externe BBC : échantillons, tête, touches et latences ; l'électronique est empruntée.</summary>
/// <remarks>Possède le signal en mémoire, sans conserver de fichier ouvert. Le lecteur avance seulement lorsque
/// Play et le moteur MOS sont actifs. L'ACIA reste un composant distinct, emprunté à la carte.
/// La cadence du fichier est suivie par un accumulateur entier ; elle ne dépend pas du nombre d'images vidéo.</remarks>
internal sealed partial class Tape(Serial serial) : IEmulatorTapeTransport
{
    private EmulationTapeSignal? _signal;
    private int _position, _samplePhase;
    private bool _inputLevel;
    private bool _inputPresented;
    private int _presentedPosition = -1;
    private int _sampleIncrement = C.RecordingSampleRate;
    private List<short>? _recorded;
    private bool _writable;
    private EmulationTapeSettings _settings = new();
    private int? _seekTarget;
    private EmulationTapeCommand? _operation;
    private long _latencyTicks;
    private bool _clockRequired;
    internal Action<bool>? ClockRequiredChanged { get; set; }
    public event Action? ActivityChanged;
    private (bool Active, EmulationTapeCommand? Operation, EmulationTapeState State) _publishedActivity;

    private void UpdateClockRequest()
    {
        // Seuls les changements de commandes, de support ou du registre ULA
        // modifient cette autorisation. Les ticks consomment son niveau retenu.
        var activity = (Active: ResolveActivity(), Operation: TapeOperation, State: TapeState);
        if (_publishedActivity != activity)
        {
            _publishedActivity = activity;
            ActivityChanged?.Invoke();
        }
        var required = _inputPresented || _signal is not null && activity.Active;
        if (_clockRequired == required) return;
        _clockRequired = required;
        ClockRequiredChanged?.Invoke(required);
    }
    internal EmulationTapeSettings Settings => _settings;
    internal bool Seeking => _seekTarget is not null;
    public EmulationTapeCommand? TapeOperation => _state == EmulationTapeState.Paused ? _operation : _seekTarget is not null ? _operation : _state == EmulationTapeState.Recording ? EmulationTapeCommand.Record : _state == EmulationTapeState.Playing ? EmulationTapeCommand.Play : null;
    internal void Configure(EmulationTapeSettings settings)
    {
        _settings = settings; serial.BaudRateOverride = settings.BaudRate;
        UpdateSampleIncrement();
        if (settings.WriteProtected && _state == EmulationTapeState.Recording) _state = EmulationTapeState.Stopped;
        if (settings.Mode != EmulationTapePlaybackMode.RealTime && _seekTarget is { } target) { _position = target; CompleteSeek(); }
        UpdateClockRequest();
    }
    /// <summary>Remplace l'index après sauvegarde tout en gardant la tête et l'opération du magnétophone.</summary>
    internal void RefreshIndex(EmulationTapeSignal signal) { _signal = signal; _recorded = null; _position = Math.Min(_position, signal.Samples.Length); _presentedPosition = -1; UpdateSampleIncrement(); UpdateClockRequest(); }
    private void UpdateSampleIncrement() => _sampleIncrement = (_signal?.SampleRate ?? C.RecordingSampleRate) * _settings.SpeedPercent / C.NormalSpeedPercent;
    /// <summary>Reste vrai jusqu'à confirmation de l'écriture atomique ; une pause ou un moteur arrêté ne perd aucune donnée.</summary>
    internal bool HasChanges { get; private set; }
    internal EmulationTapeSignal RecordedSignal => new(_recorded is null ? _signal?.Samples ?? ReadOnlyMemory<short>.Empty : _recorded.ToArray(), _signal?.SampleRate ?? C.RecordingSampleRate);
    internal void MarkSaved() => HasChanges = false;
    private volatile EmulationTapeState _state = EmulationTapeState.Empty;
    internal int Position => _position;
    internal bool Motor => serial.Ula.Motor;
    public EmulationTapeState TapeState => _state;
    public bool TapeActive => _publishedActivity.Active;
    private bool ResolveActivity() => _seekTarget is not null && _state != EmulationTapeState.Paused || Motor && !serial.Ula.Rs423Selected && _state is EmulationTapeState.Playing or EmulationTapeState.Recording;
    public IReadOnlySet<EmulationTapeCommand> TapeCommands => _signal is null ? new HashSet<EmulationTapeCommand>() : _writable && !_settings.WriteProtected ? C.RecordingCommands : C.Commands;

    /// <summary>Insère le signal au début ; Play est prêt, comme sur un lecteur laissé en lecture sous contrôle moteur.</summary>
    internal void Insert(EmulationTapeSignal signal, bool writable = false)
    {
        if (signal.SampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(signal));
        _signal = signal; _recorded = null; _writable = writable; HasChanges = false; _seekTarget = null;
        UpdateSampleIncrement();
        _position = _samplePhase = 0; _state = signal.Samples.Length == 0 ? EmulationTapeState.Stopped : EmulationTapeState.Playing; ResetDecoder();
        UpdateClockRequest();
    }
    internal void Eject() { _signal = null; _recorded = null; _writable = HasChanges = false; _position = _samplePhase = 0; _state = EmulationTapeState.Empty; ResetDecoder(); UpdateClockRequest(); }
    /// <summary>Reset électrique : ne rembobine pas une bande déjà insérée.</summary>
    internal void ResetHardware() { serial.PowerOn(); ResetDecoder(); UpdateClockRequest(); }
    internal void WriteUla(byte value)
    {
        var starting = !Motor;
        serial.Ula.Write(value);
        starting &= Motor;
        if (starting) _latencyTicks = (long)C.CpuClock * (_state == EmulationTapeState.Recording ? _settings.WriteLatencyMilliseconds : _settings.ReadLatencyMilliseconds) / 1000;
        if (!ResolveActivity()) ResetDecoder();
        UpdateClockRequest();
    }
    internal byte ReadUla() { WriteUla(UlaConstants.ReadControlValue); return UlaConstants.ReadBusValue; }
    private void ResetDecoder()
    {
        serial.Ula.SetTapeInput(null);
        _inputPresented = false;
        _presentedPosition = -1;
    }

    public void ExecuteTape(EmulationTapeCommand command)
    {
        if (_signal is null || !TapeCommands.Contains(command)) throw new NotSupportedException(command.ToString());
        switch (command)
        {
            case EmulationTapeCommand.Play: _seekTarget = null; _state = _position < Length ? EmulationTapeState.Playing : EmulationTapeState.EndOfTape; _latencyTicks = (long)C.CpuClock * _settings.ReadLatencyMilliseconds / 1000; break;
            case EmulationTapeCommand.Record: _seekTarget = null; _state = EmulationTapeState.Recording; _latencyTicks = (long)C.CpuClock * _settings.WriteLatencyMilliseconds / 1000; ResetDecoder(); break;
            case EmulationTapeCommand.Stop: _seekTarget = null; _state = EmulationTapeState.Stopped; ResetDecoder(); break;
            case EmulationTapeCommand.Pause:
                if (_state == EmulationTapeState.Paused) { _state = _operation == EmulationTapeCommand.Record ? EmulationTapeState.Recording : EmulationTapeState.Playing; }
                else { _operation = TapeOperation; _state = EmulationTapeState.Paused; }
                break;
            case EmulationTapeCommand.Rewind: Seek((_signal.BlockPositions ?? []).Where(position => position < _position - 1).DefaultIfEmpty(0).Max(), command); break;
            case EmulationTapeCommand.FastForward: Seek((_signal.BlockPositions ?? []).Where(position => position > _position + 1).DefaultIfEmpty(Length).Min(), command); break;
        }
        UpdateClockRequest();
    }

    /// <summary>Avance le support d'un cycle de carte et présente son niveau à l'électronique série.</summary>
    internal void Tick()
    {
        if (_signal is not null && TapeActive && _latencyTicks == 0 && _state == EmulationTapeState.Playing && _position < Length)
        {
            if (_presentedPosition != _position)
            {
                var sample = _recorded is null ? _signal.Samples.Span[_position] : _recorded[_position];
                var level = sample == 0 ? _inputLevel : sample > 0;
                if (!_inputPresented || level != _inputLevel) serial.Ula.SetTapeInput(level);
                _inputLevel = level;
                _inputPresented = true;
                _presentedPosition = _position;
            }
        }
        else if (_inputPresented) { ResetDecoder(); UpdateClockRequest(); }
        serial.Tick();
        if (_seekTarget is { } target)
        {
            if (_state == EmulationTapeState.Paused) return;
            _samplePhase += (_signal?.SampleRate ?? C.RecordingSampleRate) * _settings.WindMultiplier * _settings.SpeedPercent / 100;
            while (_samplePhase >= C.CpuClock && _position != target) { _samplePhase -= C.CpuClock; _position += Math.Sign(target - _position); }
            if (_position == target) CompleteSeek();
            return;
        }
        if (_signal is null || !TapeActive) return;
        if (_latencyTicks > 0) { _latencyTicks--; return; }
        if (_state == EmulationTapeState.Recording) { RecordTick(); return; }
        if (_position >= Length) { _state = EmulationTapeState.EndOfTape; ResetDecoder(); UpdateClockRequest(); return; }
        _samplePhase += _sampleIncrement;
        while (_samplePhase >= C.CpuClock) { _samplePhase -= C.CpuClock; _position++; }
    }

    private int Length => _recorded?.Count ?? _signal?.Samples.Length ?? 0;
    private void Seek(int target, EmulationTapeCommand operation)
    {
        _seekTarget = target; _operation = operation; _state = EmulationTapeState.Stopped; _samplePhase = 0; ResetDecoder();
        if (_settings.Mode != EmulationTapePlaybackMode.RealTime || target == _position) { _position = target; CompleteSeek(); }
        UpdateClockRequest();
    }
    private void CompleteSeek() { _seekTarget = null; _operation = null; _state = _position >= Length ? EmulationTapeState.EndOfTape : EmulationTapeState.Stopped; _samplePhase = 0; UpdateClockRequest(); }

    /// <summary>Réunit un fichier BBC standard validé ; le chemin direct ne devine ni les adresses ni les blocs manquants.</summary>
    internal bool TryReadFile(string requestedName, out byte[] data, out uint load, out uint execution, out int nextPosition)
    {
        data = []; load = execution = 0; nextPosition = _position;
        if (_settings.Mode != EmulationTapePlaybackMode.Direct || _state is EmulationTapeState.Recording or EmulationTapeState.Paused || _signal?.Blocks is not { Count: > 0 } blocks) return false;
        for (var first = 0; first < blocks.Count; first++)
        {
            var block = blocks[first];
            if (block.SamplePosition < _position || block.Metadata.GetValueOrDefault("blockNumber") != "0" || block.IntegrityValid != true) continue;
            var name = block.Metadata.GetValueOrDefault("fileName") ?? string.Empty;
            if (requestedName.Length != 0 && !requestedName.Equals(name, StringComparison.OrdinalIgnoreCase)) continue;
            var bytes = new List<byte>();
            for (var current = first; current < blocks.Count; current++)
            {
                block = blocks[current];
                if (block.IntegrityValid != true || block.Metadata.GetValueOrDefault("fileName") != name || block.Metadata.GetValueOrDefault("blockNumber") != (current - first).ToString(System.Globalization.CultureInfo.InvariantCulture)) return false;
                bytes.AddRange(block.Data.ToArray());
                if ((Convert.ToInt32(block.Metadata["flags"], 16) & 128) == 0) continue;
                load = Convert.ToUInt32(blocks[first].Metadata["loadAddress"], 16); execution = Convert.ToUInt32(blocks[first].Metadata["executionAddress"], 16);
                data = bytes.ToArray(); nextPosition = current + 1 < blocks.Count ? blocks[current + 1].SamplePosition : Length; return true;
            }
        }
        return false;
    }
    internal void CompleteDirectRead(int position) { _position = Math.Clamp(position, 0, Length); _state = EmulationTapeState.Stopped; ResetDecoder(); UpdateClockRequest(); }
    /// <summary>Module TXD à 1 200/2 400 Hz et enregistre à la cadence de la bande ; le débit série vient de l'ACIA.</summary>
    /// <remarks>L'écriture remplace uniquement les échantillons sous la tête et prolonge la bande à son extrémité.
    /// Les silences, fichiers précédents et fichiers suivants restent présents. L'appelant possède la sauvegarde.</remarks>
    private void RecordTick()
    {
        _samplePhase += _sampleIncrement;
        while (_samplePhase >= C.CpuClock)
        {
            _samplePhase -= C.CpuClock;
            _recorded ??= _signal!.Samples.ToArray().ToList();
            var value = serial.Ula.TapeOutput;
            if (_position < _recorded.Count) _recorded[_position] = value; else _recorded.Add(value);
            _position++; HasChanges = true;
        }
    }
}

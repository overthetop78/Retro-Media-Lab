using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Common.Constants;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;
using S = RetroMediaLab.Emulation.Constants.EmulationMechanicalSoundConstants;
using System.IO;
using CommonCoreHostFunctions = RetroMediaLab.Emulation.Acorn.Common.Functions.CoreHostFunctions;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Contracts;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Interfaces;
using NativeModelCatalog = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Dictionaries.ModelCatalog;
using BbcAudioConstants = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.AudioConstants;
using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Functions;
using TapeConstants = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.TapeConstants;
using AtomSettings = RetroMediaLab.Emulation.Acorn.Common.Machines.AcornAtom.Constants.AtomModelConstants;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Dictionaries;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Dictionaries;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

/// <summary>Adaptation du matériel AcornEmu au contrat de cœur déjà possédé par la session Common.</summary>
/// <remarks>Le cœur reste sur le thread de la machine. Il ne crée ni fenêtre, ni processus, ni thread.
/// Les ROM sont lues depuis les chemins choisis par l'utilisateur ; aucune image n'est intégrée à l'assembly.</remarks>
internal sealed partial class EmulatorCore : IEmulatorCore, IEmulatorTapeTransport
{
    private AcornEmuModelDefinition _definition;
    private IAcornMachine? _machine;

    /// <summary>Sélectionne le constructeur et les sorties du modèle sans charger de ROM ni créer de session.</summary>
    internal EmulatorCore(MachineModel model)
    {
        _definition = NativeModelCatalog.Get(model);
        if (model == MachineModel.AcornAtom)
            SupportedContentExtensions = new HashSet<string>(AtomFunctions.TapeExtensions.Concat(AtomFloppyDriveModels.All.SelectMany(drive => drive.AcceptedExtensions)), StringComparer.OrdinalIgnoreCase);
        else if (model == MachineModel.AcornElectron)
            SupportedContentExtensions = new HashSet<string>(TapeConstants.Extensions, StringComparer.OrdinalIgnoreCase);
        else if (ArchimedesFunctions.IsModel(model))
            SupportedContentExtensions = new HashSet<string>(ArchimedesDriveModels.Floppies.SelectMany(drive => drive.AcceptedExtensions)
                .Concat(StorageConstants.HardDiskExtensions), StringComparer.OrdinalIgnoreCase);
    }
    private bool _disposed;
    private long _sequence;
    private double _videoTimeSeconds;
    private long _audioSequence, _audioFrames;
    private readonly Queue<AudioChunk> _audio = new();
    private int _bufferedAudioFrames;
    private string _videoOutput = EmulatorConstants.UhfOutput;
    private string? _tapePath;
    private EmulationTapeSettings _tapeSettings = new();
    private EmulationTapePcmSettings _tapePcm = new();
    private IReadOnlyDictionary<string, string>? _mountedTapeParameters;
    private Dictionary<string, string> _runtimeOptions = new(StringComparer.Ordinal);
    private IEmulationDriveMechanics? _tapeMechanics;
    private string _tapeSoundOperation = S.None;
    public Emulator Emulator => Emulator.AcornEmu;
    public string CoreName => EmulatorConstants.Name;
    public string CoreVersion => EmulatorConstants.Version;
    public string CoreSha256 { get; } = CommonCoreHostFunctions.StateFiles.HashBytes(File.ReadAllBytes(typeof(EmulatorCore).Assembly.Location)).ToLowerInvariant();
    public double FramesPerSecond => _machine?.VideoFramesPerSecond ?? _definition.FramesPerSecond;
    public long? ProcessorClockHz => _machine?.ProcessorClockHz;
    public int SampleRate => BbcAudioConstants.SampleRate;
    public bool SupportsSaveStates => _machine?.StateType is not null;
    public bool SupportsPointerCapture => ArchimedesFunctions.IsModel(_definition.Model) || _machine?.SupportsPointerCapture == true;
    public bool ShutdownRequested { get; private set; }
    public EmulationVideoStandard? Region => _definition.Region;
    public HostProcessState HostProcessState => HostProcessState.InProcess;
    public int? HostProcessId => null;
    public int BufferedAudioFrames => Volatile.Read(ref _bufferedAudioFrames);
    public int MinimumAudioLatencyMilliseconds => 0;
    public void SetAudioBufferStatus(AudioOutputBufferStatus? status) { }
    public long AudioOverrunCount => 0;
    public long AudioUnderrunCount => 0;
    public int DiskCount => 0;
    public int CurrentDiskIndex => -1;
    public VideoFrame? LatestVideoFrame { get; private set; }
    public AudioChunk? LatestAudioChunk { get; private set; }
    public IReadOnlyList<CoreOption> Options => [.. VideoOptions, .. MechanicalSoundOptions()];
    private IReadOnlyList<CoreOption> VideoOptions => SystemModelCatalog.IsSystem(_definition.Model) || _definition.Model is MachineModel.AcornAtom or MachineModel.AcornElectron ? [] :
    [
        new(EmulatorConstants.VideoOutput, "Emulation.Acorn.Video.Output", null, null, EmulatorConstants.UhfOutput, _videoOutput,
            [new(EmulatorConstants.UhfOutput, "UHF PAL", "Emulation.Acorn.Video.Output.Uhf"), new(EmulatorConstants.MonochromeOutput, "Composite", "Emulation.Acorn.Video.Output.Monochrome"), new(EmulatorConstants.ColourCompositeOutput, "Composite", "Emulation.Acorn.Video.Output.ColourComposite"), new(EmulatorConstants.RgbOutput, "RGB", "Emulation.Acorn.Video.Output.Rgb")], ResourceKey: "Emulation.Acorn.Video.Output", Tab: EmulationMachineTab.Video)
    ];
    public IReadOnlyList<string> Diagnostics => [];
    public IReadOnlyDictionary<int, bool> LedStates
    {
        get
        {
            var states = new Dictionary<int, bool> { [1] = TapeActive };
            for (var drive = 0; drive < _floppyImages.Length; drive++) states[StorageConstants.FloppyLed(drive)] = _machine?.FloppyActive(drive) == true;
            for (var unit = 0; unit < _hardDisks.Length; unit++) states[StorageConstants.HardDiskLedStart + unit] = _machine?.HardDiskActive(unit) == true;
            return states;
        }
    }
    public IReadOnlySet<string> SupportedContentExtensions { get; } = new HashSet<string>(TapeConstants.Extensions.Concat(StorageConstants.DoubleDensityExtensions).Concat(StorageConstants.HardDiskExtensions), StringComparer.OrdinalIgnoreCase);
    public EmulationTapeState TapeState => _machine?.TapeTransport?.TapeState ?? EmulationTapeState.Empty;
    public bool TapeActive => _machine?.TapeTransport?.TapeActive ?? false;
    public EmulationTapeCommand? TapeOperation => _machine?.TapeTransport?.TapeOperation;
    public event Action? ActivityChanged;
    public IReadOnlySet<EmulationTapeCommand> TapeCommands => _machine?.TapeTransport is not { } transport ? new HashSet<EmulationTapeCommand>() : new HashSet<EmulationTapeCommand>(transport.TapeCommands.Concat(_tapeSettings.WriteProtected ? [] : new[] { EmulationTapeCommand.Record }));
    public void ExecuteTape(EmulationTapeCommand command)
    {
        var machine = _machine ?? throw new InvalidOperationException();
        var transport = machine.TapeTransport ?? throw new NotSupportedException();
        if (command != EmulationTapeCommand.Record) SaveMediaChanges(EmulationMediaSlot.Tape0);
        transport.ExecuteTape(command);
        if (command == EmulationTapeCommand.Stop) SetTapeSoundEvent(S.Stop);
        else if (command == EmulationTapeCommand.Pause) SetTapeSoundEvent(S.Pause);
    }

    private string? _nonvolatilePath;
    private void SaveNonvolatileMemory()
    {
        if (_nonvolatilePath is null || _machine?.ReadNonvolatileMemory() is not { } bytes) return;
        Directory.CreateDirectory(Path.GetDirectoryName(_nonvolatilePath)!);
        File.WriteAllBytes(_nonvolatilePath, bytes);
    }
    public void Initialize(MachineConfiguration configuration, string sessionDirectory, string? saveDirectory = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_machine is not null) throw new InvalidOperationException();
        if (configuration.Model != _definition.Model) throw new ArgumentOutOfRangeException(nameof(configuration));
        _definition = NativeModelCatalog.Get(configuration);
        _machine = _definition.Create(configuration);
        var nonvolatileFile = _machine.NonvolatileMemoryFile
            ?? (MasterFunctions.IsModel(configuration) ? MasterSettingsConstants.CmosFile : null);
        _nonvolatilePath = nonvolatileFile is null ? null : Path.Combine(saveDirectory ?? sessionDirectory, nonvolatileFile);
        if (_nonvolatilePath is not null && File.Exists(_nonvolatilePath)) _machine.WriteNonvolatileMemory(File.ReadAllBytes(_nonvolatilePath));
        _tapeSettings = EmulationTapeSettings.Read(configuration.Options);
        _runtimeOptions = new(configuration.Options, StringComparer.Ordinal);
        _tapePcm = EmulationTapePcmSettings.Read(configuration.Options);
        _machine.ConfigureTape(_tapeSettings);
        _tapeMechanics = CommonCoreHostFunctions.MediaProcessing.CreateDriveMechanics(S.Tape, hostAudio: true);
        _tapeMechanics.Configure(null, _tapeSettings.MechanicalVolumePercent, false);
        if (_machine.TapeTransport is { } transport) transport.ActivityChanged += OnTapeActivityChanged;
        ConfigureDisks(configuration);
        ConfigureHardDisks(configuration);
        ConfigureNetwork(configuration);
        foreach (var media in configuration.Media.Where(media => media.IsInserted)) InsertMedia(media);
        ResetRestoredMediaAudio();
        _videoOutput = configuration.Options.GetValueOrDefault(EmulatorConstants.VideoOutput, EmulatorConstants.UhfOutput);
        ShutdownRequested = false;
        _sequence = 0;
        _videoTimeSeconds = 0;
        _audioSequence = _audioFrames = 0;
        ClearAudio();
    }
    public void RunFrame()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var machine = _machine ?? throw new InvalidOperationException();
        var image = machine.RunFrame();
        _videoTimeSeconds += machine.VideoFrameDurationSeconds ?? 1d / FramesPerSecond;
        // Le matériel continue de tourner et de décoder chaque bit ; seule l'attente de présentation est supprimée.
        if (_tapeSettings.Mode != EmulationTapePlaybackMode.RealTime)
            for (var frame = 0; frame < 31 && TapeActive; frame++)
            {
                machine.DrainAudio();
                image = machine.RunFrame();
                _videoTimeSeconds += machine.VideoFrameDurationSeconds ?? 1d / FramesPerSecond;
            }
        // La coupure moteur du MOS termine un SAVE même lorsque le bouton REC reste armé sur le magnétophone.
        if (!TapeActive && machine.TapeHasChanges) SaveMediaChanges(EmulationMediaSlot.Tape0);
        FlushStoppedDisks();
        FlushStoppedHardDisks();
        var width = machine.VideoWidth ?? _definition.Width;
        var height = machine.VideoHeight ?? _definition.Height;
        LatestVideoFrame = new VideoFrame(image, width, height, width * _definition.BytesPerPixel, _definition.PixelFormat, _definition.AspectRatio, ++_sequence, TimeSpan.FromSeconds(_videoTimeSeconds))
        {
            ScanMode = machine.VideoScanMode
        };
        var samples = machine.DrainAudio();
        var layout = machine.AudioChannelLayout;
        var channels = System.Numerics.BitOperations.PopCount((uint)layout);
        if (samples.Length != 0)
        {
            LatestAudioChunk = new AudioChunk(samples, SampleRate, samples.Length / channels, ++_audioSequence, TimeSpan.FromSeconds((double)_audioFrames / SampleRate), layout);
            _audioFrames += samples.Length / channels;
            _audio.Enqueue(LatestAudioChunk);
            Interlocked.Add(ref _bufferedAudioFrames, LatestAudioChunk.FrameCount);
        }
    }
    public void SetInput(EmulationInputSnapshot snapshot) { ClearKeyboardPlayback(); _machine?.SetInput(snapshot); }
    public void SoftReset() { ClearKeyboardPlayback(); _network?.Reset(); _machine?.Reset(false); ClearAudio(); }
    public void HardReset() { ClearKeyboardPlayback(); _network?.Reset(); _machine?.Reset(true); ClearAudio(); }
    public void Stop() { try { SaveNonvolatileMemory(); SaveMediaChanges(EmulationMediaSlot.Tape0); SaveDisks(); SaveHardDisks(); } finally { try { DisposeNetwork(); } finally { ResetMechanicalAudio(); ShutdownRequested = true; ClearAudio(); } } }
    /// <summary>Transfère un bloc audio possédé au parcours audio Common déjà utilisé par les autres cœurs.</summary>
    public bool TryDequeueAudio(out AudioChunk? chunk)
    {
        if (!_audio.TryDequeue(out chunk)) return false;
        Interlocked.Add(ref _bufferedAudioFrames, -chunk.FrameCount);
        return true;
    }
    /// <summary>Détache les blocs non lus pour ne pas rejouer d'ancien son après arrêt ou reset.</summary>
    private void ClearAudio() { _audio.Clear(); Volatile.Write(ref _bufferedAudioFrames, 0); LatestAudioChunk = null; }
    public DiskStatus GetDiskStatus() => new(0, -1, true, []);
    public bool HasUnsavedMediaChanges(EmulationMediaSlot slot) => slot.Category switch
    {
        EmulationMediaCategory.FloppyDrive when slot.Index >= 0 && slot.Index < _floppyImages.Length => _floppyImages[slot.Index]?.HasChanges == true,
        EmulationMediaCategory.HardDisk when slot.Index >= 0 && slot.Index < _hardDisks.Length => _hardDisks[slot.Index]?.HasChanges == true,
        _ => slot == EmulationMediaSlot.Tape0 && _machine?.TapeHasChanges == true
    };
    public void SaveMediaChanges(EmulationMediaSlot slot)
    {
        if (slot.Category == EmulationMediaCategory.FloppyDrive) { if (slot.Index < 0 || slot.Index >= _floppyImages.Length) throw new ArgumentOutOfRangeException(nameof(slot)); SaveFloppy(slot.Index); return; }
        if (slot.Category == EmulationMediaCategory.HardDisk) { if (slot.Index < 0 || slot.Index >= _hardDisks.Length) throw new ArgumentOutOfRangeException(nameof(slot)); SaveHardDisk(slot.Index); return; }
        if (!HasUnsavedMediaChanges(slot)) return;
        BbcMicroTapeFunctions.Write(_tapePath ?? throw new InvalidOperationException(), _machine!.RecordedTape, _mountedTapeParameters);
        _machine.MarkTapeSaved();
        _machine.RefreshTapeIndex(BbcMicroTapeFunctions.Read(_tapePath!, _mountedTapeParameters));
    }
    public void SetControllerPortDevice(int port, PeripheralCategory peripheral) => throw new NotSupportedException();
    /// <summary>Le chargement passe par Common et Emulation ; le cœur reçoit un signal, jamais un document MediaEngine.</summary>
    public void InsertMedia(MediaConfiguration media)
    {
        if (media.Slot.Category == EmulationMediaCategory.FloppyDrive) { InsertFloppy(media); return; }
        if (media.Slot.Category == EmulationMediaCategory.HardDisk) { InsertHardDisk(media); return; }
        if (media.Slot != EmulationMediaSlot.Tape0 || media.Category != MediaCategory.Tape || !SupportedContentExtensions.Contains(Path.GetExtension(media.Path))) throw new ArgumentException(nameof(media));
        var parameters = media.Parameters ?? _tapePcm.ToParameters();
        var signal = _definition.Model == MachineModel.AcornAtom
            ? CommonCoreHostFunctions.MediaProcessing.ReadTapeAsync(media.Path, machineId: AtomSettings.TapeMachineId, parameters: parameters).GetAwaiter().GetResult()
            : BbcMicroTapeFunctions.Read(media.Path, parameters);
        SaveMediaChanges(EmulationMediaSlot.Tape0);
        (_machine ?? throw new InvalidOperationException()).InsertTape(signal, !media.IsReadOnly && BbcMicroTapeFunctions.RecordingExtensions.Contains(Path.GetExtension(media.Path), StringComparer.OrdinalIgnoreCase));
        _tapePath = media.Path;
        _mountedTapeParameters = parameters;
        SetTapeSoundEvent(S.Insert);
    }
    public void EjectMedia(EmulationMediaSlot slot)
    {
        if (slot.Category == EmulationMediaCategory.FloppyDrive) { EjectFloppy(slot.Index); return; }
        if (slot.Category == EmulationMediaCategory.HardDisk) { EjectHardDisk(slot.Index); return; }
        if (slot != EmulationMediaSlot.Tape0) throw new ArgumentOutOfRangeException(nameof(slot));
        SaveMediaChanges(slot);
        _machine?.EjectTape(); _tapePath = null; SetTapeSoundEvent(S.Eject);
    }
    public void SelectDisk(int index) => throw new NotSupportedException();
    public void SetOption(string key, string value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var machine = _machine ?? throw new InvalidOperationException();
        if (key == TapeConstants.ConnectedOption)
        {
            var connected = bool.Parse(value);
            if (!connected) EjectMedia(EmulationMediaSlot.Tape0);
            _runtimeOptions[key] = value;
            return;
        }
        if (key.StartsWith(EmulationTapeSettings.Prefix, StringComparison.Ordinal))
        {
            var options = new Dictionary<string, string>(_runtimeOptions, StringComparer.Ordinal) { [key] = value };
            var settings = EmulationTapeSettings.Read(options);
            var pcm = EmulationTapePcmSettings.Read(options);
            if (settings.BaudRate is not (0 or 300 or 1200)) throw new ArgumentOutOfRangeException(nameof(value));
            SaveMediaChanges(EmulationMediaSlot.Tape0);
            _runtimeOptions = options; _tapeSettings = settings; _tapePcm = pcm; machine.ConfigureTape(settings);
            ConfigureTapeSound();
            return;
        }
        if (ApplyMechanicalSoundOption(key, value)) return;
        machine.SetOption(key, value);
        _videoOutput = value;
    }

    /// <summary>Détache les données possédées ; aucune ressource empruntée à l'hôte n'est fermée.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        try { SaveNonvolatileMemory(); SaveMediaChanges(EmulationMediaSlot.Tape0); SaveDisks(); SaveHardDisks(); ShutdownRequested = true; }
        finally
        {
            ClearKeyboardPlayback();
            _keyboardPlayback = null;
            if (_machine?.TapeTransport is { } transport) transport.ActivityChanged -= OnTapeActivityChanged;
            ResetMechanicalAudio();
            try { DisposeDisks(); }
            finally
            {
                try { (_machine as IDisposable)?.Dispose(); }
                finally { try { DisposeHardDisks(); } finally { try { DisposeNetwork(); } finally { _machine = null; LatestVideoFrame = null; ClearAudio(); _disposed = true; } } }
            }
        }
    }
}

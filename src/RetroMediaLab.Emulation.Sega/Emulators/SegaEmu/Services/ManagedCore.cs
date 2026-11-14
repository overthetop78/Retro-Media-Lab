using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Common;
using RetroMediaLab.Chips.Common.Memory;
using System.IO;
using System.Security.Cryptography;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Services;

internal sealed partial class ManagedCore : IEmulatorCore
{
    private const int BitmapWidth = 1024, BitmapHeight = 626, BitmapPitch = BitmapWidth * 4;
    private readonly byte[] _bitmap = new byte[BitmapPitch * BitmapHeight];
    private readonly short[] _samples = new short[4800 * 2];
    private readonly Queue<AudioChunk> _audio = new();
    private readonly object _inputGate = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly List<string> _disks = [];
    private readonly Dictionary<int, bool> _leds = new();
    private MachineConfiguration? _configuration;
    private SystemConfiguration _settings = new();
    private SegaBoard? _system;
    private EmulationInputSnapshot _pendingInput = EmulationInputSnapshot.Empty, _polledInput = EmulationInputSnapshot.Empty;
    private string _contentPath = "", _contentDirectory = "", _systemDirectory = "", _saveDirectory = "";
    private int _diskIndex = -1, _bufferedAudioFrames;
    private bool _ejected, _disposed;
    private long _sequence, _overruns, _underruns;
    private TimeSpan _timestamp;
    private AudioOutputBufferStatus? _audioStatus;

    public string Emulator => CoreConstants.Id;
    public string CoreName => CoreConstants.DisplayName;
    public string CoreVersion => CoreConstants.Version;
    public string CoreSha256 { get; private set; } = "";
    public IReadOnlySet<string> SupportedContentExtensions { get; } = CoreConstants.Definition.Extensions.ToHashSet(StringComparer.OrdinalIgnoreCase);
    public bool SupportsPointerCapture => true;
    public bool SupportsSaveStates => _system is not null;
    public bool ShutdownRequested => _lifetime.IsCancellationRequested;
    public VideoFrame? LatestVideoFrame { get; private set; }
    public AudioChunk? LatestAudioChunk { get; private set; }
    public IReadOnlyList<CoreOption> Options => CoreConstants.Definition.Options;
    public IReadOnlyList<string> Diagnostics => [];
    public IReadOnlyDictionary<int, bool> LedStates => _leds;
    public EmulationVideoStandard? Region => _system is null ? null : _system.Video.vdp_pal != 0 ? EmulationVideoStandard.Pal : EmulationVideoStandard.Ntsc;
    public long? ProcessorClockHz => _system is null ? null : _system.MasterClock / (SystemHardware.IsSixteenBit(_system.Hardware) ? 7 : 15);
    public int BufferedAudioFrames => _bufferedAudioFrames;
    public long AudioOverrunCount => _overruns;
    public long AudioUnderrunCount => _underruns;
    public HostProcessState HostProcessState => HostProcessState.InProcess;
    public int? HostProcessId => null;
    public double FramesPerSecond => _system?.NominalFrameRate ?? 60;
    public int SampleRate => 44100;
    public int DiskCount => _disks.Count;
    public int CurrentDiskIndex => _diskIndex;
    public int MinimumAudioLatencyMilliseconds => 0;
    public void SetAudioBufferStatus(AudioOutputBufferStatus? status) => _audioStatus = status;

    public void Initialize(MachineConfiguration configuration, string sessionDirectory, string? saveDirectory = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        bool initialized = false;
        try
        {
            _configuration = configuration;
            _contentDirectory = Path.Combine(sessionDirectory, "content");
            _systemDirectory = Path.Combine(sessionDirectory, "system");
            _saveDirectory = saveDirectory ?? Path.Combine(sessionDirectory, "saves");
            Directory.CreateDirectory(_contentDirectory); Directory.CreateDirectory(_systemDirectory); Directory.CreateDirectory(_saveDirectory);
            Emulators.Common.Interop.Functions.FirmwareFunctions.Prepare(configuration, _systemDirectory);
            Stream? assembly = null;
            try { assembly = File.OpenRead(typeof(ManagedCore).Assembly.Location); CoreSha256 = Convert.ToHexString(SHA256.HashData(assembly)); }
            finally { assembly?.Dispose(); }
            MediaConfiguration? cartridge = null;
            foreach (MediaConfiguration media in (configuration.Media ?? []).Where(media => media.IsInserted).OrderBy(media => media.MountOrder))
            {
                string path = Path.GetFullPath(media.Path);
                if (Path.GetExtension(path).Equals(".m3u", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (string line in File.ReadLines(path))
                    {
                        string entry = line.Trim();
                        if (entry.Length != 0 && !entry.StartsWith('#')) _disks.Add(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, entry)));
                    }
                }
                else if (IsDisc(path)) _disks.Add(path);
                else cartridge ??= media;
            }
            _diskIndex = _disks.Count == 0 ? -1 : 0;
            _contentPath = cartridge?.Path ?? _disks.FirstOrDefault() ?? string.Empty;
            if (_contentPath.Length != 0 && CoreConstants.Definition.PrepareContent is { } prepare)
                _contentPath = prepare(configuration, _contentPath, _contentDirectory);
            StartSystem();
            initialized = true;
        }
        finally { if (!initialized) Dispose(); }
    }

    private void StartSystem(bool savePrevious = true)
    {
        SegaBoard? previous = _system;
        try { if (previous is not null && savePrevious) SavePersistent(); }
        finally { try { previous?.Dispose(); } finally { _system = null; } }
        _settings = CreateSettings();
        SegaBoard? next = null;
        bool transferred = false;
        try
        {
            next = Factories.BoardFactory.Create(_configuration!.Model,
                new(_settings, _bitmap, BitmapWidth, BitmapHeight, BitmapPitch,
                    SampleRate, OpenMedia, LoadFirmware, _lifetime.Token));
            _system = next;
            if (!next.Load(_contentPath)) throw new InvalidDataException(ErrorMessages.ContentRefused());
            next.RefreshConfiguration(Number("overclock", 100));
            next.PollInput = PollInputs;
            LoadPersistent();
            transferred = true;
        }
        finally { if (!transferred) { try { next?.Dispose(); } finally { _system = null; } } }
        ClearAudio();
    }

    private static bool IsDisc(string path) => Path.GetExtension(path).ToLowerInvariant() is ".cue" or ".iso" or ".chd";
    private Stream? OpenMedia(string path)
    {
        _lifetime.Token.ThrowIfCancellationRequested();
        return File.Exists(path) ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.RandomAccess) : null;
    }
    public void RunFrame()
    {
        SegaBoard system = _system ?? throw new InvalidOperationException(ErrorMessages.CoreNotInitialized());
        bool completed = false;
        try
        {
            int count = system.RunFrame(_samples, ShouldSkipFrame());
            _timestamp += TimeSpan.FromSeconds(1 / system.NominalFrameRate);
            _sequence++;
            PublishVideo(system);
            short[] samples = _samples.AsSpan(0, count * 2).ToArray();
            AudioChunk chunk = new(samples, SampleRate, count, _sequence, _timestamp);
            LatestAudioChunk = chunk;
            while (_audio.Count >= 8) { _bufferedAudioFrames -= _audio.Dequeue().FrameCount; _overruns++; }
            _audio.Enqueue(chunk); _bufferedAudioFrames += count;
            completed = true;
        }
        finally { if (!completed) Dispose(); }
    }
    public bool TryDequeueAudio(out AudioChunk? chunk)
    {
        if (_audio.TryDequeue(out chunk)) { _bufferedAudioFrames -= chunk.FrameCount; return true; }
        return false;
    }
    private void ClearAudio() { _audio.Clear(); _bufferedAudioFrames = 0; LatestAudioChunk = null; }
    public void SetInput(EmulationInputSnapshot snapshot) { lock (_inputGate) _pendingInput = snapshot; }
    public void HardReset() { _system!.Reset(); ClearAudio(); }
    public void SoftReset() { _system!.Reset(false); ClearAudio(); }
    public byte[] SaveState()
    {
        byte[] bytes = new byte[_system!.StateSize];
        int length = _system.SaveState(bytes);
        if (length != bytes.Length) Array.Resize(ref bytes, length);
        return bytes;
    }
    public void LoadState(ReadOnlySpan<byte> state)
    {
        if (_system!.LoadState(state.ToArray()) == 0) throw MachineExceptions.SavedStateIncompatible();
        ClearAudio();
    }
    public DiskStatus GetDiskStatus() => new(_disks.Count, _diskIndex, _ejected,
        _disks.Select((path, index) => new DiskImageStatus(index, path, Path.GetFileNameWithoutExtension(path))).ToArray());
    public void SelectDisk(int index)
    {
        if ((uint)index >= _disks.Count) throw new ArgumentOutOfRangeException(nameof(index));
        _diskIndex = index;
        if (!_ejected) _system!.MountDisc(_disks[index]);
    }
    public void InsertMedia(MediaConfiguration media)
    {
        if (IsDisc(media.Path))
        {
            int index = _disks.IndexOf(media.Path);
            if (index < 0) { index = _disks.Count; _disks.Add(media.Path); }
            _diskIndex = index; _ejected = false; _system!.MountDisc(media.Path);
        }
        else { SavePersistent(); _contentPath = media.Path; StartSystem(savePrevious: false); }
    }
    public void EjectMedia(EmulationMediaSlot slot)
    {
        if (slot.Category == EmulationMediaCategory.CompactDiscDrive) { _system!.UnmountDisc(); _ejected = true; }
        else { SavePersistent(); _contentPath = string.Empty; StartSystem(savePrevious: false); }
    }
    public void SaveMediaChanges(EmulationMediaSlot slot) => SavePersistent();
    public bool HasUnsavedMediaChanges(EmulationMediaSlot slot) => false;
    public void Stop()
    {
        try { SavePersistent(); }
        finally { try { _system?.Dispose(); } finally { _system = null; ClearAudio(); LatestVideoFrame = null; } }
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _lifetime.Cancel(); }
        finally { try { Stop(); } finally { _lifetime.Dispose(); _audioStatus = null; } }
    }
}

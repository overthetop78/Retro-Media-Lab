using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Boards;

using System.IO;
using System.Buffers.Binary;
using System.Security.Cryptography;
using RetroMediaLab.Chips.Audio.Yamaha.SCSP;
using RetroMediaLab.Chips.Controllers.Sega;
using RetroMediaLab.Chips.Controllers.Sega.CD;
using RetroMediaLab.Chips.CPU.Hitachi.SH2;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.Saturn;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.Sc3000;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.Sf7000;
using RetroMediaLab.MediaEngine;
using RetroMediaLab.MediaEngine.Contracts;
using SegaEmuFirmware = RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants.FirmwareConstants;

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
    private MediaTapeSignal? _sc3000Tape;
    private string? _masterSystemCartridge, _masterSystemSegaCard;
    private SaturnMachine? _saturn;
    private readonly SaturnPeripheral _saturnPeripheral = new();
    private readonly Memory<byte>?[] _saturnDevices = new Memory<byte>?[2];
#if USE_SCSP2
    private readonly object _saturnAudioGate = new();
#endif
    private int _saturnAudioFrames;
    private EmulationInputSnapshot _pendingInput = EmulationInputSnapshot.Empty, _polledInput = EmulationInputSnapshot.Empty;
    private string _contentPath = "", _contentDirectory = "", _systemDirectory = "", _saveDirectory = "";
    private int _diskIndex = -1, _bufferedAudioFrames;
    private bool _ejected, _disposed;
    private long _sequence, _overruns;
    private TimeSpan _timestamp;
    private AudioOutputBufferStatus? _audioStatus;

    public string Emulator => CoreConstants.Id;
    public string CoreName => CoreConstants.DisplayName;
    public string CoreVersion => CoreConstants.Version;
    public string CoreSha256 { get; private set; } = "";
    public IReadOnlySet<string> SupportedContentExtensions { get; } = CoreConstants.Definition.Extensions.ToHashSet(StringComparer.OrdinalIgnoreCase);
    public bool SupportsPointerCapture => true;
    public bool SupportsSaveStates => _system is not null and not Sf7000Machine
        && _system is not Sc3000Machine { HasBasicCartridge: true } && _sc3000Tape is null;
    internal EmulationTapeState TapeState => (_system as Sc3000Machine)?.Cassette.State ?? EmulationTapeState.Empty;
    internal IReadOnlySet<EmulationTapeCommand> TapeCommands =>
        (_system as Sc3000Machine)?.Cassette.Commands ?? Sc3000Cassette.EmptyCommands;

    internal void ExecuteTape(EmulationTapeCommand command)
    {
        if (_system is not Sc3000Machine machine)
            throw new InvalidOperationException();
        machine.Cassette.Execute(command, machine.MasterClock);
    }
    public bool ShutdownRequested => _lifetime.IsCancellationRequested;
    public VideoFrame? LatestVideoFrame { get; private set; }
    public AudioChunk? LatestAudioChunk { get; private set; }
    public IReadOnlyList<CoreOption> Options => CoreConstants.Definition.Options;
    public IReadOnlyList<string> Diagnostics => [];
    public IReadOnlyDictionary<int, bool> LedStates => _leds;
    public EmulationVideoStandard? Region => _saturn is { } saturn
        ? saturn.IsPal ? EmulationVideoStandard.Pal : EmulationVideoStandard.Ntsc
        : _system is null ? null : _system.Video.vdp_pal != 0 ? EmulationVideoStandard.Pal : EmulationVideoStandard.Ntsc;
    public long? ProcessorClockHz => _saturn?.ProcessorClockHz
        ?? (_system is { } system
            ? system.Has32X ? system.MasterClock * 3L / 7 : system.MasterClock / (SystemHardware.IsSixteenBit(system.Hardware) ? 7 : 15)
            : null);
    public int BufferedAudioFrames => _bufferedAudioFrames;
    public long AudioOverrunCount => _overruns;
    public long AudioUnderrunCount => 0;
    public HostProcessState HostProcessState => HostProcessState.InProcess;
    public int? HostProcessId => null;
    public double FramesPerSecond => _saturn is { } saturn ? saturn.IsPal ? 50 : 60 / 1.001 : _system?.NominalFrameRate ?? 60;
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
            _megaCdBiosRegion = PrepareMegaCdBios(configuration);
            Stream? assembly = null;
            try { assembly = File.OpenRead(typeof(ManagedCore).Assembly.Location); CoreSha256 = Convert.ToHexString(SHA256.HashData(assembly)); }
            finally { assembly?.Dispose(); }
            MediaConfiguration? cartridge = null;
            foreach (MediaConfiguration media in (configuration.Media ?? []).Where(media => media.IsInserted).OrderBy(media => media.MountOrder))
            {
                string path = Path.GetFullPath(media.Path);
                if (UsesMasterSystemMedia())
                {
                    ValidateMasterSystemMedia(media);
                    if (IsMasterSystemSegaCard(media)) _masterSystemSegaCard ??= GetMasterSystemSegaCard(path);
                    else _masterSystemCartridge ??= path;
                }
                else if (configuration.Model == ModelConstants.Sc3000 && media.Category == MediaCategory.Tape)
                {
                    _sc3000Tape ??= ReadSc3000Tape(path);
                }
                else if (configuration.Model == ModelConstants.Sf7000
                    && (media.Category == MediaCategory.Floppy || IsSf7000Disk(path)))
                {
                    _disks.Add(path);
                }
                else if (Path.GetExtension(path).Equals(".m3u", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (string line in File.ReadLines(path))
                    {
                        string entry = line.Trim();
                        if (entry.Length != 0 && !entry.StartsWith('#')) _disks.Add(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, entry)));
                    }
                }
                else if (IsDisc(path) || (configuration.Model == ModelConstants.Saturn && IsSaturnDisc(path))) _disks.Add(path);
                else cartridge ??= media;
            }
            if (_disks.Count != 0 && ModelConstants.IsMegaDriveFamily(configuration.Model)
                && !ModelConstants.HasMegaCd(configuration.Model)
                && !(configuration.Model == ModelConstants.MegaDrive && configuration.MegaCdEnabled))
                throw new InvalidDataException(ErrorMessages.MediaRefused());
            _diskIndex = _disks.Count == 0 ? -1 : 0;
            _contentPath = _masterSystemCartridge ?? _masterSystemSegaCard ?? cartridge?.Path ?? _disks.FirstOrDefault() ?? string.Empty;
            if (_contentPath.Length != 0 && CoreConstants.Definition.PrepareContent is { } prepare)
                _contentPath = prepare(configuration, _contentPath, _contentDirectory);
            StartSystem();
            initialized = true;
        }
        finally { if (!initialized) Dispose(); }
    }

    private void StartSystem(bool savePrevious = true)
    {
        if (_configuration!.Model == ModelConstants.Saturn)
        {
            try { if (savePrevious) SavePersistent(); }
            finally { DisposeSaturn(); }
            StartSaturn();
            ClearAudio();
            return;
        }

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
            if (next is Sf7000Machine sf7000)
            {
                sf7000.LoadIpl(LoadFirmware);
                sf7000.SetDisk(!_ejected && _diskIndex >= 0 ? _disks[_diskIndex] : null);
                if (!next.Load(string.Empty)) throw new InvalidDataException(ErrorMessages.ContentRefused());
                sf7000.ConnectPrinter(_configuration.Options?.GetValueOrDefault(Sf7000PrintFile.DirectoryOption));
            }
            else if (!next.Load(_contentPath)) throw new InvalidDataException(ErrorMessages.ContentRefused());
            if (next is Sc3000Machine computer && _sc3000Tape is { } tape)
                computer.Cassette.Insert(tape);
            next.RefreshConfiguration(Number("overclock", 100));
            next.PollInput = PollInputs;
            LoadPersistent();
            transferred = true;
        }
        finally { if (!transferred) { try { next?.Dispose(); } finally { _system = null; } } }
        ClearAudio();
    }

    private static bool IsDisc(string path) => Path.GetExtension(path).ToLowerInvariant() is ".cue" or ".iso" or ".chd";
    private static bool IsSaturnDisc(string path) => Path.GetExtension(path).ToLowerInvariant() is ".cue" or ".iso" or ".bin" or ".mds" or ".ccd";
    private static bool IsSf7000Disk(string path) => Path.GetExtension(path).ToLowerInvariant() is ".sf" or ".sf7";

    private string SaturnBackupPath => Path.Combine(_saveDirectory, "saturn.segaemu.brm");

    private void StartSaturn()
    {
        string? disc = !_ejected && _diskIndex >= 0 ? _disks[_diskIndex] : null;
        if (disc is not null && !IsSaturnDisc(disc))
            throw new InvalidDataException(ErrorMessages.ContentRefused());

        var cartridge = new SaturnCartridge<SaturnSh2Core>();
        const SaturnCartridge<SaturnSh2Core>.CartridgeType cartridgeType = SaturnCartridge<SaturnSh2Core>.CartridgeType.None;
        cartridge.Initialize(cartridgeType, null);
        _saturnPeripheral.ResetPorts();
        for (int port = 0; port < _saturnDevices.Length; port++)
        {
            PeripheralCategory controller = _configuration!.Controllers?.ElementAtOrDefault(port) ?? PeripheralCategory.Automatic;
            SetSaturnController(port, controller);
        }

        SaturnSmpcState.Region region = GetOption("region_detect") switch
        {
            "ntsc-u" => SaturnSmpcState.Region.NorthAmerica,
            "pal" => SaturnSmpcState.Region.Europe,
            "ntsc-j" => SaturnSmpcState.Region.Japan,
            _ => SaturnSmpcState.Region.AutoDetect
        };
        bool? pal = GetOption("vdp_mode") switch
        {
            "50hz" => true,
            "60hz" => false,
            _ => null
        };
        string mpegPath = Path.Combine(_systemDirectory, SegaEmuFirmware.SaturnMpegRomFileName);
        SaturnMachine.CdInitialization cd = new(
            cartridgeType, disc is null ? SaturnCdInterface.Dummy : SaturnCdInterface.Iso, disc,
            File.Exists(mpegPath) ? mpegPath : null, null, null,
            static _ => throw new InvalidDataException(ErrorMessages.ContentRefused()),
            static (_, _) => -1, static () => { }, static (_, _) => -1, static () => { },
            static _ => { }, static _ => { });
        SaturnScsp.RegisterConnections audio = new(
            static () => { }, Random.Shared.Next, static _ => { }, static _ => { },
            GetSaturnAudioSpace, ReceiveSaturnAudio, static _ => { });
        SaturnMachine.Connections connections = new(
            NotifySh2Write: static (_, _) => { },
            Cd: cd,
            MpegCard: new SaturnMpegCard(),
            Scsp: audio,
            Peripheral: _saturnPeripheral,
            MarkFramePolled: static () => { },
            DoMovie: static () => { },
            FrameCounter: () => unchecked((int)_sequence),
            MovieActive: static () => false,
            ApplyCheats: static () => { },
            FlushBackups: SavePersistent,
            FlushTsunami: static () => { });

        bool useCdBlockLle = GetOption("cdblock_lle") == "enabled";
        int numThreads = Environment.ProcessorCount;
        Stream? bios = null;
        Stream? backup = null;
        Stream? sh1Rom = null;
        try
        {
            bios = OpenMedia(Path.Combine(_systemDirectory, SegaEmuFirmware.SaturnBiosFileName));
            if (bios is null && useCdBlockLle)
                throw new FileNotFoundException("Saturn BIOS", SegaEmuFirmware.SaturnBiosFileName);
            if (useCdBlockLle)
            {
                string sh1Path = Path.Combine(_systemDirectory, SegaEmuFirmware.SaturnSh1RomFileName);
                sh1Rom = OpenMedia(sh1Path) ?? throw new FileNotFoundException("Saturn SH-1 ROM", sh1Path);
            }
            backup = OpenMedia(SaturnBackupPath);
            _saturn = new SaturnMachine(connections, new(new SaturnMemory(), cartridge),
                region, clockSync: false, baseTime: 0, pal: pal,
                bios: bios, useSh2Cache: GetOption("sh2_cache") == "enabled",
                useSh2DmaTiming: false, useCdBlockLle: useCdBlockLle,
                useScuDmaTiming: false, useNewScsp: false,
                useThreads: numThreads > 1, numThreads: numThreads,
#if USE_SCSP2
                backupRam: backup, sh1Rom: sh1Rom, useScsp2: true);
            _saturn.Scsp.SetFrameAccurate(true);
#else
                backupRam: backup, sh1Rom: sh1Rom);
#endif
        }
        finally
        {
            try { backup?.Dispose(); }
            finally
            {
                try { sh1Rom?.Dispose(); }
                finally { bios?.Dispose(); }
            }
        }
    }

    private void DisposeSaturn()
    {
        try { _saturn?.Dispose(); }
        finally { _saturn = null; Array.Clear(_saturnDevices); }
    }

    private void SetSaturnController(int port, PeripheralCategory controller)
    {
        if ((uint)port >= (uint)_saturnDevices.Length)
            return;

        SaturnSmpcState.PortData data = port == 0 ? _saturnPeripheral.Port1 : _saturnPeripheral.Port2;
        data.Data[0] = 0xF0;
        data.Size = 1;
        _saturnDevices[port] = controller switch
        {
            PeripheralCategory.Automatic or PeripheralCategory.Joypad or PeripheralCategory.SegaSaturnController or
                PeripheralCategory.SegaSaturnVirtuaStick or PeripheralCategory.SegaSaturnTwinStick => SaturnPeripheral.AddPad(data),
            PeripheralCategory.SegaSaturnThreeDControlPad => SaturnPeripheral.AddPeripheral(data, SaturnPeripheralId.ThreeDimensionalPad),
            PeripheralCategory.SegaSaturnShuttleMouse => SaturnPeripheral.AddPeripheral(data, SaturnPeripheralId.Mouse),
            PeripheralCategory.SegaSaturnVirtuaGun => SaturnPeripheral.AddPeripheral(data, SaturnPeripheralId.Gun),
            PeripheralCategory.SegaSaturnArcadeRacer => SaturnPeripheral.AddPeripheral(data, SaturnPeripheralId.Wheel),
            PeripheralCategory.SegaSaturnMissionStick => SaturnPeripheral.AddPeripheral(data, SaturnPeripheralId.MissionStick),
            _ => null
        };
    }

    private int GetSaturnAudioSpace()
    {
#if USE_SCSP2
        lock (_saturnAudioGate)
#endif
            return _samples.Length / 2 - _saturnAudioFrames;
    }

    private void ReceiveSaturnAudio(ReadOnlySpan<int> left, ReadOnlySpan<int> right, int frames)
    {
#if USE_SCSP2
        lock (_saturnAudioGate)
#endif
        {
            for (int frame = 0; frame < frames; frame++)
            {
                int destination = (_saturnAudioFrames + frame) * 2;
                _samples[destination] = (short)Math.Clamp(left[frame], short.MinValue, short.MaxValue);
                _samples[destination + 1] = (short)Math.Clamp(right[frame], short.MinValue, short.MaxValue);
            }
            _saturnAudioFrames += frames;
        }
    }

    private void PollSaturnInput()
    {
        lock (_inputGate)
        {
            _polledInput = _pendingInput;
            _pendingInput = _pendingInput with { Pointer = _pendingInput.Pointer with { DeltaX = 0, DeltaY = 0, Wheel = 0, HorizontalWheel = 0 } };
        }
        for (uint port = 0; port < _saturnDevices.Length; port++)
        {
            if (_saturnDevices[port] is not { } memory)
                continue;
            Span<byte> pad = memory.Span;
            int buttons = unchecked((ushort)ReadHostInput(port, 1, 0, 256));
            SaturnPeripheralId id = SaturnPeripheral.GetId(pad);
            if (id == SaturnPeripheralId.Mouse)
            {
                EmulationPointerState pointer = _polledInput.Pointer;
                if (pointer.Left) SaturnPeripheral.MouseLeftPressed(pad);
                else SaturnPeripheral.MouseLeftReleased(pad);
                if (pointer.Middle) SaturnPeripheral.MouseMiddlePressed(pad);
                else SaturnPeripheral.MouseMiddleReleased(pad);
                if (pointer.Right) SaturnPeripheral.MouseRightPressed(pad);
                else SaturnPeripheral.MouseRightReleased(pad);
                if ((buttons & (1 << 3)) != 0) SaturnPeripheral.MouseStartPressed(pad);
                else SaturnPeripheral.MouseStartReleased(pad);
                int global = PointerConstants.Percent(GetOption("pointer_speed"), 200);
                int sensitivity = PointerConstants.Percent(GetOption($"pointer_port{port + 1}_speed"), global);
                short x = ScaleRelativePointer(pointer.DeltaX, sensitivity, ref _relativeRemainderX[port]);
                short y = ScaleRelativePointer(pointer.DeltaY, sensitivity, ref _relativeRemainderY[port]);
                SaturnPeripheral.MouseMove(pad, x, invert_mouse != 0 ? y : -y);
                continue;
            }
            if (id == SaturnPeripheralId.Gun)
            {
                EmulationPointerState pointer = _polledInput.Pointer;
                if (pointer.Left || (buttons & 1) != 0) SaturnPeripheral.GunTriggerPressed(pad);
                else SaturnPeripheral.GunTriggerReleased(pad);
                if ((buttons & (1 << 3)) != 0) SaturnPeripheral.GunStartPressed(pad);
                else SaturnPeripheral.GunStartReleased(pad);
                int global = PointerConstants.Percent(GetOption("pointer_speed"), 200);
                int sensitivity = PointerConstants.Percent(GetOption($"pointer_port{port + 1}_speed"), global);
                short x = ScaleRelativePointer(pointer.DeltaX, sensitivity, ref _relativeRemainderX[port]);
                short y = ScaleRelativePointer(pointer.DeltaY, sensitivity, ref _relativeRemainderY[port]);
                if (x != 0 || y != 0)
                    SaturnPeripheral.GunMove(pad, x, invert_mouse != 0 ? y : -y);
                continue;
            }
            if ((buttons & (1 << 3)) != 0) SaturnPeripheral.PadStartPressed(pad);
            else SaturnPeripheral.PadStartReleased(pad);
            if (id != SaturnPeripheralId.MissionStick)
            {
                if ((buttons & (1 << 4)) != 0) SaturnPeripheral.PadUpPressed(pad);
                else SaturnPeripheral.PadUpReleased(pad);
                if ((buttons & (1 << 5)) != 0) SaturnPeripheral.PadDownPressed(pad);
                else SaturnPeripheral.PadDownReleased(pad);
            }
            if (id != SaturnPeripheralId.Wheel && id != SaturnPeripheralId.MissionStick)
            {
                if ((buttons & (1 << 6)) != 0) SaturnPeripheral.PadLeftPressed(pad);
                else SaturnPeripheral.PadLeftReleased(pad);
                if ((buttons & (1 << 7)) != 0) SaturnPeripheral.PadRightPressed(pad);
                else SaturnPeripheral.PadRightReleased(pad);
            }
            if ((buttons & (1 << 8)) != 0) SaturnPeripheral.PadBPressed(pad);
            else SaturnPeripheral.PadBReleased(pad);
            if ((buttons & (1 << 9)) != 0) SaturnPeripheral.PadYPressed(pad);
            else SaturnPeripheral.PadYReleased(pad);
            if ((buttons & (1 << 10)) != 0) SaturnPeripheral.PadZPressed(pad);
            else SaturnPeripheral.PadZReleased(pad);
            if ((buttons & (1 << 11)) != 0) SaturnPeripheral.PadCPressed(pad);
            else SaturnPeripheral.PadCReleased(pad);
            if ((buttons & (1 << 12)) != 0) SaturnPeripheral.PadLTriggerPressed(pad);
            else SaturnPeripheral.PadLTriggerReleased(pad);
            if ((buttons & (1 << 13)) != 0) SaturnPeripheral.PadRTriggerPressed(pad);
            else SaturnPeripheral.PadRTriggerReleased(pad);
            if ((buttons & (1 << 0)) != 0) SaturnPeripheral.PadAPressed(pad);
            else SaturnPeripheral.PadAReleased(pad);
            if ((buttons & (1 << 1)) != 0) SaturnPeripheral.PadXPressed(pad);
            else SaturnPeripheral.PadXReleased(pad);

            if (id is SaturnPeripheralId.ThreeDimensionalPad or SaturnPeripheralId.Wheel or SaturnPeripheralId.MissionStick)
            {
                EmulationControllerState controller = port < _polledInput.Controllers.Count
                    ? _polledInput.Controllers[(int)port]
                    : EmulationControllerState.Empty;
                SaturnPeripheral.Axis1Value(pad, (uint)((controller.LeftX - short.MinValue) * 255 / ushort.MaxValue));
                if (id != SaturnPeripheralId.Wheel)
                    SaturnPeripheral.Axis2Value(pad, (uint)((controller.LeftY - short.MinValue) * 255 / ushort.MaxValue));
                if (id == SaturnPeripheralId.MissionStick)
                    SaturnPeripheral.Axis3Value(pad, (uint)((controller.RightY - short.MinValue) * 255 / ushort.MaxValue));
                else if (id == SaturnPeripheralId.ThreeDimensionalPad)
                {
                    int leftTrigger = (buttons & (1 << 12)) != 0 && controller.LeftTrigger == 0 ? short.MaxValue : controller.LeftTrigger;
                    int rightTrigger = (buttons & (1 << 13)) != 0 && controller.RightTrigger == 0 ? short.MaxValue : controller.RightTrigger;
                    SaturnPeripheral.Axis3Value(pad, (uint)(Math.Max(0, leftTrigger) * 255 / short.MaxValue));
                    SaturnPeripheral.Axis4Value(pad, (uint)(Math.Max(0, rightTrigger) * 255 / short.MaxValue));
                }
            }
        }
    }

    private void PublishSaturnVideo(SaturnVideoSoftware video)
    {
        const int bytesPerPixel = 4;
        int width = video.DisplayWidth;
        int pitch = width * bytesPerPixel;
        int? parity = video.DisplayFieldParity;
        int height = parity.HasValue ? video.DisplayHeight / 2 : video.DisplayHeight;
        ReadOnlySpan<uint> source = video.DisplayBuffer;
        byte[] pixels = new byte[checked(pitch * height)];
        for (int y = 0; y < height; y++)
        {
            int sourceRow = parity is { } field ? y * 2 + field : y;
            var row = source.Slice(sourceRow * width, width);
            for (int x = 0; x < width; x++)
            {
                uint rgb = BitConverter.IsLittleEndian ? row[x] : BinaryPrimitives.ReverseEndianness(row[x]);
                uint xrgb = ((rgb & 0xFF) << 16) | (rgb & 0xFF00) | ((rgb >> 16) & 0xFF);
                BinaryPrimitives.WriteUInt32LittleEndian(pixels.AsSpan(y * pitch + x * bytesPerPixel), xrgb);
            }
        }
        LatestVideoFrame = new(pixels, width, height, pitch, EmulationPixelFormat.Xrgb8888, 4f / 3f, _sequence, _timestamp)
        {
            ScanMode = parity switch
            {
                0 => EmulationVideoScanMode.InterlacedEvenField,
                1 => EmulationVideoScanMode.InterlacedOddField,
                _ => EmulationVideoScanMode.Progressive
            }
        };
    }
    private Stream? OpenMedia(string path)
    {
        _lifetime.Token.ThrowIfCancellationRequested();
        return File.Exists(path) ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.RandomAccess) : null;
    }
    public void RunFrame()
    {
        if (_saturn is null && _system is null)
            throw new InvalidOperationException(ErrorMessages.CoreNotInitialized());
        bool completed = false;
        try
        {
            bool skipVideo = _saturn is null && ShouldSkipFrame();
            short[]? samples = null;
            int count;
            if (_saturn is { } saturn)
            {
#if !USE_SCSP2
                _saturnAudioFrames = 0;
#endif
                PollSaturnInput();
                saturn.ExecuteFrame();
#if USE_SCSP2
                lock (_saturnAudioGate)
                {
                    count = _saturnAudioFrames;
                    samples = _samples.AsSpan(0, count * 2).ToArray();
                    _saturnAudioFrames = 0;
                }
#else
                count = _saturnAudioFrames;
#endif
            }
            else
                count = _system!.RunFrame(_samples, skipVideo);
            _timestamp += TimeSpan.FromSeconds(1 / FramesPerSecond);
            _sequence++;
            if (_saturn is { } currentSaturn) PublishSaturnVideo(currentSaturn.Video);
            else if (!skipVideo || LatestVideoFrame is null) PublishVideo(_system!);
            samples ??= _samples.AsSpan(0, count * 2).ToArray();
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
    private void ClearAudio()
    {
#if USE_SCSP2
        lock (_saturnAudioGate)
            _saturnAudioFrames = 0;
#endif
        _audio.Clear();
        _bufferedAudioFrames = 0;
        LatestAudioChunk = null;
    }
    public void SetInput(EmulationInputSnapshot snapshot) { lock (_inputGate) _pendingInput = snapshot; }
    public void HardReset()
    {
        if (_saturn is { } saturn) saturn.Reset();
        else _system!.Reset();
        ClearAudio();
    }
    public void SoftReset()
    {
        if (_saturn is { } saturn) saturn.ResetButton();
        else _system!.Reset(false);
        ClearAudio();
    }
    public byte[] SaveState()
    {
        if (!SupportsSaveStates) throw new NotSupportedException();
        byte[] bytes = new byte[_system!.StateSize];
        int length = _system.SaveState(bytes);
        if (length != bytes.Length) Array.Resize(ref bytes, length);
        return bytes;
    }
    public void LoadState(ReadOnlySpan<byte> state)
    {
        if (!SupportsSaveStates) throw new NotSupportedException();
        if (_system!.LoadState(state.ToArray()) == 0) throw MachineExceptions.SavedStateIncompatible();
        ClearAudio();
    }
    public DiskStatus GetDiskStatus() => new(_disks.Count, _diskIndex, _ejected,
        _disks.Select((path, index) => new DiskImageStatus(index, path, Path.GetFileNameWithoutExtension(path))).ToArray());
    public void SelectDisk(int index)
    {
        if ((uint)index >= _disks.Count) throw new ArgumentOutOfRangeException(nameof(index));
        _diskIndex = index;
        if (!_ejected) MountDisc(_disks[index]);
    }
    private void MountDisc(string path)
    {
        if (_system is Sf7000Machine sf7000) sf7000.SetDisk(path);
        else if (_saturn is { } saturn)
        {
            if (!IsSaturnDisc(path)) throw new InvalidDataException(ErrorMessages.ContentRefused());
            saturn.ChangeDisc(path);
        }
        else _system!.MountDisc(path);
    }
    private string? GetSelectedMasterSystemConverter() => _configuration is { } config
        ? ModelConstants.GetMasterSystemConverter(config.Model, config.Options) : null;

    private bool UsesMasterSystemMedia() => _configuration?.Model is ModelConstants.MasterSystem or ModelConstants.MasterSystemII
        || GetSelectedMasterSystemConverter() is not null;

    private bool HasSegaCardSlot() => _configuration?.Model == ModelConstants.MasterSystem
        || GetSelectedMasterSystemConverter() == ModelConstants.MasterSystemConverterI;

    private void ValidateMasterSystemMedia(MediaConfiguration media)
    {
        if (media.Category != MediaCategory.Cartridge
            || media.Slot != default && media.Slot != EmulationMediaSlot.Cartridge0 && media.Slot != EmulationMediaSlot.Cartridge1)
            throw new InvalidDataException(ErrorMessages.ContentRefused());
        if (GetSelectedMasterSystemConverter() is not null &&
            ConfigurationValidationFunctions.IsIncompatibleMedia(ModelCatalog.Get(ModelConstants.MegaDrive), media, _configuration!.Options))
            throw new InvalidDataException(ErrorMessages.MediaRefused());
    }

    private static bool IsMasterSystemSegaCard(MediaConfiguration media) => media.Slot == EmulationMediaSlot.Cartridge1
        || Path.GetExtension(media.Path).Equals(".mv", StringComparison.OrdinalIgnoreCase);

    private string GetMasterSystemSegaCard(string path) => HasSegaCardSlot()
        ? path : throw new InvalidDataException(ErrorMessages.ContentRefused());

    private void SetMasterSystemMedia(string? cartridge, string? card)
    {
        string next = cartridge ?? card ?? string.Empty;
        if (!StringComparer.OrdinalIgnoreCase.Equals(_contentPath, next))
        {
            SavePersistent();
            _contentPath = next;
            StartSystem(savePrevious: false);
        }
        _masterSystemCartridge = cartridge;
        _masterSystemSegaCard = card;
    }

    public void InsertMedia(MediaConfiguration media)
    {
        if (UsesMasterSystemMedia())
        {
            ValidateMasterSystemMedia(media);
            if (IsMasterSystemSegaCard(media))
                SetMasterSystemMedia(_masterSystemCartridge, GetMasterSystemSegaCard(media.Path));
            else
                SetMasterSystemMedia(media.Path, _masterSystemSegaCard);
        }
        else if (_system is Sc3000Machine computer && media.Category == MediaCategory.Tape)
        {
            MediaTapeSignal tape = ReadSc3000Tape(media.Path);
            computer.Cassette.Insert(tape);
            _sc3000Tape = tape;
        }
        else if (IsDisc(media.Path) || (_saturn is not null && IsSaturnDisc(media.Path))
            || (_system is Sf7000Machine && (media.Category == MediaCategory.Floppy || IsSf7000Disk(media.Path))))
        {
            int index = _disks.IndexOf(media.Path);
            if (index < 0) { index = _disks.Count; _disks.Add(media.Path); }
            MountDisc(media.Path);
            _diskIndex = index; _ejected = false;
        }
        else { SavePersistent(); _contentPath = media.Path; StartSystem(savePrevious: false); }
    }
    public void EjectMedia(EmulationMediaSlot slot)
    {
        if (UsesMasterSystemMedia())
        {
            if (slot == EmulationMediaSlot.Cartridge0) SetMasterSystemMedia(null, _masterSystemSegaCard);
            else if (slot == EmulationMediaSlot.Cartridge1 && HasSegaCardSlot())
                SetMasterSystemMedia(_masterSystemCartridge, null);
            else throw new InvalidDataException(ErrorMessages.ContentRefused());
        }
        else if (_system is Sc3000Machine computer && slot == EmulationMediaSlot.Tape0)
        {
            computer.Cassette.Eject();
            _sc3000Tape = null;
        }
        else if (_system is Sf7000Machine sf7000 && slot == EmulationMediaSlot.Floppy0)
        {
            sf7000.SetDisk(null);
            _ejected = true;
        }
        else if (slot.Category == EmulationMediaCategory.CompactDiscDrive)
        {
            if (_saturn is { } saturn) saturn.ChangeDisc(null);
            else _system!.UnmountDisc();
            _ejected = true;
        }
        else { SavePersistent(); _contentPath = string.Empty; StartSystem(savePrevious: false); }
    }
    private MediaTapeSignal ReadSc3000Tape(string path)
    {
        if (!Path.GetExtension(path).Equals(".wav", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(ErrorMessages.ContentRefused());
        return MediaEngineComposition.RuntimePreparation.ReadTapeAsync(path, _lifetime.Token)
            .GetAwaiter().GetResult();
    }


    public void SaveMediaChanges(EmulationMediaSlot slot) => SavePersistent();
    public bool HasUnsavedMediaChanges(EmulationMediaSlot slot) => _saturn?.BackupRamWritten == true;
    public void Stop()
    {
        try { SavePersistent(); }
        finally
        {
            try { DisposeSaturn(); }
            finally
            {
                try { _system?.Dispose(); }
                finally { _system = null; _sc3000Tape = null; _masterSystemCartridge = _masterSystemSegaCard = null; ClearAudio(); LatestVideoFrame = null; }
            }
        }
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _lifetime.Cancel(); }
        finally { try { Stop(); } finally { _lifetime.Dispose(); _audioStatus = null; } }
    }
}

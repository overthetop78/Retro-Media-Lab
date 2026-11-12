using System.IO;
using System.Security.Cryptography;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Enums;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Services;
using C = RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants.EmulatorConstants;
using H = RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Constants.Sg1000Constants;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Services;

/// <summary>Native machine backend. Common owns the session thread and the host audio/video devices.</summary>
internal sealed partial class EmulatorCore : IEmulatorCore
{
    private Sg1000Board? _board;
    private byte[] _cartridge = [], _internalGame = [];
    private Sg1000RomConfiguration _romConfiguration = new();
    private bool _disposed;
    private readonly PeripheralCategory[] _controllers = [PeripheralCategory.SegaSg1000Joystick, PeripheralCategory.SegaSg1000Joystick];
    internal Sg1000Board Board => _board ?? throw new InvalidOperationException("The machine is not initialized.");
    public string Emulator => C.Id;
    public string CoreName => C.Name;
    public string CoreVersion => C.Version;
    public string CoreSha256 { get; } = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(EmulatorCore).Assembly.Location)));
    public double FramesPerSecond => H.CrystalHertz / (2d * 342 * 262);
    public long? ProcessorClockHz => H.CrystalHertz / 3;
    public int SampleRate => H.SampleRate;
    public bool SupportsSaveStates => _board is not null;
    public bool SupportsPointerCapture => false;
    public bool ShutdownRequested => false;
    public EmulationVideoStandard? Region => EmulationVideoStandard.Ntsc;
    public HostProcessState HostProcessState => HostProcessState.InProcess;
    public int? HostProcessId => null;
    public long AudioOverrunCount => 0;
    public long AudioUnderrunCount => 0;
    public int MinimumAudioLatencyMilliseconds => 0;
    public void SetAudioBufferStatus(AudioOutputBufferStatus? status) { }
    public int DiskCount => 0;
    public int CurrentDiskIndex => -1;
    public IReadOnlyList<CoreOption> Options => [];
    public IReadOnlyList<string> Diagnostics => [];
    public IReadOnlyDictionary<int, bool> LedStates { get; } = new Dictionary<int, bool>();
    public IReadOnlySet<string> SupportedContentExtensions => C.Extensions;

    public void Initialize(MachineConfiguration configuration, string sessionDirectory, string? saveDirectory = null)
    {
        var inserted = (configuration.Media ?? []).Where(media => media.IsInserted).ToArray();
        if (inserted.Length > 1 || inserted.Any(media => media.Slot != EmulationMediaSlot.Cartridge0 || media.Category != MediaCategory.Cartridge))
            throw new ArgumentException("SG-1000 has one cartridge slot.", nameof(configuration));
        bool enabled = ReadRomConfiguration(configuration).InternalGameEnabled;
        string? internalPath = configuration.FirmwarePaths?.GetValueOrDefault(C.InternalGameFirmware);
        Initialize(configuration, inserted.Length == 0 ? [] : File.ReadAllBytes(inserted[0].Path),
            enabled ? File.ReadAllBytes(internalPath ?? throw new ArgumentException("An internal game ROM must be selected.")) : []);
    }

    // The same initialization path accepts owned synthetic images without filesystem fixtures.
    internal void Initialize(MachineConfiguration configuration, ReadOnlySpan<byte> cartridge, ReadOnlySpan<byte> internalGame = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_board is not null) throw new InvalidOperationException("The machine is already initialized.");
        if (configuration.Model != ModelConstants.Sg1000 || configuration.EmulatorId != C.Id)
            throw new ArgumentException("This backend currently implements the SG-1000 NTSC machine.", nameof(configuration));
        var romConfiguration = ReadRomConfiguration(configuration);
        var controllers = Enumerable.Range(0, 2).Select(port => configuration.Controllers?.ElementAtOrDefault(port) ?? PeripheralCategory.SegaSg1000Joystick).ToArray();
        foreach (var controller in controllers) ValidateController(controller);
        Sg1000Board? candidate = null;
        try
        {
            candidate = new(cartridge, romConfiguration: romConfiguration, internalGame: internalGame);
            _cartridge = cartridge.ToArray();
            _internalGame = romConfiguration.InternalGameEnabled ? internalGame.ToArray() : [];
            _romConfiguration = romConfiguration;
            controllers.CopyTo(_controllers, 0);
            _board = candidate;
            candidate = null;
        }
        finally { candidate?.Dispose(); }
    }

    internal static Sg1000RomConfiguration ReadRomConfiguration(MachineConfiguration configuration)
    {
        bool enabled = false;
        if (configuration.Options?.TryGetValue(C.InternalGameEnabled, out var value) == true && !bool.TryParse(value, out enabled))
            throw new ArgumentException("Invalid internal game option.", nameof(configuration));
        var layout = Sg1000CartridgeLayout.Standard;
        if (configuration.Options?.TryGetValue(C.CartridgeLayout, out value) == true
            && (!Enum.TryParse(value, false, out layout) || !Enum.IsDefined(layout)))
            throw new ArgumentException("Invalid cartridge layout.", nameof(configuration));
        return new(enabled, layout);
    }

    public void SetInput(EmulationInputSnapshot snapshot)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var board = Board;
        for (int port = 0; port < 2; port++)
        {
            uint buttons = _controllers[port] == PeripheralCategory.None ? 0 : snapshot.Controllers.ElementAtOrDefault(port)?.Buttons ?? 0;
            // Common command indices: U/D/L/R=4..7, B=0 (button1), A=8 (button2).
            byte contacts = (byte)(((buttons >> 4) & 15) | ((buttons & 1) << 4) | (((buttons >> 8) & 1) << 5));
            board.SetController(port, contacts);
        }
        // Start is the host binding of the console's physical Pause/NMI button, not a pad contact.
        board.SetPause(snapshot.Controllers.Any(controller => (controller.Buttons & (1U << 3)) != 0));
    }

    internal static void ValidateController(PeripheralCategory peripheral)
    {
        if (peripheral is not (PeripheralCategory.Automatic or PeripheralCategory.None or PeripheralCategory.Joypad
            or PeripheralCategory.SegaSg1000Joystick or PeripheralCategory.SegaSg1000IiJoypad))
            throw new NotSupportedException("This peripheral is not wired to the SG-1000 backend.");
    }
    public void SetControllerPortDevice(int port, PeripheralCategory peripheral)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)port, 2U);
        ValidateController(peripheral);
        _controllers[port] = peripheral;
        if (peripheral == PeripheralCategory.None) Board.SetController(port, 0);
    }
    public void InsertMedia(MediaConfiguration media)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (media.Slot != EmulationMediaSlot.Cartridge0 || media.Category != MediaCategory.Cartridge || !media.IsInserted)
            throw new ArgumentException("Expected an inserted cartridge in slot0.", nameof(media));
        byte[] image = File.ReadAllBytes(media.Path);
        Board.SetCartridge(image);
        _cartridge = image;
    }
    public void EjectMedia(EmulationMediaSlot slot)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (slot != EmulationMediaSlot.Cartridge0) throw new ArgumentOutOfRangeException(nameof(slot));
        Board.SetCartridge([]);
        _cartridge = [];
    }
    public DiskStatus GetDiskStatus() => new(0, -1, true, []);
    public void SelectDisk(int index) => throw new NotSupportedException("SG-1000 has no disk drive.");
    public void SoftReset() => throw new NotSupportedException("The SG-1000 has no reset button; Pause generates NMI.");
    public void HardReset()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _ = Board;
        Sg1000Board? replacement = null;
        try
        {
            replacement = new(_cartridge, romConfiguration: _romConfiguration, internalGame: _internalGame);
            var old = _board;
            _board = replacement; replacement = null;
            try { ClearOutput(); } finally { old?.Dispose(); }
        }
        finally { replacement?.Dispose(); }
    }
    public void SetOption(string key, string value) => throw new NotSupportedException("Hardware configuration requires recreation of the machine.");
    public void SaveMediaChanges(EmulationMediaSlot slot) { }
    public bool HasUnsavedMediaChanges(EmulationMediaSlot slot) => false;
    public void Stop() => Dispose();
    public void Dispose()
    {
        if (_disposed) return;
        try { _board?.Dispose(); }
        finally { _board = null; _cartridge = []; _internalGame = []; ClearOutput(); _disposed = true; }
    }
}

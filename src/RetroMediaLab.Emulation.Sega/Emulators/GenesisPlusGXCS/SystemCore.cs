using System.Buffers.Binary;
using System.IO;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cpu;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Input;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Sound;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS;

internal sealed partial class SystemCore : IZ80Bus, IM68kBus, IVdpBus, IInputBus,
    ISmsCartridgeBus, IMdCartridgeBus, IScdHost, IMegaSdBus, ICartridgeAudioMedia, IDisposable
{
    private readonly SystemConfiguration config;
    private readonly CartridgeState cart = new();
    private readonly RomInformation rominfo = new();
    private readonly BackupRam _backup = new();
    private readonly CartridgeAddressSpace _addresses = new();
    private readonly Memory<byte>[] _readMap = new Memory<byte>[64], _writeMap = new Memory<byte>[64];
    private readonly CartridgePageMap z80_readmap;
    private readonly byte[] work_ram = new byte[65536], zram = new byte[8192], boot_rom = new byte[65536], tmss = new byte[4];
    private readonly InputHardware _input;
    private readonly Vdp _vdp;
    private readonly SmsCartridge _sms;
    private readonly MdCartridge _md;
    private readonly Scd _cd;
    private readonly CdCartridge _cdCartridge;
    private readonly MegaSd _megaSd;
    private readonly Yx5200 _yx5200;
    private readonly Eeprom93c _eeprom;
    private readonly BlipBuffer _mainAudio, _pcmAudio, _cdAudio, _cartridgeAudio;
    private readonly Func<string, Stream?> _openFile;
    private readonly Func<int, Memory<byte>, int> _loadFirmware;
    private readonly CancellationToken _cancellation;
    private M68k<SystemCore> _mainCpu;
    private Z80<SystemCore> _z80;
    private SoundUnit _sound = null!;
    private bool _cdAudioAttached, _cartridgeAudioAttached, _disposed;
    private string _mediaPath = "";
    private uint zbank, mcycles_vdp;
    private byte zstate, pico_current, pause_b, system_hw, system_bios, region_code;
    private uint system_clock = SystemHardware.NtscClock;
    private short SVP_cycles = 800;
    private int _sampleRate;
    private double _frameRate;
    private Func<uint, byte> z80_readmem = null!, z80_readport = null!;
    private Action<uint, byte> z80_writemem = null!, z80_writeport = null!;
    public Action? PollInput { get; set; }
    public InputHardware Input => _input;
    public Vdp Video => _vdp;
    public BackupRam Backup => _backup;
    public RomInformation Information => rominfo;
    public byte Hardware => system_hw;
    public uint MasterClock => system_clock;
    public uint FrameCycles => mcycles_vdp;
    public double NominalFrameRate => (double)system_clock / (3420 * _vdp.lines_per_frame);

    public SystemCore(byte hardware, SystemConfiguration configuration, byte[] framebuffer,
        int width, int height, int pitch, int sampleRate, Func<string, Stream?> openFile,
        Func<int, Memory<byte>, int> loadFirmware, CancellationToken cancellation)
    {
        config = configuration; system_hw = hardware; _sampleRate = sampleRate;
        _openFile = openFile; _loadFirmware = loadFirmware; _cancellation = cancellation;
        z80_readmap = new(_readMap);
        _mainAudio = new(sampleRate / 10); _pcmAudio = new(sampleRate / 10);
        _cdAudio = new(sampleRate / 10); _cartridgeAudio = new(sampleRate / 10);
        _mainCpu = new(this); _z80 = new(this, z80_irq_callback);
        _input = new(this);
        _vdp = new(hardware, this, config.Video, framebuffer, width, height, pitch);
        _eeprom = new(_backup);
        _sms = new(this, cart, rominfo, _backup, _eeprom, _input, config.Sms, _readMap, _writeMap, work_ram);
        _md = new(this, cart, rominfo, _backup, _addresses, _input, config.Cartridge);
        _cd = new(this, _addresses, _pcmAudio, _cdAudio, openFile, cancellation);
        _cdCartridge = new(this, _md.Initialize, _backup, _addresses);
        _megaSd = new(this, cart, _backup, _addresses);
        _yx5200 = new(_cartridgeAudio, this, cancellation);
    }

    private void InitializeSystem()
    {
        config.PadTypes.CopyTo(_input.ConfiguredPadTypes);
        gen_init();
        _input.SetMachineContext(system_hw, romtype, rominfo.peripherals, cart.special);
        _input.InitializeIo();
        _vdp.system_hw = system_hw;
        _vdp.Initialize(vdp_pal);
        _sound = new(system_hw, config.Sound, _mainAudio);
        if (_cdAudioAttached || system_hw == SystemHardware.MegaCd) EnsureCdAudio();
        if (_cartridgeAudioAttached) EnsureCartridgeAudio();
        SetAudioRate(_sampleRate, _frameRate);
    }

    public void Reset(bool hard = true)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _cancellation.ThrowIfCancellationRequested();
        gen_reset(hard ? 1 : 0);
        if (hard)
        {
            _input.ResetIo(); _vdp.Reset(); _sound.Reset(); _sound.ResetOutput();
        }
    }

    public int RunFrame(Span<short> audio, bool skipVideo = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _cancellation.ThrowIfCancellationRequested();
        bool completed = false;
        try
        {
            if (system_hw == SystemHardware.MegaCd) system_frame_scd(skipVideo ? 1 : 0);
            else if (SystemHardware.IsSixteenBit(system_hw)) system_frame_gen(skipVideo ? 1 : 0);
            else system_frame_sms(skipVideo ? 1 : 0);
            int count = _sound.UpdateAudio(mcycles_vdp, audio);
            completed = true;
            return count;
        }
        finally { if (!completed) Dispose(); }
    }

    public void SetAudioRate(int sampleRate, double frameRate = 0)
    {
        _sampleRate = sampleRate; _frameRate = frameRate;
        _sound.SetOutputRate(sampleRate, frameRate, system_clock, vdp_pal != 0);
        if (_cdAudioAttached)
        {
            double clock = frameRate != 0 ? 3420 * (vdp_pal != 0 ? 313 : 262) * frameRate : system_clock;
            _cd.PcmChip.Initialize(clock / system_clock * Scd.MasterClock, sampleRate);
            _cd.Drive.Initialize(sampleRate);
        }
        if (_cartridgeAudioAttached) _yx5200.Initialize(sampleRate);
    }

    private void EnsureCdAudio()
    {
        _cdAudioAttached = true;
        if (_sound is not null)
            _sound.AttachCdAudio(_pcmAudio, _cdAudio, samples => _cd.PcmChip.Update((uint)samples), samples => _cd.Drive.UpdateAudio((uint)samples));
    }
    private void EnsureCartridgeAudio()
    {
        _cartridgeAudioAttached = true;
        if (_sound is not null) _sound.AttachCartridgeAudio(_cartridgeAudio, _yx5200.Update);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _yx5200.Dispose(); }
        finally
        {
            try { _cd.Dispose(); }
            finally
            {
                try { _cdCartridge.Dispose(); }
                finally
                {
                    try { _md.Dispose(); }
                    finally
                    {
                        _mainCpu.Halt(); _z80.State.irq_callback = null; _z80.State.daisy = null;
                        _z80.Cycles = uint.MaxValue; zstate = 0;
                        if (_sound is not null)
                        {
                            _sound.AttachCdAudio(null, null, null, null);
                            _sound.AttachCartridgeAudio(null, null);
                        }
                        foreach (CartridgeMemoryBank bank in _addresses.Main)
                        {
                            bank.Data = default; bank.read8 = bank.read16 = null; bank.write8 = bank.write16 = null;
                        }
                        foreach (CartridgeZ80Bank bank in _addresses.Z80) { bank.read = null; bank.write = null; }
                        Array.Clear(_readMap); Array.Clear(_writeMap);
                        z80_readport = z80_readmem = null!; z80_writeport = z80_writemem = null!;
                        _mainAudio.Clear(); _pcmAudio.Clear(); _cdAudio.Clear(); _cartridgeAudio.Clear();
                        PollInput = null;
                    }
                }
            }
        }
    }
}

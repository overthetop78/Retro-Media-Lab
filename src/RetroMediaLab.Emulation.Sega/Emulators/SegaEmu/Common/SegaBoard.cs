using System.IO;
using RetroMediaLab.Chips.Common.Memory;
using RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction;
using RetroMediaLab.Chips.Controllers.Audio.YX5200;
using RetroMediaLab.Chips.Controllers.Sega.Input;
using RetroMediaLab.Chips.Video.Sega.VDP;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Common.Audio;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.MegaCd.Hardware;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Media;
using BlipBuffer = RetroMediaLab.Chips.Audio.Common.Resampling.BandLimitedBuffer;
using Cdd = RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Media.CdDrive;
using Pcm = RetroMediaLab.Chips.Audio.Ricoh.RF5C164.Rf5c164;
using System.Buffers.Binary;
using System.IO;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Common;

internal abstract partial class SegaBoard : ICpuZ80Bus, ICpu68000Bus, IVdpBus, IInputBus,
    ISmsCartridgeBus, IMdCartridgeBus, IScdHost, IMegaSdBus, IAudioTrackProvider, IDisposable
{
    protected abstract void ExecuteMachineFrame(int skip);
    protected abstract void InitializeMachineBus();
    protected abstract void ConfigureZ80Ports();
    protected abstract bool AcceptsHardware(byte hardware);
    private Cpu68000State _eightBitMainTiming;
    private readonly SystemConfiguration config;
    private readonly CartridgeState cart = new();
    private readonly RomInformation rominfo = new();
    private readonly BackupRam _backup = new();
    private readonly CartridgeAddressSpace _addresses = new();
    private readonly Memory<byte>[] _readMap = new Memory<byte>[64], _writeMap = new Memory<byte>[64];
    private readonly BytePageMap z80_readmap;
    private readonly byte[] work_ram = new byte[65536], zram = new byte[8192], boot_rom = new byte[65536], tmss = new byte[4];
    private readonly InputHardware _input;
    private readonly Vdp _vdp;
    private readonly SmsCartridge _sms;





    private readonly Eeprom93c _eeprom;
    private readonly BlipBuffer _mainAudio, _pcmAudio, _cdAudio, _cartridgeAudio;
    private readonly Func<string, Stream?> _openFile;
    private readonly Func<int, Memory<byte>, int> _loadFirmware;
    private readonly CancellationToken _cancellation;

    private Cpu68000<SegaBoard>? _mainCpuComponent;
    private Cpu68000<SegaBoard> _mainCpu => _mainCpuComponent ??= new(this);
    private MdCartridge? _mdComponent;
    private MdCartridge _md => _mdComponent ??= new(this,cart,rominfo,_backup,_addresses,_input,config.Cartridge);
    private Scd? _cdComponent;
    private Scd _cd => _cdComponent ??= new(this,_addresses,_pcmAudio,_cdAudio,_openFile,_cancellation);
    private CdCartridge? _cdCartridgeComponent;
    private CdCartridge _cdCartridge => _cdCartridgeComponent ??= new(this,_md.Initialize,_backup,_addresses);
    private MegaSd? _megaSdComponent;
    private MegaSd _megaSd => _megaSdComponent ??= new(this,cart,_backup,_addresses);
    private Yx5200? _yx5200Component;
    private Yx5200 _yx5200 => _yx5200Component ??= new(_cartridgeAudio,this,_cancellation);
    private int DiscLoaded => _cdComponent?.Drive.Loaded ?? 0;
    private CpuZ80<SegaBoard> _z80;
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

    protected SegaBoard(byte hardware, SystemConfiguration configuration, byte[] framebuffer,
        int width, int height, int pitch, int sampleRate, Func<string, Stream?> openFile,
        Func<int, Memory<byte>, int> loadFirmware, CancellationToken cancellation)
    {
        config = configuration; system_hw = hardware; _sampleRate = sampleRate;
        if (config.system == 0) config.system = hardware;
        _openFile = openFile; _loadFirmware = loadFirmware; _cancellation = cancellation;
        z80_readmap = new(_readMap);
        _mainAudio = new(sampleRate / 10); _pcmAudio = new(sampleRate / 10);
        _cdAudio = new(sampleRate / 10); _cartridgeAudio = new(sampleRate / 10);
        _z80 = new(this, z80_irq_callback);
        _input = new(this);
        _vdp = new(hardware, this, config.Video, framebuffer, width, height, pitch);
        _eeprom = new(_backup);
        _sms = new(this, cart, rominfo, _backup, _eeprom, _input, config.Sms, _readMap, _writeMap, work_ram);





    }

    private void InitializeSystem()
    {
        config.PadTypes.CopyTo(_input.ConfiguredPadTypes);
        InitializeMachineBus();
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
            ExecuteMachineFrame(skipVideo ? 1 : 0);
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
        try { _yx5200Component?.Dispose(); }
        finally
        {
            try { _cdComponent?.Dispose(); }
            finally
            {
                try { _cdCartridgeComponent?.Dispose(); }
                finally
                {
                    try { _mdComponent?.Dispose(); }
                    finally
                    {
                        _mainCpuComponent?.Halt(); _z80.State.irq_callback = null; _z80.State.daisy = null;
                        _z80.Cycles = uint.MaxValue; zstate = 0;
                        if (_sound is not null) { _sound.AttachCdAudio(null,null,null,null); _sound.AttachCartridgeAudio(null,null); }
                        foreach (CartridgeMemoryBank bank in _addresses.Main) { bank.Data = default; bank.read8 = bank.read16 = null; bank.write8 = bank.write16 = null; }
                        foreach (CartridgeZ80Bank bank in _addresses.Z80) { bank.read = null; bank.write = null; }
                        Array.Clear(_readMap); Array.Clear(_writeMap);
                        z80_readport = z80_readmem = null!; z80_writeport = z80_writemem = null!;
                        _mainAudio.Clear(); _pcmAudio.Clear(); _cdAudio.Clear(); _cartridgeAudio.Clear(); PollInput = null;
                    }
                }
            }
        }
    }
}

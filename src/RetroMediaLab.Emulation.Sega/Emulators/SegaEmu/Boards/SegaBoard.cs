using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Buses;
using System.IO;
using RetroMediaLab.Chips.Common.Memory;
using RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction;
using RetroMediaLab.Chips.Controllers.Audio.YX5200;
using RetroMediaLab.Chips.Controllers.Sega.Input;
using RetroMediaLab.Chips.Video.Sega.VDP;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Audio;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.MegaCd.Hardware;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Services;

using BlipBuffer = RetroMediaLab.Chips.Audio.Common.Resampling.BandLimitedBuffer;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Boards;

internal abstract partial class SegaBoard : ICpuZ80Bus, IVdpBus, IInputBus,
    ISmsCartridgeBus, IMdCartridgeBus, IScdHost, IMegaSdBus, IAudioTrackProvider, IDisposable
{
    protected abstract Action<int> CreateFrameScheduler();
    private Action<int> _executeFrame = null!;
    protected abstract void InitializeMachineBus();
    protected abstract void ConfigureZ80Ports();
    protected abstract bool AcceptsHardware(byte hardware);
    private Cpu68000State _eightBitMainTiming;
    private readonly SystemConfiguration config;
    private readonly CartridgeState cart;
    private readonly RomInformation rominfo = new();
    private readonly BackupRam _backup = new();
    private readonly CartridgeAddressSpace _addresses = new();
    private readonly Memory<byte>[] _readMap = new Memory<byte>[64], _writeMap = new Memory<byte>[64];
    private long _nativeZ80MappingVersion;
    private void NativeZ80MappingChanged() => _nativeZ80MappingVersion = unchecked(_nativeZ80MappingVersion + 1);
    private readonly Func<uint, byte> _nativeZ80RamRead;
    private readonly Action<uint, byte> _nativeZ80RamWrite;
    private readonly BytePageMap z80_readmap;
    private readonly byte[] work_ram = new byte[65536], zram = new byte[8192], boot_rom = new byte[65536], tmss = new byte[4];
    private readonly InputHardware _input;
    private readonly Vdp _vdp;
    private SmsCartridge? _smsComponent;
    private SmsCartridge _sms => _smsComponent ??= new(this, cart, rominfo, _backup, new(_backup), _input, config.Sms, _readMap, _writeMap, work_ram, NativeZ80MappingChanged);

    private readonly BlipBuffer _mainAudio;
    private BlipBuffer? _pcmAudioBuffer, _cdAudioBuffer, _cartridgeAudioBuffer;
    private BlipBuffer _pcmAudio => _pcmAudioBuffer ??= new(_sampleRate / 10);
    private BlipBuffer _cdAudio => _cdAudioBuffer ??= new(_sampleRate / 10);
    private BlipBuffer _cartridgeAudio => _cartridgeAudioBuffer ??= new(_sampleRate / 10);
    private readonly Func<string, Stream?> _openFile;
    private readonly Func<int, Memory<byte>, int> _loadFirmware;
    private readonly CancellationToken _cancellation;
    private readonly Gpu32XFrameComposer? _gpu32XComposer;
    private readonly bool _useDynamicCpuBlocks;
    private readonly bool _useNative68000Blocks;

    private Cpu68000<MegaDriveCpuBus>? _mainCpuComponent;
    private Cpu68000<MegaDriveCpuBus> _mainCpu => _mainCpuComponent ??= Functions.CpuConfigurationFunctions.CreateMain(new(_addresses.Main, _vdp, MachineInterruptAcknowledger), _useDynamicCpuBlocks);
    private MdCartridge? _mdComponent;
    private MdCartridge _md => _mdComponent ??= new(this,cart,rominfo,_backup,_addresses,_input,config.Cartridge);
    private Scd? _cdComponent;
    private Scd _cd
    {
        get
        {
            if (_cdComponent is null)
            {
                _cdComponent = new(this,_addresses,_pcmAudio,_cdAudio,_openFile,_cancellation);
                _cdComponent.Cpu.UseDynamicBlocks = _useDynamicCpuBlocks;
                _cdComponent.UseNative68000Blocks = _useNative68000Blocks;
            }
            return _cdComponent;
        }
    }
    private CdCartridge? _cdCartridgeComponent;
    private CdCartridge _cdCartridge => _cdCartridgeComponent ??= new(this,_md.Initialize,_backup,_addresses) { ConfiguredRamId = config.CdCartridgeId };
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
    private byte zstate, pause_b, system_hw, system_bios, region_code;
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
    protected BlipBuffer MachineAudioBuffer => _mainAudio;
    protected uint MachineCpuCycles => unchecked((uint)main.cycles);
    protected virtual Action<uint>? MachineAudioSynchronizer => _mega32X is null ? null : _mega32X.Synchronize;
    protected virtual Func<uint, bool>? MachineInterruptAcknowledger => null;
    protected virtual uint AuxiliaryInterruptLevel => 0;
    protected virtual uint MapVideoInterruptLevel(uint level) => level;
    protected virtual void ConfigureMachineAudio(int sampleRate, double frameRate) { }
    protected virtual void ResetMachineDevices() { _mega32X?.Reset(); }
    protected virtual void FinishMachineAudioFrame(uint cycles) { _mega32X?.EndFrame(cycles); }
    protected void RefreshMachineInterrupt()
    {
        uint video = (_vdp.reg[1] & _vdp.vint_pending) != 0 ? 6u : (_vdp.reg[0] & _vdp.hint_pending) != 0 ? 4u : 0u;
        _mainCpu.SetIrq(Math.Max(MapVideoInterruptLevel(video), AuxiliaryInterruptLevel));
    }

    protected SegaBoard(byte hardware, SystemConfiguration configuration, byte[] framebuffer,
        int width, int height, int pitch, int sampleRate, Func<string, Stream?> openFile,
        Func<int, Memory<byte>, int> loadFirmware, CancellationToken cancellation,
        Gpu32XFrameComposer? gpu32XComposer = null, bool useDynamicCpuBlocks = false, bool useNative68000Blocks = false)
    {
        config = configuration; system_hw = hardware; _sampleRate = sampleRate;
        _nativeZ80RamRead = z80_memory_r; _nativeZ80RamWrite = z80_memory_w;
        _machineBitmap = framebuffer; _machinePitch = pitch;
        _gpu32XComposer = gpu32XComposer;
        _useDynamicCpuBlocks = useDynamicCpuBlocks;
        _useNative68000Blocks = useNative68000Blocks;
        cart = new(SystemHardware.IsSixteenBit(hardware) ? CartridgeState.MaximumRomCapacity : 0x800000);
        if (config.system == 0) config.system = hardware;
        _openFile = openFile; _loadFirmware = loadFirmware; _cancellation = cancellation;
        z80_readmap = new(_readMap, NativeZ80MappingChanged);
        _mainAudio = new(sampleRate / 10);
        _z80 = Functions.CpuConfigurationFunctions.CreateZ80(this, z80_irq_callback, _useDynamicCpuBlocks);
        _input = new(this);
        _vdp = new(hardware, this, config.Video, framebuffer, width, height, pitch);

    }

    private void InitializeSystem()
    {
        config.PadTypes.CopyTo(_input.ConfiguredPadTypes);
        InitializeMachineBus();
        Configure32X();
        _executeFrame = CreateFrameScheduler();
        _input.Configure(system_hw, Functions.InputConfigurationFunctions.Create(system_hw, romtype, rominfo.peripherals, cart.special));
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
        Unmap32XBanks();
        gen_reset(hard ? 1 : 0);
        if (_mega32X is not null) Map32XBanks();
        if (hard)
        {
            _input.ResetIo(); _vdp.Reset(GetVideoResetPreset()); _sound.Reset(); _sound.ResetOutput();
        }
        ResetMachineDevices();
    }

    public int RunFrame(Span<short> audio, bool skipVideo = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _cancellation.ThrowIfCancellationRequested();
        bool completed = false;
        try
        {
            _gpu32XComposer?.BeginFrame();
            _executeFrame(skipVideo ? 1 : 0);
            _gpu32XComposer?.FinishFrame();
            FinishMachineAudioFrame(mcycles_vdp);
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
        ConfigureMachineAudio(sampleRate, frameRate);
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
                        try { _mega32X?.Dispose(); }
                        finally
                        {
                        _mainCpuComponent?.Halt();
                        try { _mainCpuComponent?.Dispose(); }
                        finally
                        {
                        try { _z80.Dispose(); }
                        finally
                        {
                        _z80.State.irq_callback = null; _z80.State.daisy = null;
                        _z80.Cycles = uint.MaxValue; zstate = 0;
                        if (_sound is not null) { _sound.AttachCdAudio(null,null,null,null); _sound.AttachCartridgeAudio(null,null); }
                        _vdp.LineRendered = null; _vdp.ExternalOutputWidth = 0; _mega32X = null; _unmap32X.Clear(); _mapped32XPages.Clear(); _32XCodeWindows?.Clear();
                        _addresses.Clear();
                        Array.Clear(_readMap); Array.Clear(_writeMap);
                        z80_readport = z80_readmem = null!; z80_writeport = z80_writemem = null!;
                        _mainAudio.Clear(); _pcmAudioBuffer?.Clear(); _cdAudioBuffer?.Clear(); _cartridgeAudioBuffer?.Clear(); PollInput = null; _executeFrame = null!;
                        }
                        }
                        }
                    }
                }
            }
        }
    }
}

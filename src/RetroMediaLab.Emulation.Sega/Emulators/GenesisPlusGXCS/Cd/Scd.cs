using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cpu;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Sound;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal interface IScdHost
{
    ref M68kState MainState { get; }
    double MasterClock { get; }
    void RunMain(uint cycles);
    uint ReadMainBus8(uint address);
    uint ReadMainBus16(uint address);
    void WriteMainUnused8(uint address, uint data);
    void WriteMainUnused16(uint address, uint data);
    uint ReadZ80Unused(uint address);
    void WriteZ80Unused(uint address, uint data);
    void InitializeCdCartridge();
    int MdCartridgeContextSize { get; }
    int SaveMdCartridgeContext(Span<byte> state);
    int LoadMdCartridgeContext(ReadOnlySpan<byte> state);
    bool MegaSdEnabled { get; }
    void UpdateMegaSdCdda(uint samples);
}

internal sealed partial class Scd : IM68kBus, ICdcBus, ICddBus, IGfxBus, IDisposable
{
    public const uint MasterClock = 50_000_000;
    private readonly IScdHost _host;
    private readonly M68k<Scd> _cpu;
    private readonly Cdc _cdc;
    private readonly Gfx _gfx;
    private readonly Pcm _pcm;
    private readonly Cdd _cdd;
    private readonly byte[] bootrom = new byte[0x20000], prg_ram = new byte[0x80000], word_ram_2M = new byte[0x40000], bram = new byte[0x2000];
    private readonly byte[] _wordRam1M = new byte[0x40000];
    private readonly Memory<byte>[] _wordPages;
    private readonly CartridgePageMap word_ram;
    private readonly Register16[] scd_regs = new Register16[0x100];
    private readonly CartridgeMemoryBank[] memory_map, sub_memory_map;
    private readonly CartridgeZ80Bank[] zbank_memory_map;
    private uint cd_cycles, cycles_per_line;
    private int stopwatch, timer;
    private byte pending, dmna, hardware_type, cd_boot;
    private ref M68kState main => ref _host.MainState;
    private ref M68kState sub => ref _cpu.State;
    private double system_clock => _host.MasterClock;
    private ref Register16 cdc_dac => ref _cdc.DataAddress;
    private CPointer<byte> cdc_ram => new(_cdc.Ram);
    private CPointer<int> cdc_cycles => new(_cdc.CycleCounters);
    private byte cdc_ar_mask => _cdc.AddressMask;
    private Action<uint>? cdc_dma { get => _cdc.ActiveDma; set => _cdc.ActiveDma = value; }
    private Action<uint>? cdc_halted_dma { get => _cdc.HaltedDma; set => _cdc.HaltedDma = value; }
    private uint drive_cycles { get => _cdd.Cycles; set => _cdd.Cycles = value; }
    private CPointer<ushort> drive_fader => new(_cdd.Fader);
    private uint gfx_cycles { get => _gfx.Cycles; set => _gfx.Cycles = value; }

    public M68k<Scd> Cpu => _cpu;
    public Cdc DataController => _cdc;
    public Cdd Drive => _cdd;
    public Gfx Graphics => _gfx;
    public Pcm PcmChip => _pcm;
    public Register16[] Registers => scd_regs;
    internal CartridgeMemoryBank[] SubMemoryMap => sub_memory_map;
    internal Action<uint> ProgramDmaCallback => _cdc.ProgramDmaCallback;
    internal Action<uint> WordRam2MDmaCallback => _cdc.WordRam2MDmaCallback;
    private Action<uint> word_ram_2M_dma_callback => _cdc.WordRam2MDmaCallback;
    public Memory<byte> BootRom => bootrom;
    public Memory<byte> ProgramRam => prg_ram;
    public Memory<byte> BackupRam => bram;
    public Memory<byte>[] WordRam => _wordPages;
    public byte[] WordRam2M => word_ram_2M;
    public byte Boot { get => cd_boot; set => cd_boot = value; }
    public byte HardwareType { get => hardware_type; set => hardware_type = value; }
    public byte PendingInterrupts { get => pending; set => pending = value; }
    public byte PendingDmna { get => dmna; set => dmna = value; }
    public uint Cycles { get => cd_cycles; set => cd_cycles = value; }
    public uint CyclesPerLine => cycles_per_line;
    public int Stopwatch { get => stopwatch; set => stopwatch = value; }

    public Scd(IScdHost host, CartridgeAddressSpace addressSpace, BlipBuffer pcmOutput, BlipBuffer cdOutput,
        Func<string, Stream?> openFile, CancellationToken cancellation)
    {
        _host = host; memory_map = addressSpace.Main; zbank_memory_map = addressSpace.Z80;
        sub_memory_map = new CartridgeMemoryBank[256];
        for (int i = 0; i < sub_memory_map.Length; i++) sub_memory_map[i] = new();
        _wordPages = [_wordRam1M.AsMemory(0, 0x20000), _wordRam1M.AsMemory(0x20000, 0x20000)];
        word_ram = new(_wordPages);
        _cpu = new(this, subCpu: true, subCpuAddressErrors: true);
        _pcm = new(pcmOutput); _cdc = new(this); _gfx = new(this);
        _cdd = new(this, cdOutput, openFile, cancellation);
    }
    public void Initialize() => scd_init();
    public void Reset(bool hard) => scd_reset(hard ? 1 : 0);
    public void Update(uint mainCycles) => scd_update(mainCycles);
    public void EndFrame(uint cycles) => scd_end_frame(cycles);
    public void SwitchWordRam(byte mode) => word_ram_switch(mode);

    private void m68k_run(uint cycles) => _host.RunMain(cycles);
    private void s68k_run(uint cycles) => _cpu.Run(cycles);
    private void s68k_pulse_wait(uint address, uint write) => _cpu.PulseWait(address, write != 0);
    private void s68k_pulse_reset() => _cpu.Reset();
    private void s68k_pulse_halt() => _cpu.Halt();
    private void s68k_update_irq(uint mask) => _cpu.UpdateIrq(mask);
    private uint m68k_read_bus_8(uint address) => _host.ReadMainBus8(address);
    private uint m68k_read_bus_16(uint address) => _host.ReadMainBus16(address);
    private void m68k_unused_8_w(uint address, uint data) => _host.WriteMainUnused8(address, data);
    private void m68k_unused_16_w(uint address, uint data) => _host.WriteMainUnused16(address, data);
    private uint zbank_unused_r(uint address) => _host.ReadZ80Unused(address);
    private void zbank_unused_w(uint address, uint data) => _host.WriteZ80Unused(address, data);
    private void cd_cart_init() => _host.InitializeCdCartridge();
    private void cdc_init() => _cdc.Initialize();
    private void cdc_reset() => _cdc.Reset();
    private void cdc_dma_init() => _cdc.InitializeDma();
    private void cdc_dma_update(uint cycles) => _cdc.UpdateDma(cycles);
    private byte cdc_reg_r() => _cdc.ReadRegister();
    private void cdc_reg_w(byte data) => _cdc.WriteRegister(data);
    private ushort cdc_host_r(byte access) => _cdc.ReadHost(access);
    private void gfx_init() => _gfx.Initialize();
    private void gfx_reset() => _gfx.Reset();
    private void gfx_update(int cycles) => _gfx.Update(cycles);
    private void gfx_start(uint address, int cycles) => _gfx.Start(address, cycles);
    private void cdd_reset() => _cdd.Reset();
    private void cdd_update() => _cdd.Update();
    private void cdd_process() => _cdd.ProcessCommand();
    private void pcm_reset() => _pcm.Reset();
    private byte pcm_read(uint address, uint cycles) => _pcm.Read(address, cycles);
    private void pcm_write(uint address, byte data, uint cycles) => _pcm.Write(address, data, cycles);
    private void word_ram_2M_dma_w(uint length) => _gfx.DmaWordRam2M(length);
    private uint cell_ram_0_read8(uint address) => _gfx.ReadCell0Byte(address);
    private uint cell_ram_0_read16(uint address) => _gfx.ReadCell0Word(address);
    private uint cell_ram_1_read8(uint address) => _gfx.ReadCell1Byte(address);
    private uint cell_ram_1_read16(uint address) => _gfx.ReadCell1Word(address);
    private uint dot_ram_0_read8(uint address) => _gfx.ReadDot0Byte(address);
    private uint dot_ram_0_read16(uint address) => _gfx.ReadDot0Word(address);
    private uint dot_ram_1_read8(uint address) => _gfx.ReadDot1Byte(address);
    private uint dot_ram_1_read16(uint address) => _gfx.ReadDot1Word(address);
    private void cell_ram_0_write8(uint address, uint data) => _gfx.WriteCell0Byte(address, data);
    private void cell_ram_0_write16(uint address, uint data) => _gfx.WriteCell0Word(address, data);
    private void cell_ram_1_write8(uint address, uint data) => _gfx.WriteCell1Byte(address, data);
    private void cell_ram_1_write16(uint address, uint data) => _gfx.WriteCell1Word(address, data);
    private void dot_ram_0_write8(uint address, uint data) => _gfx.WriteDot0Byte(address, data);
    private void dot_ram_0_write16(uint address, uint data) => _gfx.WriteDot0Word(address, data);
    private void dot_ram_1_write8(uint address, uint data) => _gfx.WriteDot1Byte(address, data);
    private void dot_ram_1_write16(uint address, uint data) => _gfx.WriteDot1Word(address, data);
    private void ClearRegistersAll() => Array.Clear(scd_regs);
    private void ClearRegistersSub() => scd_regs.AsSpan(0x30 >> 1).Clear();
    private void ClearProgramRam() => Array.Clear(prg_ram);
    private void ClearWordRam() => Array.Clear(_wordRam1M);
    private void ClearWordRam2M() => Array.Clear(word_ram_2M);
    private void ClearBackupRam() => Array.Clear(bram);
    private void ClearMainPolling() { main.poll_pc = main.poll_cycle = main.poll_detected = 0; }
    private void ClearSubPolling() { sub.poll_pc = sub.poll_cycle = sub.poll_detected = 0; }

    uint IM68kBus.Read8(uint address)
    {
        CartridgeMemoryBank bank = sub_memory_map[(address >> 16) & 255];
        return bank.read8 is { } read ? read(address) : bank.Data.Span[(int)((address & 65535) ^ 1)];
    }
    uint IM68kBus.Read16(uint address)
    {
        CartridgeMemoryBank bank = sub_memory_map[(address >> 16) & 255];
        return bank.read16 is { } read ? read(address) : BinaryPrimitives.ReadUInt16LittleEndian(bank.Data.Span[(int)(address & 65534)..]);
    }
    void IM68kBus.Write8(uint address, uint value)
    {
        CartridgeMemoryBank bank = sub_memory_map[(address >> 16) & 255];
        if (bank.write8 is { } write) write(address, value); else bank.Data.Span[(int)((address & 65535) ^ 1)] = (byte)value;
    }
    void IM68kBus.Write16(uint address, uint value)
    {
        CartridgeMemoryBank bank = sub_memory_map[(address >> 16) & 255];
        if (bank.write16 is { } write) write(address, value); else BinaryPrimitives.WriteUInt16LittleEndian(bank.Data.Span[(int)(address & 65534)..], (ushort)value);
    }
    uint IM68kBus.ReadMapped8(uint address) => sub_memory_map[(address >> 16) & 255].Data.Span[(int)((address & 65535) ^ 1)];
    uint IM68kBus.ReadMapped16(uint address) => BinaryPrimitives.ReadUInt16LittleEndian(sub_memory_map[(address >> 16) & 255].Data.Span[(int)(address & 65534)..]);
    void IM68kBus.AcknowledgeInterrupt(uint level) => scd_68k_irq_ack((int)level);
    int IM68kBus.MainCycles => main.cycles;
    int IM68kBus.MainRefreshCycles => main.refresh_cycles;
    public int SubCycles { get => sub.cycles; set => sub.cycles = value; }
    public uint SubStopped { get => sub.stopped; set => sub.stopped = value; }
    public void UpdateSubIrq(uint mask) => _cpu.UpdateIrq(mask);
    public void DmaPcm(uint length) => _pcm.DmaWrite(length, ref _cdc.DataAddress, ref scd_regs[0x0a >> 1], _cdc.Ram.Span);
    public void DmaProgramRam(uint length) => prg_ram_dma_w(length);
    public void DmaWordRam0(uint length) => _gfx.DmaWordRam0(length);
    public void DmaWordRam1(uint length) => _gfx.DmaWordRam1(length);
    public void DmaWordRam2M(uint length) => _gfx.DmaWordRam2M(length);
    public Memory<byte> CdcRam => _cdc.Ram;
    public ref Register16 CdcDataAddress => ref _cdc.DataAddress;
    public void ReadCdData(Span<byte> destination, Span<byte> subheader) => _cdd.ReadData(destination, subheader);
    public void UpdateDecoder(uint header) => _cdc.UpdateDecoder(header);
    public bool MegaSdEnabled => _host.MegaSdEnabled;
    public void UpdateMegaSdCdda(uint samples) => _host.UpdateMegaSdCdda(samples);
    public void Dispose()
    {
        try { _cdd.Dispose(); }
        finally
        {
            _cpu.Halt(); sub.cycle_end = 0;
            foreach (CartridgeMemoryBank bank in sub_memory_map)
            { bank.Data = default; bank.read8 = bank.read16 = null; bank.write8 = bank.write16 = null; }
            foreach (CartridgeMemoryBank bank in memory_map)
            {
                if (MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)bank.Data, out ArraySegment<byte> segment) &&
                    (ReferenceEquals(segment.Array, bootrom) || ReferenceEquals(segment.Array, prg_ram) ||
                    ReferenceEquals(segment.Array, _wordRam1M) || ReferenceEquals(segment.Array, word_ram_2M))) bank.Data = default;
                if (ReferenceEquals(bank.read8?.Target, this)) bank.read8 = null;
                if (ReferenceEquals(bank.read16?.Target, this)) bank.read16 = null;
                if (ReferenceEquals(bank.write8?.Target, this)) bank.write8 = null;
                if (ReferenceEquals(bank.write16?.Target, this)) bank.write16 = null;
            }
            foreach (CartridgeZ80Bank bank in zbank_memory_map)
            {
                if (ReferenceEquals(bank.read?.Target, this)) bank.read = null;
                if (ReferenceEquals(bank.write?.Target, this)) bank.write = null;
            }
        }
    }
}

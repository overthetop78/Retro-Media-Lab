using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cpu;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Input;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS;

internal sealed partial class SystemCore
{
    private ref M68kState main => ref _mainCpu.State;
    private ref M68kState sub => ref _cd.Cpu.State;
    private ref Z80State z80 => ref _z80.State;
    private CartridgeMemoryBank[] memory_map => _addresses.Main;
    private CartridgeMemoryBank[] sub_memory_map => _cd.SubMemoryMap;
    private CartridgeZ80Bank[] zbank_memory_map => _addresses.Z80;
    private Register16[] scd_regs => _cd.Registers;
    private CPointer<byte> cd_program_ram => new(_cd.ProgramRam);
    private CPointer<int> cdc_cycles => new(_cd.DataController.CycleCounters);
    private CPointer<byte> io_reg => new(_input.IoRegisters);
    private CPointer<ushort> input_pad => new(_input.DigitalInputs);
    private CPointer<short> input_analog => new(_input.AnalogInputs);
    private Action<uint>? cdc_dma { get => _cd.DataController.ActiveDma; set => _cd.DataController.ActiveDma = value; }
    private Action<uint>? cdc_halted_dma { get => _cd.DataController.HaltedDma; set => _cd.DataController.HaltedDma = value; }
    private Action<uint> prg_ram_dma_w => _cd.ProgramDmaCallback;
    private Action<uint> word_ram_2M_dma_w => _cd.WordRam2MDmaCallback;
    private byte cd_boot { get => _cd.Boot; set { _cd.Boot = value; _cdCartridge.Boot = value; } }
    private byte cd_pending { get => _cd.PendingInterrupts; set => _cd.PendingInterrupts = value; }
    private byte cd_dmna { get => _cd.PendingDmna; set => _cd.PendingDmna = value; }
    private int cd_stopwatch { get => _cd.Stopwatch; set => _cd.Stopwatch = value; }
    private uint cd_cycles { get => _cd.Cycles; set => _cd.Cycles = value; }
    private uint cd_cycles_per_line => _cd.CyclesPerLine;
    private uint gfx_cycles { get => _cd.Graphics.Cycles; set => _cd.Graphics.Cycles = value; }
    private byte ym2413 => unchecked((byte)config.Sound.Ym2413Mode);
    private uint psg_preamp => config.Sound.PsgPreamp;
    private int HasSvp => _md.Svp is null ? 0 : 1;
    private byte[] reg => _vdp.reg;
    private uint[] fifo_cycles => _vdp.fifo_cycles;
    private byte dma_type { get => _vdp.dma_type; set => _vdp.dma_type = value; }
    private uint dma_length { get => _vdp.dma_length; set => _vdp.dma_length = value; }
    private uint dma_endCycles { get => _vdp.dma_endCycles; set => _vdp.dma_endCycles = value; }
    private uint vint_cycle => _vdp.vint_cycle;
    private byte vdp_pal { get => _vdp.vdp_pal; set => _vdp.vdp_pal = value; }
    private byte hint_pending { get => _vdp.hint_pending; set => _vdp.hint_pending = value; }
    private byte vint_pending { get => _vdp.vint_pending; set => _vdp.vint_pending = value; }
    private byte h_counter { get => _vdp.h_counter; set => _vdp.h_counter = value; }
    private byte interlaced { get => _vdp.interlaced; set => _vdp.interlaced = value; }
    private byte im2_flag { get => _vdp.im2_flag; set => _vdp.im2_flag = value; }
    private byte odd_frame { get => _vdp.odd_frame; set => _vdp.odd_frame = value; }
    private ushort status { get => _vdp.status; set => _vdp.status = value; }
    private ushort v_counter { get => _vdp.v_counter; set => _vdp.v_counter = value; }
    private ushort vscroll { get => _vdp.vscroll; set => _vdp.vscroll = value; }
    private ushort lines_per_frame => _vdp.lines_per_frame;
    private int viewport_w { get => _vdp.ViewportWidth; set => _vdp.ViewportWidth = value; }
    private int viewport_h { get => _vdp.ViewportHeight; set => _vdp.ViewportHeight = value; }
    private int viewport_x { get => _vdp.ViewportX; set => _vdp.ViewportX = value; }
    private int viewport_y { get => _vdp.ViewportY; set => _vdp.ViewportY = value; }
    private int viewport_ow { get => _vdp.PreviousViewportWidth; set => _vdp.PreviousViewportWidth = value; }
    private int viewport_oh { get => _vdp.PreviousViewportHeight; set => _vdp.PreviousViewportHeight = value; }
    private int viewport_changed { get => _vdp.ViewportChanged; set => _vdp.ViewportChanged = value; }
    private int overscan => config.Video.overscan;
    private int gg_extra => config.Video.gg_extra;

    private void m68k_init() => _mainCpu = new(this);
    private void s68k_init() => _cd.Cpu.State = default;
    private void z80_init(int index, Func<int, int> callback) => _z80 = new(this, callback);
    private void m68k_run(uint cycles) => _mainCpu.Run(cycles);
    private void m68k_pulse_reset() => _mainCpu.Reset();
    private void m68k_pulse_halt() => _mainCpu.Halt();
    private uint m68k_cycles() => unchecked((uint)_mainCpu.InstructionCycles);
    private void m68k_update_irq(uint mask) => _mainCpu.UpdateIrq(mask);
    private void m68k_set_irq(uint level) => _mainCpu.SetIrq(level);
    private void s68k_run(uint cycles) => _cd.Cpu.Run(cycles);
    private void s68k_pulse_reset() => _cd.Cpu.Reset();
    private void s68k_pulse_halt() => _cd.Cpu.Halt();
    private void s68k_clear_halt() => _cd.Cpu.ClearHalt();
    private void s68k_clear_wait() => _cd.Cpu.ClearWait();
    private void s68k_update_irq(uint mask) => _cd.UpdateSubIrq(mask);
    private void z80_run(uint cycles) => _z80.Run(cycles);
    private void z80_reset() => _z80.Reset();
    private void z80_set_nmi_line(uint state) => _z80.SetNmiLine(state);
    private void md_cart_init() => _md.Initialize();
    private void md_cart_reset(int hard) => _md.Reset(hard != 0);
    private void sms_cart_init()
    {
        _sms.Initialize(); z80_readmem = _sms.ReadMemory; z80_writemem = _sms.WriteMemory;
    }
    private void sms_cart_reset() => _sms.Reset();
    private void scd_init() => _cd.Initialize();
    private void scd_reset(int hard) => _cd.Reset(hard != 0);
    private void scd_update(uint cycles) => _cd.Update(cycles);
    private void scd_end_frame(uint cycles) => _cd.EndFrame(cycles);
    private void cdc_dma_update(uint cycles) => _cd.DataController.UpdateDma(cycles);
    private ushort cdc_host_r(byte cpu) => _cd.DataController.ReadHost(cpu);
    private uint ReadSvpExchange() => _md.Svp!.HostReadExchange();
    private uint ReadSvpStatus() => _md.Svp!.HostReadStatus();
    private void WriteSvpExchange(uint data) => _md.Svp!.HostWriteExchange(data);
    private void ssp1601_run(int cycles) => _md.Svp!.Run(cycles);
    private void fm_reset(uint cycles) => _sound.ResetFm(cycles);
    private uint fm_read(uint cycles, uint address) => _sound.ReadFm(cycles, address);
    private void fm_write(uint cycles, uint address, uint data) => _sound.WriteFm(cycles, address, data);
    private void psg_write(uint cycles, uint data) => _sound.Psg.Write(cycles, data);
    private void psg_config(uint cycles, uint preamp, uint pan) => _sound.Psg.Configure(cycles, preamp, pan);
    private uint io_68k_read(uint address) => _input.Read68K(address);
    private void io_68k_write(uint address, uint data) => _input.Write68K(address, data);
    private uint io_z80_read(uint address) => _input.ReadZ80(address);
    private void io_z80_write(uint address, uint data, uint cycles) => _input.WriteZ80(address, data, cycles);
    private uint io_gg_read(uint address) => _input.ReadGameGear(address);
    private void io_gg_write(uint address, uint data) => _input.WriteGameGear(address, data);
    private void input_refresh() => _input.Refresh();
    private void input_end_frame(uint cycles) => _input.EndFrame(cycles);
    private void osd_input_update() => PollInput?.Invoke();
    private uint vdp_68k_data_r() => _vdp.ReadMainData();
    private uint vdp_68k_ctrl_r(uint cycles) => _vdp.ReadMainControl(cycles);
    private uint vdp_z80_data_r() => _vdp.ReadZ80Data();
    private uint vdp_z80_ctrl_r(uint cycles) => _vdp.ReadZ80Control(cycles);
    private uint vdp_hvc_r(uint cycles) => _vdp.ReadCounters(cycles);
    private void vdp_68k_data_w(uint data) => _vdp.WriteMainData(data);
    private void vdp_68k_ctrl_w(uint data) => _vdp.WriteMainControl(data);
    private void vdp_z80_data_w(uint data) => _vdp.WriteZ80Data(data);
    private void vdp_z80_ctrl_w(uint data) => _vdp.WriteZ80Control(data);
    private void vdp_sms_ctrl_w(uint data) => _vdp.WriteSmsControl(data);
    private void vdp_tms_ctrl_w(uint data) => _vdp.WriteTmsControl(data);
    private void vdp_test_w(uint data) => _vdp.WriteTest(data);
    private void vdp_dma_update(uint cycles) => _vdp.UpdateDma(cycles);
    private void blank_line(int line, int offset, int width) => _vdp.BlankLine(line, offset, width);
    private void render_line(int line) => _vdp.RenderLine(line);
    private void render_obj(int line) => _vdp.RenderObjects(line);
    private void parse_satb(int line) => _vdp.ParseSprites(line);
    private void SelectMode5Renderers() => _vdp.SelectMode5Renderers();
    private static void FillMemory(CPointer<byte> destination, int data, int length) => destination.Bytes(length).Fill((byte)data);
    private int TmssMatches() => tmss.AsSpan().SequenceEqual("SEGA"u8) ? 0 : 1;
    private static int rand() => Random.Shared.Next(int.MaxValue);

    Memory<byte>[] IZ80Bus.ReadMap => _readMap;
    byte IZ80Bus.ReadMemory(int address) => z80_readmem((uint)address);
    void IZ80Bus.WriteMemory(int address, byte value) => z80_writemem((uint)address, value);
    byte IZ80Bus.ReadPort(int port) => z80_readport((uint)port);
    void IZ80Bus.WritePort(int port, byte value) => z80_writeport((uint)port, value);
    uint IM68kBus.Read8(uint address)
    {
        CartridgeMemoryBank bank = memory_map[(address >> 16) & 255];
        return bank.read8 is { } read ? read(address) : bank.Data.Span[(int)((address & 65535) ^ 1)];
    }
    uint IM68kBus.Read16(uint address)
    {
        CartridgeMemoryBank bank = memory_map[(address >> 16) & 255];
        return bank.read16 is { } read ? read(address) : BinaryPrimitives.ReadUInt16LittleEndian(bank.Data.Span[(int)(address & 65534)..]);
    }
    void IM68kBus.Write8(uint address, uint value)
    {
        CartridgeMemoryBank bank = memory_map[(address >> 16) & 255];
        if (bank.write8 is { } write) write(address, value); else bank.Data.Span[(int)((address & 65535) ^ 1)] = (byte)value;
    }
    void IM68kBus.Write16(uint address, uint value)
    {
        CartridgeMemoryBank bank = memory_map[(address >> 16) & 255];
        if (bank.write16 is { } write) write(address, value);
        else BinaryPrimitives.WriteUInt16LittleEndian(bank.Data.Span[(int)(address & 65534)..], (ushort)value);
    }
    uint IM68kBus.ReadMapped8(uint address) => memory_map[(address >> 16) & 255].Data.Span[(int)((address & 65535) ^ 1)];
    uint IM68kBus.ReadMapped16(uint address) => BinaryPrimitives.ReadUInt16LittleEndian(memory_map[(address >> 16) & 255].Data.Span[(int)(address & 65534)..]);
    void IM68kBus.AcknowledgeInterrupt(uint level) => _vdp.AcknowledgeInterrupt((int)level);
    int IM68kBus.MainCycles => main.cycles;
    int IM68kBus.MainRefreshCycles => main.refresh_cycles;

    int IVdpBus.MainCycles { get => main.cycles; set => main.cycles = value; }
    int IVdpBus.MainInstructionCycles => _mainCpu.InstructionCycles;
    uint IVdpBus.Z80Cycles { get => z80.cycles; set => z80.cycles = value; }
    byte IVdpBus.Z80IrqState { get => (byte)z80.irq_state; set => z80.irq_state = value; }
    byte IVdpBus.Z80BusState { get => zstate; set => zstate = value; }
    uint IVdpBus.VideoCycles => mcycles_vdp;
    byte IVdpBus.SystemBios => system_bios;
    uint IVdpBus.CdBootBank => cd_boot;
    uint IVdpBus.CartridgeSpecial => cart.special;
    bool IVdpBus.HasSvp => _md.Svp is not null;
    byte[] IVdpBus.WorkRam => work_ram;
    void IVdpBus.SetMainIrq(uint level) => _mainCpu.SetIrq(level);
    void IVdpBus.SetMainIrqDelay(uint level) => _mainCpu.SetIrqDelay(level);
    void IVdpBus.SetZ80Irq(uint level) => _z80.SetIrqLine(level);
    uint IVdpBus.ReadIo(uint address) => _input.Read68K(address);
    uint IVdpBus.ReadDmaWord(uint address) => BinaryPrimitives.ReadUInt16LittleEndian(memory_map[(address >> 16) & 255].Data.Span[(int)(address & 65534)..]);
    uint IInputBus.MainCycles => unchecked((uint)main.cycles);
    uint IInputBus.Z80Cycles => z80.cycles;
    byte IInputBus.Region => region_code;
    int IInputBus.ViewportWidth => viewport_w;
    int IInputBus.ViewportHeight => viewport_h;
    ushort IInputBus.VerticalCounter => v_counter;
    uint IInputBus.HvcLatch { get => _vdp.hvc_latch; set => _vdp.hvc_latch = value; }
    byte IInputBus.BiosConfiguration => config.bios;
    uint IInputBus.PsgPreamp => psg_preamp;
    bool IInputBus.BootRomMapped => new CPointer<byte>(_readMap[0]).Same(new CPointer<byte>(cart.rom, 0x400000));
    void IInputBus.ConfigurePsg(uint cycles, uint preamp, uint panning) => psg_config(cycles, preamp, panning);
    void IInputBus.SwitchSmsCartridge(byte mode) => _sms.Switch(mode);
    ReadOnlyMemory<byte> IInputBus.VdpRegisters => reg;
    ReadOnlyMemory<byte> IInputBus.HorizontalCounterTable => _vdp.HorizontalCounterTable;
    void IInputBus.UpdateMainIrq(uint level) => m68k_update_irq(level);
}

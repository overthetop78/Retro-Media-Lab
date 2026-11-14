using RetroMediaLab.Chips.Common.Memory;

using System.Buffers.Binary;



namespace RetroMediaLab.Chips.Controllers.Sega.CD;

public interface IGfxBus
{
    Register16[] Registers { get; }
    Memory<byte>[] WordRam { get; }
    byte[] WordRam2M { get; }
    ref Register16 CdcDataAddress { get; }
    Memory<byte> CdcRam { get; }
    int SubCycles { get; set; }
    uint SubStopped { get; set; }
    byte PendingInterrupts { get; set; }
    void UpdateSubIrq(uint mask);
}

public sealed partial class Gfx
{
    public const int ContextSize = 28;
    private readonly IGfxBus _bus;
    private readonly gfx_t gfx;
    private readonly BytePageMap word_ram;
    private readonly byte[] word_ram_2M;
    private Register16[] scd_regs => _bus.Registers;
    private ref Register16 cdc_dac => ref _bus.CdcDataAddress;
    private ArrayPointer<byte> cdc_ram => new(_bus.CdcRam);
    private int sub_cycles { get => _bus.SubCycles; set => _bus.SubCycles = value; }
    private uint sub_stopped { get => _bus.SubStopped; set => _bus.SubStopped = value; }
    private byte pending { get => _bus.PendingInterrupts; set => _bus.PendingInterrupts = value; }
    public uint Cycles { get => gfx.cycles; set => gfx.cycles = value; }

    public Gfx(IGfxBus bus)
    {
        _bus = bus;
        word_ram = new(bus.WordRam);
        word_ram_2M = bus.WordRam2M;
        gfx = new(word_ram_2M);
    }
    private void s68k_update_irq(uint mask) => _bus.UpdateSubIrq(mask);
    public void Initialize() => gfx_init();
    public void Reset() => gfx_reset();
    public void Start(uint address, int cycles) => gfx_start(address, cycles);
    public void Update(int cycles) => gfx_update(cycles);
    public void DmaWordRam0(uint length) => word_ram_0_dma_w(length);
    public void DmaWordRam1(uint length) => word_ram_1_dma_w(length);
    public void DmaWordRam2M(uint length) => word_ram_2M_dma_w(length);
    public uint ReadDot0Byte(uint address) => dot_ram_0_read8(address);
    public uint ReadDot0Word(uint address) => dot_ram_0_read16(address);
    public uint ReadDot1Byte(uint address) => dot_ram_1_read8(address);
    public uint ReadDot1Word(uint address) => dot_ram_1_read16(address);
    public void WriteDot0Byte(uint address, uint data) => dot_ram_0_write8(address, data);
    public void WriteDot0Word(uint address, uint data) => dot_ram_0_write16(address, data);
    public void WriteDot1Byte(uint address, uint data) => dot_ram_1_write8(address, data);
    public void WriteDot1Word(uint address, uint data) => dot_ram_1_write16(address, data);
    public uint ReadCell0Byte(uint address) => cell_ram_0_read8(address);
    public uint ReadCell0Word(uint address) => cell_ram_0_read16(address);
    public uint ReadCell1Byte(uint address) => cell_ram_1_read8(address);
    public uint ReadCell1Word(uint address) => cell_ram_1_read16(address);
    public void WriteCell0Byte(uint address, uint data) => cell_ram_0_write8(address, data);
    public void WriteCell0Word(uint address, uint data) => cell_ram_0_write16(address, data);
    public void WriteCell1Byte(uint address, uint data) => cell_ram_1_write8(address, data);
    public void WriteCell1Word(uint address, uint data) => cell_ram_1_write16(address, data);

    public int SaveContext(Span<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("GFX context buffer is too short.", nameof(state));
        BinaryPrimitives.WriteUInt32LittleEndian(state, gfx.cycles);
        BinaryPrimitives.WriteUInt32LittleEndian(state[4..], gfx.cyclesPerLine);
        BinaryPrimitives.WriteUInt32LittleEndian(state[8..], gfx.dotMask);
        state[12] = gfx.stampShift; state[13] = gfx.mapShift;
        BinaryPrimitives.WriteUInt16LittleEndian(state[14..], gfx.bufferOffset);
        BinaryPrimitives.WriteUInt32LittleEndian(state[16..], gfx.bufferStart);
        BinaryPrimitives.WriteUInt32LittleEndian(state[20..], unchecked((uint)(gfx.tracePtrOffset * 2)));
        BinaryPrimitives.WriteUInt32LittleEndian(state[24..], unchecked((uint)(gfx.mapPtrOffset * 2)));
        return ContextSize;
    }
    public int LoadContext(ReadOnlySpan<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("GFX context buffer is too short.", nameof(state));
        gfx.cycles = BinaryPrimitives.ReadUInt32LittleEndian(state);
        gfx.cyclesPerLine = BinaryPrimitives.ReadUInt32LittleEndian(state[4..]);
        gfx.dotMask = BinaryPrimitives.ReadUInt32LittleEndian(state[8..]);
        gfx.stampShift = state[12]; gfx.mapShift = state[13];
        gfx.bufferOffset = BinaryPrimitives.ReadUInt16LittleEndian(state[14..]);
        gfx.bufferStart = BinaryPrimitives.ReadUInt32LittleEndian(state[16..]);
        gfx.tracePtrOffset = (int)(BinaryPrimitives.ReadUInt32LittleEndian(state[20..]) & 0x3fff8) / 2;
        uint map = BinaryPrimitives.ReadUInt32LittleEndian(state[24..]);
        map &= unchecked((uint)~((1 << (2 * gfx.mapShift + 1)) - 1)) & 0x3ffff;
        gfx.mapPtrOffset = (int)map / 2;
        return ContextSize;
    }
}

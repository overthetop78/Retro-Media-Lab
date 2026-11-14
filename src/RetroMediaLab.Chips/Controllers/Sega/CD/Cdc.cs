using RetroMediaLab.Chips.Common.Memory;

using System.Buffers.Binary;


namespace RetroMediaLab.Chips.Controllers.Sega.CD;

public interface ICdcBus
{
    Register16[] Registers { get; }
    byte HardwareType { get; }
    byte PendingInterrupts { get; set; }
    int SubCycles { get; set; }
    uint SubStopped { get; set; }
    void UpdateSubIrq(uint mask);
    void DmaPcm(uint length);
    void DmaProgramRam(uint length);
    void DmaWordRam0(uint length);
    void DmaWordRam1(uint length);
    void DmaWordRam2M(uint length);
    void ReadCdData(Span<byte> destination, Span<byte> subheader);
}

public sealed partial class Cdc
{
    public const int ContextSize = 2 + 8 + 2 + 8 + 4 + 8 + 0x4000 + 2352 + 1;
    private readonly ICdcBus _bus;
    private readonly Action<uint> pcm_ram_dma_w, prg_ram_dma_w, word_ram_0_dma_w, word_ram_1_dma_w, word_ram_2M_dma_w;
    private Register16[] scd_regs => _bus.Registers;
    private byte hardware_type => _bus.HardwareType;
    private byte pending { get => _bus.PendingInterrupts; set => _bus.PendingInterrupts = value; }
    private int sub_cycles { get => _bus.SubCycles; set => _bus.SubCycles = value; }
    private uint sub_stopped { get => _bus.SubStopped; set => _bus.SubStopped = value; }
    public ref Register16 DataAddress => ref cdc.dac;
    public byte AddressMask => cdc.ar_mask;
    public Memory<byte> Ram => cdc.ram;
    public Span<int> CycleCounters => cdc.cycles;
    public Action<uint>? ActiveDma { get => cdc.dma_w; set => cdc.dma_w = value; }
    public Action<uint>? HaltedDma { get => cdc.halted_dma_w; set => cdc.halted_dma_w = value; }
    public Action<uint> ProgramDmaCallback => prg_ram_dma_w;
    public Action<uint> WordRam2MDmaCallback => word_ram_2M_dma_w;

    public Cdc(ICdcBus bus)
    {
        _bus = bus;
        pcm_ram_dma_w = bus.DmaPcm; prg_ram_dma_w = bus.DmaProgramRam;
        word_ram_0_dma_w = bus.DmaWordRam0; word_ram_1_dma_w = bus.DmaWordRam1; word_ram_2M_dma_w = bus.DmaWordRam2M;
    }
    public void Initialize() => cdc_init();
    public void Reset() => cdc_reset();
    public void InitializeDma() => cdc_dma_init();
    public void UpdateDma(uint cycles) => cdc_dma_update(cycles);
    public void UpdateDecoder(uint header) => cdc_decoder_update(header);
    public void WriteRegister(byte data) => cdc_reg_w(data);
    public byte ReadRegister() => cdc_reg_r();
    public ushort ReadHost(byte cpuAccess) => cdc_host_r(cpuAccess);
    private void s68k_update_irq(uint mask) => _bus.UpdateSubIrq(mask);
    private static void CopyMemory(ArrayPointer<byte> destination, ArrayPointer<byte> source, int length) => source.Bytes(length).CopyTo(destination.Bytes(length));
    private void cdd_read_data(ArrayPointer<byte> destination, ArrayPointer<byte> subheader) =>
        _bus.ReadCdData(destination.Bytes(2328), subheader.IsNull ? [] : subheader.Bytes(4));

    private byte GetDmaIndex() => cdc.dma_w == pcm_ram_dma_w ? (byte)1 :
        cdc.dma_w == prg_ram_dma_w ? (byte)2 : cdc.dma_w == word_ram_0_dma_w ? (byte)3 :
        cdc.dma_w == word_ram_1_dma_w ? (byte)4 : cdc.dma_w == word_ram_2M_dma_w ? (byte)5 :
        cdc.halted_dma_w == prg_ram_dma_w ? (byte)6 : cdc.halted_dma_w == word_ram_2M_dma_w ? (byte)7 : (byte)0;

    public int SaveContext(Span<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("CDC context buffer is too short.", nameof(state));
        int offset = 0;
        state[offset++] = cdc.ifstat; state[offset++] = cdc.ifctrl;
        PutRegister(state, ref offset, cdc.dbc); PutRegister(state, ref offset, cdc.dac);
        PutRegister(state, ref offset, cdc.pt); PutRegister(state, ref offset, cdc.wa);
        cdc.ctrl.CopyTo(state[offset..]); offset += cdc.ctrl.Length;
        cdc.head.CopyTo(state[offset..]); offset += cdc.head.Length;
        cdc.stat.CopyTo(state[offset..]); offset += cdc.stat.Length;
        foreach (int cycle in cdc.cycles)
        {
            BinaryPrimitives.WriteInt32LittleEndian(state[offset..], cycle); offset += 4;
        }
        cdc.ram.CopyTo(state[offset..]); offset += cdc.ram.Length;
        state[offset++] = GetDmaIndex();
        return offset;
    }
    public int LoadContext(ReadOnlySpan<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("CDC context buffer is too short.", nameof(state));
        int offset = 0;
        cdc.ifstat = state[offset++]; cdc.ifctrl = state[offset++];
        cdc.dbc = GetRegister(state, ref offset); cdc.dac = GetRegister(state, ref offset);
        cdc.pt = GetRegister(state, ref offset); cdc.wa = GetRegister(state, ref offset);
        state.Slice(offset, cdc.ctrl.Length).CopyTo(cdc.ctrl); offset += cdc.ctrl.Length;
        state.Slice(offset, cdc.head.Length).CopyTo(cdc.head); offset += cdc.head.Length;
        state.Slice(offset, cdc.stat.Length).CopyTo(cdc.stat); offset += cdc.stat.Length;
        for (int i = 0; i < cdc.cycles.Length; i++)
        {
            cdc.cycles[i] = BinaryPrimitives.ReadInt32LittleEndian(state[offset..]); offset += 4;
        }
        state.Slice(offset, cdc.ram.Length).CopyTo(cdc.ram); offset += cdc.ram.Length;
        byte index = state[offset++];
        cdc.dma_w = index switch
        {
            1 => pcm_ram_dma_w, 2 => prg_ram_dma_w, 3 => word_ram_0_dma_w,
            4 => word_ram_1_dma_w, 5 => word_ram_2M_dma_w, _ => null
        };
        cdc.halted_dma_w = index switch { 6 => prg_ram_dma_w, 7 => word_ram_2M_dma_w, _ => null };
        cdc.irq = unchecked((byte)(~cdc.ifstat & cdc.ifctrl & 0x60));
        return offset;
    }
    private static void PutRegister(Span<byte> state, ref int offset, Register16 register)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(state[offset..], register.Word); offset += 2;
    }
    private static Register16 GetRegister(ReadOnlySpan<byte> state, ref int offset)
    {
        ushort value = BinaryPrimitives.ReadUInt16LittleEndian(state[offset..]); offset += 2; return value;
    }
}

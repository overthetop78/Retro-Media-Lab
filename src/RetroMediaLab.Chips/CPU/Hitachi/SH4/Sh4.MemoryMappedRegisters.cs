using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed partial class Sh4AddressSpace
{
    private static readonly byte[] OnChipRam = new byte[0x2000];

    private static readonly SortedDictionary<uint, string> RegisterNames = new()
    {
        [0x1F000000] = "CCN_PTEH",
        [0x1F000004] = "CCN_PTEL",
        [0x1F000008] = "CCN_TTB",
        [0x1F00000C] = "CCN_TEA",
        [0x1F000010] = "CCN_MMUCR",
        [0x1F00001C] = "CCN_CCR",
        [0x1F000020] = "CCN_TRA",
        [0x1F000024] = "CCN_EXPEVT",
        [0x1F000028] = "CCN_INTEVT",
        [0x1F000030] = "CPU_VERSION",
        [0x1F000034] = "CCN_PTEA",
        [0x1F000038] = "CCN_QACR0",
        [0x1F00003C] = "CCN_QACR1",
        [0x1F000044] = "CCN_PRR",
        [0x1F200000] = "UBC_BARA",
        [0x1F200004] = "UBC_BAMRA",
        [0x1F200008] = "UBC_BBRA",
        [0x1F20000C] = "UBC_BARB",
        [0x1F200010] = "UBC_BAMRB",
        [0x1F200014] = "UBC_BBRB",
        [0x1F200018] = "UBC_BDRB",
        [0x1F20001C] = "UBC_BDMRB",
        [0x1F200020] = "UBC_BRCR",
        [0x1F800000] = "BSC_BCR1",
        [0x1F800004] = "BSC_BCR2",
        [0x1F800008] = "BSC_WCR1",
        [0x1F80000C] = "BSC_WCR2",
        [0x1F800010] = "BSC_WCR3",
        [0x1F800014] = "BSC_MCR",
        [0x1F800018] = "BSC_PCR",
        [0x1F80001C] = "BSC_RTCSR",
        [0x1F800020] = "BSC_RTCNT",
        [0x1F800024] = "BSC_RTCOR",
        [0x1F800028] = "BSC_RFCR",
        [0x1F80002C] = "BSC_PCTRA",
        [0x1F800030] = "BSC_PDTRA",
        [0x1F800040] = "BSC_PCTRB",
        [0x1F800044] = "BSC_PDTRB",
        [0x1F800048] = "BSC_GPIOIC",
        [0x1F900000] = "BSC_SDMR2",
        [0x1F940000] = "BSC_SDMR3",
        [0x1FA00000] = "DMAC_SAR0",
        [0x1FA00004] = "DMAC_DAR0",
        [0x1FA00008] = "DMAC_DMATCR0",
        [0x1FA0000C] = "DMAC_CHCR0",
        [0x1FA00010] = "DMAC_SAR1",
        [0x1FA00014] = "DMAC_DAR1",
        [0x1FA00018] = "DMAC_DMATCR1",
        [0x1FA0001C] = "DMAC_CHCR1",
        [0x1FA00020] = "DMAC_SAR2",
        [0x1FA00024] = "DMAC_DAR2",
        [0x1FA00028] = "DMAC_DMATCR2",
        [0x1FA0002C] = "DMAC_CHCR2",
        [0x1FA00030] = "DMAC_SAR3",
        [0x1FA00034] = "DMAC_DAR3",
        [0x1FA00038] = "DMAC_DMATCR3",
        [0x1FA0003C] = "DMAC_CHCR3",
        [0x1FA00040] = "DMAC_DMAOR",
        [0x1FC00000] = "CPG_FRQCR",
        [0x1FC00004] = "CPG_STBCR",
        [0x1FC00008] = "CPG_WTCNT",
        [0x1FC0000C] = "CPG_WTCSR",
        [0x1FC00010] = "CPG_STBCR2",
        [0x1FC80000] = "RTC_R64CNT",
        [0x1FC80004] = "RTC_RSECCNT",
        [0x1FC80008] = "RTC_RMINCNT",
        [0x1FC8000C] = "RTC_RHRCNT",
        [0x1FC80010] = "RTC_RWKCNT",
        [0x1FC80014] = "RTC_RDAYCNT",
        [0x1FC80018] = "RTC_RMONCNT",
        [0x1FC8001C] = "RTC_RYRCNT",
        [0x1FC80020] = "RTC_RSECAR",
        [0x1FC80024] = "RTC_RMINAR",
        [0x1FC80028] = "RTC_RHRAR",
        [0x1FC8002C] = "RTC_RWKAR",
        [0x1FC80030] = "RTC_RDAYAR",
        [0x1FC80034] = "RTC_RMONAR",
        [0x1FC80038] = "RTC_RCR1",
        [0x1FC8003C] = "RTC_RCR2",
        [0x1FD00000] = "INTC_ICR",
        [0x1FD00004] = "INTC_IPRA",
        [0x1FD00008] = "INTC_IPRB",
        [0x1FD0000C] = "INTC_IPRC",
        [0x1FD00010] = "INTC_IPRD",
        [0x1FD80000] = "TMU_TOCR",
        [0x1FD80004] = "TMU_TSTR",
        [0x1FD80008] = "TMU_TCOR0",
        [0x1FD8000C] = "TMU_TCNT0",
        [0x1FD80010] = "TMU_TCR0",
        [0x1FD80014] = "TMU_TCOR1",
        [0x1FD80018] = "TMU_TCNT1",
        [0x1FD8001C] = "TMU_TCR1",
        [0x1FD80020] = "TMU_TCOR2",
        [0x1FD80024] = "TMU_TCNT2",
        [0x1FD80028] = "TMU_TCR2",
        [0x1FD8002C] = "TMU_TCPR2",
        [0x1FE00000] = "SCI_SCSMR1",
        [0x1FE00004] = "SCI_SCBRR1",
        [0x1FE00008] = "SCI_SCSCR1",
        [0x1FE0000C] = "SCI_SCTDR1",
        [0x1FE00010] = "SCI_SCSSR1",
        [0x1FE00014] = "SCI_SCRDR1",
        [0x1FE00018] = "SCI_SCSCMR1",
        [0x1FE0001C] = "SCI_SCSPTR1",
        [0x1FE80000] = "SCIF_SCSMR2",
        [0x1FE80004] = "SCIF_SCBRR2",
        [0x1FE80008] = "SCIF_SCSCR2",
        [0x1FE8000C] = "SCIF_SCFTDR2",
        [0x1FE80010] = "SCIF_SCFSR2",
        [0x1FE80014] = "SCIF_SCFRDR2",
        [0x1FE80018] = "SCIF_SCFCR2",
        [0x1FE8001C] = "SCIF_SCFDR2",
        [0x1FE80020] = "SCIF_SCSPTR2",
        [0x1FE80024] = "SCIF_SCLSR2",
        [0x1FF00000] = "UDI_SDIR",
        [0x1FF00008] = "UDI_SDDR",
    };

    private static string RegisterName(uint address)
    {
        if (RegisterNames.TryGetValue(address & 0x1FFFFFFF, out string? name))
            return name;

        return $"?{address:x8}";
    }

    private T ReadP4<T>(uint address) where T : unmanaged, IBinaryInteger<T>
    {
        int size = Unsafe.SizeOf<T>();
        switch ((address >> 24) & 0xFF)
        {
            case 0xE0:
            case 0xE1:
            case 0xE2:
            case 0xE3:
                Trace.TraceInformation($"Unhandled p4 read [Store queue] 0x{address:x}");
                return T.Zero;

            case 0xF0:
                Debug.WriteLine($"IC Address read {address:x8}");
                if (size == 4)
                    return T.CreateTruncating(InstructionCache.ReadAddressArray(address));
                return T.Zero;

            case 0xF1:
                Debug.WriteLine($"IC Data read {address:x8}");
                if (size == 4)
                    return T.CreateTruncating(InstructionCache.ReadDataArray(address));
                return T.Zero;

            case 0xF2:
            {
                uint index = (address >> 8) & 3;
                return T.CreateTruncating(Mmu.InstructionTlb[index].Address | (Mmu.InstructionTlb[index].Data & 0x100));
            }

            case 0xF3:
            {
                uint index = (address >> 8) & 3;
                return T.CreateTruncating(Mmu.InstructionTlb[index].Data);
            }

            case 0xF4:
                Debug.WriteLine($"OC Address read {address:x8}");
                if (size == 4)
                    return T.CreateTruncating(OperandCache.ReadAddressArray(address));
                return T.Zero;

            case 0xF5:
                Debug.WriteLine($"OC Data read {address:x8}");
                if (size == 4)
                    return T.CreateTruncating(OperandCache.ReadDataArray(address));
                return T.Zero;

            case 0xF6:
            {
                uint index = (address >> 8) & 63;
                uint value = Mmu.UnifiedTlb[index].Address;
                value |= (Mmu.UnifiedTlb[index].Data & 4) << 7;
                value |= Mmu.UnifiedTlb[index].Data & 0x100;
                return T.CreateTruncating(value);
            }

            case 0xF7:
            {
                uint index = (address >> 8) & 63;
                return T.CreateTruncating(Mmu.UnifiedTlb[index].Data);
            }

            case 0xFF:
                Trace.TraceInformation($"Unhandled p4 read [area7] 0x{address:x}");
                break;

            default:
                Trace.TraceInformation($"Unhandled p4 read [Reserved] 0x{address:x}");
                break;
        }

        return T.Zero;
    }

    private void WriteP4<T>(uint address, T data) where T : unmanaged, IBinaryInteger<T>
    {
        int size = Unsafe.SizeOf<T>();
        uint value = uint.CreateTruncating(data);
        switch ((address >> 24) & 0xFF)
        {
            case 0xE0:
            case 0xE1:
            case 0xE2:
            case 0xE3:
                Trace.TraceInformation($"Unhandled p4 Write [Store queue] 0x{address:x}");
                break;

            case 0xF0:
                Debug.WriteLine($"IC Address write {address:x8} = {value:x}");
                if (size == 4)
                    InstructionCache.WriteAddressArray(address, value);
                return;

            case 0xF1:
                Debug.WriteLine($"IC Data write {address:x8} = {value:x}");
                if (size == 4)
                    InstructionCache.WriteDataArray(address, value);
                return;

            case 0xF2:
            {
                int index = (int)((address >> 8) & 3);
                ref Sh4TlbEntry entry = ref Mmu.InstructionTlb[index];
                entry.Address = value & 0xFFFFFCFF;
                entry.Data = (entry.Data & ~0x100u) | (value & 0x100u);
                Mmu.SynchronizeInstruction(index);
                return;
            }

            case 0xF3:
            {
                int index = (int)((address >> 8) & 3);
                ref Sh4TlbEntry entry = ref Mmu.InstructionTlb[index];
                if ((address & 0x800000) != 0)
                    entry.Assistance = value & 0xFu;
                else
                    entry.Data = value;
                Mmu.SynchronizeInstruction(index);
                return;
            }

            case 0xF4:
                if (size == 4)
                    OperandCache.WriteAddressArray(address, value);
                return;

            case 0xF5:
                Debug.WriteLine($"OC Data write {address:x8} = {value:x}");
                if (size == 4)
                    OperandCache.WriteDataArray(address, value);
                return;

            case 0xF6:
                if ((address & 0x80) != 0)
                {
                    uint virtualAddress = value & 0xFFFFFC00;
                    for (int index = 0; index < Mmu.UnifiedTlb.Length; index++)
                    {
                        ref Sh4TlbEntry entry = ref Mmu.UnifiedTlb[index];
                        if (Mmu.Match(virtualAddress, entry))
                        {
                            entry.Data = (entry.Data & ~0x100u) | (value & 0x100u);
                            entry.Data = (entry.Data & ~4u) | ((value >> 7) & 4u);
                            Mmu.SynchronizeUnified(index);
                        }
                    }

                    for (int index = 0; index < Mmu.InstructionTlb.Length; index++)
                    {
                        ref Sh4TlbEntry entry = ref Mmu.InstructionTlb[index];
                        if (Mmu.Match(virtualAddress, entry))
                        {
                            entry.Data = (entry.Data & ~0x100u) | (value & 0x100u);
                            entry.Data = (entry.Data & ~4u) | ((value >> 7) & 4u);
                            Mmu.SynchronizeInstruction(index);
                        }
                    }
                }
                else
                {
                    int index = (int)((address >> 8) & 63);
                    ref Sh4TlbEntry entry = ref Mmu.UnifiedTlb[index];
                    entry.Address = value & 0xFFFFFCFF;
                    entry.Data = (entry.Data & ~4u) | ((value >> 7) & 4u);
                    entry.Data = (entry.Data & ~0x100u) | (value & 0x100u);
                    Mmu.SynchronizeUnified(index);
                }
                return;

            case 0xF7:
            {
                int index = (int)((address >> 8) & 63);
                ref Sh4TlbEntry entry = ref Mmu.UnifiedTlb[index];
                if ((address & 0x800000) != 0)
                    entry.Assistance = value & 0xFu;
                else
                    entry.Data = value;
                Mmu.SynchronizeUnified(index);
                return;
            }

            case 0xFF:
                Trace.TraceInformation($"Unhandled p4 Write [area7] 0x{address:x} = {value:x}");
                break;

            default:
                Trace.TraceInformation($"Unhandled p4 Write [Reserved] 0x{address:x}");
                break;
        }
    }

    private T ReadP4Register<T>(uint address) where T : unmanaged, IBinaryInteger<T>
    {
        if ((address & 0x1FFFFFFF) != Sh4MemoryMappedRegisters.TMU_TCNT0)
            Debug.WriteLine($"read {RegisterName(address)}");

        if (address == 0xFF000028)
            return T.CreateTruncating(CacheRegisters.InterruptEvent);
        if (address == 0xFFA0002C)
            return T.CreateTruncating(DmaRegisters.Channel2Control);

        address &= 0x1FFFFFFF;
        uint mapBase = address >> 16;
        int size = Unsafe.SizeOf<T>();
        switch (mapBase)
        {
            case 0x1F00:
                return T.CreateTruncating(CacheRegisters.Read(address, size));

            case 0x1F20:
                return T.CreateTruncating(UserBreakRegisters.Read(address, size));

            case 0x1F80:
                return T.CreateTruncating(BusStateRegisters.Read(address, size));

            case 0x1F90:
                Trace.TraceInformation("Read from write-only registers [dram settings 2]");
                return T.Zero;

            case 0x1F94:
                Trace.TraceInformation("Read from write-only registers [dram settings 3]");
                return T.Zero;

            case 0x1FA0:
                return T.CreateTruncating(DmaRegisters.Read(address, size));

            case 0x1FC0:
                return T.CreateTruncating(ClockRegisters.Read(address, size));

            case 0x1FC8:
                return T.CreateTruncating(RealTimeClockRegisters.Read(address, size));

            case 0x1FD0:
                return T.CreateTruncating(InterruptRegisters.Read(address, size));

            case 0x1FD8:
                return T.CreateTruncating(TimerRegisters.Read(address, size));

            case 0x1FE0:
                return T.CreateTruncating(SerialRegisters.Read(address, size));

            case 0x1FE8:
                return T.CreateTruncating(SerialFifoRegisters.Read(address, size));

            case 0x1FF0:
                switch (address)
                {
                    case 0x1FF00000:
                        break;

                    case 0x1FF00008:
                        break;
                }
                break;
        }

        Trace.TraceInformation($"Unknown Read from P4 mmr - addr={address:x}");
        return T.Zero;
    }

    private void WriteP4Register<T>(uint address, T data) where T : unmanaged, IBinaryInteger<T>
    {
        Debug.WriteLine($"write {RegisterName(address)} = {uint.CreateTruncating(data):x}");

        if (address == 0xFF000038)
        {
            CacheRegisters.WriteQueueAddress(address, uint.CreateTruncating(data));
            return;
        }
        if (address == 0xFF00003C)
        {
            CacheRegisters.WriteQueueAddress(address, uint.CreateTruncating(data));
            return;
        }

        address &= 0x1FFFFFFF;
        uint mapBase = address >> 16;
        int size = Unsafe.SizeOf<T>();
        switch (mapBase)
        {
            case 0x1F00:
                CacheRegisters.Write(address, size, data);
                return;

            case 0x1F20:
                UserBreakRegisters.Write(address, size, data);
                return;

            case 0x1F80:
                BusStateRegisters.Write(address, size, data);
                return;

            case 0x1F90:
                return;

            case 0x1F94:
                return;

            case 0x1FA0:
                DmaRegisters.Write(address, size, data);
                return;

            case 0x1FC0:
                ClockRegisters.Write(address, size, data);
                return;

            case 0x1FC8:
                RealTimeClockRegisters.Write(address, size, data);
                return;

            case 0x1FD0:
                InterruptRegisters.Write(address, size, data);
                return;

            case 0x1FD8:
                TimerRegisters.Write(address, size, data);
                return;

            case 0x1FE0:
                SerialRegisters.Write(address, size, data);
                return;

            case 0x1FE8:
                SerialFifoRegisters.Write(address, size, data);
                return;

            case 0x1FF0:
                switch (address)
                {
                    case 0x1FF00000:
                        break;

                    case 0x1FF00008:
                        break;
                }
                break;
        }

        Trace.TraceInformation($"Write to P4 mmr not implemented, addr={address:x}, data={uint.CreateTruncating(data):x}");
    }

    private int OnChipRamOffset(uint address) =>
        (int)(((address >> ((CacheRegisters.CacheControl & 0x80) != 0 ? 13 : 1)) & 0x1000) | (address & 0xFFF));

    private T ReadOnChipRam<T>(uint address) where T : unmanaged
    {
        if ((CacheRegisters.CacheControl & 0x20) != 0)
            return MemoryMarshal.Read<T>(OnChipRam.AsSpan(OnChipRamOffset(address)));

        Trace.TraceInformation($"On Chip Ram Read, but OCR is disabled. addr {address:x}");
        return default;
    }

    private void WriteOnChipRam<T>(uint address, T data) where T : unmanaged
    {
        if ((CacheRegisters.CacheControl & 0x20) != 0)
            MemoryMarshal.Write(OnChipRam.AsSpan(OnChipRamOffset(address)), in data);
        else
            Trace.TraceInformation($"On Chip Ram Write, but OCR is disabled. addr {address:x}");
    }

    private void InitializeMemoryMappedRegisters()
    {
        BusStateRegisters.Initialize();
        CacheRegisters.Initialize();
        ClockRegisters.Initialize();
        DmaRegisters.Initialize();
        InterruptRegisters.Initialize();
        RealTimeClockRegisters.Initialize();
        SerialFifoRegisters.Initialize();
        SerialRegisters.Initialize();
        TimerRegisters.Initialize();
        UserBreakRegisters.Initialize();

        Mmu.Initialize();
    }

    private void ResetMemoryMappedRegisters(bool hard)
    {
        Array.Clear(OnChipRam);

        BusStateRegisters.Reset();
        CacheRegisters.Reset();
        ClockRegisters.Reset();
        DmaRegisters.Reset();
        InterruptRegisters.Reset();
        RealTimeClockRegisters.Reset();
        SerialFifoRegisters.Reset(hard);
        SerialRegisters.Reset();
        TimerRegisters.Reset();
        UserBreakRegisters.Reset();

        Mmu.Reset();
        RecompilerContext.StoreQueues.Clear();
    }

    private void TerminateMemoryMappedRegisters()
    {
        Sh4Mmu.Terminate();

        UserBreakRegisters.Terminate();
        TimerRegisters.Terminate();
        SerialFifoRegisters.Terminate();
        SerialRegisters.Terminate();
        RealTimeClockRegisters.Terminate();
        InterruptRegisters.Terminate();
        DmaRegisters.Terminate();
        ClockRegisters.Terminate();
        CacheRegisters.Terminate();
        BusStateRegisters.Terminate();
    }

    private void MapInternalRam()
    {
        int handler = RegisterHandler(ReadOnChipRam<byte>, ReadOnChipRam<ushort>, ReadOnChipRam<uint>,
            WriteOnChipRam<byte>, WriteOnChipRam<ushort>, WriteOnChipRam<uint>);
        MapHandler(handler, 0x7C, 0x7F);
    }

    private void MapP4()
    {
        nuint storeQueues = RecompilerContext.NativeAddress + Sh4ContextLayout.StoreQueueBuffer;
        MapBlock(storeQueues, 0xE0, 0xE0, 63);
        MapBlock(storeQueues, 0xE1, 0xE1, 63);
        MapBlock(storeQueues, 0xE2, 0xE2, 63);
        MapBlock(storeQueues, 0xE3, 0xE3, 63);

        int arraysHandler = RegisterHandler(ReadP4<byte>, ReadP4<ushort>, ReadP4<uint>,
            WriteP4<byte>, WriteP4<ushort>, WriteP4<uint>);
        MapHandler(arraysHandler, 0xF0, 0xF7);

        int registersHandler = RegisterHandler(ReadP4Register<byte>, ReadP4Register<ushort>, ReadP4Register<uint>,
            WriteP4Register<byte>, WriteP4Register<ushort>, WriteP4Register<uint>);
        MapHandler(registersHandler, 0xFF, 0xFF);
    }

    private const int SaveVersion19 = 814;
    private const int SaveVersion21 = 816;
    private const int SaveVersion31 = 826;
    private const int SaveVersion32 = 827;

    internal unsafe void SerializeRegisters(BinaryWriter writer, bool rollback)
    {
        writer.Write(OnChipRam);

        writer.Write(MemoryMarshal.AsBytes(CacheRegisters.Registers.AsSpan()));
        writer.Write(MemoryMarshal.AsBytes(UserBreakRegisters.Registers.AsSpan()));
        writer.Write(MemoryMarshal.AsBytes(BusStateRegisters.Registers.AsSpan()));
        writer.Write(MemoryMarshal.AsBytes(DmaRegisters.Registers.AsSpan()));
        writer.Write(MemoryMarshal.AsBytes(ClockRegisters.Registers.AsSpan()));
        writer.Write(MemoryMarshal.AsBytes(RealTimeClockRegisters.Registers.AsSpan()));
        writer.Write(MemoryMarshal.AsBytes(InterruptRegisters.Registers.AsSpan()));
        writer.Write(MemoryMarshal.AsBytes(TimerRegisters.Registers.AsSpan()));
        writer.Write(MemoryMarshal.AsBytes(SerialRegisters.Registers.AsSpan()));
        writer.Write(MemoryMarshal.AsBytes(SerialFifoRegisters.Registers.AsSpan()));
        SerialFifoPort.Serialize(writer);
        InstructionCache.Serialize(writer);
        OperandCache.Serialize(writer);

        if (!rollback)
            writer.Write(new ReadOnlySpan<byte>((void*)RamBuffer, checked((int)RamSize)));

        Interrupts.Serialize(writer);
        writer.Write(MemoryMarshal.AsBytes(RecompilerContext.Raw));
        Scheduler.Serialize(writer);
    }

    internal unsafe void DeserializeRegisters(BinaryReader reader, int version, bool rollback)
    {
        reader.BaseStream.ReadExactly(OnChipRam);

        reader.BaseStream.ReadExactly(MemoryMarshal.AsBytes(CacheRegisters.Registers.AsSpan()));
        reader.BaseStream.ReadExactly(MemoryMarshal.AsBytes(UserBreakRegisters.Registers.AsSpan()));
        reader.BaseStream.ReadExactly(MemoryMarshal.AsBytes(BusStateRegisters.Registers.AsSpan()));
        reader.BaseStream.ReadExactly(MemoryMarshal.AsBytes(DmaRegisters.Registers.AsSpan()));
        reader.BaseStream.ReadExactly(MemoryMarshal.AsBytes(ClockRegisters.Registers.AsSpan()));
        reader.BaseStream.ReadExactly(MemoryMarshal.AsBytes(RealTimeClockRegisters.Registers.AsSpan()));
        reader.BaseStream.ReadExactly(MemoryMarshal.AsBytes(InterruptRegisters.Registers.AsSpan()));
        reader.BaseStream.ReadExactly(MemoryMarshal.AsBytes(TimerRegisters.Registers.AsSpan()));
        reader.BaseStream.ReadExactly(MemoryMarshal.AsBytes(SerialRegisters.Registers.AsSpan()));
        reader.BaseStream.ReadExactly(MemoryMarshal.AsBytes(SerialFifoRegisters.Registers.AsSpan()));
        SerialFifoPort.Deserialize(reader, version);
        InstructionCache.Deserialize(reader);
        OperandCache.Deserialize(reader, version);

        if (!rollback)
            reader.BaseStream.ReadExactly(new Span<byte>((void*)RamBuffer, checked((int)RamSize)));

        Interrupts.Deserialize(reader);
        if (version <= SaveVersion31)
            reader.ReadInt32();
        CacheRegisters.WriteQueueAddress(0xFF000038, CacheRegisters.QueueAddress0);
        CacheRegisters.WriteQueueAddress(0xFF00003C, CacheRegisters.QueueAddress1);
        reader.BaseStream.ReadExactly(MemoryMarshal.AsBytes(RecompilerContext.Raw));
        if (version >= SaveVersion19 && version < SaveVersion21)
            reader.ReadUInt32();
        if (version < SaveVersion21)
            RecompilerContext.CycleCounter = Sh4Scheduler.TimeSlice;

        Scheduler.DeserializeState(reader, version);
        SerialFifoPort.PostDeserialize();
    }

    internal void SerializeTimerAndMmu(BinaryWriter writer)
    {
        TimerRegisters.Serialize(writer);
        Mmu.Serialize(writer);
    }

    internal void DeserializeTimerAndMmu(BinaryReader reader, int version)
    {
        if (version <= SaveVersion32)
        {
            SerialFifoRegisters.Status = reader.ReadUInt32();
            SerialFifoRegisters.Control = reader.ReadUInt32();
            BusStateRegisters.PortAData = reader.ReadUInt32();
        }

        TimerRegisters.Deserialize(reader, version);
        Mmu.Deserialize(reader, version);
    }
}

using System.Diagnostics;
using System.Runtime.CompilerServices;
using RetroMediaLab.Chips.CPU.Hitachi.SH4;
using RetroMediaLab.Chips.Dreamcast.Holly;
using static RetroMediaLab.Chips.Dreamcast.Holly.HollySystemBus;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal static unsafe partial class PvrSystemBusRegisters
{
    private static void WriteSbChannel2Start(uint address, uint data)
    {
        if ((data & 1) != 0)
        {
            SB_C2DST = 1;
            Sh4DreamcastRuntime.AddressSpace.DmaRegisters.StartChannel2();
        }
    }

    private static void DoPvrDma()
    {
        uint source = SB_PDSTAR;
        uint destination = SB_PDSTAP;
        uint length = SB_PDLEN;
        Sh4AddressSpace memory = Sh4DreamcastRuntime.AddressSpace;
        uint[] dmaRegisters = memory.DmaRegisters.Registers;
        uint operation = dmaRegisters[16];

        if (0x8201 != (operation & 0xFFFF8201))
        {
            Trace.TraceInformation($"DMAC: DMAOR has invalid settings ({operation:X}) !");
            return;
        }

        Debug.WriteLine($"PVR-DMA {source:x} {(SB_PDDIR != 0 ? "<-" : "->")} {destination:x} len {length:x}");
        if (SB_PDDIR != 0)
            memory.CopyBlockWithoutMmu(source, destination, length);
        else
            memory.CopyBlockWithoutMmu(destination, source, length);

        dmaRegisters[0] = source + length;
        dmaRegisters[3] |= 2;
        dmaRegisters[2] = 0;
        SB_PDST = 0;
        HollyInterruptController.RaiseInterrupt(HollyInterruptId.PvrDma);
    }

    private static void WriteSbPvrDmaStart(uint address, uint data)
    {
        if ((data & 1) != 0)
        {
            SB_PDST = 1;
            DoPvrDma();
        }
    }

    private static uint CalculateStartLinkAddress()
    {
        uint ramMask = Sh4DreamcastRuntime.AddressSpace.RamMask;
        byte* table = (byte*)Unsafe.AsPointer(ref Sh4AddressSpace.MainRam[SB_SDSTAW & ramMask]);
        uint result;
        if (SB_SDWLT == 0)
            result = ((ushort*)table)[SB_SDDIV];
        else
            result = ((uint*)table)[SB_SDDIV];
        SB_SDDIV++;
        return result;
    }

    private static void DoSortDma()
    {
        Debug.WriteLine($"Sort-DMA SB_SDSTAW {SB_SDSTAW:x} SB_SDDIV {SB_SDDIV:x} SB_SDBAAW {SB_SDBAAW:x}");
        SB_SDDIV = 0;
        uint linkAddress = CalculateStartLinkAddress();

        while (linkAddress != 2)
        {
            if (SB_SDLAS == 1)
                linkAddress *= 32;
            else
                linkAddress &= ~31u;

            uint ramMask = Sh4DreamcastRuntime.AddressSpace.RamMask;
            uint effectiveAddress = (SB_SDBAAW + linkAddress) & ramMask;
            uint* link = (uint*)Unsafe.AsPointer(ref Sh4AddressSpace.MainRam[effectiveAddress]);
            linkAddress = link[0x1C >> 2];
            PowerVr2Ta.VertexData((Sh4StoreQueueBuffer*)link, link[0x18 >> 2]);
            if (linkAddress == 1)
                linkAddress = CalculateStartLinkAddress();
        }

        SB_SDST = 0;
        SB_SDSTAW += 32;
        HollyInterruptController.RaiseInterrupt(HollyInterruptId.PvrSortDma);
    }

    private static void WriteSbSortDmaStart(uint address, uint data)
    {
        if ((data & 1) != 0)
            DoSortDma();
    }

    internal static partial void Init()
    {
        Registers.SetWriteHandler(SB_PDST_addr, WriteSbPvrDmaStart);
        Registers.SetWriteHandler(SB_C2DST_addr, WriteSbChannel2Start);
        Registers.SetWriteHandler(SB_SDST_addr, WriteSbSortDmaStart);
    }

    internal static partial void Term()
    {
    }

    internal static partial void Reset(bool hard)
    {
    }
}

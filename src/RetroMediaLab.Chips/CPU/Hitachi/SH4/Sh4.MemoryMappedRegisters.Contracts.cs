using System.Runtime.CompilerServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal static class Sh4MemoryMappedRegisters
{
    internal const uint CCN_BASE = 0x1F000000;
    internal const uint CCN_PTEH = 0x1F000000;
    internal const uint CCN_PTEL = 0x1F000004;
    internal const uint CCN_TTB = 0x1F000008;
    internal const uint CCN_TEA = 0x1F00000C;
    internal const uint CCN_MMUCR = 0x1F000010;
    internal const uint CCN_BASRA = 0x1F000014;
    internal const uint CCN_BASRB = 0x1F000018;
    internal const uint CCN_CCR = 0x1F00001C;
    internal const uint CCN_TRA = 0x1F000020;
    internal const uint CCN_EXPEVT = 0x1F000024;
    internal const uint CCN_INTEVT = 0x1F000028;
    internal const uint CPU_VERSION = 0x1F000030;
    internal const uint CCN_PTEA = 0x1F000034;
    internal const uint CCN_QACR0 = 0x1F000038;
    internal const uint CCN_QACR1 = 0x1F00003C;
    internal const uint CCN_PRR = 0x1F000044;
    internal const uint UBC_BASE = 0x1F200000;
    internal const uint UBC_BARA = 0x1F200000;
    internal const uint UBC_BAMRA = 0x1F200004;
    internal const uint UBC_BBRA = 0x1F200008;
    internal const uint UBC_BARB = 0x1F20000C;
    internal const uint UBC_BAMRB = 0x1F200010;
    internal const uint UBC_BBRB = 0x1F200014;
    internal const uint UBC_BDRB = 0x1F200018;
    internal const uint UBC_BDMRB = 0x1F20001C;
    internal const uint UBC_BRCR = 0x1F200020;
    internal const uint BSC_BASE = 0x1F800000;
    internal const uint BSC_BCR1 = 0x1F800000;
    internal const uint BSC_BCR2 = 0x1F800004;
    internal const uint BSC_WCR1 = 0x1F800008;
    internal const uint BSC_WCR2 = 0x1F80000C;
    internal const uint BSC_WCR3 = 0x1F800010;
    internal const uint BSC_MCR = 0x1F800014;
    internal const uint BSC_PCR = 0x1F800018;
    internal const uint BSC_RTCSR = 0x1F80001C;
    internal const uint BSC_RTCNT = 0x1F800020;
    internal const uint BSC_RTCOR = 0x1F800024;
    internal const uint BSC_RFCR = 0x1F800028;
    internal const uint BSC_PCTRA = 0x1F80002C;
    internal const uint BSC_PDTRA = 0x1F800030;
    internal const uint BSC_PCTRB = 0x1F800040;
    internal const uint BSC_PDTRB = 0x1F800044;
    internal const uint BSC_GPIOIC = 0x1F800048;
    internal const uint BSC_SDMR2 = 0x1F900000;
    internal const uint BSC_SDMR3 = 0x1F940000;
    internal const uint DMAC_BASE = 0x1FA00000;
    internal const uint DMAC_SAR0 = 0x1FA00000;
    internal const uint DMAC_DAR0 = 0x1FA00004;
    internal const uint DMAC_DMATCR0 = 0x1FA00008;
    internal const uint DMAC_CHCR0 = 0x1FA0000C;
    internal const uint DMAC_SAR1 = 0x1FA00010;
    internal const uint DMAC_DAR1 = 0x1FA00014;
    internal const uint DMAC_DMATCR1 = 0x1FA00018;
    internal const uint DMAC_CHCR1 = 0x1FA0001C;
    internal const uint DMAC_SAR2 = 0x1FA00020;
    internal const uint DMAC_DAR2 = 0x1FA00024;
    internal const uint DMAC_DMATCR2 = 0x1FA00028;
    internal const uint DMAC_CHCR2 = 0x1FA0002C;
    internal const uint DMAC_SAR3 = 0x1FA00030;
    internal const uint DMAC_DAR3 = 0x1FA00034;
    internal const uint DMAC_DMATCR3 = 0x1FA00038;
    internal const uint DMAC_CHCR3 = 0x1FA0003C;
    internal const uint DMAC_DMAOR = 0x1FA00040;
    internal const uint CPG_BASE = 0x1FC00000;
    internal const uint CPG_FRQCR = 0x1FC00000;
    internal const uint CPG_STBCR = 0x1FC00004;
    internal const uint CPG_WTCNT = 0x1FC00008;
    internal const uint CPG_WTCSR = 0x1FC0000C;
    internal const uint CPG_STBCR2 = 0x1FC00010;
    internal const uint RTC_BASE = 0x1FC80000;
    internal const uint RTC_R64CNT = 0x1FC80000;
    internal const uint RTC_RSECCNT = 0x1FC80004;
    internal const uint RTC_RMINCNT = 0x1FC80008;
    internal const uint RTC_RHRCNT = 0x1FC8000C;
    internal const uint RTC_RWKCNT = 0x1FC80010;
    internal const uint RTC_RDAYCNT = 0x1FC80014;
    internal const uint RTC_RMONCNT = 0x1FC80018;
    internal const uint RTC_RYRCNT = 0x1FC8001C;
    internal const uint RTC_RSECAR = 0x1FC80020;
    internal const uint RTC_RMINAR = 0x1FC80024;
    internal const uint RTC_RHRAR = 0x1FC80028;
    internal const uint RTC_RWKAR = 0x1FC8002C;
    internal const uint RTC_RDAYAR = 0x1FC80030;
    internal const uint RTC_RMONAR = 0x1FC80034;
    internal const uint RTC_RCR1 = 0x1FC80038;
    internal const uint RTC_RCR2 = 0x1FC8003C;
    internal const uint INTC_BASE = 0x1FD00000;
    internal const uint INTC_ICR = 0x1FD00000;
    internal const uint INTC_IPRA = 0x1FD00004;
    internal const uint INTC_IPRB = 0x1FD00008;
    internal const uint INTC_IPRC = 0x1FD0000C;
    internal const uint INTC_IPRD = 0x1FD00010;
    internal const uint TMU_BASE = 0x1FD80000;
    internal const uint TMU_TOCR = 0x1FD80000;
    internal const uint TMU_TSTR = 0x1FD80004;
    internal const uint TMU_TCOR0 = 0x1FD80008;
    internal const uint TMU_TCNT0 = 0x1FD8000C;
    internal const uint TMU_TCR0 = 0x1FD80010;
    internal const uint TMU_TCOR1 = 0x1FD80014;
    internal const uint TMU_TCNT1 = 0x1FD80018;
    internal const uint TMU_TCR1 = 0x1FD8001C;
    internal const uint TMU_TCOR2 = 0x1FD80020;
    internal const uint TMU_TCNT2 = 0x1FD80024;
    internal const uint TMU_TCR2 = 0x1FD80028;
    internal const uint TMU_TCPR2 = 0x1FD8002C;
    internal const uint SCI_BASE = 0x1FE00000;
    internal const uint SCI_SCSMR1 = 0x1FE00000;
    internal const uint SCI_SCBRR1 = 0x1FE00004;
    internal const uint SCI_SCSCR1 = 0x1FE00008;
    internal const uint SCI_SCTDR1 = 0x1FE0000C;
    internal const uint SCI_SCSSR1 = 0x1FE00010;
    internal const uint SCI_SCRDR1 = 0x1FE00014;
    internal const uint SCI_SCSCMR1 = 0x1FE00018;
    internal const uint SCI_SCSPTR1 = 0x1FE0001C;
    internal const uint SCIF_BASE = 0x1FE80000;
    internal const uint SCIF_SCSMR2 = 0x1FE80000;
    internal const uint SCIF_SCBRR2 = 0x1FE80004;
    internal const uint SCIF_SCSCR2 = 0x1FE80008;
    internal const uint SCIF_SCFTDR2 = 0x1FE8000C;
    internal const uint SCIF_SCFSR2 = 0x1FE80010;
    internal const uint SCIF_SCFRDR2 = 0x1FE80014;
    internal const uint SCIF_SCFCR2 = 0x1FE80018;
    internal const uint SCIF_SCFDR2 = 0x1FE8001C;
    internal const uint SCIF_SCSPTR2 = 0x1FE80020;
    internal const uint SCIF_SCLSR2 = 0x1FE80024;
    internal const uint UDI_BASE = 0x1FF00000;
    internal const uint UDI_SDIR = 0x1FF00000;
    internal const uint UDI_SDDR = 0x1FF00008;

    internal const uint PageTableEntryHighAddress = CCN_PTEH | 0xE0000000u;
    internal const uint PageTableEntryLowAddress = CCN_PTEL | 0xE0000000u;
    internal const uint TranslationTableBaseAddress = CCN_TTB | 0xE0000000u;
    internal const uint TranslationExceptionAddress = CCN_TEA | 0xE0000000u;
    internal const uint MmuControlAddress = CCN_MMUCR | 0xE0000000u;
    internal const uint BusAccessStatusAAddress = CCN_BASRA | 0xE0000000u;
    internal const uint BusAccessStatusBAddress = CCN_BASRB | 0xE0000000u;
    internal const uint CacheControlAddress = CCN_CCR | 0xE0000000u;
    internal const uint TrapRegisterAddress = CCN_TRA | 0xE0000000u;
    internal const uint ExceptionEventAddress = CCN_EXPEVT | 0xE0000000u;
    internal const uint InterruptEventAddress = CCN_INTEVT | 0xE0000000u;
    internal const uint CpuVersionAddress = CPU_VERSION | 0xE0000000u;
    internal const uint PageTableEntryAssistanceAddress = CCN_PTEA | 0xE0000000u;
    internal const uint QueueAddress0 = CCN_QACR0 | 0xE0000000u;
    internal const uint QueueAddress1 = CCN_QACR1 | 0xE0000000u;
    internal const uint ProcessorRevisionAddress = CCN_PRR | 0xE0000000u;

    internal static int Index(uint address) => (int)((address & 255) / 4);

    internal static int IndexedRegister(uint baseAddress, int offset, int stride) =>
        Index(baseAddress + (uint)(offset * stride));

    internal static class RegisterAliases
    {
        internal static ref BSC_BCR1Register BSC_BCR1(uint[] registers) =>
            ref Unsafe.As<uint, BSC_BCR1Register>(ref registers[Index(Sh4MemoryMappedRegisters.BSC_BCR1)]);

        internal static ref BSC_BCR2Register BSC_BCR2(uint[] registers) =>
            ref Unsafe.As<uint, BSC_BCR2Register>(ref registers[Index(Sh4MemoryMappedRegisters.BSC_BCR2)]);

        internal static ref BSC_WCR1Register BSC_WCR1(uint[] registers) =>
            ref Unsafe.As<uint, BSC_WCR1Register>(ref registers[Index(Sh4MemoryMappedRegisters.BSC_WCR1)]);

        internal static ref BSC_WCR2Register BSC_WCR2(uint[] registers) =>
            ref Unsafe.As<uint, BSC_WCR2Register>(ref registers[Index(Sh4MemoryMappedRegisters.BSC_WCR2)]);

        internal static ref BSC_WCR3Register BSC_WCR3(uint[] registers) =>
            ref Unsafe.As<uint, BSC_WCR3Register>(ref registers[Index(Sh4MemoryMappedRegisters.BSC_WCR3)]);

        internal static ref BSC_MCRRegister BSC_MCR(uint[] registers) =>
            ref Unsafe.As<uint, BSC_MCRRegister>(ref registers[Index(Sh4MemoryMappedRegisters.BSC_MCR)]);

        internal static ref BSC_PCRRegister BSC_PCR(uint[] registers) =>
            ref Unsafe.As<uint, BSC_PCRRegister>(ref registers[Index(Sh4MemoryMappedRegisters.BSC_PCR)]);

        internal static ref BSC_RTCSRRegister BSC_RTCSR(uint[] registers) =>
            ref Unsafe.As<uint, BSC_RTCSRRegister>(ref registers[Index(Sh4MemoryMappedRegisters.BSC_RTCSR)]);

        internal static ref BSC_RTCNTRegister BSC_RTCNT(uint[] registers) =>
            ref Unsafe.As<uint, BSC_RTCNTRegister>(ref registers[Index(Sh4MemoryMappedRegisters.BSC_RTCNT)]);

        internal static ref BSC_RTCORRegister BSC_RTCOR(uint[] registers) =>
            ref Unsafe.As<uint, BSC_RTCORRegister>(ref registers[Index(Sh4MemoryMappedRegisters.BSC_RTCOR)]);

        internal static ref BSC_RFCRRegister BSC_RFCR(uint[] registers) =>
            ref Unsafe.As<uint, BSC_RFCRRegister>(ref registers[Index(Sh4MemoryMappedRegisters.BSC_RFCR)]);

        internal static ref BSC_PCTRARegister BSC_PCTRA(uint[] registers) =>
            ref Unsafe.As<uint, BSC_PCTRARegister>(ref registers[Index(Sh4MemoryMappedRegisters.BSC_PCTRA)]);

        internal static ref BSC_PDTRARegister BSC_PDTRA(uint[] registers) =>
            ref Unsafe.As<uint, BSC_PDTRARegister>(ref registers[Index(Sh4MemoryMappedRegisters.BSC_PDTRA)]);

        internal static ref BSC_PCTRBRegister BSC_PCTRB(uint[] registers) =>
            ref Unsafe.As<uint, BSC_PCTRBRegister>(ref registers[Index(Sh4MemoryMappedRegisters.BSC_PCTRB)]);

        internal static ref BSC_PDTRBRegister BSC_PDTRB(uint[] registers) =>
            ref Unsafe.As<uint, BSC_PDTRBRegister>(ref registers[Index(Sh4MemoryMappedRegisters.BSC_PDTRB)]);

        internal static ref BSC_GPIOICRegister BSC_GPIOIC(uint[] registers) =>
            ref Unsafe.As<uint, BSC_GPIOICRegister>(ref registers[Index(Sh4MemoryMappedRegisters.BSC_GPIOIC)]);

        internal static ref CCN_PTEHRegister CCN_PTEH(uint[] registers) =>
            ref Unsafe.As<uint, CCN_PTEHRegister>(ref registers[Index(Sh4MemoryMappedRegisters.CCN_PTEH)]);

        internal static ref CCN_PTELRegister CCN_PTEL(uint[] registers) =>
            ref Unsafe.As<uint, CCN_PTELRegister>(ref registers[Index(Sh4MemoryMappedRegisters.CCN_PTEL)]);

        internal static ref uint CCN_TTB(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.CCN_TTB)];

        internal static ref uint CCN_TEA(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.CCN_TEA)];

        internal static ref CCN_MMUCRRegister CCN_MMUCR(uint[] registers) =>
            ref Unsafe.As<uint, CCN_MMUCRRegister>(ref registers[Index(Sh4MemoryMappedRegisters.CCN_MMUCR)]);

        internal static ref uint CCN_BASRA(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.CCN_BASRA)];

        internal static ref uint CCN_BASRB(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.CCN_BASRB)];

        internal static ref CCN_CCRRegister CCN_CCR(uint[] registers) =>
            ref Unsafe.As<uint, CCN_CCRRegister>(ref registers[Index(Sh4MemoryMappedRegisters.CCN_CCR)]);

        internal static ref uint CCN_TRA(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.CCN_TRA)];

        internal static ref uint CCN_EXPEVT(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.CCN_EXPEVT)];

        internal static ref uint CCN_INTEVT(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.CCN_INTEVT)];

        internal static ref CCN_PTEARegister CCN_PTEA(uint[] registers) =>
            ref Unsafe.As<uint, CCN_PTEARegister>(ref registers[Index(Sh4MemoryMappedRegisters.CCN_PTEA)]);

        internal static ref CCN_QACRRegister CCN_QACR0(uint[] registers) =>
            ref Unsafe.As<uint, CCN_QACRRegister>(ref registers[Index(Sh4MemoryMappedRegisters.CCN_QACR0)]);

        internal static ref CCN_QACRRegister CCN_QACR1(uint[] registers) =>
            ref Unsafe.As<uint, CCN_QACRRegister>(ref registers[Index(Sh4MemoryMappedRegisters.CCN_QACR1)]);

        internal static ref uint CPG_FRQCR(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.CPG_FRQCR)];

        internal static ref uint CPG_STBCR(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.CPG_STBCR)];

        internal static ref uint CPG_WTCNT(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.CPG_WTCNT)];

        internal static ref uint CPG_WTCSR(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.CPG_WTCSR)];

        internal static ref uint CPG_STBCR2(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.CPG_STBCR2)];

        internal static ref uint DMAC_SAR(uint[] registers, int x) =>
            ref registers[IndexedRegister(Sh4MemoryMappedRegisters.DMAC_SAR0, x, 0x10)];

        internal static ref uint DMAC_DAR(uint[] registers, int x) =>
            ref registers[IndexedRegister(Sh4MemoryMappedRegisters.DMAC_DAR0, x, 0x10)];

        internal static ref uint DMAC_DMATCR(uint[] registers, int x) =>
            ref registers[IndexedRegister(Sh4MemoryMappedRegisters.DMAC_DMATCR0, x, 0x10)];

        internal static ref DMAC_CHCRRegister DMAC_CHCR(uint[] registers, int x) =>
            ref Unsafe.As<uint, DMAC_CHCRRegister>(ref registers[IndexedRegister(Sh4MemoryMappedRegisters.DMAC_CHCR0, x, 0x10)]);

        internal static ref DMAC_DMAORRegister DMAC_DMAOR(uint[] registers) =>
            ref Unsafe.As<uint, DMAC_DMAORRegister>(ref registers[Index(Sh4MemoryMappedRegisters.DMAC_DMAOR)]);

        internal static ref uint UBC_BARA(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.UBC_BARA)];

        internal static ref uint UBC_BAMRA(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.UBC_BAMRA)];

        internal static ref uint UBC_BBRA(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.UBC_BBRA)];

        internal static ref uint UBC_BARB(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.UBC_BARB)];

        internal static ref uint UBC_BAMRB(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.UBC_BAMRB)];

        internal static ref uint UBC_BBRB(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.UBC_BBRB)];

        internal static ref uint UBC_BDRB(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.UBC_BDRB)];

        internal static ref uint UBC_BDMRB(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.UBC_BDMRB)];

        internal static ref uint UBC_BRCR(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.UBC_BRCR)];

        internal static ref uint TMU_TCOR(uint[] registers, int x) =>
            ref registers[IndexedRegister(Sh4MemoryMappedRegisters.TMU_TCOR0, x, 12)];

        internal static ref uint TMU_TCR(uint[] registers, int x) =>
            ref registers[IndexedRegister(Sh4MemoryMappedRegisters.TMU_TCR0, x, 12)];

        internal static ref uint TMU_TOCR(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.TMU_TOCR)];

        internal static ref uint TMU_TSTR(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.TMU_TSTR)];

        internal static ref SCIF_SCSMR2Register SCIF_SCSMR2(uint[] registers) =>
            ref Unsafe.As<uint, SCIF_SCSMR2Register>(ref registers[Index(Sh4MemoryMappedRegisters.SCIF_SCSMR2)]);

        internal static ref uint SCIF_SCBRR2(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.SCIF_SCBRR2)];

        internal static ref SCIF_SCSCR2Register SCIF_SCSCR2(uint[] registers) =>
            ref Unsafe.As<uint, SCIF_SCSCR2Register>(ref registers[Index(Sh4MemoryMappedRegisters.SCIF_SCSCR2)]);

        internal static ref uint SCIF_SCFTDR2(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.SCIF_SCFTDR2)];

        internal static ref SCIF_SCFSR2Register SCIF_SCFSR2(uint[] registers) =>
            ref Unsafe.As<uint, SCIF_SCFSR2Register>(ref registers[Index(Sh4MemoryMappedRegisters.SCIF_SCFSR2)]);

        internal static ref SCIF_SCFCR2Register SCIF_SCFCR2(uint[] registers) =>
            ref Unsafe.As<uint, SCIF_SCFCR2Register>(ref registers[Index(Sh4MemoryMappedRegisters.SCIF_SCFCR2)]);

        internal static ref SCIF_SCSPTR2Register SCIF_SCSPTR2(uint[] registers) =>
            ref Unsafe.As<uint, SCIF_SCSPTR2Register>(ref registers[Index(Sh4MemoryMappedRegisters.SCIF_SCSPTR2)]);

        internal static ref SCIF_SCLSR2Register SCIF_SCLSR2(uint[] registers) =>
            ref Unsafe.As<uint, SCIF_SCLSR2Register>(ref registers[Index(Sh4MemoryMappedRegisters.SCIF_SCLSR2)]);

        internal static ref uint RTC_R64CNT(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.RTC_R64CNT)];

        internal static ref uint RTC_RSECCNT(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.RTC_RSECCNT)];

        internal static ref uint RTC_RMINCNT(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.RTC_RMINCNT)];

        internal static ref uint RTC_RHRCNT(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.RTC_RHRCNT)];

        internal static ref uint RTC_RWKCNT(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.RTC_RWKCNT)];

        internal static ref uint RTC_RDAYCNT(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.RTC_RDAYCNT)];

        internal static ref uint RTC_RMONCNT(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.RTC_RMONCNT)];

        internal static ref uint RTC_RYRCNT(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.RTC_RYRCNT)];

        internal static ref uint RTC_RSECAR(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.RTC_RSECAR)];

        internal static ref uint RTC_RMINAR(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.RTC_RMINAR)];

        internal static ref uint RTC_RHRAR(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.RTC_RHRAR)];

        internal static ref uint RTC_RWKAR(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.RTC_RWKAR)];

        internal static ref uint RTC_RDAYAR(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.RTC_RDAYAR)];

        internal static ref uint RTC_RMONAR(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.RTC_RMONAR)];

        internal static ref uint RTC_RCR1(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.RTC_RCR1)];

        internal static ref uint RTC_RCR2(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.RTC_RCR2)];

        internal static ref INTC_ICRRegister INTC_ICR(uint[] registers) =>
            ref Unsafe.As<uint, INTC_ICRRegister>(ref registers[Index(Sh4MemoryMappedRegisters.INTC_ICR)]);

        internal static ref INTC_IPRARegister INTC_IPRA(uint[] registers) =>
            ref Unsafe.As<uint, INTC_IPRARegister>(ref registers[Index(Sh4MemoryMappedRegisters.INTC_IPRA)]);

        internal static ref INTC_IPRBRegister INTC_IPRB(uint[] registers) =>
            ref Unsafe.As<uint, INTC_IPRBRegister>(ref registers[Index(Sh4MemoryMappedRegisters.INTC_IPRB)]);

        internal static ref INTC_IPRCRegister INTC_IPRC(uint[] registers) =>
            ref Unsafe.As<uint, INTC_IPRCRegister>(ref registers[Index(Sh4MemoryMappedRegisters.INTC_IPRC)]);

        internal static ref uint SCI_SCSMR1(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.SCI_SCSMR1)];

        internal static ref uint SCI_SCBRR1(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.SCI_SCBRR1)];

        internal static ref uint SCI_SCSCR1(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.SCI_SCSCR1)];

        internal static ref uint SCI_SCTDR1(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.SCI_SCTDR1)];

        internal static ref uint SCI_SCSSR1(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.SCI_SCSSR1)];

        internal static ref uint SCI_SCRDR1(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.SCI_SCRDR1)];

        internal static ref uint SCI_SCSPTR1(uint[] registers) =>
            ref registers[Index(Sh4MemoryMappedRegisters.SCI_SCSPTR1)];
    }
}

internal struct BSC_BCR1Register
{
    private uint _bits;

    internal uint Raw { readonly get => _bits; set => _bits = value; }

    internal uint A56PCM
    {
        readonly get => (_bits >> 0) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 0)) | ((value & 0x1u) << 0);
    }

    internal uint DRAMTP0
    {
        readonly get => (_bits >> 2) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 2)) | ((value & 0x1u) << 2);
    }

    internal uint DRAMTP1
    {
        readonly get => (_bits >> 3) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 3)) | ((value & 0x1u) << 3);
    }

    internal uint DRAMTP2
    {
        readonly get => (_bits >> 4) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 4)) | ((value & 0x1u) << 4);
    }

    internal uint A6BST0
    {
        readonly get => (_bits >> 5) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 5)) | ((value & 0x1u) << 5);
    }

    internal uint A6BST1
    {
        readonly get => (_bits >> 6) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 6)) | ((value & 0x1u) << 6);
    }

    internal uint A6BST2
    {
        readonly get => (_bits >> 7) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 7)) | ((value & 0x1u) << 7);
    }

    internal uint A5BST0
    {
        readonly get => (_bits >> 8) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 8)) | ((value & 0x1u) << 8);
    }

    internal uint A5BST1
    {
        readonly get => (_bits >> 9) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 9)) | ((value & 0x1u) << 9);
    }

    internal uint A5BST2
    {
        readonly get => (_bits >> 10) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 10)) | ((value & 0x1u) << 10);
    }

    internal uint A0BST0
    {
        readonly get => (_bits >> 11) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 11)) | ((value & 0x1u) << 11);
    }

    internal uint A0BST1
    {
        readonly get => (_bits >> 12) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 12)) | ((value & 0x1u) << 12);
    }

    internal uint A0BST2
    {
        readonly get => (_bits >> 13) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 13)) | ((value & 0x1u) << 13);
    }

    internal uint HIZCNT
    {
        readonly get => (_bits >> 14) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 14)) | ((value & 0x1u) << 14);
    }

    internal uint HIZMEM
    {
        readonly get => (_bits >> 15) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 15)) | ((value & 0x1u) << 15);
    }

    internal uint MEMMPX
    {
        readonly get => (_bits >> 17) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 17)) | ((value & 0x1u) << 17);
    }

    internal uint PSHR
    {
        readonly get => (_bits >> 18) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 18)) | ((value & 0x1u) << 18);
    }

    internal uint BREQEN
    {
        readonly get => (_bits >> 19) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 19)) | ((value & 0x1u) << 19);
    }

    internal uint A4MBC
    {
        readonly get => (_bits >> 20) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 20)) | ((value & 0x1u) << 20);
    }

    internal uint A1MBC
    {
        readonly get => (_bits >> 21) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 21)) | ((value & 0x1u) << 21);
    }

    internal uint OPUP
    {
        readonly get => (_bits >> 24) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 24)) | ((value & 0x1u) << 24);
    }

    internal uint IPUP
    {
        readonly get => (_bits >> 25) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 25)) | ((value & 0x1u) << 25);
    }

    internal uint A0MPX
    {
        readonly get => (_bits >> 29) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 29)) | ((value & 0x1u) << 29);
    }

    internal uint MASTER
    {
        readonly get => (_bits >> 30) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 30)) | ((value & 0x1u) << 30);
    }

    internal uint ENDIAN
    {
        readonly get => (_bits >> 31) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 31)) | ((value & 0x1u) << 31);
    }
}

internal struct BSC_BCR2Register
{
    private uint _bits;

    internal ushort Raw { readonly get => (ushort)_bits; set => _bits = (_bits & 0xFFFF0000u) | value; }

    internal uint PORTEN
    {
        readonly get => (_bits >> 0) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 0)) | ((value & 0x1u) << 0);
    }

    internal uint A0SZ0
    {
        readonly get => (_bits >> 2) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 2)) | ((value & 0x1u) << 2);
    }

    internal uint A1SZ1
    {
        readonly get => (_bits >> 3) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 3)) | ((value & 0x1u) << 3);
    }

    internal uint A2SZ0
    {
        readonly get => (_bits >> 4) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 4)) | ((value & 0x1u) << 4);
    }

    internal uint A2SZ1
    {
        readonly get => (_bits >> 5) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 5)) | ((value & 0x1u) << 5);
    }

    internal uint A3SZ0
    {
        readonly get => (_bits >> 6) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 6)) | ((value & 0x1u) << 6);
    }

    internal uint A3SZ1
    {
        readonly get => (_bits >> 7) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 7)) | ((value & 0x1u) << 7);
    }

    internal uint A4SZ0
    {
        readonly get => (_bits >> 8) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 8)) | ((value & 0x1u) << 8);
    }

    internal uint A4SZ1
    {
        readonly get => (_bits >> 9) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 9)) | ((value & 0x1u) << 9);
    }

    internal uint A5SZ0
    {
        readonly get => (_bits >> 10) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 10)) | ((value & 0x1u) << 10);
    }

    internal uint A5SZ1
    {
        readonly get => (_bits >> 11) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 11)) | ((value & 0x1u) << 11);
    }

    internal uint A6SZ0
    {
        readonly get => (_bits >> 12) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 12)) | ((value & 0x1u) << 12);
    }

    internal uint A6SZ1
    {
        readonly get => (_bits >> 13) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 13)) | ((value & 0x1u) << 13);
    }

    internal uint A0SZ0_inp
    {
        readonly get => (_bits >> 14) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 14)) | ((value & 0x1u) << 14);
    }

    internal uint A0SZ1_inp
    {
        readonly get => (_bits >> 15) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 15)) | ((value & 0x1u) << 15);
    }
}

internal struct BSC_WCR1Register
{
    private uint _bits;

    internal uint Raw { readonly get => _bits; set => _bits = value; }

    internal uint A0IW0
    {
        readonly get => (_bits >> 0) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 0)) | ((value & 0x1u) << 0);
    }

    internal uint A0IW1
    {
        readonly get => (_bits >> 1) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 1)) | ((value & 0x1u) << 1);
    }

    internal uint A0IW2
    {
        readonly get => (_bits >> 2) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 2)) | ((value & 0x1u) << 2);
    }

    internal uint A1IW0
    {
        readonly get => (_bits >> 4) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 4)) | ((value & 0x1u) << 4);
    }

    internal uint A1IW1
    {
        readonly get => (_bits >> 5) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 5)) | ((value & 0x1u) << 5);
    }

    internal uint A1IW2
    {
        readonly get => (_bits >> 6) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 6)) | ((value & 0x1u) << 6);
    }

    internal uint A2IW0
    {
        readonly get => (_bits >> 8) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 8)) | ((value & 0x1u) << 8);
    }

    internal uint A2IW1
    {
        readonly get => (_bits >> 9) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 9)) | ((value & 0x1u) << 9);
    }

    internal uint A2IW2
    {
        readonly get => (_bits >> 10) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 10)) | ((value & 0x1u) << 10);
    }

    internal uint A3IW0
    {
        readonly get => (_bits >> 12) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 12)) | ((value & 0x1u) << 12);
    }

    internal uint A3IW1
    {
        readonly get => (_bits >> 13) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 13)) | ((value & 0x1u) << 13);
    }

    internal uint A3IW2
    {
        readonly get => (_bits >> 14) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 14)) | ((value & 0x1u) << 14);
    }

    internal uint A4IW0
    {
        readonly get => (_bits >> 16) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 16)) | ((value & 0x1u) << 16);
    }

    internal uint A4IW1
    {
        readonly get => (_bits >> 17) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 17)) | ((value & 0x1u) << 17);
    }

    internal uint A4IW2
    {
        readonly get => (_bits >> 18) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 18)) | ((value & 0x1u) << 18);
    }

    internal uint A5IW0
    {
        readonly get => (_bits >> 20) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 20)) | ((value & 0x1u) << 20);
    }

    internal uint A5IW1
    {
        readonly get => (_bits >> 21) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 21)) | ((value & 0x1u) << 21);
    }

    internal uint A5IW2
    {
        readonly get => (_bits >> 22) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 22)) | ((value & 0x1u) << 22);
    }

    internal uint A6IW0
    {
        readonly get => (_bits >> 24) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 24)) | ((value & 0x1u) << 24);
    }

    internal uint A6IW1
    {
        readonly get => (_bits >> 25) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 25)) | ((value & 0x1u) << 25);
    }

    internal uint A6IW2
    {
        readonly get => (_bits >> 26) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 26)) | ((value & 0x1u) << 26);
    }

    internal uint DMAIW0
    {
        readonly get => (_bits >> 28) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 28)) | ((value & 0x1u) << 28);
    }

    internal uint DMAIW1
    {
        readonly get => (_bits >> 29) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 29)) | ((value & 0x1u) << 29);
    }

    internal uint DMAIW2
    {
        readonly get => (_bits >> 30) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 30)) | ((value & 0x1u) << 30);
    }
}

internal struct BSC_WCR2Register
{
    private uint _bits;

    internal uint Raw { readonly get => _bits; set => _bits = value; }

    internal uint A0B0
    {
        readonly get => (_bits >> 0) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 0)) | ((value & 0x1u) << 0);
    }

    internal uint A0B1
    {
        readonly get => (_bits >> 1) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 1)) | ((value & 0x1u) << 1);
    }

    internal uint A0B2
    {
        readonly get => (_bits >> 2) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 2)) | ((value & 0x1u) << 2);
    }

    internal uint A0W0
    {
        readonly get => (_bits >> 3) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 3)) | ((value & 0x1u) << 3);
    }

    internal uint A0W1
    {
        readonly get => (_bits >> 4) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 4)) | ((value & 0x1u) << 4);
    }

    internal uint A0W2
    {
        readonly get => (_bits >> 5) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 5)) | ((value & 0x1u) << 5);
    }

    internal uint A1W0
    {
        readonly get => (_bits >> 6) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 6)) | ((value & 0x1u) << 6);
    }

    internal uint A1W1
    {
        readonly get => (_bits >> 7) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 7)) | ((value & 0x1u) << 7);
    }

    internal uint A1W2
    {
        readonly get => (_bits >> 8) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 8)) | ((value & 0x1u) << 8);
    }

    internal uint A2W0
    {
        readonly get => (_bits >> 9) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 9)) | ((value & 0x1u) << 9);
    }

    internal uint A2W1
    {
        readonly get => (_bits >> 10) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 10)) | ((value & 0x1u) << 10);
    }

    internal uint A2W2
    {
        readonly get => (_bits >> 11) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 11)) | ((value & 0x1u) << 11);
    }

    internal uint A3W0
    {
        readonly get => (_bits >> 13) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 13)) | ((value & 0x1u) << 13);
    }

    internal uint A3W1
    {
        readonly get => (_bits >> 14) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 14)) | ((value & 0x1u) << 14);
    }

    internal uint A3W2
    {
        readonly get => (_bits >> 15) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 15)) | ((value & 0x1u) << 15);
    }

    internal uint A4W0
    {
        readonly get => (_bits >> 17) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 17)) | ((value & 0x1u) << 17);
    }

    internal uint A4W1
    {
        readonly get => (_bits >> 18) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 18)) | ((value & 0x1u) << 18);
    }

    internal uint A4W2
    {
        readonly get => (_bits >> 19) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 19)) | ((value & 0x1u) << 19);
    }

    internal uint A5B0
    {
        readonly get => (_bits >> 20) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 20)) | ((value & 0x1u) << 20);
    }

    internal uint A5B1
    {
        readonly get => (_bits >> 21) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 21)) | ((value & 0x1u) << 21);
    }

    internal uint A5B2
    {
        readonly get => (_bits >> 22) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 22)) | ((value & 0x1u) << 22);
    }

    internal uint A5W0
    {
        readonly get => (_bits >> 23) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 23)) | ((value & 0x1u) << 23);
    }

    internal uint A5W1
    {
        readonly get => (_bits >> 24) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 24)) | ((value & 0x1u) << 24);
    }

    internal uint A5W2
    {
        readonly get => (_bits >> 25) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 25)) | ((value & 0x1u) << 25);
    }

    internal uint A6B0
    {
        readonly get => (_bits >> 26) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 26)) | ((value & 0x1u) << 26);
    }

    internal uint A6B1
    {
        readonly get => (_bits >> 27) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 27)) | ((value & 0x1u) << 27);
    }

    internal uint A6B2
    {
        readonly get => (_bits >> 28) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 28)) | ((value & 0x1u) << 28);
    }

    internal uint A6W0
    {
        readonly get => (_bits >> 29) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 29)) | ((value & 0x1u) << 29);
    }

    internal uint A6W1
    {
        readonly get => (_bits >> 30) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 30)) | ((value & 0x1u) << 30);
    }

    internal uint A6W2
    {
        readonly get => (_bits >> 31) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 31)) | ((value & 0x1u) << 31);
    }
}

internal struct BSC_WCR3Register
{
    private uint _bits;

    internal uint Raw { readonly get => _bits; set => _bits = value; }

    internal uint A0H0
    {
        readonly get => (_bits >> 0) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 0)) | ((value & 0x1u) << 0);
    }

    internal uint A0H1
    {
        readonly get => (_bits >> 1) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 1)) | ((value & 0x1u) << 1);
    }

    internal uint A0S0
    {
        readonly get => (_bits >> 2) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 2)) | ((value & 0x1u) << 2);
    }

    internal uint A1H0
    {
        readonly get => (_bits >> 4) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 4)) | ((value & 0x1u) << 4);
    }

    internal uint A1H1
    {
        readonly get => (_bits >> 5) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 5)) | ((value & 0x1u) << 5);
    }

    internal uint A1S0
    {
        readonly get => (_bits >> 6) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 6)) | ((value & 0x1u) << 6);
    }

    internal uint A2H0
    {
        readonly get => (_bits >> 8) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 8)) | ((value & 0x1u) << 8);
    }

    internal uint A2H1
    {
        readonly get => (_bits >> 9) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 9)) | ((value & 0x1u) << 9);
    }

    internal uint A2S0
    {
        readonly get => (_bits >> 10) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 10)) | ((value & 0x1u) << 10);
    }

    internal uint A3H0
    {
        readonly get => (_bits >> 12) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 12)) | ((value & 0x1u) << 12);
    }

    internal uint A3H1
    {
        readonly get => (_bits >> 13) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 13)) | ((value & 0x1u) << 13);
    }

    internal uint A3S0
    {
        readonly get => (_bits >> 14) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 14)) | ((value & 0x1u) << 14);
    }

    internal uint A4H0
    {
        readonly get => (_bits >> 16) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 16)) | ((value & 0x1u) << 16);
    }

    internal uint A4H1
    {
        readonly get => (_bits >> 17) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 17)) | ((value & 0x1u) << 17);
    }

    internal uint A4S0
    {
        readonly get => (_bits >> 18) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 18)) | ((value & 0x1u) << 18);
    }

    internal uint A5H0
    {
        readonly get => (_bits >> 20) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 20)) | ((value & 0x1u) << 20);
    }

    internal uint A5H1
    {
        readonly get => (_bits >> 21) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 21)) | ((value & 0x1u) << 21);
    }

    internal uint A5S0
    {
        readonly get => (_bits >> 22) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 22)) | ((value & 0x1u) << 22);
    }

    internal uint A6H0
    {
        readonly get => (_bits >> 24) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 24)) | ((value & 0x1u) << 24);
    }

    internal uint A6H1
    {
        readonly get => (_bits >> 25) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 25)) | ((value & 0x1u) << 25);
    }

    internal uint A6S0
    {
        readonly get => (_bits >> 26) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 26)) | ((value & 0x1u) << 26);
    }
}

internal struct BSC_MCRRegister
{
    private uint _bits;

    internal uint Raw { readonly get => _bits; set => _bits = value; }

    internal uint EDO_MODE
    {
        readonly get => (_bits >> 0) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 0)) | ((value & 0x1u) << 0);
    }

    internal uint RMODE
    {
        readonly get => (_bits >> 1) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 1)) | ((value & 0x1u) << 1);
    }

    internal uint RFSH
    {
        readonly get => (_bits >> 2) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 2)) | ((value & 0x1u) << 2);
    }

    internal uint AMX0
    {
        readonly get => (_bits >> 3) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 3)) | ((value & 0x1u) << 3);
    }

    internal uint AMX1
    {
        readonly get => (_bits >> 4) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 4)) | ((value & 0x1u) << 4);
    }

    internal uint AMX2
    {
        readonly get => (_bits >> 5) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 5)) | ((value & 0x1u) << 5);
    }

    internal uint AMXEXT
    {
        readonly get => (_bits >> 6) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 6)) | ((value & 0x1u) << 6);
    }

    internal uint SZ0
    {
        readonly get => (_bits >> 7) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 7)) | ((value & 0x1u) << 7);
    }

    internal uint SZ1
    {
        readonly get => (_bits >> 8) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 8)) | ((value & 0x1u) << 8);
    }

    internal uint BE
    {
        readonly get => (_bits >> 9) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 9)) | ((value & 0x1u) << 9);
    }

    internal uint TRAS0
    {
        readonly get => (_bits >> 10) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 10)) | ((value & 0x1u) << 10);
    }

    internal uint TRAS1
    {
        readonly get => (_bits >> 11) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 11)) | ((value & 0x1u) << 11);
    }

    internal uint TRAS2
    {
        readonly get => (_bits >> 12) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 12)) | ((value & 0x1u) << 12);
    }

    internal uint TRWL0
    {
        readonly get => (_bits >> 13) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 13)) | ((value & 0x1u) << 13);
    }

    internal uint TRWL1
    {
        readonly get => (_bits >> 14) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 14)) | ((value & 0x1u) << 14);
    }

    internal uint TRWL2
    {
        readonly get => (_bits >> 15) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 15)) | ((value & 0x1u) << 15);
    }

    internal uint RCD0
    {
        readonly get => (_bits >> 16) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 16)) | ((value & 0x1u) << 16);
    }

    internal uint RCD1
    {
        readonly get => (_bits >> 17) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 17)) | ((value & 0x1u) << 17);
    }

    internal uint TPC0
    {
        readonly get => (_bits >> 19) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 19)) | ((value & 0x1u) << 19);
    }

    internal uint TPC1
    {
        readonly get => (_bits >> 20) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 20)) | ((value & 0x1u) << 20);
    }

    internal uint TPC2
    {
        readonly get => (_bits >> 21) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 21)) | ((value & 0x1u) << 21);
    }

    internal uint TCAS
    {
        readonly get => (_bits >> 23) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 23)) | ((value & 0x1u) << 23);
    }

    internal uint TRC0
    {
        readonly get => (_bits >> 27) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 27)) | ((value & 0x1u) << 27);
    }

    internal uint TRC1
    {
        readonly get => (_bits >> 28) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 28)) | ((value & 0x1u) << 28);
    }

    internal uint TRC2
    {
        readonly get => (_bits >> 29) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 29)) | ((value & 0x1u) << 29);
    }

    internal uint MRSET
    {
        readonly get => (_bits >> 30) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 30)) | ((value & 0x1u) << 30);
    }

    internal uint RASD
    {
        readonly get => (_bits >> 31) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 31)) | ((value & 0x1u) << 31);
    }
}

internal struct BSC_PCRRegister
{
    private uint _bits;

    internal ushort Raw { readonly get => (ushort)_bits; set => _bits = (_bits & 0xFFFF0000u) | value; }

    internal uint A6TEH0
    {
        readonly get => (_bits >> 0) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 0)) | ((value & 0x1u) << 0);
    }

    internal uint A6TEH1
    {
        readonly get => (_bits >> 1) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 1)) | ((value & 0x1u) << 1);
    }

    internal uint A6TEH2
    {
        readonly get => (_bits >> 2) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 2)) | ((value & 0x1u) << 2);
    }

    internal uint A5TEH0
    {
        readonly get => (_bits >> 3) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 3)) | ((value & 0x1u) << 3);
    }

    internal uint A5TEH1
    {
        readonly get => (_bits >> 4) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 4)) | ((value & 0x1u) << 4);
    }

    internal uint A5TEH2
    {
        readonly get => (_bits >> 5) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 5)) | ((value & 0x1u) << 5);
    }

    internal uint A6TED0
    {
        readonly get => (_bits >> 6) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 6)) | ((value & 0x1u) << 6);
    }

    internal uint A6TED1
    {
        readonly get => (_bits >> 7) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 7)) | ((value & 0x1u) << 7);
    }

    internal uint A6TED2
    {
        readonly get => (_bits >> 8) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 8)) | ((value & 0x1u) << 8);
    }

    internal uint A5TED0
    {
        readonly get => (_bits >> 9) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 9)) | ((value & 0x1u) << 9);
    }

    internal uint A5TED1
    {
        readonly get => (_bits >> 10) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 10)) | ((value & 0x1u) << 10);
    }

    internal uint A5TED2
    {
        readonly get => (_bits >> 11) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 11)) | ((value & 0x1u) << 11);
    }

    internal uint A6PCW0
    {
        readonly get => (_bits >> 12) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 12)) | ((value & 0x1u) << 12);
    }

    internal uint A6PCW1
    {
        readonly get => (_bits >> 13) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 13)) | ((value & 0x1u) << 13);
    }

    internal uint A5PCW0
    {
        readonly get => (_bits >> 14) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 14)) | ((value & 0x1u) << 14);
    }

    internal uint A5PCW1
    {
        readonly get => (_bits >> 15) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 15)) | ((value & 0x1u) << 15);
    }
}

internal struct BSC_RTCSRRegister
{
    private uint _bits;

    internal ushort Raw { readonly get => (ushort)_bits; set => _bits = (_bits & 0xFFFF0000u) | value; }

    internal uint LMTS
    {
        readonly get => (_bits >> 0) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 0)) | ((value & 0x1u) << 0);
    }

    internal uint OVIE
    {
        readonly get => (_bits >> 1) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 1)) | ((value & 0x1u) << 1);
    }

    internal uint OVF
    {
        readonly get => (_bits >> 2) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 2)) | ((value & 0x1u) << 2);
    }

    internal uint CKS0
    {
        readonly get => (_bits >> 3) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 3)) | ((value & 0x1u) << 3);
    }

    internal uint CKS1
    {
        readonly get => (_bits >> 4) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 4)) | ((value & 0x1u) << 4);
    }

    internal uint CKS2
    {
        readonly get => (_bits >> 5) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 5)) | ((value & 0x1u) << 5);
    }

    internal uint CMIE
    {
        readonly get => (_bits >> 6) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 6)) | ((value & 0x1u) << 6);
    }

    internal uint CMF
    {
        readonly get => (_bits >> 7) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 7)) | ((value & 0x1u) << 7);
    }
}

internal struct BSC_RTCNTRegister
{
    private uint _bits;

    internal ushort Raw { readonly get => (ushort)_bits; set => _bits = (_bits & 0xFFFF0000u) | value; }

    internal uint VALUE
    {
        readonly get => (_bits >> 0) & 0xFFu;
        set => _bits = (_bits & ~(0xFFu << 0)) | ((value & 0xFFu) << 0);
    }
}

internal struct BSC_RTCORRegister
{
    private uint _bits;

    internal ushort Raw { readonly get => (ushort)_bits; set => _bits = (_bits & 0xFFFF0000u) | value; }

    internal uint VALUE
    {
        readonly get => (_bits >> 0) & 0xFFu;
        set => _bits = (_bits & ~(0xFFu << 0)) | ((value & 0xFFu) << 0);
    }
}

internal struct BSC_RFCRRegister
{
    private uint _bits;

    internal ushort Raw { readonly get => (ushort)_bits; set => _bits = (_bits & 0xFFFF0000u) | value; }

    internal uint VALUE
    {
        readonly get => (_bits >> 0) & 0x3FFu;
        set => _bits = (_bits & ~(0x3FFu << 0)) | ((value & 0x3FFu) << 0);
    }
}

internal struct BSC_PCTRARegister
{
    private uint _bits;

    internal uint Raw { readonly get => _bits; set => _bits = value; }

    internal uint PB0IO
    {
        readonly get => (_bits >> 0) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 0)) | ((value & 0x1u) << 0);
    }

    internal uint PB0PUP
    {
        readonly get => (_bits >> 1) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 1)) | ((value & 0x1u) << 1);
    }

    internal uint PB1IO
    {
        readonly get => (_bits >> 2) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 2)) | ((value & 0x1u) << 2);
    }

    internal uint PB1PUP
    {
        readonly get => (_bits >> 3) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 3)) | ((value & 0x1u) << 3);
    }

    internal uint PB2IO
    {
        readonly get => (_bits >> 4) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 4)) | ((value & 0x1u) << 4);
    }

    internal uint PB2PUP
    {
        readonly get => (_bits >> 5) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 5)) | ((value & 0x1u) << 5);
    }

    internal uint PB3IO
    {
        readonly get => (_bits >> 6) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 6)) | ((value & 0x1u) << 6);
    }

    internal uint PB3PUP
    {
        readonly get => (_bits >> 7) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 7)) | ((value & 0x1u) << 7);
    }

    internal uint PB4IO
    {
        readonly get => (_bits >> 8) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 8)) | ((value & 0x1u) << 8);
    }

    internal uint PB4PUP
    {
        readonly get => (_bits >> 9) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 9)) | ((value & 0x1u) << 9);
    }

    internal uint PB5IO
    {
        readonly get => (_bits >> 10) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 10)) | ((value & 0x1u) << 10);
    }

    internal uint PB5PUP
    {
        readonly get => (_bits >> 11) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 11)) | ((value & 0x1u) << 11);
    }

    internal uint PB6IO
    {
        readonly get => (_bits >> 12) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 12)) | ((value & 0x1u) << 12);
    }

    internal uint PB6PUP
    {
        readonly get => (_bits >> 13) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 13)) | ((value & 0x1u) << 13);
    }

    internal uint PB7IO
    {
        readonly get => (_bits >> 14) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 14)) | ((value & 0x1u) << 14);
    }

    internal uint PB7PUP
    {
        readonly get => (_bits >> 15) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 15)) | ((value & 0x1u) << 15);
    }

    internal uint PB8IO
    {
        readonly get => (_bits >> 16) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 16)) | ((value & 0x1u) << 16);
    }

    internal uint PB8PUP
    {
        readonly get => (_bits >> 17) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 17)) | ((value & 0x1u) << 17);
    }

    internal uint PB9IO
    {
        readonly get => (_bits >> 18) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 18)) | ((value & 0x1u) << 18);
    }

    internal uint PB9PUP
    {
        readonly get => (_bits >> 19) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 19)) | ((value & 0x1u) << 19);
    }

    internal uint PB10IO
    {
        readonly get => (_bits >> 20) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 20)) | ((value & 0x1u) << 20);
    }

    internal uint PB10PUP
    {
        readonly get => (_bits >> 21) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 21)) | ((value & 0x1u) << 21);
    }

    internal uint PB11IO
    {
        readonly get => (_bits >> 22) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 22)) | ((value & 0x1u) << 22);
    }

    internal uint PB11PUP
    {
        readonly get => (_bits >> 23) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 23)) | ((value & 0x1u) << 23);
    }

    internal uint PB12IO
    {
        readonly get => (_bits >> 24) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 24)) | ((value & 0x1u) << 24);
    }

    internal uint PB12PUP
    {
        readonly get => (_bits >> 25) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 25)) | ((value & 0x1u) << 25);
    }

    internal uint PB13IO
    {
        readonly get => (_bits >> 26) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 26)) | ((value & 0x1u) << 26);
    }

    internal uint PB13PUP
    {
        readonly get => (_bits >> 27) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 27)) | ((value & 0x1u) << 27);
    }

    internal uint PB14IO
    {
        readonly get => (_bits >> 28) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 28)) | ((value & 0x1u) << 28);
    }

    internal uint PB14PUP
    {
        readonly get => (_bits >> 29) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 29)) | ((value & 0x1u) << 29);
    }

    internal uint PB15IO
    {
        readonly get => (_bits >> 30) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 30)) | ((value & 0x1u) << 30);
    }

    internal uint PB15PUP
    {
        readonly get => (_bits >> 31) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 31)) | ((value & 0x1u) << 31);
    }
}

internal struct BSC_PDTRARegister
{
    private uint _bits;

    internal ushort Raw { readonly get => (ushort)_bits; set => _bits = (_bits & 0xFFFF0000u) | value; }

    internal uint PB0DT
    {
        readonly get => (_bits >> 0) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 0)) | ((value & 0x1u) << 0);
    }

    internal uint PB1DT
    {
        readonly get => (_bits >> 1) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 1)) | ((value & 0x1u) << 1);
    }

    internal uint PB2DT
    {
        readonly get => (_bits >> 2) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 2)) | ((value & 0x1u) << 2);
    }

    internal uint PB3DT
    {
        readonly get => (_bits >> 3) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 3)) | ((value & 0x1u) << 3);
    }

    internal uint PB4DT
    {
        readonly get => (_bits >> 4) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 4)) | ((value & 0x1u) << 4);
    }

    internal uint PB5DT
    {
        readonly get => (_bits >> 5) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 5)) | ((value & 0x1u) << 5);
    }

    internal uint PB6DT
    {
        readonly get => (_bits >> 6) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 6)) | ((value & 0x1u) << 6);
    }

    internal uint PB7DT
    {
        readonly get => (_bits >> 7) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 7)) | ((value & 0x1u) << 7);
    }

    internal uint PB8DT
    {
        readonly get => (_bits >> 8) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 8)) | ((value & 0x1u) << 8);
    }

    internal uint PB9DT
    {
        readonly get => (_bits >> 9) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 9)) | ((value & 0x1u) << 9);
    }

    internal uint PB10DT
    {
        readonly get => (_bits >> 10) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 10)) | ((value & 0x1u) << 10);
    }

    internal uint PB11DT
    {
        readonly get => (_bits >> 11) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 11)) | ((value & 0x1u) << 11);
    }

    internal uint PB12DT
    {
        readonly get => (_bits >> 12) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 12)) | ((value & 0x1u) << 12);
    }

    internal uint PB13DT
    {
        readonly get => (_bits >> 13) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 13)) | ((value & 0x1u) << 13);
    }

    internal uint PB14DT
    {
        readonly get => (_bits >> 14) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 14)) | ((value & 0x1u) << 14);
    }

    internal uint PB15DT
    {
        readonly get => (_bits >> 15) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 15)) | ((value & 0x1u) << 15);
    }
}

internal struct BSC_PCTRBRegister
{
    private uint _bits;

    internal uint Raw { readonly get => _bits; set => _bits = value; }

    internal uint PB16IO
    {
        readonly get => (_bits >> 0) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 0)) | ((value & 0x1u) << 0);
    }

    internal uint PB16PUP
    {
        readonly get => (_bits >> 1) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 1)) | ((value & 0x1u) << 1);
    }

    internal uint PB17IO
    {
        readonly get => (_bits >> 2) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 2)) | ((value & 0x1u) << 2);
    }

    internal uint PB17PUP
    {
        readonly get => (_bits >> 3) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 3)) | ((value & 0x1u) << 3);
    }

    internal uint PB18IO
    {
        readonly get => (_bits >> 4) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 4)) | ((value & 0x1u) << 4);
    }

    internal uint PB18PUP
    {
        readonly get => (_bits >> 5) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 5)) | ((value & 0x1u) << 5);
    }

    internal uint PB19IO
    {
        readonly get => (_bits >> 6) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 6)) | ((value & 0x1u) << 6);
    }

    internal uint PB19PUP
    {
        readonly get => (_bits >> 7) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 7)) | ((value & 0x1u) << 7);
    }
}

internal struct BSC_PDTRBRegister
{
    private uint _bits;

    internal ushort Raw { readonly get => (ushort)_bits; set => _bits = (_bits & 0xFFFF0000u) | value; }

    internal uint PB16DT
    {
        readonly get => (_bits >> 0) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 0)) | ((value & 0x1u) << 0);
    }

    internal uint PB17DT
    {
        readonly get => (_bits >> 1) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 1)) | ((value & 0x1u) << 1);
    }

    internal uint PB18DT
    {
        readonly get => (_bits >> 2) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 2)) | ((value & 0x1u) << 2);
    }

    internal uint PB19DT
    {
        readonly get => (_bits >> 3) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 3)) | ((value & 0x1u) << 3);
    }
}

internal struct BSC_GPIOICRegister
{
    private uint _bits;

    internal ushort Raw { readonly get => (ushort)_bits; set => _bits = (_bits & 0xFFFF0000u) | value; }

    internal uint PTIREN0
    {
        readonly get => (_bits >> 0) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 0)) | ((value & 0x1u) << 0);
    }

    internal uint PTIREN1
    {
        readonly get => (_bits >> 1) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 1)) | ((value & 0x1u) << 1);
    }

    internal uint PTIREN2
    {
        readonly get => (_bits >> 2) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 2)) | ((value & 0x1u) << 2);
    }

    internal uint PTIREN3
    {
        readonly get => (_bits >> 3) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 3)) | ((value & 0x1u) << 3);
    }

    internal uint PTIREN4
    {
        readonly get => (_bits >> 4) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 4)) | ((value & 0x1u) << 4);
    }

    internal uint PTIREN5
    {
        readonly get => (_bits >> 5) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 5)) | ((value & 0x1u) << 5);
    }

    internal uint PTIREN6
    {
        readonly get => (_bits >> 6) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 6)) | ((value & 0x1u) << 6);
    }

    internal uint PTIREN7
    {
        readonly get => (_bits >> 7) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 7)) | ((value & 0x1u) << 7);
    }

    internal uint PTIREN8
    {
        readonly get => (_bits >> 8) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 8)) | ((value & 0x1u) << 8);
    }

    internal uint PTIREN9
    {
        readonly get => (_bits >> 9) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 9)) | ((value & 0x1u) << 9);
    }

    internal uint PTIREN10
    {
        readonly get => (_bits >> 10) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 10)) | ((value & 0x1u) << 10);
    }

    internal uint PTIREN11
    {
        readonly get => (_bits >> 11) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 11)) | ((value & 0x1u) << 11);
    }

    internal uint PTIREN12
    {
        readonly get => (_bits >> 12) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 12)) | ((value & 0x1u) << 12);
    }

    internal uint PTIREN13
    {
        readonly get => (_bits >> 13) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 13)) | ((value & 0x1u) << 13);
    }

    internal uint PTIREN14
    {
        readonly get => (_bits >> 14) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 14)) | ((value & 0x1u) << 14);
    }

    internal uint PTIREN15
    {
        readonly get => (_bits >> 15) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 15)) | ((value & 0x1u) << 15);
    }
}

internal struct CCN_PTEHRegister
{
    private uint _bits;

    internal uint Raw { readonly get => _bits; set => _bits = value; }

    internal uint ASID
    {
        readonly get => (_bits >> 0) & 0xFFu;
        set => _bits = (_bits & ~(0xFFu << 0)) | ((value & 0xFFu) << 0);
    }

    internal uint VPN
    {
        readonly get => (_bits >> 10) & 0x3FFFFFu;
        set => _bits = (_bits & ~(0x3FFFFFu << 10)) | ((value & 0x3FFFFFu) << 10);
    }
}

internal struct CCN_PTELRegister
{
    private uint _bits;

    internal uint Raw { readonly get => _bits; set => _bits = value; }

    internal uint WT
    {
        readonly get => (_bits >> 0) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 0)) | ((value & 0x1u) << 0);
    }

    internal uint SH
    {
        readonly get => (_bits >> 1) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 1)) | ((value & 0x1u) << 1);
    }

    internal uint D
    {
        readonly get => (_bits >> 2) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 2)) | ((value & 0x1u) << 2);
    }

    internal uint C
    {
        readonly get => (_bits >> 3) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 3)) | ((value & 0x1u) << 3);
    }

    internal uint SZ0
    {
        readonly get => (_bits >> 4) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 4)) | ((value & 0x1u) << 4);
    }

    internal uint PR
    {
        readonly get => (_bits >> 5) & 0x3u;
        set => _bits = (_bits & ~(0x3u << 5)) | ((value & 0x3u) << 5);
    }

    internal uint SZ1
    {
        readonly get => (_bits >> 7) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 7)) | ((value & 0x1u) << 7);
    }

    internal uint V
    {
        readonly get => (_bits >> 8) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 8)) | ((value & 0x1u) << 8);
    }

    internal uint PPN
    {
        readonly get => (_bits >> 10) & 0x7FFFFu;
        set => _bits = (_bits & ~(0x7FFFFu << 10)) | ((value & 0x7FFFFu) << 10);
    }
}

internal struct CCN_MMUCRRegister
{
    private uint _bits;

    internal uint Raw { readonly get => _bits; set => _bits = value; }

    internal uint AT
    {
        readonly get => (_bits >> 0) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 0)) | ((value & 0x1u) << 0);
    }

    internal uint TI
    {
        readonly get => (_bits >> 2) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 2)) | ((value & 0x1u) << 2);
    }

    internal uint SV
    {
        readonly get => (_bits >> 8) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 8)) | ((value & 0x1u) << 8);
    }

    internal uint SQMD
    {
        readonly get => (_bits >> 9) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 9)) | ((value & 0x1u) << 9);
    }

    internal uint URC
    {
        readonly get => (_bits >> 10) & 0x3Fu;
        set => _bits = (_bits & ~(0x3Fu << 10)) | ((value & 0x3Fu) << 10);
    }

    internal uint URB
    {
        readonly get => (_bits >> 18) & 0x3Fu;
        set => _bits = (_bits & ~(0x3Fu << 18)) | ((value & 0x3Fu) << 18);
    }

    internal uint LRUI
    {
        readonly get => (_bits >> 26) & 0x3Fu;
        set => _bits = (_bits & ~(0x3Fu << 26)) | ((value & 0x3Fu) << 26);
    }
}

internal struct CCN_PTEARegister
{
    private uint _bits;

    internal uint Raw { readonly get => _bits; set => _bits = value; }

    internal uint SA
    {
        readonly get => (_bits >> 0) & 0x7u;
        set => _bits = (_bits & ~(0x7u << 0)) | ((value & 0x7u) << 0);
    }

    internal uint TC
    {
        readonly get => (_bits >> 3) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 3)) | ((value & 0x1u) << 3);
    }
}

internal struct CCN_CCRRegister
{
    private uint _bits;

    internal uint Raw { readonly get => _bits; set => _bits = value; }

    internal uint OCE
    {
        readonly get => (_bits >> 0) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 0)) | ((value & 0x1u) << 0);
    }

    internal uint WT
    {
        readonly get => (_bits >> 1) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 1)) | ((value & 0x1u) << 1);
    }

    internal uint CB
    {
        readonly get => (_bits >> 2) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 2)) | ((value & 0x1u) << 2);
    }

    internal uint OCI
    {
        readonly get => (_bits >> 3) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 3)) | ((value & 0x1u) << 3);
    }

    internal uint ORA
    {
        readonly get => (_bits >> 5) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 5)) | ((value & 0x1u) << 5);
    }

    internal uint OIX
    {
        readonly get => (_bits >> 7) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 7)) | ((value & 0x1u) << 7);
    }

    internal uint ICE
    {
        readonly get => (_bits >> 8) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 8)) | ((value & 0x1u) << 8);
    }

    internal uint ICI
    {
        readonly get => (_bits >> 11) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 11)) | ((value & 0x1u) << 11);
    }

    internal uint IIX
    {
        readonly get => (_bits >> 15) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 15)) | ((value & 0x1u) << 15);
    }
}

internal struct CCN_QACRRegister
{
    private uint _bits;

    internal uint Raw { readonly get => _bits; set => _bits = value; }

    internal uint Area
    {
        readonly get => (_bits >> 2) & 0x7u;
        set => _bits = (_bits & ~(0x7u << 2)) | ((value & 0x7u) << 2);
    }
}

internal struct DMAC_CHCRRegister
{
    private uint _bits;

    internal uint Raw { readonly get => _bits; set => _bits = value; }

    internal uint DE
    {
        readonly get => (_bits >> 0) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 0)) | ((value & 0x1u) << 0);
    }

    internal uint TE
    {
        readonly get => (_bits >> 1) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 1)) | ((value & 0x1u) << 1);
    }

    internal uint IE
    {
        readonly get => (_bits >> 2) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 2)) | ((value & 0x1u) << 2);
    }

    internal uint TS
    {
        readonly get => (_bits >> 4) & 0x7u;
        set => _bits = (_bits & ~(0x7u << 4)) | ((value & 0x7u) << 4);
    }

    internal uint TM
    {
        readonly get => (_bits >> 7) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 7)) | ((value & 0x1u) << 7);
    }

    internal uint RS
    {
        readonly get => (_bits >> 8) & 0xFu;
        set => _bits = (_bits & ~(0xFu << 8)) | ((value & 0xFu) << 8);
    }

    internal uint SM
    {
        readonly get => (_bits >> 12) & 0x3u;
        set => _bits = (_bits & ~(0x3u << 12)) | ((value & 0x3u) << 12);
    }

    internal uint DM
    {
        readonly get => (_bits >> 14) & 0x3u;
        set => _bits = (_bits & ~(0x3u << 14)) | ((value & 0x3u) << 14);
    }

    internal uint AL
    {
        readonly get => (_bits >> 16) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 16)) | ((value & 0x1u) << 16);
    }

    internal uint AM
    {
        readonly get => (_bits >> 17) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 17)) | ((value & 0x1u) << 17);
    }

    internal uint RL
    {
        readonly get => (_bits >> 18) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 18)) | ((value & 0x1u) << 18);
    }

    internal uint DS
    {
        readonly get => (_bits >> 19) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 19)) | ((value & 0x1u) << 19);
    }

    internal uint DTC
    {
        readonly get => (_bits >> 24) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 24)) | ((value & 0x1u) << 24);
    }

    internal uint DSA
    {
        readonly get => (_bits >> 25) & 0x7u;
        set => _bits = (_bits & ~(0x7u << 25)) | ((value & 0x7u) << 25);
    }

    internal uint STC
    {
        readonly get => (_bits >> 28) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 28)) | ((value & 0x1u) << 28);
    }

    internal uint SSA
    {
        readonly get => (_bits >> 29) & 0x7u;
        set => _bits = (_bits & ~(0x7u << 29)) | ((value & 0x7u) << 29);
    }
}

internal struct DMAC_DMAORRegister
{
    private uint _bits;

    internal uint Raw { readonly get => _bits; set => _bits = value; }

    internal uint DME
    {
        readonly get => (_bits >> 0) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 0)) | ((value & 0x1u) << 0);
    }

    internal uint NMIF
    {
        readonly get => (_bits >> 1) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 1)) | ((value & 0x1u) << 1);
    }

    internal uint AE
    {
        readonly get => (_bits >> 2) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 2)) | ((value & 0x1u) << 2);
    }

    internal uint COD
    {
        readonly get => (_bits >> 4) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 4)) | ((value & 0x1u) << 4);
    }

    internal uint PR0
    {
        readonly get => (_bits >> 8) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 8)) | ((value & 0x1u) << 8);
    }

    internal uint PR1
    {
        readonly get => (_bits >> 9) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 9)) | ((value & 0x1u) << 9);
    }

    internal uint DDT
    {
        readonly get => (_bits >> 15) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 15)) | ((value & 0x1u) << 15);
    }
}

internal struct SCIF_SCSMR2Register
{
    private uint _bits;

    internal ushort Raw { readonly get => (ushort)_bits; set => _bits = (_bits & 0xFFFF0000u) | value; }

    internal uint CKS
    {
        readonly get => (_bits >> 0) & 0x3u;
        set => _bits = (_bits & ~(0x3u << 0)) | ((value & 0x3u) << 0);
    }

    internal uint STOP
    {
        readonly get => (_bits >> 3) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 3)) | ((value & 0x1u) << 3);
    }

    internal uint OE_paritymode
    {
        readonly get => (_bits >> 4) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 4)) | ((value & 0x1u) << 4);
    }

    internal uint PE
    {
        readonly get => (_bits >> 5) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 5)) | ((value & 0x1u) << 5);
    }

    internal uint CHR
    {
        readonly get => (_bits >> 6) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 6)) | ((value & 0x1u) << 6);
    }
}

internal struct SCIF_SCSCR2Register
{
    private uint _bits;

    internal ushort Raw { readonly get => (ushort)_bits; set => _bits = (_bits & 0xFFFF0000u) | value; }

    internal uint CKE1
    {
        readonly get => (_bits >> 1) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 1)) | ((value & 0x1u) << 1);
    }

    internal uint REIE
    {
        readonly get => (_bits >> 3) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 3)) | ((value & 0x1u) << 3);
    }

    internal uint RE
    {
        readonly get => (_bits >> 4) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 4)) | ((value & 0x1u) << 4);
    }

    internal uint TE
    {
        readonly get => (_bits >> 5) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 5)) | ((value & 0x1u) << 5);
    }

    internal uint RIE
    {
        readonly get => (_bits >> 6) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 6)) | ((value & 0x1u) << 6);
    }

    internal uint TIE
    {
        readonly get => (_bits >> 7) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 7)) | ((value & 0x1u) << 7);
    }
}

internal struct SCIF_SCFSR2Register
{
    private uint _bits;

    internal ushort Raw { readonly get => (ushort)_bits; set => _bits = (_bits & 0xFFFF0000u) | value; }

    internal uint DR
    {
        readonly get => (_bits >> 0) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 0)) | ((value & 0x1u) << 0);
    }

    internal uint RDF
    {
        readonly get => (_bits >> 1) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 1)) | ((value & 0x1u) << 1);
    }

    internal uint PER
    {
        readonly get => (_bits >> 2) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 2)) | ((value & 0x1u) << 2);
    }

    internal uint FER
    {
        readonly get => (_bits >> 3) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 3)) | ((value & 0x1u) << 3);
    }

    internal uint BRK
    {
        readonly get => (_bits >> 4) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 4)) | ((value & 0x1u) << 4);
    }

    internal uint TDFE
    {
        readonly get => (_bits >> 5) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 5)) | ((value & 0x1u) << 5);
    }

    internal uint TEND
    {
        readonly get => (_bits >> 6) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 6)) | ((value & 0x1u) << 6);
    }

    internal uint ER
    {
        readonly get => (_bits >> 7) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 7)) | ((value & 0x1u) << 7);
    }

    internal uint FERn
    {
        readonly get => (_bits >> 8) & 0xFu;
        set => _bits = (_bits & ~(0xFu << 8)) | ((value & 0xFu) << 8);
    }

    internal uint PERn
    {
        readonly get => (_bits >> 12) & 0xFu;
        set => _bits = (_bits & ~(0xFu << 12)) | ((value & 0xFu) << 12);
    }
}

internal struct SCIF_SCFCR2Register
{
    private uint _bits;

    internal ushort Raw { readonly get => (ushort)_bits; set => _bits = (_bits & 0xFFFF0000u) | value; }

    internal uint LOOP
    {
        readonly get => (_bits >> 0) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 0)) | ((value & 0x1u) << 0);
    }

    internal uint RFRST
    {
        readonly get => (_bits >> 1) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 1)) | ((value & 0x1u) << 1);
    }

    internal uint TFRST
    {
        readonly get => (_bits >> 2) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 2)) | ((value & 0x1u) << 2);
    }

    internal uint MCE
    {
        readonly get => (_bits >> 3) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 3)) | ((value & 0x1u) << 3);
    }

    internal uint TTRG
    {
        readonly get => (_bits >> 4) & 0x3u;
        set => _bits = (_bits & ~(0x3u << 4)) | ((value & 0x3u) << 4);
    }

    internal uint RTRG
    {
        readonly get => (_bits >> 6) & 0x3u;
        set => _bits = (_bits & ~(0x3u << 6)) | ((value & 0x3u) << 6);
    }
}

internal struct SCIF_SCFDR2Register
{
    private uint _bits;

    internal ushort Raw { readonly get => (ushort)_bits; set => _bits = (_bits & 0xFFFF0000u) | value; }

    internal uint R
    {
        readonly get => (_bits >> 0) & 0x1Fu;
        set => _bits = (_bits & ~(0x1Fu << 0)) | ((value & 0x1Fu) << 0);
    }

    internal uint T
    {
        readonly get => (_bits >> 8) & 0x1Fu;
        set => _bits = (_bits & ~(0x1Fu << 8)) | ((value & 0x1Fu) << 8);
    }
}

internal struct SCIF_SCSPTR2Register
{
    private uint _bits;

    internal ushort Raw { readonly get => (ushort)_bits; set => _bits = (_bits & 0xFFFF0000u) | value; }

    internal uint SPB2DT
    {
        readonly get => (_bits >> 0) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 0)) | ((value & 0x1u) << 0);
    }

    internal uint SPB2IO
    {
        readonly get => (_bits >> 1) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 1)) | ((value & 0x1u) << 1);
    }

    internal uint CTSDT
    {
        readonly get => (_bits >> 4) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 4)) | ((value & 0x1u) << 4);
    }

    internal uint CTSIO
    {
        readonly get => (_bits >> 5) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 5)) | ((value & 0x1u) << 5);
    }

    internal uint RTSDT
    {
        readonly get => (_bits >> 6) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 6)) | ((value & 0x1u) << 6);
    }

    internal uint RTSIO
    {
        readonly get => (_bits >> 7) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 7)) | ((value & 0x1u) << 7);
    }
}

internal struct SCIF_SCLSR2Register
{
    private uint _bits;

    internal ushort Raw { readonly get => (ushort)_bits; set => _bits = (_bits & 0xFFFF0000u) | value; }

    internal uint ORER
    {
        readonly get => (_bits >> 0) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 0)) | ((value & 0x1u) << 0);
    }
}

internal struct INTC_ICRRegister
{
    private uint _bits;

    internal ushort Raw { readonly get => (ushort)_bits; set => _bits = (_bits & 0xFFFF0000u) | value; }

    internal uint IRLM
    {
        readonly get => (_bits >> 7) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 7)) | ((value & 0x1u) << 7);
    }

    internal uint NMIE
    {
        readonly get => (_bits >> 8) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 8)) | ((value & 0x1u) << 8);
    }

    internal uint NMIB
    {
        readonly get => (_bits >> 9) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 9)) | ((value & 0x1u) << 9);
    }

    internal uint MAI
    {
        readonly get => (_bits >> 14) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 14)) | ((value & 0x1u) << 14);
    }

    internal uint NMIL
    {
        readonly get => (_bits >> 15) & 0x1u;
        set => _bits = (_bits & ~(0x1u << 15)) | ((value & 0x1u) << 15);
    }
}

internal struct INTC_IPRARegister
{
    private uint _bits;

    internal ushort Raw { readonly get => (ushort)_bits; set => _bits = (_bits & 0xFFFF0000u) | value; }

    internal uint RTC
    {
        readonly get => (_bits >> 0) & 0xFu;
        set => _bits = (_bits & ~(0xFu << 0)) | ((value & 0xFu) << 0);
    }

    internal uint TMU2
    {
        readonly get => (_bits >> 4) & 0xFu;
        set => _bits = (_bits & ~(0xFu << 4)) | ((value & 0xFu) << 4);
    }

    internal uint TMU1
    {
        readonly get => (_bits >> 8) & 0xFu;
        set => _bits = (_bits & ~(0xFu << 8)) | ((value & 0xFu) << 8);
    }

    internal uint TMU0
    {
        readonly get => (_bits >> 12) & 0xFu;
        set => _bits = (_bits & ~(0xFu << 12)) | ((value & 0xFu) << 12);
    }
}

internal struct INTC_IPRBRegister
{
    private uint _bits;

    internal ushort Raw { readonly get => (ushort)_bits; set => _bits = (_bits & 0xFFFF0000u) | value; }

    internal uint SCI1
    {
        readonly get => (_bits >> 4) & 0xFu;
        set => _bits = (_bits & ~(0xFu << 4)) | ((value & 0xFu) << 4);
    }

    internal uint REF
    {
        readonly get => (_bits >> 8) & 0xFu;
        set => _bits = (_bits & ~(0xFu << 8)) | ((value & 0xFu) << 8);
    }

    internal uint WDT
    {
        readonly get => (_bits >> 12) & 0xFu;
        set => _bits = (_bits & ~(0xFu << 12)) | ((value & 0xFu) << 12);
    }
}

internal struct INTC_IPRCRegister
{
    private uint _bits;

    internal ushort Raw { readonly get => (ushort)_bits; set => _bits = (_bits & 0xFFFF0000u) | value; }

    internal uint Hitachi_UDI
    {
        readonly get => (_bits >> 0) & 0xFu;
        set => _bits = (_bits & ~(0xFu << 0)) | ((value & 0xFu) << 0);
    }

    internal uint SCIF
    {
        readonly get => (_bits >> 4) & 0xFu;
        set => _bits = (_bits & ~(0xFu << 4)) | ((value & 0xFu) << 4);
    }

    internal uint DMAC
    {
        readonly get => (_bits >> 8) & 0xFu;
        set => _bits = (_bits & ~(0xFu << 8)) | ((value & 0xFu) << 8);
    }

    internal uint GPIO
    {
        readonly get => (_bits >> 12) & 0xFu;
        set => _bits = (_bits & ~(0xFu << 12)) | ((value & 0xFu) << 12);
    }
}

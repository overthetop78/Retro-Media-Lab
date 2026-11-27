using System.Diagnostics;
using System.IO;
using RetroMediaLab.Chips.Audio.Yamaha.AICA;
using RetroMediaLab.Chips.CPU.Hitachi.SH4;
using RetroMediaLab.Chips.Video.NEC.PowerVr2;
using static RetroMediaLab.Chips.Dreamcast.Holly.HollySystemBus;

namespace RetroMediaLab.Chips.Dreamcast.Holly;

internal sealed partial class HollySystemBus
{
    private static readonly SortedDictionary<uint, string> RegisterNames = new()
    {
        [SB_C2DSTAT_addr] = nameof(SB_C2DSTAT),
        [SB_C2DLEN_addr] = nameof(SB_C2DLEN),
        [SB_C2DST_addr] = nameof(SB_C2DST),
        [SB_SDSTAW_addr] = nameof(SB_SDSTAW),
        [SB_SDBAAW_addr] = nameof(SB_SDBAAW),
        [SB_SDWLT_addr] = nameof(SB_SDWLT),
        [SB_SDLAS_addr] = nameof(SB_SDLAS),
        [SB_SDST_addr] = nameof(SB_SDST),
        [SB_SDDIV_addr] = nameof(SB_SDDIV),
        [SB_DBREQM_addr] = nameof(SB_DBREQM),
        [SB_BAVLWC_addr] = nameof(SB_BAVLWC),
        [SB_C2DPRYC_addr] = nameof(SB_C2DPRYC),
        [SB_C2DMAXL_addr] = nameof(SB_C2DMAXL),
        [SB_TFREM_addr] = nameof(SB_TFREM),
        [SB_LMMODE0_addr] = nameof(SB_LMMODE0),
        [SB_LMMODE1_addr] = nameof(SB_LMMODE1),
        [SB_FFST_addr] = nameof(SB_FFST),
        [SB_SFRES_addr] = nameof(SB_SFRES),
        [SB_SBREV_addr] = nameof(SB_SBREV),
        [SB_RBSPLT_addr] = nameof(SB_RBSPLT),
        [SB_ISTNRM_addr] = nameof(SB_ISTNRM),
        [SB_ISTEXT_addr] = nameof(SB_ISTEXT),
        [SB_ISTERR_addr] = nameof(SB_ISTERR),
        [SB_IML2NRM_addr] = nameof(SB_IML2NRM),
        [SB_IML2EXT_addr] = nameof(SB_IML2EXT),
        [SB_IML2ERR_addr] = nameof(SB_IML2ERR),
        [SB_IML4NRM_addr] = nameof(SB_IML4NRM),
        [SB_IML4EXT_addr] = nameof(SB_IML4EXT),
        [SB_IML4ERR_addr] = nameof(SB_IML4ERR),
        [SB_IML6NRM_addr] = nameof(SB_IML6NRM),
        [SB_IML6EXT_addr] = nameof(SB_IML6EXT),
        [SB_IML6ERR_addr] = nameof(SB_IML6ERR),
        [SB_PDTNRM_addr] = nameof(SB_PDTNRM),
        [SB_PDTEXT_addr] = nameof(SB_PDTEXT),
        [SB_G2DTNRM_addr] = nameof(SB_G2DTNRM),
        [SB_G2DTEXT_addr] = nameof(SB_G2DTEXT),
        [SB_MDSTAR_addr] = nameof(SB_MDSTAR),
        [SB_MDTSEL_addr] = nameof(SB_MDTSEL),
        [SB_MDEN_addr] = nameof(SB_MDEN),
        [SB_MDST_addr] = nameof(SB_MDST),
        [SB_MSYS_addr] = nameof(SB_MSYS),
        [SB_MST_addr] = nameof(SB_MST),
        [SB_MSHTCL_addr] = nameof(SB_MSHTCL),
        [SB_MDAPRO_addr] = nameof(SB_MDAPRO),
        [SB_MMSEL_addr] = nameof(SB_MMSEL),
        [SB_MTXDAD_addr] = nameof(SB_MTXDAD),
        [SB_MRXDAD_addr] = nameof(SB_MRXDAD),
        [SB_MRXDBD_addr] = nameof(SB_MRXDBD),
        [SB_GDSTAR_addr] = nameof(SB_GDSTAR),
        [SB_GDLEN_addr] = nameof(SB_GDLEN),
        [SB_GDDIR_addr] = nameof(SB_GDDIR),
        [SB_GDEN_addr] = nameof(SB_GDEN),
        [SB_GDST_addr] = nameof(SB_GDST),
        [SB_G1RRC_addr] = nameof(SB_G1RRC),
        [SB_G1RWC_addr] = nameof(SB_G1RWC),
        [SB_G1FRC_addr] = nameof(SB_G1FRC),
        [SB_G1FWC_addr] = nameof(SB_G1FWC),
        [SB_G1CRC_addr] = nameof(SB_G1CRC),
        [SB_G1CWC_addr] = nameof(SB_G1CWC),
        [SB_G1GDRC_addr] = nameof(SB_G1GDRC),
        [SB_G1GDWC_addr] = nameof(SB_G1GDWC),
        [SB_G1SYSM_addr] = nameof(SB_G1SYSM),
        [SB_G1CRDYC_addr] = nameof(SB_G1CRDYC),
        [SB_GDAPRO_addr] = nameof(SB_GDAPRO),
        [SB_GDSTARD_addr] = nameof(SB_GDSTARD),
        [SB_GDLEND_addr] = nameof(SB_GDLEND),
        [SB_ADSTAG_addr] = nameof(SB_ADSTAG),
        [SB_ADSTAR_addr] = nameof(SB_ADSTAR),
        [SB_ADLEN_addr] = nameof(SB_ADLEN),
        [SB_ADDIR_addr] = nameof(SB_ADDIR),
        [SB_ADTSEL_addr] = nameof(SB_ADTSEL),
        [SB_ADEN_addr] = nameof(SB_ADEN),
        [SB_ADST_addr] = nameof(SB_ADST),
        [SB_ADSUSP_addr] = nameof(SB_ADSUSP),
        [SB_E1STAG_addr] = nameof(SB_E1STAG),
        [SB_E1STAR_addr] = nameof(SB_E1STAR),
        [SB_E1LEN_addr] = nameof(SB_E1LEN),
        [SB_E1DIR_addr] = nameof(SB_E1DIR),
        [SB_E1TSEL_addr] = nameof(SB_E1TSEL),
        [SB_E1EN_addr] = nameof(SB_E1EN),
        [SB_E1ST_addr] = nameof(SB_E1ST),
        [SB_E1SUSP_addr] = nameof(SB_E1SUSP),
        [SB_E2STAG_addr] = nameof(SB_E2STAG),
        [SB_E2STAR_addr] = nameof(SB_E2STAR),
        [SB_E2LEN_addr] = nameof(SB_E2LEN),
        [SB_E2DIR_addr] = nameof(SB_E2DIR),
        [SB_E2TSEL_addr] = nameof(SB_E2TSEL),
        [SB_E2EN_addr] = nameof(SB_E2EN),
        [SB_E2ST_addr] = nameof(SB_E2ST),
        [SB_E2SUSP_addr] = nameof(SB_E2SUSP),
        [SB_DDSTAG_addr] = nameof(SB_DDSTAG),
        [SB_DDSTAR_addr] = nameof(SB_DDSTAR),
        [SB_DDLEN_addr] = nameof(SB_DDLEN),
        [SB_DDDIR_addr] = nameof(SB_DDDIR),
        [SB_DDTSEL_addr] = nameof(SB_DDTSEL),
        [SB_DDEN_addr] = nameof(SB_DDEN),
        [SB_DDST_addr] = nameof(SB_DDST),
        [SB_DDSUSP_addr] = nameof(SB_DDSUSP),
        [SB_G2ID_addr] = nameof(SB_G2ID),
        [SB_G2DSTO_addr] = nameof(SB_G2DSTO),
        [SB_G2TRTO_addr] = nameof(SB_G2TRTO),
        [SB_G2MDMTO_addr] = nameof(SB_G2MDMTO),
        [SB_G2MDMW_addr] = nameof(SB_G2MDMW),
        [SB_G2APRO_addr] = nameof(SB_G2APRO),
        [SB_ADSTAGD_addr] = nameof(SB_ADSTAGD),
        [SB_ADSTARD_addr] = nameof(SB_ADSTARD),
        [SB_ADLEND_addr] = nameof(SB_ADLEND),
        [SB_E1STAGD_addr] = nameof(SB_E1STAGD),
        [SB_E1STARD_addr] = nameof(SB_E1STARD),
        [SB_E1LEND_addr] = nameof(SB_E1LEND),
        [SB_E2STAGD_addr] = nameof(SB_E2STAGD),
        [SB_E2STARD_addr] = nameof(SB_E2STARD),
        [SB_E2LEND_addr] = nameof(SB_E2LEND),
        [SB_DDSTAGD_addr] = nameof(SB_DDSTAGD),
        [SB_DDSTARD_addr] = nameof(SB_DDSTARD),
        [SB_DDLEND_addr] = nameof(SB_DDLEND),
        [SB_PDSTAP_addr] = nameof(SB_PDSTAP),
        [SB_PDSTAR_addr] = nameof(SB_PDSTAR),
        [SB_PDLEN_addr] = nameof(SB_PDLEN),
        [SB_PDDIR_addr] = nameof(SB_PDDIR),
        [SB_PDTSEL_addr] = nameof(SB_PDTSEL),
        [SB_PDEN_addr] = nameof(SB_PDEN),
        [SB_PDST_addr] = nameof(SB_PDST),
        [SB_PDAPRO_addr] = nameof(SB_PDAPRO),
        [SB_PDSTAPD_addr] = nameof(SB_PDSTAPD),
        [SB_PDSTARD_addr] = nameof(SB_PDSTARD),
        [SB_PDLEND_addr] = nameof(SB_PDLEND),
    };

    private static string RegisterName(uint address) => RegisterNames.TryGetValue(address & 0x7fffff, out string? name)
        ? name
        : $"?{address & 0x7fffff:x6}";

    internal static uint Read(uint address)
    {
        uint value = Registers.Read<uint>(address);
        if ((address & 0xffffff) != 0x5f6c18)
            Debug.WriteLine($"read {RegisterName(address)}.{(((address >> 26) & 7) == 2 ? 'b' : (address & 0x2000000) != 0 ? '1' : '0')} == {value:x}");
        return value;
    }

    internal static void Write(uint address, uint data)
    {
        Debug.WriteLine($"write {RegisterName(address)}.{(((address >> 26) & 7) == 2 ? 'b' : (address & 0x2000000) != 0 ? '1' : '0')} = {data:x}");
        Registers.Write(address, data);
    }

    internal static void Init()
    {
        Registers.Initialize();
        HollyInterruptController.Init();
        GdRomInterface.RegInit();
        NaomiInterface.RegInit();
        PvrSystemBusRegisters.Init();
        MapleInterface.Init();
        AicaInterface.SbInit();
        ModemInterface.Init();
    }

    internal static void Reset(bool hard)
    {
        if (hard)
        {
            Registers.Reset();
            SB_SBREV = 0xB;
            SB_G2ID = 0x12;
            SB_G1SYSM = (0u << 4) | 1;
            SB_TFREM = 8;
            SB_PDAPRO = 0x7f00;
            SB_GDAPRO = 0x7f00;
        }
        SB_ISTNRM = 0;
        SB_ISTNRM1 = 0;
        BroadbandAdapter.Reset(hard);
        ModemInterface.Reset();
        HollyInterruptController.Reset(hard);
        if (Sh4DreamcastRuntime.Platform.IsConsole)
            GdRomInterface.RegReset(hard);
        else
            NaomiInterface.RegReset(hard);
        PvrSystemBusRegisters.Reset(hard);
        MapleInterface.Reset(hard);
        AicaInterface.SbReset(hard);
    }

    internal static void Term()
    {
        BroadbandAdapter.Term();
        ModemInterface.Term();
        AicaInterface.SbTerm();
        MapleInterface.Term();
        PvrSystemBusRegisters.Term();
        GdRomInterface.RegTerm();
        NaomiInterface.RegTerm();
        HollyInterruptController.Term();
        Registers.Terminate();
    }

    internal static void Serialize(BinaryWriter writer)
    {
        foreach (uint value in Registers.Registers)
            writer.Write(value);
        writer.Write(SB_ISTNRM1);
    }

    internal static void Deserialize(BinaryReader reader, int version)
    {
        Span<uint> registers = Registers.Registers;
        for (int i = 0; i < registers.Length; i++)
            registers[i] = reader.ReadUInt32();
        if (version < 828)
            SB_ISTNRM = reader.ReadUInt32();
        if (version >= 819)
            SB_ISTNRM1 = reader.ReadUInt32();
        else
            SB_ISTNRM1 = 0;
        if (version < 828)
        {
            if (version < 825)
            {
                _ = reader.ReadUInt32();
                _ = reader.ReadUInt32();
            }
            SB_ADST = reader.ReadUInt32();
        }
    }
}

internal sealed partial class HollyRegisters
{
    private static void WriteZero(uint address, uint data)
    {
        if (data != 0)
            Trace.TraceInformation($"ERROR: non-zero write on register; offset={address - SB_BASE:x}, data={data:x}");
    }

    private static void WriteGdRomUnlock(uint address, uint data)
    {
        if (data != 0 && data != 0x001fffff && data != 0x42fe && data != 0xa677 && data != 0x3ff)
            Trace.TraceWarning($"ERROR: Unexpected GD-ROM unlock code: {data:x}");
    }

    private static uint ReadFifoStatus(uint address) => 0;

    private static void WriteSystemReset(uint address, uint data)
    {
        if ((ushort)data == 0x7611)
        {
            Trace.TraceInformation("SB/HOLLY: System reset requested");
            Sh4DreamcastRuntime.RequestReset();
        }
    }

    private static void WriteSuspend(uint registerAddress, uint data)
    {
        ref uint register = ref Register32(registerAddress);
        register = (register & 0xfffffffe) | (data & 1);
    }

    internal override void Initialize()
    {
        base.Initialize();
        SetReadWrite(SB_C2DSTAT_addr, sizeof(uint), 0x03ffffe0, 0x10000000);
        SetReadWrite(SB_C2DLEN_addr, sizeof(uint), 0x00ffffe0);
        SetReadWrite(SB_SDSTAW_addr, sizeof(uint), 0x07ffffe0, 0x08000000);
        SetReadWrite(SB_SDBAAW_addr, sizeof(uint), 0x07ffffe0, 0x08000000);
        SetReadWrite(SB_SDWLT_addr, sizeof(uint), 1);
        SetReadWrite(SB_SDLAS_addr, sizeof(uint), 1);
        SetReadOnly<uint>(SB_SDDIV_addr);
        SetReadWrite(SB_DBREQM_addr, sizeof(uint), 1);
        SetReadWrite(SB_BAVLWC_addr, sizeof(uint), 0x1f);
        SetReadWrite(SB_C2DPRYC_addr, sizeof(uint), 0xf);
        SetReadWrite(SB_C2DMAXL_addr, sizeof(uint), 3);
        SetReadOnly<uint>(SB_TFREM_addr);
        SetReadWrite(SB_LMMODE0_addr, sizeof(uint), 1);
        SetReadWrite(SB_LMMODE1_addr, sizeof(uint), 1);
        SetReadOnly<uint>(SB_FFST_addr, ReadFifoStatus);
        SetWriteOnly<uint>(SB_SFRES_addr, WriteSystemReset);
        SetReadOnly<uint>(SB_SBREV_addr);
        SetReadWrite(SB_RBSPLT_addr, sizeof(uint), 0x80000000);
        SetReadWrite(SB_PDTNRM_addr, sizeof(uint), 0x003fffff);
        SetReadWrite(SB_PDTEXT_addr, sizeof(uint), 0xf);
        SetReadWrite(SB_G2DTNRM_addr, sizeof(uint), 0x003fffff);
        SetReadWrite(SB_G2DTEXT_addr, sizeof(uint), 0xf);
#if !STRICT_MODE
        SetReadWrite(SB_MDSTAR_addr, sizeof(uint), 0x1fffffe0);
#endif
        SetReadWrite(SB_MDTSEL_addr, sizeof(uint), 1);
        SetReadWrite(SB_MSYS_addr, sizeof(uint), 0xffff130f);
        SetReadOnly<uint>(SB_MST_addr);
        SetReadWrite(SB_MMSEL_addr, sizeof(uint), 1);
        SetReadOnly<uint>(SB_MTXDAD_addr);
        SetReadOnly<uint>(SB_MRXDAD_addr);
        SetReadOnly<uint>(SB_MRXDBD_addr);
        SetReadWrite(SB_GDSTAR_addr, sizeof(uint), 0x1fffffe0);
        SetReadWrite(SB_GDLEN_addr, sizeof(uint), 0x01ffffff);
        SetReadWrite(SB_GDDIR_addr, sizeof(uint), 1);
        SetWriteOnly<uint>(SB_G1RRC_addr);
        SetWriteOnly<uint>(SB_G1RWC_addr);
        SetWriteOnly<uint>(SB_G1FRC_addr);
        SetWriteOnly<uint>(SB_G1FWC_addr);
        SetWriteOnly<uint>(SB_G1CRC_addr);
        SetWriteOnly<uint>(SB_G1CWC_addr);
        SetWriteOnly<uint>(SB_G1GDRC_addr);
        SetWriteOnly<uint>(SB_G1GDWC_addr);
        SetReadWrite(SB_G1SYSM_addr, sizeof(uint));
        SetWriteOnly<uint>(SB_G1CRDYC_addr);
        SetWriteOnly<uint>(SB_GDAPRO_addr);
        SetReadOnly<uint>(SB_GDSTARD_addr);
        SetReadOnly<uint>(SB_GDLEND_addr);
#if !STRICT_MODE
        SetReadWrite(SB_ADSTAG_addr, sizeof(uint), 0x1fffffe0);
        SetReadWrite(SB_ADSTAR_addr, sizeof(uint), 0x1fffffe0);
#endif
        SetReadWrite(SB_ADLEN_addr, sizeof(uint), 0x81FFFFE0);
        SetReadWrite(SB_ADDIR_addr, sizeof(uint), 1);
        SetReadWrite(SB_ADTSEL_addr, sizeof(uint), 7);
        SetReadWrite(SB_ADEN_addr, sizeof(uint), 1);
        SetWriteHandler<uint>(SB_ADSUSP_addr, (_, data) => WriteSuspend(SB_ADSUSP_addr, data));
        SetReadWrite(SB_E1LEN_addr, sizeof(uint), 0x81FFFFE0);
        SetReadWrite(SB_E1DIR_addr, sizeof(uint), 1);
        SetReadWrite(SB_E1TSEL_addr, sizeof(uint), 7);
        SetReadWrite(SB_E1EN_addr, sizeof(uint), 1);
        SetWriteHandler<uint>(SB_E1SUSP_addr, (_, data) => WriteSuspend(SB_E1SUSP_addr, data));
        SetReadWrite(SB_E2LEN_addr, sizeof(uint), 0x81FFFFE0);
        SetReadWrite(SB_E2DIR_addr, sizeof(uint), 1);
        SetReadWrite(SB_E2TSEL_addr, sizeof(uint), 7);
        SetReadWrite(SB_E2EN_addr, sizeof(uint), 1);
        SetWriteHandler<uint>(SB_E2SUSP_addr, (_, data) => WriteSuspend(SB_E2SUSP_addr, data));
        SetReadWrite(SB_DDLEN_addr, sizeof(uint), 0x81FFFFE0);
        SetReadWrite(SB_DDDIR_addr, sizeof(uint), 1);
        SetReadWrite(SB_DDTSEL_addr, sizeof(uint), 7);
        SetReadWrite(SB_DDEN_addr, sizeof(uint), 1);
        SetWriteHandler<uint>(SB_DDSUSP_addr, (_, data) => WriteSuspend(SB_DDSUSP_addr, data));
        SetReadOnly<uint>(SB_G2ID_addr);
        SetReadWrite(SB_G2DSTO_addr, sizeof(uint));
        SetReadWrite(SB_G2TRTO_addr, sizeof(uint));
        SetReadWrite(SB_G2MDMTO_addr, sizeof(uint), 8);
        SetReadWrite(SB_G2MDMW_addr, sizeof(uint), 8);
        SetReadOnly<uint>(SB_ADSTAGD_addr);
        SetReadOnly<uint>(SB_ADSTARD_addr);
        SetReadOnly<uint>(SB_ADLEND_addr);
        SetReadOnly<uint>(SB_E1STAGD_addr);
        SetReadOnly<uint>(SB_E1STARD_addr);
        SetReadOnly<uint>(SB_E1LEND_addr);
        SetReadOnly<uint>(SB_E2STAGD_addr);
        SetReadOnly<uint>(SB_E2STARD_addr);
        SetReadOnly<uint>(SB_E2LEND_addr);
        SetReadOnly<uint>(SB_DDSTAGD_addr);
        SetReadOnly<uint>(SB_DDSTARD_addr);
        SetReadOnly<uint>(SB_DDLEND_addr);
        SetReadWrite(SB_PDSTAP_addr, sizeof(uint), 0x1fffffe0);
        SetReadWrite(SB_PDSTAR_addr, sizeof(uint), 0x1fffffe0);
        SetReadWrite(SB_PDLEN_addr, sizeof(uint), 0x00ffffe0);
        SetReadWrite(SB_PDDIR_addr, sizeof(uint), 1);
        SetReadWrite(SB_PDTSEL_addr, sizeof(uint), 1);
        SetReadWrite(SB_PDEN_addr, sizeof(uint), 1);
        SetWriteOnly<uint>(SB_PDAPRO_addr);
        SetReadOnly<uint>(SB_PDSTAPD_addr);
        SetReadOnly<uint>(SB_PDSTARD_addr);
        SetReadOnly<uint>(SB_PDLEND_addr);
        SetWriteOnly<uint>(0x005f74e4, WriteGdRomUnlock);
        SetWriteOnly<uint>(0x005f68a4, WriteZero);
        SetWriteOnly<uint>(0x005f68ac, WriteZero);
        SetWriteOnly<uint>(0x005f78a0, WriteZero);
        SetWriteOnly<uint>(0x005f78a4, WriteZero);
        SetWriteOnly<uint>(0x005f78a8, WriteZero);
        SetWriteOnly<uint>(0x005f78ac, WriteZero);
        SetWriteOnly<uint>(0x005f78b0, WriteZero);
        SetWriteOnly<uint>(0x005f78b4, WriteZero);
        SetWriteOnly<uint>(0x005f78b8, WriteZero);
        Reset();
    }
}

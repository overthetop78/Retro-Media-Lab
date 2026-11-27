using System.Collections.Generic;
using System.Diagnostics;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal static unsafe partial class PvrRegisters
{
    internal static bool PalNeedsUpdate = true;
    internal static readonly byte[] RegisterBytes = GC.AllocateArray<byte>(RegisterSize, pinned: true);

    private static readonly SortedDictionary<uint, string> RegisterNames = new()
    {
        [ID_addr] = "ID",
        [REVISION_addr] = "REVISION",
        [SOFTRESET_addr] = "SOFTRESET",
        [STARTRENDER_addr] = "STARTRENDER",
        [TEST_SELECT_addr] = "TEST_SELECT",
        [PARAM_BASE_addr] = "PARAM_BASE",
        [REGION_BASE_addr] = "REGION_BASE",
        [SPAN_SORT_CFG_addr] = "SPAN_SORT_CFG",
        [VO_BORDER_COL_addr] = "VO_BORDER_COL",
        [FB_R_CTRL_addr] = "FB_R_CTRL",
        [FB_W_CTRL_addr] = "FB_W_CTRL",
        [FB_W_LINESTRIDE_addr] = "FB_W_LINESTRIDE",
        [FB_R_SOF1_addr] = "FB_R_SOF1",
        [FB_R_SOF2_addr] = "FB_R_SOF2",
        [FB_R_SIZE_addr] = "FB_R_SIZE",
        [FB_W_SOF1_addr] = "FB_W_SOF1",
        [FB_W_SOF2_addr] = "FB_W_SOF2",
        [FB_X_CLIP_addr] = "FB_X_CLIP",
        [FB_Y_CLIP_addr] = "FB_Y_CLIP",
        [FPU_SHAD_SCALE_addr] = "FPU_SHAD_SCALE",
        [FPU_CULL_VAL_addr] = "FPU_CULL_VAL",
        [FPU_PARAM_CFG_addr] = "FPU_PARAM_CFG",
        [HALF_OFFSET_addr] = "HALF_OFFSET",
        [FPU_PERP_VAL_addr] = "FPU_PERP_VAL",
        [ISP_BACKGND_D_addr] = "ISP_BACKGND_D",
        [ISP_BACKGND_T_addr] = "ISP_BACKGND_T",
        [ISP_FEED_CFG_addr] = "ISP_FEED_CFG",
        [SDRAM_REFRESH_addr] = "SDRAM_REFRESH",
        [SDRAM_ARB_CFG_addr] = "SDRAM_ARB_CFG",
        [SDRAM_CFG_addr] = "SDRAM_CFG",
        [FOG_COL_RAM_addr] = "FOG_COL_RAM",
        [FOG_COL_VERT_addr] = "FOG_COL_VERT",
        [FOG_DENSITY_addr] = "FOG_DENSITY",
        [FOG_CLAMP_MAX_addr] = "FOG_CLAMP_MAX",
        [FOG_CLAMP_MIN_addr] = "FOG_CLAMP_MIN",
        [SPG_TRIGGER_POS_addr] = "SPG_TRIGGER_POS",
        [SPG_HBLANK_INT_addr] = "SPG_HBLANK_INT",
        [SPG_VBLANK_INT_addr] = "SPG_VBLANK_INT",
        [SPG_CONTROL_addr] = "SPG_CONTROL",
        [SPG_HBLANK_addr] = "SPG_HBLANK",
        [SPG_LOAD_addr] = "SPG_LOAD",
        [SPG_VBLANK_addr] = "SPG_VBLANK",
        [SPG_WIDTH_addr] = "SPG_WIDTH",
        [TEXT_CONTROL_addr] = "TEXT_CONTROL",
        [VO_CONTROL_addr] = "VO_CONTROL",
        [VO_STARTX_addr] = "VO_STARTX",
        [VO_STARTY_addr] = "VO_STARTY",
        [SCALER_CTL_addr] = "SCALER_CTL",
        [PAL_RAM_CTRL_addr] = "PAL_RAM_CTRL",
        [SPG_STATUS_addr] = "SPG_STATUS",
        [FB_BURSTCTRL_addr] = "FB_BURSTCTRL",
        [FB_C_SOF_addr] = "FB_C_SOF",
        [Y_COEFF_addr] = "Y_COEFF",
        [PT_ALPHA_REF_addr] = "PT_ALPHA_REF",
        [TA_OL_BASE_addr] = "TA_OL_BASE",
        [TA_ISP_BASE_addr] = "TA_ISP_BASE",
        [TA_OL_LIMIT_addr] = "TA_OL_LIMIT",
        [TA_ISP_LIMIT_addr] = "TA_ISP_LIMIT",
        [TA_NEXT_OPB_addr] = "TA_NEXT_OPB",
        [TA_ITP_CURRENT_addr] = "TA_ITP_CURRENT",
        [TA_GLOB_TILE_CLIP_addr] = "TA_GLOB_TILE_CLIP",
        [TA_ALLOC_CTRL_addr] = "TA_ALLOC_CTRL",
        [TA_LIST_INIT_addr] = "TA_LIST_INIT",
        [TA_YUV_TEX_BASE_addr] = "TA_YUV_TEX_BASE",
        [TA_YUV_TEX_CTRL_addr] = "TA_YUV_TEX_CTRL",
        [TA_YUV_TEX_CNT_addr] = "TA_YUV_TEX_CNT",
        [TA_LIST_CONT_addr] = "TA_LIST_CONT",
        [TA_NEXT_OPB_INIT_addr] = "TA_NEXT_OPB_INIT",
        [SIGNATURE1_addr] = "SIGNATURE1",
        [SIGNATURE2_addr] = "SIGNATURE2",
    };

    private static string RegName(uint physicalAddress)
    {
        uint address = physicalAddress & RegisterMask;
        if (!RegisterNames.TryGetValue(address, out string? name))
        {
            if (address >= FOG_TABLE_START_addr && address <= FOG_TABLE_END_addr)
                return $"FOG_TABLE[{address - FOG_TABLE_START_addr:x}]";
            else if (address >= TA_OL_POINTERS_START_addr && address <= TA_OL_POINTERS_END_addr)
                return $"TA_OL_POINTERS[{address - TA_OL_POINTERS_START_addr:x}]";
            else if (address >= PALETTE_RAM_START_addr && address <= PALETTE_RAM_END_addr)
                return $"PALETTE[{address - PALETTE_RAM_START_addr:x}]";
            else
                return $"?{physicalAddress:x8}";
        }
        else
        {
            return name;
        }
    }

    internal static uint Read(uint address)
    {
        if ((address & RegisterMask) != SPG_STATUS_addr)
            Debug.WriteLine($"read {RegName(address)}.{(((address >> 26) & 7) == 2 ? 'b' : (address & 0x2000000) != 0 ? '1' : '0')} == {PvrReg<uint>(address):x}");
        return PvrReg<uint>(address);
    }

    internal static void Write(uint physicalAddress, uint data)
    {
        uint address = physicalAddress & RegisterMask;
        Debug.WriteLine($"write {RegName(physicalAddress)}.{(((physicalAddress >> 26) & 7) == 2 ? 'b' : (physicalAddress & 0x2000000) != 0 ? '1' : '0')} = {data:x}");

        switch (address)
        {
            case ID_addr:
            case REVISION_addr:
            case TA_YUV_TEX_CNT_addr:
                return;

            case STARTRENDER_addr:
                PvrRenderer.StartRender();
                PvrMemory.YuvInit();
                return;

            case TA_LIST_INIT_addr:
                if ((data >> 31) != 0)
                {
                    PowerVr2Ta.VertexListInit(false);
                    TA_NEXT_OPB = TA_NEXT_OPB_INIT;
                    TA_ITP_CURRENT = TA_ISP_BASE;
                }
                return;

            case SOFTRESET_addr:
                if ((data & 1) != 0)
                    PowerVr2Ta.VertexSoftReset();
                return;

            case TA_LIST_CONT_addr:
                PowerVr2Ta.VertexListInit(true);
                break;

            case SPG_CONTROL_addr:
            case SPG_LOAD_addr:
                if (PvrReg<uint>(address) != data)
                {
                    PvrReg<uint>(address) = data;
                    PvrScanGenerator.CalculateSync();
                }
                return;

            case FB_R_CTRL_addr:
            {
                bool clockDividerChanged = ((PvrReg<uint>(address) ^ data) & (1 << 23)) != 0;
                PvrReg<uint>(address) = data;
                if (clockDividerChanged)
                    PvrScanGenerator.CalculateSync();
                return;
            }

            case FB_R_SIZE_addr:
                if (PvrReg<uint>(address) != data)
                {
                    PvrReg<uint>(address) = data;
                    PvrRenderer.FramebufferDirty = false;
                    PvrRenderer.CheckFramebufferWrite();
                }
                return;

            case TA_YUV_TEX_BASE_addr:
                PvrReg<uint>(address) = data & 0x00fffff8;
                PvrMemory.YuvInit();
                return;

            case TA_YUV_TEX_CTRL_addr:
                PvrReg<uint>(address) = data;
                PvrMemory.YuvInit();
                return;

            case FB_R_SOF1_addr:
            case FB_R_SOF2_addr:
                data &= 0x00fffffc;
                PvrRenderer.SwapFrame(data);
                break;

            case FB_W_SOF1_addr:
                data &= 0x01fffffc;
                PvrRenderer.SetFramebufferWriteAddress(data);
                break;

            case FB_W_SOF2_addr:
                data &= 0x01fffffc;
                break;

            case SPG_HBLANK_INT_addr:
                data &= 0x03ff33ff;
                if (data != SPG_HBLANK_INT.full)
                {
                    SPG_HBLANK_INT.full = data;
                    PvrScanGenerator.Reschedule();
                }
                return;

            case PAL_RAM_CTRL_addr:
                PalNeedsUpdate = PalNeedsUpdate || ((data ^ PAL_RAM_CTRL) & 3) != 0;
                break;

            default:
                if (address >= PALETTE_RAM_START_addr && PvrReg<uint>(address) != data)
                    PalNeedsUpdate = true;
                else if (address >= FOG_TABLE_START_addr && address <= FOG_TABLE_END_addr && PvrReg<uint>(address) != data)
                    PvrRenderer.UpdateFogTable();
                break;
        }

        PvrReg<uint>(address) = data;
    }

    internal static void Reset(bool hard)
    {
        if (hard)
            Array.Clear(RegisterBytes);
        ID_Reg = 0x17FD11DB;
        REVISION = 0x00000011;
        SOFTRESET = 0x00000007;
        SPG_HBLANK_INT.full = 0x031D0000;
        SPG_VBLANK_INT.full = 0x00150104;
        FPU_PARAM_CFG = 0x0007DF77;
        HALF_OFFSET = 0x00000007;
        ISP_FEED_CFG = 0x00402000;
        SDRAM_REFRESH = 0x00000020;
        SDRAM_ARB_CFG = 0x0000001F;
        SDRAM_CFG = 0x15F28997;
        SPG_HBLANK.full = 0x007E0345;
        SPG_LOAD.full = 0x01060359;
        SPG_VBLANK.full = 0x01500104;
        SPG_WIDTH.full = 0x07F1933F;
        VO_CONTROL.full = 0x00000108;
        VO_STARTX.full = 0x0000009D;
        VO_STARTY.full = 0x00150015;
        SCALER_CTL.full = 0x00000400;
        FB_BURSTCTRL = 0x00090639;
        PT_ALPHA_REF = 0x000000FF;
    }
}

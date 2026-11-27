using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal static unsafe partial class PvrRegisters
{
    internal const int RegisterSize = 0x8000;
    internal const uint RegisterMask = RegisterSize - 1;

    internal static ref T PvrReg<T>(uint address) where T : unmanaged =>
        ref Unsafe.As<byte, T>(ref RegisterBytes[address & RegisterMask]);

    internal const uint ID_addr = 0x00000000;
    internal const uint REVISION_addr = 0x00000004;
    internal const uint SOFTRESET_addr = 0x00000008;
    internal const uint STARTRENDER_addr = 0x00000014;
    internal const uint TEST_SELECT_addr = 0x00000018;
    internal const uint PARAM_BASE_addr = 0x00000020;
    internal const uint REGION_BASE_addr = 0x0000002C;
    internal const uint SPAN_SORT_CFG_addr = 0x00000030;
    internal const uint VO_BORDER_COL_addr = 0x00000040;
    internal const uint FB_R_CTRL_addr = 0x00000044;
    internal const uint FB_W_CTRL_addr = 0x00000048;
    internal const uint FB_W_LINESTRIDE_addr = 0x0000004C;
    internal const uint FB_R_SOF1_addr = 0x00000050;
    internal const uint FB_R_SOF2_addr = 0x00000054;
    internal const uint FB_R_SIZE_addr = 0x0000005C;
    internal const uint FB_W_SOF1_addr = 0x00000060;
    internal const uint FB_W_SOF2_addr = 0x00000064;
    internal const uint FB_X_CLIP_addr = 0x00000068;
    internal const uint FB_Y_CLIP_addr = 0x0000006C;
    internal const uint FPU_SHAD_SCALE_addr = 0x00000074;
    internal const uint FPU_CULL_VAL_addr = 0x00000078;
    internal const uint FPU_PARAM_CFG_addr = 0x0000007C;
    internal const uint HALF_OFFSET_addr = 0x00000080;
    internal const uint FPU_PERP_VAL_addr = 0x00000084;
    internal const uint ISP_BACKGND_D_addr = 0x00000088;
    internal const uint ISP_BACKGND_T_addr = 0x0000008C;
    internal const uint ISP_FEED_CFG_addr = 0x00000098;
    internal const uint SDRAM_REFRESH_addr = 0x000000A0;
    internal const uint SDRAM_ARB_CFG_addr = 0x000000A4;
    internal const uint SDRAM_CFG_addr = 0x000000A8;
    internal const uint FOG_COL_RAM_addr = 0x000000B0;
    internal const uint FOG_COL_VERT_addr = 0x000000B4;
    internal const uint FOG_DENSITY_addr = 0x000000B8;
    internal const uint FOG_CLAMP_MAX_addr = 0x000000BC;
    internal const uint FOG_CLAMP_MIN_addr = 0x000000C0;
    internal const uint SPG_TRIGGER_POS_addr = 0x000000C4;
    internal const uint SPG_HBLANK_INT_addr = 0x000000C8;
    internal const uint SPG_VBLANK_INT_addr = 0x000000CC;
    internal const uint SPG_CONTROL_addr = 0x000000D0;
    internal const uint SPG_HBLANK_addr = 0x000000D4;
    internal const uint SPG_LOAD_addr = 0x000000D8;
    internal const uint SPG_VBLANK_addr = 0x000000DC;
    internal const uint SPG_WIDTH_addr = 0x000000E0;
    internal const uint TEXT_CONTROL_addr = 0x000000E4;
    internal const uint VO_CONTROL_addr = 0x000000E8;
    internal const uint VO_STARTX_addr = 0x000000Ec;
    internal const uint VO_STARTY_addr = 0x000000F0;
    internal const uint SCALER_CTL_addr = 0x000000F4;
    internal const uint PAL_RAM_CTRL_addr = 0x00000108;
    internal const uint SPG_STATUS_addr = 0x0000010C;
    internal const uint FB_BURSTCTRL_addr = 0x00000110;
    internal const uint FB_C_SOF_addr = 0x00000114;
    internal const uint Y_COEFF_addr = 0x00000118;
    internal const uint PT_ALPHA_REF_addr = 0x0000011C;
    internal const uint TA_OL_BASE_addr = 0x00000124;
    internal const uint TA_ISP_BASE_addr = 0x00000128;
    internal const uint TA_OL_LIMIT_addr = 0x0000012C;
    internal const uint TA_ISP_LIMIT_addr = 0x00000130;
    internal const uint TA_NEXT_OPB_addr = 0x00000134;
    internal const uint TA_ITP_CURRENT_addr = 0x00000138;
    internal const uint TA_GLOB_TILE_CLIP_addr = 0x0000013C;
    internal const uint TA_ALLOC_CTRL_addr = 0x00000140;
    internal const uint TA_LIST_INIT_addr = 0x00000144;
    internal const uint TA_YUV_TEX_BASE_addr = 0x00000148;
    internal const uint TA_YUV_TEX_CTRL_addr = 0x0000014C;
    internal const uint TA_YUV_TEX_CNT_addr = 0x00000150;
    internal const uint TA_LIST_CONT_addr = 0x00000160;
    internal const uint TA_NEXT_OPB_INIT_addr = 0x00000164;
    internal const uint SIGNATURE1_addr = 0x00000180;
    internal const uint SIGNATURE2_addr = 0x00000184;
    internal const uint FOG_TABLE_START_addr = 0x00000200;
    internal const uint FOG_TABLE_END_addr = 0x000003FC;
    internal const uint TA_OL_POINTERS_START_addr = 0x00000600;
    internal const uint TA_OL_POINTERS_END_addr = 0x00000F5C;
    internal const uint PALETTE_RAM_START_addr = 0x00001000;
    internal const uint PALETTE_RAM_END_addr = 0x00001FFC;

    internal static ref uint ID_Reg => ref PvrReg<uint>(ID_addr);
    internal static ref uint REVISION => ref PvrReg<uint>(REVISION_addr);
    internal static ref uint SOFTRESET => ref PvrReg<uint>(SOFTRESET_addr);
    internal static ref uint STARTRENDER => ref PvrReg<uint>(STARTRENDER_addr);
    internal static ref uint TEST_SELECT => ref PvrReg<uint>(TEST_SELECT_addr);
    internal static ref uint PARAM_BASE => ref PvrReg<uint>(PARAM_BASE_addr);
    internal static ref uint REGION_BASE => ref PvrReg<uint>(REGION_BASE_addr);
    internal static ref uint SPAN_SORT_CFG => ref PvrReg<uint>(SPAN_SORT_CFG_addr);
    internal static ref VO_BORDER_COL_type VO_BORDER_COL => ref PvrReg<VO_BORDER_COL_type>(VO_BORDER_COL_addr);
    internal static ref FB_R_CTRL_type FB_R_CTRL => ref PvrReg<FB_R_CTRL_type>(FB_R_CTRL_addr);
    internal static ref FB_W_CTRL_type FB_W_CTRL => ref PvrReg<FB_W_CTRL_type>(FB_W_CTRL_addr);
    internal static ref FB_W_LINESTRIDE_type FB_W_LINESTRIDE => ref PvrReg<FB_W_LINESTRIDE_type>(FB_W_LINESTRIDE_addr);
    internal static ref uint FB_R_SOF1 => ref PvrReg<uint>(FB_R_SOF1_addr);
    internal static ref uint FB_R_SOF2 => ref PvrReg<uint>(FB_R_SOF2_addr);
    internal static ref FB_R_SIZE_type FB_R_SIZE => ref PvrReg<FB_R_SIZE_type>(FB_R_SIZE_addr);
    internal static ref uint FB_W_SOF1 => ref PvrReg<uint>(FB_W_SOF1_addr);
    internal static ref uint FB_W_SOF2 => ref PvrReg<uint>(FB_W_SOF2_addr);
    internal static ref FB_X_CLIP_type FB_X_CLIP => ref PvrReg<FB_X_CLIP_type>(FB_X_CLIP_addr);
    internal static ref FB_Y_CLIP_type FB_Y_CLIP => ref PvrReg<FB_Y_CLIP_type>(FB_Y_CLIP_addr);
    internal static ref FPU_SHAD_SCALE_type FPU_SHAD_SCALE => ref PvrReg<FPU_SHAD_SCALE_type>(FPU_SHAD_SCALE_addr);
    internal static ref float FPU_CULL_VAL => ref PvrReg<float>(FPU_CULL_VAL_addr);
    internal static ref uint FPU_PARAM_CFG => ref PvrReg<uint>(FPU_PARAM_CFG_addr);
    internal static ref uint HALF_OFFSET => ref PvrReg<uint>(HALF_OFFSET_addr);
    internal static ref uint FPU_PERP_VAL => ref PvrReg<uint>(FPU_PERP_VAL_addr);
    internal static ref ISP_BACKGND_D_type ISP_BACKGND_D => ref PvrReg<ISP_BACKGND_D_type>(ISP_BACKGND_D_addr);
    internal static ref ISP_BACKGND_T_type ISP_BACKGND_T => ref PvrReg<ISP_BACKGND_T_type>(ISP_BACKGND_T_addr);
    internal static ref uint ISP_FEED_CFG => ref PvrReg<uint>(ISP_FEED_CFG_addr);
    internal static ref uint SDRAM_REFRESH => ref PvrReg<uint>(SDRAM_REFRESH_addr);
    internal static ref uint SDRAM_ARB_CFG => ref PvrReg<uint>(SDRAM_ARB_CFG_addr);
    internal static ref uint SDRAM_CFG => ref PvrReg<uint>(SDRAM_CFG_addr);
    internal static ref RGBColor FOG_COL_RAM => ref PvrReg<RGBColor>(FOG_COL_RAM_addr);
    internal static ref RGBColor FOG_COL_VERT => ref PvrReg<RGBColor>(FOG_COL_VERT_addr);
    internal static ref FOG_DENSITY_type FOG_DENSITY => ref PvrReg<FOG_DENSITY_type>(FOG_DENSITY_addr);
    internal static ref RGBAColor FOG_CLAMP_MAX => ref PvrReg<RGBAColor>(FOG_CLAMP_MAX_addr);
    internal static ref RGBAColor FOG_CLAMP_MIN => ref PvrReg<RGBAColor>(FOG_CLAMP_MIN_addr);
    internal static ref uint SPG_TRIGGER_POS => ref PvrReg<uint>(SPG_TRIGGER_POS_addr);
    internal static ref SPG_HBLANK_INT_type SPG_HBLANK_INT => ref PvrReg<SPG_HBLANK_INT_type>(SPG_HBLANK_INT_addr);
    internal static ref SPG_VBLANK_INT_type SPG_VBLANK_INT => ref PvrReg<SPG_VBLANK_INT_type>(SPG_VBLANK_INT_addr);
    internal static ref SPG_CONTROL_type SPG_CONTROL => ref PvrReg<SPG_CONTROL_type>(SPG_CONTROL_addr);
    internal static ref SPG_HBLANK_type SPG_HBLANK => ref PvrReg<SPG_HBLANK_type>(SPG_HBLANK_addr);
    internal static ref SPG_LOAD_type SPG_LOAD => ref PvrReg<SPG_LOAD_type>(SPG_LOAD_addr);
    internal static ref SPG_VBLANK_type SPG_VBLANK => ref PvrReg<SPG_VBLANK_type>(SPG_VBLANK_addr);
    internal static ref SPG_WIDTH_type SPG_WIDTH => ref PvrReg<SPG_WIDTH_type>(SPG_WIDTH_addr);
    internal static ref uint TEXT_CONTROL => ref PvrReg<uint>(TEXT_CONTROL_addr);
    internal static ref VO_CONTROL_type VO_CONTROL => ref PvrReg<VO_CONTROL_type>(VO_CONTROL_addr);
    internal static ref VO_STARTX_type VO_STARTX => ref PvrReg<VO_STARTX_type>(VO_STARTX_addr);
    internal static ref VO_STARTY_type VO_STARTY => ref PvrReg<VO_STARTY_type>(VO_STARTY_addr);
    internal static ref SCALER_CTL_type SCALER_CTL => ref PvrReg<SCALER_CTL_type>(SCALER_CTL_addr);
    internal static ref uint PAL_RAM_CTRL => ref PvrReg<uint>(PAL_RAM_CTRL_addr);
    internal static ref SPG_STATUS_type SPG_STATUS => ref PvrReg<SPG_STATUS_type>(SPG_STATUS_addr);
    internal static ref uint FB_BURSTCTRL => ref PvrReg<uint>(FB_BURSTCTRL_addr);
    internal static ref uint FB_C_SOF => ref PvrReg<uint>(FB_C_SOF_addr);
    internal static ref uint Y_COEFF => ref PvrReg<uint>(Y_COEFF_addr);
    internal static ref uint PT_ALPHA_REF => ref PvrReg<uint>(PT_ALPHA_REF_addr);
    internal static ref uint TA_OL_BASE => ref PvrReg<uint>(TA_OL_BASE_addr);
    internal static ref uint TA_ISP_BASE => ref PvrReg<uint>(TA_ISP_BASE_addr);
    internal static ref uint TA_OL_LIMIT => ref PvrReg<uint>(TA_OL_LIMIT_addr);
    internal static ref uint TA_ISP_LIMIT => ref PvrReg<uint>(TA_ISP_LIMIT_addr);
    internal static ref uint TA_NEXT_OPB => ref PvrReg<uint>(TA_NEXT_OPB_addr);
    internal static ref uint TA_ITP_CURRENT => ref PvrReg<uint>(TA_ITP_CURRENT_addr);
    internal static ref TA_GLOB_TILE_CLIP_type TA_GLOB_TILE_CLIP => ref PvrReg<TA_GLOB_TILE_CLIP_type>(TA_GLOB_TILE_CLIP_addr);
    internal static ref uint TA_ALLOC_CTRL => ref PvrReg<uint>(TA_ALLOC_CTRL_addr);
    internal static ref uint TA_LIST_INIT => ref PvrReg<uint>(TA_LIST_INIT_addr);
    internal static ref uint TA_YUV_TEX_BASE => ref PvrReg<uint>(TA_YUV_TEX_BASE_addr);
    internal static ref TA_YUV_TEX_CTRL_type TA_YUV_TEX_CTRL => ref PvrReg<TA_YUV_TEX_CTRL_type>(TA_YUV_TEX_CTRL_addr);
    internal static ref uint TA_YUV_TEX_CNT => ref PvrReg<uint>(TA_YUV_TEX_CNT_addr);
    internal static ref uint TA_LIST_CONT => ref PvrReg<uint>(TA_LIST_CONT_addr);
    internal static ref uint TA_NEXT_OPB_INIT => ref PvrReg<uint>(TA_NEXT_OPB_INIT_addr);

    internal static uint* FOG_TABLE => (uint*)Unsafe.AsPointer(ref PvrReg<uint>(FOG_TABLE_START_addr));

    internal static uint* TA_OL_POINTERS => (uint*)Unsafe.AsPointer(ref PvrReg<uint>(TA_OL_POINTERS_START_addr));

    internal static uint* PALETTE_RAM => (uint*)Unsafe.AsPointer(ref PvrReg<uint>(PALETTE_RAM_START_addr));

    internal enum fb_depth_enum
    {
        fbde_0555 = 0,
        fbde_565 = 1,
        fbde_888 = 2,
        fbde_C888 = 3
    }

    internal struct FB_R_CTRL_type
    {
        internal uint full;

        internal uint fb_enable
        {
            readonly get => (full >> 0) & 0x1u;
            set => full = (full & ~(0x1u << 0)) | ((value & 0x1u) << 0);
        }

        internal uint fb_line_double
        {
            readonly get => (full >> 1) & 0x1u;
            set => full = (full & ~(0x1u << 1)) | ((value & 0x1u) << 1);
        }

        internal uint fb_depth
        {
            readonly get => (full >> 2) & 0x3u;
            set => full = (full & ~(0x3u << 2)) | ((value & 0x3u) << 2);
        }

        internal uint fb_concat
        {
            readonly get => (full >> 4) & 0x7u;
            set => full = (full & ~(0x7u << 4)) | ((value & 0x7u) << 4);
        }

        internal uint fb_chroma_threshold
        {
            readonly get => (full >> 8) & 0xffu;
            set => full = (full & ~(0xffu << 8)) | ((value & 0xffu) << 8);
        }

        internal uint fb_stripsize
        {
            readonly get => (full >> 16) & 0x3fu;
            set => full = (full & ~(0x3fu << 16)) | ((value & 0x3fu) << 16);
        }

        internal uint fb_strip_buf_en
        {
            readonly get => (full >> 22) & 0x1u;
            set => full = (full & ~(0x1u << 22)) | ((value & 0x1u) << 22);
        }

        internal uint vclk_div
        {
            readonly get => (full >> 23) & 0x1u;
            set => full = (full & ~(0x1u << 23)) | ((value & 0x1u) << 23);
        }
    }

    internal struct FB_R_SIZE_type
    {
        internal uint full;

        internal uint fb_x_size
        {
            readonly get => (full >> 0) & 0x3ffu;
            set => full = (full & ~(0x3ffu << 0)) | ((value & 0x3ffu) << 0);
        }

        internal uint fb_y_size
        {
            readonly get => (full >> 10) & 0x3ffu;
            set => full = (full & ~(0x3ffu << 10)) | ((value & 0x3ffu) << 10);
        }

        internal uint fb_modulus
        {
            readonly get => (full >> 20) & 0x3ffu;
            set => full = (full & ~(0x3ffu << 20)) | ((value & 0x3ffu) << 20);
        }
    }

    internal struct SPG_STATUS_type
    {
        internal uint full;

        internal uint scanline
        {
            readonly get => (full >> 0) & 0x3ffu;
            set => full = (full & ~(0x3ffu << 0)) | ((value & 0x3ffu) << 0);
        }

        internal uint fieldnum
        {
            readonly get => (full >> 10) & 0x1u;
            set => full = (full & ~(0x1u << 10)) | ((value & 0x1u) << 10);
        }

        internal uint blank
        {
            readonly get => (full >> 11) & 0x1u;
            set => full = (full & ~(0x1u << 11)) | ((value & 0x1u) << 11);
        }

        internal uint hsync
        {
            readonly get => (full >> 12) & 0x1u;
            set => full = (full & ~(0x1u << 12)) | ((value & 0x1u) << 12);
        }

        internal uint vsync
        {
            readonly get => (full >> 13) & 0x1u;
            set => full = (full & ~(0x1u << 13)) | ((value & 0x1u) << 13);
        }
    }

    internal struct SPG_HBLANK_INT_type
    {
        internal uint full;

        internal uint line_comp_val
        {
            readonly get => (full >> 0) & 0x3ffu;
            set => full = (full & ~(0x3ffu << 0)) | ((value & 0x3ffu) << 0);
        }

        internal uint hblank_int_mode
        {
            readonly get => (full >> 12) & 0x3u;
            set => full = (full & ~(0x3u << 12)) | ((value & 0x3u) << 12);
        }

        internal uint hblank_in_interrupt
        {
            readonly get => (full >> 16) & 0x3ffu;
            set => full = (full & ~(0x3ffu << 16)) | ((value & 0x3ffu) << 16);
        }
    }

    internal struct SPG_VBLANK_INT_type
    {
        internal uint full;

        internal uint vblank_in_interrupt_line_number
        {
            readonly get => (full >> 0) & 0x3ffu;
            set => full = (full & ~(0x3ffu << 0)) | ((value & 0x3ffu) << 0);
        }

        internal uint vblank_out_interrupt_line_number
        {
            readonly get => (full >> 16) & 0x3ffu;
            set => full = (full & ~(0x3ffu << 16)) | ((value & 0x3ffu) << 16);
        }
    }

    internal struct SPG_CONTROL_type
    {
        internal uint full;

        internal uint mhsync_pol
        {
            readonly get => (full >> 0) & 0x1u;
            set => full = (full & ~(0x1u << 0)) | ((value & 0x1u) << 0);
        }

        internal uint mvsync_pol
        {
            readonly get => (full >> 1) & 0x1u;
            set => full = (full & ~(0x1u << 1)) | ((value & 0x1u) << 1);
        }

        internal uint mcsync_pol
        {
            readonly get => (full >> 2) & 0x1u;
            set => full = (full & ~(0x1u << 2)) | ((value & 0x1u) << 2);
        }

        internal uint spg_lock
        {
            readonly get => (full >> 3) & 0x1u;
            set => full = (full & ~(0x1u << 3)) | ((value & 0x1u) << 3);
        }

        internal uint interlace
        {
            readonly get => (full >> 4) & 0x1u;
            set => full = (full & ~(0x1u << 4)) | ((value & 0x1u) << 4);
        }

        internal uint force_field2
        {
            readonly get => (full >> 5) & 0x1u;
            set => full = (full & ~(0x1u << 5)) | ((value & 0x1u) << 5);
        }

        internal uint NTSC
        {
            readonly get => (full >> 6) & 0x1u;
            set => full = (full & ~(0x1u << 6)) | ((value & 0x1u) << 6);
        }

        internal uint PAL
        {
            readonly get => (full >> 7) & 0x1u;
            set => full = (full & ~(0x1u << 7)) | ((value & 0x1u) << 7);
        }

        internal uint sync_direction
        {
            readonly get => (full >> 8) & 0x1u;
            set => full = (full & ~(0x1u << 8)) | ((value & 0x1u) << 8);
        }

        internal uint csync_on_h
        {
            readonly get => (full >> 9) & 0x1u;
            set => full = (full & ~(0x1u << 9)) | ((value & 0x1u) << 9);
        }

        internal readonly bool IsNTSC() => NTSC == 1 && PAL == 0;

        internal readonly bool IsPAL() => NTSC == 0 && PAL == 1;

        internal readonly bool IsVGA() => NTSC == 0 && PAL == 0;
    }

    internal struct SPG_HBLANK_type
    {
        internal uint full;

        internal uint hstart
        {
            readonly get => (full >> 0) & 0x3ffu;
            set => full = (full & ~(0x3ffu << 0)) | ((value & 0x3ffu) << 0);
        }

        internal uint hbend
        {
            readonly get => (full >> 16) & 0x3ffu;
            set => full = (full & ~(0x3ffu << 16)) | ((value & 0x3ffu) << 16);
        }
    }

    internal struct SPG_LOAD_type
    {
        internal uint full;

        internal uint hcount
        {
            readonly get => (full >> 0) & 0x3ffu;
            set => full = (full & ~(0x3ffu << 0)) | ((value & 0x3ffu) << 0);
        }

        internal uint vcount
        {
            readonly get => (full >> 16) & 0x3ffu;
            set => full = (full & ~(0x3ffu << 16)) | ((value & 0x3ffu) << 16);
        }
    }

    internal struct SPG_VBLANK_type
    {
        internal uint full;

        internal uint vstart
        {
            readonly get => (full >> 0) & 0x3ffu;
            set => full = (full & ~(0x3ffu << 0)) | ((value & 0x3ffu) << 0);
        }

        internal uint vbend
        {
            readonly get => (full >> 16) & 0x3ffu;
            set => full = (full & ~(0x3ffu << 16)) | ((value & 0x3ffu) << 16);
        }
    }

    internal struct SPG_WIDTH_type
    {
        internal uint full;

        internal uint hswidth
        {
            readonly get => (full >> 0) & 0x7fu;
            set => full = (full & ~(0x7fu << 0)) | ((value & 0x7fu) << 0);
        }

        internal uint vswidth
        {
            readonly get => (full >> 8) & 0xfu;
            set => full = (full & ~(0xfu << 8)) | ((value & 0xfu) << 8);
        }

        internal uint bpwidth
        {
            readonly get => (full >> 12) & 0x3ffu;
            set => full = (full & ~(0x3ffu << 12)) | ((value & 0x3ffu) << 12);
        }

        internal uint eqwidth
        {
            readonly get => (full >> 22) & 0x3ffu;
            set => full = (full & ~(0x3ffu << 22)) | ((value & 0x3ffu) << 22);
        }
    }

    internal struct SCALER_CTL_type
    {
        internal uint full;

        internal uint vscalefactor
        {
            readonly get => (full >> 0) & 0xffffu;
            set => full = (full & ~(0xffffu << 0)) | ((value & 0xffffu) << 0);
        }

        internal uint hscale
        {
            readonly get => (full >> 16) & 0x1u;
            set => full = (full & ~(0x1u << 16)) | ((value & 0x1u) << 16);
        }

        internal uint interlace
        {
            readonly get => (full >> 17) & 0x1u;
            set => full = (full & ~(0x1u << 17)) | ((value & 0x1u) << 17);
        }

        internal uint fieldselect
        {
            readonly get => (full >> 18) & 0x1u;
            set => full = (full & ~(0x1u << 18)) | ((value & 0x1u) << 18);
        }
    }

    internal struct FB_X_CLIP_type
    {
        internal uint full;

        internal uint min
        {
            readonly get => (full >> 0) & 0x7ffu;
            set => full = (full & ~(0x7ffu << 0)) | ((value & 0x7ffu) << 0);
        }

        internal uint max
        {
            readonly get => (full >> 16) & 0x7ffu;
            set => full = (full & ~(0x7ffu << 16)) | ((value & 0x7ffu) << 16);
        }
    }

    internal struct FB_Y_CLIP_type
    {
        internal uint full;

        internal uint min
        {
            readonly get => (full >> 0) & 0x3ffu;
            set => full = (full & ~(0x3ffu << 0)) | ((value & 0x3ffu) << 0);
        }

        internal uint max
        {
            readonly get => (full >> 16) & 0x3ffu;
            set => full = (full & ~(0x3ffu << 16)) | ((value & 0x3ffu) << 16);
        }
    }

    internal struct VO_CONTROL_type
    {
        internal uint full;

        internal uint hsync_pol
        {
            readonly get => (full >> 0) & 0x1u;
            set => full = (full & ~(0x1u << 0)) | ((value & 0x1u) << 0);
        }

        internal uint vsync_pol
        {
            readonly get => (full >> 1) & 0x1u;
            set => full = (full & ~(0x1u << 1)) | ((value & 0x1u) << 1);
        }

        internal uint blank_pol
        {
            readonly get => (full >> 2) & 0x1u;
            set => full = (full & ~(0x1u << 2)) | ((value & 0x1u) << 2);
        }

        internal uint blank_video
        {
            readonly get => (full >> 3) & 0x1u;
            set => full = (full & ~(0x1u << 3)) | ((value & 0x1u) << 3);
        }

        internal uint field_mode
        {
            readonly get => (full >> 4) & 0xfu;
            set => full = (full & ~(0xfu << 4)) | ((value & 0xfu) << 4);
        }

        internal uint pixel_double
        {
            readonly get => (full >> 8) & 0x1u;
            set => full = (full & ~(0x1u << 8)) | ((value & 0x1u) << 8);
        }

        internal uint pclk_delay
        {
            readonly get => (full >> 16) & 0x3fu;
            set => full = (full & ~(0x3fu << 16)) | ((value & 0x3fu) << 16);
        }
    }

    internal struct VO_STARTX_type
    {
        internal uint full;

        internal uint HStart
        {
            readonly get => (full >> 0) & 0x3ffu;
            set => full = (full & ~(0x3ffu << 0)) | ((value & 0x3ffu) << 0);
        }
    }

    internal struct VO_STARTY_type
    {
        internal uint full;

        internal uint VStart_field1
        {
            readonly get => (full >> 0) & 0x3ffu;
            set => full = (full & ~(0x3ffu << 0)) | ((value & 0x3ffu) << 0);
        }

        internal uint VStart_field2
        {
            readonly get => (full >> 16) & 0x3ffu;
            set => full = (full & ~(0x3ffu << 16)) | ((value & 0x3ffu) << 16);
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct ISP_BACKGND_D_type
    {
        [FieldOffset(0)] internal uint i;
        [FieldOffset(0)] internal float f;
    }

    internal struct ISP_BACKGND_T_type
    {
        internal uint full;

        internal uint tag_offset
        {
            readonly get => (full >> 0) & 0x7u;
            set => full = (full & ~(0x7u << 0)) | ((value & 0x7u) << 0);
        }

        internal uint tag_address
        {
            readonly get => (full >> 3) & 0x1fffffu;
            set => full = (full & ~(0x1fffffu << 3)) | ((value & 0x1fffffu) << 3);
        }

        internal uint skip
        {
            readonly get => (full >> 24) & 0x7u;
            set => full = (full & ~(0x7u << 24)) | ((value & 0x7u) << 24);
        }

        internal uint shadow
        {
            readonly get => (full >> 27) & 0x1u;
            set => full = (full & ~(0x1u << 27)) | ((value & 0x1u) << 27);
        }

        internal uint cache_bypass
        {
            readonly get => (full >> 28) & 0x1u;
            set => full = (full & ~(0x1u << 28)) | ((value & 0x1u) << 28);
        }
    }

    internal struct FB_W_CTRL_type
    {
        internal uint full;

        internal uint fb_packmode
        {
            readonly get => (full >> 0) & 0x7u;
            set => full = (full & ~(0x7u << 0)) | ((value & 0x7u) << 0);
        }

        internal uint fb_dither
        {
            readonly get => (full >> 3) & 0x1u;
            set => full = (full & ~(0x1u << 3)) | ((value & 0x1u) << 3);
        }

        internal uint fb_kval
        {
            readonly get => (full >> 8) & 0xffu;
            set => full = (full & ~(0xffu << 8)) | ((value & 0xffu) << 8);
        }

        internal uint fb_alpha_threshold
        {
            readonly get => (full >> 16) & 0xffu;
            set => full = (full & ~(0xffu << 16)) | ((value & 0xffu) << 16);
        }
    }

    internal struct FB_W_LINESTRIDE_type
    {
        internal uint full;

        internal uint stride
        {
            readonly get => (full >> 0) & 0x1ffu;
            set => full = (full & ~(0x1ffu << 0)) | ((value & 0x1ffu) << 0);
        }
    }

    internal struct FPU_SHAD_SCALE_type
    {
        internal uint full;

        internal uint scale_factor
        {
            readonly get => (full >> 0) & 0xffu;
            set => full = (full & ~(0xffu << 0)) | ((value & 0xffu) << 0);
        }

        internal uint intensity_shadow
        {
            readonly get => (full >> 8) & 0x1u;
            set => full = (full & ~(0x1u << 8)) | ((value & 0x1u) << 8);
        }
    }

    internal struct TA_GLOB_TILE_CLIP_type
    {
        internal uint full;

        internal uint tile_x_num
        {
            readonly get => (full >> 0) & 0x3fu;
            set => full = (full & ~(0x3fu << 0)) | ((value & 0x3fu) << 0);
        }

        internal uint tile_y_num
        {
            readonly get => (full >> 16) & 0xfu;
            set => full = (full & ~(0xfu << 16)) | ((value & 0xfu) << 16);
        }
    }

    internal struct TA_YUV_TEX_CTRL_type
    {
        internal uint full;

        internal uint yuv_u_size
        {
            readonly get => (full >> 0) & 0x3fu;
            set => full = (full & ~(0x3fu << 0)) | ((value & 0x3fu) << 0);
        }

        internal uint yuv_v_size
        {
            readonly get => (full >> 8) & 0x3fu;
            set => full = (full & ~(0x3fu << 8)) | ((value & 0x3fu) << 8);
        }

        internal uint yuv_tex
        {
            readonly get => (full >> 16) & 0x1u;
            set => full = (full & ~(0x1u << 16)) | ((value & 0x1u) << 16);
        }

        internal uint yuv_form
        {
            readonly get => (full >> 24) & 0x1u;
            set => full = (full & ~(0x1u << 24)) | ((value & 0x1u) << 24);
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct FOG_DENSITY_type
    {
        [FieldOffset(0)] internal sbyte exponent;
        [FieldOffset(1)] internal byte mantissa;
        [FieldOffset(0)] internal uint full;

        internal readonly float Get() => mantissa / 128f * MathF.Pow(2f, exponent);
    }

    private static class PvrColorTemplate
    {
        internal static float Red(uint full) => ((full >> 16) & 255) / 255f;

        internal static float Green(uint full) => ((full >> 8) & 255) / 255f;

        internal static float Blue(uint full) => (full & 255) / 255f;

        internal static float Alpha(uint full) => (full >> 24) / 255f;

        internal static void GetRGBColor(uint full, Span<float> color)
        {
            color[0] = Red(full);
            color[1] = Green(full);
            color[2] = Blue(full);
        }

        internal static void GetRGBAColor(uint full, Span<float> color)
        {
            GetRGBColor(full, color);
            color[3] = Alpha(full);
        }
    }

    internal struct VO_BORDER_COL_type
    {
        internal uint full;

        internal uint _blue
        {
            readonly get => (full >> 0) & 0xffu;
            set => full = (full & ~(0xffu << 0)) | ((value & 0xffu) << 0);
        }

        internal uint _green
        {
            readonly get => (full >> 8) & 0xffu;
            set => full = (full & ~(0xffu << 8)) | ((value & 0xffu) << 8);
        }

        internal uint _red
        {
            readonly get => (full >> 16) & 0xffu;
            set => full = (full & ~(0xffu << 16)) | ((value & 0xffu) << 16);
        }

        internal uint _chroma
        {
            readonly get => (full >> 24) & 0x1u;
            set => full = (full & ~(0x1u << 24)) | ((value & 0x1u) << 24);
        }

        internal readonly float Red() => PvrColorTemplate.Red(full);

        internal readonly float Green() => PvrColorTemplate.Green(full);

        internal readonly float Blue() => full == 0xff ? 0f : PvrColorTemplate.Blue(full);

        internal readonly void GetRGBColor(Span<float> color)
        {
            color[0] = Red();
            color[1] = Green();
            color[2] = Blue();
        }
    }

    internal struct RGBColor
    {
        internal uint full;

        internal uint _blue
        {
            readonly get => (full >> 0) & 0xffu;
            set => full = (full & ~(0xffu << 0)) | ((value & 0xffu) << 0);
        }

        internal uint _green
        {
            readonly get => (full >> 8) & 0xffu;
            set => full = (full & ~(0xffu << 8)) | ((value & 0xffu) << 8);
        }

        internal uint _red
        {
            readonly get => (full >> 16) & 0xffu;
            set => full = (full & ~(0xffu << 16)) | ((value & 0xffu) << 16);
        }

        internal readonly float Red() => PvrColorTemplate.Red(full);

        internal readonly float Green() => PvrColorTemplate.Green(full);

        internal readonly float Blue() => PvrColorTemplate.Blue(full);

        internal readonly void GetRGBColor(Span<float> color) => PvrColorTemplate.GetRGBColor(full, color);
    }

    internal struct RGBAColor
    {
        internal uint full;

        internal uint _blue
        {
            readonly get => (full >> 0) & 0xffu;
            set => full = (full & ~(0xffu << 0)) | ((value & 0xffu) << 0);
        }

        internal uint _green
        {
            readonly get => (full >> 8) & 0xffu;
            set => full = (full & ~(0xffu << 8)) | ((value & 0xffu) << 8);
        }

        internal uint _red
        {
            readonly get => (full >> 16) & 0xffu;
            set => full = (full & ~(0xffu << 16)) | ((value & 0xffu) << 16);
        }

        internal uint _alpha
        {
            readonly get => (full >> 24) & 0xffu;
            set => full = (full & ~(0xffu << 24)) | ((value & 0xffu) << 24);
        }

        internal readonly float Red() => PvrColorTemplate.Red(full);

        internal readonly float Green() => PvrColorTemplate.Green(full);

        internal readonly float Blue() => PvrColorTemplate.Blue(full);

        internal readonly void GetRGBColor(Span<float> color) => PvrColorTemplate.GetRGBColor(full, color);

        internal readonly float Alpha() => PvrColorTemplate.Alpha(full);

        internal readonly void GetRGBAColor(Span<float> color) => PvrColorTemplate.GetRGBAColor(full, color);
    }
}

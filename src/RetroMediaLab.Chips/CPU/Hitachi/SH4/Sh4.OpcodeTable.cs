using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

using static Sh4DecodeMode;
using static Sh4DecodeParameter;
using static Sh4OpcodeFlags;
using static Sh4ShilOperation;

internal static unsafe partial class Sh4OpcodeTable
{
    internal static readonly nuint[] NativeOpcodeHandlers = new nuint[0x10000];
    internal static readonly Sh4OpcodeDescriptor[] OpcodeDescriptors = new Sh4OpcodeDescriptor[0x10000];

    private const ushort Mask_n_m = 0xF00F;
    private const ushort Mask_n_m_imm4 = 0xF000;
    private const ushort Mask_n = 0xF0FF;
    private const ushort Mask_none = 0xFFFF;
    private const ushort Mask_imm8 = 0xFF00;
    private const ushort Mask_n_imm8 = 0xF000;
    private const ushort Mask_n_ml3bit = 0xF08F;
    private const ushort Mask_nh3bit = 0xF1FF;
    private const ushort Mask_nh2bit = 0xF3FF;
    private const ushort FirmwareTrapOpcode = 0x085B;

    private static ulong dec_Fill(Sh4DecodeMode mode, Sh4DecodeParameter destination,
        Sh4DecodeParameter source, Sh4ShilOperation operation, int extra = 0) =>
        ((ulong)unchecked((uint)extra) << 32) | ((ulong)mode << 24) | ((ulong)destination << 16)
        | ((ulong)source << 8) | (ulong)operation;

    private static ulong dec_Un_rNrM(Sh4ShilOperation operation) =>
        dec_Fill(DM_UnaryOp, PRM_RN, PRM_RM, operation);

    private static ulong dec_Un_frNfrN(Sh4ShilOperation operation) =>
        dec_Fill(DM_UnaryOp, PRM_FRN, PRM_FRN, operation);

    private static ulong dec_Bin_frNfrM(Sh4ShilOperation operation, int hasWrite = 1) =>
        dec_Fill(DM_BinaryOp, PRM_FRN, PRM_FRM, operation, hasWrite);

    private static ulong dec_Bin_rNrM(Sh4ShilOperation operation, int hasWrite = 1) =>
        dec_Fill(DM_BinaryOp, PRM_RN, PRM_RM, operation, hasWrite);

    private static ulong dec_mul(int type) =>
        dec_Fill(DM_MUL, PRM_RN, PRM_RM, shop_mul_s16, type);

    private static ulong dec_Bin_S8R(Sh4ShilOperation operation, int hasWrite = 1) =>
        dec_Fill(DM_BinaryOp, PRM_RN, PRM_SIMM8, operation, hasWrite);

    private static ulong dec_Bin_r0u8(Sh4ShilOperation operation, int hasWrite = 1) =>
        dec_Fill(DM_BinaryOp, PRM_R0, PRM_UIMM8, operation, hasWrite);

    private static ulong dec_shft(int offset, bool arithmetic)
    {
        if (offset > 0)
            return dec_Fill(DM_Shift, PRM_RN, PRM_RN, shop_shl, offset);
        else
            return dec_Fill(DM_Shift, PRM_RN, PRM_RN, arithmetic ? shop_sar : shop_shr, -offset);
    }

    private static ulong dec_cmp(Sh4ShilOperation operation, Sh4DecodeParameter left, Sh4DecodeParameter right) =>
        dec_Fill(DM_WriteTOp, left, right, operation);

    private static ulong dec_LD(Sh4DecodeParameter destination) =>
        dec_Fill(DM_UnaryOp, destination, PRM_RN, shop_mov32);

    private static ulong dec_LDM(Sh4DecodeParameter destination) =>
        dec_Fill(DM_ReadM, destination, PRM_RN, shop_readm, -4);

    private static ulong dec_ST(Sh4DecodeParameter source) =>
        dec_Fill(DM_UnaryOp, PRM_RN, source, shop_mov32);

    private static ulong dec_STSRF(Sh4DecodeParameter source) =>
        dec_Fill(DM_ReadSRF, PRM_RN, source, shop_mov32);

    private static ulong dec_STM(Sh4DecodeParameter source) =>
        dec_Fill(DM_WriteM, PRM_RN, source, shop_writem, -4);

    private static ulong dec_MRd(Sh4DecodeParameter destination, Sh4DecodeParameter source, int size) =>
        dec_Fill(DM_ReadM, destination, source, shop_readm, size);

    private static ulong dec_MWt(Sh4DecodeParameter destination, Sh4DecodeParameter source, int size) =>
        dec_Fill(DM_WriteM, destination, source, shop_writem, size);

    private static readonly Sh4OpcodeDescriptor MissingOpcode = new(
        static (decoder, opcode) => decoder.Decode_illegalOp(opcode),
        static (interpreter, context, opcode) => interpreter.Execute_iNotImplemented(context, opcode),
        0, 0, ReadWritePC, "missing", 0, 0, Sh4ExecutionUnit.CO, 1, 0, Native_iNotImplemented);

    private static readonly Sh4OpcodeDescriptor[] Definitions =
    [
        new(
            null,
            static (_, context, opcode) => Sh4Reios.Trap(context, opcode),
            Mask_none, FirmwareTrapOpcode, Branch_dir, "reios_trap", 100, 100, Sh4ExecutionUnit.CO, 1,
            0, Native_ReiosTrap),
        new(
            static (decoder, opcode) => decoder.Decode_i0000_nnnn_0010_0011(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_0010_0011(context, opcode),
            Mask_n, 0x0023, Branch_rel_d, "braf <REG_N>", 2, 3, Sh4ExecutionUnit.CO, 4,
            0, Native_i0000_nnnn_0010_0011),
        new(
            static (decoder, opcode) => decoder.Decode_i0000_nnnn_0000_0011(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_0000_0011(context, opcode),
            Mask_n, 0x0003, Branch_rel_d, "bsrf <REG_N>", 2, 3, Sh4ExecutionUnit.CO, 24,
            0, Native_i0000_nnnn_0000_0011),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_1100_0011(context, opcode),
            Mask_n, 0x00C3, Normal, "movca.l R0, @<REG_N>", 1, 4, Sh4ExecutionUnit.LS, 12,
            dec_MWt(PRM_RN, PRM_R0, 4), Native_i0000_nnnn_1100_0011),
        new(
            static (decoder, opcode) => decoder.Decode_i0000_0000_0000_1001(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_1001_0011(context, opcode),
            Mask_n, 0x0093, Normal, "ocbi @<REG_N>", 1, 2, Sh4ExecutionUnit.LS, 10,
            0, Native_i0000_nnnn_1001_0011),
        new(
            static (decoder, opcode) => decoder.Decode_i0000_0000_0000_1001(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_1010_0011(context, opcode),
            Mask_n, 0x00A3, Normal, "ocbp @<REG_N>", 1, 3, Sh4ExecutionUnit.LS, 11,
            0, Native_i0000_nnnn_1010_0011),
        new(
            static (decoder, opcode) => decoder.Decode_i0000_0000_0000_1001(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_1011_0011(context, opcode),
            Mask_n, 0x00B3, Normal, "ocbwb @<REG_N>", 1, 3, Sh4ExecutionUnit.LS, 11,
            0, Native_i0000_nnnn_1011_0011),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_1000_0011(context, opcode),
            Mask_n, 0x0083, Normal, "pref @<REG_N>", 1, 1, Sh4ExecutionUnit.LS, 2,
            dec_Fill(DM_UnaryOp, PRM_RN, PRM_ONE, shop_pref, 1), Native_i0000_nnnn_1000_0011),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_mmmm_0111(context, opcode),
            Mask_n_m, 0x0007, Normal, "mul.l <REG_M>,<REG_N>", 2, 4, Sh4ExecutionUnit.CO, 34,
            dec_mul(-32), Native_i0000_nnnn_mmmm_0111),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_0000_0010_1000(context, opcode),
            Mask_none, 0x0028, Normal, "clrmac", 1, 3, Sh4ExecutionUnit.CO, 28,
            0, Native_i0000_0000_0010_1000),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_0000_0100_1000(context, opcode),
            Mask_none, 0x0048, Normal, "clrs", 1, 1, Sh4ExecutionUnit.CO, 1,
            dec_Fill(DM_BinaryOp, PRM_SR_STATUS, PRM_TWO_INV, shop_and, 1), Native_i0000_0000_0100_1000),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_0000_0000_1000(context, opcode),
            Mask_none, 0x0008, Normal, "clrt", 1, 1, Sh4ExecutionUnit.MT, 1,
            dec_Fill(DM_UnaryOp, PRM_SR_T, PRM_ZERO, shop_mov32), Native_i0000_0000_0000_1000),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_0000_0011_1000(context, opcode),
            Mask_none, 0x0038, Normal, "ldtlb", 1, 1, Sh4ExecutionUnit.CO, 2,
            0, Native_i0000_0000_0011_1000),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_0000_0101_1000(context, opcode),
            Mask_none, 0x0058, Normal, "sets", 1, 1, Sh4ExecutionUnit.CO, 1,
            dec_Fill(DM_BinaryOp, PRM_SR_STATUS, PRM_TWO, shop_or, 1), Native_i0000_0000_0101_1000),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_0000_0001_1000(context, opcode),
            Mask_none, 0x0018, Normal, "sett", 1, 1, Sh4ExecutionUnit.MT, 1,
            dec_Fill(DM_UnaryOp, PRM_SR_T, PRM_ONE, shop_mov32), Native_i0000_0000_0001_1000),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_0000_0001_1001(context, opcode),
            Mask_none, 0x0019, Normal, "div0u", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Fill(DM_DIV0, PRM_RN, PRM_RM, shop_or, 1), Native_i0000_0000_0001_1001),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_0010_1001(context, opcode),
            Mask_n, 0x0029, Normal, "movt <REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Fill(DM_UnaryOp, PRM_RN, PRM_SR_T, shop_mov32), Native_i0000_nnnn_0010_1001),
        new(
            static (decoder, opcode) => decoder.Decode_i0000_0000_0000_1001(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i0000_0000_0000_1001(context, opcode),
            Mask_none, 0x0009, Normal, "nop", 1, 0, Sh4ExecutionUnit.MT, 1,
            0, Native_i0000_0000_0000_1001),
        new(
            static (decoder, opcode) => decoder.Decode_i0000_0000_0000_1001(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i0000_0000_0000_1001(context, opcode),
            Mask_none, 0x0000, Normal, "nop0", 1, 0, Sh4ExecutionUnit.MT, 1,
            0, Native_i0000_0000_0000_1001),
        new(
            static (decoder, opcode) => decoder.Decode_i0000_0000_0010_1011(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i0000_0000_0010_1011(context, opcode),
            Mask_none, 0x002B, Branch_dir_d, "rte", 5, 5, Sh4ExecutionUnit.CO, 8,
            0, Native_i0000_0000_0010_1011),
        new(
            static (decoder, opcode) => decoder.Decode_i0000_0000_0000_1011(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i0000_0000_0000_1011(context, opcode),
            Mask_none, 0x000B, Branch_dir_d, "rts", 2, 3, Sh4ExecutionUnit.CO, 4,
            0, Native_i0000_0000_0000_1011),
        new(
            static (decoder, opcode) => decoder.Decode_i0000_0000_0001_1011(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i0000_0000_0001_1011(context, opcode),
            Mask_none, 0x001B, ReadWritePC, "sleep", 4, 4, Sh4ExecutionUnit.CO, 9,
            0, Native_i0000_0000_0001_1011),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_mmmm_1111(context, opcode),
            Mask_n_m, 0x000F, Normal, "mac.l @<REG_M>+,@<REG_N>+", 2, 4, Sh4ExecutionUnit.CO, 35,
            0, Native_i0000_nnnn_mmmm_1111),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0010_nnnn_mmmm_0111(context, opcode),
            Mask_n_m, 0x2007, Normal, "div0s <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Fill(DM_DIV0, PRM_RN, PRM_RM, shop_or, -1), Native_i0010_nnnn_mmmm_0111),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0010_nnnn_mmmm_1000(context, opcode),
            Mask_n_m, 0x2008, Normal, "tst <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.MT, 1,
            dec_cmp(shop_test, PRM_RN, PRM_RM), Native_i0010_nnnn_mmmm_1000),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0010_nnnn_mmmm_1001(context, opcode),
            Mask_n_m, 0x2009, Normal, "and <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Bin_rNrM(shop_and), Native_i0010_nnnn_mmmm_1001),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0010_nnnn_mmmm_1010(context, opcode),
            Mask_n_m, 0x200A, Normal, "xor <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Bin_rNrM(shop_xor), Native_i0010_nnnn_mmmm_1010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0010_nnnn_mmmm_1011(context, opcode),
            Mask_n_m, 0x200B, Normal, "or <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Bin_rNrM(shop_or), Native_i0010_nnnn_mmmm_1011),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0010_nnnn_mmmm_1100(context, opcode),
            Mask_n_m, 0x200C, Normal, "cmp/str <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.MT, 1,
            dec_cmp(shop_setpeq, PRM_RN, PRM_RM), Native_i0010_nnnn_mmmm_1100),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0010_nnnn_mmmm_1101(context, opcode),
            Mask_n_m, 0x200D, Normal, "xtrct <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Bin_rNrM(shop_xtrct), Native_i0010_nnnn_mmmm_1101),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0010_nnnn_mmmm_1110(context, opcode),
            Mask_n_m, 0x200E, Normal, "mulu.w <REG_M>,<REG_N>", 2, 4, Sh4ExecutionUnit.CO, 34,
            dec_mul(16), Native_i0010_nnnn_mmmm_1110),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0010_nnnn_mmmm_1111(context, opcode),
            Mask_n_m, 0x200F, Normal, "muls.w <REG_M>,<REG_N>", 2, 4, Sh4ExecutionUnit.CO, 34,
            dec_mul(-16), Native_i0010_nnnn_mmmm_1111),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0011_nnnn_mmmm_0000(context, opcode),
            Mask_n_m, 0x3000, Normal, "cmp/eq <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.MT, 1,
            dec_cmp(shop_seteq, PRM_RN, PRM_RM), Native_i0011_nnnn_mmmm_0000),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0011_nnnn_mmmm_0010(context, opcode),
            Mask_n_m, 0x3002, Normal, "cmp/hs <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.MT, 1,
            dec_cmp(shop_setae, PRM_RN, PRM_RM), Native_i0011_nnnn_mmmm_0010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0011_nnnn_mmmm_0011(context, opcode),
            Mask_n_m, 0x3003, Normal, "cmp/ge <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.MT, 1,
            dec_cmp(shop_setge, PRM_RN, PRM_RM), Native_i0011_nnnn_mmmm_0011),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0011_nnnn_mmmm_0100(context, opcode),
            Mask_n_m, 0x3004, Normal, "div1 <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Fill(DM_ADC, PRM_RN, PRM_RM, shop_div1, 0), Native_i0011_nnnn_mmmm_0100),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0011_nnnn_mmmm_0101(context, opcode),
            Mask_n_m, 0x3005, Normal, "dmulu.l <REG_M>,<REG_N>", 2, 4, Sh4ExecutionUnit.CO, 34,
            dec_mul(64), Native_i0011_nnnn_mmmm_0101),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0011_nnnn_mmmm_0110(context, opcode),
            Mask_n_m, 0x3006, Normal, "cmp/hi <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.MT, 1,
            dec_cmp(shop_setab, PRM_RN, PRM_RM), Native_i0011_nnnn_mmmm_0110),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0011_nnnn_mmmm_0111(context, opcode),
            Mask_n_m, 0x3007, Normal, "cmp/gt <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.MT, 1,
            dec_cmp(shop_setgt, PRM_RN, PRM_RM), Native_i0011_nnnn_mmmm_0111),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0011_nnnn_mmmm_1000(context, opcode),
            Mask_n_m, 0x3008, Normal, "sub <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Bin_rNrM(shop_sub), Native_i0011_nnnn_mmmm_1000),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0011_nnnn_mmmm_1010(context, opcode),
            Mask_n_m, 0x300A, Normal, "subc <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Fill(DM_ADC, PRM_RN, PRM_RM, shop_sbc, -1), Native_i0011_nnnn_mmmm_1010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0011_nnnn_mmmm_1011(context, opcode),
            Mask_n_m, 0x300B, Normal, "subv <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            0, Native_i0011_nnnn_mmmm_1011),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0011_nnnn_mmmm_1100(context, opcode),
            Mask_n_m, 0x300C, Normal, "add <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Bin_rNrM(shop_add), Native_i0011_nnnn_mmmm_1100),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0011_nnnn_mmmm_1101(context, opcode),
            Mask_n_m, 0x300D, Normal, "dmuls.l <REG_M>,<REG_N>", 2, 4, Sh4ExecutionUnit.CO, 34,
            dec_mul(-64), Native_i0011_nnnn_mmmm_1101),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0011_nnnn_mmmm_1110(context, opcode),
            Mask_n_m, 0x300E, Normal, "addc <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Fill(DM_ADC, PRM_RN, PRM_RM, shop_adc, -1), Native_i0011_nnnn_mmmm_1110),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0011_nnnn_mmmm_1111(context, opcode),
            Mask_n_m, 0x300F, Normal, "addv <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            0, Native_i0011_nnnn_mmmm_1111),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_mmmm_0100(context, opcode),
            Mask_n_m, 0x0004, Normal, "mov.b <REG_M>,@(R0,<REG_N>)", 1, 1, Sh4ExecutionUnit.LS, 2,
            dec_MWt(PRM_RN_R0, PRM_RM, 1), Native_i0000_nnnn_mmmm_0100),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_mmmm_0101(context, opcode),
            Mask_n_m, 0x0005, Normal, "mov.w <REG_M>,@(R0,<REG_N>)", 1, 1, Sh4ExecutionUnit.LS, 2,
            dec_MWt(PRM_RN_R0, PRM_RM, 2), Native_i0000_nnnn_mmmm_0101),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_mmmm_0110(context, opcode),
            Mask_n_m, 0x0006, Normal, "mov.l <REG_M>,@(R0,<REG_N>)", 1, 1, Sh4ExecutionUnit.LS, 2,
            dec_MWt(PRM_RN_R0, PRM_RM, 4), Native_i0000_nnnn_mmmm_0110),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_mmmm_1100(context, opcode),
            Mask_n_m, 0x000C, Normal, "mov.b @(R0,<REG_M>),<REG_N>", 1, 2, Sh4ExecutionUnit.LS, 2,
            dec_MRd(PRM_RN, PRM_RM_R0, 1), Native_i0000_nnnn_mmmm_1100),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_mmmm_1101(context, opcode),
            Mask_n_m, 0x000D, Normal, "mov.w @(R0,<REG_M>),<REG_N>", 1, 2, Sh4ExecutionUnit.LS, 2,
            dec_MRd(PRM_RN, PRM_RM_R0, 2), Native_i0000_nnnn_mmmm_1101),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_mmmm_1110(context, opcode),
            Mask_n_m, 0x000E, Normal, "mov.l @(R0,<REG_M>),<REG_N>", 1, 2, Sh4ExecutionUnit.LS, 2,
            dec_MRd(PRM_RN, PRM_RM_R0, 4), Native_i0000_nnnn_mmmm_1110),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0001_nnnn_mmmm_iiii(context, opcode),
            Mask_n_imm8, 0x1000, Normal, "mov.l <REG_M>,@(<disp4dw>,<REG_N>)", 1, 1, Sh4ExecutionUnit.LS, 2,
            dec_MWt(PRM_RN_D4_x4, PRM_RM, 4), Native_i0001_nnnn_mmmm_iiii),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0101_nnnn_mmmm_iiii(context, opcode),
            Mask_n_m_imm4, 0x5000, Normal, "mov.l @(<disp4dw>,<REG_M>),<REG_N>", 1, 2, Sh4ExecutionUnit.LS, 2,
            dec_MRd(PRM_RN, PRM_RM_D4_x4, 4), Native_i0101_nnnn_mmmm_iiii),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0010_nnnn_mmmm_0000(context, opcode),
            Mask_n_m, 0x2000, Normal, "mov.b <REG_M>,@<REG_N>", 1, 1, Sh4ExecutionUnit.LS, 2,
            dec_MWt(PRM_RN, PRM_RM, 1), Native_i0010_nnnn_mmmm_0000),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0010_nnnn_mmmm_0001(context, opcode),
            Mask_n_m, 0x2001, Normal, "mov.w <REG_M>,@<REG_N>", 1, 1, Sh4ExecutionUnit.LS, 2,
            dec_MWt(PRM_RN, PRM_RM, 2), Native_i0010_nnnn_mmmm_0001),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0010_nnnn_mmmm_0010(context, opcode),
            Mask_n_m, 0x2002, Normal, "mov.l <REG_M>,@<REG_N>", 1, 1, Sh4ExecutionUnit.LS, 2,
            dec_MWt(PRM_RN, PRM_RM, 4), Native_i0010_nnnn_mmmm_0010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0110_nnnn_mmmm_0000(context, opcode),
            Mask_n_m, 0x6000, Normal, "mov.b @<REG_M>,<REG_N>", 1, 2, Sh4ExecutionUnit.LS, 2,
            dec_MRd(PRM_RN, PRM_RM, 1), Native_i0110_nnnn_mmmm_0000),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0110_nnnn_mmmm_0001(context, opcode),
            Mask_n_m, 0x6001, Normal, "mov.w @<REG_M>,<REG_N>", 1, 2, Sh4ExecutionUnit.LS, 2,
            dec_MRd(PRM_RN, PRM_RM, 2), Native_i0110_nnnn_mmmm_0001),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0110_nnnn_mmmm_0010(context, opcode),
            Mask_n_m, 0x6002, Normal, "mov.l @<REG_M>,<REG_N>", 1, 2, Sh4ExecutionUnit.LS, 2,
            dec_MRd(PRM_RN, PRM_RM, 4), Native_i0110_nnnn_mmmm_0010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0010_nnnn_mmmm_0100(context, opcode),
            Mask_n_m, 0x2004, Normal, "mov.b <REG_M>,@-<REG_N>", 1, 1, Sh4ExecutionUnit.LS, 2,
            dec_MWt(PRM_RN, PRM_RM, -1), Native_i0010_nnnn_mmmm_0100),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0010_nnnn_mmmm_0101(context, opcode),
            Mask_n_m, 0x2005, Normal, "mov.w <REG_M>,@-<REG_N>", 1, 1, Sh4ExecutionUnit.LS, 2,
            dec_MWt(PRM_RN, PRM_RM, -2), Native_i0010_nnnn_mmmm_0101),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0010_nnnn_mmmm_0110(context, opcode),
            Mask_n_m, 0x2006, Normal, "mov.l <REG_M>,@-<REG_N>", 1, 1, Sh4ExecutionUnit.LS, 2,
            dec_MWt(PRM_RN, PRM_RM, -4), Native_i0010_nnnn_mmmm_0110),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0110_nnnn_mmmm_0100(context, opcode),
            Mask_n_m, 0x6004, Normal, "mov.b @<REG_M>+,<REG_N>", 1, 2, Sh4ExecutionUnit.LS, 2,
            dec_MRd(PRM_RN, PRM_RM, -1), Native_i0110_nnnn_mmmm_0100),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0110_nnnn_mmmm_0101(context, opcode),
            Mask_n_m, 0x6005, Normal, "mov.w @<REG_M>+,<REG_N>", 1, 2, Sh4ExecutionUnit.LS, 2,
            dec_MRd(PRM_RN, PRM_RM, -2), Native_i0110_nnnn_mmmm_0101),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0110_nnnn_mmmm_0110(context, opcode),
            Mask_n_m, 0x6006, Normal, "mov.l @<REG_M>+,<REG_N>", 1, 2, Sh4ExecutionUnit.LS, 2,
            dec_MRd(PRM_RN, PRM_RM, -4), Native_i0110_nnnn_mmmm_0110),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1000_0000_mmmm_iiii(context, opcode),
            Mask_imm8, 0x8000, Normal, "mov.b R0,@(<disp4b>,<REG_M>)", 1, 1, Sh4ExecutionUnit.LS, 2,
            dec_MWt(PRM_RM_D4_x1, PRM_R0, 1), Native_i1000_0000_mmmm_iiii),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1000_0001_mmmm_iiii(context, opcode),
            Mask_imm8, 0x8100, Normal, "mov.w R0,@(<disp4w>,<REG_M>)", 1, 1, Sh4ExecutionUnit.LS, 2,
            dec_MWt(PRM_RM_D4_x2, PRM_R0, 2), Native_i1000_0001_mmmm_iiii),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1000_0100_mmmm_iiii(context, opcode),
            Mask_imm8, 0x8400, Normal, "mov.b @(<disp4b>,<REG_M>),R0", 1, 2, Sh4ExecutionUnit.LS, 2,
            dec_MRd(PRM_R0, PRM_RM_D4_x1, 1), Native_i1000_0100_mmmm_iiii),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1000_0101_mmmm_iiii(context, opcode),
            Mask_imm8, 0x8500, Normal, "mov.w @(<disp4w>,<REG_M>),R0", 1, 2, Sh4ExecutionUnit.LS, 2,
            dec_MRd(PRM_R0, PRM_RM_D4_x2, 2), Native_i1000_0101_mmmm_iiii),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1001_nnnn_iiii_iiii(context, opcode),
            Mask_n_imm8, 0x9000, ReadsPC, "mov.w @(<PCdisp8w>),<REG_N>", 1, 2, Sh4ExecutionUnit.LS, 2,
            dec_MRd(PRM_RN, PRM_PC_D8_x2, 2), Native_i1001_nnnn_iiii_iiii),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1100_0000_iiii_iiii(context, opcode),
            Mask_imm8, 0xC000, Normal, "mov.b R0,@(<disp8b>,GBR)", 1, 1, Sh4ExecutionUnit.LS, 3,
            dec_MWt(PRM_GBR_D8_x1, PRM_R0, 1), Native_i1100_0000_iiii_iiii),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1100_0001_iiii_iiii(context, opcode),
            Mask_imm8, 0xC100, Normal, "mov.w R0,@(<disp8w>,GBR)", 1, 1, Sh4ExecutionUnit.LS, 3,
            dec_MWt(PRM_GBR_D8_x2, PRM_R0, 2), Native_i1100_0001_iiii_iiii),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1100_0010_iiii_iiii(context, opcode),
            Mask_imm8, 0xC200, Normal, "mov.l R0,@(<disp8dw>,GBR)", 1, 1, Sh4ExecutionUnit.LS, 3,
            dec_MWt(PRM_GBR_D8_x4, PRM_R0, 4), Native_i1100_0010_iiii_iiii),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1100_0100_iiii_iiii(context, opcode),
            Mask_imm8, 0xC400, Normal, "mov.b @(<GBRdisp8b>),R0", 1, 2, Sh4ExecutionUnit.LS, 3,
            dec_MRd(PRM_R0, PRM_GBR_D8_x1, 1), Native_i1100_0100_iiii_iiii),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1100_0101_iiii_iiii(context, opcode),
            Mask_imm8, 0xC500, Normal, "mov.w @(<GBRdisp8w>),R0", 1, 2, Sh4ExecutionUnit.LS, 3,
            dec_MRd(PRM_R0, PRM_GBR_D8_x2, 2), Native_i1100_0101_iiii_iiii),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1100_0110_iiii_iiii(context, opcode),
            Mask_imm8, 0xC600, Normal, "mov.l @(<GBRdisp8dw>),R0", 1, 2, Sh4ExecutionUnit.LS, 3,
            dec_MRd(PRM_R0, PRM_GBR_D8_x4, 4), Native_i1100_0110_iiii_iiii),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1101_nnnn_iiii_iiii(context, opcode),
            Mask_n_imm8, 0xD000, ReadsPC, "mov.l @(<PCdisp8d>),<REG_N>", 1, 2, Sh4ExecutionUnit.LS, 2,
            dec_MRd(PRM_RN, PRM_PC_D8_x4, 4), Native_i1101_nnnn_iiii_iiii),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0110_nnnn_mmmm_0011(context, opcode),
            Mask_n_m, 0x6003, Normal, "mov <REG_M>,<REG_N>", 1, 0, Sh4ExecutionUnit.MT, 1,
            dec_Un_rNrM(shop_mov32), Native_i0110_nnnn_mmmm_0011),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1100_0111_iiii_iiii(context, opcode),
            Mask_imm8, 0xC700, ReadsPC, "mova @(<PCdisp8d>),R0", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Fill(DM_UnaryOp, PRM_R0, PRM_PC_D8_x4, shop_mov32), Native_i1100_0111_iiii_iiii),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1110_nnnn_iiii_iiii(context, opcode),
            Mask_n_imm8, 0xE000, Normal, "mov #<simm8hex>,<REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Fill(DM_UnaryOp, PRM_RN, PRM_SIMM8, shop_mov32), Native_i1110_nnnn_iiii_iiii),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0101_0010(context, opcode),
            Mask_n, 0x4052, UsesFPU, "sts.l FPUL,@-<REG_N>", 1, 1, Sh4ExecutionUnit.CO, 2,
            dec_STM(PRM_SREG), Native_i0100_nnnn_0101_0010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0110_0010(context, opcode),
            Mask_n, 0x4062, UsesFPU, "sts.l FPSCR,@-<REG_N>", 1, 1, Sh4ExecutionUnit.CO, 2,
            dec_STM(PRM_SREG), Native_i0100_nnnn_0110_0010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0000_0010(context, opcode),
            Mask_n, 0x4002, Normal, "sts.l MACH,@-<REG_N>", 1, 1, Sh4ExecutionUnit.CO, 31,
            dec_STM(PRM_SREG), Native_i0100_nnnn_0000_0010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0001_0010(context, opcode),
            Mask_n, 0x4012, Normal, "sts.l MACL,@-<REG_N>", 1, 1, Sh4ExecutionUnit.CO, 31,
            dec_STM(PRM_SREG), Native_i0100_nnnn_0001_0010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0010_0010(context, opcode),
            Mask_n, 0x4022, Normal, "sts.l PR,@-<REG_N>", 2, 2, Sh4ExecutionUnit.CO, 27,
            dec_STM(PRM_SREG), Native_i0100_nnnn_0010_0010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_1111_0010(context, opcode),
            Mask_n, 0x40F2, Normal, "stc.l DBR,@-<REG_N>", 2, 2, Sh4ExecutionUnit.CO, 22,
            dec_STM(PRM_SREG), Native_i0100_nnnn_1111_0010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0011_0010(context, opcode),
            Mask_n, 0x4032, Normal, "stc.l SGR,@-<REG_N>", 3, 3, Sh4ExecutionUnit.CO, 23,
            dec_STM(PRM_SREG), Native_i0100_nnnn_0011_0010),
        new(
            static (decoder, opcode) => decoder.Decode_i0100_nnnn_0000_0011(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0000_0011(context, opcode),
            Mask_n, 0x4003, Normal, "stc.l SR,@-<REG_N>", 2, 2, Sh4ExecutionUnit.CO, 22,
            0, Native_i0100_nnnn_0000_0011),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0001_0011(context, opcode),
            Mask_n, 0x4013, Normal, "stc.l GBR,@-<REG_N>", 2, 2, Sh4ExecutionUnit.CO, 22,
            dec_STM(PRM_CREG), Native_i0100_nnnn_0001_0011),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0010_0011(context, opcode),
            Mask_n, 0x4023, Normal, "stc.l VBR,@-<REG_N>", 2, 2, Sh4ExecutionUnit.CO, 22,
            dec_STM(PRM_CREG), Native_i0100_nnnn_0010_0011),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0011_0011(context, opcode),
            Mask_n, 0x4033, Normal, "stc.l SSR,@-<REG_N>", 2, 2, Sh4ExecutionUnit.CO, 22,
            dec_STM(PRM_CREG), Native_i0100_nnnn_0011_0011),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0100_0011(context, opcode),
            Mask_n, 0x4043, Normal, "stc.l SPC,@-<REG_N>", 2, 2, Sh4ExecutionUnit.CO, 22,
            dec_STM(PRM_CREG), Native_i0100_nnnn_0100_0011),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_1mmm_0011(context, opcode),
            Mask_n_ml3bit, 0x4083, Normal, "stc <RM_BANK>,@-<REG_N>", 2, 2, Sh4ExecutionUnit.CO, 22,
            dec_STM(PRM_CREG), Native_i0100_nnnn_1mmm_0011),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0000_0110(context, opcode),
            Mask_n, 0x4006, Normal, "lds.l @<REG_N>+,MACH", 1, 3, Sh4ExecutionUnit.CO, 29,
            dec_LDM(PRM_SREG), Native_i0100_nnnn_0000_0110),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0001_0110(context, opcode),
            Mask_n, 0x4016, Normal, "lds.l @<REG_N>+,MAC", 1, 3, Sh4ExecutionUnit.CO, 29,
            dec_LDM(PRM_SREG), Native_i0100_nnnn_0001_0110),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0010_0110(context, opcode),
            Mask_n, 0x4026, Normal, "lds.l @<REG_N>+,PR", 2, 3, Sh4ExecutionUnit.CO, 25,
            dec_LDM(PRM_SREG), Native_i0100_nnnn_0010_0110),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0011_0110(context, opcode),
            Mask_n, 0x4036, Normal, "ldc.l @<REG_N>+,SGR", 3, 3, Sh4ExecutionUnit.CO, 18,
            dec_LDM(PRM_SREG), Native_i0100_nnnn_0011_0110),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0101_0110(context, opcode),
            Mask_n, 0x4056, UsesFPU, "lds.l @<REG_N>+,FPUL", 1, 2, Sh4ExecutionUnit.CO, 2,
            dec_LDM(PRM_SREG), Native_i0100_nnnn_0101_0110),
        new(
            static (decoder, opcode) => decoder.Decode_i0100_nnnn_0110_0110(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0110_0110(context, opcode),
            Mask_n, 0x4066, FWritesFPSCR, "lds.l @<REG_N>+,FPSCR", 1, 4, Sh4ExecutionUnit.CO, 33,
            0, Native_i0100_nnnn_0110_0110),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_1111_0110(context, opcode),
            Mask_n, 0x40F6, Normal, "ldc.l @<REG_N>+,DBR", 1, 3, Sh4ExecutionUnit.CO, 17,
            dec_LDM(PRM_SREG), Native_i0100_nnnn_1111_0110),
        new(
            static (decoder, opcode) => decoder.Decode_i0100_nnnn_0000_0111(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0000_0111(context, opcode),
            Mask_n, 0x4007, WritesSRRWPC, "ldc.l @<REG_N>+,SR", 4, 4, Sh4ExecutionUnit.CO, 19,
            0, Native_i0100_nnnn_0000_0111),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0001_0111(context, opcode),
            Mask_n, 0x4017, Normal, "ldc.l @<REG_N>+,GBR", 3, 3, Sh4ExecutionUnit.CO, 18,
            dec_LDM(PRM_CREG), Native_i0100_nnnn_0001_0111),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0010_0111(context, opcode),
            Mask_n, 0x4027, Normal, "ldc.l @<REG_N>+,VBR", 1, 3, Sh4ExecutionUnit.CO, 17,
            dec_LDM(PRM_CREG), Native_i0100_nnnn_0010_0111),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0011_0111(context, opcode),
            Mask_n, 0x4037, Normal, "ldc.l @<REG_N>+,SSR", 1, 3, Sh4ExecutionUnit.CO, 17,
            dec_LDM(PRM_CREG), Native_i0100_nnnn_0011_0111),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0100_0111(context, opcode),
            Mask_n, 0x4047, Normal, "ldc.l @<REG_N>+,SPC", 1, 3, Sh4ExecutionUnit.CO, 17,
            dec_LDM(PRM_CREG), Native_i0100_nnnn_0100_0111),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_1mmm_0111(context, opcode),
            Mask_n_ml3bit, 0x4087, Normal, "ldc.l @<REG_N>+,RM_BANK", 1, 3, Sh4ExecutionUnit.CO, 17,
            dec_LDM(PRM_CREG), Native_i0100_nnnn_1mmm_0111),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_0000_0010(context, opcode),
            Mask_n, 0x0002, Normal, "stc SR,<REG_N>", 2, 2, Sh4ExecutionUnit.CO, 20,
            dec_STSRF(PRM_CREG), Native_i0000_nnnn_0000_0010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_0001_0010(context, opcode),
            Mask_n, 0x0012, Normal, "stc GBR,<REG_N>", 2, 2, Sh4ExecutionUnit.CO, 20,
            dec_ST(PRM_CREG), Native_i0000_nnnn_0001_0010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_0010_0010(context, opcode),
            Mask_n, 0x0022, Normal, "stc VBR,<REG_N>", 2, 2, Sh4ExecutionUnit.CO, 20,
            dec_ST(PRM_CREG), Native_i0000_nnnn_0010_0010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_0011_0010(context, opcode),
            Mask_n, 0x0032, Normal, "stc SSR,<REG_N>", 2, 2, Sh4ExecutionUnit.CO, 20,
            dec_ST(PRM_CREG), Native_i0000_nnnn_0011_0010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_0100_0010(context, opcode),
            Mask_n, 0x0042, Normal, "stc SPC,<REG_N>", 2, 2, Sh4ExecutionUnit.CO, 20,
            dec_ST(PRM_CREG), Native_i0000_nnnn_0100_0010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_1mmm_0010(context, opcode),
            Mask_n_ml3bit, 0x0082, Normal, "stc RM_BANK,<REG_N>", 2, 2, Sh4ExecutionUnit.CO, 20,
            dec_ST(PRM_CREG), Native_i0000_nnnn_1mmm_0010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_0000_1010(context, opcode),
            Mask_n, 0x000A, Normal, "sts MACH,<REG_N>", 1, 3, Sh4ExecutionUnit.CO, 30,
            dec_ST(PRM_SREG), Native_i0000_nnnn_0000_1010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_0001_1010(context, opcode),
            Mask_n, 0x001A, Normal, "sts MACL,<REG_N>", 1, 3, Sh4ExecutionUnit.CO, 30,
            dec_ST(PRM_SREG), Native_i0000_nnnn_0001_1010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_0010_1010(context, opcode),
            Mask_n, 0x002A, Normal, "sts PR,<REG_N>", 2, 2, Sh4ExecutionUnit.CO, 26,
            dec_ST(PRM_SREG), Native_i0000_nnnn_0010_1010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_0011_1010(context, opcode),
            Mask_n, 0x003A, Normal, "sts SGR,<REG_N>", 3, 3, Sh4ExecutionUnit.CO, 21,
            dec_ST(PRM_SREG), Native_i0000_nnnn_0011_1010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_0101_1010(context, opcode),
            Mask_n, 0x005A, UsesFPU, "sts FPUL,<REG_N>", 1, 3, Sh4ExecutionUnit.LS, 1,
            dec_ST(PRM_SREG), Native_i0000_nnnn_0101_1010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_0110_1010(context, opcode),
            Mask_n, 0x006A, UsesFPU, "sts FPSCR,<REG_N>", 1, 3, Sh4ExecutionUnit.CO, 1,
            dec_ST(PRM_SREG), Native_i0000_nnnn_0110_1010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0000_nnnn_1111_1010(context, opcode),
            Mask_n, 0x00FA, Normal, "sts DBR,<REG_N>", 2, 2, Sh4ExecutionUnit.CO, 20,
            dec_ST(PRM_SREG), Native_i0000_nnnn_1111_1010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0000_1010(context, opcode),
            Mask_n, 0x400A, Normal, "lds <REG_N>,MACH", 1, 3, Sh4ExecutionUnit.CO, 28,
            dec_LD(PRM_SREG), Native_i0100_nnnn_0000_1010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0001_1010(context, opcode),
            Mask_n, 0x401A, Normal, "lds <REG_N>,MAC", 1, 3, Sh4ExecutionUnit.CO, 28,
            dec_LD(PRM_SREG), Native_i0100_nnnn_0001_1010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0010_1010(context, opcode),
            Mask_n, 0x402A, Normal, "lds <REG_N>,PR", 2, 3, Sh4ExecutionUnit.CO, 24,
            dec_LD(PRM_SREG), Native_i0100_nnnn_0010_1010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0011_1010(context, opcode),
            Mask_n, 0x403A, Normal, "ldc <REG_N>,SGR", 3, 3, Sh4ExecutionUnit.CO, 15,
            dec_LD(PRM_SREG), Native_i0100_nnnn_0011_1010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0101_1010(context, opcode),
            Mask_n, 0x405A, UsesFPU, "lds <REG_N>,FPUL", 1, 1, Sh4ExecutionUnit.LS, 1,
            dec_LD(PRM_SREG), Native_i0100_nnnn_0101_1010),
        new(
            static (decoder, opcode) => decoder.Decode_i0100_nnnn_0110_1010(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0110_1010(context, opcode),
            Mask_n, 0x406A, FWritesFPSCR, "lds <REG_N>,FPSCR", 1, 4, Sh4ExecutionUnit.CO, 32,
            0, Native_i0100_nnnn_0110_1010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_1111_1010(context, opcode),
            Mask_n, 0x40FA, Normal, "ldc <REG_N>,DBR", 1, 3, Sh4ExecutionUnit.CO, 14,
            dec_LD(PRM_SREG), Native_i0100_nnnn_1111_1010),
        new(
            static (decoder, opcode) => decoder.Decode_i0100_nnnn_0000_1110(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0000_1110(context, opcode),
            Mask_n, 0x400E, WritesSRRWPC, "ldc <REG_N>,SR", 4, 4, Sh4ExecutionUnit.CO, 16,
            0, Native_i0100_nnnn_0000_1110),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0001_1110(context, opcode),
            Mask_n, 0x401E, Normal, "ldc <REG_N>,GBR", 3, 3, Sh4ExecutionUnit.CO, 15,
            dec_LD(PRM_CREG), Native_i0100_nnnn_0001_1110),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0010_1110(context, opcode),
            Mask_n, 0x402E, Normal, "ldc <REG_N>,VBR", 1, 3, Sh4ExecutionUnit.CO, 14,
            dec_LD(PRM_CREG), Native_i0100_nnnn_0010_1110),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0011_1110(context, opcode),
            Mask_n, 0x403E, Normal, "ldc <REG_N>,SSR", 1, 3, Sh4ExecutionUnit.CO, 14,
            dec_LD(PRM_CREG), Native_i0100_nnnn_0011_1110),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0100_1110(context, opcode),
            Mask_n, 0x404E, Normal, "ldc <REG_N>,SPC", 1, 3, Sh4ExecutionUnit.CO, 14,
            dec_LD(PRM_CREG), Native_i0100_nnnn_0100_1110),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_1mmm_1110(context, opcode),
            Mask_n_ml3bit, 0x408E, Normal, "ldc <REG_N>,<RM_BANK>", 1, 3, Sh4ExecutionUnit.CO, 14,
            dec_LD(PRM_CREG), Native_i0100_nnnn_1mmm_1110),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0000_0000(context, opcode),
            Mask_n, 0x4000, Normal, "shll <REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_shft(1, false), Native_i0100_nnnn_0000_0000),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0001_0000(context, opcode),
            Mask_n, 0x4010, Normal, "dt <REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Fill(DM_DT, PRM_RN, PRM_ONE, shop_sub), Native_i0100_nnnn_0001_0000),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0010_0000(context, opcode),
            Mask_n, 0x4020, Normal, "shal <REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_shft(1, true), Native_i0100_nnnn_0010_0000),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0000_0001(context, opcode),
            Mask_n, 0x4001, Normal, "shlr <REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_shft(-1, false), Native_i0100_nnnn_0000_0001),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0001_0001(context, opcode),
            Mask_n, 0x4011, Normal, "cmp/pz <REG_N>", 1, 1, Sh4ExecutionUnit.MT, 1,
            dec_cmp(shop_setge, PRM_RN, PRM_ZERO), Native_i0100_nnnn_0001_0001),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0010_0001(context, opcode),
            Mask_n, 0x4021, Normal, "shar <REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_shft(-1, true), Native_i0100_nnnn_0010_0001),
        new(
            static (decoder, opcode) => decoder.Decode_i0100_nnnn_0010_0100(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0010_0100(context, opcode),
            Mask_n, 0x4024, Normal, "rotcl <REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            0, Native_i0100_nnnn_0010_0100),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0000_0100(context, opcode),
            Mask_n, 0x4004, Normal, "rotl <REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Fill(DM_Rot, PRM_RN, PRM_RN, shop_ror, -31), Native_i0100_nnnn_0000_0100),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0001_0101(context, opcode),
            Mask_n, 0x4015, Normal, "cmp/pl <REG_N>", 1, 1, Sh4ExecutionUnit.MT, 1,
            dec_cmp(shop_setgt, PRM_RN, PRM_ZERO), Native_i0100_nnnn_0001_0101),
        new(
            static (decoder, opcode) => decoder.Decode_i0100_nnnn_0010_0101(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0010_0101(context, opcode),
            Mask_n, 0x4025, Normal, "rotcr <REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            0, Native_i0100_nnnn_0010_0101),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0000_0101(context, opcode),
            Mask_n, 0x4005, Normal, "rotr <REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Fill(DM_Rot, PRM_RN, PRM_RN, shop_ror, 1), Native_i0100_nnnn_0000_0101),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0000_1000(context, opcode),
            Mask_n, 0x4008, Normal, "shll2 <REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_shft(2, false), Native_i0100_nnnn_0000_1000),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0001_1000(context, opcode),
            Mask_n, 0x4018, Normal, "shll8 <REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_shft(8, false), Native_i0100_nnnn_0001_1000),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0010_1000(context, opcode),
            Mask_n, 0x4028, Normal, "shll16 <REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_shft(16, false), Native_i0100_nnnn_0010_1000),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0000_1001(context, opcode),
            Mask_n, 0x4009, Normal, "shlr2 <REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_shft(-2, false), Native_i0100_nnnn_0000_1001),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0001_1001(context, opcode),
            Mask_n, 0x4019, Normal, "shlr8 <REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_shft(-8, false), Native_i0100_nnnn_0001_1001),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0010_1001(context, opcode),
            Mask_n, 0x4029, Normal, "shlr16 <REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_shft(-16, false), Native_i0100_nnnn_0010_1001),
        new(
            static (decoder, opcode) => decoder.Decode_i0100_nnnn_0010_1011(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0010_1011(context, opcode),
            Mask_n, 0x402B, Branch_dir_d, "jmp @<REG_N>", 2, 3, Sh4ExecutionUnit.CO, 4,
            0, Native_i0100_nnnn_0010_1011),
        new(
            static (decoder, opcode) => decoder.Decode_i0100_nnnn_0000_1011(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0000_1011(context, opcode),
            Mask_n, 0x400B, Branch_dir_d, "jsr @<REG_N>", 2, 3, Sh4ExecutionUnit.CO, 24,
            0, Native_i0100_nnnn_0000_1011),
        new(
            static (decoder, opcode) => decoder.Decode_i0100_nnnn_0001_1011(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_0001_1011(context, opcode),
            Mask_n, 0x401B, Normal, "tas.b @<REG_N>", 5, 5, Sh4ExecutionUnit.CO, 7,
            0, Native_i0100_nnnn_0001_1011),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_mmmm_1100(context, opcode),
            Mask_n_m, 0x400C, Normal, "shad <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Bin_rNrM(shop_shad), Native_i0100_nnnn_mmmm_1100),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_mmmm_1101(context, opcode),
            Mask_n_m, 0x400D, Normal, "shld <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Bin_rNrM(shop_shld), Native_i0100_nnnn_mmmm_1101),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0100_nnnn_mmmm_1111(context, opcode),
            Mask_n_m, 0x400F, Normal, "mac.w @<REG_M>+,@<REG_N>+", 2, 4, Sh4ExecutionUnit.CO, 35,
            0, Native_i0100_nnnn_mmmm_1111),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0110_nnnn_mmmm_0111(context, opcode),
            Mask_n_m, 0x6007, Normal, "not <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Un_rNrM(shop_not), Native_i0110_nnnn_mmmm_0111),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0110_nnnn_mmmm_1000(context, opcode),
            Mask_n_m, 0x6008, Normal, "swap.b <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Un_rNrM(shop_swaplb), Native_i0110_nnnn_mmmm_1000),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0110_nnnn_mmmm_1001(context, opcode),
            Mask_n_m, 0x6009, Normal, "swap.w <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Fill(DM_Rot, PRM_RN, PRM_RM, shop_ror, 16|0x1000), Native_i0110_nnnn_mmmm_1001),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0110_nnnn_mmmm_1010(context, opcode),
            Mask_n_m, 0x600A, Normal, "negc <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Fill(DM_NEGC, PRM_RN, PRM_RM, shop_negc), Native_i0110_nnnn_mmmm_1010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0110_nnnn_mmmm_1011(context, opcode),
            Mask_n_m, 0x600B, Normal, "neg <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Un_rNrM(shop_neg), Native_i0110_nnnn_mmmm_1011),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0110_nnnn_mmmm_1100(context, opcode),
            Mask_n_m, 0x600C, Normal, "extu.b <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Fill(DM_EXTOP, PRM_RN, PRM_RM, shop_and, 1), Native_i0110_nnnn_mmmm_1100),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0110_nnnn_mmmm_1101(context, opcode),
            Mask_n_m, 0x600D, Normal, "extu.w <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Fill(DM_EXTOP, PRM_RN, PRM_RM, shop_and, 2), Native_i0110_nnnn_mmmm_1101),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0110_nnnn_mmmm_1110(context, opcode),
            Mask_n_m, 0x600E, Normal, "exts.b <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Un_rNrM(shop_ext_s8), Native_i0110_nnnn_mmmm_1110),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0110_nnnn_mmmm_1111(context, opcode),
            Mask_n_m, 0x600F, Normal, "exts.w <REG_M>,<REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Un_rNrM(shop_ext_s16), Native_i0110_nnnn_mmmm_1111),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i0111_nnnn_iiii_iiii(context, opcode),
            Mask_n_imm8, 0x7000, Normal, "add #<simm8>,<REG_N>", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Bin_S8R(shop_add), Native_i0111_nnnn_iiii_iiii),
        new(
            static (decoder, opcode) => decoder.Decode_i1000_1011_iiii_iiii(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i1000_1011_iiii_iiii(context, opcode),
            Mask_imm8, 0x8B00, Branch_rel, "bf <bdisp8>", 1, 2, Sh4ExecutionUnit.BR, 1,
            0, Native_i1000_1011_iiii_iiii),
        new(
            static (decoder, opcode) => decoder.Decode_i1000_1111_iiii_iiii(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i1000_1111_iiii_iiii(context, opcode),
            Mask_imm8, 0x8F00, Branch_rel_d, "bf.s <bdisp8>", 1, 2, Sh4ExecutionUnit.BR, 1,
            0, Native_i1000_1111_iiii_iiii),
        new(
            static (decoder, opcode) => decoder.Decode_i1000_1001_iiii_iiii(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i1000_1001_iiii_iiii(context, opcode),
            Mask_imm8, 0x8900, Branch_rel, "bt <bdisp8>", 1, 2, Sh4ExecutionUnit.BR, 1,
            0, Native_i1000_1001_iiii_iiii),
        new(
            static (decoder, opcode) => decoder.Decode_i1000_1101_iiii_iiii(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i1000_1101_iiii_iiii(context, opcode),
            Mask_imm8, 0x8D00, Branch_rel_d, "bt.s <bdisp8>", 1, 2, Sh4ExecutionUnit.BR, 1,
            0, Native_i1000_1101_iiii_iiii),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1000_1000_iiii_iiii(context, opcode),
            Mask_imm8, 0x8800, Normal, "cmp/eq #<simm8hex>,R0", 1, 1, Sh4ExecutionUnit.MT, 1,
            dec_cmp(shop_seteq, PRM_R0, PRM_SIMM8), Native_i1000_1000_iiii_iiii),
        new(
            static (decoder, opcode) => decoder.Decode_i1010_iiii_iiii_iiii(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i1010_iiii_iiii_iiii(context, opcode),
            Mask_n_imm8, 0xA000, Branch_rel_d, "bra <bdisp12>", 1, 2, Sh4ExecutionUnit.BR, 1,
            0, Native_i1010_iiii_iiii_iiii),
        new(
            static (decoder, opcode) => decoder.Decode_i1011_iiii_iiii_iiii(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i1011_iiii_iiii_iiii(context, opcode),
            Mask_n_imm8, 0xB000, Branch_rel_d, "bsr <bdisp12>", 1, 2, Sh4ExecutionUnit.BR, 14,
            0, Native_i1011_iiii_iiii_iiii),
        new(
            static (decoder, opcode) => decoder.Decode_i1100_0011_iiii_iiii(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i1100_0011_iiii_iiii(context, opcode),
            Mask_imm8, 0xC300, ReadWritePC, "trapa #<imm8>", 7, 7, Sh4ExecutionUnit.CO, 13,
            0, Native_i1100_0011_iiii_iiii),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1100_1000_iiii_iiii(context, opcode),
            Mask_imm8, 0xC800, Normal, "tst #<imm8>,R0", 1, 1, Sh4ExecutionUnit.MT, 1,
            dec_cmp(shop_test, PRM_R0, PRM_UIMM8), Native_i1100_1000_iiii_iiii),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1100_1001_iiii_iiii(context, opcode),
            Mask_imm8, 0xC900, Normal, "and #<imm8>,R0", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Bin_r0u8(shop_and), Native_i1100_1001_iiii_iiii),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1100_1010_iiii_iiii(context, opcode),
            Mask_imm8, 0xCA00, Normal, "xor #<imm8>,R0", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Bin_r0u8(shop_xor), Native_i1100_1010_iiii_iiii),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1100_1011_iiii_iiii(context, opcode),
            Mask_imm8, 0xCB00, Normal, "or #<imm8>,R0", 1, 1, Sh4ExecutionUnit.EX, 1,
            dec_Bin_r0u8(shop_or), Native_i1100_1011_iiii_iiii),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1100_1100_iiii_iiii(context, opcode),
            Mask_imm8, 0xCC00, Normal, "tst.b #<imm8>,@(R0,GBR)", 3, 3, Sh4ExecutionUnit.CO, 5,
            0, Native_i1100_1100_iiii_iiii),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1100_1101_iiii_iiii(context, opcode),
            Mask_imm8, 0xCD00, Normal, "and.b #<imm8>,@(R0,GBR)", 4, 4, Sh4ExecutionUnit.CO, 6,
            0, Native_i1100_1101_iiii_iiii),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1100_1110_iiii_iiii(context, opcode),
            Mask_imm8, 0xCE00, Normal, "xor.b #<imm8>,@(R0,GBR)", 4, 4, Sh4ExecutionUnit.CO, 6,
            0, Native_i1100_1110_iiii_iiii),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1100_1111_iiii_iiii(context, opcode),
            Mask_imm8, 0xCF00, Normal, "or.b #<imm8>,@(R0,GBR)", 4, 4, Sh4ExecutionUnit.CO, 6,
            0, Native_i1100_1111_iiii_iiii),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nnnn_mmmm_0000(context, opcode),
            Mask_n_m, 0xF000, UsesFPU, "fadd <FREG_M_SD_F>,<FREG_N>", 1, 4, Sh4ExecutionUnit.FE, 36,
            dec_Bin_frNfrM(shop_fadd), Native_i1111_nnnn_mmmm_0000),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nnnn_mmmm_0001(context, opcode),
            Mask_n_m, 0xF001, UsesFPU, "fsub <FREG_M_SD_F>,<FREG_N>", 1, 4, Sh4ExecutionUnit.FE, 36,
            dec_Bin_frNfrM(shop_fsub), Native_i1111_nnnn_mmmm_0001),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nnnn_mmmm_0010(context, opcode),
            Mask_n_m, 0xF002, UsesFPU, "fmul <FREG_M_SD_F>,<FREG_N>", 1, 4, Sh4ExecutionUnit.FE, 36,
            dec_Bin_frNfrM(shop_fmul), Native_i1111_nnnn_mmmm_0010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nnnn_mmmm_0011(context, opcode),
            Mask_n_m, 0xF003, UsesFPU, "fdiv <FREG_M_SD_F>,<FREG_N>", 1, 13, Sh4ExecutionUnit.FE, 37,
            dec_Bin_frNfrM(shop_fdiv), Native_i1111_nnnn_mmmm_0011),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nnnn_mmmm_0100(context, opcode),
            Mask_n_m, 0xF004, UsesFPU, "fcmp/eq <FREG_M>,<FREG_N>", 1, 4, Sh4ExecutionUnit.FE, 36,
            dec_cmp(shop_fseteq, PRM_FRN, PRM_FRM), Native_i1111_nnnn_mmmm_0100),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nnnn_mmmm_0101(context, opcode),
            Mask_n_m, 0xF005, UsesFPU, "fcmp/gt <FREG_M>,<FREG_N>", 1, 4, Sh4ExecutionUnit.FE, 36,
            dec_cmp(shop_fsetgt, PRM_FRN, PRM_FRM), Native_i1111_nnnn_mmmm_0101),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nnnn_mmmm_0110(context, opcode),
            Mask_n_m, 0xF006, UsesFPU, "fmov.s @(R0,<REG_M>),<FREG_N>", 1, 2, Sh4ExecutionUnit.LS, 2,
            dec_MRd(PRM_FRN_SZ, PRM_RM_R0, 4), Native_i1111_nnnn_mmmm_0110),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nnnn_mmmm_0111(context, opcode),
            Mask_n_m, 0xF007, UsesFPU, "fmov.s <FREG_M>,@(R0,<REG_N>)", 1, 1, Sh4ExecutionUnit.LS, 2,
            dec_MWt(PRM_RN_R0, PRM_FRM_SZ, 4), Native_i1111_nnnn_mmmm_0111),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nnnn_mmmm_1000(context, opcode),
            Mask_n_m, 0xF008, UsesFPU, "fmov.s @<REG_M>,<FREG_N>", 1, 2, Sh4ExecutionUnit.LS, 2,
            dec_MRd(PRM_FRN_SZ, PRM_RM, 4), Native_i1111_nnnn_mmmm_1000),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nnnn_mmmm_1001(context, opcode),
            Mask_n_m, 0xF009, UsesFPU, "fmov.s @<REG_M>+,<FREG_N>", 1, 2, Sh4ExecutionUnit.LS, 2,
            dec_MRd(PRM_FRN_SZ, PRM_RM, -4), Native_i1111_nnnn_mmmm_1001),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nnnn_mmmm_1010(context, opcode),
            Mask_n_m, 0xF00A, UsesFPU, "fmov.s <FREG_M>,@<REG_N>", 1, 1, Sh4ExecutionUnit.LS, 2,
            dec_MWt(PRM_RN, PRM_FRM_SZ, 4), Native_i1111_nnnn_mmmm_1010),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nnnn_mmmm_1011(context, opcode),
            Mask_n_m, 0xF00B, UsesFPU, "fmov.s <FREG_M>,@-<REG_N>", 1, 1, Sh4ExecutionUnit.LS, 2,
            dec_MWt(PRM_RN, PRM_FRM_SZ, -4), Native_i1111_nnnn_mmmm_1011),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nnnn_mmmm_1100(context, opcode),
            Mask_n_m, 0xF00C, UsesFPU, "fmov <FREG_M>,<FREG_N>", 1, 0, Sh4ExecutionUnit.LS, 1,
            dec_Fill(DM_UnaryOp, PRM_FRN_SZ, PRM_FRM_SZ, shop_mov32), Native_i1111_nnnn_mmmm_1100),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nnnn_0101_1101(context, opcode),
            Mask_n, 0xF05D, UsesFPU, "fabs <FREG_N>", 1, 0, Sh4ExecutionUnit.LS, 1,
            dec_Un_frNfrN(shop_fabs), Native_i1111_nnnn_0101_1101),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nnn0_1111_1101(context, opcode),
            Mask_nh3bit, 0xF0FD, UsesFPU, "FSCA FPUL, <DR_N>", 1, 3, Sh4ExecutionUnit.FE, 44,
            dec_Fill(DM_UnaryOp, PRM_FPN, PRM_FPUL, shop_fsca), Native_i1111_nnn0_1111_1101),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nnnn_1011_1101(context, opcode),
            Mask_n, 0xF0BD, UsesFPU, "fcnvds <DR_N>,FPUL", 1, 4, Sh4ExecutionUnit.FE, 38,
            0, Native_i1111_nnnn_1011_1101),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nnnn_1010_1101(context, opcode),
            Mask_n, 0xF0AD, UsesFPU, "fcnvsd FPUL,<DR_N>", 1, 4, Sh4ExecutionUnit.FE, 38,
            0, Native_i1111_nnnn_1010_1101),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nnmm_1110_1101(context, opcode),
            Mask_n, 0xF0ED, UsesFPU, "fipr <FV_M>,<FV_N>", 1, 5, Sh4ExecutionUnit.FE, 42,
            dec_Fill(DM_fiprOp, PRM_FVN, PRM_FVM, shop_fipr), Native_i1111_nnmm_1110_1101),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nnnn_1000_1101(context, opcode),
            Mask_n, 0xF08D, UsesFPU, "fldi0 <FREG_N>", 1, 0, Sh4ExecutionUnit.LS, 1,
            dec_Fill(DM_UnaryOp, PRM_FRN, PRM_ZERO, shop_mov32), Native_i1111_nnnn_1000_1101),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nnnn_1001_1101(context, opcode),
            Mask_n, 0xF09D, UsesFPU, "fldi1 <FREG_N>", 1, 0, Sh4ExecutionUnit.LS, 1,
            dec_Fill(DM_UnaryOp, PRM_FRN, PRM_ONE_F32, shop_mov32), Native_i1111_nnnn_1001_1101),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nnnn_0001_1101(context, opcode),
            Mask_n, 0xF01D, UsesFPU, "flds <FREG_N>,FPUL", 1, 0, Sh4ExecutionUnit.LS, 1,
            dec_Fill(DM_UnaryOp, PRM_FPUL, PRM_FRN, shop_mov32), Native_i1111_nnnn_0001_1101),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nnnn_0010_1101(context, opcode),
            Mask_n, 0xF02D, UsesFPU, "float FPUL,<FREG_N>", 1, 4, Sh4ExecutionUnit.FE, 36,
            dec_Fill(DM_UnaryOp, PRM_FRN, PRM_FPUL, shop_cvt_i2f_n), Native_i1111_nnnn_0010_1101),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nnnn_0100_1101(context, opcode),
            Mask_n, 0xF04D, UsesFPU, "fneg <FREG_N> ", 1, 0, Sh4ExecutionUnit.LS, 1,
            dec_Un_frNfrN(shop_fneg), Native_i1111_nnnn_0100_1101),
        new(
            static (decoder, opcode) => decoder.Decode_i1111_1011_1111_1101(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i1111_1011_1111_1101(context, opcode),
            Mask_none, 0xFBFD, FWritesFPSCR, "frchg", 1, 4, Sh4ExecutionUnit.FE, 36,
            0, Native_i1111_1011_1111_1101),
        new(
            static (decoder, opcode) => decoder.Decode_i1111_0011_1111_1101(opcode),
            static (interpreter, context, opcode) => interpreter.Execute_i1111_0011_1111_1101(context, opcode),
            Mask_none, 0xF3FD, FWritesFPSCR, "fschg", 1, 4, Sh4ExecutionUnit.FE, 36,
            0, Native_i1111_0011_1111_1101),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nnnn_0110_1101(context, opcode),
            Mask_n, 0xF06D, UsesFPU, "fsqrt <FREG_N>", 1, 23, Sh4ExecutionUnit.FE, 41,
            dec_Un_frNfrN(shop_fsqrt), Native_i1111_nnnn_0110_1101),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nnnn_0011_1101(context, opcode),
            Mask_n, 0xF03D, UsesFPU, "ftrc <FREG_N>, FPUL", 1, 4, Sh4ExecutionUnit.FE, 38,
            dec_Fill(DM_UnaryOp, PRM_FPUL, PRM_FRN, shop_cvt_f2i_t), Native_i1111_nnnn_0011_1101),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nnnn_0000_1101(context, opcode),
            Mask_n, 0xF00D, UsesFPU, "fsts FPUL,<FREG_N>", 1, 0, Sh4ExecutionUnit.LS, 1,
            dec_Fill(DM_UnaryOp, PRM_FRN, PRM_FPUL, shop_mov32), Native_i1111_nnnn_0000_1101),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nn01_1111_1101(context, opcode),
            Mask_nh2bit, 0xF1FD, UsesFPU, "ftrv xmtrx,<FV_N>", 1, 8, Sh4ExecutionUnit.FE, 43,
            dec_Fill(DM_BinaryOp, PRM_FVN, PRM_XMTRX, shop_ftrv, 1), Native_i1111_nn01_1111_1101),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nnnn_mmmm_1110(context, opcode),
            Mask_n_m, 0xF00E, UsesFPU, "fmac <FREG_0>,<FREG_M>,<FREG_N>", 1, 4, Sh4ExecutionUnit.FE, 36,
            dec_Fill(DM_BinaryOp, PRM_FRN, PRM_FRM_FR0, shop_fmac, 1), Native_i1111_nnnn_mmmm_1110),
        new(
            null,
            static (interpreter, context, opcode) => interpreter.Execute_i1111_nnnn_0111_1101(context, opcode),
            Mask_n, 0xF07D, UsesFPU, "FSRRA <FREG_N>", 1, 4, Sh4ExecutionUnit.FE, 36,
            dec_Un_frNfrN(shop_fsrra), Native_i1111_nnnn_0111_1101),
        new(null, null, 0, 0, ReadWritePC, null, 0, 0, default, 0, 0, 0),
    ];

    static Sh4OpcodeTable()
    {
        for (int index = 0; index < 0x10000; index++)
        {
            NativeOpcodeHandlers[index] = MissingOpcode.NativeInterpreterOpcode;
            OpcodeDescriptors[index] = MissingOpcode;
        }

        for (int definition = 0; Definitions[definition].NativeInterpreterOpcode != 0; definition++)
        {
            Sh4OpcodeDescriptor descriptor = Definitions[definition];
            if (descriptor.Format is null)
                descriptor.Format = "Unknown Opcode";

            int shift;
            uint count;
            uint mask = ~descriptor.Mask;
            uint baseOpcode = descriptor.Value;
            switch (descriptor.Mask)
            {
                case Mask_none:
                    count = 1;
                    shift = 0;
                    break;

                case Mask_n:
                    count = 16;
                    shift = 8;
                    break;

                case Mask_n_m:
                    count = 256;
                    shift = 4;
                    break;

                case Mask_n_m_imm4:
                    count = 256 * 16;
                    shift = 0;
                    break;

                case Mask_imm8:
                    count = 256;
                    shift = 0;
                    break;

                case Mask_n_ml3bit:
                    count = 256;
                    shift = 4;
                    break;

                case Mask_nh3bit:
                    count = 8;
                    shift = 9;
                    break;

                case Mask_nh2bit:
                    count = 4;
                    shift = 10;
                    break;

                default:
                    Sh4HostRuntime.Die("Error");
                    return;
            }

            for (uint index = 0; index < count; index++)
            {
                uint opcode = ((index << shift) & mask) + baseOpcode;
                NativeOpcodeHandlers[opcode] = descriptor.NativeInterpreterOpcode;
                OpcodeDescriptors[opcode] = descriptor;
            }
        }
    }

    private static Sh4Interpreter InterpreterForNativeEntry =>
#if SH4_DYNAREC_NONE
        Sh4Interpreter.Instance!;
#else
        Sh4RecompilerDriver.Current;
#endif

    private static nuint Native_iNotImplemented =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_iNotImplemented
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_iNotImplemented;

    [UnmanagedCallersOnly]
    private static void Entry_iNotImplemented(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_iNotImplemented(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_iNotImplemented(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_iNotImplemented(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_ReiosTrap =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_ReiosTrap
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_ReiosTrap;

    [UnmanagedCallersOnly]
    private static void Entry_ReiosTrap(nint contextAddress, uint opcode) =>
        Sh4Reios.Trap(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_ReiosTrap(nint contextAddress, uint opcode) =>
        Sh4Reios.Trap(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_0010_0011 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_0010_0011
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_0010_0011;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_0010_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_0010_0011(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_0010_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_0010_0011(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_0000_0011 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_0000_0011
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_0000_0011;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_0000_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_0000_0011(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_0000_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_0000_0011(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_1100_0011 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_1100_0011
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_1100_0011;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_1100_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_1100_0011(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_1100_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_1100_0011(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_1001_0011 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_1001_0011
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_1001_0011;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_1001_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_1001_0011(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_1001_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_1001_0011(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_1010_0011 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_1010_0011
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_1010_0011;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_1010_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_1010_0011(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_1010_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_1010_0011(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_1011_0011 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_1011_0011
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_1011_0011;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_1011_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_1011_0011(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_1011_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_1011_0011(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_1000_0011 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_1000_0011
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_1000_0011;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_1000_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_1000_0011(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_1000_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_1000_0011(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_mmmm_0111 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_mmmm_0111
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_mmmm_0111;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_mmmm_0111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_mmmm_0111(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_mmmm_0111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_mmmm_0111(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_0000_0010_1000 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_0000_0010_1000
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_0000_0010_1000;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_0000_0010_1000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_0000_0010_1000(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_0000_0010_1000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_0000_0010_1000(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_0000_0100_1000 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_0000_0100_1000
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_0000_0100_1000;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_0000_0100_1000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_0000_0100_1000(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_0000_0100_1000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_0000_0100_1000(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_0000_0000_1000 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_0000_0000_1000
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_0000_0000_1000;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_0000_0000_1000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_0000_0000_1000(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_0000_0000_1000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_0000_0000_1000(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_0000_0011_1000 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_0000_0011_1000
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_0000_0011_1000;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_0000_0011_1000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_0000_0011_1000(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_0000_0011_1000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_0000_0011_1000(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_0000_0101_1000 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_0000_0101_1000
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_0000_0101_1000;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_0000_0101_1000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_0000_0101_1000(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_0000_0101_1000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_0000_0101_1000(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_0000_0001_1000 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_0000_0001_1000
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_0000_0001_1000;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_0000_0001_1000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_0000_0001_1000(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_0000_0001_1000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_0000_0001_1000(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_0000_0001_1001 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_0000_0001_1001
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_0000_0001_1001;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_0000_0001_1001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_0000_0001_1001(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_0000_0001_1001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_0000_0001_1001(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_0010_1001 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_0010_1001
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_0010_1001;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_0010_1001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_0010_1001(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_0010_1001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_0010_1001(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_0000_0000_1001 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_0000_0000_1001
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_0000_0000_1001;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_0000_0000_1001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_0000_0000_1001(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_0000_0000_1001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_0000_0000_1001(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_0000_0010_1011 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_0000_0010_1011
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_0000_0010_1011;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_0000_0010_1011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_0000_0010_1011(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_0000_0010_1011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_0000_0010_1011(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_0000_0000_1011 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_0000_0000_1011
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_0000_0000_1011;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_0000_0000_1011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_0000_0000_1011(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_0000_0000_1011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_0000_0000_1011(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_0000_0001_1011 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_0000_0001_1011
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_0000_0001_1011;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_0000_0001_1011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_0000_0001_1011(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_0000_0001_1011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_0000_0001_1011(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_mmmm_1111 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_mmmm_1111
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_mmmm_1111;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_mmmm_1111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_mmmm_1111(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_mmmm_1111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_mmmm_1111(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0010_nnnn_mmmm_0111 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0010_nnnn_mmmm_0111
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0010_nnnn_mmmm_0111;

    [UnmanagedCallersOnly]
    private static void Entry_i0010_nnnn_mmmm_0111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_0111(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0010_nnnn_mmmm_0111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_0111(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0010_nnnn_mmmm_1000 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0010_nnnn_mmmm_1000
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0010_nnnn_mmmm_1000;

    [UnmanagedCallersOnly]
    private static void Entry_i0010_nnnn_mmmm_1000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_1000(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0010_nnnn_mmmm_1000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_1000(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0010_nnnn_mmmm_1001 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0010_nnnn_mmmm_1001
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0010_nnnn_mmmm_1001;

    [UnmanagedCallersOnly]
    private static void Entry_i0010_nnnn_mmmm_1001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_1001(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0010_nnnn_mmmm_1001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_1001(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0010_nnnn_mmmm_1010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0010_nnnn_mmmm_1010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0010_nnnn_mmmm_1010;

    [UnmanagedCallersOnly]
    private static void Entry_i0010_nnnn_mmmm_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_1010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0010_nnnn_mmmm_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_1010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0010_nnnn_mmmm_1011 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0010_nnnn_mmmm_1011
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0010_nnnn_mmmm_1011;

    [UnmanagedCallersOnly]
    private static void Entry_i0010_nnnn_mmmm_1011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_1011(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0010_nnnn_mmmm_1011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_1011(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0010_nnnn_mmmm_1100 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0010_nnnn_mmmm_1100
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0010_nnnn_mmmm_1100;

    [UnmanagedCallersOnly]
    private static void Entry_i0010_nnnn_mmmm_1100(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_1100(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0010_nnnn_mmmm_1100(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_1100(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0010_nnnn_mmmm_1101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0010_nnnn_mmmm_1101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0010_nnnn_mmmm_1101;

    [UnmanagedCallersOnly]
    private static void Entry_i0010_nnnn_mmmm_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_1101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0010_nnnn_mmmm_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_1101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0010_nnnn_mmmm_1110 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0010_nnnn_mmmm_1110
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0010_nnnn_mmmm_1110;

    [UnmanagedCallersOnly]
    private static void Entry_i0010_nnnn_mmmm_1110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_1110(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0010_nnnn_mmmm_1110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_1110(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0010_nnnn_mmmm_1111 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0010_nnnn_mmmm_1111
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0010_nnnn_mmmm_1111;

    [UnmanagedCallersOnly]
    private static void Entry_i0010_nnnn_mmmm_1111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_1111(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0010_nnnn_mmmm_1111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_1111(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0011_nnnn_mmmm_0000 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0011_nnnn_mmmm_0000
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0011_nnnn_mmmm_0000;

    [UnmanagedCallersOnly]
    private static void Entry_i0011_nnnn_mmmm_0000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0011_nnnn_mmmm_0000(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0011_nnnn_mmmm_0000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0011_nnnn_mmmm_0000(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0011_nnnn_mmmm_0010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0011_nnnn_mmmm_0010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0011_nnnn_mmmm_0010;

    [UnmanagedCallersOnly]
    private static void Entry_i0011_nnnn_mmmm_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0011_nnnn_mmmm_0010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0011_nnnn_mmmm_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0011_nnnn_mmmm_0010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0011_nnnn_mmmm_0011 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0011_nnnn_mmmm_0011
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0011_nnnn_mmmm_0011;

    [UnmanagedCallersOnly]
    private static void Entry_i0011_nnnn_mmmm_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0011_nnnn_mmmm_0011(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0011_nnnn_mmmm_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0011_nnnn_mmmm_0011(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0011_nnnn_mmmm_0100 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0011_nnnn_mmmm_0100
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0011_nnnn_mmmm_0100;

    [UnmanagedCallersOnly]
    private static void Entry_i0011_nnnn_mmmm_0100(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0011_nnnn_mmmm_0100(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0011_nnnn_mmmm_0100(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0011_nnnn_mmmm_0100(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0011_nnnn_mmmm_0101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0011_nnnn_mmmm_0101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0011_nnnn_mmmm_0101;

    [UnmanagedCallersOnly]
    private static void Entry_i0011_nnnn_mmmm_0101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0011_nnnn_mmmm_0101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0011_nnnn_mmmm_0101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0011_nnnn_mmmm_0101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0011_nnnn_mmmm_0110 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0011_nnnn_mmmm_0110
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0011_nnnn_mmmm_0110;

    [UnmanagedCallersOnly]
    private static void Entry_i0011_nnnn_mmmm_0110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0011_nnnn_mmmm_0110(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0011_nnnn_mmmm_0110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0011_nnnn_mmmm_0110(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0011_nnnn_mmmm_0111 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0011_nnnn_mmmm_0111
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0011_nnnn_mmmm_0111;

    [UnmanagedCallersOnly]
    private static void Entry_i0011_nnnn_mmmm_0111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0011_nnnn_mmmm_0111(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0011_nnnn_mmmm_0111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0011_nnnn_mmmm_0111(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0011_nnnn_mmmm_1000 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0011_nnnn_mmmm_1000
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0011_nnnn_mmmm_1000;

    [UnmanagedCallersOnly]
    private static void Entry_i0011_nnnn_mmmm_1000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0011_nnnn_mmmm_1000(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0011_nnnn_mmmm_1000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0011_nnnn_mmmm_1000(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0011_nnnn_mmmm_1010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0011_nnnn_mmmm_1010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0011_nnnn_mmmm_1010;

    [UnmanagedCallersOnly]
    private static void Entry_i0011_nnnn_mmmm_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0011_nnnn_mmmm_1010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0011_nnnn_mmmm_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0011_nnnn_mmmm_1010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0011_nnnn_mmmm_1011 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0011_nnnn_mmmm_1011
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0011_nnnn_mmmm_1011;

    [UnmanagedCallersOnly]
    private static void Entry_i0011_nnnn_mmmm_1011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0011_nnnn_mmmm_1011(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0011_nnnn_mmmm_1011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0011_nnnn_mmmm_1011(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0011_nnnn_mmmm_1100 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0011_nnnn_mmmm_1100
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0011_nnnn_mmmm_1100;

    [UnmanagedCallersOnly]
    private static void Entry_i0011_nnnn_mmmm_1100(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0011_nnnn_mmmm_1100(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0011_nnnn_mmmm_1100(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0011_nnnn_mmmm_1100(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0011_nnnn_mmmm_1101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0011_nnnn_mmmm_1101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0011_nnnn_mmmm_1101;

    [UnmanagedCallersOnly]
    private static void Entry_i0011_nnnn_mmmm_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0011_nnnn_mmmm_1101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0011_nnnn_mmmm_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0011_nnnn_mmmm_1101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0011_nnnn_mmmm_1110 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0011_nnnn_mmmm_1110
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0011_nnnn_mmmm_1110;

    [UnmanagedCallersOnly]
    private static void Entry_i0011_nnnn_mmmm_1110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0011_nnnn_mmmm_1110(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0011_nnnn_mmmm_1110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0011_nnnn_mmmm_1110(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0011_nnnn_mmmm_1111 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0011_nnnn_mmmm_1111
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0011_nnnn_mmmm_1111;

    [UnmanagedCallersOnly]
    private static void Entry_i0011_nnnn_mmmm_1111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0011_nnnn_mmmm_1111(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0011_nnnn_mmmm_1111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0011_nnnn_mmmm_1111(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_mmmm_0100 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_mmmm_0100
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_mmmm_0100;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_mmmm_0100(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_mmmm_0100(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_mmmm_0100(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_mmmm_0100(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_mmmm_0101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_mmmm_0101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_mmmm_0101;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_mmmm_0101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_mmmm_0101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_mmmm_0101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_mmmm_0101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_mmmm_0110 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_mmmm_0110
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_mmmm_0110;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_mmmm_0110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_mmmm_0110(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_mmmm_0110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_mmmm_0110(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_mmmm_1100 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_mmmm_1100
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_mmmm_1100;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_mmmm_1100(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_mmmm_1100(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_mmmm_1100(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_mmmm_1100(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_mmmm_1101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_mmmm_1101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_mmmm_1101;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_mmmm_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_mmmm_1101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_mmmm_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_mmmm_1101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_mmmm_1110 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_mmmm_1110
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_mmmm_1110;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_mmmm_1110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_mmmm_1110(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_mmmm_1110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_mmmm_1110(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0001_nnnn_mmmm_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0001_nnnn_mmmm_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0001_nnnn_mmmm_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i0001_nnnn_mmmm_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0001_nnnn_mmmm_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0001_nnnn_mmmm_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0001_nnnn_mmmm_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0101_nnnn_mmmm_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0101_nnnn_mmmm_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0101_nnnn_mmmm_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i0101_nnnn_mmmm_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0101_nnnn_mmmm_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0101_nnnn_mmmm_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0101_nnnn_mmmm_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0010_nnnn_mmmm_0000 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0010_nnnn_mmmm_0000
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0010_nnnn_mmmm_0000;

    [UnmanagedCallersOnly]
    private static void Entry_i0010_nnnn_mmmm_0000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_0000(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0010_nnnn_mmmm_0000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_0000(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0010_nnnn_mmmm_0001 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0010_nnnn_mmmm_0001
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0010_nnnn_mmmm_0001;

    [UnmanagedCallersOnly]
    private static void Entry_i0010_nnnn_mmmm_0001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_0001(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0010_nnnn_mmmm_0001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_0001(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0010_nnnn_mmmm_0010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0010_nnnn_mmmm_0010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0010_nnnn_mmmm_0010;

    [UnmanagedCallersOnly]
    private static void Entry_i0010_nnnn_mmmm_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_0010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0010_nnnn_mmmm_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_0010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0110_nnnn_mmmm_0000 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0110_nnnn_mmmm_0000
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0110_nnnn_mmmm_0000;

    [UnmanagedCallersOnly]
    private static void Entry_i0110_nnnn_mmmm_0000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_0000(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0110_nnnn_mmmm_0000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_0000(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0110_nnnn_mmmm_0001 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0110_nnnn_mmmm_0001
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0110_nnnn_mmmm_0001;

    [UnmanagedCallersOnly]
    private static void Entry_i0110_nnnn_mmmm_0001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_0001(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0110_nnnn_mmmm_0001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_0001(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0110_nnnn_mmmm_0010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0110_nnnn_mmmm_0010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0110_nnnn_mmmm_0010;

    [UnmanagedCallersOnly]
    private static void Entry_i0110_nnnn_mmmm_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_0010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0110_nnnn_mmmm_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_0010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0010_nnnn_mmmm_0100 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0010_nnnn_mmmm_0100
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0010_nnnn_mmmm_0100;

    [UnmanagedCallersOnly]
    private static void Entry_i0010_nnnn_mmmm_0100(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_0100(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0010_nnnn_mmmm_0100(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_0100(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0010_nnnn_mmmm_0101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0010_nnnn_mmmm_0101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0010_nnnn_mmmm_0101;

    [UnmanagedCallersOnly]
    private static void Entry_i0010_nnnn_mmmm_0101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_0101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0010_nnnn_mmmm_0101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_0101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0010_nnnn_mmmm_0110 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0010_nnnn_mmmm_0110
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0010_nnnn_mmmm_0110;

    [UnmanagedCallersOnly]
    private static void Entry_i0010_nnnn_mmmm_0110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_0110(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0010_nnnn_mmmm_0110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0010_nnnn_mmmm_0110(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0110_nnnn_mmmm_0100 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0110_nnnn_mmmm_0100
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0110_nnnn_mmmm_0100;

    [UnmanagedCallersOnly]
    private static void Entry_i0110_nnnn_mmmm_0100(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_0100(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0110_nnnn_mmmm_0100(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_0100(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0110_nnnn_mmmm_0101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0110_nnnn_mmmm_0101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0110_nnnn_mmmm_0101;

    [UnmanagedCallersOnly]
    private static void Entry_i0110_nnnn_mmmm_0101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_0101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0110_nnnn_mmmm_0101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_0101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0110_nnnn_mmmm_0110 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0110_nnnn_mmmm_0110
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0110_nnnn_mmmm_0110;

    [UnmanagedCallersOnly]
    private static void Entry_i0110_nnnn_mmmm_0110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_0110(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0110_nnnn_mmmm_0110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_0110(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1000_0000_mmmm_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1000_0000_mmmm_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1000_0000_mmmm_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1000_0000_mmmm_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1000_0000_mmmm_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1000_0000_mmmm_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1000_0000_mmmm_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1000_0001_mmmm_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1000_0001_mmmm_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1000_0001_mmmm_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1000_0001_mmmm_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1000_0001_mmmm_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1000_0001_mmmm_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1000_0001_mmmm_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1000_0100_mmmm_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1000_0100_mmmm_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1000_0100_mmmm_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1000_0100_mmmm_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1000_0100_mmmm_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1000_0100_mmmm_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1000_0100_mmmm_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1000_0101_mmmm_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1000_0101_mmmm_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1000_0101_mmmm_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1000_0101_mmmm_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1000_0101_mmmm_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1000_0101_mmmm_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1000_0101_mmmm_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1001_nnnn_iiii_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1001_nnnn_iiii_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1001_nnnn_iiii_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1001_nnnn_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1001_nnnn_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1001_nnnn_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1001_nnnn_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1100_0000_iiii_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1100_0000_iiii_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1100_0000_iiii_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1100_0000_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_0000_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1100_0000_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_0000_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1100_0001_iiii_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1100_0001_iiii_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1100_0001_iiii_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1100_0001_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_0001_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1100_0001_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_0001_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1100_0010_iiii_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1100_0010_iiii_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1100_0010_iiii_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1100_0010_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_0010_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1100_0010_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_0010_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1100_0100_iiii_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1100_0100_iiii_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1100_0100_iiii_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1100_0100_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_0100_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1100_0100_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_0100_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1100_0101_iiii_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1100_0101_iiii_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1100_0101_iiii_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1100_0101_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_0101_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1100_0101_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_0101_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1100_0110_iiii_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1100_0110_iiii_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1100_0110_iiii_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1100_0110_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_0110_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1100_0110_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_0110_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1101_nnnn_iiii_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1101_nnnn_iiii_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1101_nnnn_iiii_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1101_nnnn_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1101_nnnn_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1101_nnnn_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1101_nnnn_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0110_nnnn_mmmm_0011 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0110_nnnn_mmmm_0011
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0110_nnnn_mmmm_0011;

    [UnmanagedCallersOnly]
    private static void Entry_i0110_nnnn_mmmm_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_0011(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0110_nnnn_mmmm_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_0011(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1100_0111_iiii_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1100_0111_iiii_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1100_0111_iiii_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1100_0111_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_0111_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1100_0111_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_0111_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1110_nnnn_iiii_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1110_nnnn_iiii_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1110_nnnn_iiii_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1110_nnnn_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1110_nnnn_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1110_nnnn_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1110_nnnn_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0101_0010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0101_0010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0101_0010;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0101_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0101_0010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0101_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0101_0010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0110_0010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0110_0010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0110_0010;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0110_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0110_0010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0110_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0110_0010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0000_0010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0000_0010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0000_0010;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0000_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0000_0010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0000_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0000_0010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0001_0010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0001_0010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0001_0010;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0001_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0001_0010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0001_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0001_0010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0010_0010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0010_0010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0010_0010;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0010_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0010_0010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0010_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0010_0010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_1111_0010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_1111_0010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_1111_0010;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_1111_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_1111_0010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_1111_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_1111_0010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0011_0010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0011_0010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0011_0010;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0011_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0011_0010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0011_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0011_0010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0000_0011 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0000_0011
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0000_0011;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0000_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0000_0011(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0000_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0000_0011(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0001_0011 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0001_0011
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0001_0011;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0001_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0001_0011(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0001_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0001_0011(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0010_0011 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0010_0011
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0010_0011;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0010_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0010_0011(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0010_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0010_0011(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0011_0011 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0011_0011
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0011_0011;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0011_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0011_0011(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0011_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0011_0011(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0100_0011 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0100_0011
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0100_0011;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0100_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0100_0011(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0100_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0100_0011(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_1mmm_0011 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_1mmm_0011
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_1mmm_0011;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_1mmm_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_1mmm_0011(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_1mmm_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_1mmm_0011(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0000_0110 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0000_0110
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0000_0110;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0000_0110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0000_0110(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0000_0110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0000_0110(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0001_0110 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0001_0110
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0001_0110;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0001_0110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0001_0110(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0001_0110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0001_0110(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0010_0110 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0010_0110
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0010_0110;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0010_0110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0010_0110(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0010_0110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0010_0110(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0011_0110 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0011_0110
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0011_0110;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0011_0110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0011_0110(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0011_0110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0011_0110(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0101_0110 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0101_0110
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0101_0110;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0101_0110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0101_0110(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0101_0110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0101_0110(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0110_0110 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0110_0110
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0110_0110;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0110_0110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0110_0110(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0110_0110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0110_0110(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_1111_0110 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_1111_0110
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_1111_0110;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_1111_0110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_1111_0110(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_1111_0110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_1111_0110(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0000_0111 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0000_0111
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0000_0111;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0000_0111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0000_0111(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0000_0111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0000_0111(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0001_0111 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0001_0111
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0001_0111;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0001_0111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0001_0111(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0001_0111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0001_0111(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0010_0111 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0010_0111
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0010_0111;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0010_0111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0010_0111(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0010_0111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0010_0111(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0011_0111 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0011_0111
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0011_0111;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0011_0111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0011_0111(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0011_0111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0011_0111(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0100_0111 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0100_0111
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0100_0111;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0100_0111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0100_0111(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0100_0111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0100_0111(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_1mmm_0111 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_1mmm_0111
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_1mmm_0111;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_1mmm_0111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_1mmm_0111(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_1mmm_0111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_1mmm_0111(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_0000_0010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_0000_0010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_0000_0010;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_0000_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_0000_0010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_0000_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_0000_0010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_0001_0010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_0001_0010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_0001_0010;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_0001_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_0001_0010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_0001_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_0001_0010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_0010_0010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_0010_0010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_0010_0010;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_0010_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_0010_0010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_0010_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_0010_0010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_0011_0010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_0011_0010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_0011_0010;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_0011_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_0011_0010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_0011_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_0011_0010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_0100_0010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_0100_0010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_0100_0010;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_0100_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_0100_0010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_0100_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_0100_0010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_1mmm_0010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_1mmm_0010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_1mmm_0010;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_1mmm_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_1mmm_0010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_1mmm_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_1mmm_0010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_0000_1010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_0000_1010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_0000_1010;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_0000_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_0000_1010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_0000_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_0000_1010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_0001_1010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_0001_1010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_0001_1010;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_0001_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_0001_1010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_0001_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_0001_1010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_0010_1010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_0010_1010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_0010_1010;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_0010_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_0010_1010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_0010_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_0010_1010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_0011_1010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_0011_1010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_0011_1010;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_0011_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_0011_1010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_0011_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_0011_1010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_0101_1010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_0101_1010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_0101_1010;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_0101_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_0101_1010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_0101_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_0101_1010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_0110_1010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_0110_1010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_0110_1010;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_0110_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_0110_1010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_0110_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_0110_1010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0000_nnnn_1111_1010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0000_nnnn_1111_1010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0000_nnnn_1111_1010;

    [UnmanagedCallersOnly]
    private static void Entry_i0000_nnnn_1111_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_1111_1010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0000_nnnn_1111_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0000_nnnn_1111_1010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0000_1010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0000_1010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0000_1010;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0000_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0000_1010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0000_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0000_1010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0001_1010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0001_1010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0001_1010;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0001_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0001_1010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0001_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0001_1010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0010_1010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0010_1010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0010_1010;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0010_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0010_1010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0010_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0010_1010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0011_1010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0011_1010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0011_1010;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0011_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0011_1010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0011_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0011_1010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0101_1010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0101_1010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0101_1010;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0101_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0101_1010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0101_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0101_1010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0110_1010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0110_1010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0110_1010;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0110_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0110_1010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0110_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0110_1010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_1111_1010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_1111_1010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_1111_1010;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_1111_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_1111_1010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_1111_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_1111_1010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0000_1110 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0000_1110
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0000_1110;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0000_1110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0000_1110(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0000_1110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0000_1110(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0001_1110 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0001_1110
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0001_1110;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0001_1110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0001_1110(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0001_1110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0001_1110(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0010_1110 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0010_1110
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0010_1110;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0010_1110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0010_1110(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0010_1110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0010_1110(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0011_1110 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0011_1110
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0011_1110;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0011_1110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0011_1110(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0011_1110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0011_1110(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0100_1110 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0100_1110
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0100_1110;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0100_1110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0100_1110(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0100_1110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0100_1110(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_1mmm_1110 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_1mmm_1110
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_1mmm_1110;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_1mmm_1110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_1mmm_1110(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_1mmm_1110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_1mmm_1110(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0000_0000 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0000_0000
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0000_0000;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0000_0000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0000_0000(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0000_0000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0000_0000(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0001_0000 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0001_0000
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0001_0000;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0001_0000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0001_0000(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0001_0000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0001_0000(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0010_0000 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0010_0000
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0010_0000;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0010_0000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0010_0000(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0010_0000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0010_0000(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0000_0001 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0000_0001
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0000_0001;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0000_0001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0000_0001(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0000_0001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0000_0001(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0001_0001 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0001_0001
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0001_0001;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0001_0001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0001_0001(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0001_0001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0001_0001(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0010_0001 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0010_0001
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0010_0001;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0010_0001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0010_0001(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0010_0001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0010_0001(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0010_0100 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0010_0100
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0010_0100;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0010_0100(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0010_0100(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0010_0100(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0010_0100(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0000_0100 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0000_0100
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0000_0100;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0000_0100(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0000_0100(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0000_0100(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0000_0100(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0001_0101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0001_0101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0001_0101;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0001_0101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0001_0101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0001_0101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0001_0101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0010_0101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0010_0101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0010_0101;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0010_0101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0010_0101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0010_0101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0010_0101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0000_0101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0000_0101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0000_0101;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0000_0101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0000_0101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0000_0101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0000_0101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0000_1000 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0000_1000
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0000_1000;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0000_1000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0000_1000(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0000_1000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0000_1000(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0001_1000 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0001_1000
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0001_1000;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0001_1000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0001_1000(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0001_1000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0001_1000(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0010_1000 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0010_1000
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0010_1000;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0010_1000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0010_1000(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0010_1000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0010_1000(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0000_1001 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0000_1001
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0000_1001;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0000_1001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0000_1001(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0000_1001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0000_1001(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0001_1001 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0001_1001
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0001_1001;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0001_1001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0001_1001(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0001_1001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0001_1001(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0010_1001 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0010_1001
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0010_1001;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0010_1001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0010_1001(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0010_1001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0010_1001(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0010_1011 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0010_1011
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0010_1011;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0010_1011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0010_1011(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0010_1011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0010_1011(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0000_1011 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0000_1011
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0000_1011;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0000_1011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0000_1011(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0000_1011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0000_1011(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_0001_1011 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_0001_1011
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_0001_1011;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_0001_1011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0001_1011(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_0001_1011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_0001_1011(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_mmmm_1100 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_mmmm_1100
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_mmmm_1100;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_mmmm_1100(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_mmmm_1100(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_mmmm_1100(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_mmmm_1100(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_mmmm_1101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_mmmm_1101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_mmmm_1101;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_mmmm_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_mmmm_1101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_mmmm_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_mmmm_1101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0100_nnnn_mmmm_1111 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0100_nnnn_mmmm_1111
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0100_nnnn_mmmm_1111;

    [UnmanagedCallersOnly]
    private static void Entry_i0100_nnnn_mmmm_1111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_mmmm_1111(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0100_nnnn_mmmm_1111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0100_nnnn_mmmm_1111(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0110_nnnn_mmmm_0111 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0110_nnnn_mmmm_0111
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0110_nnnn_mmmm_0111;

    [UnmanagedCallersOnly]
    private static void Entry_i0110_nnnn_mmmm_0111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_0111(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0110_nnnn_mmmm_0111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_0111(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0110_nnnn_mmmm_1000 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0110_nnnn_mmmm_1000
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0110_nnnn_mmmm_1000;

    [UnmanagedCallersOnly]
    private static void Entry_i0110_nnnn_mmmm_1000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_1000(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0110_nnnn_mmmm_1000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_1000(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0110_nnnn_mmmm_1001 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0110_nnnn_mmmm_1001
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0110_nnnn_mmmm_1001;

    [UnmanagedCallersOnly]
    private static void Entry_i0110_nnnn_mmmm_1001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_1001(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0110_nnnn_mmmm_1001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_1001(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0110_nnnn_mmmm_1010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0110_nnnn_mmmm_1010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0110_nnnn_mmmm_1010;

    [UnmanagedCallersOnly]
    private static void Entry_i0110_nnnn_mmmm_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_1010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0110_nnnn_mmmm_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_1010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0110_nnnn_mmmm_1011 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0110_nnnn_mmmm_1011
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0110_nnnn_mmmm_1011;

    [UnmanagedCallersOnly]
    private static void Entry_i0110_nnnn_mmmm_1011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_1011(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0110_nnnn_mmmm_1011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_1011(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0110_nnnn_mmmm_1100 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0110_nnnn_mmmm_1100
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0110_nnnn_mmmm_1100;

    [UnmanagedCallersOnly]
    private static void Entry_i0110_nnnn_mmmm_1100(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_1100(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0110_nnnn_mmmm_1100(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_1100(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0110_nnnn_mmmm_1101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0110_nnnn_mmmm_1101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0110_nnnn_mmmm_1101;

    [UnmanagedCallersOnly]
    private static void Entry_i0110_nnnn_mmmm_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_1101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0110_nnnn_mmmm_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_1101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0110_nnnn_mmmm_1110 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0110_nnnn_mmmm_1110
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0110_nnnn_mmmm_1110;

    [UnmanagedCallersOnly]
    private static void Entry_i0110_nnnn_mmmm_1110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_1110(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0110_nnnn_mmmm_1110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_1110(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0110_nnnn_mmmm_1111 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0110_nnnn_mmmm_1111
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0110_nnnn_mmmm_1111;

    [UnmanagedCallersOnly]
    private static void Entry_i0110_nnnn_mmmm_1111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_1111(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0110_nnnn_mmmm_1111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0110_nnnn_mmmm_1111(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i0111_nnnn_iiii_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i0111_nnnn_iiii_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i0111_nnnn_iiii_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i0111_nnnn_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0111_nnnn_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i0111_nnnn_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i0111_nnnn_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1000_1011_iiii_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1000_1011_iiii_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1000_1011_iiii_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1000_1011_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1000_1011_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1000_1011_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1000_1011_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1000_1111_iiii_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1000_1111_iiii_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1000_1111_iiii_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1000_1111_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1000_1111_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1000_1111_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1000_1111_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1000_1001_iiii_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1000_1001_iiii_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1000_1001_iiii_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1000_1001_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1000_1001_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1000_1001_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1000_1001_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1000_1101_iiii_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1000_1101_iiii_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1000_1101_iiii_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1000_1101_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1000_1101_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1000_1101_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1000_1101_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1000_1000_iiii_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1000_1000_iiii_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1000_1000_iiii_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1000_1000_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1000_1000_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1000_1000_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1000_1000_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1010_iiii_iiii_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1010_iiii_iiii_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1010_iiii_iiii_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1010_iiii_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1010_iiii_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1010_iiii_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1010_iiii_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1011_iiii_iiii_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1011_iiii_iiii_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1011_iiii_iiii_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1011_iiii_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1011_iiii_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1011_iiii_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1011_iiii_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1100_0011_iiii_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1100_0011_iiii_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1100_0011_iiii_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1100_0011_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_0011_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1100_0011_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_0011_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1100_1000_iiii_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1100_1000_iiii_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1100_1000_iiii_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1100_1000_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_1000_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1100_1000_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_1000_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1100_1001_iiii_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1100_1001_iiii_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1100_1001_iiii_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1100_1001_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_1001_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1100_1001_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_1001_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1100_1010_iiii_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1100_1010_iiii_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1100_1010_iiii_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1100_1010_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_1010_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1100_1010_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_1010_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1100_1011_iiii_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1100_1011_iiii_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1100_1011_iiii_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1100_1011_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_1011_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1100_1011_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_1011_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1100_1100_iiii_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1100_1100_iiii_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1100_1100_iiii_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1100_1100_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_1100_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1100_1100_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_1100_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1100_1101_iiii_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1100_1101_iiii_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1100_1101_iiii_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1100_1101_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_1101_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1100_1101_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_1101_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1100_1110_iiii_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1100_1110_iiii_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1100_1110_iiii_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1100_1110_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_1110_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1100_1110_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_1110_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1100_1111_iiii_iiii =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1100_1111_iiii_iiii
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1100_1111_iiii_iiii;

    [UnmanagedCallersOnly]
    private static void Entry_i1100_1111_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_1111_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1100_1111_iiii_iiii(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1100_1111_iiii_iiii(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nnnn_mmmm_0000 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nnnn_mmmm_0000
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nnnn_mmmm_0000;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nnnn_mmmm_0000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_mmmm_0000(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nnnn_mmmm_0000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_mmmm_0000(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nnnn_mmmm_0001 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nnnn_mmmm_0001
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nnnn_mmmm_0001;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nnnn_mmmm_0001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_mmmm_0001(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nnnn_mmmm_0001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_mmmm_0001(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nnnn_mmmm_0010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nnnn_mmmm_0010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nnnn_mmmm_0010;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nnnn_mmmm_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_mmmm_0010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nnnn_mmmm_0010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_mmmm_0010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nnnn_mmmm_0011 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nnnn_mmmm_0011
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nnnn_mmmm_0011;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nnnn_mmmm_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_mmmm_0011(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nnnn_mmmm_0011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_mmmm_0011(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nnnn_mmmm_0100 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nnnn_mmmm_0100
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nnnn_mmmm_0100;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nnnn_mmmm_0100(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_mmmm_0100(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nnnn_mmmm_0100(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_mmmm_0100(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nnnn_mmmm_0101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nnnn_mmmm_0101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nnnn_mmmm_0101;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nnnn_mmmm_0101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_mmmm_0101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nnnn_mmmm_0101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_mmmm_0101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nnnn_mmmm_0110 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nnnn_mmmm_0110
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nnnn_mmmm_0110;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nnnn_mmmm_0110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_mmmm_0110(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nnnn_mmmm_0110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_mmmm_0110(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nnnn_mmmm_0111 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nnnn_mmmm_0111
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nnnn_mmmm_0111;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nnnn_mmmm_0111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_mmmm_0111(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nnnn_mmmm_0111(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_mmmm_0111(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nnnn_mmmm_1000 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nnnn_mmmm_1000
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nnnn_mmmm_1000;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nnnn_mmmm_1000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_mmmm_1000(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nnnn_mmmm_1000(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_mmmm_1000(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nnnn_mmmm_1001 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nnnn_mmmm_1001
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nnnn_mmmm_1001;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nnnn_mmmm_1001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_mmmm_1001(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nnnn_mmmm_1001(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_mmmm_1001(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nnnn_mmmm_1010 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nnnn_mmmm_1010
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nnnn_mmmm_1010;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nnnn_mmmm_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_mmmm_1010(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nnnn_mmmm_1010(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_mmmm_1010(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nnnn_mmmm_1011 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nnnn_mmmm_1011
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nnnn_mmmm_1011;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nnnn_mmmm_1011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_mmmm_1011(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nnnn_mmmm_1011(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_mmmm_1011(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nnnn_mmmm_1100 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nnnn_mmmm_1100
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nnnn_mmmm_1100;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nnnn_mmmm_1100(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_mmmm_1100(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nnnn_mmmm_1100(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_mmmm_1100(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nnnn_0101_1101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nnnn_0101_1101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nnnn_0101_1101;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nnnn_0101_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_0101_1101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nnnn_0101_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_0101_1101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nnn0_1111_1101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nnn0_1111_1101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nnn0_1111_1101;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nnn0_1111_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnn0_1111_1101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nnn0_1111_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnn0_1111_1101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nnnn_1011_1101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nnnn_1011_1101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nnnn_1011_1101;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nnnn_1011_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_1011_1101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nnnn_1011_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_1011_1101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nnnn_1010_1101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nnnn_1010_1101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nnnn_1010_1101;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nnnn_1010_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_1010_1101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nnnn_1010_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_1010_1101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nnmm_1110_1101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nnmm_1110_1101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nnmm_1110_1101;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nnmm_1110_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnmm_1110_1101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nnmm_1110_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnmm_1110_1101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nnnn_1000_1101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nnnn_1000_1101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nnnn_1000_1101;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nnnn_1000_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_1000_1101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nnnn_1000_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_1000_1101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nnnn_1001_1101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nnnn_1001_1101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nnnn_1001_1101;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nnnn_1001_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_1001_1101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nnnn_1001_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_1001_1101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nnnn_0001_1101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nnnn_0001_1101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nnnn_0001_1101;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nnnn_0001_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_0001_1101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nnnn_0001_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_0001_1101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nnnn_0010_1101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nnnn_0010_1101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nnnn_0010_1101;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nnnn_0010_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_0010_1101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nnnn_0010_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_0010_1101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nnnn_0100_1101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nnnn_0100_1101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nnnn_0100_1101;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nnnn_0100_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_0100_1101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nnnn_0100_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_0100_1101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_1011_1111_1101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_1011_1111_1101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_1011_1111_1101;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_1011_1111_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_1011_1111_1101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_1011_1111_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_1011_1111_1101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_0011_1111_1101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_0011_1111_1101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_0011_1111_1101;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_0011_1111_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_0011_1111_1101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_0011_1111_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_0011_1111_1101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nnnn_0110_1101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nnnn_0110_1101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nnnn_0110_1101;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nnnn_0110_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_0110_1101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nnnn_0110_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_0110_1101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nnnn_0011_1101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nnnn_0011_1101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nnnn_0011_1101;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nnnn_0011_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_0011_1101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nnnn_0011_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_0011_1101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nnnn_0000_1101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nnnn_0000_1101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nnnn_0000_1101;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nnnn_0000_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_0000_1101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nnnn_0000_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_0000_1101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nn01_1111_1101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nn01_1111_1101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nn01_1111_1101;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nn01_1111_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nn01_1111_1101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nn01_1111_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nn01_1111_1101(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nnnn_mmmm_1110 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nnnn_mmmm_1110
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nnnn_mmmm_1110;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nnnn_mmmm_1110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_mmmm_1110(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nnnn_mmmm_1110(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_mmmm_1110(Sh4Context.FromNative(contextAddress), opcode);

    private static nuint Native_i1111_nnnn_0111_1101 =>
        RuntimeInformation.ProcessArchitecture == Architecture.X86
            ? (nuint)(delegate* unmanaged[Fastcall]<nint, uint, void>)&EntryX86_i1111_nnnn_0111_1101
            : (nuint)(delegate* unmanaged<nint, uint, void>)&Entry_i1111_nnnn_0111_1101;

    [UnmanagedCallersOnly]
    private static void Entry_i1111_nnnn_0111_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_0111_1101(Sh4Context.FromNative(contextAddress), opcode);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvFastcall) })]
    private static void EntryX86_i1111_nnnn_0111_1101(nint contextAddress, uint opcode) =>
        InterpreterForNativeEntry.Execute_i1111_nnnn_0111_1101(Sh4Context.FromNative(contextAddress), opcode);

    private static bool MatchTag(string format, ref int index, string tag)
    {
        int size = 0;
        while (size < tag.Length)
        {
            if (index + size == format.Length || format[index + size] != tag[size])
                return false;

            size++;
        }

        index += size;
        return true;
    }

    internal static string Disassemble(string format, uint pc, ushort opcode, Sh4Context context)
    {
        StringBuilder registers = new();
        StringBuilder text = new();
        int terminator = format.IndexOf('\0');
        if (terminator >= 0)
            format = format[..terminator];

        int n = (opcode >> 8) & 0xF;
        int m = (opcode >> 4) & 0xF;
        uint imm4 = (uint)(opcode & 0xF);
        uint imm8 = (uint)(opcode & 0xFF);
        uint imm12 = (uint)(opcode & 0xFFF);

        int index = 0;
        while (index < format.Length)
        {
            if (format[index] == '<' && index + 1 < format.Length)
            {
                index++;
                string replacement;
                if (MatchTag(format, ref index, "REG_N>"))
                {
                    registers.Append(CultureInfo.InvariantCulture, $"R{n}={context.Registers[n]:x} ");
                    replacement = $"R{n}";
                }
                else if (MatchTag(format, ref index, "REG_M>"))
                {
                    registers.Append(CultureInfo.InvariantCulture, $"R{m}={context.Registers[m]:x} ");
                    replacement = $"R{m}";
                }
                else if (MatchTag(format, ref index, "FREG_N>"))
                {
                    registers.Append(CultureInfo.InvariantCulture, $"FR{n}={context.FloatingRegisters[n]:F6} ");
                    replacement = $"FR{n}";
                }
                else if (MatchTag(format, ref index, "FREG_M>"))
                {
                    registers.Append(CultureInfo.InvariantCulture, $"FR{m}={context.FloatingRegisters[m]:F6} ");
                    replacement = $"FR{m}";
                }
                else if (MatchTag(format, ref index, "FREG_M_SD_F>"))
                {
                    registers.Append(CultureInfo.InvariantCulture, $"FR{m}={context.FloatingRegisters[m]:F6} ");
                    replacement = $"FR{m}";
                }
                else if (MatchTag(format, ref index, "FREG_0>"))
                {
                    registers.Append(CultureInfo.InvariantCulture, $"FR0={context.FloatingRegisters[0]:F6} ");
                    replacement = "FR0";
                }
                else if (MatchTag(format, ref index, "RM_BANK>"))
                {
                    replacement = $"R{m & 7}_BANK";
                }
                else if (MatchTag(format, ref index, "DFREG_N>"))
                {
                    replacement = $"DR{n >> 1}";
                }
                else if (MatchTag(format, ref index, "DFREG_M>"))
                {
                    replacement = $"DR{m >> 1}";
                }
                else if (MatchTag(format, ref index, "XDFREG_N>"))
                {
                    if ((n & 1) != 0)
                        replacement = $"XD{n >> 1}";
                    else
                        replacement = $"DR{n >> 1}";
                }
                else if (MatchTag(format, ref index, "XDFREG_M>"))
                {
                    if ((m & 1) != 0)
                        replacement = $"XD{m >> 1}";
                    else
                        replacement = $"DR{m >> 1}";
                }
                else if (MatchTag(format, ref index, "disp4b>"))
                {
                    replacement = $"0x{imm4 * 1:X}";
                }
                else if (MatchTag(format, ref index, "disp4w>"))
                {
                    replacement = $"0x{imm4 * 2:X}";
                }
                else if (MatchTag(format, ref index, "disp4dw>"))
                {
                    replacement = $"0x{imm4 * 4:X}";
                }
                else if (MatchTag(format, ref index, "PCdisp8w>"))
                {
                    replacement = $"0x{unchecked(pc + 4 + (imm8 << 1)):X}[PC]";
                }
                else if (MatchTag(format, ref index, "PCdisp8d>"))
                {
                    replacement = $"0x{unchecked((pc & 0xFFFFFFFC) + 4 + (imm8 << 2)):X}[PC]";
                }
                else if (MatchTag(format, ref index, "disp8b>"))
                {
                    replacement = $"0x{imm8 * 1:X}";
                }
                else if (MatchTag(format, ref index, "disp8w>"))
                {
                    replacement = $"0x{imm8 * 2:X}";
                }
                else if (MatchTag(format, ref index, "disp8dw>"))
                {
                    replacement = $"0x{imm8 * 4:X}";
                }
                else if (MatchTag(format, ref index, "GBRdisp8b>"))
                {
                    replacement = $"0x{unchecked(imm8 * 1 + context.Gbr):X}";
                }
                else if (MatchTag(format, ref index, "GBRdisp8w>"))
                {
                    replacement = $"0x{unchecked(imm8 * 2 + context.Gbr):X}";
                }
                else if (MatchTag(format, ref index, "GBRdisp8dw>"))
                {
                    replacement = $"0x{unchecked(imm8 * 4 + context.Gbr):X}";
                }
                else if (MatchTag(format, ref index, "bdisp8>"))
                {
                    replacement = $"0x{unchecked((uint)((sbyte)opcode * 2) + 4 + pc):X}";
                }
                else if (MatchTag(format, ref index, "bdisp12>"))
                {
                    replacement = $"0x{unchecked((uint)((short)(imm12 << 4) >> 3) + pc + 4):X}";
                }
                else if (MatchTag(format, ref index, "imm8>"))
                {
                    replacement = $"0x{imm8:X}";
                }
                else if (MatchTag(format, ref index, "simm8>"))
                {
                    replacement = unchecked((sbyte)opcode).ToString(CultureInfo.InvariantCulture);
                }
                else if (MatchTag(format, ref index, "simm8hex>"))
                {
                    replacement = $"0x{unchecked((uint)(sbyte)opcode):X}";
                }
                else
                {
                    int tagIndex = index;
                    while (tagIndex < format.Length)
                    {
                        if (format[tagIndex] == '>')
                            break;
                        else
                            tagIndex++;
                    }

                    Debug.WriteLine($"Sh4Dissasm : Tag not known\"{format[index..]}\"");
                    replacement = $"<{format[index]}";
                    index++;
                }

                text.Append(replacement);
            }
            else
            {
                text.Append(format[index]);
                index++;
            }
        }

        return text.Append(" ; ").Append(registers).ToString();
    }
}

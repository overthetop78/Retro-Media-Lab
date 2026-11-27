using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

using static Sh4CanonicalParameterType;
using static Sh4ShilOperation;

internal enum Sh4ShilOperation
{
    shop_mov32,
    shop_mov64,
    shop_jdyn,
    shop_jcond,
    shop_ifb,
    shop_readm,
    shop_writem,
    shop_sync_sr,
    shop_sync_fpscr,
    shop_and,
    shop_or,
    shop_xor,
    shop_not,
    shop_add,
    shop_sub,
    shop_neg,
    shop_shl,
    shop_shr,
    shop_sar,
    shop_adc,
    shop_sbc,
    shop_negc,
    shop_ror,
    shop_rocl,
    shop_rocr,
    shop_swaplb,
    shop_shld,
    shop_shad,
    shop_ext_s8,
    shop_ext_s16,
    shop_mul_u16,
    shop_mul_s16,
    shop_mul_i32,
    shop_mul_u64,
    shop_mul_s64,
    shop_div32u,
    shop_div32s,
    shop_div32p2,
    shop_div1,
    shop_debug_3,
    shop_debug_1,
    shop_cvt_f2i_t,
    shop_cvt_i2f_n,
    shop_cvt_i2f_z,
    shop_pref,
    shop_test,
    shop_seteq,
    shop_setge,
    shop_setgt,
    shop_setae,
    shop_setab,
    shop_setpeq,
    shop_fadd,
    shop_fsub,
    shop_fmul,
    shop_fdiv,
    shop_fabs,
    shop_fneg,
    shop_fsqrt,
    shop_fipr,
    shop_ftrv,
    shop_fmac,
    shop_fsrra,
    shop_fsca,
    shop_fseteq,
    shop_fsetgt,
    shop_frswap,
    shop_xtrct,
    shop_illegal,
    shop_max
}

internal static unsafe class Sh4ShilCanonical
{
    private static readonly string[] Names =
    [
        "mov32",
        "mov64",
        "jdyn",
        "jcond",
        "ifb",
        "readm",
        "writem",
        "sync_sr",
        "sync_fpscr",
        "and",
        "or",
        "xor",
        "not",
        "add",
        "sub",
        "neg",
        "shl",
        "shr",
        "sar",
        "adc",
        "sbc",
        "negc",
        "ror",
        "rocl",
        "rocr",
        "swaplb",
        "shld",
        "shad",
        "ext_s8",
        "ext_s16",
        "mul_u16",
        "mul_s16",
        "mul_i32",
        "mul_u64",
        "mul_s64",
        "div32u",
        "div32s",
        "div32p2",
        "div1",
        "debug_3",
        "debug_1",
        "cvt_f2i_t",
        "cvt_i2f_n",
        "cvt_i2f_z",
        "pref",
        "test",
        "seteq",
        "setge",
        "setgt",
        "setae",
        "setab",
        "setpeq",
        "fadd",
        "fsub",
        "fmul",
        "fdiv",
        "fabs",
        "fneg",
        "fsqrt",
        "fipr",
        "ftrv",
        "fmac",
        "fsrra",
        "fsca",
        "fseteq",
        "fsetgt",
        "frswap",
        "xtrct",
        "illegal",
    ];

    internal static string Name(Sh4ShilOperation operation) => Names[(int)operation];

    private static readonly Sh4ShilCompiler[] Compilers =
    [
        Compile_mov32,
        Compile_mov64,
        Compile_jdyn,
        Compile_jcond,
        Compile_ifb,
        Compile_readm,
        Compile_writem,
        Compile_sync_sr,
        Compile_sync_fpscr,
        Compile_and,
        Compile_or,
        Compile_xor,
        Compile_not,
        Compile_add,
        Compile_sub,
        Compile_neg,
        Compile_shl,
        Compile_shr,
        Compile_sar,
        Compile_adc,
        Compile_sbc,
        Compile_negc,
        Compile_ror,
        Compile_rocl,
        Compile_rocr,
        Compile_swaplb,
        Compile_shld,
        Compile_shad,
        Compile_ext_s8,
        Compile_ext_s16,
        Compile_mul_u16,
        Compile_mul_s16,
        Compile_mul_i32,
        Compile_mul_u64,
        Compile_mul_s64,
        Compile_div32u,
        Compile_div32s,
        Compile_div32p2,
        Compile_div1,
        Compile_debug_3,
        Compile_debug_1,
        Compile_cvt_f2i_t,
        Compile_cvt_i2f_n,
        Compile_cvt_i2f_z,
        Compile_pref,
        Compile_test,
        Compile_seteq,
        Compile_setge,
        Compile_setgt,
        Compile_setae,
        Compile_setab,
        Compile_setpeq,
        Compile_fadd,
        Compile_fsub,
        Compile_fmul,
        Compile_fdiv,
        Compile_fabs,
        Compile_fneg,
        Compile_fsqrt,
        Compile_fipr,
        Compile_ftrv,
        Compile_fmac,
        Compile_fsrra,
        Compile_fsca,
        Compile_fseteq,
        Compile_fsetgt,
        Compile_frswap,
        Compile_xtrct,
        Compile_illegal,
    ];

    internal static void Compile(Sh4NativeBackend backend, Sh4ShilOpcode opcode) =>
        Compilers[(int)opcode.Operation](backend, opcode);

    private static void Compile_mov32(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        Sh4HostRuntime.Die("This opcode requires native dynarec implementation");
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_mov64(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        Sh4HostRuntime.Die("This opcode requires native dynarec implementation");
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_jdyn(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        Sh4HostRuntime.Die("This opcode requires native dynarec implementation");
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_jcond(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        Sh4HostRuntime.Die("This opcode requires native dynarec implementation");
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_ifb(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        Sh4HostRuntime.Die("This opcode requires native dynarec implementation");
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_readm(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        Sh4HostRuntime.Die("This opcode requires native dynarec implementation");
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_writem(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        Sh4HostRuntime.Die("This opcode requires native dynarec implementation");
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_sync_sr(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<void>)&Execute_sync_sr);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_sync_fpscr(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, default, ContextPointer);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<nint, void>)&Execute_sync_fpscr);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_and(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, uint>)&Execute_and);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_or(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, uint>)&Execute_or);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_xor(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, uint>)&Execute_xor);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_not(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint>)&Execute_not);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_add(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, uint>)&Execute_add);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_sub(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, uint>)&Execute_sub);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_neg(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint>)&Execute_neg);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_shl(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, uint>)&Execute_shl);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_shr(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, uint>)&Execute_shr);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_sar(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, uint>)&Execute_sar);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_adc(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source3, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, uint, ulong>)&Execute_adc);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt64LowReturn);
        backend.CanonicalParameter(opcode, opcode.Destination2, UInt64HighReturn);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_sbc(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source3, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, uint, ulong>)&Execute_sbc);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt64LowReturn);
        backend.CanonicalParameter(opcode, opcode.Destination2, UInt64HighReturn);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_negc(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, ulong>)&Execute_negc);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt64LowReturn);
        backend.CanonicalParameter(opcode, opcode.Destination2, UInt64HighReturn);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_ror(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, uint>)&Execute_ror);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_rocl(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, ulong>)&Execute_rocl);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt64LowReturn);
        backend.CanonicalParameter(opcode, opcode.Destination2, UInt64HighReturn);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_rocr(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, ulong>)&Execute_rocr);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt64LowReturn);
        backend.CanonicalParameter(opcode, opcode.Destination2, UInt64HighReturn);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_swaplb(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint>)&Execute_swaplb);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_shld(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, uint>)&Execute_shld);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_shad(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<int, uint, uint>)&Execute_shad);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_ext_s8(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint>)&Execute_ext_s8);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_ext_s16(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint>)&Execute_ext_s16);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_mul_u16(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, uint>)&Execute_mul_u16);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_mul_s16(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, uint>)&Execute_mul_s16);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_mul_i32(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, uint>)&Execute_mul_i32);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_mul_u64(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, ulong>)&Execute_mul_u64);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt64LowReturn);
        backend.CanonicalParameter(opcode, opcode.Destination2, UInt64HighReturn);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_mul_s64(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, ulong>)&Execute_mul_s64);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt64LowReturn);
        backend.CanonicalParameter(opcode, opcode.Destination2, UInt64HighReturn);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_div32u(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source3, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, uint, ulong>)&Execute_div32u);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt64LowReturn);
        backend.CanonicalParameter(opcode, opcode.Destination2, UInt64HighReturn);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_div32s(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source3, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, int, int, ulong>)&Execute_div32s);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt64LowReturn);
        backend.CanonicalParameter(opcode, opcode.Destination2, UInt64HighReturn);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_div32p2(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source3, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<int, int, uint, uint>)&Execute_div32p2);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_div1(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, default, ContextPointer);
        backend.CanonicalParameter(opcode, opcode.Source3, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, int, uint, nint, ulong>)&Execute_div1);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt64LowReturn);
        backend.CanonicalParameter(opcode, opcode.Destination2, UInt64HighReturn);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_debug_3(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source3, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, uint, void>)&Execute_debug_3);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_debug_1(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, void>)&Execute_debug_1);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_cvt_f2i_t(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source1, Float32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<float, uint>)&Execute_cvt_f2i_t);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_cvt_i2f_n(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, float>)&Execute_cvt_i2f_n);
        backend.CanonicalParameter(opcode, opcode.Destination, Float32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_cvt_i2f_z(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, float>)&Execute_cvt_i2f_z);
        backend.CanonicalParameter(opcode, opcode.Destination, Float32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_pref(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, default, ContextPointer);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, nint, void>)&Execute_pref);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_test(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, uint>)&Execute_test);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_seteq(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, uint>)&Execute_seteq);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_setge(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, uint>)&Execute_setge);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_setgt(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, uint>)&Execute_setgt);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_setae(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, uint>)&Execute_setae);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_setab(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, uint>)&Execute_setab);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_setpeq(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, uint>)&Execute_setpeq);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_fadd(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, Float32);
        backend.CanonicalParameter(opcode, opcode.Source1, Float32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<float, float, float>)&Execute_fadd);
        backend.CanonicalParameter(opcode, opcode.Destination, Float32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_fsub(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, Float32);
        backend.CanonicalParameter(opcode, opcode.Source1, Float32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<float, float, float>)&Execute_fsub);
        backend.CanonicalParameter(opcode, opcode.Destination, Float32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_fmul(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, Float32);
        backend.CanonicalParameter(opcode, opcode.Source1, Float32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<float, float, float>)&Execute_fmul);
        backend.CanonicalParameter(opcode, opcode.Destination, Float32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_fdiv(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, Float32);
        backend.CanonicalParameter(opcode, opcode.Source1, Float32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<float, float, float>)&Execute_fdiv);
        backend.CanonicalParameter(opcode, opcode.Destination, Float32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_fabs(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source1, Float32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<float, float>)&Execute_fabs);
        backend.CanonicalParameter(opcode, opcode.Destination, Float32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_fneg(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source1, Float32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<float, float>)&Execute_fneg);
        backend.CanonicalParameter(opcode, opcode.Destination, Float32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_fsqrt(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source1, Float32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<float, float>)&Execute_fsqrt);
        backend.CanonicalParameter(opcode, opcode.Destination, Float32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_fipr(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, RegisterPointer);
        backend.CanonicalParameter(opcode, opcode.Source1, RegisterPointer);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<float*, float*, float>)&Execute_fipr);
        backend.CanonicalParameter(opcode, opcode.Destination, Float32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_ftrv(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, RegisterPointer);
        backend.CanonicalParameter(opcode, opcode.Source1, RegisterPointer);
        backend.CanonicalParameter(opcode, opcode.Destination, RegisterPointer);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<float*, float*, float*, void>)&Execute_ftrv);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_fmac(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source3, Float32);
        backend.CanonicalParameter(opcode, opcode.Source2, Float32);
        backend.CanonicalParameter(opcode, opcode.Source1, Float32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<float, float, float, float>)&Execute_fmac);
        backend.CanonicalParameter(opcode, opcode.Destination, Float32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_fsrra(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source1, Float32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<float, float>)&Execute_fsrra);
        backend.CanonicalParameter(opcode, opcode.Destination, Float32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_fsca(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalParameter(opcode, opcode.Destination, RegisterPointer);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<float*, uint, void>)&Execute_fsca);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_fseteq(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, Float32);
        backend.CanonicalParameter(opcode, opcode.Source1, Float32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<float, float, uint>)&Execute_fseteq);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_fsetgt(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, Float32);
        backend.CanonicalParameter(opcode, opcode.Source1, Float32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<float, float, uint>)&Execute_fsetgt);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_frswap(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, RegisterPointer);
        backend.CanonicalParameter(opcode, opcode.Source1, RegisterPointer);
        backend.CanonicalParameter(opcode, opcode.Destination, RegisterPointer);
        backend.CanonicalParameter(opcode, opcode.Destination2, RegisterPointer);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<ulong*, ulong*, ulong*, ulong*, void>)&Execute_frswap);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_xtrct(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, uint>)&Execute_xtrct);
        backend.CanonicalParameter(opcode, opcode.Destination, UInt32Return);
        backend.CanonicalFinish(opcode);
    }

    private static void Compile_illegal(Sh4NativeBackend backend, Sh4ShilOpcode opcode)
    {
        backend.CanonicalStart(opcode);
        backend.CanonicalParameter(opcode, opcode.Source2, UInt32);
        backend.CanonicalParameter(opcode, opcode.Source1, UInt32);
        backend.CanonicalCall(opcode, (nuint)(delegate* unmanaged<uint, uint, void>)&Execute_illegal);
        backend.CanonicalFinish(opcode);
    }

    private static ulong Pack(uint low, uint high) => low | ((ulong)high << 32);

    private static float FixNaN(float value) => Sh4FloatingPoint.FixNaN(value);

    private static float InnerProduct(float* left, float* right, int stride)
    {
        double product = (double)left[0] * right[0]
                       + (double)left[1] * right[stride]
                       + (double)left[2] * right[stride * 2]
                       + (double)left[3] * right[stride * 3];
        return FixNaN((float)product);
    }

    internal static void FscaNative(float* destination, uint angle)
    {
        float radians = (angle & 0xFFFF) / (65536.0f / 2) * 3.14159265f;
        destination[0] = MathF.Sin(radians);
        destination[1] = MathF.Cos(radians);
    }

    [UnmanagedCallersOnly]
    private static void Execute_sync_sr()
    {
        Sh4Context.Active.UpdateStatusRegister();
    }

    [UnmanagedCallersOnly]
    private static void Execute_sync_fpscr(nint context)
    {
        Sh4Context.FromNative(context).UpdateFpscr();
    }

    [UnmanagedCallersOnly]
    private static uint Execute_and(uint left, uint right) => Evaluate_and(left, right);

    internal static uint Evaluate_and(uint left, uint right)
    {
        return left & right;
    }

    [UnmanagedCallersOnly]
    private static uint Execute_or(uint left, uint right) => Evaluate_or(left, right);

    internal static uint Evaluate_or(uint left, uint right)
    {
        return left | right;
    }

    [UnmanagedCallersOnly]
    private static uint Execute_xor(uint left, uint right) => Evaluate_xor(left, right);

    internal static uint Evaluate_xor(uint left, uint right)
    {
        return left ^ right;
    }

    [UnmanagedCallersOnly]
    private static uint Execute_not(uint value) => Evaluate_not(value);

    internal static uint Evaluate_not(uint value)
    {
        return ~value;
    }

    [UnmanagedCallersOnly]
    private static uint Execute_add(uint left, uint right) => Evaluate_add(left, right);

    internal static uint Evaluate_add(uint left, uint right)
    {
        return unchecked(left + right);
    }

    [UnmanagedCallersOnly]
    private static uint Execute_sub(uint left, uint right) => Evaluate_sub(left, right);

    internal static uint Evaluate_sub(uint left, uint right)
    {
        return unchecked(left - right);
    }

    [UnmanagedCallersOnly]
    private static uint Execute_neg(uint value) => Evaluate_neg(value);

    internal static uint Evaluate_neg(uint value)
    {
        return unchecked(0u - value);
    }

    [UnmanagedCallersOnly]
    private static uint Execute_shl(uint value, uint shift) => Evaluate_shl(value, shift);

    internal static uint Evaluate_shl(uint value, uint shift)
    {
        return value << (int)shift;
    }

    [UnmanagedCallersOnly]
    private static uint Execute_shr(uint value, uint shift) => Evaluate_shr(value, shift);

    internal static uint Evaluate_shr(uint value, uint shift)
    {
        return value >> (int)shift;
    }

    [UnmanagedCallersOnly]
    private static uint Execute_sar(uint value, uint shift) => Evaluate_sar(value, shift);

    internal static uint Evaluate_sar(uint value, uint shift)
    {
        return unchecked((uint)((int)value >> (int)shift));
    }

    [UnmanagedCallersOnly]
    private static ulong Execute_adc(uint left, uint right, uint carry) => Evaluate_adc(left, right, carry);

    internal static ulong Evaluate_adc(uint left, uint right, uint carry)
    {
        return (ulong)left + right + carry;
    }

    [UnmanagedCallersOnly]
    private static ulong Execute_sbc(uint left, uint right, uint carry) => Evaluate_sbc(left, right, carry);

    internal static ulong Evaluate_sbc(uint left, uint right, uint carry)
    {
        ulong result = unchecked((ulong)left - right - carry);
        return Pack(unchecked((uint)result), (uint)(result >> 32) & 1);
    }

    [UnmanagedCallersOnly]
    private static ulong Execute_negc(uint value, uint carry) => Evaluate_negc(value, carry);

    internal static ulong Evaluate_negc(uint value, uint carry)
    {
        ulong result = unchecked(0UL - value - carry);
        return Pack(unchecked((uint)result), (uint)(result >> 32) & 1);
    }

    [UnmanagedCallersOnly]
    private static uint Execute_ror(uint value, uint amount) => Evaluate_ror(value, amount);

    internal static uint Evaluate_ror(uint value, uint amount)
    {
        return (value >> (int)amount) | (value << unchecked((int)(32 - amount)));
    }

    [UnmanagedCallersOnly]
    private static ulong Execute_rocl(uint value, uint carry) => Evaluate_rocl(value, carry);

    internal static ulong Evaluate_rocl(uint value, uint carry)
    {
        return Pack((value << 1) | carry, value >> 31);
    }

    [UnmanagedCallersOnly]
    private static ulong Execute_rocr(uint value, uint carry) => Evaluate_rocr(value, carry);

    internal static ulong Evaluate_rocr(uint value, uint carry)
    {
        return Pack((value >> 1) | (carry << 31), value & 1);
    }

    [UnmanagedCallersOnly]
    private static uint Execute_swaplb(uint value) => Evaluate_swaplb(value);

    internal static uint Evaluate_swaplb(uint value)
    {
        return (value & 0xFFFF0000) | ((value & 0xFF) << 8) | ((value >> 8) & 0xFF);
    }

    [UnmanagedCallersOnly]
    private static uint Execute_shld(uint value, uint amount) => Evaluate_shld(value, amount);

    internal static uint Evaluate_shld(uint value, uint amount)
    {
        if ((int)amount >= 0)
            return value << (int)(amount & 31);
        return (amount & 31) == 0 ? 0 : value >> (int)((~amount & 31) + 1);
    }

    [UnmanagedCallersOnly]
    private static uint Execute_shad(int value, uint amount) => Evaluate_shad(value, amount);

    internal static uint Evaluate_shad(int value, uint amount)
    {
        if ((int)amount >= 0)
            return unchecked((uint)(value << (int)(amount & 31)));
        int shift = (amount & 31) == 0 ? 31 : (int)((~amount & 31) + 1);
        return unchecked((uint)(value >> shift));
    }

    [UnmanagedCallersOnly]
    private static uint Execute_ext_s8(uint value) => Evaluate_ext_s8(value);

    internal static uint Evaluate_ext_s8(uint value)
    {
        return unchecked((uint)(sbyte)value);
    }

    [UnmanagedCallersOnly]
    private static uint Execute_ext_s16(uint value) => Evaluate_ext_s16(value);

    internal static uint Evaluate_ext_s16(uint value)
    {
        return unchecked((uint)(short)value);
    }

    [UnmanagedCallersOnly]
    private static uint Execute_mul_u16(uint left, uint right) => Evaluate_mul_u16(left, right);

    internal static uint Evaluate_mul_u16(uint left, uint right)
    {
        return unchecked((uint)((ushort)left * (ushort)right));
    }

    [UnmanagedCallersOnly]
    private static uint Execute_mul_s16(uint left, uint right) => Evaluate_mul_s16(left, right);

    internal static uint Evaluate_mul_s16(uint left, uint right)
    {
        return unchecked((uint)((short)left * (short)right));
    }

    [UnmanagedCallersOnly]
    private static uint Execute_mul_i32(uint left, uint right) => Evaluate_mul_i32(left, right);

    internal static uint Evaluate_mul_i32(uint left, uint right)
    {
        return unchecked(left * right);
    }

    [UnmanagedCallersOnly]
    private static ulong Execute_mul_u64(uint left, uint right) => Evaluate_mul_u64(left, right);

    internal static ulong Evaluate_mul_u64(uint left, uint right)
    {
        return (ulong)left * right;
    }

    [UnmanagedCallersOnly]
    private static ulong Execute_mul_s64(uint left, uint right) => Evaluate_mul_s64(left, right);

    internal static ulong Evaluate_mul_s64(uint left, uint right)
    {
        return unchecked((ulong)((long)(int)left * (int)right));
    }

    [UnmanagedCallersOnly]
    private static ulong Execute_div32u(uint low, uint divisor, uint high) => Evaluate_div32u(low, divisor, high);

    internal static ulong Evaluate_div32u(uint low, uint divisor, uint high)
    {
        ulong dividend = Pack(low, high);
        uint quotient = divisor == 0 ? 0 : (uint)(dividend / divisor);
        uint remainder = divisor == 0 ? low : (uint)(dividend % divisor);
        return Pack(quotient, remainder);
    }

    [UnmanagedCallersOnly]
    private static ulong Execute_div32s(uint low, int divisor, int high) => Evaluate_div32s(low, divisor, high);

    internal static ulong Evaluate_div32s(uint low, int divisor, int high)
    {
        long dividend = ((long)high << 32) | low;
        if (dividend < 0)
            dividend++;

        int quotient = divisor == 0 ? 0 : unchecked((int)(dividend / divisor));
        int remainder = unchecked((int)(dividend - quotient * divisor));
        if ((high ^ divisor) < 0)
            quotient--;
        else if (high < 0)
            remainder--;

        return Pack(unchecked((uint)quotient), unchecked((uint)remainder));
    }

    [UnmanagedCallersOnly]
    private static uint Execute_div32p2(int remainder, int divisor, uint t) => Evaluate_div32p2(remainder, divisor, t);

    internal static uint Evaluate_div32p2(int remainder, int divisor, uint t)
    {
        if ((t & 0x80000000) == 0)
        {
            if ((t & 1) == 0)
                remainder -= divisor;
        }
        else
        {
            if (divisor > 0)
                remainder--;
            if ((t & 1) != 0)
                remainder += divisor;
        }
        return unchecked((uint)remainder);
    }

    [UnmanagedCallersOnly]
    private static ulong Execute_div1(uint value, int divisor, uint t, nint context)
    {
        ref var status = ref Sh4Context.FromNative(context).StatusRegister;
        bool qXorM = status.Q ^ status.M;
        status.Q = (int)value < 0;
        value = (value << 1) | t;
        uint oldValue = value;
        value = unchecked(value + (uint)((qXorM ? 1 : -1) * divisor));
        status.Q ^= status.M ^ (qXorM ? value < oldValue : value > oldValue);
        return Pack(value, status.Q ^ status.M ? 0u : 1u);
    }

    [UnmanagedCallersOnly]
    private static void Execute_debug_3(uint first, uint second, uint third)
    {
        Trace.WriteLine($"debug_3: {first:X8}, {second:X8}, {third:X8}");
    }

    [UnmanagedCallersOnly]
    private static void Execute_debug_1(uint value)
    {
        Trace.WriteLine($"debug_1: {value:X8}");
    }

    [UnmanagedCallersOnly]
    private static uint Execute_cvt_f2i_t(float value) => Evaluate_cvt_f2i_t(value);

    internal static uint Evaluate_cvt_f2i_t(float value)
    {
        int result;
        if (value > 2147483520.0f)
        {
            result = int.MaxValue;
        }
        else if (RuntimeInformation.ProcessArchitecture is Architecture.X86 or Architecture.X64)
        {
            result = Sse.ConvertToInt32WithTruncation(Vector128.CreateScalar(value));
            if (result == int.MinValue && value > 0)
                result = int.MaxValue;
        }
        else
        {
            result = unchecked((int)value);
            if (float.IsNaN(value))
                result = int.MinValue;
        }
        return unchecked((uint)result);
    }

    [UnmanagedCallersOnly]
    private static float Execute_cvt_i2f_n(uint value) => Evaluate_cvt_i2f_n(value);

    internal static float Evaluate_cvt_i2f_n(uint value)
    {
        return unchecked((int)value);
    }

    [UnmanagedCallersOnly]
    private static float Execute_cvt_i2f_z(uint value) => Evaluate_cvt_i2f_z(value);

    internal static float Evaluate_cvt_i2f_z(uint value)
    {
        return unchecked((int)value);
    }

    [UnmanagedCallersOnly]
    private static void Execute_pref(uint value, nint context)
    {
        if (value >> 26 == 0x38)
            Sh4Context.FromNative(context).WriteStoreQueue(value);
    }

    [UnmanagedCallersOnly]
    private static uint Execute_test(uint left, uint right) => Evaluate_test(left, right);

    internal static uint Evaluate_test(uint left, uint right)
    {
        return (left & right) == 0 ? 1u : 0u;
    }

    [UnmanagedCallersOnly]
    private static uint Execute_seteq(uint left, uint right) => Evaluate_seteq(left, right);

    internal static uint Evaluate_seteq(uint left, uint right)
    {
        return left == right ? 1u : 0u;
    }

    [UnmanagedCallersOnly]
    private static uint Execute_setge(uint left, uint right) => Evaluate_setge(left, right);

    internal static uint Evaluate_setge(uint left, uint right)
    {
        return (int)left >= (int)right ? 1u : 0u;
    }

    [UnmanagedCallersOnly]
    private static uint Execute_setgt(uint left, uint right) => Evaluate_setgt(left, right);

    internal static uint Evaluate_setgt(uint left, uint right)
    {
        return (int)left > (int)right ? 1u : 0u;
    }

    [UnmanagedCallersOnly]
    private static uint Execute_setae(uint left, uint right) => Evaluate_setae(left, right);

    internal static uint Evaluate_setae(uint left, uint right)
    {
        return left >= right ? 1u : 0u;
    }

    [UnmanagedCallersOnly]
    private static uint Execute_setab(uint left, uint right) => Evaluate_setab(left, right);

    internal static uint Evaluate_setab(uint left, uint right)
    {
        return left > right ? 1u : 0u;
    }

    [UnmanagedCallersOnly]
    private static uint Execute_setpeq(uint left, uint right) => Evaluate_setpeq(left, right);

    internal static uint Evaluate_setpeq(uint left, uint right)
    {
        uint difference = left ^ right;
        return (difference & 0xFF000000) != 0 && (difference & 0x00FF0000) != 0
            && (difference & 0x0000FF00) != 0 && (difference & 0x000000FF) != 0 ? 0u : 1u;
    }

    [UnmanagedCallersOnly]
    private static float Execute_fadd(float left, float right) => Evaluate_fadd(left, right);

    internal static float Evaluate_fadd(float left, float right)
    {
        return FixNaN(left + right);
    }

    [UnmanagedCallersOnly]
    private static float Execute_fsub(float left, float right) => Evaluate_fsub(left, right);

    internal static float Evaluate_fsub(float left, float right)
    {
        return FixNaN(left - right);
    }

    [UnmanagedCallersOnly]
    private static float Execute_fmul(float left, float right) => Evaluate_fmul(left, right);

    internal static float Evaluate_fmul(float left, float right)
    {
        return FixNaN(left * right);
    }

    [UnmanagedCallersOnly]
    private static float Execute_fdiv(float left, float right) => Evaluate_fdiv(left, right);

    internal static float Evaluate_fdiv(float left, float right)
    {
        return FixNaN(left / right);
    }

    [UnmanagedCallersOnly]
    private static float Execute_fabs(float value) => Evaluate_fabs(value);

    internal static float Evaluate_fabs(float value)
    {
        return MathF.Abs(value);
    }

    [UnmanagedCallersOnly]
    private static float Execute_fneg(float value) => Evaluate_fneg(value);

    internal static float Evaluate_fneg(float value)
    {
        return -value;
    }

    [UnmanagedCallersOnly]
    private static float Execute_fsqrt(float value) => Evaluate_fsqrt(value);

    internal static float Evaluate_fsqrt(float value)
    {
        return MathF.Sqrt(value);
    }

    [UnmanagedCallersOnly]
    private static float Execute_fipr(float* left, float* right)
    {
        return InnerProduct(left, right, 1);
    }

    [UnmanagedCallersOnly]
    private static void Execute_ftrv(float* destination, float* vector, float* matrix)
    {
        float first = InnerProduct(vector, matrix, 4);
        float second = InnerProduct(vector, matrix + 1, 4);
        float third = InnerProduct(vector, matrix + 2, 4);
        float fourth = InnerProduct(vector, matrix + 3, 4);
        destination[0] = first;
        destination[1] = second;
        destination[2] = third;
        destination[3] = fourth;
    }

    [UnmanagedCallersOnly]
    private static float Execute_fmac(float destination, float first, float second) => Evaluate_fmac(destination, first, second);

    internal static float Evaluate_fmac(float destination, float first, float second)
    {
        return FixNaN(MathF.FusedMultiplyAdd(first, second, destination));
    }

    [UnmanagedCallersOnly]
    private static float Execute_fsrra(float value) => Evaluate_fsrra(value);

    internal static float Evaluate_fsrra(float value)
    {
        return 1f / MathF.Sqrt(value);
    }

    [UnmanagedCallersOnly]
    private static void Execute_fsca(float* destination, uint angle) => Evaluate_fsca(destination, angle);

    internal static void Evaluate_fsca(float* destination, uint angle)
    {
        int index = (int)(angle & 0xFFFF);
        (destination[0], destination[1]) = Sh4FscaTable.GetPair(index);
    }

    [UnmanagedCallersOnly]
    private static uint Execute_fseteq(float left, float right) => Evaluate_fseteq(left, right);

    internal static uint Evaluate_fseteq(float left, float right)
    {
        return left == right ? 1u : 0u;
    }

    [UnmanagedCallersOnly]
    private static uint Execute_fsetgt(float left, float right) => Evaluate_fsetgt(left, right);

    internal static uint Evaluate_fsetgt(float left, float right)
    {
        return left > right ? 1u : 0u;
    }

    [UnmanagedCallersOnly]
    private static void Execute_frswap(ulong* destination1, ulong* destination2, ulong* source1, ulong* source2)
    {
        for (int i = 0; i < 8; i++)
        {
            ulong temporary = source1[i];
            destination1[i] = source2[i];
            destination2[i] = temporary;
        }
    }

    [UnmanagedCallersOnly]
    private static uint Execute_xtrct(uint left, uint right) => Evaluate_xtrct(left, right);

    internal static uint Evaluate_xtrct(uint left, uint right)
    {
        return (left >> 16) | (right << 16);
    }

    [UnmanagedCallersOnly]
    private static void Execute_illegal(uint pc, uint delaySlot)
    {
        if (delaySlot == 1)
            Sh4Context.Active.RaiseException(pc - 2, Sh4ExceptionEvent.SlotIllegalInstruction);
        else
            Sh4Context.Active.RaiseException(pc, Sh4ExceptionEvent.IllegalInstruction);
    }
}

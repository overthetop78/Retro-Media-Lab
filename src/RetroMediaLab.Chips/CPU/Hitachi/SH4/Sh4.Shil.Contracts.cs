using System.Runtime.CompilerServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

using static Sh4Register;
using static Sh4ShilParameterType;

#if !SH4_DYNAREC_NONE
internal delegate void Sh4ShilCompiler(Sh4NativeBackend backend, Sh4ShilOpcode opcode);
#endif

internal enum Sh4Register
{
    reg_r0,
    reg_r1,
    reg_r2,
    reg_r3,
    reg_r4,
    reg_r5,
    reg_r6,
    reg_r7,
    reg_r8,
    reg_r9,
    reg_r10,
    reg_r11,
    reg_r12,
    reg_r13,
    reg_r14,
    reg_r15,
    reg_fr_0,
    reg_fr_1,
    reg_fr_2,
    reg_fr_3,
    reg_fr_4,
    reg_fr_5,
    reg_fr_6,
    reg_fr_7,
    reg_fr_8,
    reg_fr_9,
    reg_fr_10,
    reg_fr_11,
    reg_fr_12,
    reg_fr_13,
    reg_fr_14,
    reg_fr_15,
    reg_xf_0,
    reg_xf_1,
    reg_xf_2,
    reg_xf_3,
    reg_xf_4,
    reg_xf_5,
    reg_xf_6,
    reg_xf_7,
    reg_xf_8,
    reg_xf_9,
    reg_xf_10,
    reg_xf_11,
    reg_xf_12,
    reg_xf_13,
    reg_xf_14,
    reg_xf_15,
    reg_r0_Bank,
    reg_r1_Bank,
    reg_r2_Bank,
    reg_r3_Bank,
    reg_r4_Bank,
    reg_r5_Bank,
    reg_r6_Bank,
    reg_r7_Bank,
    reg_gbr,
    reg_ssr,
    reg_spc,
    reg_sgr,
    reg_dbr,
    reg_vbr,
    reg_mach,
    reg_macl,
    reg_pr,
    reg_fpul,
    reg_nextpc,
    reg_sr_status,
    reg_sr_T,
    reg_old_fpscr,
    reg_fpscr,
    reg_pc_dyn,
    reg_temp,
    sh4_reg_count,
    regv_dr_0,
    regv_dr_2,
    regv_dr_4,
    regv_dr_6,
    regv_dr_8,
    regv_dr_10,
    regv_dr_12,
    regv_dr_14,
    regv_xd_0,
    regv_xd_2,
    regv_xd_4,
    regv_xd_6,
    regv_xd_8,
    regv_xd_10,
    regv_xd_12,
    regv_xd_14,
    regv_fv_0,
    regv_fv_4,
    regv_fv_8,
    regv_fv_12,
    regv_xmtrx,
    regv_fmtrx,
    reg_sq_buffer,
    NoReg = -1
}

internal enum Sh4ShilParameterType
{
    FMT_NULL,
    FMT_IMM,
    FMT_I32,
    FMT_F32,
    FMT_F64,
    FMT_V4,
    FMT_V16,
    FMT_REG_BASE = FMT_I32,
    FMT_VECTOR_BASE = FMT_V4,
    FMT_MASK = 0xFFFF
}

internal struct Sh4ShilParameter
{
    private uint _value;
    private RegisterVersions _version;

    public Sh4ShilParameter()
    {
        Type = FMT_NULL;
        _value = uint.MaxValue;
    }

    internal Sh4ShilParameter(uint immediate)
    {
        Type = FMT_IMM;
        _value = immediate;
    }

    internal Sh4ShilParameter(Sh4Register register)
    {
        (Type, register) = register switch
        {
            >= reg_fr_0 and <= reg_xf_15 => (FMT_F32, register),
            >= regv_dr_0 and <= regv_dr_14 => (FMT_F64, reg_fr_0 + (register - regv_dr_0) * 2),
            >= regv_xd_0 and <= regv_xd_14 => (FMT_F64, reg_xf_0 + (register - regv_xd_0) * 2),
            >= regv_fv_0 and <= regv_fv_12 => (FMT_V4, reg_fr_0 + (register - regv_fv_0) * 4),
            regv_xmtrx => (FMT_V16, reg_xf_0),
            regv_fmtrx => (FMT_V16, reg_fr_0),
            _ => (FMT_I32, register)
        };
        _value = unchecked((uint)register);
    }

    internal Sh4ShilParameterType Type { get; private set; }
    internal uint Immediate => IsNull ? uint.MaxValue : _value;
    internal uint ImmediateValue
    {
        get
        {
            Sh4HostRuntime.Verify(IsImmediate);
            return Immediate;
        }
    }

    internal Sh4Register Register => unchecked((Sh4Register)Immediate);
    internal bool IsNull => Type == FMT_NULL;
    internal bool IsImmediate => Type == FMT_IMM;
    internal bool IsRegister => Type >= FMT_REG_BASE;
    internal bool IsInteger32 => Type == FMT_I32;
    internal bool IsFloat32 => Type == FMT_F32;
    internal int FloatVectorCount => Type >= FMT_VECTOR_BASE ? Count : 0;
    internal bool IsFloat64 => Type == FMT_F64;
    internal bool Is32 => IsInteger32 || IsFloat32;
    internal bool IsSignedByteImmediate => IsImmediate && unchecked((sbyte)_value) == unchecked((int)_value);
    internal int Count => Type switch
    {
        FMT_F64 => 2,
        FMT_V4 => 4,
        FMT_V16 => 16,
        _ => 1
    };

    internal readonly ushort GetVersion(int index) => _version[index];

    internal void SetVersion(int index, ushort version) => _version[index] = version;

    internal void ReplaceValue(Sh4ShilParameterType type, uint value)
    {
        Type = type;
        _value = value;
    }

    internal unsafe uint* GetRegisterPointer(Sh4Context context)
    {
        Sh4HostRuntime.Verify(IsRegister);
        return Sh4Shil.GetRegisterPointer(context, Register);
    }

    internal uint GetRegisterOffset()
    {
        Sh4HostRuntime.Verify(IsRegister);
        return Sh4Shil.GetRegisterOffset(Register);
    }

    internal int GetRelativeRegisterOffset() => (int)GetRegisterOffset() - Sh4ContextLayout.Size;

    public static implicit operator Sh4ShilParameter(Sh4Register register) => new(register);

    [InlineArray(16)]
    private struct RegisterVersions
    {
        private ushort _element0;
    }
}

internal sealed partial class Sh4ShilOpcode
{
    private ushort _hostOffset;
    private ushort _guestOffset;

    internal Sh4ShilOperation Operation;
    internal uint Size;
    internal Sh4ShilParameter Destination = new();
    internal Sh4ShilParameter Destination2 = new();
    internal Sh4ShilParameter Source1 = new();
    internal Sh4ShilParameter Source2 = new();
    internal Sh4ShilParameter Source3 = new();
    internal int HostOffset
    {
        get => _hostOffset;
        set => _hostOffset = unchecked((ushort)value);
    }

    internal int GuestOffset
    {
        get => _guestOffset;
        set => _guestOffset = unchecked((ushort)value);
    }

    internal bool DelaySlot;
}

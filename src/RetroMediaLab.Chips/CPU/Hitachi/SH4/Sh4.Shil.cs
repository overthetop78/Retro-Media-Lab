using System.Diagnostics;
using System.Globalization;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

using static Sh4Register;

internal static class Sh4Shil
{
#if !SH4_DYNAREC_NONE
    internal static void AnalyseBlock(Sh4RuntimeBlockInfo block, Sh4RecompilerDriver driver) =>
        new Sh4SsaOptimizer(block, driver).Optimize();
#endif

    internal static uint GetRegisterOffset(Sh4Register register)
    {
        if (register is >= reg_r0 and <= reg_r15)
            return Sh4ContextLayout.GeneralRegisters + (uint)(register - reg_r0) * sizeof(uint);
        if (register is >= reg_r0_Bank and <= reg_r7_Bank)
            return Sh4ContextLayout.BankedRegisters + (uint)(register - reg_r0_Bank) * sizeof(uint);
        if (register is >= reg_fr_0 and <= reg_fr_15)
            return Sh4ContextLayout.FloatingRegisters + (uint)(register - reg_fr_0) * sizeof(float);
        if (register is >= reg_xf_0 and <= reg_xf_15)
            return Sh4ContextLayout.ExtendedFloatingRegisters + (uint)(register - reg_xf_0) * sizeof(float);

        switch (register)
        {
            case reg_gbr: return Sh4ContextLayout.Gbr;
            case reg_vbr: return Sh4ContextLayout.Vbr;
            case reg_ssr: return Sh4ContextLayout.Ssr;
            case reg_spc: return Sh4ContextLayout.Spc;
            case reg_sgr: return Sh4ContextLayout.Sgr;
            case reg_dbr: return Sh4ContextLayout.Dbr;
            case reg_mach: return Sh4ContextLayout.Mach;
            case reg_macl: return Sh4ContextLayout.Macl;
            case reg_pr: return Sh4ContextLayout.Pr;
            case reg_fpul: return Sh4ContextLayout.Fpul;
            case reg_nextpc: return Sh4ContextLayout.Pc;
            case reg_sr_status: return Sh4ContextLayout.SrStatus;
            case reg_sr_T: return Sh4ContextLayout.SrT;
            case reg_old_fpscr: return Sh4ContextLayout.OldFpscr;
            case reg_fpscr: return Sh4ContextLayout.Fpscr;
            case reg_pc_dyn: return Sh4ContextLayout.DynamicPc;
            case reg_temp: return Sh4ContextLayout.TemporaryRegister;
            case reg_sq_buffer: return Sh4ContextLayout.StoreQueueBuffer;
            default:
                Trace.TraceError($"Unknown register ID {(int)register}");
                Sh4HostRuntime.Die("Invalid reg");
                return 0;
        }
    }

    internal static unsafe uint* GetRegisterPointer(Sh4Context context, Sh4Register register) =>
        (uint*)(context.NativeAddress + GetRegisterOffset(register));

    internal static string RegisterName(Sh4Register register)
    {
        if (register is >= reg_fr_0 and <= reg_xf_15)
            return FormattableString.Invariant($"f{register - reg_fr_0}");
        if (register <= reg_r15)
            return FormattableString.Invariant($"r{(int)register}");
        if (register <= reg_r7_Bank)
            return FormattableString.Invariant($"r{register - reg_r0_Bank}b");

        return register switch
        {
            reg_sr_T => "sr.T",
            reg_fpscr => "fpscr",
            reg_sr_status => "sr",
            reg_pc_dyn => "pc_dyn",
            reg_macl => "macl",
            reg_mach => "mach",
            reg_pr => "pr",
            reg_gbr => "gbr",
            reg_nextpc => "pc",
            reg_fpul => "fpul",
            reg_old_fpscr => "old_fpscr",
            reg_ssr => "ssr",
            reg_temp => "temp",
            _ => FormattableString.Invariant($"s{(int)register}")
        };
    }

    internal static string DisassembleParameter(Sh4ShilParameter parameter, bool comma)
    {
        string prefix = !parameter.IsNull && comma ? ", " : "";
        if (parameter.IsImmediate)
        {
            return prefix + (parameter.IsSignedByteImmediate
                ? unchecked((int)parameter.Immediate).ToString(CultureInfo.InvariantCulture)
                : FormattableString.Invariant($"0x{parameter.Immediate:x}"));
        }
        if (!parameter.IsRegister)
            return prefix;

        string name = RegisterName(parameter.Register);
        if (parameter.Count > 1)
            name += FormattableString.Invariant($"v{parameter.Count}");
        return FormattableString.Invariant($"{prefix}{name}.{parameter.GetVersion(0)}");
    }

    internal static string OpcodeName(int operation) => Sh4ShilCanonical.Name((Sh4ShilOperation)operation);
}

internal sealed partial class Sh4ShilOpcode
{
    internal string Disassemble() =>
        $"{Sh4ShilCanonical.Name(Operation)} {Sh4Shil.DisassembleParameter(Destination, false)}"
        + $"{Sh4Shil.DisassembleParameter(Destination2, true)} <- {Sh4Shil.DisassembleParameter(Source1, false)}"
        + $"{Sh4Shil.DisassembleParameter(Source2, true)}{Sh4Shil.DisassembleParameter(Source3, true)}";
}

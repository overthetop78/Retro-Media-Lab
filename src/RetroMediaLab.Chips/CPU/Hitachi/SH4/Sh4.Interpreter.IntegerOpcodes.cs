using System.Diagnostics;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

using static Sh4DecoderConstants;

internal partial class Sh4Interpreter
{
    internal partial void Execute_i0000_nnnn_0001_0010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Registers[n] = context.Gbr;
    }

    internal partial void Execute_i0000_nnnn_0010_0010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Registers[n] = context.Vbr;
    }

    internal partial void Execute_i0000_nnnn_0011_0010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Registers[n] = context.Ssr;
    }

    internal partial void Execute_i0000_nnnn_0011_1010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Registers[n] = context.Sgr;
    }

    internal partial void Execute_i0000_nnnn_0100_0010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Registers[n] = context.Spc;
    }

    internal partial void Execute_i0000_nnnn_1mmm_0010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode) & 0x7;
        context.Registers[n] = context.BankedRegisters[m];
    }

    internal partial void Execute_i0000_nnnn_0101_1010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Registers[n] = context.Fpul;
    }

    internal partial void Execute_i0000_nnnn_1111_1010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Registers[n] = context.Dbr;
    }

    internal partial void Execute_i0000_nnnn_0000_1010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Registers[n] = context.Mach;
    }

    internal partial void Execute_i0000_nnnn_0001_1010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Registers[n] = context.Macl;
    }

    internal partial void Execute_i0000_nnnn_0010_1010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Registers[n] = context.ProcedureRegister;
    }

    internal partial void Execute_i0000_nnnn_mmmm_1100(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        context.Registers[n] = unchecked((uint)(sbyte)context.ReadMemory8(unchecked(context.Registers[0] + context.Registers[m])));
    }

    internal partial void Execute_i0000_nnnn_mmmm_1101(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        context.Registers[n] = unchecked((uint)(short)context.ReadMemory16(unchecked(context.Registers[0] + context.Registers[m])));
    }

    internal partial void Execute_i0000_nnnn_mmmm_1110(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);

        context.Registers[n] = context.ReadMemory32(unchecked(context.Registers[0] + context.Registers[m]));
    }

    internal partial void Execute_i0000_nnnn_mmmm_0100(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);

        context.WriteMemory8(unchecked(context.Registers[0] + context.Registers[n]), unchecked((byte)(context.Registers[m])));
    }

    internal partial void Execute_i0000_nnnn_mmmm_0101(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        context.WriteMemory16(unchecked(context.Registers[0] + context.Registers[n]), unchecked((ushort)(context.Registers[m])));
    }

    internal partial void Execute_i0000_nnnn_mmmm_0110(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        context.WriteMemory32(unchecked(context.Registers[0] + context.Registers[n]), context.Registers[m]);
    }

    internal partial void Execute_i0001_nnnn_mmmm_iiii(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        uint disp = GetImm4(opcode);
        context.WriteMemory32(unchecked(context.Registers[n] + (disp <<2)), context.Registers[m]);
    }

    internal partial void Execute_i0010_nnnn_mmmm_0000(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        context.WriteMemory8(context.Registers[n], unchecked((byte)(context.Registers[m])));
    }

    internal partial void Execute_i0010_nnnn_mmmm_0001(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        context.WriteMemory16(context.Registers[n], unchecked((ushort)(context.Registers[m])));
    }

    internal partial void Execute_i0010_nnnn_mmmm_0010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        context.WriteMemory32(context.Registers[n], context.Registers[m]);
    }

    internal partial void Execute_i0010_nnnn_mmmm_0100(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);

        uint addr = context.Registers[n] - 1;
        context.WriteMemory8(unchecked(context.Registers[n] + uint.MaxValue), unchecked((byte)(context.Registers[m])));
        context.Registers[n] = addr;
    }

    internal partial void Execute_i0010_nnnn_mmmm_0101(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);

        uint addr = context.Registers[n] - 2;
        context.WriteMemory16(addr, unchecked((ushort)(context.Registers[m])));
        context.Registers[n] = addr;
    }

    internal partial void Execute_i0010_nnnn_mmmm_0110(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);

        uint addr = context.Registers[n] - 4;
        context.WriteMemory32(addr, context.Registers[m]);
        context.Registers[n] = addr;
    }

    internal partial void Execute_i0100_nnnn_0101_0010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        uint addr = context.Registers[n] - 4;
        context.WriteMemory32(addr, context.Fpul);
        context.Registers[n] = addr;
    }

    internal partial void Execute_i0100_nnnn_0000_0010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        uint addr = context.Registers[n] - 4;
        context.WriteMemory32(addr, context.Mach);
        context.Registers[n] = addr;
    }

    internal partial void Execute_i0100_nnnn_0001_0010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        uint addr = context.Registers[n] - 4;
        context.WriteMemory32(addr, context.Macl);
        context.Registers[n] = addr;
    }

    internal partial void Execute_i0100_nnnn_0010_0010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        uint addr = context.Registers[n] - 4;
        context.WriteMemory32(addr, context.ProcedureRegister);
        context.Registers[n] = addr;
    }

    internal partial void Execute_i0100_nnnn_1111_0010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        uint addr = context.Registers[n] - 4;
        context.WriteMemory32(addr, context.Dbr);
        context.Registers[n] = addr;
    }

    internal partial void Execute_i0100_nnnn_0001_0011(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        uint addr = context.Registers[n] - 4;
        context.WriteMemory32(addr, context.Gbr);
        context.Registers[n] = addr;
    }

    internal partial void Execute_i0100_nnnn_0010_0011(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        uint addr = context.Registers[n] - 4;
        context.WriteMemory32(addr, context.Vbr);
        context.Registers[n] = addr;
    }

    internal partial void Execute_i0100_nnnn_0011_0011(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        uint addr = context.Registers[n] - 4;
        context.WriteMemory32(addr, context.Ssr);
        context.Registers[n] = addr;
    }

    internal partial void Execute_i0100_nnnn_0011_0010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        uint addr = context.Registers[n] - 4;
        context.WriteMemory32(addr, context.Sgr);
        context.Registers[n] = addr;
    }

    internal partial void Execute_i0100_nnnn_0100_0011(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        uint addr = context.Registers[n] - 4;
        context.WriteMemory32(addr, context.Spc);
        context.Registers[n] = addr;
    }

    internal partial void Execute_i0100_nnnn_1mmm_0011(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode) & 0x07;

        uint addr = context.Registers[n] - 4;
        context.WriteMemory32(addr, context.BankedRegisters[m]);
        context.Registers[n] = addr;
    }

    internal partial void Execute_i0100_nnnn_0000_0110(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Mach = context.ReadMemory32(context.Registers[n]);

        context.Registers[n] += 4;
    }

    internal partial void Execute_i0100_nnnn_0001_0110(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Macl = context.ReadMemory32(context.Registers[n]);

        context.Registers[n] += 4;
    }

    internal partial void Execute_i0100_nnnn_0010_0110(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.ProcedureRegister = context.ReadMemory32(context.Registers[n]);

        context.Registers[n] += 4;
    }

    internal partial void Execute_i0100_nnnn_0101_0110(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        context.Fpul = context.ReadMemory32(context.Registers[n]);
        context.Registers[n] += 4;
    }

    internal partial void Execute_i0100_nnnn_1111_0110(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        context.Dbr = context.ReadMemory32(context.Registers[n]);
        context.Registers[n] += 4;
    }

    internal partial void Execute_i0100_nnnn_0001_0111(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        context.Gbr = context.ReadMemory32(context.Registers[n]);
        context.Registers[n] += 4;
    }

    internal partial void Execute_i0100_nnnn_0010_0111(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        context.Vbr = context.ReadMemory32(context.Registers[n]);
        context.Registers[n] += 4;
    }

    internal partial void Execute_i0100_nnnn_0011_0111(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        context.Ssr = context.ReadMemory32(context.Registers[n]);
        context.Registers[n] += 4;
    }

    internal partial void Execute_i0100_nnnn_0011_0110(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        context.Sgr = context.ReadMemory32(context.Registers[n]);
        context.Registers[n] += 4;
    }

    internal partial void Execute_i0100_nnnn_0100_0111(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        context.Spc = context.ReadMemory32(context.Registers[n]);
        context.Registers[n] += 4;
    }

    internal partial void Execute_i0100_nnnn_1mmm_0111(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode) & 7;

        context.BankedRegisters[m] = context.ReadMemory32(context.Registers[n]);
        context.Registers[n] += 4;
    }

    internal partial void Execute_i0100_nnnn_0000_1010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Mach = context.Registers[n];
    }

    internal partial void Execute_i0100_nnnn_0001_1010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Macl = context.Registers[n];
    }

    internal partial void Execute_i0100_nnnn_0010_1010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.ProcedureRegister = context.Registers[n];
    }

    internal partial void Execute_i0100_nnnn_0101_1010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Fpul = context.Registers[n];
    }

    internal partial void Execute_i0100_nnnn_1111_1010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Dbr = context.Registers[n];
    }

    internal partial void Execute_i0100_nnnn_0001_1110(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Gbr = context.Registers[n];
    }

    internal partial void Execute_i0100_nnnn_0010_1110(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        context.Vbr = context.Registers[n];
    }

    internal partial void Execute_i0100_nnnn_0011_1110(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        context.Ssr = context.Registers[n];
    }

    internal partial void Execute_i0100_nnnn_0011_1010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        context.Sgr = context.Registers[n];
    }

    internal partial void Execute_i0100_nnnn_0100_1110(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        context.Spc = context.Registers[n];
    }

    internal partial void Execute_i0100_nnnn_1mmm_1110(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode) & 7;

        context.BankedRegisters[m] = context.Registers[n];
    }

    internal partial void Execute_i0101_nnnn_mmmm_iiii(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        uint disp = GetImm4(opcode) << 2;

        context.Registers[n] = context.ReadMemory32(unchecked(context.Registers[m] + disp));
    }

    internal partial void Execute_i0110_nnnn_mmmm_0000(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);

        context.Registers[n] = unchecked((uint)(sbyte)context.ReadMemory8(context.Registers[m]));
    }

    internal partial void Execute_i0110_nnnn_mmmm_0001(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        context.Registers[n] = unchecked((uint)(short)context.ReadMemory16(context.Registers[m]));
    }

    internal partial void Execute_i0110_nnnn_mmmm_0010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);

        context.Registers[n] = context.ReadMemory32(context.Registers[m]);
    }

    internal partial void Execute_i0110_nnnn_mmmm_0011(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        context.Registers[n] = context.Registers[m];
    }

    internal partial void Execute_i0110_nnnn_mmmm_0100(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        context.Registers[n] = unchecked((uint)(sbyte)context.ReadMemory8(context.Registers[m]));
        if (n != m)
            context.Registers[m] += 1;
    }

    internal partial void Execute_i0110_nnnn_mmmm_0101(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        context.Registers[n] = unchecked((uint)(short)context.ReadMemory16(context.Registers[m]));
        if (n != m)
            context.Registers[m] += 2;
    }

    internal partial void Execute_i0110_nnnn_mmmm_0110(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);

        context.Registers[n] = context.ReadMemory32(context.Registers[m]);
        if (n != m)
            context.Registers[m] += 4;
    }

    internal partial void Execute_i1000_0000_mmmm_iiii(Sh4Context context, uint opcode)
    {
        int n = GetM(opcode);
        uint disp = GetImm4(opcode);
        context.WriteMemory8(unchecked(context.Registers[n] + disp), unchecked((byte)(context.Registers[0])));
    }

    internal partial void Execute_i1000_0001_mmmm_iiii(Sh4Context context, uint opcode)
    {
        uint disp = GetImm4(opcode);
        int m = GetM(opcode);
        context.WriteMemory16(unchecked(context.Registers[m] + (disp << 1)), unchecked((ushort)(context.Registers[0])));
    }

    internal partial void Execute_i1000_0100_mmmm_iiii(Sh4Context context, uint opcode)
    {
        uint disp = GetImm4(opcode);
        int m = GetM(opcode);
        context.Registers[0] = unchecked((uint)(sbyte)context.ReadMemory8(unchecked(context.Registers[m] + disp)));
    }

    internal partial void Execute_i1000_0101_mmmm_iiii(Sh4Context context, uint opcode)
    {
        uint disp = GetImm4(opcode);
        int m = GetM(opcode);
        context.Registers[0] = unchecked((uint)(short)context.ReadMemory16(unchecked(context.Registers[m] + (disp << 1))));
    }

    internal partial void Execute_i1001_nnnn_iiii_iiii(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        uint disp = GetImm8(opcode);
        context.Registers[n] = unchecked((uint)(short)context.ReadMemory16((disp << 1) + context.ProgramCounter + 2));
    }

    internal partial void Execute_i1100_0000_iiii_iiii(Sh4Context context, uint opcode)
    {
        uint disp = GetImm8(opcode);
        context.WriteMemory8(unchecked(context.Gbr + disp), unchecked((byte)(context.Registers[0])));
    }

    internal partial void Execute_i1100_0001_iiii_iiii(Sh4Context context, uint opcode)
    {
        uint disp = GetImm8(opcode);
        context.WriteMemory16(unchecked(context.Gbr + (disp << 1)), unchecked((ushort)(context.Registers[0])));
    }

    internal partial void Execute_i1100_0010_iiii_iiii(Sh4Context context, uint opcode)
    {
        uint disp = GetImm8(opcode);
        context.WriteMemory32(unchecked(context.Gbr + (disp << 2)), context.Registers[0]);
    }

    internal partial void Execute_i1100_0100_iiii_iiii(Sh4Context context, uint opcode)
    {
        uint disp = GetImm8(opcode);
        context.Registers[0] = unchecked((uint)(sbyte)context.ReadMemory8(unchecked(context.Gbr + disp)));
    }

    internal partial void Execute_i1100_0101_iiii_iiii(Sh4Context context, uint opcode)
    {
        uint disp = GetImm8(opcode);
        context.Registers[0] = unchecked((uint)(short)context.ReadMemory16(unchecked(context.Gbr + (disp << 1))));
    }

    internal partial void Execute_i1100_0110_iiii_iiii(Sh4Context context, uint opcode)
    {
        uint disp = GetImm8(opcode);

        context.Registers[0] = context.ReadMemory32(unchecked(context.Gbr + (disp << 2)));
    }

    internal partial void Execute_i1100_0111_iiii_iiii(Sh4Context context, uint opcode)
    {
        context.Registers[0] = ((context.ProgramCounter + 2) & 0xFFFFFFFC) + (GetImm8(opcode) << 2);
    }

    internal partial void Execute_i1101_nnnn_iiii_iiii(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        uint disp = GetImm8(opcode);

        context.Registers[n] = context.ReadMemory32((disp << 2) + ((context.ProgramCounter + 2) & 0xFFFFFFFC));
    }

    internal partial void Execute_i1110_nnnn_iiii_iiii(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Registers[n] = (uint)(int)(sbyte)GetSImm8(opcode);
    }

    internal partial void Execute_i0000_nnnn_1100_0011(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.WriteMemory32(context.Registers[n], context.Registers[0]);

    }

    internal partial void Execute_i0000_0000_0010_1000(Sh4Context context, uint opcode)
    {
        context.Mac = 0;
    }

    private static void ExecuteBranchDelaySlot() => Instance!.ExecuteDelaySlot();

    internal partial void Execute_i0000_nnnn_0010_0011(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        uint target = context.Registers[n] + context.ProgramCounter + 2;
        ExecuteBranchDelaySlot();
        context.ProgramCounter = target;
    }

    internal partial void Execute_i0000_nnnn_0000_0011(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        uint target = context.Registers[n] + context.ProgramCounter +2;
        uint returnAddress = context.ProgramCounter + 2;

        ExecuteBranchDelaySlot();

        context.ProcedureRegister = returnAddress;
        context.ProgramCounter = target;
        _debugger.SubroutineCall();
    }

    internal partial void Execute_i0000_0000_0010_1011(Sh4Context context, uint opcode)
    {
        uint target = context.Spc;
        Instance!.ExecuteReturnFromExceptionDelaySlot();
        context.ProgramCounter = target;
        if (context.UpdateStatusRegister())
            _interrupts.Update();
        _debugger.SubroutineReturn();
    }

    internal partial void Execute_i0000_0000_0000_1011(Sh4Context context, uint opcode)
    {
        uint target = context.ProcedureRegister;
        ExecuteBranchDelaySlot();
        context.ProgramCounter = target;
        _debugger.SubroutineReturn();
    }

    internal static uint BranchTargetSigned8(Sh4Context context, uint opcode)
    {
        return unchecked((uint)(GetSImm8(opcode) * 2 + 2) + context.ProgramCounter);
    }

    internal partial void Execute_i1000_1011_iiii_iiii(Sh4Context context, uint opcode)
    {
        if (!context.StatusRegister.T)
        {

            context.ProgramCounter = BranchTargetSigned8(context, opcode);
        }
    }

    internal partial void Execute_i1000_1111_iiii_iiii(Sh4Context context, uint opcode)
    {
        if (!context.StatusRegister.T)
        {

            uint target = BranchTargetSigned8(context, opcode);
            ExecuteBranchDelaySlot();
            context.ProgramCounter = target;
        }
    }

    internal partial void Execute_i1000_1001_iiii_iiii(Sh4Context context, uint opcode)
    {
        if (context.StatusRegister.T)
        {

            context.ProgramCounter = BranchTargetSigned8(context, opcode);
        }
    }

    internal partial void Execute_i1000_1101_iiii_iiii(Sh4Context context, uint opcode)
    {
        if (context.StatusRegister.T)
        {

            uint target = BranchTargetSigned8(context, opcode);
            ExecuteBranchDelaySlot();
            context.ProgramCounter = target;
        }
    }

    private static uint BranchTargetSigned12(Sh4Context context, uint opcode)
    {
        return unchecked((uint)(GetSImm12(opcode) * 2 + 2) + context.ProgramCounter);
    }

    internal partial void Execute_i1010_iiii_iiii_iiii(Sh4Context context, uint opcode)
    {
        uint target = BranchTargetSigned12(context, opcode);
        ExecuteBranchDelaySlot();
        context.ProgramCounter = target;
    }

    internal partial void Execute_i1011_iiii_iiii_iiii(Sh4Context context, uint opcode)
    {
        uint returnAddress = context.ProgramCounter + 2;
        uint target = BranchTargetSigned12(context, opcode);
        ExecuteBranchDelaySlot();

        context.ProcedureRegister = returnAddress;
        context.ProgramCounter = target;
        _debugger.SubroutineCall();
    }

    internal partial void Execute_i1100_0011_iiii_iiii(Sh4Context context, uint opcode)
    {
        Trace.TraceWarning($"TRAP #{GetImm8(opcode):X}");
        _debugger.Trap(Sh4ExceptionEvent.Trap);
        Mmu.TrapRegister = GetImm8(opcode) << 2;
        RaiseException(context.ProgramCounter, Sh4ExceptionEvent.Trap);
    }

    internal partial void Execute_i0100_nnnn_0010_1011(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        uint target = context.Registers[n];
        ExecuteBranchDelaySlot();
        context.ProgramCounter = target;
    }

    internal partial void Execute_i0100_nnnn_0000_1011(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        uint returnAddress = context.ProgramCounter + 2;
        uint target = context.Registers[n];
        ExecuteBranchDelaySlot();

        context.ProcedureRegister = returnAddress;
        context.ProgramCounter = target;
        _debugger.SubroutineCall();
    }

    internal partial void Execute_i0000_0000_0001_1011(Sh4Context context, uint opcode)
    {

        int i = 0, s = 1;

        while (UpdateSystemInterrupts() == 0)
        {
            if (i++>1000)
            {
                s = 0;
                break;
            }
        }

        if (s == 0)
            context.ProgramCounter -= 2;
    }

    internal partial void Execute_i0011_nnnn_mmmm_1000(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        context.Registers[n] -= context.Registers[m];
    }

    internal partial void Execute_i0011_nnnn_mmmm_1100(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        context.Registers[n] += context.Registers[m];
    }

    internal partial void Execute_i0111_nnnn_iiii_iiii(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int stmp1 = GetSImm8(opcode);
        context.Registers[n] = unchecked(context.Registers[n] + (uint)stmp1);
    }

    internal partial void Execute_i0010_nnnn_mmmm_1001(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        context.Registers[n] &= context.Registers[m];
    }

    internal partial void Execute_i0010_nnnn_mmmm_1010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        context.Registers[n] ^= context.Registers[m];
    }

    internal partial void Execute_i0010_nnnn_mmmm_1011(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        context.Registers[n] |= context.Registers[m];
    }

    internal partial void Execute_i0100_nnnn_0000_1000(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Registers[n] <<= 2;
    }

    internal partial void Execute_i0100_nnnn_0001_1000(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Registers[n] <<= 8;
    }

    internal partial void Execute_i0100_nnnn_0010_1000(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Registers[n] <<= 16;
    }

    internal partial void Execute_i0100_nnnn_0000_1001(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Registers[n] >>= 2;
    }

    internal partial void Execute_i0100_nnnn_0001_1001(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Registers[n] >>= 8;
    }

    internal partial void Execute_i0100_nnnn_0010_1001(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Registers[n] >>= 16;
    }

    internal partial void Execute_i1100_1001_iiii_iiii(Sh4Context context, uint opcode)
    {
        uint imm = GetImm8(opcode);
        context.Registers[0] &= imm;
    }

    internal partial void Execute_i1100_1010_iiii_iiii(Sh4Context context, uint opcode)
    {
        uint imm = GetImm8(opcode);
        context.Registers[0] ^= imm;
    }

    internal partial void Execute_i1100_1011_iiii_iiii(Sh4Context context, uint opcode)
    {
        uint imm = GetImm8(opcode);
        context.Registers[0] |= imm;
    }

    internal partial void Execute_i0000_0000_0000_1001(Sh4Context context, uint opcode)
    {
    }

    internal partial void Execute_i0000_0000_0011_1000(Sh4Context context, uint opcode)
    {
        Mmu.UnifiedTlb[Mmu.Control.Urc].Data = Mmu.PageTableEntryLow;
        Mmu.UnifiedTlb[Mmu.Control.Urc].Address = Mmu.PageTableEntryHigh;
        Mmu.UnifiedTlb[Mmu.Control.Urc].Assistance = Mmu.PageTableEntryAssistance;

        Mmu.SynchronizeUnifiedTlb(Mmu.Control.Urc);
    }

    internal partial void Execute_i0000_nnnn_1001_0011(Sh4Context context, uint opcode)
    {
#if SH4_STRICT_MODE
        _operandCache.WriteBack(context.Registers[GetN(opcode)], false, true);
#endif
    }

    internal partial void Execute_i0000_nnnn_1010_0011(Sh4Context context, uint opcode)
    {
#if SH4_STRICT_MODE
        _operandCache.WriteBack(context.Registers[GetN(opcode)], true, true);
#endif
    }

    internal partial void Execute_i0000_nnnn_1011_0011(Sh4Context context, uint opcode)
    {
#if SH4_STRICT_MODE
        _operandCache.WriteBack(context.Registers[GetN(opcode)], true, false);
#endif
    }

    internal partial void Execute_i0000_nnnn_1000_0011(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        uint destination = context.Registers[n];

        if ((destination >> 26) == 0x38)
        {
            context.WriteStoreQueue(destination);
        }
        else
        {
#if SH4_STRICT_MODE
            _operandCache.Prefetch(destination);
#endif
        }
    }

    internal partial void Execute_i0000_0000_0101_1000(Sh4Context context, uint opcode)
    {
        context.StatusRegister.S = true;
    }

    internal partial void Execute_i0000_0000_0100_1000(Sh4Context context, uint opcode)
    {
        context.StatusRegister.S = false;
    }

    internal partial void Execute_i0000_0000_0001_1000(Sh4Context context, uint opcode)
    {
        context.StatusRegister.T = true;
    }

    internal partial void Execute_i0000_0000_0000_1000(Sh4Context context, uint opcode)
    {
        context.StatusRegister.T = false;
    }

    internal partial void Execute_i0000_nnnn_0010_1001(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Registers[n] = (context.StatusRegister.T ? 1u : 0u);
    }

    internal partial void Execute_i0100_nnnn_0001_0001(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        if (((int)context.Registers[n]) >= 0)
            context.StatusRegister.T = true;
        else
            context.StatusRegister.T = false;
    }

    internal partial void Execute_i0100_nnnn_0001_0101(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        if ((int)context.Registers[n] > 0)
            context.StatusRegister.T = true;
        else
            context.StatusRegister.T = false;
    }

    internal partial void Execute_i1000_1000_iiii_iiii(Sh4Context context, uint opcode)
    {
        uint imm = (uint)(int)(GetSImm8(opcode));
        if (context.Registers[0] == imm)
            context.StatusRegister.T = true;
        else
            context.StatusRegister.T = false;
    }

    internal partial void Execute_i0011_nnnn_mmmm_0000(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);

        if (context.Registers[m] == context.Registers[n])
            context.StatusRegister.T = true;
        else
            context.StatusRegister.T = false;
    }

    internal partial void Execute_i0011_nnnn_mmmm_0010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        if (context.Registers[n] >= context.Registers[m])
            context.StatusRegister.T = true;
        else
            context.StatusRegister.T = false;
    }

    internal partial void Execute_i0011_nnnn_mmmm_0011(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        if ((int)context.Registers[n] >= (int)context.Registers[m])
            context.StatusRegister.T = true;
        else
            context.StatusRegister.T = false;
    }

    internal partial void Execute_i0011_nnnn_mmmm_0110(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);

        if (context.Registers[n] > context.Registers[m])
            context.StatusRegister.T = true;
        else
            context.StatusRegister.T = false;
    }

    internal partial void Execute_i0011_nnnn_mmmm_0111(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);

        if ((int)context.Registers[n] > (int)context.Registers[m])
            context.StatusRegister.T = true;
        else
            context.StatusRegister.T = false;
    }

    internal partial void Execute_i0010_nnnn_mmmm_1100(Sh4Context context, uint opcode)
    {

        int n = GetN(opcode);
        int m = GetM(opcode);

        uint temp;
        uint HH, HL, LH, LL;

        temp = context.Registers[n] ^ context.Registers[m];

        HH = (temp & 0xFF000000) >> 24;
        HL = (temp & 0x00FF0000) >> 16;
        LH = (temp & 0x0000FF00) >> 8;
        LL = temp & 0x000000FF;
        HH = HH != 0 && HL != 0 && LH != 0 && LL != 0 ? 1u : 0u;
        if (HH == 0)
            context.StatusRegister.T = true;
        else
            context.StatusRegister.T = false;
    }

    internal partial void Execute_i1100_1000_iiii_iiii(Sh4Context context, uint opcode)
    {
        uint utmp1 = context.Registers[0] & GetImm8(opcode);
        if (utmp1 == 0)
            context.StatusRegister.T = true;
        else
            context.StatusRegister.T = false;
    }

    internal partial void Execute_i0010_nnnn_mmmm_1000(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);

        if ((context.Registers[n] & context.Registers[m]) != 0)
            context.StatusRegister.T = false;
        else
            context.StatusRegister.T = true;

    }

    internal partial void Execute_i0010_nnnn_mmmm_1110(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        context.Macl = unchecked((uint)((ushort)context.Registers[n] * (ushort)context.Registers[m]));
    }

    internal partial void Execute_i0010_nnnn_mmmm_1111(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);

        context.Macl = (uint)((short)context.Registers[n] * (short)context.Registers[m]);
    }

    internal partial void Execute_i0011_nnnn_mmmm_0101(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);

        context.Mac = (ulong)context.Registers[n] * (ulong)context.Registers[m];
    }

    internal partial void Execute_i0011_nnnn_mmmm_1101(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);

        context.Mac = unchecked((ulong)((long)(int)context.Registers[n] * (long)(int)context.Registers[m]));
    }

    internal partial void Execute_i0100_nnnn_mmmm_1111(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        if (context.StatusRegister.S)
        {
            Sh4HostRuntime.Die("mac.w @<REG_M>+,@<REG_N>+ : S=1");
        }
        else
        {
            int rm,rn;

            rn = (int)(short)context.ReadMemory16(context.Registers[n]);
            rm = (int)(short)context.ReadMemory16(context.Registers[m] + (n == m ? 2u : 0u));

            context.Registers[n] += 2;
            context.Registers[m] += 2;

            int mul = rm * rn;
            context.Mac = unchecked(context.Mac + (ulong)((long)mul));
        }
    }

    internal partial void Execute_i0000_nnnn_mmmm_1111(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        int rm, rn;

        Sh4HostRuntime.Verify(!context.StatusRegister.S);

        rm = unchecked((int)context.ReadMemory32(context.Registers[m]));
        rn = unchecked((int)context.ReadMemory32(context.Registers[n] + (n == m ? 4u : 0u)));
        context.Registers[m] += 4;
        context.Registers[n] += 4;

        context.Mac = unchecked(context.Mac + (ulong)((long)rm * (long)rn));
    }

    internal partial void Execute_i0000_nnnn_mmmm_0111(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        context.Macl = (uint)((((int)context.Registers[n]) * ((int)context.Registers[m])));
    }

    internal partial void Execute_i0000_0000_0001_1001(Sh4Context context, uint opcode)
    {
        context.StatusRegister.Q = false;
        context.StatusRegister.M = false;
        context.StatusRegister.T = false;
    }

    internal partial void Execute_i0010_nnnn_mmmm_0111(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);

        context.StatusRegister.Q = (context.Registers[n] >> 31) != 0;
        context.StatusRegister.M = (context.Registers[m] >> 31) != 0;
        context.StatusRegister.T = context.StatusRegister.M ^ context.StatusRegister.Q;
    }

    internal partial void Execute_i0011_nnnn_mmmm_0100(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);

        bool oldQ = context.StatusRegister.Q;
        context.StatusRegister.Q = ((0x80000000 & context.Registers[n]) != 0);

        uint old_rm = context.Registers[m];
        context.Registers[n] <<= 1;
        context.Registers[n] |= (context.StatusRegister.T ? 1u : 0u);

        uint old_rn = context.Registers[n];

        if (!oldQ)
        {
            if (!context.StatusRegister.M)
            {
                context.Registers[n] -= old_rm;
                bool tmp1 = context.Registers[n] > old_rn;
                context.StatusRegister.Q = context.StatusRegister.Q ^ tmp1;
            }
            else
            {
                context.Registers[n] += old_rm;
                bool tmp1 = context.Registers[n] < old_rn;
                context.StatusRegister.Q = !context.StatusRegister.Q ^ tmp1;
            }
        }
        else
        {
            if (!context.StatusRegister.M)
            {
                context.Registers[n] += old_rm;
                bool tmp1 = context.Registers[n] < old_rn;
                context.StatusRegister.Q = context.StatusRegister.Q ^ tmp1;
            }
            else
            {
                context.Registers[n] -= old_rm;
                bool tmp1 = context.Registers[n] > old_rn;
                context.StatusRegister.Q = !context.StatusRegister.Q ^ tmp1;
            }
        }
        context.StatusRegister.T = (context.StatusRegister.Q == context.StatusRegister.M);
    }

    internal partial void Execute_i0011_nnnn_mmmm_1110(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        uint tmp1 = context.Registers[n] + context.Registers[m];
        uint tmp0 = context.Registers[n];

        context.Registers[n] = tmp1 + (context.StatusRegister.T ? 1u : 0u);

        if (tmp0 > tmp1)
            context.StatusRegister.T = true;
        else
            context.StatusRegister.T = false;

        if (tmp1 > context.Registers[n])
            context.StatusRegister.T = true;
    }

    internal partial void Execute_i0011_nnnn_mmmm_1111(Sh4Context context, uint opcode)
    {

        int n = GetN(opcode);
        int m = GetM(opcode);
        long br = (long)(int)context.Registers[n] + (long)(int)context.Registers[m];

        if (br >= 0x80000000)
            context.StatusRegister.T = true;
        else if (br < int.MinValue)
            context.StatusRegister.T = true;
        else
            context.StatusRegister.T = false;

        context.Registers[n] += context.Registers[m];
    }

    internal partial void Execute_i0011_nnnn_mmmm_1010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);

        uint tmp1 = context.Registers[n] - context.Registers[m];
        uint tmp0 = context.Registers[n];
        context.Registers[n] = tmp1 - (context.StatusRegister.T ? 1u : 0u);

        if (tmp0 < tmp1)
            context.StatusRegister.T = true;
        else
            context.StatusRegister.T = false;

        if (tmp1 < context.Registers[n])
            context.StatusRegister.T = true;
    }

    internal partial void Execute_i0011_nnnn_mmmm_1011(Sh4Context context, uint opcode)
    {

        int n = GetN(opcode);
        int m = GetM(opcode);
        long br = (long)(int)context.Registers[n] - (long)(int)context.Registers[m];

        if (br >= 0x80000000)
            context.StatusRegister.T = true;
        else if (br < int.MinValue)
            context.StatusRegister.T = true;
        else
            context.StatusRegister.T = false;

        context.Registers[n] -= context.Registers[m];
    }

    internal partial void Execute_i0100_nnnn_0001_0000(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Registers[n] -= 1;
        if (context.Registers[n] == 0)
            context.StatusRegister.T = true;
        else
            context.StatusRegister.T = false;
    }

    internal partial void Execute_i0110_nnnn_mmmm_1010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);

        uint tmp = unchecked(0u - context.Registers[m]);
        context.Registers[n] = tmp - (context.StatusRegister.T ? 1u : 0u);

        if (0 < tmp)
            context.StatusRegister.T = true;
        else
            context.StatusRegister.T = false;

        if (tmp < context.Registers[n])
            context.StatusRegister.T = true;
    }

    internal partial void Execute_i0110_nnnn_mmmm_1011(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        context.Registers[n] = unchecked(0u - context.Registers[m]);
    }

    internal partial void Execute_i0110_nnnn_mmmm_0111(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);

        context.Registers[n] = ~context.Registers[m];
    }

    internal partial void Execute_i0100_nnnn_0000_0000(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        context.StatusRegister.T = (context.Registers[n] >> 31) != 0;
        context.Registers[n] <<= 1;
    }

    internal partial void Execute_i0100_nnnn_0010_0000(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.StatusRegister.T = (context.Registers[n] >> 31) != 0;
        context.Registers[n] = unchecked((uint)(((int)context.Registers[n]) << 1));
    }

    internal partial void Execute_i0100_nnnn_0000_0001(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.StatusRegister.T = (context.Registers[n] & 0x1) != 0;
        context.Registers[n] >>= 1;
    }

    internal partial void Execute_i0100_nnnn_0010_0001(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        context.StatusRegister.T = (context.Registers[n] & 1) != 0;
        context.Registers[n] = unchecked((uint)(((int)context.Registers[n]) >> 1));
    }

    internal partial void Execute_i0100_nnnn_mmmm_1100(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        uint sgn = context.Registers[m] & 0x80000000;
        if (sgn == 0)
            context.Registers[n] <<= (int)(context.Registers[m] & 0x1F);
        else if ((context.Registers[m] & 0x1F) == 0)
            context.Registers[n] = unchecked((uint)((int)context.Registers[n] >> 31));
        else
            context.Registers[n] = unchecked((uint)((int)context.Registers[n] >> (int)((~context.Registers[m] & 0x1F) + 1)));
    }

    internal partial void Execute_i0100_nnnn_mmmm_1101(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        uint sgn = context.Registers[m] & 0x80000000;
        if (sgn == 0)
            context.Registers[n] <<= (int)(context.Registers[m] & 0x1F);
        else if ((context.Registers[m] & 0x1F) == 0)
            context.Registers[n] = 0;
        else
            context.Registers[n] = ((uint)context.Registers[n]) >> (int)((~context.Registers[m] & 0x1F) + 1);
    }

    internal partial void Execute_i0100_nnnn_0010_0100(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        uint t = (context.StatusRegister.T ? 1u : 0u);
        context.StatusRegister.T = (context.Registers[n] >> 31) != 0;
        context.Registers[n] <<= 1;
        context.Registers[n] |= t;
    }

    internal partial void Execute_i0100_nnnn_0000_0100(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        context.StatusRegister.T = (context.Registers[n] >> 31) != 0;
        context.Registers[n] <<= 1;
        context.Registers[n] |= (context.StatusRegister.T ? 1u : 0u);
    }

    internal partial void Execute_i0100_nnnn_0010_0101(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        uint t = context.Registers[n] & 0x1;
        context.Registers[n] >>= 1;
        context.Registers[n] |= (context.StatusRegister.T ? 1u : 0u) << 31;
        context.StatusRegister.T = (t) != 0;
    }

    internal partial void Execute_i0100_nnnn_0000_0101(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        context.StatusRegister.T = (context.Registers[n] & 0x1) != 0;
        context.Registers[n] >>= 1;
        context.Registers[n] |= (context.StatusRegister.T ? 1u : 0u) << 31;
    }

    internal partial void Execute_i0110_nnnn_mmmm_1000(Sh4Context context, uint opcode)
    {
        int m = GetM(opcode);
        int n = GetN(opcode);

        uint rg = context.Registers[m];
        context.Registers[n] = (rg & 0xFFFF0000) | ((rg & 0xFF) << 8) | ((rg >> 8) & 0xFF);
    }

    internal partial void Execute_i0110_nnnn_mmmm_1001(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);

        ushort t = (ushort)(context.Registers[m] >> 16);
        context.Registers[n] = (context.Registers[m] << 16) | t;
    }

    internal partial void Execute_i0110_nnnn_mmmm_1100(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        context.Registers[n] = (uint)(byte)context.Registers[m];
    }

    internal partial void Execute_i0110_nnnn_mmmm_1101(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);
        context.Registers[n] = (uint)(ushort)context.Registers[m];
    }

    internal partial void Execute_i0110_nnnn_mmmm_1110(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);

        context.Registers[n] = (uint)(int)(sbyte)(byte)context.Registers[m];
    }

    internal partial void Execute_i0110_nnnn_mmmm_1111(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);

        context.Registers[n] = (uint)(int)(short)(ushort)context.Registers[m];
    }

    internal partial void Execute_i0010_nnnn_mmmm_1101(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        int m = GetM(opcode);

        context.Registers[n] = ((context.Registers[n] >> 16) & 0xFFFF) | ((context.Registers[m] << 16) & 0xFFFF0000);
    }

    internal partial void Execute_i1100_1100_iiii_iiii(Sh4Context context, uint opcode)
    {

        uint imm = GetImm8(opcode);

        uint temp = (byte)context.ReadMemory8(context.Gbr + context.Registers[0]);

        temp &= imm;

        if (temp == 0)
            context.StatusRegister.T = true;
        else
            context.StatusRegister.T = false;
    }

    internal partial void Execute_i1100_1101_iiii_iiii(Sh4Context context, uint opcode)
    {
        byte temp = (byte)context.ReadMemory8(context.Gbr + context.Registers[0]);

        temp &= (byte)GetImm8(opcode);

        context.WriteMemory8(context.Gbr + context.Registers[0], temp);
    }

    internal partial void Execute_i1100_1110_iiii_iiii(Sh4Context context, uint opcode)
    {
        byte temp = (byte)context.ReadMemory8(context.Gbr + context.Registers[0]);

        temp ^= (byte)GetImm8(opcode);

        context.WriteMemory8(context.Gbr + context.Registers[0], temp);
    }

    internal partial void Execute_i1100_1111_iiii_iiii(Sh4Context context, uint opcode)
    {
        byte temp = (byte)context.ReadMemory8(context.Gbr + context.Registers[0]);

        temp |= (byte)GetImm8(opcode);

        context.WriteMemory8(context.Gbr + context.Registers[0], temp);
    }

    internal partial void Execute_i0100_nnnn_0001_1011(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        byte val = (byte)context.ReadMemory8(context.Registers[n]);

        uint srT;
        if (val == 0)
            srT = 1;
        else
            srT = 0;

        val |= 0x80;

        context.WriteMemory8(context.Registers[n], val);

        context.StatusRegister.T = (srT) != 0;
    }

    internal partial void Execute_i0000_nnnn_0000_0010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Registers[n] = context.StatusRegister.Full;
    }

    internal partial void Execute_i0000_nnnn_0110_1010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Registers[n] = context.Fpscr.Full;
    }

    internal partial void Execute_i0100_nnnn_0110_0010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.WriteMemory32(context.Registers[n] - 4, context.Fpscr.Full);
        context.Registers[n] -= 4;
    }

    internal partial void Execute_i0100_nnnn_0000_0011(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.WriteMemory32(context.Registers[n] - 4, context.StatusRegister.Full);
        context.Registers[n] -= 4;
    }

    internal partial void Execute_i0100_nnnn_0110_0110(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        context.Fpscr.Full = context.ReadMemory32(context.Registers[n]);
        context.UpdateFpscr();
        context.Registers[n] += 4;
    }

    internal partial void Execute_i0100_nnnn_0000_0111(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);

        uint sr_t;
        sr_t = context.ReadMemory32(context.Registers[n]);

        context.StatusRegister.SetFull(sr_t);
        context.Registers[n] += 4;
        if (context.UpdateStatusRegister())
            _interrupts.Update();
    }

    internal partial void Execute_i0100_nnnn_0110_1010(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.Fpscr.Full = context.Registers[n];
        context.UpdateFpscr();
    }

    internal partial void Execute_i0100_nnnn_0000_1110(Sh4Context context, uint opcode)
    {
        int n = GetN(opcode);
        context.StatusRegister.SetFull(context.Registers[n]);
        if (context.UpdateStatusRegister())
            _interrupts.Update();
    }

    internal partial void Execute_iNotImplemented(Sh4Context context, uint opcode)
    {
        Trace.TraceInformation($"iNimp {opcode:X4}");
        _debugger.Trap(Sh4ExceptionEvent.IllegalInstruction);

        throw new Sh4InstructionException(context.ProgramCounter - 2, Sh4ExceptionEvent.IllegalInstruction);
    }
}

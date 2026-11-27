namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal enum Sh4ExecutionUnit
{
    MT,
    EX,
    BR,
    LS,
    FE,
    CO,
}

[Flags]
internal enum Sh4OpcodeFlags : uint
{
    Normal = 0,
    ReadsPC = 1,
    WritesPC = 2,
    Delayslot = 4,
    WritesSR = 8,
    WritesFPSCR = 16,
    Invalid = 128,
    UsesFPU = 2048,
    FWritesFPSCR = UsesFPU | WritesFPSCR,
    ReadWritePC = ReadsPC | WritesPC,
    WritesSRRWPC = WritesSR | ReadsPC | WritesPC,
    Branch_dir = ReadWritePC,
    Branch_rel = ReadWritePC,
    Branch_dir_d = Delayslot | Branch_dir,
    Branch_rel_d = Delayslot | Branch_rel,
}

internal enum Sh4DecodeParameter
{
    PRM_PC_D8_x2,
    PRM_PC_D8_x4,
    PRM_ZERO,
    PRM_ONE,
    PRM_TWO,
    PRM_TWO_INV,
    PRM_ONE_F32,
    PRM_SIMM8,
    PRM_UIMM8,
    PRM_R0,
    PRM_RN,
    PRM_RM,
    PRM_FRN,
    PRM_FRN_SZ,
    PRM_FRM,
    PRM_FRM_SZ,
    PRM_FPN,
    PRM_FVN,
    PRM_FVM,
    PRM_XMTRX,
    PRM_FRM_FR0,
    PRM_FPUL,
    PRM_SR_T,
    PRM_SR_STATUS,
    PRM_SREG,
    PRM_CREG,
    PRM_RN_D4_x1,
    PRM_RN_D4_x2,
    PRM_RN_D4_x4,
    PRM_RN_R0,
    PRM_RM_R0,
    PRM_RM_D4_x1,
    PRM_RM_D4_x2,
    PRM_RM_D4_x4,
    PRM_GBR_D8_x1,
    PRM_GBR_D8_x2,
    PRM_GBR_D8_x4,
}

internal enum Sh4DecodeMode
{
    DM_ReadSRF,
    DM_BinaryOp,
    DM_UnaryOp,
    DM_ReadM,
    DM_WriteM,
    DM_fiprOp,
    DM_WriteTOp,
    DM_DT,
    DM_Shift,
    DM_Rot,
    DM_EXTOP,
    DM_MUL,
    DM_DIV0,
    DM_ADC,
    DM_NEGC,
}

internal sealed class Sh4OpcodeDescriptor(
    Action<Sh4Decoder, uint>? recompilerOpcode,
    Action<Sh4Interpreter, Sh4Context, uint>? interpreterOpcode,
    uint mask,
    uint value,
    Sh4OpcodeFlags flags,
    string? format,
    byte issueCycles,
    byte latencyCycles,
    Sh4ExecutionUnit unit,
    byte executionType,
    ulong decode,
    nuint nativeInterpreterOpcode)
{
    internal Action<Sh4Decoder, uint>? RecompilerOpcode { get; } = recompilerOpcode;
    internal Action<Sh4Interpreter, Sh4Context, uint>? InterpreterOpcode { get; } = interpreterOpcode;
    internal nuint NativeInterpreterOpcode { get; } = nativeInterpreterOpcode;
    internal uint Mask { get; } = mask;
    internal uint Value { get; } = value;
    internal byte IssueCycles { get; } = issueCycles;
    internal byte LatencyCycles { get; } = latencyCycles;
    internal Sh4ExecutionUnit Unit { get; } = unit;
    internal byte ExecutionType { get; } = executionType;
    internal ulong Decode { get; } = decode;

    internal bool SetsPc => (flags & Sh4OpcodeFlags.WritesPC) != 0;
    internal bool NeedPc => (flags & Sh4OpcodeFlags.ReadsPC) != 0;
    internal bool SetsSr => (flags & Sh4OpcodeFlags.WritesSR) != 0;
    internal bool SetsFpscr => (flags & Sh4OpcodeFlags.WritesFPSCR) != 0;
    internal bool IsFloatingPoint => (flags & Sh4OpcodeFlags.UsesFPU) != 0;

    internal string Format { get; set; } = format!;

    internal string Disassemble(uint pc, ushort opcode, Sh4Context context) =>
        Sh4OpcodeTable.Disassemble(Format == "missing" ? $"?{opcode:X4}" : Format, pc, opcode, context);
}

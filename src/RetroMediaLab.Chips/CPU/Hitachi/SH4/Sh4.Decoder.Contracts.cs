namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal enum Sh4BlockEndType
{
    BET_CLS_Static = 0,
    BET_CLS_Dynamic = 1,
    BET_CLS_COND = 2,
    BET_SCL_Jump = 0,
    BET_SCL_Call = 1,
    BET_SCL_Ret = 2,
    BET_SCL_Intr = 3,

    BET_StaticJump = (BET_CLS_Static << 3) | (BET_SCL_Jump << 1),
    BET_StaticCall = (BET_CLS_Static << 3) | (BET_SCL_Call << 1),
    BET_StaticIntr = (BET_CLS_Static << 3) | (BET_SCL_Intr << 1),
    BET_DynamicJump = (BET_CLS_Dynamic << 3) | (BET_SCL_Jump << 1),
    BET_DynamicCall = (BET_CLS_Dynamic << 3) | (BET_SCL_Call << 1),
    BET_DynamicRet = (BET_CLS_Dynamic << 3) | (BET_SCL_Ret << 1),
    BET_DynamicIntr = (BET_CLS_Dynamic << 3) | (BET_SCL_Intr << 1),
    BET_Cond_0 = (BET_CLS_COND << 3) | (BET_SCL_Jump << 1),
    BET_Cond_1 = (BET_CLS_COND << 3) | (BET_SCL_Jump << 1) | 1
}

internal enum Sh4NextDecoderOperation
{
    Next,
    End,
    DelaySlot
}

internal sealed class Sh4DecoderState
{
    public Sh4NextDecoderOperation NextOperation;
    public Sh4NextDecoderOperation DelayOperation;
    public uint JumpAddress;
    public uint NextAddress;
    public Sh4BlockEndType BlockType;
    public CpuState Cpu;
    public BlockInfo Info;

    internal struct CpuState
    {
        public bool Fpr64;
        public bool Fsz64;
        public bool RoundToZero;
        public uint ProgramCounter;
        public bool IsDelaySlot;
    }

    internal struct BlockInfo
    {
        public bool HasReadMemory;
        public bool HasWriteMemory;
        public bool HasFpu;
    }
}

internal static class Sh4DecoderConstants
{
    internal const uint NullAddress = uint.MaxValue;

    internal static Sh4BlockEndType GetBlockClass(Sh4BlockEndType type) => (Sh4BlockEndType)((int)type >> 3);

    internal static uint GetN(uint opcode) => (opcode >> 8) & 0xF;
    internal static uint GetM(uint opcode) => (opcode >> 4) & 0xF;
    internal static uint GetImm4(uint opcode) => opcode & 0xF;
    internal static uint GetImm8(uint opcode) => opcode & 0xFF;
    internal static int GetSImm8(uint opcode) => unchecked((sbyte)GetImm8(opcode));
    internal static uint GetImm12(uint opcode) => opcode & 0xFFF;
    internal static int GetSImm12(uint opcode) => unchecked((short)(GetImm12(opcode) << 4)) >> 4;
}

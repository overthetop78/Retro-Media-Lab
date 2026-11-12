namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;

public enum CpuZ80Operation
{
    InterruptNmi, InterruptMode2, None, LoadImmediateByte, LoadImmediatePair, ReadAbsoluteByte, WriteAbsoluteByte,
    ReadAbsoluteWord, WriteAbsoluteWord, JumpAbsolute, JumpConditional,
    InputImmediate, OutputImmediate, PopPair, Return, ReturnNmi, ArithmeticImmediate,
    IncrementPair, DecrementPair, LoadStackPointer, AddPair, AddCarryPair, SubtractCarryPair,
    PushPair, Call, CallConditional, Restart, JumpRelative, JumpRelativeConditional,
    DecrementJumpRelative, IncrementMemory, DecrementMemory, CbMemory,
    IndexedReadRegister, IndexedWriteRegister, IndexedImmediate, IndexedArithmetic,
    LoadIFromAccumulator, LoadRFromAccumulator, LoadAccumulatorFromI, LoadAccumulatorFromR,
    RotateDigitsLeft, RotateDigitsRight,
    BlockTransfer, BlockCompare, BlockInput, BlockOutput,
}

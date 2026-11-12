namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;

/// <summary>Position du séquenceur C# canonique, conservée pour une reprise en cours d'accès.</summary>
public enum CpuZ80Flow
{
    HaltFetch, SpecialResetFetch, NmiFetch, InterruptAcknowledge, InterruptVectorLow, InterruptVectorHigh, InternalResetRecovery,
    Fetch, PrefixEd, ImmediateByte, ImmediateLow, ImmediateHigh, ReadRegister,
    WriteMemory, ReadWordLow, ReadWordHigh, WriteWordLow, WriteWordHigh,
    IoRead, IoWrite, PopLow, PopHigh, ReadArithmetic, PrefixCb,
    InternalOperation, InternalPush, InternalRelativeRead, InternalRelativeJump,
    InternalConditionalReturn, InternalModifyWrite, ReadModifyWrite, PushHigh, PushLow,
    PrefixIndex, IndexDisplacement, IndexImmediate, InternalIndexedAccess, ReadIndexedRegister,
    IndexCbDisplacement, IndexCbOpcode, InternalIndexCbRead,
    ExchangeLow, ExchangeHigh, InternalExchangeWrite, ExchangeWriteHigh, ExchangeWriteLow, InternalExchangeFinish,
    IoReadRegister,
    ReadBit, InternalBitFinish,
    BlockReadMemory, BlockWriteMemory, BlockInput, BlockOutput,
    InternalBlockStart, InternalBlockFinish, InternalBlockRepeat,
}

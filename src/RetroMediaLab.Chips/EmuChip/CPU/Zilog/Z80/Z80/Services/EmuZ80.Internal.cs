using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;
using F = RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Constants.Z80Flags;

namespace RetroMediaLab.Chips.EmuChip.CPU.Zilog.Z80.Z80.Services;

public sealed partial class EmuZ80
{
    // États internes après une transaction réelle : le bus finit normalement
    // ses propagations. Aucun MREQ/RD/WR ni rafraîchissement supplémentaire.
    private void BeginInternal(int clocks, CpuZ80Flow continuation)
    {
        _internalClocksRemaining = clocks;
        _flow = continuation;
        if (clocks == 1) SampleControlInputs();
        else _control.SampledBusRequest = false;
    }

    private void CompleteInternal()
    {
        switch (_flow)
        {
            case CpuZ80Flow.InternalResetRecovery: BeginFetch(); break;
            case CpuZ80Flow.InternalBlockStart: BeginBlockIo(); break;
            case CpuZ80Flow.InternalBlockFinish: CompleteBlockOperation(); break;
            case CpuZ80Flow.InternalBlockRepeat: RepeatBlock(); break;
            case CpuZ80Flow.InternalOperation:
                CompletePairOperation();
                RetireInstruction();
                break;
            case CpuZ80Flow.InternalPush:
                _registers.SP = unchecked((ushort)(_registers.SP - 1));
                WriteMemory(_registers.SP, (byte)(_word >> 8), CpuZ80Flow.PushHigh);
                break;
            case CpuZ80Flow.InternalRelativeRead:
                _registers.B = unchecked((byte)(_registers.B - 1));
                OperandRead(CpuZ80Flow.ImmediateByte);
                break;
            case CpuZ80Flow.InternalRelativeJump:
                _registers.PC = _address;
                _registers.WZ = _address;
                RetireInstruction();
                break;
            case CpuZ80Flow.InternalConditionalReturn:
                if (Condition((byte)((_opcode >> 3) & 7))) BeginPop(CpuZ80Operation.Return);
                else RetireInstruction();
                break;
            case CpuZ80Flow.InternalModifyWrite:
                CompleteMemoryModification();
                break;
            case CpuZ80Flow.InternalIndexedAccess:
                CompleteIndexedAccess();
                break;
            case CpuZ80Flow.InternalIndexCbRead:
                ReadMemory(_address, (_opcode >> 6) == 1 ? CpuZ80Flow.ReadBit : CpuZ80Flow.ReadModifyWrite);
                break;
            case CpuZ80Flow.InternalBitFinish:
                TestBit(_low, (byte)(_registers.WZ >> 8));
                RetireInstruction();
                break;
            case CpuZ80Flow.InternalExchangeWrite:
                WriteMemory(unchecked((ushort)(_registers.SP + 1)), (byte)(_word >> 8), CpuZ80Flow.ExchangeWriteHigh);
                break;
            case CpuZ80Flow.InternalExchangeFinish:
                ActivePair = _address;
                _registers.WZ = _address;
                _bus.ReleaseWriteData();
                RetireInstruction();
                break;
            default: throw new InvalidOperationException("Invalid Z80 internal continuation.");
        }
    }

    private void CompletePairOperation()
    {
        byte flags;
        switch (_operation)
        {
            case CpuZ80Operation.LoadIFromAccumulator:
            case CpuZ80Operation.LoadRFromAccumulator:
            case CpuZ80Operation.LoadAccumulatorFromI:
            case CpuZ80Operation.LoadAccumulatorFromR:
                CompleteSpecialTransfer();
                break;
            case CpuZ80Operation.IncrementPair:
                SetPair(_pairCode, unchecked((ushort)(GetPair(_pairCode) + 1)));
                break;
            case CpuZ80Operation.DecrementPair:
                SetPair(_pairCode, unchecked((ushort)(GetPair(_pairCode) - 1)));
                break;
            case CpuZ80Operation.LoadStackPointer:
                _registers.SP = ActivePair;
                break;
            case CpuZ80Operation.AddPair:
                LatchNextAddress(ActivePair);
                ActivePair = Z80Arithmetic.Add16(ActivePair, GetPair(_pairCode), _registers.F, out flags);
                WriteFlags((byte)(flags | ((ActivePair >> 8) & F.ResultBits)));
                break;
            case CpuZ80Operation.AddCarryPair:
                LatchNextAddress(_registers.HL);
                _registers.HL = Z80Arithmetic.AddCarry16(_registers.HL, GetPair(_pairCode),
                    (_registers.F & F.Carry) != 0, out flags);
                WriteFlags((byte)(flags | ((_registers.HL >> 8) & F.ResultBits)));
                break;
            case CpuZ80Operation.SubtractCarryPair:
                LatchNextAddress(_registers.HL);
                _registers.HL = Z80Arithmetic.SubtractCarry16(_registers.HL, GetPair(_pairCode),
                    (_registers.F & F.Carry) != 0, out flags);
                WriteFlags((byte)(flags | ((_registers.HL >> 8) & F.ResultBits)));
                break;
            default: throw new InvalidOperationException("Invalid Z80 pair operation.");
        }
    }

    private void BeginPairOperation(CpuZ80Operation operation, int clocks)
    {
        _operation = operation;
        _pairCode = (byte)((_opcode >> 4) & 3);
        BeginInternal(clocks, CpuZ80Flow.InternalOperation);
    }

    private void BeginStackWrite(CpuZ80Operation operation, ushort word)
    {
        _operation = operation;
        _word = word;
        BeginInternal(1, CpuZ80Flow.InternalPush);
    }

    private void CompleteRelative(byte displacement)
    {
        bool taken = _operation == CpuZ80Operation.JumpRelative
            || (_operation == CpuZ80Operation.DecrementJumpRelative ? _registers.B != 0
                : Condition((byte)((_opcode >> 3) & 3)));
        if (!taken) { RetireInstruction(); return; }
        _address = unchecked((ushort)(_registers.PC + unchecked((sbyte)displacement)));
        BeginInternal(5, CpuZ80Flow.InternalRelativeJump);
    }

    private void CompleteMemoryModification()
    {
        byte value, flags;
        if (_operation == CpuZ80Operation.CbMemory)
        {
            value = ModifyCb(_low);
            // DDCB/FDCB copie aussi vers le registre, même si la mémoire
            // raccordée refuse son écriture (Young §3.5, relevés Devic).
            if (_index != CpuZ80Index.None && _registerCode != 6)
                SetRegister(_registerCode, value, substituteIndex: false);
        }
        else if (_operation is CpuZ80Operation.RotateDigitsLeft or CpuZ80Operation.RotateDigitsRight)
            value = RotateDigits(_low);
        else
        {
            if (_operation == CpuZ80Operation.IncrementMemory)
                value = Z80Arithmetic.Increment8(_low, _registers.F, out flags);
            else value = Z80Arithmetic.Decrement8(_low, _registers.F, out flags);
            WriteFlags((byte)(flags | (value & F.ResultBits)));
        }
        WriteMemory(_address, value);
    }
}

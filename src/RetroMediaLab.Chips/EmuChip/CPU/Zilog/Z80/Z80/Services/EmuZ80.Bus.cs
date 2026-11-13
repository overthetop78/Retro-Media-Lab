using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;

namespace RetroMediaLab.Chips.EmuChip.CPU.Zilog.Z80.Z80.Services;

public sealed partial class EmuZ80
{
    private void BeginFetch(bool prefixEd = false, bool prefixCb = false, bool prefixIndex = false)
    {
        bool continuation = prefixEd || prefixCb || prefixIndex;
        _prefixEd = prefixEd;
        if (!continuation)
        {
            _instructionAddress = _registers.PC;
            _operation = CpuZ80Operation.None;
            _index = CpuZ80Index.None;
            _instructionFlagsModified = false;
        }
        _flow = prefixEd ? CpuZ80Flow.PrefixEd : prefixCb ? CpuZ80Flow.PrefixCb
            : prefixIndex ? CpuZ80Flow.PrefixIndex : CpuZ80Flow.Fetch;
        ushort address = _registers.PC;
        _registers.PC = unchecked((ushort)(address + 1));
        // Relevés matériels Devic : IR=0 après RESET, premier refresh à 0,
        // puis 1 au M1 suivant. Le bus reçoit la valeur de ce M1, pas la suivante.
        ushort refreshAddress = (ushort)((_registers.I << 8) | _registers.R);
        _registers.IncrementRefresh();
        _bus.BeginCycle(Z80BusCycle.OpcodeFetch, address, refreshAddress);
    }

    private void OperandRead(CpuZ80Flow flow)
    {
        ushort address = _registers.PC;
        _registers.PC = unchecked((ushort)(address + 1));
        ReadMemory(address, flow);
    }

    private void ReadMemory(ushort address, CpuZ80Flow flow)
    {
        _flow = flow;
        _bus.BeginCycle(Z80BusCycle.MemoryRead, address, 0);
    }

    private void WriteMemory(ushort address, byte data, CpuZ80Flow flow = CpuZ80Flow.WriteMemory, bool holdData = false)
    {
        _flow = flow;
        _bus.BeginCycle(Z80BusCycle.MemoryWrite, address, 0, data, holdData);
    }

    private void BusCycleCompleted()
    {
        switch (_flow)
        {
            case CpuZ80Flow.HaltFetch:
                ContinueAtInstructionBoundary();
                break;
            case CpuZ80Flow.SpecialResetFetch:
                ContinueAtInstructionBoundary();
                break;
            case CpuZ80Flow.NmiFetch:
                _address = _registers.WZ = 0x66;
                BeginStackWrite(CpuZ80Operation.InterruptNmi, _registers.PC);
                break;
            case CpuZ80Flow.InterruptAcknowledge:
                CompleteInterruptAcknowledge();
                break;
            case CpuZ80Flow.InterruptVectorLow:
                _low = _bus.SampledData;
                ReadMemory(unchecked((ushort)(_address + 1)), CpuZ80Flow.InterruptVectorHigh);
                break;
            case CpuZ80Flow.InterruptVectorHigh:
                _registers.PC = _registers.WZ = (ushort)(_low | (_bus.SampledData << 8));
                _control.InterruptSequence = false;
                ContinueAtInstructionBoundary();
                break;
            case CpuZ80Flow.Fetch:
            case CpuZ80Flow.PrefixIndex:
                _opcode = _bus.SampledData;
                Decode(_opcode);
                break;
            case CpuZ80Flow.PrefixEd:
                _opcode = _bus.SampledData;
                DecodeEd(_opcode);
                break;
            case CpuZ80Flow.PrefixCb:
                _opcode = _bus.SampledData;
                DecodeCb(_opcode);
                break;
            case CpuZ80Flow.ImmediateByte:
                CompleteImmediateByte(_bus.SampledData);
                break;
            case CpuZ80Flow.ImmediateLow:
                _low = _bus.SampledData;
                OperandRead(CpuZ80Flow.ImmediateHigh);
                break;
            case CpuZ80Flow.ImmediateHigh:
                _address = (ushort)(_low | (_bus.SampledData << 8));
                CompleteImmediateWord();
                break;
            case CpuZ80Flow.ReadRegister:
                SetRegister(_registerCode, _bus.SampledData);
                RetireInstruction();
                break;
            case CpuZ80Flow.ReadIndexedRegister:
                SetRegister(_registerCode, _bus.SampledData, substituteIndex: false);
                RetireInstruction();
                break;
            case CpuZ80Flow.IndexDisplacement:
                CompleteDisplacement(_bus.SampledData);
                break;
            case CpuZ80Flow.IndexImmediate:
                _low = _bus.SampledData;
                BeginInternal(2, CpuZ80Flow.InternalIndexedAccess);
                break;
            case CpuZ80Flow.IndexCbDisplacement:
                _address = unchecked((ushort)(ActivePair + unchecked((sbyte)_bus.SampledData)));
                _registers.WZ = _address;
                OperandRead(CpuZ80Flow.IndexCbOpcode);
                break;
            case CpuZ80Flow.IndexCbOpcode:
                _opcode = _bus.SampledData;
                DecodeIndexedCb(_opcode);
                break;
            case CpuZ80Flow.ExchangeLow:
                _low = _bus.SampledData;
                ReadMemory(unchecked((ushort)(_registers.SP + 1)), CpuZ80Flow.ExchangeHigh);
                break;
            case CpuZ80Flow.ExchangeHigh:
                _address = (ushort)(_low | (_bus.SampledData << 8));
                BeginInternal(1, CpuZ80Flow.InternalExchangeWrite);
                break;
            case CpuZ80Flow.ExchangeWriteHigh:
                WriteMemory(_registers.SP, (byte)_word, CpuZ80Flow.ExchangeWriteLow, holdData: true);
                break;
            case CpuZ80Flow.ExchangeWriteLow:
                BeginInternal(2, CpuZ80Flow.InternalExchangeFinish);
                break;
            case CpuZ80Flow.ReadArithmetic:
                ExecuteArithmetic(_bus.SampledData);
                break;
            case CpuZ80Flow.ReadBit:
                _low = _bus.SampledData;
                BeginInternal(1, CpuZ80Flow.InternalBitFinish);
                break;
            case CpuZ80Flow.BlockReadMemory:
                CompleteBlockRead(_bus.SampledData);
                break;
            case CpuZ80Flow.BlockInput:
                _low = _bus.SampledData;
                WriteMemory(_registers.HL, _low, CpuZ80Flow.BlockWriteMemory);
                break;
            case CpuZ80Flow.BlockWriteMemory:
                if (_operation == CpuZ80Operation.BlockTransfer) BeginInternal(2, CpuZ80Flow.InternalBlockFinish);
                else CompleteBlockOperation();
                break;
            case CpuZ80Flow.BlockOutput:
                CompleteBlockOperation();
                break;
            case CpuZ80Flow.ReadModifyWrite:
                _low = _bus.SampledData;
                BeginInternal(_operation is CpuZ80Operation.RotateDigitsLeft or CpuZ80Operation.RotateDigitsRight
                    ? 4 : 1, CpuZ80Flow.InternalModifyWrite);
                break;
            case CpuZ80Flow.PushHigh:
                _registers.SP = unchecked((ushort)(_registers.SP - 1));
                WriteMemory(_registers.SP, (byte)_word, CpuZ80Flow.PushLow);
                break;
            case CpuZ80Flow.PushLow:
                if (_control.InterruptSequence) { CompleteInterruptStack(); break; }
                if (_operation is CpuZ80Operation.Call or CpuZ80Operation.Restart) _registers.PC = _address;
                RetireInstruction();
                break;
            case CpuZ80Flow.ReadWordLow:
                _low = _bus.SampledData;
                ReadMemory(unchecked((ushort)(_address + 1)), CpuZ80Flow.ReadWordHigh);
                break;
            case CpuZ80Flow.ReadWordHigh:
                SetPair(_pairCode, (ushort)(_low | (_bus.SampledData << 8)));
                RetireInstruction();
                break;
            case CpuZ80Flow.WriteWordLow:
                WriteMemory(unchecked((ushort)(_address + 1)), (byte)(_word >> 8), CpuZ80Flow.WriteWordHigh);
                break;
            case CpuZ80Flow.WriteMemory:
            case CpuZ80Flow.WriteWordHigh:
            case CpuZ80Flow.IoWrite:
                RetireInstruction();
                break;
            case CpuZ80Flow.IoRead:
                _registers.A = _bus.SampledData;
                RetireInstruction();
                break;
            case CpuZ80Flow.IoReadRegister:
                CompleteRegisterInput(_bus.SampledData);
                break;
            case CpuZ80Flow.PopLow:
                _low = _bus.SampledData;
                _registers.SP = unchecked((ushort)(_registers.SP + 1));
                ReadMemory(_registers.SP, CpuZ80Flow.PopHigh);
                break;
            case CpuZ80Flow.PopHigh:
                _registers.SP = unchecked((ushort)(_registers.SP + 1));
                ushort word = (ushort)(_low | (_bus.SampledData << 8));
                if (_operation is CpuZ80Operation.Return or CpuZ80Operation.ReturnNmi)
                {
                    _registers.PC = word;
                    _registers.WZ = word;
                    if (_operation == CpuZ80Operation.ReturnNmi) _interrupts.ReturnFromNonMaskableInterrupt();
                }
                else SetPair(_pairCode, word, stack: true);
                RetireInstruction();
                break;
            default:
                throw new InvalidOperationException("Invalid Z80 instruction flow.");
        }
    }
}

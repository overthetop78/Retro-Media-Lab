using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;
using F = RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Constants.Z80Flags;

namespace RetroMediaLab.Chips.EmuChip.CPU.Zilog.Z80.Z80.Services;

public sealed partial class EmuZ80
{
    // UM008011-0816 : transferts 71–119, échanges 124–126, JP 262–265,
    // RET 285, RETN 290, IN/OUT 295/306. Aucun accès mémoire hôte direct.
    private void Decode(byte opcode)
    {
        if ((opcode & 0xcf) == 0x03) { BeginPairOperation(CpuZ80Operation.IncrementPair, 2); return; }
        if ((opcode & 0xcf) == 0x0b) { BeginPairOperation(CpuZ80Operation.DecrementPair, 2); return; }
        if ((opcode & 0xcf) == 0x09) { BeginPairOperation(CpuZ80Operation.AddPair, 7); return; }
        if ((opcode & 0xcf) == 0xc5)
        {
            byte pair = (byte)((opcode >> 4) & 3);
            BeginStackWrite(CpuZ80Operation.PushPair, pair == 3 ? _registers.AF : GetPair(pair));
            return;
        }
        if ((opcode & 0xc7) == 0xc4) { BeginImmediateWord(CpuZ80Operation.CallConditional); return; }
        if ((opcode & 0xc7) == 0xc0) { BeginInternal(1, CpuZ80Flow.InternalConditionalReturn); return; }
        if ((opcode & 0xc7) == 0xc7)
        {
            _address = (ushort)(opcode & 0x38);
            _registers.WZ = _address;
            BeginStackWrite(CpuZ80Operation.Restart, _registers.PC);
            return;
        }
        if ((opcode & 0xc0) == 0x80)
        {
            byte source = (byte)(opcode & 7);
            if (source == 6)
            {
                if (_index != CpuZ80Index.None) BeginIndexed(CpuZ80Operation.IndexedArithmetic);
                else ReadMemory(_registers.HL, CpuZ80Flow.ReadArithmetic);
            }
            else ExecuteArithmetic(GetRegister(source));
            return;
        }
        if ((opcode & 0xc7) == 0xc6)
        {
            _operation = CpuZ80Operation.ArithmeticImmediate;
            OperandRead(CpuZ80Flow.ImmediateByte);
            return;
        }
        if ((opcode & 0xc6) == 0x04 && ((opcode >> 3) & 7) != 6)
        {
            IncrementDecrementRegister(opcode);
            return;
        }
        if ((opcode & 0xc0) == 0x40)
        {
            if (opcode == 0x76) { _control.Halted = true; RetireInstruction(); return; }
            byte destination = (byte)((opcode >> 3) & 7), source = (byte)(opcode & 7);
            if (_index != CpuZ80Index.None && (destination == 6 || source == 6))
            {
                _registerCode = destination == 6 ? source : destination;
                BeginIndexed(destination == 6 ? CpuZ80Operation.IndexedWriteRegister : CpuZ80Operation.IndexedReadRegister);
            }
            else if (destination == 6) WriteMemory(_registers.HL, GetRegister(source));
            else if (source == 6) { _registerCode = destination; ReadMemory(_registers.HL, CpuZ80Flow.ReadRegister); }
            else { SetRegister(destination, GetRegister(source)); RetireInstruction(); }
            return;
        }
        if ((opcode & 0xc7) == 0x06)
        {
            _registerCode = (byte)((opcode >> 3) & 7);
            if (_registerCode == 6 && _index != CpuZ80Index.None) BeginIndexed(CpuZ80Operation.IndexedImmediate);
            else
            {
                _operation = CpuZ80Operation.LoadImmediateByte;
                OperandRead(CpuZ80Flow.ImmediateByte);
            }
            return;
        }
        if ((opcode & 0xcf) == 0x01)
        {
            _pairCode = (byte)((opcode >> 4) & 3);
            BeginImmediateWord(CpuZ80Operation.LoadImmediatePair);
            return;
        }
        if ((opcode & 0xcf) == 0xc1)
        {
            _pairCode = (byte)((opcode >> 4) & 3);
            BeginPop(CpuZ80Operation.PopPair);
            return;
        }
        if ((opcode & 0xc7) == 0xc2) { BeginImmediateWord(CpuZ80Operation.JumpConditional); return; }
        switch (opcode)
        {
            case 0x00: RetireInstruction(); break;
            case 0x07: case 0x0f: case 0x17: case 0x1f: RotateAccumulator(opcode); break;
            case 0x2f: ComplementAccumulator(); break;
            case 0x27: DecimalAdjust(); break;
            case 0x37: SetComplementCarry(false); break;
            case 0x3f: SetComplementCarry(true); break;
            case 0xf9: BeginPairOperation(CpuZ80Operation.LoadStackPointer, 2); break;
            case 0xcd: BeginImmediateWord(CpuZ80Operation.Call); break;
            case 0x10:
                _operation = CpuZ80Operation.DecrementJumpRelative;
                BeginInternal(1, CpuZ80Flow.InternalRelativeRead);
                break;
            case 0x18:
                _operation = CpuZ80Operation.JumpRelative;
                OperandRead(CpuZ80Flow.ImmediateByte);
                break;
            case 0x20: case 0x28: case 0x30: case 0x38:
                _operation = CpuZ80Operation.JumpRelativeConditional;
                OperandRead(CpuZ80Flow.ImmediateByte);
                break;
            case 0x34: case 0x35:
                _operation = opcode == 0x34 ? CpuZ80Operation.IncrementMemory : CpuZ80Operation.DecrementMemory;
                if (_index != CpuZ80Index.None) BeginIndexed(_operation);
                else
                {
                    _address = _registers.HL;
                    ReadMemory(_address, CpuZ80Flow.ReadModifyWrite);
                }
                break;
            case 0x02: LatchByteStoreAddress(_registers.BC); WriteMemory(_registers.BC, _registers.A); break;
            case 0x12: LatchByteStoreAddress(_registers.DE); WriteMemory(_registers.DE, _registers.A); break;
            case 0x0a: LatchNextAddress(_registers.BC); _registerCode = 7; ReadMemory(_registers.BC, CpuZ80Flow.ReadRegister); break;
            case 0x1a: LatchNextAddress(_registers.DE); _registerCode = 7; ReadMemory(_registers.DE, CpuZ80Flow.ReadRegister); break;
            case 0x08: _registers.ExchangeAccumulator(); RetireInstruction(); break;
            case 0xd9: _registers.ExchangeGeneral(); RetireInstruction(); break;
            case 0xeb: _registers.ExchangeDeHl(); RetireInstruction(); break;
            case 0xe3:
                _word = ActivePair;
                ReadMemory(_registers.SP, CpuZ80Flow.ExchangeLow);
                break;
            case 0x32: BeginImmediateWord(CpuZ80Operation.WriteAbsoluteByte); break;
            case 0x3a: BeginImmediateWord(CpuZ80Operation.ReadAbsoluteByte); break;
            case 0x22: _pairCode = 2; BeginImmediateWord(CpuZ80Operation.WriteAbsoluteWord); break;
            case 0x2a: _pairCode = 2; BeginImmediateWord(CpuZ80Operation.ReadAbsoluteWord); break;
            case 0xc3: BeginImmediateWord(CpuZ80Operation.JumpAbsolute); break;
            case 0xe9: _registers.PC = ActivePair; RetireInstruction(); break;
            case 0xc9: BeginPop(CpuZ80Operation.Return); break;
            case 0xd3: _operation = CpuZ80Operation.OutputImmediate; OperandRead(CpuZ80Flow.ImmediateByte); break;
            case 0xdb: _operation = CpuZ80Operation.InputImmediate; OperandRead(CpuZ80Flow.ImmediateByte); break;
            case 0xf3: _interrupts.DisableInterrupts(); RetireInstruction(); break;
            case 0xfb: _interrupts.EnableInterrupts(); RetireInstruction(); break;
            case 0xdd: case 0xfd:
                _registers.Q = 0;
                _index = opcode == 0xdd ? CpuZ80Index.IX : CpuZ80Index.IY;
                BeginFetch(prefixIndex: true);
                break;
            case 0xed: _index = CpuZ80Index.None; BeginFetch(prefixEd: true); break;
            case 0xcb:
                if (_index != CpuZ80Index.None) OperandRead(CpuZ80Flow.IndexCbDisplacement);
                else BeginFetch(prefixCb: true);
                break;
            default: Unsupported(opcode); break;
        }
    }

    private void DecodeEd(byte opcode)
    {
        if ((opcode & 0xe4) == 0xa0) { BeginBlock(opcode); return; }
        if ((opcode & 0xc7) == 0x44) { NegateAccumulator(); return; }
        if ((opcode & 0xc7) == 0x45) { BeginPop(CpuZ80Operation.ReturnNmi); return; }
        if ((opcode & 0xc7) == 0x46)
        {
            int selector = (opcode >> 3) & 3;
            _interrupts.SetMode(selector < 2 ? Z80InterruptMode.Mode0 : selector == 2 ? Z80InterruptMode.Mode1 : Z80InterruptMode.Mode2);
            RetireInstruction();
            return;
        }
        if ((opcode & 0xc7) == 0x40)
        {
            _registerCode = (byte)((opcode >> 3) & 7);
            _flow = CpuZ80Flow.IoReadRegister;
            LatchNextAddress(_registers.BC);
            _bus.BeginCycle(Z80BusCycle.IoRead, _registers.BC, 0);
            return;
        }
        if ((opcode & 0xc7) == 0x41)
        {
            _flow = CpuZ80Flow.IoWrite;
            LatchNextAddress(_registers.BC);
            byte source = (byte)((opcode >> 3) & 7);
            _bus.BeginCycle(Z80BusCycle.IoWrite, _registers.BC, 0, source == 6 ? _profile.UndocumentedOutCValue : GetRegister(source));
            return;
        }
        if ((opcode & 0xcf) == 0x4a) { BeginPairOperation(CpuZ80Operation.AddCarryPair, 7); return; }
        if ((opcode & 0xcf) == 0x42) { BeginPairOperation(CpuZ80Operation.SubtractCarryPair, 7); return; }
        if ((opcode & 0xcf) == 0x43)
        {
            _pairCode = (byte)((opcode >> 4) & 3);
            BeginImmediateWord(CpuZ80Operation.WriteAbsoluteWord);
            return;
        }
        if ((opcode & 0xcf) == 0x4b)
        {
            _pairCode = (byte)((opcode >> 4) & 3);
            BeginImmediateWord(CpuZ80Operation.ReadAbsoluteWord);
            return;
        }
        switch (opcode)
        {
            case 0x44: NegateAccumulator(); break;
            case 0x47: BeginSpecialTransfer(CpuZ80Operation.LoadIFromAccumulator); break;
            case 0x4f: BeginSpecialTransfer(CpuZ80Operation.LoadRFromAccumulator); break;
            case 0x57: BeginSpecialTransfer(CpuZ80Operation.LoadAccumulatorFromI); break;
            case 0x5f: BeginSpecialTransfer(CpuZ80Operation.LoadAccumulatorFromR); break;
            case 0x67: case 0x6f:
                _operation = opcode == 0x67 ? CpuZ80Operation.RotateDigitsRight : CpuZ80Operation.RotateDigitsLeft;
                _address = _registers.HL;
                LatchNextAddress(_address);
                ReadMemory(_address, CpuZ80Flow.ReadModifyWrite);
                break;
            case 0x45: BeginPop(CpuZ80Operation.ReturnNmi); break;
            case 0x46: _interrupts.SetMode(Z80InterruptMode.Mode0); RetireInstruction(); break;
            case 0x56: _interrupts.SetMode(Z80InterruptMode.Mode1); RetireInstruction(); break;
            case 0x5e: _interrupts.SetMode(Z80InterruptMode.Mode2); RetireInstruction(); break;
            default: RetireInstruction(); break; // ED non assigné : deux M1 réels, aucune autre opération (Young §3.4).
        }
    }

    private void BeginImmediateWord(CpuZ80Operation operation)
    {
        _operation = operation;
        OperandRead(CpuZ80Flow.ImmediateLow);
    }

    private void BeginPop(CpuZ80Operation operation)
    {
        _operation = operation;
        ReadMemory(_registers.SP, CpuZ80Flow.PopLow);
    }

    private void CompleteImmediateByte(byte data)
    {
        switch (_operation)
        {
            case CpuZ80Operation.JumpRelative:
            case CpuZ80Operation.JumpRelativeConditional:
            case CpuZ80Operation.DecrementJumpRelative:
                CompleteRelative(data);
                break;
            case CpuZ80Operation.ArithmeticImmediate:
                ExecuteArithmetic(data);
                break;
            case CpuZ80Operation.LoadImmediateByte:
                if (_registerCode == 6) WriteMemory(_registers.HL, data);
                else { SetRegister(_registerCode, data); RetireInstruction(); }
                break;
            case CpuZ80Operation.InputImmediate:
                _flow = CpuZ80Flow.IoRead;
                _registers.WZ = unchecked((ushort)((_registers.A << 8) + data + 1));
                _bus.BeginCycle(Z80BusCycle.IoRead, (ushort)((_registers.A << 8) | data), 0);
                break;
            case CpuZ80Operation.OutputImmediate:
                _flow = CpuZ80Flow.IoWrite;
                LatchByteStoreAddress(data);
                _bus.BeginCycle(Z80BusCycle.IoWrite, (ushort)((_registers.A << 8) | data), 0, _registers.A);
                break;
            default: throw new InvalidOperationException("Invalid Z80 immediate-byte operation.");
        }
    }

    private void CompleteImmediateWord()
    {
        switch (_operation)
        {
            case CpuZ80Operation.Call:
            case CpuZ80Operation.CallConditional:
                _registers.WZ = _address;
                if (_operation == CpuZ80Operation.CallConditional && !Condition((byte)((_opcode >> 3) & 7)))
                    RetireInstruction();
                else BeginStackWrite(CpuZ80Operation.Call, _registers.PC);
                break;
            case CpuZ80Operation.LoadImmediatePair:
                SetPair(_pairCode, _address); RetireInstruction();
                break;
            case CpuZ80Operation.ReadAbsoluteByte:
                LatchNextAddress(_address);
                _registerCode = 7; ReadMemory(_address, CpuZ80Flow.ReadRegister);
                break;
            case CpuZ80Operation.WriteAbsoluteByte:
                LatchByteStoreAddress(_address);
                WriteMemory(_address, _registers.A);
                break;
            case CpuZ80Operation.ReadAbsoluteWord:
                LatchNextAddress(_address);
                ReadMemory(_address, CpuZ80Flow.ReadWordLow);
                break;
            case CpuZ80Operation.WriteAbsoluteWord:
                LatchNextAddress(_address);
                _word = GetPair(_pairCode);
                WriteMemory(_address, (byte)_word, CpuZ80Flow.WriteWordLow);
                break;
            case CpuZ80Operation.JumpAbsolute:
                _registers.WZ = _address;
                _registers.PC = _address; RetireInstruction();
                break;
            case CpuZ80Operation.JumpConditional:
                _registers.WZ = _address;
                if (Condition((byte)((_opcode >> 3) & 7))) _registers.PC = _address;
                RetireInstruction();
                break;
            default: throw new InvalidOperationException("Invalid Z80 immediate-word operation.");
        }
    }

    private bool Condition(byte code) => code switch
    {
        0 => (_registers.F & F.Zero) == 0, 1 => (_registers.F & F.Zero) != 0,
        2 => (_registers.F & F.Carry) == 0, 3 => (_registers.F & F.Carry) != 0,
        4 => (_registers.F & F.ParityOverflow) == 0, 5 => (_registers.F & F.ParityOverflow) != 0,
        6 => (_registers.F & F.Sign) == 0, 7 => (_registers.F & F.Sign) != 0,
        _ => throw new ArgumentOutOfRangeException(nameof(code)),
    };

    private byte GetRegister(byte code, bool substituteIndex = true) => code switch
    {
        0 => _registers.B, 1 => _registers.C, 2 => _registers.D, 3 => _registers.E,
        4 => substituteIndex ? (byte)(ActivePair >> 8) : _registers.H,
        5 => substituteIndex ? (byte)ActivePair : _registers.L, 7 => _registers.A,
        _ => throw new ArgumentOutOfRangeException(nameof(code)),
    };

    private void SetRegister(byte code, byte value, bool substituteIndex = true)
    {
        switch (code)
        {
            case 0: _registers.B = value; break;
            case 1: _registers.C = value; break;
            case 2: _registers.D = value; break;
            case 3: _registers.E = value; break;
            case 4:
                if (substituteIndex) ActivePair = (ushort)((value << 8) | (ActivePair & 0xff));
                else _registers.H = value;
                break;
            case 5:
                if (substituteIndex) ActivePair = (ushort)((ActivePair & 0xff00) | value);
                else _registers.L = value;
                break;
            case 7: _registers.A = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(code));
        }
    }

    private ushort GetPair(byte code) => code switch
    {
        0 => _registers.BC, 1 => _registers.DE, 2 => ActivePair, 3 => _registers.SP,
        _ => throw new ArgumentOutOfRangeException(nameof(code)),
    };

    private void SetPair(byte code, ushort value, bool stack = false)
    {
        switch (code)
        {
            case 0: _registers.BC = value; break;
            case 1: _registers.DE = value; break;
            case 2: ActivePair = value; break;
            case 3: if (stack) _registers.AF = value; else _registers.SP = value; break;
            default: throw new ArgumentOutOfRangeException(nameof(code));
        }
    }
}

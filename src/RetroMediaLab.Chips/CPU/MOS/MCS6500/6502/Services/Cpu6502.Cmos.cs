using C = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Constants.Cpu6502Constants;
using Phase = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Enums.Cpu6502BusPhase;
using Mode = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Enums.Cpu6502AddressMode;
using Op = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Enums.Cpu6502Operation;

namespace RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;

/// <summary>Séquence CMOS conservée dans le CPU, y compris pendant les lectures et écritures retenues.</summary>
public partial class Cpu6502
{
    private Phase _cmosPhase;
    private CmosInstruction _cmosInstruction;
    private byte _cmosFlagsBefore;
    private byte _cmosLow;
    private byte _cmosPointer;
    private byte _cmosData;
    private byte _cmosNopRemaining;
    private ushort _cmosBase;
    private ushort _cmosAddress;
    private ushort _cmosVector;
    private bool _cmosHardwareInterrupt;

    private void StepCmos()
    {
        _cmosHeldSync = false;
        if (_samplesReset && ResetInputActive) { HoldCmosReset(); Tick(); return; }
        if (_cmosResetHeld) BeginCmosReset();
        if (_cmosPhase == Phase.Opcode)
        {
            _cmosFlagsBefore = _p;
            if (_nmiAccepted) BeginCmosInterrupt(C.NmiVector);
            else if (_irqPending) BeginCmosInterrupt(C.IrqVector);
            else _instructionAddress = _pc;
        }
        try
        {
            do { if (!ExecuteCmosPhase()) return; }
            while (_cmosPhase != Phase.Opcode);
        }
        finally { if (!_cmosHeldSync) SetCmosSync(false); }
    }

    private bool ExecuteCmosPhase()
    {
        byte value;
        switch (_cmosPhase)
        {
            case Phase.Opcode:
                if (!ReadCmosCycle(_pc, out value, sync: true)) return false;
                if (_cmosOpcodeHeld && (_nmiPrevious || _irqPrevious && (_p & InterruptDisable) == 0))
                {
                    BeginCmosInterrupt(_nmiPrevious ? C.NmiVector : C.IrqVector, opcodeRead: true);
                    break;
                }
                _cmosOpcodeHeld = _cmosHardwareInterrupt = false;
                _pc++;
                _cmosInstruction = CmosDecode.Instructions[value];
                _cmosNopRemaining = _cmosInstruction.RepeatedCycles;
                if (_cmosInstruction.Mode == Mode.NopOneCycle) CompleteCmosInstruction();
                else _cmosPhase = _cmosInstruction.Operation == Op.Brk ? Phase.BrkPadding
                    : _cmosInstruction.Mode is Mode.Implied or Mode.Accumulator ? Phase.Implied
                    : _cmosNopRemaining != 0 ? Phase.NopRepeat : Phase.OperandLow;
                break;
            case Phase.NopRepeat:
                if (!ReadCmosCycle(_pc, out value)) return false;
                if (--_cmosNopRemaining == 0) _cmosPhase = Phase.OperandLow;
                break;
            case Phase.OperandLow:
                if (!ReadCmosCycle(_pc, out value)) return false;
                _pc++;
                _cmosLow = _cmosPointer = value;
                switch (_cmosInstruction.Mode)
                {
                    case Mode.Immediate: FinishCmosRead(value); break;
                    case Mode.NopImmediate: CompleteCmosInstruction(); break;
                    case Mode.NopRepeatedOperand:
                        if (_cmosInstruction.RepeatedCycles == 5) _cmosPhase = Phase.OperandHigh;
                        else CompleteCmosInstruction();
                        break;
                    case Mode.Relative:
                        if (CmosBranchTaken) _cmosPhase = Phase.BranchTaken;
                        else CompleteCmosInstruction();
                        break;
                    case Mode.ZeroPage: _cmosAddress = _cmosBase = value; BeginCmosData(); break;
                    case Mode.ZeroPageX or Mode.ZeroPageY or Mode.IndexedIndirect: _cmosPhase = Phase.ZeroIndex; break;
                    case Mode.IndirectIndexed or Mode.ZeroPageIndirect: _cmosPhase = Phase.PointerLow; break;
                    default: _cmosPhase = _cmosInstruction.Operation == Op.Jsr ? Phase.JsrDummy : Phase.OperandHigh; break;
                }
                break;
            case Phase.OperandHigh:
                if (!ReadCmosCycle(_pc, out value)) return false;
                _pc++;
                _cmosBase = (ushort)(_cmosLow | value << C.HighByteShift);
                if (_cmosInstruction.Mode == Mode.NopRepeatedOperand) CompleteCmosInstruction();
                else if (_cmosInstruction.Operation == Op.Jsr || _cmosInstruction.Operation == Op.Jmp && _cmosInstruction.Mode == Mode.Absolute)
                { _pc = _cmosBase; CompleteCmosInstruction(); }
                else if (_cmosInstruction.Mode == Mode.Indirect) _cmosPhase = Phase.JumpIndirectLow;
                else if (_cmosInstruction.Mode == Mode.AbsoluteIndexedIndirect) _cmosPhase = Phase.JumpIndexed;
                else FinishCmosAddress();
                break;
            case Phase.ZeroIndex:
                if (!ReadCmosCycle(_cmosPointer, out value)) return false;
                _cmosPointer = (byte)(_cmosPointer + CmosIndex);
                if (_cmosInstruction.Mode == Mode.IndexedIndirect) _cmosPhase = Phase.PointerLow;
                else { _cmosAddress = _cmosBase = _cmosPointer; BeginCmosData(); }
                break;
            case Phase.PointerLow:
                if (!ReadCmosCycle(_cmosPointer, out value)) return false;
                _cmosLow = value; _cmosPhase = Phase.PointerHigh; break;
            case Phase.PointerHigh:
                if (!ReadCmosCycle((byte)(_cmosPointer + 1), out value)) return false;
                _cmosBase = (ushort)(_cmosLow | value << C.HighByteShift); FinishCmosAddress(); break;
            case Phase.IndexedRead:
                var crossing = (_cmosBase & C.PageMask) != (_cmosAddress & C.PageMask);
                var correctionAddress = crossing || _cmosInstruction.Mode == Mode.IndirectIndexed
                    ? (ushort)(_pc - 1) : _cmosAddress;
                if (!ReadCmosCycle(correctionAddress, out value)) return false;
                BeginCmosData(); break;
            case Phase.Data:
                if (_cmosInstruction.Stores)
                {
                    if (!WriteCmosCycle(_cmosAddress, _cmosData)) return false;
                    CompleteCmosInstruction();
                }
                else
                {
                    if (!ReadCmosCycle(_cmosAddress, out value)) return false;
                    if (_cmosInstruction.Modifies) { _cmosData = value; _cmosPhase = Phase.RmwOriginal; }
                    else FinishCmosRead(value);
                }
                break;
            case Phase.RmwOriginal:
                SetMemoryLock(true);
                if (!ReadCmosCycle(_cmosAddress, out value)) return false;
                _cmosData = ModifyCmosValue(_cmosData); _cmosPhase = Phase.RmwResult; break;
            case Phase.RmwResult:
                if (!WriteCmosCycle(_cmosAddress, _cmosData)) return false;
                SetMemoryLock(false);
                CompleteCmosInstruction(); break;
            case Phase.DecimalAdjust:
                if (!ReadCmosCycle(_pc, out value)) return false;
                ApplyCmosRead(_cmosData); CompleteCmosInstruction(); break;
            case Phase.Implied:
                if (!ReadCmosCycle(_pc, out value)) return false;
                switch (_cmosInstruction.Operation)
                {
                    case Op.Pha or Op.Php or Op.Phx or Op.Phy: _cmosPhase = Phase.StackPush; break;
                    case Op.Pla or Op.Plp or Op.Plx or Op.Ply or Op.Rts or Op.Rti: _cmosPhase = Phase.StackDummy; break;
                    default: ApplyCmosImplied(); CompleteCmosInstruction(); break;
                }
                break;
            case Phase.BranchTaken:
                if (!ReadCmosCycle(_pc, out value)) return false;
                _cmosBase = _pc;
                _pc = (ushort)(_pc + (sbyte)_cmosLow);
                if ((_pc & C.PageMask) == (_cmosBase & C.PageMask)) CompleteCmosInstruction();
                else _cmosPhase = Phase.BranchCross;
                break;
            case Phase.BranchCross:
                if (!ReadCmosCycle((ushort)((_cmosBase & C.PageMask) | (byte)_pc), out value)) return false;
                CompleteCmosInstruction(); break;
            case Phase.StackPush:
                var pushed = _cmosInstruction.Operation switch { Op.Pha => _a, Op.Phx => _x, Op.Phy => _y, _ => (byte)(_p | Break | Unused) };
                if (!WriteCmosCycle((ushort)(C.StackBase | _s), pushed)) return false;
                _s--; CompleteCmosInstruction(); break;
            case Phase.StackDummy:
                if (!ReadCmosCycle((ushort)(C.StackBase | _s), out value)) return false;
                _s++;
                _cmosPhase = _cmosInstruction.Operation switch { Op.Rti => Phase.PullStatus, Op.Rts => Phase.PullLow, _ => Phase.StackPull };
                break;
            case Phase.StackPull:
                if (!ReadCmosCycle((ushort)(C.StackBase | _s), out value)) return false;
                switch (_cmosInstruction.Operation)
                {
                    case Op.Pla: _a = Nz(value); break;
                    case Op.Plx: _x = Nz(value); break;
                    case Op.Ply: _y = Nz(value); break;
                    default: _p = (byte)((value & ~Break) | Unused); break;
                }
                CompleteCmosInstruction(); break;
            case Phase.PullStatus:
                if (!ReadCmosCycle((ushort)(C.StackBase | _s), out value)) return false;
                _p = (byte)((value & ~Break) | Unused); _s++; _cmosPhase = Phase.PullLow; break;
            case Phase.PullLow:
                if (!ReadCmosCycle((ushort)(C.StackBase | _s), out value)) return false;
                _cmosLow = value; _s++; _cmosPhase = Phase.PullHigh; break;
            case Phase.PullHigh:
                if (!ReadCmosCycle((ushort)(C.StackBase | _s), out value)) return false;
                _cmosAddress = (ushort)(_cmosLow | value << C.HighByteShift);
                if (_cmosInstruction.Operation == Op.Rts) _cmosPhase = Phase.ReturnRead;
                else { _pc = _cmosAddress; CompleteCmosInstruction(); }
                break;
            case Phase.ReturnRead:
                if (!ReadCmosCycle(_cmosAddress, out value)) return false;
                _pc = (ushort)(_cmosAddress + 1); CompleteCmosInstruction(); break;
            case Phase.JsrDummy:
                if (!ReadCmosCycle((ushort)(C.StackBase | _s), out value)) return false;
                _cmosPhase = Phase.PushHigh; break;
            case Phase.PushHigh:
                if (!WriteCmosCycle((ushort)(C.StackBase | _s), (byte)(_pc >> C.HighByteShift))) return false;
                _s--; _cmosPhase = Phase.PushLow; break;
            case Phase.PushLow:
                if (!WriteCmosCycle((ushort)(C.StackBase | _s), (byte)_pc)) return false;
                _s--;
                _cmosPhase = !_cmosHardwareInterrupt && _cmosInstruction.Operation == Op.Jsr ? Phase.OperandHigh : Phase.PushStatus;
                break;
            case Phase.BrkPadding:
                if (!ReadCmosCycle(_pc, out value)) return false;
                _pc++; _cmosVector = C.IrqVector; _cmosPhase = Phase.PushHigh; break;
            case Phase.InterruptOpcode:
                if (!ReadCmosCycle(_pc, out value, sync: true)) return false;
                _cmosPhase = Phase.InterruptDummy; break;
            case Phase.InterruptDummy:
                if (!ReadCmosCycle(_pc, out value)) return false;
                _cmosPhase = Phase.PushHigh; break;
            case Phase.PushStatus:
                if (!WriteCmosCycle((ushort)(C.StackBase | _s), (byte)(_cmosHardwareInterrupt ? (_p | Unused) & ~Break : _p | Unused | Break))) return false;
                if (_cmosHardwareInterrupt && _cmosVector == C.IrqVector && _nmiPending)
                {
                    _cmosVector = C.NmiVector;
                    _nmiPending = _nmiAccepted = false;
                }
                _s--; _p = (byte)((_p | InterruptDisable) & ~Decimal); _cmosPhase = Phase.VectorLow; break;
            case Phase.VectorLow:
                if (!ReadCmosCycle(_cmosVector, out value)) return false;
                _cmosLow = value; _cmosPhase = Phase.VectorHigh; break;
            case Phase.VectorHigh:
                if (!ReadCmosCycle((ushort)(_cmosVector + 1), out value)) return false;
                _pc = (ushort)(_cmosLow | value << C.HighByteShift); CompleteCmosInstruction(); break;
            case Phase.JumpIndexed:
                if (!ReadCmosCycle((ushort)(_pc - 2), out value)) return false;
                _cmosBase = (ushort)(_cmosBase + _x); _cmosPhase = Phase.JumpIndirectLow; break;
            case Phase.JumpIndirectLow:
                if (!ReadCmosCycle(_cmosBase, out value)) return false;
                _cmosLow = value;
                _cmosPhase = _cmosInstruction.Mode == Mode.Indirect ? Phase.JumpIndirectHigh : Phase.JumpIndirectCarry;
                break;
            case Phase.JumpIndirectHigh:
                if (!ReadCmosCycle((ushort)((_cmosBase & C.PageMask) | (byte)(_cmosBase + 1)), out value)) return false;
                _cmosPhase = Phase.JumpIndirectCarry; break;
            case Phase.JumpIndirectCarry:
                if (!ReadCmosCycle((ushort)(_cmosBase + 1), out value)) return false;
                _pc = (ushort)(_cmosLow | value << C.HighByteShift); CompleteCmosInstruction(); break;
            case Phase.ResetFirst:
                if (!ReadCmosCycle(_pc, out value)) return false;
                _cmosPhase = Phase.ResetStackFirst; break;
            case Phase.ResetStackFirst:
                if (!WriteCmosCycle((ushort)(C.StackBase | _s), (byte)(_pc >> C.HighByteShift))) return false;
                _s--; _cmosPhase = Phase.ResetStackSecond; break;
            case Phase.ResetStackSecond:
                if (!WriteCmosCycle((ushort)(C.StackBase | _s), (byte)_pc)) return false;
                _s--; _cmosPhase = Phase.ResetStackThird; break;
            case Phase.ResetStackThird:
                if (!WriteCmosCycle((ushort)(C.StackBase | _s), (byte)((_p | Unused) & ~Break))) return false;
                _s--; _p = (byte)((_p | InterruptDisable) & ~Decimal); _cmosPhase = Phase.ResetVectorLow; break;
            case Phase.ResetVectorLow:
                if (!ReadCmosCycle(C.ResetVector, out value)) return false;
                _cmosLow = value; _cmosPhase = Phase.ResetVectorHigh; break;
            case Phase.ResetVectorHigh:
                if (!ReadCmosCycle((ushort)(C.ResetVector + 1), out value)) return false;
                _pc = (ushort)(_cmosLow | value << C.HighByteShift);
                _irqPending = false; _nmiAccepted = _nmiPrevious; _cmosPhase = Phase.Opcode; break;
            default: throw new InvalidOperationException(nameof(_cmosPhase));
        }
        return true;
    }
}

using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Enums;
using C = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Constants.Cpu6502Constants;
using Phase = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Enums.Cpu6502BusPhase;
using Mode = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Enums.Cpu6502AddressMode;
using Op = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Enums.Cpu6502Operation;

namespace RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;

/// <summary>Séquenceur NMOS sans allocation : une phase conservée par cycle de bus, même pendant RDY.</summary>
public sealed partial class Cpu6502
{
    private Phase _nmosPhase;
    private NmosInstruction _nmosInstruction;
    private byte _nmosFlagsBefore;
    private byte _nmosLow;
    private byte _nmosPointer;
    private byte _nmosData;
    private ushort _nmosAddress;
    private ushort _nmosBase;
    private ushort _nmosVector;
    private bool _nmosHardwareInterrupt;
    private bool _nmosSync;
    private bool _nmosOpcodeHeld;
    private bool _nmosResetHeld;

    public bool AtInstructionBoundary => cmos || _nmosPhase == Phase.Opcode && !_nmosOpcodeHeld;

    private bool _nmosHeldSync;

    private void SetNmosSync(bool asserted)
    {
        if (_nmosSync == asserted) return;
        _nmosSync = asserted;
        if (_publishesSync) bus.SetSync(asserted);
    }

    private void ResetNmosSequence()
    {
        _nmosPhase = Phase.Opcode;
        _nmosOpcodeHeld = false;
        _nmosHardwareInterrupt = false;
        _nmosResetHeld = false;
        SetNmosSync(false);
    }

    private void BeginNmosReset()
    {
        ResetNmosSequence();
        _nmiLevel = bus.Nmi;
        _overflowLevel = _samplesOverflow && bus.SetOverflow;
        _nmiPending = _nmiPrevious = _nmiAccepted = false;
        _irqPending = _irqPrevious = _irqLevel = false;
        _branchIrqSample = _branchNmiSample = null;
        _p |= Unused;
        _nmosPhase = Phase.ResetFirst;
    }

    private void HoldNmosReset()
    {
        _nmosResetHeld = true;
        _nmosOpcodeHeld = false;
        _nmosPhase = Phase.ResetFirst;
        SetNmosSync(false);
    }

    private void StepNmos()
    {
        _nmosHeldSync = false;
        if (_samplesReset && bus.ResetRequested)
        {
            HoldNmosReset();
            Tick();
            return;
        }
        if (_nmosResetHeld) BeginNmosReset();
        if (_nmosPhase == Phase.Opcode)
        {
            _nmosFlagsBefore = _p;
            if (_nmiAccepted) BeginNmosInterrupt(C.NmiVector);
            else if (_irqPending) BeginNmosInterrupt(C.IrqVector);
            else _instructionAddress = _pc;
        }
        try
        {
            do
            {
                if (!ExecuteNmosPhase()) return;
                if (_nmosPhase == Phase.Jammed) return;
            }
            while (_nmosPhase != Phase.Opcode);
        }
        finally
        {
            if (!_nmosHeldSync) SetNmosSync(false);
        }
    }

    private bool ReadNmosCycle(ushort address, out byte value, bool sync = false)
    {
        value = default;
        SetNmosSync(sync);
        Tick();
        if (_samplesReset && bus.ResetRequested) { HoldNmosReset(); return false; }
        value = bus.Read(address);
        if (!_samplesReady || bus.Ready) return true;
        _nmosOpcodeHeld |= _nmosPhase == Phase.Opcode;
        _nmosHeldSync = sync;
        return false;
    }

    private bool WriteNmosCycle(ushort address, byte value)
    {
        SetNmosSync(false);
        Tick();
        if (_samplesReset && bus.ResetRequested) { HoldNmosReset(); return false; }
        // Les écritures NMOS ne sont jamais retenues par RDY.
        if (!_samplesDataEnable || bus.DataBusEnabled) bus.Write(address, value);
        else bus.WriteUndriven(address);
        return true;
    }

    private bool ExecuteNmosPhase()
    {
        byte value = default;
        switch (_nmosPhase)
        {
            case Phase.Opcode:
                if (!ReadNmosCycle(_pc, out value, sync: true)) return false;
                // Une attente sur SYNC laisse les entrées se stabiliser avant d'accepter l'opcode.
                if (_nmosOpcodeHeld && (_nmiPrevious || _irqPrevious && (_p & InterruptDisable) == 0))
                {
                    BeginNmosInterrupt(_nmiPrevious ? C.NmiVector : C.IrqVector, opcodeRead: true);
                    return true;
                }
                _nmosOpcodeHeld = false;
                _pc++;
                _nmosInstruction = NmosInstructions[value];
                _nmosHardwareInterrupt = false;
                _branchIrqSample = _branchNmiSample = null;
                _nmosPhase = _nmosInstruction.Operation switch
                {
                    Op.Brk => Phase.BrkPadding,
                    Op.Jam => Phase.JamOperand,
                    _ => _nmosInstruction.Mode is Mode.Implied or Mode.Accumulator ? Phase.Implied : Phase.OperandLow
                };
                break;
            case Phase.OperandLow:
                if (!ReadNmosCycle(_pc, out value)) return false;

                _pc++;
                _nmosLow = _nmosPointer = value;
                switch (_nmosInstruction.Mode)
                {
                    case Mode.Immediate: ApplyNmosRead(value); CompleteNmosInstruction(); break;
                    case Mode.Relative:
                        if (!NmosBranchTaken) CompleteNmosInstruction();
                        else
                        {
                            _branchIrqSample = _irqPrevious && (_p & InterruptDisable) == 0;
                            _branchNmiSample = _nmiPrevious;
                            _nmosPhase = Phase.BranchTaken;
                        }
                        break;
                    case Mode.ZeroPage: _nmosBase = _nmosAddress = value; BeginNmosData(); break;
                    case Mode.ZeroPageX or Mode.ZeroPageY or Mode.IndexedIndirect: _nmosPhase = Phase.ZeroIndex; break;
                    case Mode.IndirectIndexed: _nmosPhase = Phase.PointerLow; break;
                    default: _nmosPhase = _nmosInstruction.Operation == Op.Jsr ? Phase.JsrDummy : Phase.OperandHigh; break;
                }
                break;
            case Phase.OperandHigh:
                if (!ReadNmosCycle(_pc, out value)) return false;

                _pc++;
                _nmosBase = (ushort)(_nmosLow | value << C.HighByteShift);
                if (_nmosInstruction.Operation == Op.Jsr || _nmosInstruction.Operation == Op.Jmp && _nmosInstruction.Mode == Mode.Absolute)
                {
                    _pc = _nmosBase;
                    CompleteNmosInstruction();
                }
                else if (_nmosInstruction.Mode == Mode.Indirect) _nmosPhase = Phase.JumpIndirectLow;
                else FinishNmosAddress();
                break;
            case Phase.ZeroIndex:
                if (!ReadNmosCycle(_nmosPointer, out value)) return false;

                _nmosPointer = (byte)(_nmosPointer + NmosIndex);
                if (_nmosInstruction.Mode == Mode.IndexedIndirect) _nmosPhase = Phase.PointerLow;
                else { _nmosAddress = _nmosBase = _nmosPointer; BeginNmosData(); }
                break;
            case Phase.PointerLow:
                if (!ReadNmosCycle(_nmosPointer, out value)) return false;
                _nmosLow = value; _nmosPhase = Phase.PointerHigh; break;
            case Phase.PointerHigh:
                if (!ReadNmosCycle((byte)(_nmosPointer + 1), out value)) return false;
                _nmosBase = (ushort)(_nmosLow | value << C.HighByteShift); FinishNmosAddress(); break;
            case Phase.IndexedRead:
                if (!ReadNmosCycle((ushort)((_nmosBase & C.PageMask) | (byte)_nmosAddress), out value)) return false;
                BeginNmosData(); break;
            case Phase.Data:
                if (NmosStores)
                {
                    if (!WriteNmosCycle(_nmosAddress, _nmosData)) return false;
                }
                else if (!ReadNmosCycle(_nmosAddress, out value)) return false;

                if (NmosStores) CompleteNmosInstruction();
                else if (NmosModifies) { _nmosData = value; _nmosPhase = Phase.RmwOriginal; }
                else { ApplyNmosRead(value); CompleteNmosInstruction(); }
                break;
            case Phase.RmwOriginal:
                if (!WriteNmosCycle(_nmosAddress, _nmosData)) return false;
                _nmosData = ModifyNmosValue(_nmosData); _nmosPhase = Phase.RmwResult; break;
            case Phase.RmwResult:
                if (!WriteNmosCycle(_nmosAddress, _nmosData)) return false;
                ApplyNmosModified(_nmosData); CompleteNmosInstruction(); break;
            case Phase.Implied:
                if (!ReadNmosCycle(_pc, out value)) return false;

                switch (_nmosInstruction.Operation)
                {
                    case Op.Pha or Op.Php: _nmosPhase = Phase.StackPush; break;
                    case Op.Pla or Op.Plp or Op.Rts or Op.Rti: _nmosPhase = Phase.StackDummy; break;
                    default: ApplyNmosImplied(); CompleteNmosInstruction(); break;
                }
                break;
            case Phase.BranchTaken:
                if (!ReadNmosCycle(_pc, out value)) return false;

                _nmosBase = _pc;
                _pc = (ushort)(_pc + (sbyte)_nmosLow);
                if ((_pc & C.PageMask) == (_nmosBase & C.PageMask)) CompleteNmosInstruction();
                else _nmosPhase = Phase.BranchCross;
                break;
            case Phase.BranchCross:
                if (!ReadNmosCycle((ushort)((_nmosBase & C.PageMask) | (byte)_pc), out value)) return false;

                _branchIrqSample |= _irqPrevious && (_p & InterruptDisable) == 0;
                _branchNmiSample |= _nmiPrevious;
                CompleteNmosInstruction();
                break;
            case Phase.StackPush:
                if (!WriteNmosCycle((ushort)(C.StackBase | _s), _nmosInstruction.Operation == Op.Pha ? _a : (byte)(_p | Break | Unused))) return false;
                _s--; CompleteNmosInstruction(); break;
            case Phase.StackDummy:
                if (!ReadNmosCycle((ushort)(C.StackBase | _s), out value)) return false;

                _s++;
                _nmosPhase = _nmosInstruction.Operation switch { Op.Rti => Phase.PullStatus, Op.Rts => Phase.PullLow, _ => Phase.StackPull };
                break;
            case Phase.StackPull:
                if (!ReadNmosCycle((ushort)(C.StackBase | _s), out value)) return false;

                if (_nmosInstruction.Operation == Op.Pla) _a = Nz(value);
                else _p = (byte)((value & ~Break) | Unused);
                CompleteNmosInstruction();
                break;
            case Phase.PullStatus:
                if (!ReadNmosCycle((ushort)(C.StackBase | _s), out value)) return false;
                _p = (byte)((value & ~Break) | Unused); _s++; _nmosPhase = Phase.PullLow; break;
            case Phase.PullLow:
                if (!ReadNmosCycle((ushort)(C.StackBase | _s), out value)) return false;
                _nmosLow = value; _s++; _nmosPhase = Phase.PullHigh; break;
            case Phase.PullHigh:
                if (!ReadNmosCycle((ushort)(C.StackBase | _s), out value)) return false;

                _nmosAddress = (ushort)(_nmosLow | value << C.HighByteShift);
                if (_nmosInstruction.Operation == Op.Rts) _nmosPhase = Phase.ReturnRead;
                else { _pc = _nmosAddress; CompleteNmosInstruction(); }
                break;
            case Phase.ReturnRead:
                if (!ReadNmosCycle(_nmosAddress, out value)) return false;
                _pc = (ushort)(_nmosAddress + 1); CompleteNmosInstruction(); break;
            case Phase.JsrDummy:
                if (!ReadNmosCycle((ushort)(C.StackBase | _s), out value)) return false;
                _nmosPhase = Phase.PushHigh; break;
            case Phase.PushHigh:
                if (!WriteNmosCycle((ushort)(C.StackBase | _s), (byte)(_pc >> C.HighByteShift))) return false;
                _s--; _nmosPhase = Phase.PushLow; break;
            case Phase.PushLow:
                if (!WriteNmosCycle((ushort)(C.StackBase | _s), (byte)_pc)) return false;

                _s--;
                _nmosPhase = !_nmosHardwareInterrupt && _nmosInstruction.Operation == Op.Jsr ? Phase.OperandHigh : Phase.PushStatus;
                break;
            case Phase.BrkPadding:
                if (!ReadNmosCycle(_pc, out value)) return false;
                _pc++; _nmosVector = C.IrqVector; _nmosPhase = Phase.PushHigh; break;
            case Phase.InterruptOpcode:
                if (!ReadNmosCycle(_pc, out value, sync: true)) return false;
                _nmosPhase = Phase.InterruptDummy; break;
            case Phase.InterruptDummy:
                if (!ReadNmosCycle(_pc, out value)) return false;
                _nmosPhase = Phase.PushHigh; break;
            case Phase.PushStatus:
                if (!WriteNmosCycle((ushort)(C.StackBase | _s), (byte)(_nmosHardwareInterrupt ? (_p | Unused) & ~Break : _p | Unused | Break))) return false;

                _s--;
                _p |= InterruptDisable;
                _nmosVector = InterruptVector(_nmosVector, _nmosHardwareInterrupt);
                _nmosPhase = Phase.VectorLow;
                break;
            case Phase.VectorLow:
                if (!ReadNmosCycle(_nmosVector, out value)) return false;
                _nmosLow = value; _nmosPhase = Phase.VectorHigh; break;
            case Phase.VectorHigh:
                if (!ReadNmosCycle((ushort)(_nmosVector + 1), out value)) return false;
                _pc = (ushort)(_nmosLow | value << C.HighByteShift); CompleteNmosInstruction(); break;
            case Phase.JumpIndirectLow:
                if (!ReadNmosCycle(_nmosBase, out value)) return false;
                _nmosLow = value; _nmosPhase = Phase.JumpIndirectHigh; break;
            case Phase.JumpIndirectHigh:
                if (!ReadNmosCycle((ushort)((_nmosBase & C.PageMask) | (byte)(_nmosBase + 1)), out value)) return false;
                _pc = (ushort)(_nmosLow | value << C.HighByteShift); CompleteNmosInstruction(); break;
            case Phase.JamOperand:
                if (!ReadNmosCycle(_pc, out value)) return false;
                _nmosPhase = Phase.JamHigh; break;
            case Phase.JamHigh:
                if (!ReadNmosCycle(C.JamBusAddress, out value)) return false;
                _nmosPhase = Phase.JamLowFirst; break;
            case Phase.JamLowFirst:
                if (!ReadNmosCycle(C.IrqVector, out value)) return false;
                _nmosPhase = Phase.JamLowSecond; break;
            case Phase.JamLowSecond:
                if (!ReadNmosCycle(C.IrqVector, out value)) return false;
                CompleteNmosInstruction(Phase.Jammed); break;
            case Phase.Jammed:
                if (!ReadNmosCycle(C.JamBusAddress, out value)) return false;
                break;
            case Phase.ResetFirst:
                if (!ReadNmosCycle(_pc, out value)) return false;
                _nmosPhase = Phase.ResetSecond; break;
            case Phase.ResetSecond:
                if (!ReadNmosCycle(_pc, out value)) return false;
                _nmosPhase = Phase.ResetStackFirst; break;
            case Phase.ResetStackFirst:
                if (!ReadNmosCycle((ushort)(C.StackBase | _s), out value)) return false;
                _s--; _nmosPhase = Phase.ResetStackSecond; break;
            case Phase.ResetStackSecond:
                if (!ReadNmosCycle((ushort)(C.StackBase | _s), out value)) return false;
                _s--; _nmosPhase = Phase.ResetStackThird; break;
            case Phase.ResetStackThird:
                if (!ReadNmosCycle((ushort)(C.StackBase | _s), out value)) return false;
                _s--; _p |= InterruptDisable; _nmosPhase = Phase.ResetVectorLow; break;
            case Phase.ResetVectorLow:
                if (!ReadNmosCycle(C.ResetVector, out value)) return false;
                _nmosLow = value; _nmosPhase = Phase.ResetVectorHigh; break;
            case Phase.ResetVectorHigh:
                if (!ReadNmosCycle((ushort)(C.ResetVector + 1), out value)) return false;

                _pc = (ushort)(_nmosLow | value << C.HighByteShift);
                _irqPending = false;
                _nmiAccepted = _nmiPrevious;
                _nmosPhase = Phase.Opcode;
                break;
            default: throw new InvalidOperationException(nameof(_nmosPhase));
        }
        return true;
    }

    private void BeginNmosInterrupt(ushort vector, bool opcodeRead = false)
    {
        if (vector == C.NmiVector) _nmiPending = _nmiAccepted = false;
        _irqPending = false;
        _nmosHardwareInterrupt = true;
        _nmosVector = vector;
        _nmosOpcodeHeld = false;
        _branchIrqSample = _branchNmiSample = null;
        _nmosPhase = opcodeRead ? Phase.InterruptDummy : Phase.InterruptOpcode;
    }

    private void CompleteNmosInstruction(Phase next = Phase.Opcode)
    {
        _p |= Unused;
        var flags = !_nmosHardwareInterrupt && _nmosInstruction.Operation is Op.Cli or Op.Sei or Op.Plp ? _nmosFlagsBefore : _p;
        _irqPending = _branchIrqSample ?? (_irqPrevious && (flags & InterruptDisable) == 0);
        _nmiAccepted = _branchNmiSample ?? _nmiPrevious;
        _nmosPhase = next;
    }

    private byte NmosIndex => _nmosInstruction.Mode switch
    {
        Mode.ZeroPageX or Mode.AbsoluteX or Mode.IndexedIndirect => _x,
        Mode.ZeroPageY or Mode.AbsoluteY or Mode.IndirectIndexed => _y,
        _ => 0
    };

    private bool NmosStores => _nmosInstruction.Stores;
    private bool NmosModifies => _nmosInstruction.Modifies;

    private void FinishNmosAddress()
    {
        _nmosAddress = (ushort)(_nmosBase + (_nmosInstruction.Mode == Mode.IndexedIndirect ? 0 : NmosIndex));
        if (_nmosInstruction.Mode is Mode.AbsoluteX or Mode.AbsoluteY or Mode.IndirectIndexed &&
            (NmosStores || NmosModifies || (_nmosAddress & C.PageMask) != (_nmosBase & C.PageMask)))
            _nmosPhase = Phase.IndexedRead;
        else BeginNmosData();
    }

    private void BeginNmosData()
    {
        if (NmosStores)
        {
            if (_nmosInstruction.Operation == Op.Tas) _s = (byte)(_a & _x);
            _nmosData = _nmosInstruction.Operation switch
            {
                Op.Sta => _a,
                Op.Stx => _x,
                Op.Sty => _y,
                Op.Sax => (byte)(_a & _x),
                Op.Shx => (byte)(_x & ((_nmosBase >> C.HighByteShift) + 1)),
                Op.Shy => (byte)(_y & ((_nmosBase >> C.HighByteShift) + 1)),
                _ => (byte)(_a & _x & ((_nmosBase >> C.HighByteShift) + 1))
            };
            if (_nmosInstruction.Operation is Op.Shx or Op.Shy or Op.Ahx or Op.Tas &&
                (_nmosAddress & C.PageMask) != (_nmosBase & C.PageMask))
                _nmosAddress = (ushort)((_nmosData << C.HighByteShift) | (byte)_nmosAddress);
        }
        _nmosPhase = Phase.Data;
    }
}

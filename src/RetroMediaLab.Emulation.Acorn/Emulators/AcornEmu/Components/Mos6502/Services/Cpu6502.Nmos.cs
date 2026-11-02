using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6502.Enums;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6502.Constants.Cpu6502Constants;
using Phase = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6502.Enums.Cpu6502BusPhase;
using Mode = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6502.Enums.Cpu6502AddressMode;
using Op = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6502.Enums.Cpu6502Operation;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6502.Services;

/// <summary>Séquenceur NMOS sans allocation : une phase conservée par cycle de bus, même pendant RDY.</summary>
internal sealed partial class Cpu6502
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

    internal bool AtInstructionBoundary => cmos || _nmosPhase == Phase.Opcode && !_nmosOpcodeHeld;

    private readonly record struct NmosBusCycle(ushort Address, byte Value = 0, bool Write = false, bool Sync = false);

    private void SetNmosSync(bool asserted)
    {
        if (_nmosSync == asserted) return;
        _nmosSync = asserted;
        bus.SetSync(asserted);
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
        _overflowLevel = bus.SetOverflow;
        _nmiPending = _nmiPrevious = _nmiAccepted = false;
        _irqPending = _irqPrevious = _irqLevel = false;
        _branchIrqSample = _branchNmiSample = null;
        P |= Unused;
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
        if (bus.ResetRequested)
        {
            HoldNmosReset();
            Tick();
            return;
        }
        if (_nmosResetHeld) BeginNmosReset();
        if (_nmosPhase == Phase.Opcode)
        {
            _nmosFlagsBefore = P;
            if (_nmiAccepted) BeginNmosInterrupt(C.NmiVector);
            else if (_irqPending) BeginNmosInterrupt(C.IrqVector);
            else InstructionAddress = PC;
        }
        var holdSync = false;
        try
        {
            do
            {
                var cycle = SelectNmosCycle();
                SetNmosSync(cycle.Sync);
                Tick();
                if (bus.ResetRequested) { HoldNmosReset(); return; }
                byte value;
                if (cycle.Write)
                {
                    // RDY n'arrête pas une écriture NMOS, y compris les deux écritures d'un RMW.
                    if (bus.DataBusEnabled) bus.Write(cycle.Address, cycle.Value);
                    else bus.WriteUndriven(cycle.Address);
                    value = cycle.Value;
                }
                else
                {
                    value = bus.Read(cycle.Address);
                    if (!bus.Ready)
                    {
                        _nmosOpcodeHeld |= _nmosPhase == Phase.Opcode;
                        holdSync = cycle.Sync;
                        return;
                    }
                }
                AdvanceNmosPhase(value);
                if (_nmosPhase == Phase.Jammed) return;
            }
            while (_nmosPhase != Phase.Opcode);
        }
        finally
        {
            if (!holdSync) SetNmosSync(false);
        }
    }

    private NmosBusCycle SelectNmosCycle() => _nmosPhase switch
    {
        Phase.Opcode or Phase.InterruptOpcode => new(PC, Sync: true),
        Phase.OperandLow or Phase.OperandHigh or Phase.Implied or Phase.BranchTaken or
            Phase.InterruptDummy or Phase.BrkPadding or Phase.JamOperand or Phase.ResetFirst or Phase.ResetSecond => new(PC),
        Phase.ZeroIndex => new(_nmosPointer),
        Phase.PointerLow => new(_nmosPointer),
        Phase.PointerHigh => new((byte)(_nmosPointer + 1)),
        Phase.IndexedRead => new((ushort)((_nmosBase & C.PageMask) | (byte)_nmosAddress)),
        Phase.Data => new(_nmosAddress, _nmosData, NmosStores),
        Phase.RmwOriginal or Phase.RmwResult => new(_nmosAddress, _nmosData, Write: true),
        Phase.BranchCross => new((ushort)((_nmosBase & C.PageMask) | (byte)PC)),
        Phase.StackDummy or Phase.JsrDummy or Phase.ResetStackFirst or Phase.ResetStackSecond or Phase.ResetStackThird => new((ushort)(C.StackBase | S)),
        Phase.StackPush => new((ushort)(C.StackBase | S),
            _nmosInstruction.Operation == Op.Pha ? A : (byte)(P | Break | Unused), Write: true),
        Phase.StackPull or Phase.PullStatus or Phase.PullLow or Phase.PullHigh => new((ushort)(C.StackBase | S)),
        Phase.ReturnRead => new(_nmosAddress),
        Phase.PushHigh => new((ushort)(C.StackBase | S), (byte)(PC >> C.HighByteShift), Write: true),
        Phase.PushLow => new((ushort)(C.StackBase | S), (byte)PC, Write: true),
        Phase.PushStatus => new((ushort)(C.StackBase | S),
            (byte)(_nmosHardwareInterrupt ? (P | Unused) & ~Break : P | Unused | Break), Write: true),
        Phase.JumpIndirectLow => new(_nmosBase),
        Phase.JumpIndirectHigh => new((ushort)((_nmosBase & C.PageMask) | (byte)(_nmosBase + 1))),
        Phase.VectorLow => new(_nmosVector),
        Phase.VectorHigh => new((ushort)(_nmosVector + 1)),
        Phase.JamHigh or Phase.Jammed => new(C.JamBusAddress),
        Phase.JamLowFirst or Phase.JamLowSecond => new(C.IrqVector),
        Phase.ResetVectorLow => new(C.ResetVector),
        Phase.ResetVectorHigh => new((ushort)(C.ResetVector + 1)),
        _ => throw new InvalidOperationException(nameof(_nmosPhase))
    };

    private void AdvanceNmosPhase(byte value)
    {
        switch (_nmosPhase)
        {
            case Phase.Opcode:
                // Une attente sur SYNC laisse les entrées se stabiliser avant d'accepter l'opcode.
                if (_nmosOpcodeHeld && (_nmiPrevious || _irqPrevious && (P & InterruptDisable) == 0))
                {
                    BeginNmosInterrupt(_nmiPrevious ? C.NmiVector : C.IrqVector, opcodeRead: true);
                    return;
                }
                _nmosOpcodeHeld = false;
                PC++;
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
                PC++;
                _nmosLow = _nmosPointer = value;
                switch (_nmosInstruction.Mode)
                {
                    case Mode.Immediate: ApplyNmosRead(value); CompleteNmosInstruction(); break;
                    case Mode.Relative:
                        if (!NmosBranchTaken) CompleteNmosInstruction();
                        else
                        {
                            _branchIrqSample = _irqPrevious && (P & InterruptDisable) == 0;
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
                PC++;
                _nmosBase = (ushort)(_nmosLow | value << C.HighByteShift);
                if (_nmosInstruction.Operation == Op.Jsr || _nmosInstruction.Operation == Op.Jmp && _nmosInstruction.Mode == Mode.Absolute)
                {
                    PC = _nmosBase;
                    CompleteNmosInstruction();
                }
                else if (_nmosInstruction.Mode == Mode.Indirect) _nmosPhase = Phase.JumpIndirectLow;
                else FinishNmosAddress();
                break;
            case Phase.ZeroIndex:
                _nmosPointer = (byte)(_nmosPointer + NmosIndex);
                if (_nmosInstruction.Mode == Mode.IndexedIndirect) _nmosPhase = Phase.PointerLow;
                else { _nmosAddress = _nmosBase = _nmosPointer; BeginNmosData(); }
                break;
            case Phase.PointerLow: _nmosLow = value; _nmosPhase = Phase.PointerHigh; break;
            case Phase.PointerHigh: _nmosBase = (ushort)(_nmosLow | value << C.HighByteShift); FinishNmosAddress(); break;
            case Phase.IndexedRead: BeginNmosData(); break;
            case Phase.Data:
                if (NmosStores) CompleteNmosInstruction();
                else if (NmosModifies) { _nmosData = value; _nmosPhase = Phase.RmwOriginal; }
                else { ApplyNmosRead(value); CompleteNmosInstruction(); }
                break;
            case Phase.RmwOriginal: _nmosData = ModifyNmosValue(_nmosData); _nmosPhase = Phase.RmwResult; break;
            case Phase.RmwResult: ApplyNmosModified(_nmosData); CompleteNmosInstruction(); break;
            case Phase.Implied:
                switch (_nmosInstruction.Operation)
                {
                    case Op.Pha or Op.Php: _nmosPhase = Phase.StackPush; break;
                    case Op.Pla or Op.Plp or Op.Rts or Op.Rti: _nmosPhase = Phase.StackDummy; break;
                    default: ApplyNmosImplied(); CompleteNmosInstruction(); break;
                }
                break;
            case Phase.BranchTaken:
                _nmosBase = PC;
                PC = (ushort)(PC + (sbyte)_nmosLow);
                if ((PC & C.PageMask) == (_nmosBase & C.PageMask)) CompleteNmosInstruction();
                else _nmosPhase = Phase.BranchCross;
                break;
            case Phase.BranchCross:
                _branchIrqSample |= _irqPrevious && (P & InterruptDisable) == 0;
                _branchNmiSample |= _nmiPrevious;
                CompleteNmosInstruction();
                break;
            case Phase.StackPush: S--; CompleteNmosInstruction(); break;
            case Phase.StackDummy:
                S++;
                _nmosPhase = _nmosInstruction.Operation switch { Op.Rti => Phase.PullStatus, Op.Rts => Phase.PullLow, _ => Phase.StackPull };
                break;
            case Phase.StackPull:
                if (_nmosInstruction.Operation == Op.Pla) A = Nz(value);
                else P = (byte)((value & ~Break) | Unused);
                CompleteNmosInstruction();
                break;
            case Phase.PullStatus: P = (byte)((value & ~Break) | Unused); S++; _nmosPhase = Phase.PullLow; break;
            case Phase.PullLow: _nmosLow = value; S++; _nmosPhase = Phase.PullHigh; break;
            case Phase.PullHigh:
                _nmosAddress = (ushort)(_nmosLow | value << C.HighByteShift);
                if (_nmosInstruction.Operation == Op.Rts) _nmosPhase = Phase.ReturnRead;
                else { PC = _nmosAddress; CompleteNmosInstruction(); }
                break;
            case Phase.ReturnRead: PC = (ushort)(_nmosAddress + 1); CompleteNmosInstruction(); break;
            case Phase.JsrDummy: _nmosPhase = Phase.PushHigh; break;
            case Phase.PushHigh: S--; _nmosPhase = Phase.PushLow; break;
            case Phase.PushLow:
                S--;
                _nmosPhase = !_nmosHardwareInterrupt && _nmosInstruction.Operation == Op.Jsr ? Phase.OperandHigh : Phase.PushStatus;
                break;
            case Phase.BrkPadding: PC++; _nmosVector = C.IrqVector; _nmosPhase = Phase.PushHigh; break;
            case Phase.InterruptOpcode: _nmosPhase = Phase.InterruptDummy; break;
            case Phase.InterruptDummy: _nmosPhase = Phase.PushHigh; break;
            case Phase.PushStatus:
                S--;
                P |= InterruptDisable;
                _nmosVector = InterruptVector(_nmosVector, _nmosHardwareInterrupt);
                _nmosPhase = Phase.VectorLow;
                break;
            case Phase.VectorLow: _nmosLow = value; _nmosPhase = Phase.VectorHigh; break;
            case Phase.VectorHigh: PC = (ushort)(_nmosLow | value << C.HighByteShift); CompleteNmosInstruction(); break;
            case Phase.JumpIndirectLow: _nmosLow = value; _nmosPhase = Phase.JumpIndirectHigh; break;
            case Phase.JumpIndirectHigh: PC = (ushort)(_nmosLow | value << C.HighByteShift); CompleteNmosInstruction(); break;
            case Phase.JamOperand: _nmosPhase = Phase.JamHigh; break;
            case Phase.JamHigh: _nmosPhase = Phase.JamLowFirst; break;
            case Phase.JamLowFirst: _nmosPhase = Phase.JamLowSecond; break;
            case Phase.JamLowSecond: CompleteNmosInstruction(Phase.Jammed); break;
            case Phase.Jammed: break;
            case Phase.ResetFirst: _nmosPhase = Phase.ResetSecond; break;
            case Phase.ResetSecond: _nmosPhase = Phase.ResetStackFirst; break;
            case Phase.ResetStackFirst: S--; _nmosPhase = Phase.ResetStackSecond; break;
            case Phase.ResetStackSecond: S--; _nmosPhase = Phase.ResetStackThird; break;
            case Phase.ResetStackThird: S--; P |= InterruptDisable; _nmosPhase = Phase.ResetVectorLow; break;
            case Phase.ResetVectorLow: _nmosLow = value; _nmosPhase = Phase.ResetVectorHigh; break;
            case Phase.ResetVectorHigh:
                PC = (ushort)(_nmosLow | value << C.HighByteShift);
                _irqPending = false;
                _nmiAccepted = _nmiPrevious;
                _nmosPhase = Phase.Opcode;
                break;
            default: throw new InvalidOperationException(nameof(_nmosPhase));
        }
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
        P |= Unused;
        var flags = !_nmosHardwareInterrupt && _nmosInstruction.Operation is Op.Cli or Op.Sei or Op.Plp ? _nmosFlagsBefore : P;
        _irqPending = _branchIrqSample ?? (_irqPrevious && (flags & InterruptDisable) == 0);
        _nmiAccepted = _branchNmiSample ?? _nmiPrevious;
        _nmosPhase = next;
    }

    private byte NmosIndex => _nmosInstruction.Mode switch
    {
        Mode.ZeroPageX or Mode.AbsoluteX or Mode.IndexedIndirect => X,
        Mode.ZeroPageY or Mode.AbsoluteY or Mode.IndirectIndexed => Y,
        _ => 0
    };

    private bool NmosStores => _nmosInstruction.Operation is Op.Sta or Op.Stx or Op.Sty or Op.Sax or Op.Ahx or Op.Shx or Op.Shy or Op.Tas;
    private bool NmosModifies => _nmosInstruction.Operation is Op.Asl or Op.Lsr or Op.Rol or Op.Ror or Op.Inc or Op.Dec or Op.Slo or Op.Sre or Op.Rla or Op.Rra or Op.Dcp or Op.Isc;

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
            if (_nmosInstruction.Operation == Op.Tas) S = (byte)(A & X);
            _nmosData = _nmosInstruction.Operation switch
            {
                Op.Sta => A,
                Op.Stx => X,
                Op.Sty => Y,
                Op.Sax => (byte)(A & X),
                Op.Shx => (byte)(X & ((_nmosBase >> C.HighByteShift) + 1)),
                Op.Shy => (byte)(Y & ((_nmosBase >> C.HighByteShift) + 1)),
                _ => (byte)(A & X & ((_nmosBase >> C.HighByteShift) + 1))
            };
            if (_nmosInstruction.Operation is Op.Shx or Op.Shy or Op.Ahx or Op.Tas &&
                (_nmosAddress & C.PageMask) != (_nmosBase & C.PageMask))
                _nmosAddress = (ushort)((_nmosData << C.HighByteShift) | (byte)_nmosAddress);
        }
        _nmosPhase = Phase.Data;
    }
}

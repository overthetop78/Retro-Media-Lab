using C = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Constants.Cpu6502Constants;
using Phase = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Enums.Cpu6502BusPhase;
using Mode = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Enums.Cpu6502AddressMode;
using Op = RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Enums.Cpu6502Operation;

namespace RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;

public partial class Cpu6502
{
    private bool _cmosSync;
    private bool _cmosHeldSync;
    private bool _cmosOpcodeHeld;
    private bool _cmosResetHeld;

    private void SetCmosSync(bool high)
    {
        if (_cmosSync == high) return;
        _cmosSync = high;
        if (_publishesSync) bus.SetSync(high);
    }

    private bool ReadCmosCycle(ushort address, out byte value, bool sync = false)
    {
        value = default;
        if (!_clockCyclePrepared)
        {
            SetCmosSync(sync);
            PresentCmosRead(address);
        }
        if (_clockPresentOnly) return false;
        Tick();
        if (_samplesReset && ResetInputActive) { HoldCmosReset(); return false; }
        value = _busPinsEnabled ? bus.Read(address) : bus.ReadFloatingData();
        if (!_samplesReady || ReadyInputHigh) return true;
        _cmosOpcodeHeld |= _cmosPhase == Phase.Opcode;
        _cmosHeldSync = sync;
        return false;
    }

    private bool WriteCmosCycle(ushort address, byte value)
    {
        if (!_clockCyclePrepared)
        {
            SetCmosSync(false);
            PresentCmosWrite(address, value);
        }
        else value = _busCycleData;
        if (_clockPresentOnly) return false;
        Tick();
        if (_samplesReset && ResetInputActive) { HoldCmosReset(); return false; }
        if (_samplesReady && !ReadyInputHigh) return false;
        if (_busPinsEnabled)
        {
            if (DirectDataBusEnabled) bus.Write(address, value);
            else bus.WriteUndriven(address);
        }
        return true;
    }

    private void HoldCmosReset()
    {
        SetMemoryLock(false);
        _cmosResetHeld = true;
        _cmosOpcodeHeld = false;
        _cmosPhase = Phase.ResetFirst;
        SetCmosSync(false);
    }

    private void BeginCmosReset()
    {
        SetMemoryLock(false);
        _cmosResetHeld = _cmosOpcodeHeld = _cmosHardwareInterrupt = false;
        ResetOverflowInput();
        ResetNmiInput();
        _nmiPending = _nmiAccepted = _nmiPrevious = false;
        _irqPending = _irqPrevious = _irqLevel = false;
        _p |= Unused;
        _cmosPhase = Phase.ResetFirst;
        SetCmosSync(false);
    }

    private void BeginCmosInterrupt(ushort vector, bool opcodeRead = false)
    {
        if (vector == C.NmiVector) _nmiPending = _nmiAccepted = false;
        _irqPending = false;
        _cmosHardwareInterrupt = true;
        _cmosVector = vector;
        _cmosOpcodeHeld = false;
        // BRK keeps its own vector; a hardware IRQ can still be redirected before vector fetch.
        _cmosPhase = opcodeRead ? Phase.InterruptDummy : Phase.InterruptOpcode;
    }

    private void CompleteCmosInstruction()
    {
        _p |= Unused;
        var flags = !_cmosHardwareInterrupt && _cmosInstruction.Operation is Op.Cli or Op.Sei or Op.Plp
            ? _cmosFlagsBefore : _p;
        _irqPending = _irqPrevious && (flags & InterruptDisable) == 0;
        _nmiAccepted = _nmiPrevious;
        _cmosPhase = Phase.Opcode;
    }

    private byte CmosIndex => _cmosInstruction.Mode switch
    {
        Mode.ZeroPageX or Mode.AbsoluteX or Mode.IndexedIndirect or Mode.AbsoluteIndexedIndirect => _x,
        Mode.ZeroPageY or Mode.AbsoluteY or Mode.IndirectIndexed => _y,
        _ => 0
    };

    private void FinishCmosAddress()
    {
        _cmosAddress = (ushort)(_cmosBase + (_cmosInstruction.Mode == Mode.IndexedIndirect ? 0 : CmosIndex));
        var crossing = (_cmosAddress & C.PageMask) != (_cmosBase & C.PageMask);
        var fixedModifyCycle = _cmosInstruction.Operation is Op.Inc or Op.Dec;
        if (_cmosInstruction.Mode is Mode.AbsoluteX or Mode.AbsoluteY or Mode.IndirectIndexed
            && (_cmosInstruction.Stores || crossing || _cmosInstruction.Modifies && fixedModifyCycle))
            _cmosPhase = Phase.IndexedRead;
        else BeginCmosData();
    }

    private void BeginCmosData()
    {
        if (_cmosInstruction.Stores)
            _cmosData = _cmosInstruction.Operation switch { Op.Sta => _a, Op.Stx => _x, Op.Sty => _y, _ => 0 };
        _cmosPhase = Phase.Data;
    }

    private void FinishCmosRead(byte value)
    {
        if ((_p & Decimal) != 0 && _cmosInstruction.Operation is Op.Adc or Op.Sbc)
        {
            _cmosData = value;
            _cmosPhase = Phase.DecimalAdjust;
        }
        else { ApplyCmosRead(value); CompleteCmosInstruction(); }
    }
}

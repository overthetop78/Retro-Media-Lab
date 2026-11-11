using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Enums;
using C = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Constants.Via6522Constants;

namespace RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;

public sealed partial class Via6522
{
    private bool _ca1;
    private bool _ca2;
    private bool _cb1;
    private bool _cb2;
    private bool _ca2Output = true;
    private bool _cb2Output = true;
    private int _ca2Pulse;
    private int _cb2Pulse;
    private byte _inputLatchA;
    private byte _inputLatchB;
    private bool? _pb6Input;
    private bool _lastPb6 = true;

    public Action<bool>? Ca2PinsChanged { get; set; }
    public Action<bool>? Cb2PinsChanged { get; set; }
    public Action<ViaControlOutputs>? ControlOutputsChanged { get; set; }
    public ViaControlOutputs ControlOutputs =>
        (_controlModeA >= ViaControlMode.HandshakeOutput ? ViaControlOutputs.Ca2 : ViaControlOutputs.None)
        | (_shiftInternalClock ? ViaControlOutputs.Cb1 : ViaControlOutputs.None)
        | ((ShiftOwnsCb2 ? _shiftOutput : _controlModeB >= ViaControlMode.HandshakeOutput)
            ? ViaControlOutputs.Cb2 : ViaControlOutputs.None);
    public bool Ca2 => _controlModeA >= ViaControlMode.HandshakeOutput ? _ca2Output : _ca2;
    public bool Cb2 => ShiftOwnsCb2 ? _shiftOutput ? _cb2Output : _cb2
        : _controlModeB >= ViaControlMode.HandshakeOutput ? _cb2Output : _cb2;
    private ViaControlMode _controlModeA;
    private ViaControlMode _controlModeB;
    private bool ShiftOwnsCb2 => _shiftMode != ViaShiftMode.Disabled && (!_shiftOutput || _shiftActive);
    private void PublishControlOwnership(ViaControlOutputs previous)
    {
        var current = ControlOutputs;
        if (current != previous) ControlOutputsChanged?.Invoke(current);
    }
    private byte PortAPins => (byte)(_registers[C.PortA] | ~_registers[C.DirectionA]);
    private byte InputA => ReadPortA?.Invoke() ?? byte.MaxValue;
    private byte InputB
    {
        get
        {
            var pins = ReadPortB?.Invoke() ?? byte.MaxValue;
            return _pb6Input is not { } level ? pins : level ? (byte)(pins | C.Pb6) : (byte)(pins & ~C.Pb6);
        }
    }
    private bool Pb6Level => (_registers[C.DirectionB] & C.Pb6) != 0
        ? (_registers[C.PortB] & C.Pb6) != 0
        : _pb6Input ?? ((ReadPortB?.Invoke() ?? byte.MaxValue) & C.Pb6) != 0;

    public void SetPb6(bool level) { if (_receivingPhi2Input) SynchronizePhi2Input(); _pb6Input = level; }
    public void VSync() { SetCa1(true); SetCa1(false); }

    public void SetCa1(bool level)
    {
        if (_receivingPhi2Input) SynchronizePhi2Input();
        if (level != _ca1 && level == ((_registers[C.Peripheral] & C.Ca1Rising) != 0))
        {
            if ((_registers[C.Auxiliary] & C.LatchA) != 0) _inputLatchA = LivePinsA;
            InterruptFlags |= C.Ca1Interrupt;
            if (_controlModeA == ViaControlMode.HandshakeOutput) SetControlOutput(true, true);
        }
        _ca1 = level;
    }

    public void SetKeyboardLine(bool level) => SetCa2(level);
    public void SetCa2(bool level)
    {
        if (_receivingPhi2Input) SynchronizePhi2Input();
        if (level != _ca2 && ActiveControlEdge(_controlModeA, level)) InterruptFlags |= C.Ca2Interrupt;
        _ca2 = level;
    }

    public void SetCb1(bool level)
    {
        if (_receivingPhi2Input) SynchronizePhi2Input();
        if (!_shiftInternalClock)
        {
            CaptureCb1Edge(_cb1, level);
            // Le mode 0 interdit l'IRQ série, mais conserve le décalage statique
            // de CB2 par CB1 (notamment utilisé par les interfaces MMFS).
            if (!_cb1 && level && _shiftMode == ViaShiftMode.Disabled)
                _registers[C.Shift] = (byte)((_registers[C.Shift] << 1) | (Cb2 ? 1 : 0));
            if (_cb1 != level && _shiftMode != ViaShiftMode.Disabled)
            {
                _cb1 = level;
                if (_shiftMode == ViaShiftMode.InputExternalClock)
                {
                    if (level && _shiftActive) _externalShiftPending = true;
                }
                else ShiftEdge(level, true);
            }
        }
        _cb1 = level;
    }

    private void CaptureCb1Edge(bool previous, bool level)
    {
        if (level != previous && level == ((_registers[C.Peripheral] & C.Cb1Rising) != 0))
        {
            if ((_registers[C.Auxiliary] & C.LatchB) != 0) _inputLatchB = InputB;
            InterruptFlags |= C.Cb1Interrupt;
            if (!ShiftOwnsCb2 && _controlModeB == ViaControlMode.HandshakeOutput) SetControlOutput(false, true);
        }
    }

    public void SetCb2(bool level)
    {
        if (_receivingPhi2Input) SynchronizePhi2Input();
        if (!ShiftOwnsCb2 && level != _cb2 && ActiveControlEdge(_controlModeB, level)) InterruptFlags |= C.Cb2Interrupt;
        _cb2 = level;
    }

    private static bool ActiveControlEdge(ViaControlMode mode, bool level) => mode switch
    {
        ViaControlMode.FallingInput or ViaControlMode.IndependentFallingInput => !level,
        ViaControlMode.RisingInput or ViaControlMode.IndependentRisingInput => level,
        _ => false
    };

    private byte LivePinsA => (byte)((_registers[C.PortA] | ~_registers[C.DirectionA]) & InputA);
    private byte ReadPinsA() => (_registers[C.Auxiliary] & C.LatchA) != 0 ? _inputLatchA : LivePinsA;
    private byte ReadPinsB()
    {
        var inputs = (_registers[C.Auxiliary] & C.LatchB) != 0 ? _inputLatchB : InputB;
        return ApplyTimerPb7((byte)(_registers[C.PortB] & _registers[C.DirectionB] | inputs & ~_registers[C.DirectionB]));
    }

    private void AcknowledgePort(bool portA, bool strobe)
    {
        var mode = portA ? _controlModeA : _controlModeB;
        InterruptFlags &= unchecked((byte)~(portA ? C.Ca1Interrupt : C.Cb1Interrupt));
        if (mode is not (ViaControlMode.IndependentFallingInput or ViaControlMode.IndependentRisingInput))
            InterruptFlags &= unchecked((byte)~(portA ? C.Ca2Interrupt : C.Cb2Interrupt));
        if (!strobe || (!portA && ShiftOwnsCb2) || mode is not (ViaControlMode.HandshakeOutput or ViaControlMode.PulseOutput)) return;
        SetControlOutput(portA, false);
        if (mode != ViaControlMode.PulseOutput) return;
        var clocks = C.PulseCycles + (_phi2High ? C.PulseLoadCycles : 0);
        if (portA) _ca2Pulse = clocks;
        else _cb2Pulse = clocks;
    }

    private void SetControlOutput(bool portA, bool level)
    {
        if (portA)
        {
            if (_ca2Output == level) return;
            _ca2Output = level;
            Ca2PinsChanged?.Invoke(level);
        }
        else
        {
            if (_cb2Output == level) return;
            _cb2Output = level;
            Cb2PinsChanged?.Invoke(level);
        }
    }

    private void ConfigureControlOutputs(ViaControlMode previousA, ViaControlMode previousB)
    {
        if (previousA != _controlModeA)
        {
            _ca2Pulse = 0;
            if (_controlModeA >= ViaControlMode.HandshakeOutput) SetControlOutput(true, _controlModeA != ViaControlMode.LowOutput);
        }
        if (previousB == _controlModeB) return;
        _cb2Pulse = 0;
        if (!ShiftOwnsCb2) RestoreCb2Control();
    }

    private void RestoreCb2Control()
    {
        _cb2Pulse = 0;
        SetControlOutput(false, _controlModeB >= ViaControlMode.HandshakeOutput
            ? _controlModeB != ViaControlMode.LowOutput : _cb2);
    }

    private void TickControlOutputs()
    {
        if (_ca2Pulse > 0 && --_ca2Pulse == 0) SetControlOutput(true, true);
        if (_cb2Pulse > 0 && --_cb2Pulse == 0) SetControlOutput(false, true);
    }

    private void ResetPorts()
    {
        _inputLatchA = _inputLatchB = 0;
        _ca2Pulse = _cb2Pulse = 0;
        _lastPb6 = true;
        SetControlOutput(true, true);
        SetControlOutput(false, true);
        PortAPinsChanged?.Invoke(PortAPins);
        PortBPinsChanged?.Invoke(PortBPins);
    }
}

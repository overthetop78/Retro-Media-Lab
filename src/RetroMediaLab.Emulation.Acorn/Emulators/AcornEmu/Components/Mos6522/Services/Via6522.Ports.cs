using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6522.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Enums;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6522.Constants.Via6522Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6522.Services;

internal sealed partial class Via6522
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

    internal Action<bool>? Ca2PinsChanged { get; set; }
    internal Action<bool>? Cb2PinsChanged { get; set; }
    internal bool Ca2 => ModeA >= ViaControlMode.HandshakeOutput ? _ca2Output : _ca2;
    internal bool Cb2 => ModeB >= ViaControlMode.HandshakeOutput ? _cb2Output : _cb2;
    private ViaControlMode ModeA => (ViaControlMode)(_registers[C.Peripheral] >> C.Ca2ModeShift & C.ControlModeMask);
    private ViaControlMode ModeB => (ViaControlMode)(_registers[C.Peripheral] >> C.Cb2ModeShift & C.ControlModeMask);
    private byte InputA => ReadPortA?.Invoke() ?? byte.MaxValue;
    private byte InputB
    {
        get
        {
            var pins = ReadPortB?.Invoke() ?? byte.MaxValue;
            return _pb6Input is not { } level ? pins : level ? (byte)(pins | C.Pb6) : (byte)(pins & ~C.Pb6);
        }
    }
    private bool Pb6Level => ((InputB & ~_registers[C.DirectionB] | _registers[C.PortB] & _registers[C.DirectionB]) & C.Pb6) != 0;

    internal void SetPb6(bool level) => _pb6Input = level;
    internal void VSync() { SetCa1(true); SetCa1(false); }

    internal void SetCa1(bool level)
    {
        if (level != _ca1 && level == ((_registers[C.Peripheral] & C.Ca1Rising) != 0))
        {
            if ((_registers[C.Auxiliary] & C.LatchA) != 0) _inputLatchA = InputA;
            _flags |= C.Ca1Interrupt;
            if (ModeA == ViaControlMode.HandshakeOutput) SetControlOutput(true, true);
        }
        _ca1 = level;
    }

    internal void SetKeyboardLine(bool level) => SetCa2(level);
    internal void SetCa2(bool level)
    {
        if (level != _ca2 && ActiveControlEdge(ModeA, level)) _flags |= C.Ca2Interrupt;
        _ca2 = level;
    }

    internal void SetCb1(bool level)
    {
        if (level != _cb1 && level == ((_registers[C.Peripheral] & C.Cb1Rising) != 0))
        {
            if ((_registers[C.Auxiliary] & C.LatchB) != 0) _inputLatchB = InputB;
            _flags |= C.Cb1Interrupt;
            if (ModeB == ViaControlMode.HandshakeOutput) SetControlOutput(false, true);
        }
        _cb1 = level;
    }

    internal void SetCb2(bool level)
    {
        if (level != _cb2 && ActiveControlEdge(ModeB, level)) _flags |= C.Cb2Interrupt;
        _cb2 = level;
    }

    private static bool ActiveControlEdge(ViaControlMode mode, bool level) => mode switch
    {
        ViaControlMode.FallingInput or ViaControlMode.IndependentFallingInput => !level,
        ViaControlMode.RisingInput or ViaControlMode.IndependentRisingInput => level,
        _ => false
    };

    private byte ReadPinsA()
    {
        var inputs = (_registers[C.Auxiliary] & C.LatchA) != 0 ? _inputLatchA : InputA;
        return (byte)(_registers[C.PortA] & _registers[C.DirectionA] | inputs & ~_registers[C.DirectionA]);
    }
    private byte ReadPinsB()
    {
        var inputs = (_registers[C.Auxiliary] & C.LatchB) != 0 ? _inputLatchB : InputB;
        return (byte)(_registers[C.PortB] & _registers[C.DirectionB] | inputs & ~_registers[C.DirectionB]);
    }

    private void AcknowledgePort(bool portA, bool strobe)
    {
        var mode = portA ? ModeA : ModeB;
        _flags &= unchecked((byte)~(portA ? C.Ca1Interrupt : C.Cb1Interrupt));
        if (mode is not (ViaControlMode.IndependentFallingInput or ViaControlMode.IndependentRisingInput))
            _flags &= unchecked((byte)~(portA ? C.Ca2Interrupt : C.Cb2Interrupt));
        if (!strobe || mode is not (ViaControlMode.HandshakeOutput or ViaControlMode.PulseOutput)) return;
        SetControlOutput(portA, false);
        if (mode != ViaControlMode.PulseOutput) return;
        if (portA) _ca2Pulse = C.PulseCycles;
        else _cb2Pulse = C.PulseCycles;
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

    private void ConfigureControlOutputs()
    {
        _ca2Pulse = _cb2Pulse = 0;
        if (ModeA >= ViaControlMode.HandshakeOutput) SetControlOutput(true, ModeA != ViaControlMode.LowOutput);
        if (ModeB >= ViaControlMode.HandshakeOutput) SetControlOutput(false, ModeB != ViaControlMode.LowOutput);
    }

    private void TickControlOutputs()
    {
        if (_ca2Pulse > 0 && --_ca2Pulse == 0) SetControlOutput(true, true);
        if (_cb2Pulse > 0 && --_cb2Pulse == 0) SetControlOutput(false, true);
    }

    private void ResetPorts()
    {
        _ca1 = _ca2 = _cb1 = _cb2 = false;
        _inputLatchA = _inputLatchB = 0;
        _ca2Pulse = _cb2Pulse = 0;
        _lastPb6 = true;
        SetControlOutput(true, true);
        SetControlOutput(false, true);
    }
}

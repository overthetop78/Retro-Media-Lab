using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Enums;
using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Interfaces;
using C = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Constants.Via6522Constants;

namespace RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;

/// <summary>VIA 6522 ; Tick correspond à un cycle de son horloge, indépendamment de la carte propriétaire.</summary>
public sealed partial class Via6522 : IVia6522
{
    private readonly byte[] _registers = new byte[C.RegisterCount];
    private int _timer1 = C.CounterMask;
    private int _timer2 = C.CounterMask;
    private int _latch1 = C.CounterMask;
    private bool _timer1Running;
    private bool _timer2Running;
    private bool _timer2Armed;

    public Func<byte>? ReadPortA { get; set; }
    public Func<byte>? ReadPortB { get; set; }
    public Action<byte>? WritePortA { get; set; }
    public Action<byte>? WritePortB { get; set; }
    public Action<byte>? PortAPinsChanged { get; set; }
    public Action<byte>? PortBPinsChanged { get; set; }
    public byte OutputA => _registers[C.PortA];
    public bool Irq => _irq;
    private byte PortBPins => ApplyTimerPb7((byte)(_registers[C.PortB] | ~_registers[C.DirectionB]));

    public void Reset()
    {
        if (_receivingPhi2Input) SynchronizePhi2Input();
        var previousOutputs = ControlOutputs;
        var shift = _registers[C.Shift];
        var latch2Low = _registers[C.Timer2Low];
        Array.Clear(_registers);
        _registers[C.Shift] = shift;
        _registers[C.Timer2Low] = latch2Low;
        InterruptFlags = InterruptEnabled = 0;
        _shiftMode = ViaShiftMode.Disabled;
        _shiftOutput = _shiftUsesTimer = false;
        _controlModeA = _controlModeB = ViaControlMode.FallingInput;
        _timer2Armed = false;
        ResetTimers();
        ResetShift();
        ResetPorts();
        PublishControlOwnership(previousOutputs);
    }

    private void CountTimer2()
    {
        _timer2--;
        if (_timer2 >= 0) return;
        if (_timer2 < 0) _timer2 = C.CounterMask;
        FireTimer2();
    }

    public byte Read(int register)
    {
        if (_receivingPhi2Input) SynchronizePhi2Input();
        register &= C.RegisterMask;
        switch (register)
        {
            case C.PortB:
                var inputB = ReadPinsB();
                AcknowledgePort(false, false);
                return inputB;
            case C.PortA:
                var inputA = ReadPinsA();
                AcknowledgePort(true, true);
                return inputA;
            case C.PortANoHandshake: return ReadPinsA();
            case C.Timer1Low:
                if (!_timer1JustFired) InterruptFlags &= unchecked((byte)~C.Timer1Interrupt);
                return (byte)_timer1;
            case C.Timer1High: return (byte)(_timer1 >> C.HighByteShift);
            case C.Latch1Low: return (byte)_latch1;
            case C.Latch1High: return (byte)(_latch1 >> C.HighByteShift);
            case C.Timer2Low:
                if (!_timer2JustFired) InterruptFlags &= unchecked((byte)~C.Timer2Interrupt);
                return (byte)_timer2;
            case C.Timer2High: return (byte)(_timer2 >> C.HighByteShift);
            case C.Flags: return (byte)(InterruptFlags | (Irq ? C.InterruptSummary : 0));
            case C.Enable: return (byte)(InterruptEnabled | C.InterruptSummary);
            case C.Shift:
                var shifted = _registers[C.Shift];
                StartShift();
                return shifted;
            default: return _registers[register];
        }
    }

    public void Write(int register, byte value)
    {
        if (_receivingPhi2Input) SynchronizePhi2Input();
        register &= C.RegisterMask;
        switch (register)
        {
            case C.PortB:
                _registers[C.PortB] = value;
                WritePortB?.Invoke(value);
                PortBPinsChanged?.Invoke(PortBPins);
                AcknowledgePort(false, true);
                break;
            case C.DirectionB:
                _registers[C.DirectionB] = value;
                PortBPinsChanged?.Invoke(PortBPins);
                break;
            case C.DirectionA:
                _registers[C.DirectionA] = value;
                PortAPinsChanged?.Invoke(PortAPins);
                break;
            case C.PortA:
            case C.PortANoHandshake:
                _registers[C.PortA] = value;
                WritePortA?.Invoke(value);
                PortAPinsChanged?.Invoke(PortAPins);
                if (register == C.PortA) AcknowledgePort(true, true);
                break;
            case C.Timer1Low:
            case C.Latch1Low: _latch1 = (_latch1 & C.HighByteMask) | value; break;
            case C.Timer1High:
                _latch1 = (_latch1 & byte.MaxValue) | value << C.HighByteShift;
                LoadTimer1();
                if (!_timer1JustFired) InterruptFlags &= unchecked((byte)~C.Timer1Interrupt);
                break;
            case C.Latch1High:
                _latch1 = (_latch1 & byte.MaxValue) | value << C.HighByteShift;
                if (!_timer1JustFired) InterruptFlags &= unchecked((byte)~C.Timer1Interrupt);
                break;
            case C.Timer2Low: _registers[C.Timer2Low] = value; break;
            case C.Timer2High:
                _timer2 = _registers[C.Timer2Low] | value << C.HighByteShift;
                _timer2Running = _timer2Armed = true;
                _timer2LoadDelay = C.TimerLoadCycles;
                _timer2Reload = false;
                _lastPb6 = Pb6Level;
                _timer2PulseSampled = false;
                if (!_timer2JustFired) InterruptFlags &= unchecked((byte)~C.Timer2Interrupt);
                break;
            case C.Shift:
                _registers[C.Shift] = value;
                StartShift();
                break;
            case C.Auxiliary:
                var previousShiftOutputs = ControlOutputs;
                var previousMode = _shiftMode;
                _registers[C.Auxiliary] = value;
                _shiftMode = (ViaShiftMode)(value >> C.ShiftModeShift & C.ShiftModeMask);
                _shiftOutput = _shiftMode >= ViaShiftMode.OutputTimer2Continuous;
                _shiftUsesTimer = _shiftMode is ViaShiftMode.InputTimer2 or ViaShiftMode.OutputTimer2Continuous or ViaShiftMode.OutputTimer2;
                _shiftInternalClock = _shiftMode is not (ViaShiftMode.Disabled or ViaShiftMode.InputExternalClock or ViaShiftMode.OutputExternalClock);
                _shiftSystemClock = _shiftMode is ViaShiftMode.InputSystemClock or ViaShiftMode.OutputSystemClock;
                _timer1Continuous = (value & C.Timer1Continuous) != 0;
                _timer1Output = (value & C.Timer1Pb7) != 0;
                _timer2PulseMode = (value & C.Timer2PulseCount) != 0;
                if (_timer1JustFired && (value & C.Timer1Continuous) == 0) _timer1Armed = false;
                ConfigureShift(previousMode);
                PortBPinsChanged?.Invoke(PortBPins);
                PublishControlOwnership(previousShiftOutputs);
                break;
            case C.Peripheral:
                var previousPeripheralOutputs = ControlOutputs;
                var previousA = _controlModeA;
                var previousB = _controlModeB;
                _registers[C.Peripheral] = value;
                _controlModeA = (ViaControlMode)(value >> C.Ca2ModeShift & C.ControlModeMask);
                _controlModeB = (ViaControlMode)(value >> C.Cb2ModeShift & C.ControlModeMask);
                ConfigureControlOutputs(previousA, previousB);
                PublishControlOwnership(previousPeripheralOutputs);
                break;
            case C.Flags: InterruptFlags &= (byte)~(value & C.InterruptMask); break;
            case C.Enable:
                InterruptEnabled = (value & C.InterruptSummary) != 0
                    ? (byte)(InterruptEnabled | (value & C.InterruptMask))
                    : (byte)(InterruptEnabled & ~value);
                break;
            default: _registers[register] = value; break;
        }
    }
}

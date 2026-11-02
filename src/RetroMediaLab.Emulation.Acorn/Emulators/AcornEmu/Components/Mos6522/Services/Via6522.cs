using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6522.Constants.Via6522Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6522.Services;

/// <summary>VIA 6522 ; Tick correspond à un cycle de son horloge, indépendamment de la carte propriétaire.</summary>
internal sealed partial class Via6522
{
    private readonly byte[] _registers = new byte[C.RegisterCount];
    private int _timer1 = C.CounterMask;
    private int _timer2 = C.CounterMask;
    private int _latch1 = C.CounterMask;
    private bool _timer1Running;
    private bool _timer2Running;
    private bool _timer2Armed;
    private byte _flags;
    private byte _enabled;

    internal Func<byte>? ReadPortA { get; set; }
    internal Func<byte>? ReadPortB { get; set; }
    internal Action<byte>? WritePortA { get; set; }
    internal Action<byte>? WritePortB { get; set; }
    internal Action<byte>? PortBPinsChanged { get; set; }
    internal byte OutputA => _registers[C.PortA];
    internal bool Irq => (_flags & _enabled & C.InterruptMask) != 0;
    private byte PortBPins => ApplyTimerPb7((byte)(_registers[C.PortB] | ~_registers[C.DirectionB]));

    internal void Reset()
    {
        var shift = _registers[C.Shift];
        var latch2Low = _registers[C.Timer2Low];
        Array.Clear(_registers);
        _registers[C.Shift] = shift;
        _registers[C.Timer2Low] = latch2Low;
        _flags = _enabled = 0;
        _timer2Armed = false;
        ResetTimers();
        ResetShift();
        ResetPorts();
    }

    internal void Tick()
    {
        TickControlOutputs();
        TickShift();
        TickTimers();
    }

    private void CountTimer2(bool pulseCount)
    {
        _timer2--;
        if (pulseCount ? _timer2 > 0 : _timer2 >= 0) return;
        if (_timer2 < 0) _timer2 = C.CounterMask;
        FireTimer2();
    }

    internal byte Read(int register)
    {
        register &= C.RegisterMask;
        switch (register)
        {
            case C.PortB: AcknowledgePort(false, false); return ReadPinsB();
            case C.PortA: AcknowledgePort(true, true); return ReadPinsA();
            case C.PortANoHandshake: return ReadPinsA();
            case C.Timer1Low:
                if (!_timer1JustFired) _flags &= unchecked((byte)~C.Timer1Interrupt);
                return (byte)_timer1;
            case C.Timer1High: return (byte)(_timer1 >> C.HighByteShift);
            case C.Latch1Low: return (byte)_latch1;
            case C.Latch1High: return (byte)(_latch1 >> C.HighByteShift);
            case C.Timer2Low:
                if (!_timer2JustFired) _flags &= unchecked((byte)~C.Timer2Interrupt);
                return (byte)_timer2;
            case C.Timer2High: return (byte)(_timer2 >> C.HighByteShift);
            case C.Flags: return (byte)(_flags | (Irq ? C.InterruptSummary : 0));
            case C.Enable: return (byte)(_enabled | C.InterruptSummary);
            case C.Shift:
                var shifted = _registers[C.Shift];
                StartShift();
                return shifted;
            default: return _registers[register];
        }
    }

    internal void Write(int register, byte value)
    {
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
            case C.PortA:
            case C.PortANoHandshake:
                _registers[C.PortA] = value;
                WritePortA?.Invoke(value);
                if (register == C.PortA) AcknowledgePort(true, true);
                break;
            case C.Timer1Low:
            case C.Latch1Low: _latch1 = (_latch1 & C.HighByteMask) | value; break;
            case C.Timer1High:
                _latch1 = (_latch1 & byte.MaxValue) | value << C.HighByteShift;
                LoadTimer1();
                if (!_timer1JustFired) _flags &= unchecked((byte)~C.Timer1Interrupt);
                break;
            case C.Latch1High:
                _latch1 = (_latch1 & byte.MaxValue) | value << C.HighByteShift;
                if (!_timer1JustFired) _flags &= unchecked((byte)~C.Timer1Interrupt);
                break;
            case C.Timer2Low: _registers[C.Timer2Low] = value; break;
            case C.Timer2High:
                _timer2 = _registers[C.Timer2Low] | value << C.HighByteShift;
                _timer2Running = _timer2Armed = true;
                _timer2LoadDelay = C.TimerLoadCycles;
                _timer2Reload = false;
                _lastPb6 = Pb6Level;
                if (!_timer2JustFired) _flags &= unchecked((byte)~C.Timer2Interrupt);
                break;
            case C.Shift:
                _registers[C.Shift] = value;
                StartShift();
                break;
            case C.Auxiliary:
                var previousMode = ShiftMode;
                _registers[C.Auxiliary] = value;
                ConfigureShift(previousMode);
                PortBPinsChanged?.Invoke(PortBPins);
                break;
            case C.Peripheral:
                _registers[C.Peripheral] = value;
                ConfigureControlOutputs();
                break;
            case C.Flags: _flags &= (byte)~(value & C.InterruptMask); break;
            case C.Enable:
                _enabled = (value & C.InterruptSummary) != 0
                    ? (byte)(_enabled | (value & C.InterruptMask))
                    : (byte)(_enabled & ~value);
                break;
            default: _registers[register] = value; break;
        }
    }
}

using RetroMediaLab.Chips.IO.Acorn.Archimedes.IOC.Enums;
using C = RetroMediaLab.Chips.IO.Acorn.Archimedes.IOC.Constants.ArchimedesIocConstants;

namespace RetroMediaLab.Chips.IO.Acorn.Archimedes.IOC.Services;

/// <summary>IOC : interruptions, quatre compteurs 2 MHz, port ouvert et KART 8N2.</summary>
public sealed class ArchimedesIoc
:RetroMediaLab.Chips.IO.Acorn.Archimedes.IOC.Interfaces.IArchimedesIoc{
    private readonly ushort[] _timerReload = new ushort[C.TimerCount];
    private readonly int[] _timerRemaining = new int[C.TimerCount];
    private readonly ushort[] _timerLatched = new ushort[C.TimerCount];
    private byte _control;
    private byte _controlInputs;
    private byte _irqA;
    private byte _irqB;
    private byte _fiq;
    private byte _maskA;
    private byte _maskB;
    private byte _maskFiq;
    private bool _fallingPin;
    private bool _risingPin;
    private byte _receiveData;
    private byte _receiveShift;
    private int _receiveRemaining;
    private bool _receiveSignalled;
    private byte _transmitData;
    private int _transmitRemaining;

    public bool Irq => ((StatusA & _maskA) | (_irqB & _maskB)) != 0;
    public bool Fiq => (StatusFiq & _maskFiq) != 0;
    public byte StatusA => (byte)(_irqA | C.ForceInterrupt);
    public byte StatusB => _irqB;
    public byte StatusFiq => (byte)(_fiq | C.ForceInterrupt | (~(_control & _controlInputs) & C.ControlFiqPins));
    public bool KeyboardReceiveBusy => _receiveRemaining != 0;
    public byte ControlOutput => _control;
    public Action<byte>? ControlWritten { get; set; }
    public Action<byte>? KeyboardTransmitted { get; set; }
    public Action<int>? BaudTransitions { get; set; }

    public void Reset(bool cold)
    {
        _control = C.InitialControl;
        _controlInputs = C.ControlPins;
        _fallingPin = true;
        _risingPin = false;
        _irqA = cold ? C.PowerOnInterrupt : (byte)0;
        _irqB = C.KeyboardTransmitInterrupt;
        _fiq = _maskA = _maskB = _maskFiq = 0;
        _receiveData = _receiveShift = _transmitData = 0;
        _receiveRemaining = _transmitRemaining = 0;
        _receiveSignalled = false;
        Array.Clear(_timerReload);
        Array.Clear(_timerLatched);
        Array.Fill(_timerRemaining, 1);
    }

    public void SetControlInputs(byte pins) => _controlInputs = (byte)(pins & C.ControlPins);

    public void SetFallingInput(bool high)
    {
        if (_fallingPin && !high) _irqA |= C.FallingInterrupt;
        _fallingPin = high;
    }

    public void SetFlyback(bool high)
    {
        if (!_risingPin && high) _irqA |= C.FlybackInterrupt;
        _risingPin = high;
    }

    public void SetIrqA(byte mask, bool active) => _irqA = active ? (byte)(_irqA | mask) : (byte)(_irqA & ~mask);
    public void SetIrqB(byte mask, bool active) => _irqB = active ? (byte)(_irqB | mask) : (byte)(_irqB & ~mask);
    public void SetFiq(byte mask, bool active) => _fiq = active ? (byte)(_fiq | mask) : (byte)(_fiq & ~mask);

    public byte Read(uint address)
    {
        var register = (IocRegister)(address & C.RegisterMask);
        if (register >= IocRegister.TimerBase)
        {
            var offset = (int)(register - IocRegister.TimerBase);
            var timer = offset / C.TimerStride;
            return (offset & C.TimerRegisterMask) switch
            {
                C.TimerLow => (byte)_timerLatched[timer],
                C.TimerHigh => (byte)(_timerLatched[timer] >> C.ByteBits),
                _ => 0
            };
        }
        switch (register)
        {
            case IocRegister.Control:
                return (byte)((_control & _controlInputs & C.ControlPins) |
                    (_fallingPin ? C.ControlFallingPin : 0) | (_risingPin ? C.ControlRisingPin : 0));
            case IocRegister.Keyboard:
                _irqB &= unchecked((byte)~C.KeyboardReceiveInterrupt);
                return _receiveData;
            case IocRegister.IrqStatusA: return StatusA;
            case IocRegister.IrqRequestA: return (byte)(StatusA & _maskA);
            case IocRegister.IrqMaskA: return _maskA;
            case IocRegister.IrqStatusB: return StatusB;
            case IocRegister.IrqRequestB: return (byte)(_irqB & _maskB);
            case IocRegister.IrqMaskB: return _maskB;
            case IocRegister.FiqStatus: return StatusFiq;
            case IocRegister.FiqRequest: return (byte)(StatusFiq & _maskFiq);
            case IocRegister.FiqMask: return _maskFiq;
            default: return 0;
        }
    }

    public void Write(uint address, byte value)
    {
        var register = (IocRegister)(address & C.RegisterMask);
        if (register >= IocRegister.TimerBase)
        {
            var offset = (int)(register - IocRegister.TimerBase);
            var timer = offset / C.TimerStride;
            switch (offset & C.TimerRegisterMask)
            {
                case C.TimerLow: _timerReload[timer] = (ushort)((_timerReload[timer] & C.HighByteMask) | value); break;
                case C.TimerHigh: _timerReload[timer] = (ushort)((_timerReload[timer] & C.LowByteMask) | (value << C.ByteBits)); break;
                case C.TimerGo: _timerRemaining[timer] = _timerReload[timer] + 1; break;
                case C.TimerLatch: _timerLatched[timer] = (ushort)(_timerRemaining[timer] - 1); break;
            }
            return;
        }
        switch (register)
        {
            case IocRegister.Control:
                _control = value;
                ControlWritten?.Invoke(value);
                break;
            case IocRegister.Keyboard:
                _transmitData = value;
                _transmitRemaining = SerialHalfBitTicks * C.SerialFrameHalfBits;
                _irqB &= unchecked((byte)~C.KeyboardTransmitInterrupt);
                break;
            case IocRegister.IrqRequestA: _irqA &= (byte)~(value & C.IrqLatchedMask); break;
            case IocRegister.IrqMaskA: _maskA = value; break;
            case IocRegister.IrqMaskB: _maskB = value; break;
            case IocRegister.FiqMask: _maskFiq = value; break;
        }
    }

    private int SerialHalfBitTicks => (_timerReload[C.KeyboardTimer] + 1) * C.SerialTicksPerHalfBit;

    public bool ReceiveKeyboardByte(byte value)
    {
        if (KeyboardReceiveBusy) return false;
        _receiveShift = value;
        _receiveRemaining = SerialHalfBitTicks * C.SerialFrameHalfBits;
        _receiveSignalled = false;
        return true;
    }

    /// <summary>Avance l'IOC en ticks 2 MHz, indépendamment de la fréquence du CPU.</summary>
    public void Advance(int ticks)
    {
        if (ticks <= 0) return;
        for (var timer = 0; timer < C.TimerCount; timer++)
        {
            var remaining = _timerRemaining[timer] - ticks;
            if (remaining > 0) { _timerRemaining[timer] = remaining; continue; }
            var period = _timerReload[timer] + 1;
            var transitions = 1 + (-remaining / period);
            _timerRemaining[timer] = period - (-remaining % period);
            if (timer < C.InterruptTimerCount) _irqA |= (byte)(C.TimerZeroInterrupt << timer);
            else if (timer == C.BaudTimer) BaudTransitions?.Invoke(transitions);
        }
        if (_receiveRemaining > 0)
        {
            _receiveRemaining = Math.Max(0, _receiveRemaining - ticks);
            if (!_receiveSignalled && _receiveRemaining <= SerialHalfBitTicks * (C.SerialFrameHalfBits - C.ReceiveInterruptHalfBits))
            {
                _receiveData = _receiveShift;
                _receiveSignalled = true;
                _irqB |= C.KeyboardReceiveInterrupt;
            }
        }
        if (_transmitRemaining > 0)
        {
            _transmitRemaining = Math.Max(0, _transmitRemaining - ticks);
            if (_transmitRemaining == 0)
            {
                _irqB |= C.KeyboardTransmitInterrupt;
                KeyboardTransmitted?.Invoke(_transmitData);
            }
        }
    }
}

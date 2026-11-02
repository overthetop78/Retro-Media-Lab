using System.Numerics;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Ns16450.Constants.Ns16450Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Ns16450.Services;

internal sealed partial class Ns16450
{
    private bool _rxd = true;
    private bool _transmitLine = true;
    private bool _transmitActive;
    private uint _transmitFrame;
    private int _transmitRemaining;
    private int _transmitTicks;
    private int _transmitStopTicks;
    private int _receiveStage = C.IdleReceive;
    private int _receiveTicks;
    private int _receiveValue;
    private byte _receiveControl;
    private bool _receiveParityError;
    private bool _receiveBreak;
    private int _lowTicks;
    private int _lowFrameTicks;
    private int _markTicks;

    private bool ReceiveInput => Loopback
        ? _transmitLine && (_lineControl & C.Break) == 0 : _rxd;

    private bool ReceiverCanSkip => _receiveStage == C.IdleReceive && _receiveTicks == 0
        && _lowTicks == 0 && !_receiveBreak && ReceiveInput;

    private bool CanSkipClocks => !_transmitActive && !_holdingFull
        && (_externalReceiveClock || ReceiverCanSkip);

    internal void SetRxd(bool high) => _rxd = high;

    /// <summary>Board clocks. Source/divider remains rational, including the UPC 24 MHz / 13 clock.</summary>
    internal void Advance(int ticks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ticks);
        if (_divisor == 0 || ticks == 0) return;
        var period = (long)_ticksPerSecond * _clockDivider * _divisor;
        var elapsed = (long)ticks * _sourceClock;
        ulong clocks;
        if (elapsed <= long.MaxValue - _clockPhase)
        {
            elapsed += _clockPhase;
            clocks = (ulong)(elapsed / period);
            _clockPhase = elapsed % period;
        }
        else
        {
            var total = (UInt128)(ulong)elapsed + (ulong)_clockPhase;
            clocks = (ulong)(total / (ulong)period);
            _clockPhase = (long)(total % (ulong)period);
        }
        while (clocks > 0 && !CanSkipClocks)
        {
            clocks--;
            TransmitClock();
            if (!_externalReceiveClock) ReceiveClock();
        }
    }

    /// <summary>Rising edges on a separately wired RCLK input, already at sixteen times the baud rate.</summary>
    internal void AdvanceReceiveClock(int risingEdges)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(risingEdges);
        if (!_externalReceiveClock) return;
        for (var edge = 0; edge < risingEdges && !ReceiverCanSkip; edge++) ReceiveClock();
    }

    private void ResetSerial()
    {
        _transmitLine = true;
        _transmitActive = false;
        _transmitFrame = 0;
        _transmitRemaining = _transmitTicks = _transmitStopTicks = 0;
        _receiveStage = C.IdleReceive;
        _receiveTicks = _receiveValue = _lowTicks = _lowFrameTicks = _markTicks = 0;
        _receiveControl = 0;
        _receiveParityError = _receiveBreak = false;
    }

    private static int DataBits(byte control) => C.MinimumDataBits + (control & C.WordLengthMask);

    private static int StopTicks(byte control) => (control & C.ExtraStop) == 0 ? C.Oversampling
        : DataBits(control) == C.MinimumDataBits ? C.Oversampling + C.HalfBit : C.Oversampling * 2;

    private static bool ParityBit(int data, byte control)
    {
        var even = (control & C.EvenParity) != 0;
        if ((control & C.StickParity) != 0) return !even;
        var odd = (BitOperations.PopCount((uint)data) & 1) != 0;
        return even ? odd : !odd;
    }

    private void TransmitClock()
    {
        if (_transmitActive)
        {
            if (--_transmitTicks > 0) return;
            if (_transmitRemaining > 0)
            {
                _transmitLine = (_transmitFrame & 1) != 0;
                _transmitFrame >>= 1;
                _transmitRemaining--;
                _transmitTicks = _transmitRemaining == 0 ? _transmitStopTicks : C.Oversampling;
                RefreshOutputs();
                return;
            }
            _transmitActive = false;
        }
        if (!_holdingFull)
        {
            _lineStatus |= C.TransmitterEmpty;
            _transmitLine = true;
            RefreshOutputs();
            return;
        }
        var bits = DataBits(_lineControl);
        var data = _holding & ((1 << bits) - 1);
        _transmitFrame = (uint)data;
        if ((_lineControl & C.ParityEnable) != 0)
        {
            if (ParityBit(data, _lineControl)) _transmitFrame |= 1u << bits;
            bits++;
        }
        _transmitFrame |= 1u << bits;
        _transmitRemaining = bits + 1;
        _transmitStopTicks = StopTicks(_lineControl);
        _transmitTicks = C.Oversampling;
        _transmitActive = true;
        _transmitLine = false;
        _holdingFull = false;
        _lineStatus |= C.HoldingEmpty;
        _lineStatus &= unchecked((byte)~C.TransmitterEmpty);
        _transmitInterrupt = true;
        RefreshOutputs();
        UpdateInterrupt();
    }

    private void ReceiveClock()
    {
        var high = ReceiveInput;
        if (high)
        {
            _lowTicks = 0;
            if (_receiveBreak)
            {
                if (++_markTicks >= C.HalfBit) _receiveBreak = false;
                return;
            }
        }
        else
        {
            _markTicks = 0;
            if (_lowTicks == 0)
                _lowFrameTicks = C.Oversampling * (1 + DataBits(_lineControl)
                    + ((_lineControl & C.ParityEnable) != 0 ? 1 : 0)) + StopTicks(_lineControl);
            if (_lowTicks <= _lowFrameTicks) _lowTicks++;
            if (!_receiveBreak && _lowTicks > _lowFrameTicks)
            {
                _receiveBreak = true;
                _receiveStage = C.IdleReceive;
                _receiveTicks = 0;
                _lineStatus |= C.BreakInterrupt;
                UpdateInterrupt();
            }
            if (_receiveBreak) return;
        }
        if (_receiveStage == C.ResynchronizingReceive)
        {
            if (high)
            {
                _receiveStage = C.IdleReceive;
                _receiveTicks = 0;
                return;
            }
            if (++_receiveTicks < C.ResynchronizationSamples) return;
            _receiveControl = _lineControl;
            _receiveStage = 0;
            _receiveValue = 0;
            _receiveParityError = false;
            return;
        }
        if (_receiveStage == C.IdleReceive)
        {
            if (high) { _receiveTicks = 0; return; }
            if (++_receiveTicks < C.HalfBit) return;
            _receiveControl = _lineControl;
            _receiveStage = 0;
            _receiveTicks = _receiveValue = 0;
            _receiveParityError = false;
            return;
        }
        if (++_receiveTicks < C.Oversampling) return;
        _receiveTicks = 0;
        var bits = DataBits(_receiveControl);
        if (_receiveStage < bits)
        {
            if (high) _receiveValue |= 1 << _receiveStage;
            _receiveStage++;
            return;
        }
        if ((_receiveControl & C.ParityEnable) != 0 && _receiveStage == bits)
        {
            _receiveParityError = high != ParityBit(_receiveValue, _receiveControl);
            _receiveStage++;
            return;
        }
        var errors = (_receiveParityError ? C.ParityError : 0) | (!high ? C.FramingError : 0)
            | ((_lineStatus & C.DataReady) != 0 ? C.OverrunError : 0);
        _received = (byte)_receiveValue;
        _lineStatus |= (byte)(C.DataReady | errors);
        _receiveStage = high ? C.IdleReceive : C.ResynchronizingReceive;
        UpdateInterrupt();
    }
}

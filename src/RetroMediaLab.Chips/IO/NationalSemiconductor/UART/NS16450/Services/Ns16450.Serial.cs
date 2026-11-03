using System.Numerics;
using C = RetroMediaLab.Chips.IO.NationalSemiconductor.UART.NS16450.Constants.Ns16450Constants;

namespace RetroMediaLab.Chips.IO.NationalSemiconductor.UART.NS16450.Services;

public sealed partial class Ns16450
{
    private bool ReceiveInput => Loopback
        ? _transmitLine && (_lineControl & C.Break) == 0 : _rxd;

    private bool ReceiverCanSkip => _receiveStage == C.IdleReceive && _receiveTicks == 0
        && _lowTicks == 0 && !_receiveBreak && _receiveInterruptTicks == 0 && ReceiveInput;

    private bool CanSkipClocks => _transmitIdle && (_externalReceiveClock || ReceiverCanSkip);

    public void SetRxd(bool high) => _rxd = high;

    /// <summary>Board clocks. Source/divider remains rational, including the UPC 24 MHz / 13 clock.</summary>
    public void Advance(int ticks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ticks);
        if (_clockPeriod == 0 || ticks == 0) return;
        var budget = (long)ticks * _sourceClock;
        if (BaudOutChanged is not null || _baudHalfPhase)
        {
            AdvanceObservedBaud((UInt128)(ulong)budget * C.HalfClocksPerSourceClock);
            return;
        }
        if (_reset || CanSkipClocks)
        {
            AdvanceIdlePhase(budget);
            return;
        }
        while (budget > 0 && !_reset && _clockPeriod != 0)
        {
            if (BaudOutChanged is not null)
            {
                AdvanceObservedBaud((UInt128)(ulong)budget * C.HalfClocksPerSourceClock);
                return;
            }
            var distance = _clockPeriod - _clockPhase;
            if (budget < distance)
            {
                _clockPhase += budget;
                return;
            }
            if (CanSkipClocks)
            {
                // Do not iterate idle baud edges, but preserve the divider's phase.
                AdvanceIdlePhase(budget);
                return;
            }
            if (budget - distance >= _clockPeriod && SkipQuietClocks(ref budget, distance)) continue;
            budget -= distance;
            _clockPhase = 0;
            TransmitClock();
            if (!_reset && !_externalReceiveClock) ReceiveClock();
            // A pin callback may have reloaded either divisor latch or asserted MR.
            // The remaining source-clock budget must use that new state.
        }
        if (budget > 0 && _reset && _clockPeriod != 0)
        {
            if (BaudOutChanged is not null)
                AdvanceObservedBaud((UInt128)(ulong)budget * C.HalfClocksPerSourceClock);
            else AdvanceIdlePhase(budget);
        }
    }

    private void ReloadClockPeriod(bool previousBaudOut)
    {
        _clockPeriod = (long)_ticksPerSecond * _clockDivider * _divisor;
        _clockPhase = 0;
        _baudHalfPhase = false;
        if (!previousBaudOut) BaudOutChanged?.Invoke(true);
    }

    private void AdvanceIdlePhase(long budget)
    {
        if (budget <= long.MaxValue - _clockPhase)
        {
            var phase = budget + _clockPhase;
            _clockPhase = phase < _clockPeriod ? phase : phase == _clockPeriod ? 0 : phase % _clockPeriod;
        }
        else
            _clockPhase = (long)(((UInt128)(ulong)budget + (ulong)_clockPhase) % (ulong)_clockPeriod);
    }

    private bool SkipQuietClocks(ref long budget, long distance)
    {
        if ((!_externalReceiveClock && !ReceiverCanSkip) || _busWriteActive) return false;
        var next = int.MaxValue;
        if (_transmitActive) next = _transmitTicks;
        if (_transmitStartTicks != 0) next = Math.Min(next, _transmitStartTicks);
        if (!_externalReceiveClock && _transmitEmptyTicks != 0) next = Math.Min(next, _transmitEmptyTicks);
        if (_transmitInterruptTicks != 0) next = Math.Min(next, _transmitInterruptTicks);
        if (next <= 1) return false;
        var count = (int)Math.Min(next - 1L, 1 + (budget - distance) / _clockPeriod);
        // Every skipped edge precedes all deadlines. No pins, receiver samples or IRQs can change.
        budget -= distance + (count - 1L) * _clockPeriod;
        _clockPhase = 0;
        if (_transmitActive) _transmitTicks -= count;
        if (_transmitStartTicks != 0) _transmitStartTicks -= count;
        if (!_externalReceiveClock && _transmitEmptyTicks != 0) _transmitEmptyTicks -= count;
        if (_transmitInterruptTicks != 0) _transmitInterruptTicks -= count;
        return true;
    }

    /// <summary>Rising edges on a separately wired RCLK input, already at sixteen times the baud rate.</summary>
    public void AdvanceReceiveClock(int risingEdges)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(risingEdges);
        if (_reset || !_externalReceiveClock) return;
        for (var edge = 0; edge < risingEdges && !_reset
            && (!ReceiverCanSkip || _transmitEmptyTicks != 0); edge++)
        {
            TransmitterEmptyClock();
            ReceiveClock();
        }
    }

    private void ResetSerial()
    {
        _transmitLine = true;
        _transmitIdle = true;
        _transmitActive = false;
        _transmitFrame = 0;
        _transmitRemaining = _transmitTicks = _transmitStopTicks = 0;
        _transmitStartTicks = _transmitEmptyTicks = _transmitInterruptTicks = 0;
        _receiveStage = C.IdleReceive;
        _receiveTicks = _receiveValue = _lowTicks = _lowFrameTicks = _markTicks = 0;
        _receiveControl = 0;
        _receiveParityError = _receiveBreak = false;
        _receiveInterruptTicks = 0;
        _receiveInterruptStatus = _pendingReceiveInterruptStatus = 0;
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

}

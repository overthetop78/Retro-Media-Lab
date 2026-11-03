using C = RetroMediaLab.Chips.IO.NationalSemiconductor.UART.NS16450.Constants.Ns16450Constants;

namespace RetroMediaLab.Chips.IO.NationalSemiconductor.UART.NS16450.Services;

public sealed partial class Ns16450
{
    private bool _baudHalfPhase;
    private bool _receiveClockHigh;

    public Action<bool>? BaudOutChanged { get; set; }

    // National waveform TL/C/8401-4: /1 is symmetrical, /2 low for one source
    // cycle, /3 and larger low for two source cycles. Do not invent a 50% duty cycle.
    private UInt128 BaudPeriod => (UInt128)(ulong)_clockPeriod * C.HalfClocksPerSourceClock;
    private UInt128 BaudPhase => (UInt128)(ulong)_clockPhase * C.HalfClocksPerSourceClock
        + (_baudHalfPhase ? (uint)1 : 0);
    private UInt128 BaudLowWidth => (UInt128)((long)_ticksPerSecond * _clockDivider)
        * (uint)(_divisor == 1 ? 1
            : Math.Min(_divisor - 1, C.MaximumBaudLowSourceClocks) * C.HalfClocksPerSourceClock);

    public bool BaudOutHigh => _clockPeriod == 0 || BaudPhase < BaudPeriod - BaudLowWidth;

    public void SetReceiveClock(bool high)
    {
        var rising = high && !_receiveClockHigh;
        _receiveClockHigh = high;
        if (rising) AdvanceReceiveClock(1);
    }

    private void SetBaudPhase(UInt128 phase)
    {
        _clockPhase = (long)(phase / C.HalfClocksPerSourceClock);
        _baudHalfPhase = phase % C.HalfClocksPerSourceClock != 0;
    }

    private void AdvanceObservedBaud(UInt128 budget)
    {
        // Only an observer requires both output edges. The ordinary serial path
        // retains its whole-period batching; both paths share the same phase.
        while (budget != 0 && _clockPeriod != 0)
        {
            var high = BaudOutHigh;
            var phase = BaudPhase;
            var end = high ? BaudPeriod - BaudLowWidth : BaudPeriod;
            var distance = end - phase;
            if (budget < distance)
            {
                SetBaudPhase(phase + budget);
                return;
            }
            budget -= distance;
            if (high)
            {
                SetBaudPhase(end);
                BaudOutChanged?.Invoke(false);
            }
            else
            {
                SetBaudPhase(0);
                if (!_reset)
                {
                    TransmitClock();
                    if (!_reset && !_externalReceiveClock) ReceiveClock();
                }
                BaudOutChanged?.Invoke(BaudOutHigh);
            }
            // Recompute phase and period after callbacks: a board may reload DLL/DLM.
        }
    }
}

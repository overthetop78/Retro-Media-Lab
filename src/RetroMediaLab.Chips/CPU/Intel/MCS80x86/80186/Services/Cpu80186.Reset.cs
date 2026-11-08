using static RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Constants.Cpu80186Constants;

namespace RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Services;

public sealed partial class Cpu80186
{
    private bool _resetInput;
    private bool _resetOutput;
    private int _resetReleaseHalfClocks;
    private int _clockHalfPhase;

    public long ClockHalfCount => checked(Cycles * HalfClocksPerCycle + _clockHalfPhase);
    public bool ResetOutputHigh => _resetOutput;
    public Action<bool>? ResetOutputChanged { get; set; }

    /// <summary>RES active, normalized from the physical low level. Register state belongs to the CPU.</summary>
    public void SetReset(bool asserted)
    {
        if (_resetInput == asserted) return;
        _resetInput = asserted;
        if (asserted)
        {
            Reset();
            SetResetOutput(true);
        }
        else _resetReleaseHalfClocks = ResetReleaseHalfClocks;
    }

    private void SetResetOutput(bool high)
    {
        if (_resetOutput == high) return;
        _resetOutput = high;
        ResetOutputChanged?.Invoke(high);
    }

    private bool AdvanceReset()
    {
        if (!_resetInput && _resetReleaseHalfClocks == 0) return false;
        var halfClocks = _resetInput ? HalfClocksPerCycle
            : Math.Min(HalfClocksPerCycle, _resetReleaseHalfClocks);
        var wholeClocks = (_clockHalfPhase + halfClocks) / HalfClocksPerCycle;
        _clockHalfPhase = (_clockHalfPhase + halfClocks) % HalfClocksPerCycle;
        Cycles += wholeClocks;
        // RESET holds the instruction sequencer, DMA and timers, not CLKOUT.
        // The retained half-cycle places the next prefetch 6.5 CLKOUT after RES release.
        if (!_resetInput)
        {
            _resetReleaseHalfClocks -= halfClocks;
            if (_resetReleaseHalfClocks <= ResetReleaseHalfClocks - ResetOutputReleaseHalfClocks)
                SetResetOutput(false);
        }
        if (wholeClocks != 0) BusClocksAdvanced?.Invoke(wholeClocks);
        return true;
    }
}

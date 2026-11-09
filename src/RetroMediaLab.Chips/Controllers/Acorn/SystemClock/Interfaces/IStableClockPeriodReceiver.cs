namespace RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Interfaces;

/// <summary>A clock input accepting exact, output-stable complete periods.</summary>
public interface IStableClockPeriodReceiver
{
    void SetClock(bool high);
    /// <summary>Accept up to the offered low-to-low periods, without any observable
    /// output notification. Retain all internal phases, counters and elapsed time.
    /// Return zero at an input/event boundary; the source then delivers real edges.</summary>
    int AdvanceStablePeriods(int maximumPeriods);
}

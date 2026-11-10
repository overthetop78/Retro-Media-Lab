namespace RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Interfaces;

/// <summary>A receiver accepting complete clock periods including output changes.</summary>
/// <remarks>During the accepted interval its outputs must neither affect nor read
/// the source or the other connected domains. All its own fronts, responses and
/// notifications retain their exact local times. The source bounds delivery at
/// the next possible interaction; this is not permission to omit clock edges.</remarks>
public interface IIndependentClockPeriodReceiver : IStableClockPeriodReceiver
{
    /// <summary>Consume exactly this many complete periods, preserving the given
    /// initial CLK level. Local stable portions may use the existing fast path.</summary>
    void AdvancePeriods(int periods, bool startHigh);
}

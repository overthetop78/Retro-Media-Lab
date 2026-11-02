namespace RetroMediaLab.Emulation.Functions;

internal static class EmulationExecutionRateFunctions
{
    private const double HertzPerMegahertz = 1_000_000d;

    internal static EmulationExecutionRate? Measure(TimeSpan emulatedElapsed, TimeSpan realElapsed, long? processorClockHz)
    {
        if (emulatedElapsed <= TimeSpan.Zero || realElapsed <= TimeSpan.Zero) return null;
        var ratio = emulatedElapsed.TotalSeconds / realElapsed.TotalSeconds;
        return new(ratio, processorClockHz is > 0 ? processorClockHz.Value * ratio / HertzPerMegahertz : null);
    }
}

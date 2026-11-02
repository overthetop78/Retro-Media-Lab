using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Contracts;

namespace RetroMediaLab.Tests.Emulation;

public sealed class EmulationExecutionRateTests
{
    [Theory]
    [InlineData(1, 1, 2_000_000, 1, 2)]
    [InlineData(0.5, 1, 2_000_000, 0.5, 1)]
    [InlineData(32, 1, 2_000_000, 32, 64)]
    [InlineData(1, 2, 8_000_000, 0.5, 4)]
    public void UsesEmulatedTimeRatherThanPresentedFrameCount(double emulated, double real, long clock, double ratio, double megahertz)
    {
        var rate = EmulationSdk.MachineExecution.MeasureExecutionRate(TimeSpan.FromSeconds(emulated), TimeSpan.FromSeconds(real), clock);
        Assert.NotNull(rate);
        Assert.Equal(ratio, rate.Value.SpeedRatio, 8);
        Assert.Equal(megahertz, rate.Value.EffectiveMegahertz!.Value, 8);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    public void RejectsPauseAndResetIntervals(double emulated, double real) =>
        Assert.Null(EmulationSdk.MachineExecution.MeasureExecutionRate(TimeSpan.FromSeconds(emulated), TimeSpan.FromSeconds(real), 2_000_000));

    [Fact]
    public void UnknownProcessorFrequencyDoesNotInventMegahertz()
    {
        var rate = EmulationSdk.MachineExecution.MeasureExecutionRate(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1), null);
        Assert.Equal(2, rate!.Value.SpeedRatio);
        Assert.Null(rate.Value.EffectiveMegahertz);
    }
}

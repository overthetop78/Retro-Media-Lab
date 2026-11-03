using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Functions;

namespace RetroMediaLab.Tests.Media;

public sealed class MediaFluxWindowTests
{
    [Fact]
    public void ClosingTheGateAfterTheFinalInstantErasesItsOldTransition()
    {
        var source = new MediaFluxRevolutionData(100, [20, 20, 20]);
        var result = MediaFluxTrackDataFunctions.ReplaceWindow(source, 20, 20, [], includeEndingInstant: true);
        Assert.Equal(new uint[] { 60 }, Positions(result));
    }

    [Fact]
    public void ATransitionEmittedBeforeGateClosureSurvivesAtTheFinalInstant()
    {
        var source = new MediaFluxRevolutionData(100, [20, 20, 20]);
        var result = MediaFluxTrackDataFunctions.ReplaceWindow(source, 20, 20, [20], includeEndingInstant: true);
        Assert.Equal(new uint[] { 40, 60 }, Positions(result));
    }

    [Fact]
    public void ClosingOnALaterVisitErasesTheEarlierPulseAtTheSamePosition()
    {
        var source = new MediaFluxRevolutionData(100, []);
        var result = MediaFluxTrackDataFunctions.ReplaceWindow(source, 0, 120, [20], includeEndingInstant: true);
        Assert.Empty(result.FluxIntervalsNanoseconds);
    }

    [Fact]
    public void OnlyTheOpenWriteWindowIsErasedAndTheInputRemainsUntouched()
    {
        var source = new MediaFluxRevolutionData(100, [10, 10, 10, 10, 10, 10, 10, 10, 10, 10]);
        var result = MediaFluxTrackDataFunctions.ReplaceWindow(source, 25, 30, [2, 9, 20]);
        Assert.Equal(new uint[] { 10, 20, 27, 34, 45, 60, 70, 80, 90, 100 }, Positions(result));
        Assert.Equal(Enumerable.Repeat(10u, 10), source.FluxIntervalsNanoseconds);
        Assert.Equal(100u, result.IndexTimeNanoseconds);
    }

    [Fact]
    public void AnInterruptedWriteUsesOnlyThePulsesActuallyReceived()
    {
        var source = new MediaFluxRevolutionData(100, [10, 10, 10, 10, 10, 10, 10, 10, 10, 10]);
        var result = MediaFluxTrackDataFunctions.ReplaceWindow(source, 20, 15, [0, 7]);
        Assert.Equal(new uint[] { 10, 20, 27, 40, 50, 60, 70, 80, 90, 100 }, Positions(result));
    }

    [Fact]
    public void TheGateErasesAcrossIndexAndLeavesTheOppositeArcUnchanged()
    {
        var source = new MediaFluxRevolutionData(100, [10, 10, 10, 10, 10, 10, 10, 10, 10, 10]);
        var result = MediaFluxTrackDataFunctions.ReplaceWindow(source, 85, 40, [5, 15, 25, 35]);
        Assert.Equal(new uint[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 }, Positions(result));
        var erased = MediaFluxTrackDataFunctions.ReplaceWindow(source, 85, 40, []);
        Assert.Equal(new uint[] { 30, 40, 50, 60, 70, 80 }, Positions(erased));
    }

    [Fact]
    public void TheEndingEdgeIsExcludedAndTheStartingEdgeIsIncluded()
    {
        var source = new MediaFluxRevolutionData(100, [20, 20, 20, 20]);
        var result = MediaFluxTrackDataFunctions.ReplaceWindow(source, 20, 20, []);
        Assert.Equal(new uint[] { 40, 60, 80 }, Positions(result));
    }

    [Fact]
    public void NoTransitionIsInventedAtTheEndOfASilentRevolution()
    {
        var source = new MediaFluxRevolutionData(100, [10, 20]);
        var result = MediaFluxTrackDataFunctions.ReplaceWindow(source, 20, 80, []);
        Assert.Equal(new uint[] { 10 }, Positions(result));
        Assert.Equal(100u, result.IndexTimeNanoseconds);
    }

    [Fact]
    public void AFullRevolutionCanBeCompletelyErased()
    {
        var source = new MediaFluxRevolutionData(100, [10, 20, 30]);
        Assert.Empty(MediaFluxTrackDataFunctions.ReplaceWindow(source, 35, 100, []).FluxIntervalsNanoseconds);
    }

    [Fact]
    public void LaterPassesOverwriteEarlierPulsesAtTheSamePhysicalPosition()
    {
        var source = new MediaFluxRevolutionData(100, [10, 20, 30]);
        var result = MediaFluxTrackDataFunctions.ReplaceWindow(source, 0, 230, [10, 40, 110, 180, 220]);
        // Seules les impulsions émises pendant le dernier passage de leur position survivent.
        Assert.Equal(new uint[] { 20, 80 }, Positions(result));
    }

    [Theory]
    [InlineData(15u, new uint[] { 15 })]
    [InlineData(15u, new uint[] { 10, 5 })]
    [InlineData(15u, new uint[] { 5, 5 })]
    public void ImpossiblePulseSequencesAreRejected(uint duration, uint[] offsets)
        => Assert.Throws<ArgumentOutOfRangeException>(() => MediaFluxTrackDataFunctions.ReplaceWindow(
            new(100, [10, 20]), 0, duration, offsets));

    [Fact]
    public void ZeroDurationDoesNotEraseAnything()
    {
        var source = new MediaFluxRevolutionData(100, [10, 20]);
        Assert.Equal(source.FluxIntervalsNanoseconds,
            MediaFluxTrackDataFunctions.ReplaceWindow(source, 0, 0, []).FluxIntervalsNanoseconds);
    }

    private static uint[] Positions(MediaFluxRevolutionData revolution)
    {
        uint total = 0;
        return revolution.FluxIntervalsNanoseconds.Select(interval => total += interval).ToArray();
    }
}

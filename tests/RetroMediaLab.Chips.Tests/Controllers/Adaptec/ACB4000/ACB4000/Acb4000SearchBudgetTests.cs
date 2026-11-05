using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed class Acb4000SearchBudgetTests
{
    [Theory]
    [InlineData(9)]
    [InlineData(17)]
    [InlineData(18)]
    [InlineData(32)]
    [InlineData(33)]
    public void FourTrackBudgetsAreConsumedByIdentitiesRatherThanHostTicks(int sectors)
    {
        var budget = new Acb4000SearchBudget();
        budget.Begin(sectors);
        Assert.Equal(sectors * 4, budget.Remaining);
        for (var index = 1; index < sectors * 4; index++) Assert.Null(budget.ObserveId(true));
        Assert.Equal<byte?>(0x14, budget.ObserveId(true));
        Assert.Equal(0, budget.Remaining);
    }

    [Theory]
    [InlineData(true, false, 0x10)]
    [InlineData(false, true, 0x14)]
    public void FinalIdentityDeterminesTheErrorRatherThanAnEarlierBadId(bool firstValid, bool lastValid, byte error)
    {
        var budget = new Acb4000SearchBudget();
        budget.Begin(1);
        for (var index = 0; index < 3; index++) Assert.Null(budget.ObserveId(firstValid));
        Assert.Equal(error, budget.ObserveId(lastValid));
        budget.Begin(1);
        Assert.Equal(4, budget.Remaining);
        budget.Reset();
        Assert.Equal(0, budget.Remaining);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(34)]
    [InlineData(int.MaxValue)]
    public void InvalidGeometryCannotOverflowOrReplaceTheActiveBudget(int sectors)
    {
        var budget = new Acb4000SearchBudget();
        budget.Begin(33);
        Assert.Throws<ArgumentOutOfRangeException>(() => budget.Begin(sectors));
        Assert.Equal(132, budget.Remaining);
    }
}

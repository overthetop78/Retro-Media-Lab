using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed class Acb4000ReadSynchronizerTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(16, false)]
    [InlineData(55, false)]
    [InlineData(64, true)]
    [InlineData(96, true)]
    [InlineData(120, true)]
    [InlineData(128, false)]
    public void AddressMarkRequiresTheQualifiedSearchWindow(int zeroBits, bool expected)
    {
        var reader = new Acb4000ReadSynchronizer();
        Zeros(reader, zeroBits);
        Assert.Equal(expected, Mark(reader));
    }

    [Fact]
    public void AcquiredAddressMarkStaysLatchedUntilReadGateFalls()
    {
        var reader = new Acb4000ReadSynchronizer();
        Zeros(reader, 96);
        Assert.True(Mark(reader));
        Zeros(reader, 96);
        Assert.False(Mark(reader));
        reader.ClockCell(false, false);
        Zeros(reader, 96);
        Assert.True(Mark(reader));
    }

    [Fact]
    public void ExpiredWindowReturnsToOscillatorForFourByteTimes()
    {
        var reader = new Acb4000ReadSynchronizer();
        Zeros(reader, 128 + 32 + 96);
        Assert.True(Mark(reader));
    }

    [Fact]
    public void OnesBeforeQualificationRestartAcquisition()
    {
        var reader = new Acb4000ReadSynchronizer();
        Zeros(reader, 40);
        reader.ClockCell(false, true);
        reader.ClockCell(true, true);
        Zeros(reader, 40);
        Assert.False(Mark(reader));
        Zeros(reader, 96);
        Assert.True(Mark(reader));
    }

    private static void Zeros(Acb4000ReadSynchronizer reader, int bits)
    {
        for (var bit = 0; bit < bits; bit++)
        {
            Assert.False(reader.ClockCell(true, true));
            Assert.False(reader.ClockCell(false, true));
        }
    }

    private static bool Mark(Acb4000ReadSynchronizer reader)
    {
        var found = false;
        for (var bit = 15; bit >= 0; bit--)
            found |= reader.ClockCell((0x4489 & (1 << bit)) != 0, true);
        return found;
    }
}

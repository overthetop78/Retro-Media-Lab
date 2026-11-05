using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed partial class Acb4000CommandTests
{
    [Fact]
    public void PhysicalStatisticsCountTheSelectedPortAndDoNotCountBufferDiagnostics()
    {
        var first = new Disk();
        var second = new Disk();
        var target = CreateTarget(unit => unit == 0 ? first : second, 2_000_000);
        Assert.Equal(0, Run(target, Short((byte)Acb4000CommandCode.Read, count: 2)).Status);
        Assert.Equal(2, target.ReadBlocks(0));
        Assert.Equal(0, target.ReadBlocks(1));
        Assert.Equal(32, target.SeekCount(0));
        Assert.Equal(0, target.SeekCount(1));
        Assert.Equal(0, Run(target, Short((byte)Acb4000CommandCode.Write, unit: 1, count: 1), new byte[256]).Status);
        Assert.Equal(1, target.WrittenBlocks(1));
        Assert.Equal(0, target.WrittenBlocks(0));
        Assert.Equal(0, target.ReadBlocks(1));
        Assert.Equal(32, target.SeekCount(1));
        Assert.Equal(0, Run(target, Short((byte)Acb4000CommandCode.WriteBuffer), new byte[1024]).Status);
        Assert.Equal(0, Run(target, Short((byte)Acb4000CommandCode.ReadBuffer)).Status);
        Assert.Equal(2, target.ReadBlocks(0));
        Assert.Equal(1, target.WrittenBlocks(1));
        target.Reset();
        for (var unit = 0; unit < 2; unit++)
        {
            Assert.Equal(0, target.SeekCount(unit));
            Assert.Equal(0, target.ReadBlocks(unit));
            Assert.Equal(0, target.WrittenBlocks(unit));
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => target.ReadBlocks(2));
    }
}

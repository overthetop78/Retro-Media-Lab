using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed partial class Acb4000CommandTests
{
    [Theory]
    [InlineData(0, true, true)]
    [InlineData(1, true, true)]
    [InlineData(0, false, true)]
    [InlineData(1, false, true)]
    [InlineData(0, true, false)]
    [InlineData(1, true, false)]
    public void ReceiveWithoutDumpDoesNotLoadParametersOrTouchTheDisk(int unit, bool ready, bool formatted)
    {
        var disk = new Disk { Ready = ready, Formatted = formatted };
        var target = CreateTarget(index => index == unit ? disk : null, 2_000_000);
        var result = Run(target, Short((byte)Acb4000CommandCode.ReceiveDiagnostic, unit, count: 4));
        Assert.Equal(2, result.Status);
        Assert.Empty(result.Data);
        Assert.Equal(0x20, Run(target, Short((byte)Acb4000CommandCode.RequestSense, unit, count: 4)).Data[0]);
        Assert.Equal(0, disk.MechanicalSteps);
        Assert.Equal(0, disk.ParameterAcquisitions);
        Assert.Equal(0, disk.Writes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ReceiveWithoutDumpIsRejectedEvenWithoutAMechanism(int unit)
    {
        var target = CreateTarget(_ => null, 2_000_000);
        var result = Run(target, Short((byte)Acb4000CommandCode.ReceiveDiagnostic, unit, count: 4));
        Assert.Equal(2, result.Status);
        Assert.Empty(result.Data);
        Assert.Equal(0x20, Run(target, Short((byte)Acb4000CommandCode.RequestSense, unit, count: 4)).Data[0]);
    }
}

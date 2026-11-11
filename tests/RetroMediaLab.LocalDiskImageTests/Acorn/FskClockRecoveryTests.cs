using RetroMediaLab.MediaEngine.Functions;

namespace RetroMediaLab.MediaAudit.Acorn;

public sealed class FskClockRecoveryTests
{
    [Fact]
    public void EachRecordedSectionRecoversItsOwnTapeSpeed()
    {
        const int rate = 48000;
        byte[] expected = [0x2a, 0x00, 0x54, 0xff, 0x81, 0x2a, 0x00, 0x54, 0xff, 0x81];
        var samples = new List<short>();
        short level = -16000;
        FskSerialFunctions.AppendPcmCarrier(samples, ref level, rate, 315, 1260, 2520, 0.2);
        foreach (var value in expected.Take(5)) FskSerialFunctions.AppendPcmFrame(samples, ref level, rate, 315, 1260, 2520, 8, 1, value);
        FskSerialFunctions.AppendPcmCarrier(samples, ref level, rate, 285, 1140, 2280, 0.4);
        foreach (var value in expected.Skip(5)) FskSerialFunctions.AppendPcmFrame(samples, ref level, rate, 285, 1140, 2280, 8, 1, value);
        var decoded = FskSerialFunctions.Decode(samples.Select(value => (int)value).ToArray(), rate, 300, 1200, 2400, 8, 1, 8);
        Assert.Equal(expected, decoded.Bytes);
    }
}

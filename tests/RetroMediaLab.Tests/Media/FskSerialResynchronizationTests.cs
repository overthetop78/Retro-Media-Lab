using RetroMediaLab.MediaEngine.Functions;

namespace RetroMediaLab.Tests.Media;

public sealed class FskSerialResynchronizationTests
{
    private const int Baud = 1200;
    private const int ZeroTone = 1200;
    private const int OneTone = 2400;
    private const int DataBits = 8;
    private const int StopBits = 1;
    private const int MinimumSamples = 4;

    [Theory]
    [InlineData(48000, 1.0)]
    [InlineData(48000, 16_000_000.0 / 13 / 1024 / Baud)]
    [InlineData(44100, 1.02)]
    [InlineData(22050, 0.98)]
    public void ContinuousPhaseToneAndClockDriftResynchronizeForEveryCharacter(int sampleRate, double speed)
    {
        var expected = Enumerable.Range(0, 512).Select(index => (byte)(index ^ 0x5a)).ToArray();
        var samples = new List<int>();
        var phase = Math.PI / 7;
        var position = 0.0;
        void Tone(bool bit, double bitTimes)
        {
            position += sampleRate / (Baud * speed) * bitTimes;
            var frequency = (bit ? OneTone : ZeroTone) * speed;
            while (samples.Count < (int)Math.Round(position))
            {
                samples.Add((int)Math.Round(16000 * Math.Sin(phase)));
                phase += Math.Tau * frequency / sampleRate;
                if (phase >= Math.Tau) phase -= Math.Tau;
            }
        }
        Tone(true, 300);
        for (var index = 0; index < expected.Length; index++)
        {
            Tone(false, 1);
            for (var bit = 0; bit < DataBits; bit++) Tone((expected[index] & 1 << bit) != 0, 1);
            Tone(true, StopBits + (index % 5) * 0.13);
        }
        Tone(true, 2);
        var actual = FskSerialFunctions.Decode(samples, sampleRate, Baud, ZeroTone, OneTone, DataBits, StopBits, MinimumSamples);
        Assert.Equal(expected, actual.Bytes);
        Assert.Equal(expected.Length, actual.Positions.Count);
        Assert.True(actual.Positions.Zip(actual.Positions.Skip(1)).All(pair => pair.First < pair.Second));
    }

    [Fact]
    public void SilenceAndConstantLevelsAreNotSerialFrames()
    {
        foreach (var level in new[] { 0, -16000, 16000 })
            Assert.Empty(FskSerialFunctions.Decode(Enumerable.Repeat(level, 48000).ToArray(), 48000,
                Baud, ZeroTone, OneTone, DataBits, StopBits, MinimumSamples).Bytes);
    }
}

using BbcAudio = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services.Audio;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuAudioClockTests
{
    private const int InputValues = 256;
    private const int ResetInterval = 29;

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(45)]
    [InlineData(46)]
    [InlineData(137)]
    [InlineData(1337)]
    public void RetainedPeriodsProduceTheSamePcmAsObservationAfterEveryCycle(int interval)
    {
        var unit = new BbcAudio();
        var grouped = new BbcAudio();
        var expected = new List<short>();
        for (var value = 0; value < InputValues; value++)
        {
            if (value % ResetInterval == 0)
            {
                unit.Reset();
                grouped.Reset();
                Assert.Empty(unit.DrainSamples());
                Assert.Empty(grouped.DrainSamples());
            }
            unit.Write((byte)value);
            grouped.Write((byte)value);
            Advance();
            unit.SetBus((byte)~value, (value & 1) != 0);
            grouped.SetBus((byte)~value, (value & 1) != 0);
            Advance();
        }

        void Advance()
        {
            for (var tick = 0; tick < interval; tick++)
            {
                unit.Tick();
                // La consolidation forcée après chaque cycle conserve le chemin
                // de référence unitaire, sans regroupement d'intégrales.
                expected.AddRange(unit.DrainSamples());
                grouped.Tick();
            }
            Assert.Equal(expected, grouped.DrainSamples());
            expected.Clear();
            Assert.Empty(grouped.DrainSamples());
        }
    }
}

using System.Text.Json;

namespace RetroMediaLab.Tests.Emulation.Sega;

public sealed class Sg1000ProfilingTests
{
    [Fact]
    public void FrameStatisticsKeepMillisecondsPercentilesAndNominalSpeedDistinct()
    {
        Sg1000Profiling.FrameRow row = new(0, "Neutral", 0, 1, 0, 10, .25, 0, 0, 0,
            0, 0, 0, 0, 0, 0, true, false, false, false, 0, 0, 0, "", "");
        var result = Sg1000Profiling.Summarize([row, row with { WallMs = 9 }, row with { WallMs = 2 }, row with { WallMs = 8 }], 60);
        Assert.Equal(5, result.MedianMs); Assert.Equal(5, result.MeanMs); Assert.Equal(9, result.P95Ms);
        Assert.Equal(200, result.HostFps); Assert.Equal(1000d / 3, result.NominalPercent, 8);
        Assert.Equal(40, result.AllocatedBytes); Assert.Equal(1, result.GcPauseMs);
        Assert.Throws<ArgumentException>(() => Sg1000Profiling.Summarize([], 60));
    }

    [Fact]
    public void CompleteCpuCostsAttributePseudoLeavesAndNeverAddInclusiveChildren()
    {
        const string root = "retromedialab.emulation.sega!RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Services.EmulatorCore.RunFrame()";
        var trace = new
        {
            shared = new { frames = new[] { new { name = root }, new { name = "retromedialab.chips!RetroMediaLab.Chips.Memory.Memory.Read()" }, new { name = "CPU_TIME" }, new { name = "Outside" } } },
            profiles = new[] { new { type = "evented", unit = "milliseconds", startValue = 0,
                events = new[]
                {
                    new { type = "O", frame = 3, at = 0 }, new { type = "C", frame = 3, at = 4 },
                    new { type = "O", frame = 0, at = 4 }, new { type = "O", frame = 1, at = 5 },
                    new { type = "O", frame = 2, at = 5 }, new { type = "C", frame = 2, at = 7 },
                    new { type = "C", frame = 1, at = 7 }, new { type = "C", frame = 0, at = 9 }
                } } }
        };
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(trace));
        var result = Sg1000Profiling.AnalyzeCpu(document.RootElement);
        Assert.Equal(5, result.ScopedSampleMs);
        Assert.Equal(3, Assert.Single(result.Methods, item => item.Name == root).OwnMs);
        Assert.Equal(5, Assert.Single(result.Methods, item => item.Name == root).InclusiveMs);
        Assert.Equal(2, result.OwnGroupsMs["Memory"]);
        Assert.Equal(100, result.Methods.Sum(item => item.OwnPercent));
        Assert.DoesNotContain(result.Methods, item => item.Name is "Outside" or "CPU_TIME");
    }
}

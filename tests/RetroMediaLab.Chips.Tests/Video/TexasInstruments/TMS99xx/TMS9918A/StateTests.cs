using System.Text.Json;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Models;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.TexasInstruments.TMS99xx.TMS9918A;

public sealed class StateTests
{
    private static Tms9918A Create(byte r0 = 0, byte r1 = 0xe0, byte r7 = 3)
    {
        var chip = new Tms9918A(new(new(r0, r1, 6, 0x80, 0, 0x7e, 7, r7), 0,
            new(0x55, true), new(0, 0, Tms9918CpuVramOperation.Read),
            new(0, true, true, true, true), default, new(0, 168, 0)), Tms9918CpuTiming.SpecifiedMaximum);
        chip.SetVramReadData(0, 0xa5, true);
        return chip;
    }

    private static long Date(int edge) => (long)((Int128)edge * 1_000_000_000 / 21_477_270);
    private static void Run(Tms9918A chip, int first, int count)
        => chip.ReceiveMasterClockTrain(new(0, 1_000_000_000, 21_477_270, first, count));
    private static string Dump(Tms9918A chip) => JsonSerializer.Serialize(chip.CaptureState());

    [Fact]
    public void SerializedStateResumesBusRasterSpritesAndInterruptsWithoutRestoreEdges()
    {
        foreach (var mode in new (byte R0, byte R1)[] { (0, 0xe0), (2, 0xd0), (2, 0xc8), (0, 0xd8), (2, 0xd8), (2, 0xe0) })
        foreach (int prefix in new[] { 1, 3, 8, 15, 63, 4096, 9000 })
        {
            var original = Create(mode.R0, mode.R1);
            var restored = Create(mode.R0, mode.R1);
            if (mode == (2, 0xe0))
            {
                // Y=25 : clones8–31 présents au bloc2 (ligne168).
                original.SetVramReadData(0, 0x25, true);
                restored.SetVramReadData(0, 0x25, true);
            }
            var originalBus = new List<Tms9918VramSignal>();
            var restoredBus = new List<Tms9918VramSignal>();
            var originalVideo = new List<Tms9918VideoSignal>();
            var restoredVideo = new List<Tms9918VideoSignal>();
            var originalCpu = new List<Tms9918CpuOutputs>();
            var restoredCpu = new List<Tms9918CpuOutputs>();
            var originalGrom = new List<Tms9918ClockOutput>();
            var restoredGrom = new List<Tms9918ClockOutput>();
            try
            {
                Run(original, 0, prefix);
                original.AdvanceTo(Date(prefix - 1) + 10); // milieu d'une demi-période, parfois bus actif
                var snapshot = JsonSerializer.Deserialize<Tms9918AState>(Dump(original))!;
                original.VramPinsChanged = originalBus.Add; restored.VramPinsChanged = restoredBus.Add;
                original.VideoSignalProduced = originalVideo.Add; restored.VideoSignalProduced = restoredVideo.Add;
                original.CpuOutputsChanged = originalCpu.Add; restored.CpuOutputsChanged = restoredCpu.Add;
                original.GromClockChanged = originalGrom.Add; restored.GromClockChanged = restoredGrom.Add;
                restored.RestoreState(snapshot);
                Assert.Empty(restoredBus); Assert.Empty(restoredVideo); Assert.Empty(restoredCpu);
                Assert.Empty(restoredGrom);
                Assert.Equal(Dump(original), Dump(restored));
                snapshot.Sprites.SelectedNumbers[0] = 31;
                snapshot.Sprites.Active[0] = default;
                Assert.Equal(Dump(original), Dump(restored)); // aucune référence de tableau empruntée
                Run(original, prefix, 30_000);
                Run(restored, prefix, 30_000);
                Assert.Equal(originalBus, restoredBus);
                Assert.Equal(originalVideo, restoredVideo);
                Assert.Equal(originalCpu, restoredCpu);
                Assert.Equal(originalGrom, restoredGrom);
                Assert.NotEmpty(restoredGrom);
                Assert.Equal(Dump(original), Dump(restored));
                Assert.True(original.CaptureState().Control.AwaitingSecondByte);
            }
            finally
            {
                original.VramPinsChanged = restored.VramPinsChanged = null;
                original.VideoSignalProduced = restored.VideoSignalProduced = null;
                original.CpuOutputsChanged = restored.CpuOutputsChanged = null;
                original.GromClockChanged = restored.GromClockChanged = null;
            }
        }
    }

    [Fact]
    public void InvalidStateIsRejectedAtomicallyIncludingExternalAndTimingProfiles()
    {
        var chip = Create();
        Run(chip, 0, 4096);
        var good = chip.CaptureState();
        string before = Dump(chip);
        Tms9918AState[] invalid =
        [
            good with { Version = 1 },
            good with { Version = 2 },
            good with { Version = 3 },
            good with { Version = 4 },
            good with { SpriteCounter = 32 },
            good with { Clock = good.Clock with { GromHalfEdgePhase = 48 } },
            good with { CpuInterface = good.CpuInterface with { DataDue = good.TimeNanoseconds - 1 } },
            good with { Sprites = good.Sprites with { SelectedNumbers = new int[4] } },
            good with { Memory = good.Memory with { Address = 0x4000 } },
            good with { VramSequencer = good.VramSequencer with { SlotKind = 256 } },
            good with { CpuWriteBus = good.CpuWriteBus with { TimeNanoseconds = good.TimeNanoseconds + 1 } },
            good with { ExternalVideo = good.ExternalVideo with { VideoVoltage = double.NaN } },
            good with { CpuOutputs = good.CpuOutputs with { IntHigh = !good.CpuOutputs.IntHigh } },
            good with { CpuAccessNanoseconds = 100 },
        ];
        foreach (var state in invalid)
        {
            Assert.ThrowsAny<ArgumentException>(() => chip.RestoreState(state));
            Assert.Equal(before, Dump(chip));
        }
    }

    [Fact]
    public void ExternalVoltageAndTrilevelSyncKeepRegistersAndRestorePendingPixelSync()
    {
        var chip = Create(1, 0x80, 0);
        var restored = Create(1, 0x80, 0);
        var changes = new List<Tms9918VideoSignal>();
        try
        {
            chip.SetExternalVideoVoltage(0, 3, true);
            Run(chip, 0, 100);
            Assert.True(chip.VideoSignal.ExternalVideo);
            Assert.Equal(2.3, chip.VideoSignal.ExternalOutputVoltage!.Value, 8);
            chip.VideoSignalProduced = changes.Add;
            long time = chip.TimeNanoseconds + 10;
            chip.SetExternalVideoVoltage(time, 3.2, true);
            Assert.Single(changes);
            Assert.Equal(time, changes[0].TimeNanoseconds);
            Assert.Equal(2.5, changes[0].ExternalOutputVoltage!.Value, 8);
            chip.SetExternalVideoVoltage(time, 3.2, true);
            Assert.Single(changes);
            chip.SetResetSyncVoltage(time, 12, true);
            restored.RestoreState(chip.CaptureState());
            Run(chip, 100, 200); Run(restored, 100, 200);
            Assert.Equal(Dump(chip), Dump(restored));
            Assert.Contains(changes, signal => signal.Dot == 279 && signal.HorizontalSync);
            Assert.True(chip.CaptureState().ExternalVideo.VerticalSyncRecognized);
            Assert.Equal(219, chip.RasterState.Line);
            Assert.Equal((byte)1, chip.RegisterState.R0);
            Assert.Equal((byte)0x80, chip.RegisterState.R1);
            chip.SetResetSyncVoltage(chip.TimeNanoseconds, 5, true);
            chip.SetResetSyncVoltage(chip.TimeNanoseconds, 0, true);
            Run(chip, 300, 100);
            Assert.Equal((byte)0, chip.RegisterState.R0);
            Assert.Equal((byte)0, chip.RegisterState.R1);
            chip.SetResetSyncVoltage(chip.TimeNanoseconds, 5, true);
            var opaque = Create(1, 0x80, 5);
            opaque.SetExternalVideoVoltage(0, 3.2, true);
            Run(opaque, 0, 100);
            Assert.False(opaque.VideoSignal.ExternalVideo);
            Assert.Null(opaque.VideoSignal.ExternalOutputVoltage);
        }
        finally { chip.VideoSignalProduced = null; }
    }
}

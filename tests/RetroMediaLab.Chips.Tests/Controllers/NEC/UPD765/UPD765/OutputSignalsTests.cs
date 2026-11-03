using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;
using static RetroMediaLab.Chips.Tests.Shared.Upd765TestCommands;

namespace RetroMediaLab.Chips.Tests.Controllers.NEC.UPD765.UPD765;

public sealed class OutputSignalsTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ResetClearsDensitySideUnitAndHeadLoadOutputs(bool mfm)
    {
        var rig = new PinFixture(mfm); rig.Status[2] = 0x38;
        rig.Command(6, head: 1, unit: 2); rig.ReadyForTrack();
        Assert.Equal(mfm, rig.Chip.Mfm); Assert.Equal(1, rig.Chip.Head);
        Assert.Equal(2, rig.Chip.Unit); Assert.True(rig.Chip.HeadLoaded);
        rig.Chip.SetReset(true);
        Assert.False(rig.Chip.Mfm); Assert.Equal(0, rig.Chip.Head); Assert.Equal(0, rig.Chip.Unit);
        Assert.False(rig.Chip.HeadLoaded); Assert.False(rig.Chip.Sync); Assert.False(rig.Chip.LowCurrent);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void SenseAndVersionCommandsDoNotChangeTheLatchedDensity(bool mfm)
    {
        var rig = new PinFixture(mfm); rig.Sector(1, new byte[128]);
        Send(rig.Chip, mfm ? (byte)0x4a : (byte)0x0a, 0); rig.FeedTrack(); rig.Result();
        Send(rig.Chip, 4, 0); rig.Chip.ReadFifo(); Assert.Equal(mfm, rig.Chip.Mfm);
        Send(rig.Chip, 0x10); rig.Chip.ReadFifo(); Assert.Equal(mfm, rig.Chip.Mfm);
    }

    [Theory]
    [InlineData(41, false)] [InlineData(42, true)] [InlineData(43, true)]
    public void LowCurrentStartsAtCylinderFortyTwo(byte cylinder, bool expected)
    {
        var rig = new PinFixture(); rig.Command(6, cylinder: cylinder); rig.ReadyForTrack();
        Assert.Equal(expected, rig.Chip.LowCurrent);
        rig.Index(); rig.Index(); rig.Result(); Assert.False(rig.Chip.LowCurrent);
    }

    [Fact]
    public void SyncEnablesTheExternalPllWhileSearchingAndDisablesItAfterTheCommand()
    {
        var rig = new PinFixture(); rig.Sector(1, new byte[128]);
        Send(rig.Chip, 0x4a, 0); Assert.False(rig.Chip.Sync);
        rig.ReadyForTrack(); Assert.True(rig.Chip.Sync);
        rig.FeedTrack(); Assert.False(rig.Chip.Sync); rig.Result(); Assert.False(rig.Chip.Sync);
    }

    [Fact]
    public void HeadLoadIsAssertedDuringHltAndRetainedUntilHutExpires()
    {
        var rig = new PinFixture();
        // Specify HLT=2 ms and HUT=16 ms at the controller's native 8 MHz.
        Send(rig.Chip, 0x4a, 0);
        Assert.True(rig.Chip.FaultReset); Assert.False(rig.Chip.HeadLoaded);
        rig.Chip.Advance(64);
        Assert.True(rig.Chip.HeadLoaded); Assert.False(rig.Chip.Sync);
        rig.Chip.Advance(15999); Assert.True(rig.Chip.HeadLoaded); Assert.False(rig.Chip.Sync);
        rig.Chip.Advance(1); Assert.True(rig.Chip.Sync);
        rig.Index(); rig.Index(); Assert.True(rig.Chip.HeadLoaded);
        rig.Chip.Advance(127999); Assert.True(rig.Chip.HeadLoaded);
        rig.Chip.Advance(1); Assert.False(rig.Chip.HeadLoaded);
    }

    [Theory]
    [InlineData(false, 12, 24)]
    [InlineData(true, 21, 45)]
    public void SyncCoversIdAndDataButBlanksTheWriteSplice(bool mfm, int idEnd, int blankEnd)
    {
        var rig = new PinFixture(mfm) { TerminalAfter = 128 };
        rig.Sector(1, new byte[128]);
        var samples = new List<bool>();
        rig.WordObserved = _ => samples.Add(rig.Chip.Sync);
        rig.Command(6); rig.FeedTrack();
        Assert.True(samples[idEnd - 1]);
        Assert.All(samples.Skip(idEnd).Take(blankEnd - idEnd), sample => Assert.False(sample));
        Assert.True(samples[blankEnd]);
        Assert.True(samples[blankEnd + 5]);
        Assert.False(rig.Chip.Sync); rig.Result();
    }

    [Fact]
    public void WriteCommandsStillEnableSyncWhileFindingTheRecordedId()
    {
        var rig = new PinFixture(); rig.Command(5); rig.ReadyForTrack();
        Assert.True(rig.Chip.Sync);
    }

    [Theory]
    [InlineData(4_000_000)] [InlineData(8_000_000)]
    public void FaultResetPulseLastsSixtyFourNativeClocksBeforeHeadLoad(int clock)
    {
        var transitions = new List<bool>();
        var chip = new Upd765(
            _ => 0x38, (_, _) => { }, clockFrequency: clock);
        Specify(chip, false); chip.FaultResetChanged = transitions.Add;
        Send(chip, 0x4a, 0);
        Assert.True(chip.FaultReset); Assert.False(chip.HeadLoaded);
        chip.Advance(63); Assert.True(chip.FaultReset); Assert.False(chip.HeadLoaded);
        chip.Advance(1); Assert.False(chip.FaultReset); Assert.True(chip.HeadLoaded);
        Assert.Equal(new[] { true, false }, transitions);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void FaultResetAllowsTheExternalDriveToClearItsFault(bool clearFault)
    {
        var rig = new PinFixture(); rig.Status[0] |= 0x80;
        rig.Chip.FaultResetChanged = high => { if (high && clearFault) rig.Status[0] &= 0x7f; };
        rig.Command(6); Assert.True(rig.Chip.FaultReset);
        rig.Chip.Advance(64); Assert.False(rig.Chip.FaultReset);
        if (clearFault) { Assert.True(rig.Chip.Active); Assert.True(rig.Chip.HeadLoaded); }
        else { Assert.False(rig.Chip.Active); Assert.Equal(0x50, rig.Result()[0]); }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ResetFromEitherFaultResetEdgeCannotRestartTheCommand(bool resetAtHigh)
    {
        var rig = new PinFixture();
        rig.Chip.FaultResetChanged = high => { if (high == resetAtHigh) rig.Chip.SetReset(true); };
        Send(rig.Chip, 0x4a, 0); rig.Chip.Advance(64);
        Assert.Equal(0, rig.Chip.ReadMainStatus()); Assert.False(rig.Chip.Active);
        Assert.False(rig.Chip.FaultReset); Assert.False(rig.Chip.HeadLoaded);
    }
    [Theory]
    [InlineData(Upd765Model.Upd765A, 4800)]
    [InlineData(Upd765Model.Upd765B, 4800)]
    [InlineData(Upd765Model.Upd72065B, 800)]
    public void IndexBlankingEndsAtTheVariantNativeClockBoundary(Upd765Model model, int clocks)
    {
        var rig = new PinFixture(model: model);
        Send(rig.Chip, 0x4a, 0); rig.ReadyForTrack();
        Assert.True(rig.Chip.Sync);
        rig.Index(); Assert.False(rig.Chip.Sync);
        rig.Chip.Advance(clocks - 1); Assert.False(rig.Chip.Sync);
        rig.Chip.Advance(1); Assert.True(rig.Chip.Sync);
        rig.Sector(1, new byte[128]); rig.FeedTrack();
        Assert.Equal(1, rig.Result()[5]);
    }

    [Theory]
    [InlineData(4_000_000)] [InlineData(8_000_000)]
    public void IndexBlankingUsesNativeClocksRatherThanFixedWallTime(int clock)
    {
        var chip = new Upd765(_ => 0x38, (_, _) => { }, clockFrequency: clock);
        Specify(chip, false); Send(chip, 0x4a, 0);
        while (chip.NextEventTicks != long.MaxValue) Event(chip);
        chip.SetIndex(true); Assert.Equal(4800, chip.NextEventTicks);
        chip.Advance(4799); Assert.False(chip.Sync);
        chip.Advance(1); Assert.True(chip.Sync);
    }

    [Fact]
    public void IdentifiersWithinTheIndexBlankingWindowCannotCompleteReadId()
    {
        var rig = new PinFixture(); rig.Sector(1, new byte[128]);
        Send(rig.Chip, 0x4a, 0); rig.ReadyForTrack(); rig.Index();
        rig.FeedTrack(waitForReady: false);
        Assert.True(rig.Chip.Active); Assert.Equal(0, rig.Chip.ReadBytes(0));
        rig.FeedTrack(); Assert.False(rig.Chip.Active); Assert.Equal(1, rig.Result()[5]);
    }

    [Fact]
    public void ResetClearsIndexBlankingAndReadTrackStillWaitsForItsOwnIndex()
    {
        var rig = new PinFixture();
        rig.Chip.SetIndex(true); rig.Chip.Reset();
        Send(rig.Chip, 0x4a, 0); rig.ReadyForTrack(); Assert.True(rig.Chip.Sync);
        rig.Chip.Reset(); rig.Command(2); rig.ReadyForTrack();
        Assert.False(rig.Chip.Sync); rig.Index(); Assert.False(rig.Chip.Sync);
        rig.Chip.Advance(4800); Assert.True(rig.Chip.Sync);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void WriteCommandsExposePreshiftPinsWithoutChangingDataOrCrc(bool mfm)
    {
        var rig = new PinFixture(mfm) { TerminalAfter = 128 };
        foreach (var value in Enumerable.Range(0, 128).Select(i => (byte)i)) rig.HostInput.Enqueue(value);
        rig.Sector(1, new byte[128]);
        var selections = new List<Upd765Precompensation>();
        rig.Chip.PrecompensationChanged = (_, value) =>
        {
            Assert.False(rig.Chip.Preshift0 && rig.Chip.Preshift1);
            Assert.Equal(value == Upd765Precompensation.Early, rig.Chip.Preshift0);
            Assert.Equal(value == Upd765Precompensation.Late, rig.Chip.Preshift1);
            selections.Add(value);
        };
        rig.Command(5); rig.FeedTrack(); rig.UntilResult(); rig.Result();
        if (mfm)
        {
            Assert.Contains(Upd765Precompensation.Early, selections);
            Assert.Contains(Upd765Precompensation.Late, selections);
        }
        else Assert.Empty(selections);
        Assert.False(rig.Chip.Preshift0); Assert.False(rig.Chip.Preshift1);
        Assert.Equal(128, rig.Chip.WrittenBytes(0));
    }

    [Fact]
    public void ResetFromTheStatusInputCallbackCancelsTheAdvance()
    {
        Upd765? chip = null; var armed = false;
        chip = new(_ => { if (armed) chip!.SetReset(true); return 0x38; }, (_, _) => { });
        Specify(chip, false); Send(chip, 0x4a, 0); chip.Advance(64 + 16000);
        Assert.True(chip.Active); armed = true; chip.Advance(1000); armed = false;
        Assert.Equal(0, chip.ReadMainStatus()); Assert.False(chip.Active); Assert.False(chip.HeadLoaded);
    }

    [Theory]
    [InlineData(false, 0)] [InlineData(false, 1)] [InlineData(true, 0)]
    public void SeekAndRecalibrateUnloadTheHeadsAndTheNextReadRepeatsHlt(bool recalibrate, byte cylinder)
    {
        var rig = new PinFixture(); rig.Sector(1, new byte[128]);
        Send(rig.Chip, 0x4a, 0); rig.FeedTrack(); rig.Result(); Assert.True(rig.Chip.HeadLoaded);
        if (recalibrate) Send(rig.Chip, 7, 0); else Send(rig.Chip, 0x0f, 0, cylinder);
        Assert.False(rig.Chip.HeadLoaded); Wait(rig.Chip, () => rig.Chip.Irq);
        Send(rig.Chip, 8); Result(rig.Chip, 2);
        Send(rig.Chip, 0x4a, 0); Assert.False(rig.Chip.HeadLoaded);
        rig.Chip.Advance(64); Assert.True(rig.Chip.HeadLoaded); Assert.False(rig.Chip.Sync);
        rig.Chip.Advance(15999); Assert.False(rig.Chip.Sync);
        rig.Chip.Advance(1); Assert.True(rig.Chip.Sync);
    }

    [Fact]
    public void ResetFromTheSeekHeadUnloadNotificationCannotResumeTheCommand()
    {
        var rig = new PinFixture(); rig.Sector(1, new byte[128]);
        Send(rig.Chip, 0x4a, 0); rig.FeedTrack(); rig.Result();
        rig.Chip.DriveSignalsChanged = () => { if (!rig.Chip.HeadLoaded) rig.Chip.SetReset(true); };
        Send(rig.Chip, 0x0f, 0, 1); rig.Chip.Advance(20000);
        Assert.Equal(0, rig.Chip.ReadMainStatus()); Assert.False(rig.Chip.Irq); Assert.False(rig.Chip.Step);
    }

}

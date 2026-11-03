using RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Intel.FDC.Chip8271;

public sealed class DriveSignalTests
{

    [Fact]
    public void FaultDropsWriteStepDirectionAndLowCurrentAndInterrupts()
    {
        var chip = Controller();
        Command(chip, 0x69, 43);
        for (var tick = 0; tick < 100 && (chip.DriveControl & 2) == 0; tick++) chip.Tick();
        Assert.NotEqual(0, chip.DriveControl & 0x16);
        chip.SetWriteFault(0, true);
        Assert.True(chip.Nmi);
        Assert.Equal(0x16, chip.Read(1));
        Assert.Equal(0, chip.DriveControl & 0x17);
        Assert.NotEqual(0, Special(chip, 0x22) & 0x20);
    }

    [Theory]
    [InlineData(2_000_000, 4000)]
    [InlineData(4_000_000, 2000)]
    public void StepEdgesRespectDirectionSetupPulseWidthAndProgrammedPeriod(int chipClock, int period)
    {
        var chip = Controller(chipClock);
        Command(chip, 0x69, 2);
        Assert.Equal(4, chip.DriveControl & 4);
        for (var i = 1; i < 20; i++) { chip.Tick(); Assert.Equal(0, chip.DriveControl & 2); }
        chip.Tick();
        Assert.Equal(2, chip.DriveControl & 2);
        Assert.Equal(1, chip.SeekSteps(0));
        for (var i = 1; i < 20; i++) { chip.Tick(); Assert.Equal(2, chip.DriveControl & 2); }
        chip.Tick();
        Assert.Equal(0, chip.DriveControl & 2);
        for (var i = 21; i < period; i++) { chip.Tick(); Assert.Equal(0, chip.DriveControl & 2); }
        chip.Tick();
        Assert.Equal(2, chip.DriveControl & 2);
        Assert.Equal(2, chip.SeekSteps(0));
        Assert.Equal(0, Finish(chip));
        Assert.Equal(2, Special(chip, 0x12));
    }

    [Fact]
    public void StepRateZeroWaitsForExternalCountEdges()
    {
        var chip = Controller();
        Command(chip, 0x35, 0x0d, 0, 0, 0x10);
        Command(chip, 0x69, 3);
        for (var i = 0; i < 10000; i++) chip.Tick();
        Assert.True(chip.Active);
        Assert.Equal(0, chip.SeekSteps(0));
        Assert.Equal(2, chip.DriveControl & 2);
        chip.SetCount(true);
        chip.SetCount(true);
        Assert.Equal(1, chip.SeekSteps(0));
        chip.SetCount(false); chip.SetCount(true);
        Assert.Equal(2, chip.SeekSteps(0));
        chip.SetCount(false); chip.SetCount(true);
        Assert.Equal(0, Finish(chip));
        Assert.Equal(3, chip.SeekSteps(0));
        Assert.Equal(0, chip.DriveControl & 2);
    }

    [Fact]
    public void RecalibrationStopsOnTrackZeroAndFailsAfter255StepsWithoutIt()
    {
        var chip = Controller();
        chip.MechanicalSpeedPercent = 0;
        chip.SetTrackZero(0, false);
        Command(chip, 0x69, 0);
        Assert.Equal(0x14, Finish(chip));
        Assert.Equal(255, chip.SeekSteps(0));
        chip.SetTrackZero(0, true);
        Command(chip, 0x69, 0);
        Assert.Equal(0, Finish(chip));
        Assert.Equal(255, chip.SeekSteps(0));
    }

    [Fact]
    public void TrackZeroInputIsIndependentOfTheSoftwareCurrentTrackRegister()
    {
        var chip = Controller();
        Command(chip, 0x3a, 0x12, 255);
        Assert.Equal(2, Special(chip, 0x22) & 2);
        Command(chip, 0x69, 0);
        Assert.Equal(0, Finish(chip));
        Assert.Equal(0, chip.SeekSteps(0));
    }

    [Fact]
    public void SpecifyBadTracksAdjustsThePhysicalTargetIncludingTheSecondBadTrack()
    {
        var chip = Controller();
        chip.MechanicalSpeedPercent = 0;
        Command(chip, 0x35, 0x10, 2, 5, 0);
        Command(chip, 0x69, 4);
        Assert.Equal(0, Finish(chip));
        Assert.Equal(6, chip.SeekSteps(0));
        Assert.Equal(6, Special(chip, 0x12));
        Assert.Equal(0, Special(chip, 0x1a));
    }

    [Theory]
    [InlineData(0xc0, 0)]
    [InlineData(0xc2, 3)]
    public void SingleActuatorSharesTrackRegistersButDoubleActuatorDoesNot(byte mode, byte otherTrack)
    {
        var chip = Controller();
        chip.MechanicalSpeedPercent = 0;
        Command(chip, 0x3a, 0x17, mode);
        Command(chip, 0x69, 3);
        Assert.Equal(0, Finish(chip));
        Assert.Equal(3, Special(chip, 0x12));
        Assert.Equal(otherTrack, Special(chip, 0x1a));
    }

    [Theory]
    [InlineData(42, 0)]
    [InlineData(43, 0x10)]
    public void LowHeadCurrentChangesAtTrack43(byte track, byte output)
    {
        var chip = Controller();
        chip.MechanicalSpeedPercent = 0;
        Command(chip, 0x69, track);
        Assert.Equal(0, Finish(chip));
        Assert.Equal(output, chip.DriveControl & 0x10);
    }

    [Theory]
    [InlineData(0x00, 0)]
    [InlineData(0x20, 2)]
    [InlineData(0xf0, 20)]
    public void HeadUnloadingCountsIndexEdgesAndFifteenMeansNoAutomaticUnload(byte loadUnload, int pulses)
    {
        var chip = Controller();
        chip.SetIndex(0, false);
        Command(chip, 0x35, 0x0d, 1, 0, loadUnload);
        Command(chip, 0x69, 0);
        Assert.Equal(0, Finish(chip));
        if (loadUnload == 0) { Assert.False(chip.Motor); return; }
        for (var i = 0; i < pulses; i++)
        {
            chip.SetIndex(0, true);
            chip.SetIndex(0, true); // Un niveau maintenu ne fait pas deux index.
            Assert.Equal(loadUnload == 0xf0 || i < pulses - 1, chip.Motor);
            chip.SetIndex(0, false);
        }
    }

    [Fact]
    public void ResetPreservesTheExternalReadyAndTrackZeroLevels()
    {
        var chip = Controller();
        chip.SetReady(0, false);
        chip.SetTrackZero(0, false);
        chip.Reset();
        Command(chip, 0x6c);
        Assert.Equal(0, chip.Read(1) & 6);
    }

    private static Intel8271 Controller(int chipClock = 2_000_000)
    {
        var chip = new Intel8271(chipClock: chipClock);
        chip.SetReady(0, true);
        chip.SetReady(1, true);
        chip.SetTrackZero(0, true);
        Command(chip, 0x35, 0x0d, 1, 0, 0x10);
        return chip;
    }

    private static byte Finish(Intel8271 chip)
    {
        for (var i = 0; i < 1_000_000; i++)
        {
            chip.Tick();
            if ((chip.Read(0) & 0x90) == 0x10) return chip.Read(1);
        }
        Assert.Fail("No command result.");
        return 255;
    }

    private static byte Special(Intel8271 chip, byte register)
    {
        Command(chip, 0x3d, register);
        return chip.Read(1);
    }

    private static void Command(Intel8271 chip, byte command, params byte[] parameters)
    {
        chip.Write(0, command);
        foreach (var value in parameters) chip.Write(1, value);
    }

}

using System.Reflection;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using Chip = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services.Intel8271;
using H = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Constants.Intel8271Constants;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.Intel8271DriveConstants;

namespace RetroMediaLab.Tests.Emulation.Intel8271;

public sealed class Intel8271DriveTimingTests
{
    private const int TickCount = 10_001;
    private static readonly FieldInfo TimeField = typeof(Intel8271Drive)
        .GetField("_time", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly FieldInfo FractionField = typeof(Intel8271Drive)
        .GetField("_timeFraction", BindingFlags.Instance | BindingFlags.NonPublic)!;

    [Theory]
    [InlineData(15_625)]
    [InlineData(15_627)]
    [InlineData(2_000_000)]
    [InlineData(3_000_000)]
    [InlineData(int.MaxValue)]
    public void PreparedClockPreservesEveryFractionalTick(int ownerClock)
    {
        var chip = new Chip(ownerClock);
        var drive = new Intel8271Drive(chip, static _ => null, ownerClock);
        try
        {
            for (var tick = 1; tick <= TickCount; tick++)
            {
                drive.Tick();
                var numerator = (long)tick * C.NanosecondsPerSecond;
                Assert.Equal((ulong)(numerator / ownerClock), (ulong)TimeField.GetValue(drive)!);
                Assert.Equal(numerator % ownerClock, (long)FractionField.GetValue(drive)!);
            }
        }
        finally
        {
            try { chip.Reset(); }
            finally { drive.Reset(); }
        }
    }

    [Fact]
    public void RotatingDriveUsesTrackUntilAnInputChanges()
    {
        using var disk = new MemoryTrackDisk();
        var chip = new Chip();
        var imageReads = 0;
        var headReads = 0;
        var drive = new Intel8271Drive(chip,
            index => { imageReads++; return index == 0 ? disk : null; },
            headSelect: output => { headReads++; return (output & H.OptionalOutput) != 0 ? 1 : 0; });
        try
        {
            WriteControl((byte)(H.Drive0Select | H.OutputLoadHead));
            drive.Tick();
            Assert.True(chip.Motor);
            CheckStable();
            WriteControl((byte)(H.Drive0Select | H.OutputLoadHead | H.OptionalOutput));
            drive.Tick();
            Assert.Equal(1, disk.RequestedHead);
            CheckStable();
            drive.MediaChanged(0);
            drive.Tick();
            CheckStable();
            drive.Reset();
            drive.Tick();
            CheckStable();
        }
        finally
        {
            try { chip.Reset(); }
            finally { drive.Reset(); }
        }
        void WriteControl(byte output)
        {
            drive.SynchronizeClock();
            chip.Write(0, H.WriteSpecial);
            chip.Write(1, H.DriveOutput);
            chip.Write(1, output);
            drive.Synchronize();
        }
        void CheckStable()
        {
            var images = imageReads;
            var heads = headReads;
            for (var tick = 0; tick < TickCount; tick++) drive.Tick();
            Assert.Equal(images, imageReads);
            Assert.Equal(heads, headReads);
        }
    }
    [Fact]
    public void UnchangedSignalsDoNotDecodeHeadAgain()
    {
        var chip = new Chip();
        var calls = 0;
        var drive = new Intel8271Drive(chip, static _ => null, headSelect: _ => { calls++; return 0; });
        try
        {
            drive.Synchronize();
            var initialCalls = calls;
            for (var tick = 0; tick < TickCount; tick++) drive.Tick();
            Assert.Equal(initialCalls, calls);
            drive.MediaChanged(0);
            Assert.Equal(initialCalls + 1, calls);
        }
        finally
        {
            try { chip.Reset(); }
            finally { drive.Reset(); }
        }
    }
}

using RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services;
using C = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Constants.Intel8271Constants;

namespace RetroMediaLab.Chips.Tests.Controllers.Intel.FDC.Chip8271;

public sealed class DriveSignalNotificationsTests
{
    [Fact]
    public void StableIdleDoesNotNotifyAndAllObservedStateChangesDo()
    {
        var chip = new Intel8271 { MechanicalSpeedPercent = 0 };
        var notifications = 0;
        chip.DriveSignalsChanged += () => notifications++;
        for (var tick = 0; tick < 10_000; tick++) chip.Tick();
        Assert.Equal(0, notifications);
        for (var command = 0; command <= byte.MaxValue; command++)
        {
            Check(chip.Reset);
            Check(() => chip.SetReady(0, true));
            Check(() => chip.SetReady(1, true));
            Check(() => chip.SetTrackZero(0, false));
            Check(() => chip.SetTrackZero(1, false));
            Check(() => chip.Write(0, (byte)command));
            foreach (var parameter in new byte[] { 3, 0, 0x21, 16, 16 }) Check(() => chip.Write(1, parameter));
            for (var tick = 0; tick < 256; tick++)
            {
                Check(chip.Tick);
                if (tick % 17 == 0) Check(() => chip.SetIndex(chip.ActiveDrive, true));
                if (tick % 17 == 1) Check(() => chip.SetIndex(chip.ActiveDrive, false));
                if (tick % 13 == 0) Check(() => chip.SetCount(true));
                if (tick % 13 == 1) Check(() => chip.SetCount(false));
                if (tick % 7 == 0) Check(() => chip.Write(4, (byte)tick));
                if (tick % 11 == 0) Check(() => chip.Read(4));
            }
            Check(() => chip.SetReady(chip.ActiveDrive, false));
            Check(() => chip.SetWriteFault(chip.ActiveDrive, true));
            Check(() => chip.SetReset(true));
            Check(() => chip.SetReset(false));
        }
        for (var control = 0; control <= byte.MaxValue; control++)
        {
            Check(chip.Reset);
            Check(() => chip.Write(0, C.WriteSpecial));
            Check(() => chip.Write(1, C.DriveOutput));
            Check(() => chip.Write(1, (byte)control));
        }
        Assert.True(notifications > 100);

        void Check(Action action)
        {
            var before = (chip.DriveControl, chip.ActiveDrive, chip.Reading, chip.Writing);
            var count = notifications;
            action();
            var after = (chip.DriveControl, chip.ActiveDrive, chip.Reading, chip.Writing);
            if (before != after) Assert.True(notifications > count);
        }
    }
}

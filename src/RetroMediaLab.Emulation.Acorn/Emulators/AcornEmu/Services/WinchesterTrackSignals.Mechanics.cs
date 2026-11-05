using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.WinchesterDriveConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

internal sealed partial class WinchesterTrackSignals
{
    private readonly DriveState[] drives = [new(), new()];

    internal void RefreshDrives() => UpdateDriveSignals(advanceTime: false);

    private void UpdateDriveSignals(bool advanceTime = true)
    {
        for (var unit = 0; unit < C.DriveCount; unit++)
        {
            var drive = drives[unit];
            var selected = storage(unit);
            if (!ReferenceEquals(drive.Storage?.Image, selected?.Image))
            {
                // A pending window still belongs to the old image, even when it has been detached.
                if (writingImage is not null && writingUnit == unit) PublishWindow();
                if (drive.Storage is { } previous)
                {
                    previous.Image.Mechanics.SetActivity(false, false);
                    previous.Image.Mechanics.SetMotor(false);
                }
                drive.Settle = 0;
                drive.Reading = drive.Writing = false;
                cache[unit].Clear();
                selected?.UsePhysicalSignals();
                selected?.Image.Mechanics.SetMotor(true);
            }
            drive.Storage = selected;
            if (advanceTime && drive.Settle > 0)
                drive.Settle = drive.Settle > C.NanosecondsPerSecond ? drive.Settle - C.NanosecondsPerSecond : 0;
            chip.SetDriveSignals(unit, selected is not null, selected is not null && drive.Settle == 0,
                selected is not null && selected.Cylinder == 0);
        }
    }

    private void ApplyDriveSteps()
    {
        for (var unit = 0; unit < C.DriveCount; unit++)
        {
            var drive = drives[unit];
            if (drive.Storage is null || !chip.DriveStepIssued(unit)) continue;
            // Consume the new edge before returning to the caller: a subsequent RESET cannot
            // erase a displacement already issued to the mechanism.
            drive.Storage.Step(chip.DriveDirectionIn(unit));
            drive.Settle = (ulong)C.SeekSettleNanoseconds * ownerClock;
        }
    }

    private void UpdateActivity()
    {
        for (var unit = 0; unit < C.DriveCount; unit++)
        {
            var drive = drives[unit];
            var reading = chip.Unit == unit && chip.ReadGate;
            var writing = chip.Unit == unit && chip.WriteGate;
            if (drive.Storage is null || drive.Reading == reading && drive.Writing == writing) continue;
            drive.Storage.Image.Mechanics.SetActivity(reading, writing);
            drive.Reading = reading;
            drive.Writing = writing;
        }
    }

    private void StopActivity()
    {
        foreach (var drive in drives)
        {
            if (drive.Storage is not null && (drive.Reading || drive.Writing))
                drive.Storage.Image.Mechanics.SetActivity(false, false);
            drive.Reading = drive.Writing = false;
        }
        // RESET does not move the heads, stop a separately powered spindle, or cancel its settling.
    }

    private sealed class DriveState
    {
        internal WinchesterStorage? Storage;
        internal ulong Settle;
        internal bool Reading;
        internal bool Writing;
    }
}

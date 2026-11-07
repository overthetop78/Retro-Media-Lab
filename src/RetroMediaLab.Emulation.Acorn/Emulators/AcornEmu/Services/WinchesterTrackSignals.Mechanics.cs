using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.WinchesterDriveConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

internal sealed partial class WinchesterTrackSignals
{
    private readonly DriveState[] drives = [new(), new()];
    private bool refreshDrives = true;
    private bool mechanicsPending;
    private bool stepsPending;

    internal void RefreshDrives()
    {
        refreshDrives = false;
        for (var unit = 0; unit < C.DriveCount; unit++) RefreshDrive(unit);
    }

    private void RefreshDrive(int unit)
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
        activityChanged = true;
        PublishDriveInputs(unit);
    }

    private void AdvanceMechanics()
    {
        if (refreshDrives) RefreshDrives();
        if (!mechanicsPending) return;
        mechanicsPending = false;
        for (var unit = 0; unit < C.DriveCount; unit++)
        {
            var drive = drives[unit];
            if (drive.Settle > 0)
            {
                drive.Settle = drive.Settle > C.NanosecondsPerSecond ? drive.Settle - C.NanosecondsPerSecond : 0;
                if (drive.Settle == 0) drive.InputsChanged = true;
                else mechanicsPending = true;
            }
            if (drive.InputsChanged) PublishDriveInputs(unit);
        }
    }

    private void PublishDriveInputs(int unit)
    {
        var drive = drives[unit];
        drive.InputsChanged = false;
        chip.SetDriveSignals(unit, drive.Storage is not null, drive.Storage is not null && drive.Settle == 0,
            drive.Storage is not null && drive.Storage.Cylinder == 0);
    }

    private void ReceiveDriveStep(int unit, bool directionIn)
    {
        var drive = drives[unit];
        drive.StepPending = true;
        drive.DirectionIn = directionIn;
        stepsPending = true;
    }

    private void ApplyDriveSteps()
    {
        if (!stepsPending) return;
        stepsPending = false;
        for (var unit = 0; unit < C.DriveCount; unit++)
        {
            var drive = drives[unit];
            if (!drive.StepPending) continue;
            drive.StepPending = false;
            if (drive.Storage is null) continue;
            // Consume STEP after the controller's edge, before returning to its caller.
            // The physical displacement survives RESET; feedback is sampled next owner edge.
            drive.Storage.Step(drive.DirectionIn);
            drive.Settle = (ulong)C.SeekSettleNanoseconds * ownerClock;
            drive.InputsChanged = true;
            mechanicsPending = true;
        }
    }

    private void UpdateActivity()
    {
        if (!activityChanged) return;
        activityChanged = false;
        for (var unit = 0; unit < C.DriveCount; unit++)
        {
            var drive = drives[unit];
            var reading = selectedUnit == unit && readGate;
            var writing = selectedUnit == unit && writeGate;
            if (drive.Storage is null || drive.Reading == reading && drive.Writing == writing) continue;
            drive.Storage.Image.Mechanics.SetActivity(reading, writing);
            drive.Reading = reading;
            drive.Writing = writing;
        }
    }

    private void ReceiveDriveSelection(int unit, int head, int cylinder, int writeHead)
    {
        selectedUnit = unit;
        selectedHead = head;
        selectedCylinder = cylinder;
        selectedWriteHead = writeHead;
        activityChanged = true;
    }

    private void ReceiveDriveGates(bool read, bool write)
    {
        readGate = read;
        writeGate = write;
        activityChanged = true;
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
        activityChanged = true;
    }

    private sealed class DriveState
    {
        internal WinchesterStorage? Storage;
        internal ulong Settle;
        internal bool Reading;
        internal bool Writing;
        internal bool InputsChanged;
        internal bool StepPending;
        internal bool DirectionIn;
    }
}

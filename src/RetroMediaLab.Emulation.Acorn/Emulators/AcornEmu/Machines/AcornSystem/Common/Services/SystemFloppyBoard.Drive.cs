using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN7438.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Enums;
using H = RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Constants.Intel8271Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

internal sealed partial class SystemFloppyBoard
{
    private byte _driveLevels, _driveKnown;
    /// <summary>Resolved physical levels, known mask, changed mask. No media is owned.</summary>
    internal Action<byte, byte, byte>? DriveLinesChanged { get; set; }
    internal Action<bool?>? NmiChanged { get; set; }
    internal byte DriveLevels => _driveLevels;
    internal byte DriveKnown => _driveKnown;
    internal bool? Nmi => _ic3.GetOutputDrive(0) switch
    {
        Sn7438OutputDrive.Sinking => true,
        Sn7438OutputDrive.Released => false,
        _ => null
    };

    private void WireDrive()
    {
        _ic3.SetObservedOutputs(1); // Three unused gates are not connected to the CPU.
        _ic3.OutputDriveChanged = (_, _) => { if (!_disposed) NmiChanged?.Invoke(Nmi); };
        _ic5.OutputDriveChanged = (gate, drive) => ResolveDriveLine(gate, drive);
        _ic6.OutputDriveChanged = (gate, drive) => ResolveDriveLine(gate + 4, drive);
    }

    private void ControllerDriveChanged()
    {
        if (_disposed) return;
        _ic5.AdvanceTo(_time); _ic6.AdvanceTo(_time);
        var control = _fdc.DriveControl;
        _ic5.SetInputs(0, (control & H.Drive0Select) != 0, (control & H.Drive0Select) != 0);
        _ic5.SetInputs(1, (control & H.Drive1Select) != 0, (control & H.Drive1Select) != 0);
        _ic5.SetInputs(2, (control & H.OutputLoadHead) != 0, (control & H.OutputLoadHead) != 0);
        _ic5.SetInputs(3, (control & H.OutputDirection) != 0, (control & H.OutputDirection) != 0);
        _ic6.SetInputs(0, (control & H.OutputStep) != 0, (control & H.OutputStep) != 0);
        _ic6.SetInputs(2, (control & H.OutputWriteEnable) != 0, (control & H.OutputWriteEnable) != 0);
        _ic6.SetInputs(3, (control & H.OptionalOutput) != 0, (control & H.OptionalOutput) != 0);
    }

    private void ControllerWriteDataChanged(bool high)
    {
        if (_disposed) return;
        _ic6.AdvanceTo(_time);
        _ic6.SetInputs(1, high, high);
    }

    private void ControllerInterruptChanged(bool high)
    {
        if (_disposed) return;
        _ic3.AdvanceTo(_time);
        _ic3.SetInputs(0, high, high);
    }

    private void ResolveDriveLine(int line, Sn7438OutputDrive drive)
    {
        var mask = (byte)(1 << line);
        var oldLevels = _driveLevels; var oldKnown = _driveKnown;
        if (drive == Sn7438OutputDrive.Unknown)
        {
            _driveKnown &= (byte)~mask;
            _driveLevels &= (byte)~mask;
        }
        else
        {
            _driveKnown |= mask;
            if (drive == Sn7438OutputDrive.Released) _driveLevels |= mask; // External pull-up.
            else _driveLevels &= (byte)~mask;
        }
        if (line is 0 or 1 && (_driveKnown & mask) != 0)
        {
            var chip = line == 0 ? _ic11 : _ic10;
            chip.AdvanceTo(_time);
            chip.SetSet(0, (_driveLevels & mask) != 0);
        }
        if (((oldLevels ^ _driveLevels) | (oldKnown ^ _driveKnown)) != 0 && !_disposed)
            DriveLinesChanged?.Invoke(_driveLevels, _driveKnown, mask);
    }

    internal void SetDriveSensorLevels(bool nTrackZero, bool nWriteProtected)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        SynchronizeController();
        // One pair of physical input wires, viewed by the controller for the selected drive.
        for (var drive = 0; drive < 2; drive++)
        {
            _fdc.SetTrackZero(drive, !nTrackZero);
            _fdc.SetWriteProtected(drive, !nWriteProtected);
        }
    }
}

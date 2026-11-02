using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Constants.ArchimedesBoardConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Services;

internal sealed partial class ArchimedesMachine
{
    private readonly IEmulationDiskImage?[] _disks = new IEmulationDiskImage?[C.FloppyDrives];
    private readonly IEmulationDiskImage?[] _hardDisks = new IEmulationDiskImage?[C.HardDiskDrives];
    private readonly int[] _floppySpeed = Enumerable.Repeat(C.DefaultMechanicalSpeedPercent, C.FloppyDrives).ToArray();
    private byte _floppyLatchA;
    private byte _floppyLatchB;

    public void AttachHardDisk(int unit, IEmulationDiskImage? image)
    {
        ValidateHardDisk(unit);
        if (_hardDisk is null && image is not null) throw new NotSupportedException();
        _hardDisks[unit] = image;
        _hardDisk?.MediaChanged(unit);
        RefreshHardDiskInterrupt();
    }

    public bool HardDiskActive(int unit) => HasHardDiskUnit(unit) && _hardDisk?.Active(unit) == true;
    public bool HardDiskMotor(int unit) => HasHardDiskUnit(unit) && _hardDisk is not null && _hardDisks[unit] is not null;
    public bool HardDiskWriting(int unit) => HardDiskActive(unit) && _hardDisk!.Unit == unit && _hardDisk.Writing;
    public long HardDiskSeekCount(int unit) => HasHardDiskUnit(unit) ? _hardDisk?.SeekCount(unit) ?? 0 : 0;
    public long HardDiskReadBlocks(int unit) => HasHardDiskUnit(unit) ? _hardDisk?.ReadBlocks(unit) ?? 0 : 0;
    public long HardDiskWrittenBlocks(int unit) => HasHardDiskUnit(unit) ? _hardDisk?.WrittenBlocks(unit) ?? 0 : 0;

    public void ConfigureHardDisk(int unit, int latencyMilliseconds, int speedPercent, int? writeLatencyMilliseconds = null)
    {
        ValidateHardDisk(unit);
        _hardDisk?.Configure(unit, latencyMilliseconds, speedPercent, writeLatencyMilliseconds);
    }

    private static bool HasHardDiskUnit(int unit) => unit >= 0 && unit < C.HardDiskDrives;

    private static void ValidateHardDisk(int unit)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(unit);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(unit, C.HardDiskDrives);
    }

    public void AttachFloppy(int index, IEmulationDiskImage? image)
    {
        ValidateDrive(index);
        _disks[index] = image;
        RefreshControlInputs();
    }

    public bool FloppyActive(int index) => _floppy.Selected && _floppy.Drive == index && _floppy.Active;
    public bool FloppyMotor(int index) => _floppy.Selected && _floppy.Drive == index && (_floppyLatchA & C.FloppyMotor) == 0;
    public bool FloppyReading(int index) => FloppyActive(index) && _floppy.Reading;
    public bool FloppyWriting(int index) => FloppyActive(index) && _floppy.Writing;
    public long FloppySeekSteps(int index) => _floppy.SeekSteps(index);
    public long FloppyReadBytes(int index) => _floppy.ReadBytes(index);
    public long FloppyWrittenBytes(int index) => _floppy.WrittenBytes(index);

    public void ConfigureFloppySpeed(int index, int percent)
    {
        ValidateDrive(index);
        ArgumentOutOfRangeException.ThrowIfNegative(percent);
        _floppySpeed[index] = percent;
        if (_floppy.Drive == index) _floppy.MechanicalSpeedPercent = percent;
    }

    private void ValidateDrive(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _floppyDriveCount);
    }

    private void WriteFloppyLatchA(byte value)
    {
        _floppyLatchA = value;
        _floppy.Selected = false;
        for (var drive = 0; drive < C.FloppyDrives; drive++)
        {
            if ((value & (1 << drive)) != 0) continue;
            _floppy.Selected = true;
            _floppy.Drive = drive;
        }
        _floppy.Side = (value & C.FloppySide) == 0 ? 1 : 0;
        _floppy.MechanicalSpeedPercent = _floppySpeed[_floppy.Drive];
        RefreshControlInputs();
    }

    private void WriteFloppyLatchB(byte value)
    {
        _floppyLatchB = value;
        _floppy.DoubleDensity = (value & C.FloppyDensity) == 0;
        if ((value & C.FloppyReset) == 0) _floppy.Reset();
        RefreshControlInputs();
    }
}

using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants.SystemConstants;
using Storage = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.StorageConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

internal sealed partial class AcornSystemMachine
{
    public int FloppyDriveCount => _fdc is null
        ? Storage.NoDrives
        : C.FloppyDrives;
    public IEmulatorTapeTransport TapeTransport => _tape;
    public bool TapeHasChanges => _tape.HasChanges;
    public EmulationTapeSignal RecordedTape => _tape.RecordedSignal;
    public void InsertTape(EmulationTapeSignal signal, bool writable = false) => _tape.Insert(signal, writable);
    public void EjectTape() => _tape.Eject();
    public void MarkTapeSaved() => _tape.MarkSaved();
    public void RefreshTapeIndex(EmulationTapeSignal signal) => _tape.RefreshIndex(signal);
    public void ConfigureTape(EmulationTapeSettings settings) => _tape.Configure(settings);

    public void AttachFloppy(int index, IEmulationDiskImage? image)
    {
        CheckDrive(index);
        if (_fdc is null && image is not null) throw new NotSupportedException();
        if (_fdc?.Active == true && _fdc.ActiveDrive == index) { _fdc.Reset(); _fdcDrive!.Reset(); }
        _disks[index] = image;
        _fdcDrive?.MediaChanged(index);
    }

    public bool FloppyActive(int index) => FloppyMotor(index) && _disks[index] is not null;
    public bool FloppyMotor(int index) => _fdc?.Motor == true && _fdc.ActiveDrive == index;
    public bool FloppyReading(int index) => _fdc?.Reading == true && _fdc.ActiveDrive == index;
    public bool FloppyWriting(int index) => _fdc?.Writing == true && _fdc.ActiveDrive == index;
    public long FloppySeekSteps(int index) => _fdc?.SeekSteps(index) ?? 0;
    public long FloppyReadBytes(int index) => _fdc?.ReadBytes(index) ?? 0;
    public long FloppyWrittenBytes(int index) => _fdc?.WrittenBytes(index) ?? 0;

    public void ConfigureFloppySpeed(int index, int percent)
    {
        CheckDrive(index);
        ArgumentOutOfRangeException.ThrowIfNegative(percent);
        _speeds[index] = percent;
        if (_fdc?.ActiveDrive == index) _fdc.MechanicalSpeedPercent = percent;
    }

    private static void CheckDrive(int index)
    {
        if (index < 0 || index >= C.FloppyDrives) throw new ArgumentOutOfRangeException(nameof(index));
    }

    public void AttachHardDisk(int unit, IEmulationDiskImage? image) { if (image is not null) throw new NotSupportedException(); }
    public bool HardDiskActive(int unit) => false;
    public bool HardDiskMotor(int unit) => false;
    public bool HardDiskWriting(int unit) => false;
    public long HardDiskSeekCount(int unit) => 0;
    public long HardDiskReadBlocks(int unit) => 0;
    public long HardDiskWrittenBlocks(int unit) => 0;
    public void ConfigureHardDisk(int unit, int latencyMilliseconds, int speedPercent, int? writeLatencyMilliseconds = null) => throw new NotSupportedException();
}

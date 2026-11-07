namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornElectron.Common.Services;

internal sealed partial class ElectronMachine
{
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
        if (image is not null) throw new NotSupportedException();
    }
    public bool FloppyActive(int index) => false;
    public bool FloppyMotor(int index) => false;
    public bool FloppyReading(int index) => false;
    public bool FloppyWriting(int index) => false;
    public long FloppySeekSteps(int index) => 0;
    public long FloppyReadBytes(int index) => 0;
    public long FloppyWrittenBytes(int index) => 0;
    public void ConfigureFloppySpeed(int index, int percent) => throw new NotSupportedException();

    public void AttachHardDisk(int unit, IEmulationDiskImage? image)
    {
        if (image is not null) throw new NotSupportedException();
    }
    public bool HardDiskActive(int unit) => false;
    public bool HardDiskMotor(int unit) => false;
    public bool HardDiskWriting(int unit) => false;
    public long HardDiskSeekCount(int unit) => 0;
    public long HardDiskReadBlocks(int unit) => 0;
    public long HardDiskWrittenBlocks(int unit) => 0;
    public void ConfigureHardDisk(int unit, int latencyMilliseconds, int speedPercent, int? writeLatencyMilliseconds = null) => throw new NotSupportedException();
}

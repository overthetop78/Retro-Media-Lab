namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornElectron.Common.Services;

internal sealed partial class ElectronMachine
{
    public IEmulatorCassetteTransport CassetteTransport => _cassette;
    public bool CassetteHasChanges => _cassette.HasChanges;
    public EmulationCassetteSignal RecordedCassette => _cassette.RecordedSignal;
    public void InsertCassette(EmulationCassetteSignal signal, bool writable = false) => _cassette.Insert(signal, writable);
    public void EjectCassette() => _cassette.Eject();
    public void MarkCassetteSaved() => _cassette.MarkSaved();
    public void RefreshCassetteIndex(EmulationCassetteSignal signal) => _cassette.RefreshIndex(signal);
    public void ConfigureCassette(EmulationCassetteSettings settings) => _cassette.Configure(settings);

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

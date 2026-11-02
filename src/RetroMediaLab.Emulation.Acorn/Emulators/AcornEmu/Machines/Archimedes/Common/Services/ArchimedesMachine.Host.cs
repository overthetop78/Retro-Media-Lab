using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Constants.ArchimedesBoardConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Services;

/// <summary>Adaptation de la carte aux sorties communes, sans ajouter de périphérique absent.</summary>
internal sealed partial class ArchimedesMachine
{
    public long? ProcessorClockHz => _cpuClockHz;
    public int? VideoWidth => Width;
    public int? VideoHeight => Height;
    public double? VideoFramesPerSecond => _vidc.FramesPerSecond;
    public double? VideoFrameDurationSeconds => _lastFrameSeconds > 0 ? _lastFrameSeconds : null;
    public int FloppyDriveCount => _floppyDriveCount;
    public int HardDiskDriveCount => _hardDisk is null ? 0 : C.HardDiskDrives;
    public EmulationAudioChannelLayout AudioChannelLayout => EmulationAudioChannelLayout.Stereo;
    public string? NonvolatileMemoryFile => C.CmosFile;
    public IEmulatorCassetteTransport? CassetteTransport => null;
    public bool CassetteHasChanges => false;
    public EmulationCassetteSignal RecordedCassette => throw new NotSupportedException();
    public void InsertCassette(EmulationCassetteSignal signal, bool writable = false) => throw new NotSupportedException();
    public void MarkCassetteSaved() { }
    public void RefreshCassetteIndex(EmulationCassetteSignal signal) => throw new NotSupportedException();
    public void ConfigureCassette(EmulationCassetteSettings settings) { }
    public void EjectCassette() { }
    public Action<ReadOnlyMemory<byte>>? NetworkFrameTransmitted { get; set; }
    public Action<int>? NetworkPoll { get; set; }
    public bool ReceiveNetworkFrame(ReadOnlySpan<byte> frame, bool validFcs = true) => false;
    public void SetNetworkLine(bool clockPresent, bool clearToSend = true, bool idle = true) { }
    public void SetOption(string key, string value) => throw new ArgumentOutOfRangeException(nameof(key));
}

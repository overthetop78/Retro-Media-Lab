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
    public IEmulatorTapeTransport? TapeTransport => null;
    public bool TapeHasChanges => false;
    public EmulationTapeSignal RecordedTape => throw new NotSupportedException();
    public void InsertTape(EmulationTapeSignal signal, bool writable = false) => throw new NotSupportedException();
    public void MarkTapeSaved() { }
    public void RefreshTapeIndex(EmulationTapeSignal signal) => throw new NotSupportedException();
    public void ConfigureTape(EmulationTapeSettings settings) { }
    public void EjectTape() { }
    public Action<ReadOnlyMemory<byte>>? NetworkFrameTransmitted { get; set; }
    public Action<int>? NetworkPoll { get; set; }
    public bool ReceiveNetworkFrame(ReadOnlySpan<byte> frame, bool validFcs = true) => false;
    public void SetNetworkLine(bool clockPresent, bool clearToSend = true, bool idle = true) { }
    public void SetOption(string key, string value) => throw new ArgumentOutOfRangeException(nameof(key));
}

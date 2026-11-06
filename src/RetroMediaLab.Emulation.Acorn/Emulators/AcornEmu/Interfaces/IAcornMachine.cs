using Storage = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.StorageConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Interfaces;

/// <summary>Échanges entre le cœur AcornEmu et l'assemblage matériel choisi dans son catalogue de modèles.</summary>
/// <remarks>Le cœur ne connaît ni le bus, ni la RAM, ni les périphériques d'un modèle particulier.
/// Chaque assemblage possède ses composants gérés ; le thread et la présentation restent possédés par la session Common.</remarks>
internal interface IAcornMachine
{
    /// <summary>Type des données de l'assemblage ; null tant que sa reprise n'est pas raccordée.</summary>
    Type? StateType => null;
    object CaptureState() => throw new NotSupportedException();
    void RestoreState(object state) => throw new NotSupportedException();
    long? ProcessorClockHz => null;
    byte[] RunFrame();
    /// <summary>Dimensions programmables ; null conserve les dimensions fixes du modèle.</summary>
    int? VideoWidth => null;
    int? VideoHeight => null;
    /// <summary>Cadence programmable ; null conserve celle du catalogue.</summary>
    double? VideoFramesPerSecond => null;
    /// <summary>Durée réellement exécutée par le dernier RunFrame, pour les cartes à balayage programmable.</summary>
    double? VideoFrameDurationSeconds => null;
    int FloppyDriveCount => Storage.NoDrives;
    int HardDiskDriveCount => Storage.NoDrives;
    EmulationAudioChannelLayout AudioChannelLayout => EmulationAudioChannelLayout.Mono;
    string? NonvolatileMemoryFile => null;
    /// <summary>Transfère les échantillons entrelacés produits depuis le dernier prélèvement.</summary>
    short[] DrainAudio();
    IEmulatorCassetteTransport? CassetteTransport { get; }
    void InsertCassette(EmulationCassetteSignal signal, bool writable = false);
    bool CassetteHasChanges { get; }
    EmulationCassetteSignal RecordedCassette { get; }
    void MarkCassetteSaved();
    void RefreshCassetteIndex(EmulationCassetteSignal signal);
    void ConfigureCassette(EmulationCassetteSettings settings);
    void EjectCassette();
    void AttachFloppy(int index, IEmulationDiskImage? image);
    bool FloppyActive(int index);
    bool FloppyMotor(int index);
    bool FloppyReading(int index);
    bool FloppyWriting(int index);
    long FloppySeekSteps(int index);
    long FloppyReadBytes(int index);
    long FloppyWrittenBytes(int index);
    void ConfigureFloppySpeed(int index, int percent);
    void AttachHardDisk(int unit, IEmulationDiskImage? image);
    bool HardDiskActive(int unit);
    bool HardDiskMotor(int unit);
    bool HardDiskWriting(int unit);
    long HardDiskSeekCount(int unit);
    long HardDiskReadBlocks(int unit);
    long HardDiskWrittenBlocks(int unit);
    void ConfigureHardDisk(int unit, int latencyMilliseconds, int speedPercent, int? writeLatencyMilliseconds = null);
    /// <summary>Raccorde les trames et l'état de ligne du transport possédé par le cœur.</summary>
    Action<ReadOnlyMemory<byte>>? NetworkFrameTransmitted { get; set; }
    Action<int>? NetworkPoll { get; set; }
    bool ReceiveNetworkFrame(ReadOnlySpan<byte> frame, bool validFcs = true);
    void SetNetworkLine(bool clockPresent, bool clearToSend = true, bool idle = true);
    void SetInput(EmulationInputSnapshot snapshot);
    byte[]? ReadNonvolatileMemory();
    void WriteNonvolatileMemory(ReadOnlySpan<byte> bytes);
    void Reset(bool cold);
    void SetOption(string key, string value);
}

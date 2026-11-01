using RetroMediaLab.App.Enums.Services.PhysicalDiskWriting;
using RetroMediaLab.Infrastructure.Hardware.Greaseweazle;

namespace RetroMediaLab.App.Contracts.Services.PhysicalDiskWriting;

public sealed record PhysicalDiskWriteOptions(
    string PortName,
    GreaseweazleBusType BusType,
    byte DriveUnit,
    bool CueAtIndex = true,
    bool TerminateAtIndex = true,
    bool Verify = false,
    PhysicalTrackEncoding PrecompensationEncoding = PhysicalTrackEncoding.Mfm,
    IReadOnlyList<PhysicalWritePrecompensationStep>? Precompensation = null,
    uint HardSectorTicks = 0,
    int ScpRevolution = 0);

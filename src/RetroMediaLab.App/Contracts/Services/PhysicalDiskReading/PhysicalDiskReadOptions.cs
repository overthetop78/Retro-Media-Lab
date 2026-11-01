using RetroMediaLab.App.Constants.Services.PhysicalDiskReading;
using RetroMediaLab.Infrastructure.Hardware.Greaseweazle;

using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp;

namespace RetroMediaLab.App.Contracts.Services.PhysicalDiskReading;

public sealed record PhysicalDiskReadOptions(
    string PortName,
    GreaseweazleBusType BusType,
    byte DriveUnit,
    IReadOnlyList<PhysicalDiskTrackAddress> Tracks,
    ScpDiskType DiskType,
    int Revolutions = PhysicalDiskReadDefaults.Revolutions,
    int FluxOverflowRetries = PhysicalDiskReadDefaults.FluxOverflowRetries,
    int SeekRetries = PhysicalDiskReadDefaults.SeekRetries,
    TimeSpan? FakeIndexPeriod = null,
    bool HardSectors = false,
    TimeSpan? MotorSpinUpDelay = null,
    TimeSpan? TrackSettleDelay = null);

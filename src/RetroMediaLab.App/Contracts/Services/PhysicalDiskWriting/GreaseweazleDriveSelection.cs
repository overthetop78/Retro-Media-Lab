using RetroMediaLab.Infrastructure.Hardware.Greaseweazle;

namespace RetroMediaLab.App.Contracts.Services.PhysicalDiskWriting;

public sealed record GreaseweazleDriveSelection(GreaseweazleBusType BusType, byte Unit);

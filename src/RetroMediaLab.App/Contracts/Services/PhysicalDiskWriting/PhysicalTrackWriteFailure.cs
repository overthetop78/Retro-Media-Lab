using RetroMediaLab.App.Enums.Services.PhysicalDiskWriting;
namespace RetroMediaLab.App.Contracts.Services.PhysicalDiskWriting;

public sealed record PhysicalTrackWriteFailure(int? Cylinder, int? Head, PhysicalDiskWriteFailureCategory Category, Exception? Exception = null);

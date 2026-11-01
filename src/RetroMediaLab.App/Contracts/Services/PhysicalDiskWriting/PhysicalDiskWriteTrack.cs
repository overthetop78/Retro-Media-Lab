using RetroMediaLab.MediaEngine.Images.Writing.Encoding;

namespace RetroMediaLab.App.Contracts.Services.PhysicalDiskWriting;

internal sealed record PhysicalDiskWriteTrack(int Cylinder, int Head, IReadOnlyList<uint> FluxIntervals, uint SourceTickNanoseconds, EncodedDiskTrack? EncodedTrack = null);

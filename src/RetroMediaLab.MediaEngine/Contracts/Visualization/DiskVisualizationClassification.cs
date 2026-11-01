using RetroMediaLab.MediaEngine.Enums;

namespace RetroMediaLab.MediaEngine.Contracts.Visualization;

public sealed record DiskVisualizationClassification(string? DecoderId, DiskMediaCategory MediaCategory);

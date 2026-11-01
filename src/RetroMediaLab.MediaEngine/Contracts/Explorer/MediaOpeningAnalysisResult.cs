using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Contracts.Explorer;

namespace RetroMediaLab.MediaEngine.Contracts.Explorer;

/// <summary>Collects one loaded media document and its requested explorations.</summary>
public sealed record MediaOpeningAnalysisResult(MediaImageDocument Document, ExploredDiskImage? DiskExploration, ExploredMediaImage? MediaExploration);


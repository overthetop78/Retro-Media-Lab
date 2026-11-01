using RetroMediaLab.MediaEngine.Images.Models.Sectors;

namespace RetroMediaLab.MediaEngine.Contracts.Migration;

/// <summary>Image remplie par MediaFileSystems et rapport retournés par l'API MediaEngine.</summary>
public sealed record MigrationResult(SectorImage Image, MigrationValidationReport Report);

using RetroMediaLab.VideoPresentation.Enums;

namespace RetroMediaLab.VideoPresentation.Dictionaries;

public static partial class EmulationVideoProcessingCatalog
{
    public const string ProjectionTechnology = "Emulation.Video.Projection.Technology";
    public const string ProjectionSurface = "Emulation.Video.Projection.Surface";

    public static IReadOnlyDictionary<EmulationProjectionTechnology, string> ProjectionTechnologyResourceKeys { get; } =
        ResourceKeys("Projection.Technology", Enum.GetValues<EmulationProjectionTechnology>());

    public static IReadOnlyDictionary<EmulationProjectionSurface, string> ProjectionSurfaceResourceKeys { get; } =
        ResourceKeys("Projection.Surface", Enum.GetValues<EmulationProjectionSurface>());

    public static IReadOnlyDictionary<EmulationCrtConnection, string> ProjectionConnectionResourceKeys { get; } =
        ResourceKeys("Crt.Connection", new[] { EmulationCrtConnection.Digital, EmulationCrtConnection.Composite,
            EmulationCrtConnection.Component, EmulationCrtConnection.SVideo, EmulationCrtConnection.Scart });
}

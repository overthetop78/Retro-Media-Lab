using RetroMediaLab.VideoPresentation.Enums;

namespace RetroMediaLab.VideoPresentation.Dictionaries;

public static partial class EmulationVideoProcessingCatalog
{
    public static IReadOnlyDictionary<EmulationCrtColorMode, string> VectorPhosphorResourceKeys { get; } =
        ResourceKeys("Vector.Phosphor", new[] { EmulationCrtColorMode.White, EmulationCrtColorMode.Green,
            EmulationCrtColorMode.Amber, EmulationCrtColorMode.Color });
}

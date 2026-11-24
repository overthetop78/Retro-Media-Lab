using System.Numerics;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal readonly record struct PlasmaVideoShaderParameters(Vector4 Effect, Vector4 Temporal, Vector4 Display)
{
    internal const int Dithering = 45;
    internal const int Persistence = 12;
    internal const int PhosphorIntensity = 22;
    internal const int BrightnessLimiter = 65;

    internal static PlasmaVideoShaderParameters From(EmulationVideoProcessingConfiguration configuration,
        bool hasHistory, long sequence, float averageLuminance)
    {
        return new PlasmaVideoShaderParameters(
            new Vector4(configuration.DisplayTechnology == EmulationVideoDisplayTechnology.Plasma ? 1f : 0f,
                (float)configuration.Plasma.CellStructure, 0f, Dithering / 100f),
            new Vector4(Persistence / 100f, hasHistory ? 1f : 0f, Math.Abs(sequence % 16), averageLuminance),
            new Vector4(0f, PhosphorIntensity / 100f, 0f, BrightnessLimiter / 100f));
    }
}

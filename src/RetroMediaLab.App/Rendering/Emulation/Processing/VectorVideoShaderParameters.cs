using System.Numerics;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal readonly record struct VectorVideoShaderParameters(Vector4 Effect, Vector4 Temporal, Vector4 Display)
{
    internal static VectorVideoShaderParameters From(EmulationVideoProcessingConfiguration configuration, bool hasHistory = false)
    {
        var vector = configuration.Vector;
        return new VectorVideoShaderParameters(
            new Vector4(configuration.DisplayTechnology == EmulationVideoDisplayTechnology.Vector ? 1f : 0f, vector.LineThreshold / 100f, vector.LineIntensity / 100f, vector.HaloIntensity / 100f),
            new Vector4(vector.PersistenceIntensity / 100f, hasHistory ? 1f : 0f, 0f, 0f),
            new Vector4(vector.BeamWidth / 100f, vector.BeamFocus / 100f, (float)vector.PhosphorColor, vector.HaloRadius / 100f));
    }
}

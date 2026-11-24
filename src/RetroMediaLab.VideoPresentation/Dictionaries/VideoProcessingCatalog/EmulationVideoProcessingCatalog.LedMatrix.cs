using RetroMediaLab.VideoPresentation.Enums;

namespace RetroMediaLab.VideoPresentation.Dictionaries;

public static partial class EmulationVideoProcessingCatalog
{
    public static IReadOnlyDictionary<EmulationLedMatrixDefinition, string> LedMatrixDefinitionResourceKeys { get; } =
        Enum.GetValues<EmulationLedMatrixDefinition>().ToDictionary(value => value, value =>
            value == EmulationLedMatrixDefinition.Native ? "Emulation.Video.Lcd.Definition.Native" : $"Emulation.Video.LedMatrix.Definition.{value}");

    public static (int Width, int Height) LedMatrixDefinitionDimensions(
        EmulationLedMatrixDefinition definition, int sourceWidth, int sourceHeight) => definition switch
    {
        EmulationLedMatrixDefinition.Size16x16 => (16, 16),
        EmulationLedMatrixDefinition.Size32x16 => (32, 16),
        EmulationLedMatrixDefinition.Size32x32 => (32, 32),
        EmulationLedMatrixDefinition.Size64x32 => (64, 32),
        EmulationLedMatrixDefinition.Size64x64 => (64, 64),
        EmulationLedMatrixDefinition.Size128x64 => (128, 64),
        EmulationLedMatrixDefinition.Size128x128 => (128, 128),
        EmulationLedMatrixDefinition.Size256x128 => (256, 128),
        EmulationLedMatrixDefinition.Size256x256 => (256, 256),
        _ => (Math.Max(1, sourceWidth), Math.Max(1, sourceHeight))
    };
}

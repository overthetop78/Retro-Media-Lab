namespace RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Dictionaries;

/// <summary>Identités des ROM de la catégorie Speech.</summary>
internal static partial class AcornFirmware
{
    private static IReadOnlyDictionary<string, FirmwareDefinition> SpeechRoms() =>
        new Dictionary<string, FirmwareDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["b369809275cb67dfd8a749265e91adb2d2558ae6"] = new("CM62024 VSM", null, FirmwareCategory.Speech),
            ["66876702d1d95eecc034d20f25047f893a27cde5"] = new("VM61002 VSM", null, FirmwareCategory.Speech)
        };
}

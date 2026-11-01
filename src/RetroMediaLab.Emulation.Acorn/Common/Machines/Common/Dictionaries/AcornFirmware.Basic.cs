namespace RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Dictionaries;

/// <summary>Identités des ROM de la catégorie Basic.</summary>
internal static partial class AcornFirmware
{
    private static IReadOnlyDictionary<string, FirmwareDefinition> BasicRoms() =>
        new Dictionary<string, FirmwareDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["890f6e3e7fab3340f75b85e93ff29332bc9ecb2e"] = new("BASIC I", "I", FirmwareCategory.Basic),
            ["4a7393f3a45ea309f744441c16723e2ef447a281"] = new("BASIC II", "II", FirmwareCategory.Basic),
            ["b39014610a968789afd7695aa04d1277d874405c"] = new("BASIC III US", "III", FirmwareCategory.Basic),
            ["24d903e736aee7bd7bd44dbf8413bbf2d94634a9"] = new("BASIC II Dutch [hacked]", "2", FirmwareCategory.Basic),
            ["0f011a3a7f2a60f478f1ec25d3651608315ce9a4"] = new("BASIC II (65C02)", "2", FirmwareCategory.Basic)
        };
}

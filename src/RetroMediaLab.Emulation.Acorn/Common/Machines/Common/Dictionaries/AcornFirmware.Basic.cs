namespace RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Dictionaries;

/// <summary>Identités des ROM de la catégorie Basic.</summary>
internal static partial class AcornFirmware
{
    private static IReadOnlyDictionary<string, FirmwareDefinition> BasicRoms() =>
        new Dictionary<string, FirmwareDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["ebcde5b36cb3a3344567cbba4c7b9fde015f4802"] = new("Acorn Atom Floating Point", null, FirmwareCategory.Basic),
            ["890f6e3e7fab3340f75b85e93ff29332bc9ecb2e"] = new("BASIC I", "I", FirmwareCategory.Basic),
            ["4a7393f3a45ea309f744441c16723e2ef447a281"] = new("BASIC II", "II", FirmwareCategory.Basic),
            ["b39014610a968789afd7695aa04d1277d874405c"] = new("BASIC III US", "III", FirmwareCategory.Basic),
            ["24d903e736aee7bd7bd44dbf8413bbf2d94634a9"] = new("BASIC II Dutch [hacked]", "2", FirmwareCategory.Basic),
            ["0f011a3a7f2a60f478f1ec25d3651608315ce9a4"] = new("BASIC II (65C02)", "2", FirmwareCategory.Basic),
            ["2278018884c65ef7b4edd174505ac1138e4dc2db"] = new("Acorn Master Compact 5.00 basic4", null, FirmwareCategory.Basic),
            ["c45fd3c0abdcd32b78d2288addd2a777b19e9a1d"] = new("Acorn Master ET BASIC4", null, FirmwareCategory.Basic),
        };
}

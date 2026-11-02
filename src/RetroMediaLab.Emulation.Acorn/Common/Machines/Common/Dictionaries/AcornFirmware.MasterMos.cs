namespace RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Dictionaries;

internal static partial class AcornFirmware
{
    private static IReadOnlyDictionary<string, FirmwareDefinition> MasterMosRoms() =>
        new Dictionary<string, FirmwareDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["8720284aafa21928c519be806b0383f4a3b50088"] = new("Acorn Master Compact 5.00 mos", null, FirmwareCategory.MasterMos),
            ["844b056bcfb4a585fc5fd0f0bdfd8a849e686ce2"] = new("Acorn Master Compact 5.10 mos", null, FirmwareCategory.MasterMos),
            ["c9483ef3ebd66930e635674a3776d171785ae959"] = new("Acorn Master ET MOS400", null, FirmwareCategory.MasterMos),
            ["0275719aa7746dd3b627f95ccc4362b564063a5e"] = new("Acorn Master MOS 3.20 (128K)", null, FirmwareCategory.MasterMos),
            ["85211b5bc7c7a269952d2b063b7ec0e1f0196803"] = new("Acorn Master MOS 3.50 (128K)", null, FirmwareCategory.MasterMos),
        };
}

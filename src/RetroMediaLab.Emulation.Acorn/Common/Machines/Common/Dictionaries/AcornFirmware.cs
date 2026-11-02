namespace RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Dictionaries;

/// <summary>Réunit les identités Acorn déclarées par catégorie ; les règles de modèles restent dans les profils des machines.</summary>
internal static partial class AcornFirmware
{
    internal static IReadOnlyDictionary<string, FirmwareDefinition> BySha1 { get; } = Assemble(
        DiscFilingSystemRoms(),
        OperatingSystemRoms(),
        MemoryExtensionRoms(),
        NetworkRoms(),
        CharacterGeneratorRoms(),
        BasicRoms(),
        LanguageRoms(),
        SpeechRoms(),
        ApplicationRoms(),
        MasterMosRoms(),
        CoprocessorRoms());

    private static IReadOnlyDictionary<string, FirmwareDefinition> Assemble(params IReadOnlyDictionary<string, FirmwareDefinition>[] categories)
    {
        var roms = new Dictionary<string, FirmwareDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var category in categories)
            foreach (var rom in category) roms.Add(rom.Key, rom.Value);
        return roms;
    }

    /// <summary>Appartenances générales aux rôles ; aucune compatibilité de modèle n'est déduite de ces listes.</summary>
    internal static IReadOnlyDictionary<FirmwareCategory, IReadOnlySet<string>> ByCategory { get; } = CreateCategories();

    private static IReadOnlyDictionary<FirmwareCategory, IReadOnlySet<string>> CreateCategories()
    {
        var categories = Enum.GetValues<FirmwareCategory>().ToDictionary(category => category,
            _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        foreach (var entry in BySha1)
        {
            categories[entry.Value.Category].Add(entry.Key);
            if (entry.Value.Combined) categories[FirmwareCategory.Basic].Add(entry.Key);
        }
        // Ces images contiennent à la fois le système disque et le système réseau.
        categories[FirmwareCategory.Network].UnionWith([
            "161f585dc45665ea77433c84afd2f95049f7f5a0",
            "7e3c536baeae84d6498a14e8405319e01ee78232"]);
        return categories.ToDictionary(entry => entry.Key, entry => (IReadOnlySet<string>)entry.Value);
    }

}

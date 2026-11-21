using System.IO;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Dictionaries;

namespace RetroMediaLab.Emulation.Sega.Common.Machines.Common.Functions;

internal static class FirmwareConfigurationFunctions
{
    private static readonly IReadOnlyDictionary<string, string[]> FirmwareFileAliases = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["dc_nvmem.bin"] = [],
        ["font.bin"] = [],
        ["naomi_boot.bin"] = [],
        ["sc3000.rom"] = [],
        ["sc3000-variant.rom"] = [],
        ["Mega CD - M2 V2.00.bin"] = ["bios_CD_U.bin", "bios_CD_E.bin", "bios_CD_J.bin"],
        ["Mega CD - M2 V2.00w.bin"] = ["bios_CD_U.bin", "bios_CD_E.bin", "bios_CD_J.bin"],
        ["Sega Saturn BIOS v1.01 (1995)(Sega)(JP)(M6)[h].bin"] = ["sega_101.bin", "saturn_bios.bin"]
    };

    private static readonly HashSet<string> FirmwareSlotNames = CoreCatalog.All.SelectMany(core => core.Firmware)
        .SelectMany(SlotFileNames).ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static IEnumerable<string> SlotFileNames(FirmwareSlot slot) =>
        (slot.CandidateFileNames ?? []).Prepend(slot.FileName).Select(fileName => Path.GetFileName(fileName));

    private static int SelectedRegions(MachineConfiguration? configuration)
    {
        if (configuration?.Options is not { } options) return 0;
        string? region = configuration.EmulatorId == Emulators.SegaEmu.Constants.CoreConstants.Id
            && ModelConstants.IsMegaDriveFamily(configuration.Model)
            ? options.GetValueOrDefault(SettingsConstants.MegaDriveRegion) ?? options.GetValueOrDefault("segaemu_region_detect")
            : CoreCatalog.Get(configuration.EmulatorId).Options
                .Where(option => option.Key.EndsWith("_region", StringComparison.Ordinal)
                    || option.Key.EndsWith("_region_detect", StringComparison.Ordinal))
                .Select(option => options.GetValueOrDefault(option.Key)).FirstOrDefault(value => value is not null);
        return region?.ToLowerInvariant() switch
        {
            "pal" or "europe" or "secam" => 8,
            "ntsc-u" or "usa" or "us" or "north_america" or "north america" => 4,
            "ntsc-j" or "japan" or "japan ntsc" or "japan pal" or "master system japan" or "game gear japan" => 1,
            "master system export" or "game gear export" => 12,
            _ => 0
        };
    }

    private static int FirmwareRegions(IEnumerable<string> fileNames) => fileNames.Aggregate(0, (regions, fileName) =>
        regions | (Path.GetFileName(fileName).ToLowerInvariant() switch
        {
            "bios_j.sms" or "bios_cd_j.bin" or "sega_100.bin" or "sega_100a.bin" or "sega1003.bin" or "sega_101.bin" => 1,
            "bios_u.sms" or "bios_cd_u.bin" => 4,
            "bios_e.sms" or "bios_cd_e.bin" => 8,
            "mpr-17933.bin" or "mpr-18100.bin" => 12,
            _ => 0
        }));

    internal static IReadOnlyList<EmulationSettingsField> Fields(MachineConfiguration configuration, IEmulatorAdapter adapter) =>
        adapter.GetFirmwareSlots(configuration).Select(slot => new EmulationSettingsField(slot.FieldId,
            EmulationMachineTab.Rom, SettingsDescriptionFunctionsConstants.Firmware,
            slot.LabelResourceKey, EmulationSettingsEditor.Path,
            configuration.FirmwarePaths?.GetValueOrDefault(slot.FieldId),
            DefaultFolderCategory: EmulationDefaultFolderCategory.Firmware)).ToArray();

    internal static MachineConfiguration Apply(MachineConfiguration configuration, IReadOnlyDictionary<string, string?> values, IEmulatorAdapter adapter)
    {
        var paths = new Dictionary<string, string>(configuration.FirmwarePaths ?? new Dictionary<string, string>(), StringComparer.Ordinal);
        foreach (var slot in adapter.GetFirmwareSlots(configuration))
            if (values.TryGetValue(slot.FieldId, out var value))
            {
                if (string.IsNullOrWhiteSpace(value)) paths.Remove(slot.FieldId);
                else paths[slot.FieldId] = value;
            }
        return configuration with { FirmwarePaths = paths };
    }

    /// <summary>Inspecte un fichier et conserve tous ses rôles de firmware possibles.</summary>
    /// <param name="configuration">Configuration empruntée dont les emplacements sont consultés.</param>
    /// <param name="path">Fichier source, conservé en lecture seule.</param>
    /// <param name="adapter">Backend emprunté fournissant ses emplacements de firmware.</param>
    /// <param name="cancellationToken">Annulation contrôlée avant la lecture du fichier.</param>
    /// <returns>Tous les candidats reconnus ou possibles, avec leurs identités et destinations.</returns>
    internal static ValueTask<IReadOnlyList<EmulationFirmwareCandidate>> ScanFileAsync(MachineConfiguration configuration, string path, IEmulatorAdapter adapter, CancellationToken cancellationToken) =>
        ValueTask.FromResult(InspectFile(configuration, path, adapter.GetFirmwareSlots(configuration), cancellationToken));

    /// <summary>Réutilise les emplacements déjà obtenus par le parcours de dossier et libère les lectures avant le retour.</summary>
    private static IReadOnlyList<EmulationFirmwareCandidate> InspectFile(MachineConfiguration configuration, string path, IReadOnlyList<FirmwareSlot> slots, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (slots.Count == 0) return [];
        return Candidates(configuration.Model, FirmwareCatalog.Inspect(path, cancellationToken), slots, configuration);
    }

    internal static IReadOnlyList<EmulationFirmwareCandidate> Candidates(string machineId, Firmware firmware, IReadOnlyList<FirmwareSlot> slots,
        MachineConfiguration? configuration = null)
    {
        string name = Path.GetFileName(firmware.Path);
        var destinationNames = FirmwareFileAliases.GetValueOrDefault(name);
        int selectedRegions = SelectedRegions(configuration);
        int firmwareRegions = FirmwareRegions(firmware.IsKnown ? firmware.ExpectedFileNames ?? [] : destinationNames ?? [name]);
        bool wrongRegion = selectedRegions != 0 && firmwareRegions != 0 && (selectedRegions & firmwareRegions) == 0;
        if (!firmware.IsKnown)
        {
            // Names can identify a foreign firmware slot. Content hashes are still required for official compatibility.
            bool hasKnownRole = destinationNames is not null || FirmwareSlotNames.Contains(name);
            bool incompatible = wrongRegion || hasKnownRole && !slots.SelectMany(SlotFileNames)
                .Intersect(destinationNames ?? [name], StringComparer.OrdinalIgnoreCase).Any();
            return [new(firmware.Sha256, firmware.Path, name, null, incompatible
                ? EmulationFirmwareCompatibility.Incompatible : EmulationFirmwareCompatibility.Unknown)];
        }

        bool Matches(string fileName) => firmware.ExpectedFileNames?.Any(expected =>
            string.Equals(expected.Replace('\\', '/'), fileName.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase)) == true;
        var targets = !wrongRegion && (firmware.CompatibleModels.Contains(machineId, StringComparer.Ordinal)
            || ModelConstants.IsMegaDriveFamily(machineId)
            && firmware.CompatibleModels.Contains(ModelConstants.MegaDrive, StringComparer.Ordinal))
            ? slots.Where(slot => slot.KnownMd5 is { } md5
                ? string.Equals(md5, firmware.Md5, StringComparison.OrdinalIgnoreCase)
                : Matches(slot.FileName) || slot.CandidateFileNames?.Any(Matches) == true).ToArray()
            : [];
        string displayName = firmware.Name ?? name;
        if (targets.Length == 0)
            return [new(firmware.Sha256, firmware.Path, displayName, firmware.Version, EmulationFirmwareCompatibility.Incompatible)];
        return targets.Select(slot => new EmulationFirmwareCandidate(firmware.Sha256 + slot.FieldId, firmware.Path,
            displayName, firmware.Version, firmware.IsOfficial ? EmulationFirmwareCompatibility.Official : EmulationFirmwareCompatibility.Compatible,
            slot.FieldId)).ToArray();
    }

}

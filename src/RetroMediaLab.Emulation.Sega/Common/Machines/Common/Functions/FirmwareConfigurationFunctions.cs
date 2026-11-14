using System.IO;

namespace RetroMediaLab.Emulation.Sega.Common.Machines.Common.Functions;

internal static class FirmwareConfigurationFunctions
{
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
        ValueTask.FromResult(InspectFile(configuration.Model, path, adapter.GetFirmwareSlots(configuration), cancellationToken));

    /// <summary>Réutilise les emplacements déjà obtenus par le parcours de dossier et libère les lectures avant le retour.</summary>
    private static IReadOnlyList<EmulationFirmwareCandidate> InspectFile(string machineId, string path, IReadOnlyList<FirmwareSlot> slots, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (slots.Count == 0) return [];
        return Candidates(machineId, FirmwareCatalog.Inspect(path, cancellationToken), slots);
    }

    internal static IReadOnlyList<EmulationFirmwareCandidate> Candidates(string machineId, Firmware firmware, IReadOnlyList<FirmwareSlot> slots)
    {
        string name = Path.GetFileName(firmware.Path);
        if (!firmware.IsKnown)
            return [new(firmware.Sha256, firmware.Path, name, null, EmulationFirmwareCompatibility.Unknown)];

        bool Matches(string fileName) => firmware.ExpectedFileNames?.Any(expected =>
            string.Equals(expected.Replace('\\', '/'), fileName.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase)) == true;
        var targets = firmware.CompatibleModels.Contains(machineId, StringComparer.Ordinal)
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

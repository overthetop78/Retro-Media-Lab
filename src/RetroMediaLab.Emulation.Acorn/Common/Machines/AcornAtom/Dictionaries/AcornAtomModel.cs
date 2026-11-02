namespace RetroMediaLab.Emulation.Acorn.Common.Machines.AcornAtom.Dictionaries;

/// <summary>Identités compatibles avec les puces ROM et le Disc Pack d’Atom.</summary>
internal static class AcornAtomModel
{
    private static readonly IReadOnlyDictionary<FirmwareCategory, IReadOnlySet<string>> Official =
        new Dictionary<FirmwareCategory, IReadOnlySet<string>>
        {
            [FirmwareCategory.OperatingSystem] = Roms("0072c83458a9690a3ea1f6094f0f38cf8e96a445"),
            [FirmwareCategory.Basic] = Roms("ebcde5b36cb3a3344567cbba4c7b9fde015f4802"),
            [FirmwareCategory.CharacterGenerator] = Roms("5e9d68e55e73cae3d28adaff34fe115e00029009"),
            [FirmwareCategory.DiscFilingSystem] = Roms("71ea0a4b8d9c3caf9718fc7cc279f4306a23b39c")
        };

    internal static EmulationFirmwareCompatibility Get(string hash, FirmwareCategory role)
    {
        if (Official.GetValueOrDefault(role)?.Contains(hash) == true) return EmulationFirmwareCompatibility.Official;
        return AcornFirmware.BySha1.ContainsKey(hash)
            ? EmulationFirmwareCompatibility.Incompatible : EmulationFirmwareCompatibility.Unknown;
    }

    private static IReadOnlySet<string> Roms(params string[] hashes) => hashes.ToHashSet(StringComparer.OrdinalIgnoreCase);
}

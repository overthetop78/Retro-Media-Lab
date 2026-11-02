namespace RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Dictionaries;

/// <summary>Résolution par rôle demandé, puis par modèle. Les identités et appartenances générales restent dans AcornFirmware.</summary>
internal static partial class AcornBBCMicroModel
{
    internal static EmulationFirmwareCompatibility Get(MachineModel model, string sha1) =>
        AcornFirmware.BySha1.TryGetValue(sha1, out var firmware) ? Get(model, sha1, firmware.Category) : EmulationFirmwareCompatibility.Unknown;

    internal static EmulationFirmwareCompatibility Get(MachineModel model, string sha1, FirmwareCategory category)
    {
        if (!AcornFirmware.BySha1.ContainsKey(sha1)) return EmulationFirmwareCompatibility.Unknown;
        return Rule(model, sha1, category)
            ?? (AcornFirmware.ByCategory[category].Contains(sha1) ? EmulationFirmwareCompatibility.Unknown : EmulationFirmwareCompatibility.Incompatible);
    }

    internal static IEnumerable<FirmwareCategory> Roles(MachineModel model, string sha1) =>
        Enum.GetValues<FirmwareCategory>().Where(category => AcornFirmware.ByCategory[category].Contains(sha1) || Rule(model, sha1, category).HasValue);

    private static EmulationFirmwareCompatibility? Rule(MachineModel model, string sha1, FirmwareCategory category)
    {
        if (AcornMasterModel.IsMaster(model))
            return category == FirmwareCategory.CharacterGenerator ? CharacterRom(sha1)
                : AcornMasterModel.Get(model, sha1, category);
        if (!IsStandard(model) && !IsPlus(model)) return null;
        var rule = category switch
        {
            FirmwareCategory.OperatingSystem => SystemRom(model, sha1),
            FirmwareCategory.Basic => BasicRom(sha1),
            FirmwareCategory.CharacterGenerator => CharacterRom(sha1),
            FirmwareCategory.DiscFilingSystem => DiscRom(model, sha1),
            FirmwareCategory.Network => NetworkRom(sha1),
            FirmwareCategory.Application => ApplicationRom(sha1),
            FirmwareCategory.Speech or FirmwareCategory.MasterMos => AcornFirmware.ByCategory[category].Contains(sha1) ? EmulationFirmwareCompatibility.Incompatible : (EmulationFirmwareCompatibility?)null,
            _ => null
        };
        if (rule.HasValue) return rule;
        return IsStandard(model) && BPlusOnly.TryGetValue(category, out var restricted) && restricted.Contains(sha1)
            ? EmulationFirmwareCompatibility.Incompatible : null;
    }

    private static bool IsStandard(MachineModel model) => model is MachineModel.BbcMicroModelA or MachineModel.BbcMicroModelB;
    private static bool IsPlus(MachineModel model) => model is MachineModel.BbcMicroModelBPlus or MachineModel.BbcMicroModelBPlus128;
    private static IReadOnlySet<string> Roms(params string[] hashes) => hashes.ToHashSet(StringComparer.OrdinalIgnoreCase);
}

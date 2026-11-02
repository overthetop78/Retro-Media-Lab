namespace RetroMediaLab.Emulation.Acorn.Common.Machines.AcornSystem.Dictionaries;

/// <summary>Appartenances des ROM aux cartes System ; une ROM connue d’un autre rôle reste incompatible.</summary>
internal static class AcornSystemModel
{
    private static readonly IReadOnlyDictionary<MachineModel, IReadOnlySet<string>> Monitors =
        new Dictionary<MachineModel, IReadOnlySet<string>>
        {
            [MachineModel.AcornSystem1] = Roms("b987354c55beb5e2ced761970c3339b895a8c09d"),
            [MachineModel.AcornSystem2] = Roms("c587da5dcc6878dcd0bc823c508472d38296003e"),
            [MachineModel.AcornSystem3] = Roms("4cb322dadcfba9c452797d6cc2096f0c92e8792c"),
            [MachineModel.AcornSystem4] = Roms("4cb322dadcfba9c452797d6cc2096f0c92e8792c"),
            [MachineModel.AcornSystem5] = Roms("e588298239b5360b5d1e15d5cd9f7fe2b1693e5d")
        };
    private static readonly IReadOnlySet<string> MotorolaMonitors = Roms("b14a884bf82a7a8c23bc03c2e112728dd1a74896");
    private static readonly IReadOnlySet<string> Teletext = Roms("6c8daba70374e5aa3a6402f24cdc5f8677d58a0f");
    private static readonly IReadOnlySet<string> Monochrome = Roms("04c326a745ba6a78185ebc2adeecb53a166a6ab3");
    private static readonly IReadOnlySet<string> CompatibleDos40 = Roms("3becc10f6a938971f83b8898c28cfdd3e6897a41", "8d492106a1b06aa8ae2744919fcd83c7b42a2612");
    private static readonly IReadOnlySet<string> CompatibleSystemFive = Roms("3f787ab83e41e50889e367cbccddfa716b6a5c2b4");

    internal static EmulationFirmwareCompatibility Get(MachineModel model, HardwareCpu cpu, string hash, FirmwareCategory role)
    {
        if (!AcornFirmware.BySha1.ContainsKey(hash)) return EmulationFirmwareCompatibility.Unknown;
        if (role == FirmwareCategory.OperatingSystem && model == MachineModel.AcornSystem5 && CompatibleSystemFive.Contains(hash))
            return EmulationFirmwareCompatibility.Compatible;
        if (role == FirmwareCategory.OperatingSystem && cpu != HardwareCpu.Motorola6809
            && model is MachineModel.AcornSystem3 or MachineModel.AcornSystem4 && CompatibleDos40.Contains(hash))
            return EmulationFirmwareCompatibility.Compatible;
        var accepted = role switch
        {
            FirmwareCategory.OperatingSystem => cpu == HardwareCpu.Motorola6809 ? MotorolaMonitors
                : Monitors.GetValueOrDefault(model),
            FirmwareCategory.CharacterGenerator when model != MachineModel.AcornSystem1 =>
                model == MachineModel.AcornSystem5 ? Monochrome : Teletext,
            _ => null
        };
        return accepted?.Contains(hash) == true ? EmulationFirmwareCompatibility.Official : EmulationFirmwareCompatibility.Incompatible;
    }

    private static IReadOnlySet<string> Roms(params string[] hashes) => hashes.ToHashSet(StringComparer.OrdinalIgnoreCase);
}

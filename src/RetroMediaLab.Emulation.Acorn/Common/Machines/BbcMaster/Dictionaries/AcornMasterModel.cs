namespace RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMaster.Dictionaries;

internal static class AcornMasterModel
{
    internal static bool IsMaster(MachineModel model) => model is MachineModel.BbcMaster128 or MachineModel.BbcMasterET or MachineModel.BbcMasterCompact;
    private static IReadOnlySet<string> Roms(params string[] hashes) => hashes.ToHashSet(StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlySet<string> CompatibleCoprocessorRoms = Roms("eb2a043d139ae0f3d512b4f1afa32286510c886c", "8acaf42a4a6b5f62f6be5e62df5fcffbf48890dd");
    // AMX 2.1 variant 4 : OSWORD 64 fonctionne ; le pointeur de l'ancien menu échoue.
    private static readonly IReadOnlySet<string> PartialPointerRoms = Roms("50eae23d4686f5f9547c741517af23dc27b75a2c");
    private static readonly IReadOnlyDictionary<MachineModel, IReadOnlyDictionary<FirmwareCategory, IReadOnlySet<string>>> Profiles =
        new Dictionary<MachineModel, IReadOnlyDictionary<FirmwareCategory, IReadOnlySet<string>>>
        {
            [MachineModel.BbcMaster128] = new Dictionary<FirmwareCategory, IReadOnlySet<string>>
            {
                [FirmwareCategory.MasterMos] = Roms("0275719aa7746dd3b627f95ccc4362b564063a5e", "85211b5bc7c7a269952d2b063b7ec0e1f0196803"),
                [FirmwareCategory.Coprocessor] = Roms("d95815c367e07f2d5a00e860092e7d6da795cbe5")
            },
            [MachineModel.BbcMasterET] = new Dictionary<FirmwareCategory, IReadOnlySet<string>>
            {
                [FirmwareCategory.MasterMos] = Roms("c9483ef3ebd66930e635674a3776d171785ae959"),
                [FirmwareCategory.Basic] = Roms("c45fd3c0abdcd32b78d2288addd2a777b19e9a1d"),
                [FirmwareCategory.Network] = Roms("14f75d36ffe9af14aaac42df55b4fe3729ba75cf"),
                [FirmwareCategory.Application] = Roms("0b4425149ca93dddf8d03da3767635dc3f1eecb8")
            },
            [MachineModel.BbcMasterCompact] = new Dictionary<FirmwareCategory, IReadOnlySet<string>>
            {
                [FirmwareCategory.MasterMos] = Roms("8720284aafa21928c519be806b0383f4a3b50088", "844b056bcfb4a585fc5fd0f0bdfd8a849e686ce2"),
                [FirmwareCategory.Basic] = Roms("2278018884c65ef7b4edd174505ac1138e4dc2db"),
                [FirmwareCategory.DiscFilingSystem] = Roms("e5b6988ada0971761cd4804fe0e6fb2457f9e965", "a080fff109e19d189c324eb8537e1764b6fb0a01"),
                [FirmwareCategory.Application] = Roms("b8ebb39e6373ddaa81fb0c6eb572a9f1a30402c4", "8134476d88509b024fbc81cba1b4e89cb8e3d655")
            }
        };
    internal static EmulationFirmwareCompatibility? Get(MachineModel model, string hash, FirmwareCategory category)
    {
        if (model == MachineModel.BbcMaster128 && category == FirmwareCategory.Coprocessor && CompatibleCoprocessorRoms.Contains(hash)) return EmulationFirmwareCompatibility.Compatible;
        if (model == MachineModel.BbcMaster128 && category == FirmwareCategory.Application && PartialPointerRoms.Contains(hash)) return EmulationFirmwareCompatibility.PartiallyCompatible;
        if (Profiles[model].TryGetValue(category, out var entries) && entries.Contains(hash)) return EmulationFirmwareCompatibility.Official;
        if (category == FirmwareCategory.OperatingSystem || Profiles.Values.Any(profile => profile.TryGetValue(category, out var hashes) && hashes.Contains(hash))) return EmulationFirmwareCompatibility.Incompatible;
        if (category == FirmwareCategory.DiscFilingSystem && AcornFirmware.BySha1[hash].DiskController == FirmwareDiskController.Intel8271) return EmulationFirmwareCompatibility.Incompatible;
        return null;
    }
}

namespace RetroMediaLab.Emulation.Acorn.Common.Machines.AcornElectron.Dictionaries;

internal static class AcornElectronModel
{
    private static readonly IReadOnlyDictionary<FirmwareCategory, IReadOnlySet<string>> Official =
        new Dictionary<FirmwareCategory, IReadOnlySet<string>>
        {
            [FirmwareCategory.OperatingSystem] = Roms("4a66c83aba07d0a8e76ed8a5545af04e11c19fdc", "9ac82b4ab1be8d4456a9946b3931cdbdc759a167"),
            [FirmwareCategory.Basic] = Roms("4a7393f3a45ea309f744441c16723e2ef447a281", "4a66c83aba07d0a8e76ed8a5545af04e11c19fdc")
        };

    internal static EmulationFirmwareCompatibility Get(string hash, FirmwareCategory role)
    {
        if (Official.GetValueOrDefault(role)?.Contains(hash) == true) return EmulationFirmwareCompatibility.Official;
        return AcornFirmware.BySha1.ContainsKey(hash) ? EmulationFirmwareCompatibility.Incompatible : EmulationFirmwareCompatibility.Unknown;
    }

    private static IReadOnlySet<string> Roms(params string[] hashes) => hashes.ToHashSet(StringComparer.OrdinalIgnoreCase);
}

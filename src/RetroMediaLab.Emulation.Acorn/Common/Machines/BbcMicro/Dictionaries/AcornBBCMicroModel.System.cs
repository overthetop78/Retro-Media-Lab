namespace RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Dictionaries;

internal static partial class AcornBBCMicroModel
{
    private static readonly IReadOnlySet<string> StandardSystemOfficial = Roms(
        "4b0ece6dc139d5d3f4fabd023716fb6f25149b80", "dca33995c0d008a527efe923d03333394b01022c",
        "d35f6723132aabe3c4d00fc16fd9ecc6768df753", "0d9bcaf6a393c9ce2359ed700ddb53c232c2c45d");
    private static readonly IReadOnlySet<string> BPlusSystemOfficial = Roms("ea7d3a7e3ee1ecfaa1483af994048057362b01f2");

    private static EmulationFirmwareCompatibility? SystemRom(MachineModel model, string sha1)
    {
        if (IsStandard(model) && StandardSystemOfficial.Contains(sha1) || IsPlus(model) && BPlusSystemOfficial.Contains(sha1))
            return EmulationFirmwareCompatibility.Official;
        if (IsStandard(model) && BPlusSystemOfficial.Contains(sha1)) return EmulationFirmwareCompatibility.Incompatible;
        if (IsPlus(model) && AcornFirmware.BySha1[sha1].Category == FirmwareCategory.OperatingSystem && !AcornFirmware.BySha1[sha1].Combined)
            return EmulationFirmwareCompatibility.Incompatible;
        return null;
    }
}

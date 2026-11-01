namespace RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Dictionaries;

internal static partial class AcornBBCMicroModel
{
    private static readonly IReadOnlySet<string> BasicOfficial = Roms(
        "890f6e3e7fab3340f75b85e93ff29332bc9ecb2e", "4a7393f3a45ea309f744441c16723e2ef447a281",
        "ea7d3a7e3ee1ecfaa1483af994048057362b01f2");
    private static readonly IReadOnlySet<string> BasicRequires65C02 = Roms("0f011a3a7f2a60f478f1ec25d3651608315ce9a4");

    private static EmulationFirmwareCompatibility? BasicRom(string sha1) =>
        BasicOfficial.Contains(sha1) ? EmulationFirmwareCompatibility.Official
            : BasicRequires65C02.Contains(sha1) ? EmulationFirmwareCompatibility.Incompatible : null;
}

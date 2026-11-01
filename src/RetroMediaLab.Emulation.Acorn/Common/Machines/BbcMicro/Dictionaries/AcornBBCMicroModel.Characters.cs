namespace RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Dictionaries;

internal static partial class AcornBBCMicroModel
{
    private static readonly IReadOnlySet<string> CharacterOfficial = Roms("6c8daba70374e5aa3a6402f24cdc5f8677d58a0f");
    private static EmulationFirmwareCompatibility? CharacterRom(string sha1) =>
        CharacterOfficial.Contains(sha1) ? EmulationFirmwareCompatibility.Official : null;
}

namespace RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Dictionaries;

internal static partial class AcornBBCMicroModel
{
    private static readonly IReadOnlySet<string> BPlusDiscOfficial = Roms(
        "0d7ed0b0b3852cb61970ada1993244f2896896aa", "cf2ebc422a8d24ec6f1a0320520c38a0e704109a");
    // Lecture DFS 0.9H vérifiée avec le 8271 du cœur A/B ; le choix de l'interface vérifie séparément le contrôleur.
    private static readonly IReadOnlySet<string> StandardDiscCompatible = Roms("dbbec4d2540a854c120be3194c7566a2b79d153b");

    private static EmulationFirmwareCompatibility? DiscRom(MachineModel model, string sha1)
    {
        if (IsPlus(model) && BPlusDiscOfficial.Contains(sha1)) return EmulationFirmwareCompatibility.Official;
        if (IsStandard(model) && StandardDiscCompatible.Contains(sha1)) return EmulationFirmwareCompatibility.Compatible;
        var controller = AcornFirmware.BySha1[sha1].DiskController;
        if (IsPlus(model) && controller.HasFlag(FirmwareDiskController.Intel8271) && !controller.HasFlag(FirmwareDiskController.AcornWd1770))
            return EmulationFirmwareCompatibility.Incompatible;
        return null;
    }
}

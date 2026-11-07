namespace RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Dictionaries;

internal static partial class AcornBBCMicroModel
{
    // Logiciels des équipements Torch/Cisco non présents dans les machines BBC Micro émulées.
    private static readonly IReadOnlySet<string> UnsupportedSoftware = Roms(
        "be10872aeb88714bd56462a2e86929953dee1c01", "409762deafb76b1f86be39bfbf2f812d5de3ff92",
        "4398ba29c871fa397654aa182c63ccdcad597625", "5ea266f47ff83821ccdbec006b8506b2e892b115",
        "e37d043c8df30c49ba8717e1aa0b92105cb0c937");
    // AMX 2.1 variant 4 : coordonnées OSWORD 64 vérifiées sur B et les deux B+.
    private static readonly IReadOnlySet<string> VerifiedPointerRoms = Roms("50eae23d4686f5f9547c741517af23dc27b75a2c");

    private static EmulationFirmwareCompatibility? ApplicationRom(MachineModel model, string sha1) =>
        UnsupportedSoftware.Contains(sha1) ? EmulationFirmwareCompatibility.Incompatible
            : (model == MachineModel.BbcMicroModelB || IsPlus(model)) && VerifiedPointerRoms.Contains(sha1)
                ? EmulationFirmwareCompatibility.Compatible : null;
}

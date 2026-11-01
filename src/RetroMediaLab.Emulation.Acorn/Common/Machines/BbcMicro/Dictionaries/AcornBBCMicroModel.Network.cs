namespace RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Dictionaries;

internal static partial class AcornBBCMicroModel
{
    // Cette ROM Torch ne fonctionne pas comme une ROM NFS Acorn avec l'interface émulée.
    private static readonly IReadOnlySet<string> UnsupportedNetwork = Roms("7462ced7b83d74b822815bc00ed40a89f84e0276");
    private static EmulationFirmwareCompatibility? NetworkRom(string sha1) =>
        UnsupportedNetwork.Contains(sha1) ? EmulationFirmwareCompatibility.Incompatible : null;
}

using RetroMediaLab.Emulation.Atari.Emulators.BeetleLynx.Constants;

namespace RetroMediaLab.Emulation.Atari.Emulators.BeetleLynx.Factories;

internal sealed class BeetleLynxMachineFactory() : MachineFactory(EmulatorConstants.Entry)
{
    internal override IReadOnlySet<string> CartridgeExtensions => EmulatorConstants.CartridgeExtensions;
}

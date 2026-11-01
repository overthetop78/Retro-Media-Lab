using RetroMediaLab.Emulation.Atari.Emulators.ProSystem.Constants;

namespace RetroMediaLab.Emulation.Atari.Emulators.ProSystem.Factories;

internal sealed class ProSystemMachineFactory() : MachineFactory(EmulatorConstants.Entry)
{
    internal override IReadOnlySet<string> CartridgeExtensions => EmulatorConstants.CartridgeExtensions;
}

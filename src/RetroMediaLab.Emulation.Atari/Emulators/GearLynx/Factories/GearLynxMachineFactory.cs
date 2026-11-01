using RetroMediaLab.Emulation.Atari.Emulators.GearLynx.Constants;

namespace RetroMediaLab.Emulation.Atari.Emulators.GearLynx.Factories;

internal sealed class GearLynxMachineFactory() : MachineFactory(EmulatorConstants.Entry)
{
    internal override IReadOnlySet<string> CartridgeExtensions => EmulatorConstants.CartridgeExtensions;
}

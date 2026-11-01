using RetroMediaLab.Emulation.Sega.Emulators.BeetleSaturn.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Sega.Emulators.BeetleSaturn.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.BeetleSaturn;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

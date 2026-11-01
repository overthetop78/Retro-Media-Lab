using RetroMediaLab.Emulation.Sega.Emulators.SmsPlusGX.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Sega.Emulators.SmsPlusGX.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.SmsPlusGX;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

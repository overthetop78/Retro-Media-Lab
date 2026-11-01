using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXWide.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXWide.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.GenesisPlusGXWide;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

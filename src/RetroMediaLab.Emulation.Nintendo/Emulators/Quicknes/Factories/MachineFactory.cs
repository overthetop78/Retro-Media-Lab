using RetroMediaLab.Emulation.Nintendo.Emulators.Quicknes.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.Quicknes.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Quicknes;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

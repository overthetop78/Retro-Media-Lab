using RetroMediaLab.Emulation.Nintendo.Emulators.Fixnes.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.Fixnes.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Fixnes;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

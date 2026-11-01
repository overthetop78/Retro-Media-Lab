using RetroMediaLab.Emulation.Nintendo.Emulators.BsnesCpp98.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.BsnesCpp98.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.BsnesCpp98;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

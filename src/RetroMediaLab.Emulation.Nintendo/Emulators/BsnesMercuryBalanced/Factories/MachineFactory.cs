using RetroMediaLab.Emulation.Nintendo.Emulators.BsnesMercuryBalanced.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.BsnesMercuryBalanced.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.BsnesMercuryBalanced;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

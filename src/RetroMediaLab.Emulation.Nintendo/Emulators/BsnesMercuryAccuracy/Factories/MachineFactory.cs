using RetroMediaLab.Emulation.Nintendo.Emulators.BsnesMercuryAccuracy.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.BsnesMercuryAccuracy.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.BsnesMercuryAccuracy;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

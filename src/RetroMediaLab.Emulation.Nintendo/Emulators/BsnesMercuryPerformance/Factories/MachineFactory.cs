using RetroMediaLab.Emulation.Nintendo.Emulators.BsnesMercuryPerformance.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.BsnesMercuryPerformance.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.BsnesMercuryPerformance;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

using RetroMediaLab.Emulation.Nintendo.Emulators.Bsnes2014Balanced.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.Bsnes2014Balanced.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Bsnes2014Balanced;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

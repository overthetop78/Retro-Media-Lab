using RetroMediaLab.Emulation.Nintendo.Emulators.DoubleCherryGB.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.DoubleCherryGB.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.DoubleCherryGB;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

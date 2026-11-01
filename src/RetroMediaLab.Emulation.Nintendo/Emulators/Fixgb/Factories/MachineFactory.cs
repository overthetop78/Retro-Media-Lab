using RetroMediaLab.Emulation.Nintendo.Emulators.Fixgb.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.Fixgb.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Fixgb;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

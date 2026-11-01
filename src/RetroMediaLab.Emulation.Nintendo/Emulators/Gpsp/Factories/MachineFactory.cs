using RetroMediaLab.Emulation.Nintendo.Emulators.Gpsp.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.Gpsp.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Gpsp;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

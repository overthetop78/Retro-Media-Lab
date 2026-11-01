using RetroMediaLab.Emulation.Nintendo.Emulators.VbaNext.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.VbaNext.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.VbaNext;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

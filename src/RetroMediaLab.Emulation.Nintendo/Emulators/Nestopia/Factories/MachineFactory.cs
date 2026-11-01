using RetroMediaLab.Emulation.Nintendo.Emulators.Nestopia.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.Nestopia.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Nestopia;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

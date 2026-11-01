using RetroMediaLab.Emulation.Nintendo.Emulators.Gearboy.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.Gearboy.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Gearboy;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

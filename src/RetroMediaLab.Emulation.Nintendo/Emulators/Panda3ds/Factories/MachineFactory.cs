using RetroMediaLab.Emulation.Nintendo.Emulators.Panda3ds.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.Panda3ds.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Panda3ds;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

using RetroMediaLab.Emulation.Nintendo.Emulators.Skyemu.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.Skyemu.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Skyemu;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

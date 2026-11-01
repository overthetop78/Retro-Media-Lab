using RetroMediaLab.Emulation.Sega.Emulators.Ymir.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Sega.Emulators.Ymir.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Ymir;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

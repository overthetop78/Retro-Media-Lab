using RetroMediaLab.Emulation.Sega.Emulators.YabaSanshiro.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Sega.Emulators.YabaSanshiro.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.YabaSanshiro;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

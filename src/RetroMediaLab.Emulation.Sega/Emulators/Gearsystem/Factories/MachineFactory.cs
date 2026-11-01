using RetroMediaLab.Emulation.Sega.Emulators.Gearsystem.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Sega.Emulators.Gearsystem.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Gearsystem;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

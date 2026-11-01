using RetroMediaLab.Emulation.Sega.Emulators.ClownMDEmu.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Sega.Emulators.ClownMDEmu.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.ClownMDEmu;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

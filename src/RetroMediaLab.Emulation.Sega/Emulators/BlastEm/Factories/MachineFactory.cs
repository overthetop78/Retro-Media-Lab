using RetroMediaLab.Emulation.Sega.Emulators.BlastEm.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Sega.Emulators.BlastEm.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.BlastEm;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

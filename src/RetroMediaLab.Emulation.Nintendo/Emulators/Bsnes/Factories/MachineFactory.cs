using RetroMediaLab.Emulation.Nintendo.Emulators.Bsnes.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.Bsnes.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Bsnes;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

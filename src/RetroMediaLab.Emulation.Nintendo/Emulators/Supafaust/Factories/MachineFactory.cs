using RetroMediaLab.Emulation.Nintendo.Emulators.Supafaust.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.Supafaust.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Supafaust;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

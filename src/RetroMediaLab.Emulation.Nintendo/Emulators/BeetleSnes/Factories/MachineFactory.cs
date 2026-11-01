using RetroMediaLab.Emulation.Nintendo.Emulators.BeetleSnes.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.BeetleSnes.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.BeetleSnes;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

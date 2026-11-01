using RetroMediaLab.Emulation.Nintendo.Emulators.TgbDual.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.TgbDual.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.TgbDual;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

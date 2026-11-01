using RetroMediaLab.Emulation.Nintendo.Emulators.Mesen2.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.Mesen2.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Mesen2;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

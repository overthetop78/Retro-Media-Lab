using RetroMediaLab.Emulation.Nintendo.Emulators.BeetleGba.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.BeetleGba.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.BeetleGba;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

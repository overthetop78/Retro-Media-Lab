using RetroMediaLab.Emulation.Nintendo.Emulators.Sameboy.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.Sameboy.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Sameboy;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

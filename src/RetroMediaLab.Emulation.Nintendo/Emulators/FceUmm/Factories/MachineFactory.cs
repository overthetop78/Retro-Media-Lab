using RetroMediaLab.Emulation.Nintendo.Emulators.FceUmm.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.FceUmm.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.FceUmm;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

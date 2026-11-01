using RetroMediaLab.Emulation.Nintendo.Emulators.VbaM.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.VbaM.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.VbaM;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

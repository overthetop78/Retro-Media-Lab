using RetroMediaLab.Emulation.Nintendo.Emulators.DeSmuME.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.DeSmuME.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.DeSmuME;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

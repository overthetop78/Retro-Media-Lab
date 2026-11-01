using RetroMediaLab.Emulation.Nintendo.Emulators.DeSmuME2015.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.DeSmuME2015.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.DeSmuME2015;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

using RetroMediaLab.Emulation.Nintendo.Emulators.MesenS.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.MesenS.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.MesenS;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

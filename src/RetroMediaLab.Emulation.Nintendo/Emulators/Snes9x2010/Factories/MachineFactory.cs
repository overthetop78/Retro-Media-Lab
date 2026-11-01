using RetroMediaLab.Emulation.Nintendo.Emulators.Snes9x2010.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.Snes9x2010.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Snes9x2010;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

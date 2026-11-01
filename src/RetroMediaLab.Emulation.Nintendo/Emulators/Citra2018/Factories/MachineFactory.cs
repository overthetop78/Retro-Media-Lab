using RetroMediaLab.Emulation.Nintendo.Emulators.Citra2018.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.Citra2018.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Citra2018;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

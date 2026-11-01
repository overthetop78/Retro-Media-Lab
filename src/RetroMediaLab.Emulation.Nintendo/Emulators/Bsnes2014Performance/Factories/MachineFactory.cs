using RetroMediaLab.Emulation.Nintendo.Emulators.Bsnes2014Performance.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.Bsnes2014Performance.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Bsnes2014Performance;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

using RetroMediaLab.Emulation.Nintendo.Emulators.GameWatch.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.GameWatch.Factories;

internal sealed class GameWatchMachineFactory : CoreMachineFactory, IEmulatorMachineDefaults
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.GameWatch;
    public bool IsDefaultFor(string machineId) => machineId is ModelConstants.GameWatch;

    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

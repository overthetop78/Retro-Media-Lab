using RetroMediaLab.Emulation.Nintendo.Emulators.Mgba.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.Mgba.Factories;

internal sealed class MgbaMachineFactory : CoreMachineFactory, IEmulatorMachineDefaults
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Mgba;
    public bool IsDefaultFor(string machineId) => machineId is ModelConstants.GameBoyAdvance;

    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

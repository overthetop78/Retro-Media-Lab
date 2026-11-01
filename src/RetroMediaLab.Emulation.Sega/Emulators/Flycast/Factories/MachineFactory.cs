using RetroMediaLab.Emulation.Sega.Emulators.Flycast.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Sega.Emulators.Flycast.Factories;

internal sealed class MachineFactory : CoreMachineFactory, IEmulatorMachineDefaults
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Flycast;
    public bool IsDefaultFor(string machineId) => machineId is ModelConstants.Dreamcast or ModelConstants.Naomi or ModelConstants.Naomi2 or ModelConstants.Atomiswave or ModelConstants.SystemSp;

    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

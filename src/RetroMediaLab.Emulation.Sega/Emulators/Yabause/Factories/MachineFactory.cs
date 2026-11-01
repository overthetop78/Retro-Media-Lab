using RetroMediaLab.Emulation.Sega.Emulators.Yabause.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Sega.Emulators.Yabause.Factories;

internal sealed class MachineFactory : CoreMachineFactory, IEmulatorMachineDefaults
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Yabause;
    public bool IsDefaultFor(string machineId) => machineId is ModelConstants.Saturn;

    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

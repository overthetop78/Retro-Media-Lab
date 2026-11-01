using RetroMediaLab.Emulation.Nintendo.Emulators.Cemu.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.Cemu.Factories;

internal sealed class MachineFactory : CoreMachineFactory, IEmulatorMachineDefaults
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Cemu;
    public bool IsDefaultFor(string machineId) => machineId is ModelConstants.WiiU;

    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

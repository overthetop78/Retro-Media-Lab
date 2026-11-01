using RetroMediaLab.Emulation.Sega.Emulators.BlueMSX.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Sega.Emulators.BlueMSX.Factories;

internal sealed class MachineFactory : CoreMachineFactory, IEmulatorMachineDefaults
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.BlueMSX;
    public bool IsDefaultFor(string machineId) => machineId is ModelConstants.Sf7000;

    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

using RetroMediaLab.Emulation.Sega.Emulators.Supermodel.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Sega.Emulators.Supermodel.Factories;

internal sealed class MachineFactory : CoreMachineFactory, IEmulatorMachineDefaults
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Supermodel;
    public bool IsDefaultFor(string machineId) => machineId is ModelConstants.Model3;

    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGX.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGX.Factories;

internal sealed class MachineFactory : CoreMachineFactory, IEmulatorMachineDefaults
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.GenesisPlusGX;
    public bool IsDefaultFor(string machineId) => machineId is ModelConstants.Sg1000 or ModelConstants.Sc3000 or ModelConstants.MarkIII or ModelConstants.MasterSystem or ModelConstants.MegaDrive or ModelConstants.Pico or ModelConstants.GameGear or ModelConstants.MegaCd;

    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

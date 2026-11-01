using RetroMediaLab.Emulation.Nintendo.Emulators.MelonDsDs.Constants;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nintendo.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nintendo.Emulators.MelonDsDs.Factories;

internal sealed class MachineFactory : CoreMachineFactory, IEmulatorMachineDefaults
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.MelonDsDs;
    public bool IsDefaultFor(string machineId) => machineId is ModelConstants.NintendoDsi;

    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
}

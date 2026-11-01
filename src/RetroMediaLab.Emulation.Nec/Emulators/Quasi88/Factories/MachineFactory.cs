using RetroMediaLab.Emulation.Nec.Emulators.Quasi88.Constants;
using RetroMediaLab.Emulation.Nec.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nec.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nec.Emulators.Quasi88.Factories;

internal sealed class MachineFactory : CoreMachineFactory, IEmulatorMachineDefaults
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Quasi88;
    public bool IsDefaultFor(string machineId) => Definition.MachineIds.Contains(machineId);

    /// <inheritdoc/>
    public override IReadOnlyList<FirmwareSlot> GetFirmwareSlots(MachineConfiguration configuration) => RetroMediaLab.Emulation.Nec.Emulators.Quasi88.Constants.FirmwareConstants.Slots;
    protected override CoreReleaseSettings ReleaseSettings => CoreConstants.ReleaseSettings;
    public override string EmulatorId => CoreConstants.Id;
    public override EmulationEmulatorDefinition Definition { get; } = new(
        CoreConstants.Id, CoreConstants.DisplayName, CoreConstants.DescriptionResourceKey,
        new[]
        {
            RetroMediaLab.Emulation.Nec.Common.Machines.Pc8001.Constants.MachineConstants.Id,
            RetroMediaLab.Emulation.Nec.Common.Machines.Pc8801.Constants.MachineConstants.Id,
            RetroMediaLab.Emulation.Nec.Common.Machines.Pc8801.Constants.MachineConstants.MkIIId,
            RetroMediaLab.Emulation.Nec.Common.Machines.Pc8801.Constants.MachineConstants.MkIISrId,
        }.ToHashSet(StringComparer.Ordinal));
}

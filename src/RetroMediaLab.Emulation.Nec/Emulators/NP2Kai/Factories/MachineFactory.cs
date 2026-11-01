using RetroMediaLab.Emulation.Nec.Emulators.NP2Kai.Constants;
using RetroMediaLab.Emulation.Nec.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nec.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nec.Emulators.NP2Kai.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.NP2Kai;
    /// <inheritdoc/>
    public override IReadOnlyList<FirmwareSlot> GetFirmwareSlots(MachineConfiguration configuration) => RetroMediaLab.Emulation.Nec.Emulators.NP2Kai.Constants.FirmwareConstants.Slots;
    protected override CoreReleaseSettings ReleaseSettings => CoreConstants.ReleaseSettings;
    public override string EmulatorId => CoreConstants.Id;
    public override EmulationEmulatorDefinition Definition { get; } = new(
        CoreConstants.Id, CoreConstants.DisplayName, CoreConstants.DescriptionResourceKey,
        new[]
        {
            RetroMediaLab.Emulation.Nec.Common.Machines.Pc9801.Constants.MachineConstants.Id,
            RetroMediaLab.Emulation.Nec.Common.Machines.Pc9821.Constants.MachineConstants.Id,
        }.ToHashSet(StringComparer.Ordinal));
}

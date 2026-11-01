using RetroMediaLab.Emulation.Nec.Emulators.BeetlePce.Constants;
using RetroMediaLab.Emulation.Nec.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nec.Emulators.Common.Interop.Factories;

namespace RetroMediaLab.Emulation.Nec.Emulators.BeetlePce.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.BeetlePce;
    protected override CoreReleaseSettings ReleaseSettings => CoreConstants.ReleaseSettings;
    public override string EmulatorId => CoreConstants.Id;
    public override EmulationEmulatorDefinition Definition { get; } = new(
        CoreConstants.Id, CoreConstants.DisplayName, CoreConstants.DescriptionResourceKey,
        new[]
        {
            RetroMediaLab.Emulation.Nec.Common.Machines.PcEngine.Constants.PcEngineMachineConstants.Id,
            RetroMediaLab.Emulation.Nec.Common.Machines.CoreGrafx.Constants.CoreGrafxMachineConstants.Id,
            RetroMediaLab.Emulation.Nec.Common.Machines.PcEngineDuo.Constants.PcEngineDuoMachineConstants.Id,
            RetroMediaLab.Emulation.Nec.Common.Machines.PcEngineLt.Constants.PcEngineLtMachineConstants.Id,
            RetroMediaLab.Emulation.Nec.Common.Machines.TurboExpress.Constants.TurboExpressMachineConstants.Id,
            RetroMediaLab.Emulation.Nec.Common.Machines.SuperGrafx.Constants.SuperGrafxMachineConstants.Id,
            RetroMediaLab.Emulation.Nec.Common.Machines.PcEngineCd.Constants.MachineConstants.Id,
            RetroMediaLab.Emulation.Nec.Common.Machines.PcEngineSuperCd.Constants.MachineConstants.Id,
            RetroMediaLab.Emulation.Nec.Common.Machines.PcEngineArcadeCard.Constants.MachineConstants.Id,
        }.ToHashSet(StringComparer.Ordinal));
}

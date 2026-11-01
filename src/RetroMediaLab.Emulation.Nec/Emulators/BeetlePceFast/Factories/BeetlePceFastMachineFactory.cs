using RetroMediaLab.Emulation.Nec.Emulators.Common.Interop.Factories;
using RetroMediaLab.Emulation.Nec.Emulators.BeetlePceFast.Functions;
using RetroMediaLab.Emulation.Nec.Emulators.Common.Interop.Functions;
using RetroMediaLab.Emulation.Nec.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nec.Emulators.BeetlePceFast.Constants;
using RetroMediaLab.Emulation.Nec.Emulators.Common.Interop.Constants;
using RetroMediaLab.Emulation.Nec.Emulators.Common.Interop.Services;
using System.IO;
using RetroMediaLab.Emulation.Nec.Common.Machines.PcEngine.Constants;
using RetroMediaLab.Emulation.Nec.Common.Machines.CoreGrafx.Constants;
using RetroMediaLab.Emulation.Nec.Common.Machines.PcEngineDuo.Constants;
using RetroMediaLab.Emulation.Nec.Common.Machines.PcEngineLt.Constants;
using RetroMediaLab.Emulation.Nec.Common.Machines.TurboExpress.Constants;

namespace RetroMediaLab.Emulation.Nec.Emulators.BeetlePceFast.Factories;

internal sealed class BeetlePceFastMachineFactory : CoreMachineFactory, IEmulatorMachineDefaults
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.BeetlePceFast;
    public bool IsDefaultFor(string machineId) => Definition.MachineIds.Contains(machineId);

    public override EmulationEmulatorDefinition Definition { get; } = new(
        BeetlePceFastConstants.Id, BeetlePceFastConstants.DisplayName,
        BeetlePceFastConstants.DescriptionResourceKey,
        new[] { PcEngineMachineConstants.Id, CoreGrafxMachineConstants.Id,
            PcEngineDuoMachineConstants.Id, PcEngineLtMachineConstants.Id,
            TurboExpressMachineConstants.Id,
            RetroMediaLab.Emulation.Nec.Common.Machines.PcEngineCd.Constants.MachineConstants.Id,
            RetroMediaLab.Emulation.Nec.Common.Machines.PcEngineSuperCd.Constants.MachineConstants.Id ,
            RetroMediaLab.Emulation.Nec.Common.Machines.PcEngineArcadeCard.Constants.MachineConstants.Id }
            .ToHashSet(StringComparer.Ordinal));

    public override string EmulatorId => BeetlePceFastConstants.Id;
    protected override CoreReleaseSettings ReleaseSettings => ReleaseConstants.Settings;
    protected override MachineConfiguration Configure(MachineConfiguration configuration) => BeetlePceFastOptionFunctions.ToNative(configuration);

    public override ValueTask<string?> FindInstalledCorePathAsync(EmulatorManagementContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context.HttpClient);
        return base.FindInstalledCorePathAsync(context, cancellationToken);
    }
}

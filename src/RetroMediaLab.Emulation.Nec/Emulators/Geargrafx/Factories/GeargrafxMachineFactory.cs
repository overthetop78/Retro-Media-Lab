using RetroMediaLab.Emulation.Nec.Emulators.Common.Interop.Factories;
using System.IO;
using RetroMediaLab.Emulation.Nec.Common.Machines.CoreGrafx.Constants;
using RetroMediaLab.Emulation.Nec.Common.Machines.LaserActive.Constants;
using RetroMediaLab.Emulation.Nec.Common.Machines.PcEngine.Constants;
using RetroMediaLab.Emulation.Nec.Common.Machines.PcEngineDuo.Constants;
using RetroMediaLab.Emulation.Nec.Common.Machines.PcEngineLt.Constants;
using RetroMediaLab.Emulation.Nec.Common.Machines.SuperGrafx.Constants;
using RetroMediaLab.Emulation.Nec.Common.Machines.TurboExpress.Constants;
using RetroMediaLab.Emulation.Nec.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Nec.Emulators.BeetlePceFast.Functions;
using RetroMediaLab.Emulation.Nec.Emulators.Common.Interop.Functions;
using RetroMediaLab.Emulation.Nec.Emulators.Common.Interop.Services;
using RetroMediaLab.Emulation.Nec.Emulators.Geargrafx.Constants;
using RetroMediaLab.Emulation.Nec.Emulators.Geargrafx.Functions;

namespace RetroMediaLab.Emulation.Nec.Emulators.Geargrafx.Factories;

internal sealed class GeargrafxMachineFactory : CoreMachineFactory, IEmulatorMachineDefaults, IEmulatorFirmwareAdapter
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.Geargrafx;
    public EmulationFirmwareObservation<TDefinition>? IdentifyFirmware<TDefinition>(EmulationFirmwareObservation<TDefinition> firmware) where TDefinition : class => GeargrafxFirmwareFunctions.Identify(firmware);

    public bool IsDefaultFor(string machineId) => machineId == LaserActiveMachineConstants.Id;

    public override EmulationEmulatorDefinition Definition { get; } = new(
        GeargrafxConstants.Id, GeargrafxConstants.DisplayName,
        GeargrafxConstants.DescriptionResourceKey,
        new[] { PcEngineMachineConstants.Id, CoreGrafxMachineConstants.Id,
            PcEngineDuoMachineConstants.Id, PcEngineLtMachineConstants.Id,
            TurboExpressMachineConstants.Id,
            RetroMediaLab.Emulation.Nec.Common.Machines.PcEngineCd.Constants.MachineConstants.Id,
            RetroMediaLab.Emulation.Nec.Common.Machines.PcEngineSuperCd.Constants.MachineConstants.Id, SuperGrafxMachineConstants.Id,
            LaserActiveMachineConstants.Id }.ToHashSet(StringComparer.Ordinal));

    public override string EmulatorId => GeargrafxConstants.Id;
    protected override CoreReleaseSettings ReleaseSettings => GeargrafxConstants.ReleaseSettings;
    protected override MachineConfiguration Configure(MachineConfiguration configuration) => BeetlePceFastOptionFunctions.ToNative(configuration);

    protected override void PrepareSession(MachineConfiguration configuration, EmulatorCreationContext context, string sessionDirectory) =>
        GeargrafxFirmwareFunctions.Prepare(context.FirmwareDirectory!, sessionDirectory, configuration.FirmwarePath);
}

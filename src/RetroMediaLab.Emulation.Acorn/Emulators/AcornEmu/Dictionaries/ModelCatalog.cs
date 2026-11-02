using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMaster.Common.Dictionaries;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Dictionaries;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Dictionaries;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornElectron.Common.Dictionaries;
using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Constants;
using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Functions;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Contracts;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;
using VideoConstants = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.VideoConstants;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Dictionaries;

/// <summary>Catalogue des implémentations matérielles disponibles dans l'émulateur multimodèle AcornEmu.</summary>
/// <remarks>Les profils A et B restent distincts et assemblent les composants de la carte partagée.
/// La fabrique et le cœur consultent ce catalogue : ils ne transforment pas automatiquement un autre modèle en Model A.</remarks>
internal static class ModelCatalog
{
    internal static IReadOnlyList<AcornEmuModelDefinition> All { get; } =
    [
        new(MachineModel.BbcMicroModelA, (double)BbcMicroModelConstants.CpuFrequencyHz / BbcMicroModelConstants.CyclesPerFrame,
            VideoConstants.Width, VideoConstants.Height, 4, EmulationPixelFormat.Xrgb8888, 4f / 3, EmulationVideoStandard.Pal,
            CreateModelA),
        new(MachineModel.BbcMicroModelB, (double)BbcMicroModelConstants.CpuFrequencyHz / BbcMicroModelConstants.CyclesPerFrame,
            VideoConstants.Width, VideoConstants.Height, 4, EmulationPixelFormat.Xrgb8888, 4f / 3, EmulationVideoStandard.Pal,
            CreateModelB),
        new(MachineModel.BbcMicroModelBPlus, (double)BbcMicroModelConstants.CpuFrequencyHz / BbcMicroModelConstants.CyclesPerFrame,
            VideoConstants.Width, VideoConstants.Height, 4, EmulationPixelFormat.Xrgb8888, 4f / 3, EmulationVideoStandard.Pal,
            CreateModelB),
        .. AcornEmuMasterModelCatalog.All,
        .. AcornEmuSystemModelCatalog.All,
        .. AcornEmuAtomModelCatalog.All,
        .. AcornEmuElectronModelCatalog.All
    ];

    private static BbcMicroMachine CreateModelA(MachineConfiguration configuration) => CreateBoard(configuration, expandedIo: false);
    private static BbcMicroMachine CreateModelB(MachineConfiguration configuration) => CreateBoard(configuration, expandedIo: true);

    private static BbcMicroMachine CreateBoard(MachineConfiguration configuration, bool expandedIo)
    {
        var network = NetworkFunctions.Read(configuration);
        var chips = SidewaysExpansionFunctions.ChipBanks(configuration);
        var basic = !ModelBPlusFunctions.IsModel(configuration) && chips.TryGetValue(SidewaysExpansionFunctions.NativeBank(configuration, Machines.BbcMicro.Common.Constants.DiskConstants.BasicBank), out var programmedBasic) ? programmedBasic : FirmwareFunctions.Read(configuration, EmulatorConstants.BasicFirmware);
        var mos = FirmwareFunctions.Read(configuration, EmulatorConstants.SystemFirmware);
        MemoryExtensionFunctions.ValidateMos(configuration, mos);
        return new BbcMicroMachine(mos, basic, BbcMicroMemoryFunctions.MainBytes(configuration), FirmwareFunctions.Read(configuration, EmulatorConstants.CharacterFirmware), StorageFunctions.HasInterface(configuration), FirmwareFunctions.OptionRoms(configuration, chips), StorageFunctions.UsesWd1770(configuration), StorageFunctions.HasWinchester(configuration), network.Enabled, network.Station, network.CyclesPerByte, MemoryExtensionFunctions.Read(configuration), SidewaysExpansionFunctions.Read(configuration), expandedIo: expandedIo, videoStandard: VideoStandardFunctions.Resolve(configuration), modelBPlusMemory: ModelBPlusFunctions.IsModel(configuration) ? ModelBPlusFunctions.MemoryBytes(configuration) : 0)
        {
            Video = { Monochrome = configuration.Options.GetValueOrDefault(EmulatorConstants.VideoOutput, EmulatorConstants.UhfOutput) == EmulatorConstants.MonochromeOutput }
        };
    }

    internal static AcornEmuModelDefinition Get(MachineConfiguration configuration)
    {
        if (SystemFunctions.IsModel(configuration) || AtomFunctions.IsModel(configuration) || ElectronFunctions.IsModel(configuration)) return Get(configuration.Model);
        var standard = VideoStandardFunctions.Resolve(configuration);
        var ntsc = standard == EmulationVideoStandard.Ntsc;
        return Get(configuration.Model) with
        {
            Region = standard,
            Height = ntsc ? VideoConstants.NtscHeight : VideoConstants.Height,
            FramesPerSecond = (double)BbcMicroModelConstants.CpuFrequencyHz / (ntsc ? BbcMicroModelConstants.NtscCyclesPerFrame : BbcMicroModelConstants.CyclesPerFrame)
        };
    }

    internal static AcornEmuModelDefinition Get(MachineModel model) => All.SingleOrDefault(item => item.Model == model) ?? throw new NotSupportedException(model.ToString());
}

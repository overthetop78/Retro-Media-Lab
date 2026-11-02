using C = RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMaster.Constants.MasterModelConstants;

namespace RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMaster.Dictionaries;

public static class MasterModelCatalog
{
    public static IReadOnlyList<HardwareModelDefinition> All { get; } =
    [
        Create(MachineModel.BbcMaster128, C.Master128Label, Emulator.B2,
            [HardwareStorageCapability.Floppy, HardwareStorageCapability.Cassette, HardwareStorageCapability.Cartridge],
            [MediaCategory.Floppy, MediaCategory.Cassette, MediaCategory.Cartridge]),
        Create(MachineModel.BbcMasterET, C.MasterEtLabel, Emulator.AcornEmu, [], []),
        Create(MachineModel.BbcMasterCompact, C.MasterCompactLabel, Emulator.AcornEmu, [HardwareStorageCapability.Floppy], [MediaCategory.Floppy])
    ];
    private static HardwareModelDefinition Create(MachineModel model, string label, Emulator emulator,
        IReadOnlyList<HardwareStorageCapability> storage, IReadOnlyList<MediaCategory> media) =>
        HardwareModelFunctions.Create(model, model.ToString(), label, emulator, C.CpuFrequency, C.MemoryBytes,
            [HardwareCpu.Rockwell65C12], [HardwareRegion.Pal],
            [HardwareVideoCapability.Mc6845, HardwareVideoCapability.VideoUla, HardwareVideoCapability.Saa5050],
            [HardwareAudioCapability.Sn76489], storage,
            model == MachineModel.BbcMasterET ? [new(HardwarePortCapability.Keyboard, C.KeyboardPorts)]
                : [new(HardwarePortCapability.Keyboard, C.KeyboardPorts), new(model == MachineModel.BbcMasterCompact ? HardwarePortCapability.DigitalJoystick : HardwarePortCapability.AnalogJoystick, model == MachineModel.BbcMasterCompact ? C.KeyboardPorts : C.ControllerPorts)],
            [FirmwareCategory.MasterMos, FirmwareCategory.Basic, FirmwareCategory.DiscFilingSystem], media);
}

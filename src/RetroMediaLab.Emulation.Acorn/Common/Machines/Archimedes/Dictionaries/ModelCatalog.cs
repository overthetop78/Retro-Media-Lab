using C = RetroMediaLab.Emulation.Acorn.Common.Machines.Archimedes.Constants.ArchimedesModelConstants;

namespace RetroMediaLab.Emulation.Acorn.Common.Machines.Archimedes.Dictionaries;

/// <summary>Assemblage configurable des premières cartes ARM2/ARM3 à IOC, MEMC et VIDC10.</summary>
public static class ArchimedesModelCatalog
{
    public static IReadOnlyList<HardwareModelDefinition> All { get; } =
    [
        Create(MachineModel.AcornArchimedes, C.ModelResource, hardDisk: true),
        Create(MachineModel.AcornA3000, C.A3000ModelResource, hardDisk: false)
    ];

    private static HardwareModelDefinition Create(MachineModel model, string resource, bool hardDisk) =>
        HardwareModelFunctions.Create(model, model.ToString(),
            resource, Emulator.AcornEmu, C.Arm2ClockHz, C.DefaultMemoryKib * C.Kibibyte,
            [HardwareCpu.Arm2, HardwareCpu.Arm3], [HardwareRegion.Pal], [HardwareVideoCapability.Vidc10],
            [HardwareAudioCapability.VidcStereo], hardDisk
                ? [HardwareStorageCapability.Floppy, HardwareStorageCapability.HardDisk] : [HardwareStorageCapability.Floppy],
            [new(HardwarePortCapability.Keyboard, C.OnePort), new(HardwarePortCapability.Mouse, C.OnePort)],
            [FirmwareCategory.OperatingSystem], hardDisk ? [MediaCategory.Floppy, MediaCategory.HardDisk] : [MediaCategory.Floppy]);
}

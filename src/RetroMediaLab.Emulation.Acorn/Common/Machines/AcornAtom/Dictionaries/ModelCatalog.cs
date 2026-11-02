using C = RetroMediaLab.Emulation.Acorn.Common.Machines.AcornAtom.Constants.AtomModelConstants;

namespace RetroMediaLab.Emulation.Acorn.Common.Machines.AcornAtom.Dictionaries;

public static class AtomModelCatalog
{
    public static IReadOnlyList<HardwareModelDefinition> All { get; } =
    [
        HardwareModelFunctions.Create(MachineModel.AcornAtom, nameof(MachineModel.AcornAtom),
            C.ResourcePrefix + nameof(MachineModel.AcornAtom), Emulator.AcornEmu, C.CpuClock, C.DefaultMemoryBytes,
            [HardwareCpu.Mos6502], [HardwareRegion.Ntsc], [HardwareVideoCapability.Mc6847],
            [HardwareAudioCapability.Buzzer], [HardwareStorageCapability.Cassette, HardwareStorageCapability.Floppy],
            [new(HardwarePortCapability.Keyboard, C.KeyboardPorts)],
            [FirmwareCategory.OperatingSystem, FirmwareCategory.Basic, FirmwareCategory.Application,
                FirmwareCategory.CharacterGenerator, FirmwareCategory.DiscFilingSystem],
            [MediaCategory.Cassette, MediaCategory.Floppy])
    ];
}

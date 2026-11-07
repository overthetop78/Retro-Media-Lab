using C = RetroMediaLab.Emulation.Acorn.Common.Machines.AcornSystem.Constants.SystemModelConstants;

namespace RetroMediaLab.Emulation.Acorn.Common.Machines.AcornSystem.Dictionaries;

public static class SystemModelCatalog
{
    public static IReadOnlyList<HardwareModelDefinition> All { get; } =
    [
        Create(MachineModel.AcornSystem1, C.SystemOneClock, C.CpuRamBytes,
            [HardwareCpu.Mos6502], [HardwareVideoCapability.SevenSegmentDisplay], [HardwareStorageCapability.Tape]),
        Create(MachineModel.AcornSystem2, C.StandardClock, C.SystemTwoRamBytes,
            [HardwareCpu.Mos6502], [HardwareVideoCapability.Mc6845, HardwareVideoCapability.Saa5050], [HardwareStorageCapability.Tape]),
        Create(MachineModel.AcornSystem3, C.StandardClock, C.SystemThreeRamBytes,
            [HardwareCpu.Mos6502, HardwareCpu.Motorola6809], [HardwareVideoCapability.Mc6845, HardwareVideoCapability.Saa5050], [HardwareStorageCapability.Floppy]),
        Create(MachineModel.AcornSystem4, C.StandardClock, C.SystemThreeRamBytes,
            [HardwareCpu.Mos6502], [HardwareVideoCapability.Mc6845, HardwareVideoCapability.Saa5050], [HardwareStorageCapability.Floppy]),
        Create(MachineModel.AcornSystem5, C.FastClock, C.SystemFiveRamBytes,
            [HardwareCpu.Mos6502A], [HardwareVideoCapability.Mc6845], [HardwareStorageCapability.Floppy])
    ];

    public static bool IsSystem(MachineModel model) => model is MachineModel.AcornSystem1 or MachineModel.AcornSystem2
        or MachineModel.AcornSystem3 or MachineModel.AcornSystem4 or MachineModel.AcornSystem5;

    private static HardwareModelDefinition Create(MachineModel model, int clock, int memory,
        IReadOnlyList<HardwareCpu> cpus, IReadOnlyList<HardwareVideoCapability> video, IReadOnlyList<HardwareStorageCapability> storage) =>
        HardwareModelFunctions.Create(model, model.ToString(), C.ResourcePrefix + model, Emulator.AcornEmu, clock, memory,
            cpus, [HardwareRegion.Pal], video, [], storage, [new(HardwarePortCapability.Keyboard, C.KeyboardPorts)],
            model == MachineModel.AcornSystem1 ? [FirmwareCategory.OperatingSystem]
                : [FirmwareCategory.OperatingSystem, FirmwareCategory.CharacterGenerator, FirmwareCategory.Basic],
            storage.Select(item => item == HardwareStorageCapability.Tape ? MediaCategory.Tape : MediaCategory.Floppy).ToArray());
}

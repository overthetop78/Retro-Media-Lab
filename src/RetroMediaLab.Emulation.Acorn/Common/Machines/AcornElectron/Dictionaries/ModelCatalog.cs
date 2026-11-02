using C = RetroMediaLab.Emulation.Acorn.Common.Machines.AcornElectron.Constants.ElectronModelConstants;

namespace RetroMediaLab.Emulation.Acorn.Common.Machines.AcornElectron.Dictionaries;

public static class ElectronModelCatalog
{
    public static IReadOnlyList<HardwareModelDefinition> All { get; } =
    [
        HardwareModelFunctions.Create(MachineModel.AcornElectron, nameof(MachineModel.AcornElectron),
            C.ResourcePrefix + nameof(MachineModel.AcornElectron), Emulator.AcornEmu, C.CpuClock, C.MemoryBytes,
            [HardwareCpu.Mos6502A], [HardwareRegion.Pal], [HardwareVideoCapability.VideoUla],
            [HardwareAudioCapability.Buzzer], [HardwareStorageCapability.Cassette],
            [new(HardwarePortCapability.Keyboard, C.KeyboardPorts)],
            [FirmwareCategory.OperatingSystem, FirmwareCategory.Basic], [MediaCategory.Cassette])
    ];
}

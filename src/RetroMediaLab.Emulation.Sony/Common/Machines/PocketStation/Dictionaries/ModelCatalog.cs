using Hardware = RetroMediaLab.Emulation.Sony.Common.Machines.PocketStation.Constants.ModelConstants;

namespace RetroMediaLab.Emulation.Sony.Common.Machines.PocketStation.Dictionaries;

internal static class ModelCatalog
{
    internal static Model PocketStation { get; } = new(
        Hardware.Id, Hardware.DisplayName, Hardware.BackendModel, (int)RamCapacity._2KB,
        HasKeyboard: false,
        BuiltInFloppyDriveCount: Hardware.NoFloppyDrive,
        MaximumFloppyDriveCount: Hardware.NoFloppyDrive,
        HasBuiltInTapeDrive: false, SupportsTapeDrive: false,
        HasBuiltInCartridgeSlot: false, SupportsCartridgeSlot: false,
        ControllerPortCount: Hardware.IntegratedControllerCount,
        CpuModels: [Hardware.Cpu], VideoChip: Hardware.Display, AudioChip: Hardware.Audio,
        RomKib: Hardware.BiosCapacityKib, CpuFrequency: Hardware.MaximumCpuFrequency,
        HasBuiltInMemoryCard: true);
}

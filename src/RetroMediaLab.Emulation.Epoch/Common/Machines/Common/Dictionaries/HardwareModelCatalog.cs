namespace RetroMediaLab.Emulation.Epoch.Common.Machines.Common.Dictionaries;

internal static class HardwareModelCatalog
{
    internal static HardwareModelDefinition Get(MachineModel model) => model switch
    {
        MachineModel.TapeVision or MachineModel.TapeVisionJr =>
            new(HardwareConstants.PdCpuResource, null, null, HardwareConstants.PdDisplayWidth, HardwareConstants.PdDisplayHeight),
        MachineModel.SuperTapeVision or MachineModel.SuperTapeVisionLady =>
            new(HardwareConstants.ScvCpuResource, HardwareConstants.ScvCpuFrequencyHz, HardwareConstants.ScvInternalMemoryBytes, HardwareConstants.ScvDisplayWidth, HardwareConstants.ScvDisplayHeight, HardwareConstants.ScvVideoMemoryBytes),
        MachineModel.SuperTapeVisionYeno =>
            new(HardwareConstants.ScvCpuResource, null, HardwareConstants.ScvInternalMemoryBytes, HardwareConstants.YenoDisplayWidth, HardwareConstants.YenoDisplayHeight, HardwareConstants.ScvVideoMemoryBytes),
        _ => throw new ArgumentOutOfRangeException(nameof(model))
    };
}

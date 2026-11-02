namespace RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMaster.Constants;

internal static class MasterModelConstants
{
    internal const string Master128Id = nameof(MachineModel.BbcMaster128);
    internal const string MasterEtId = nameof(MachineModel.BbcMasterET);
    internal const string MasterCompactId = nameof(MachineModel.BbcMasterCompact);
    internal const string ResourcePrefix = "Emulation.Acorn.Model.";
    internal const string Master128Label = ResourcePrefix + Master128Id;
    internal const string MasterEtLabel = ResourcePrefix + MasterEtId;
    internal const string MasterCompactLabel = ResourcePrefix + MasterCompactId;
    internal const int MemoryBytes = 131072;
    internal const int CpuFrequency = 2000000;
    internal const int KeyboardPorts = 1;
    internal const int ControllerPorts = 2;
    internal const int PixelBytes = 4;
    internal const float AspectRatio = 4f / 3f;
}

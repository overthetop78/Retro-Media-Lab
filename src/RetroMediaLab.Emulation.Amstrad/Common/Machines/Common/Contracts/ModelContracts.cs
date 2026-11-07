namespace RetroMediaLab.Emulation.Amstrad.Common.Machines.Common.Contracts;

public sealed record Model(
    string Id,
    string DisplayName,
    string BackendModel,
    int RamKib,
    bool HasKeyboard,
    int BuiltInFloppyDriveCount,
    int MaximumFloppyDriveCount,
    bool HasBuiltInTapeDrive,
    bool SupportsTapeDrive,
    bool HasBuiltInCartridgeSlot,
    bool SupportsCartridgeSlot,
    int ControllerPortCount = ModelConstants.DefaultControllerPortCount,
    int MouseButtonCount = ModelConstants.DefaultMouseButtonCount);

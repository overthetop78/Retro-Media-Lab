using RetroMediaLab.App.Contracts.Input;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Contracts.Machine;

internal sealed record MachineControllerOptions(
    string ModuleId,
    string MachineId,
    Guid ConfigurationId,
    IEmulatedMachine Machine,
    Func<IReadOnlyList<EmulationMedia>, IEmulatedMachine> MachineFactory,
    Func<IEmulationConfiguration, CancellationToken, ValueTask<EmulationMachineRuntime>> CreateRuntimeAsync,
    IReadOnlyList<EmulationMediaDevice> MediaDevices,
    IReadOnlyList<EmulationMedia> MountedMedia,
    EmulationVideoRenderer VideoRenderer,
    EmulationVideoProcessingConfiguration VideoProcessing,
    IReadOnlyList<GlobalShortcutBinding> GlobalShortcuts,
    string QuickStatePath,
    string CaptureFolder,
    string WindowTitle,
    Action<Exception> ShowError,
    bool SupportsPointerCapture,
    Func<EmulationMediaDevice, string?> InitialMediaDirectory,
    Action<EmulationMediaDevice, string> RememberMediaDirectory,
    Func<bool> IsActive,
    Func<EmulationMachinePresentation> CreatePresentation,
    Func<EmulationMedia, CancellationToken, ValueTask<EmulationMedia>>? PrepareMediaAsync = null,
    Func<Task>? SwitchControllerPointer = null,
    Func<IReadOnlyDictionary<string, string>, Task>? PersistRuntimeOptions = null,
    Func<IReadOnlyList<EmulationMedia>, Task>? PersistMountedMedia = null,
    Func<Task>? OpenSettings = null,
    IReadOnlyDictionary<string, string>? ShortcutSettings = null,
    IEmulationModule? Module = null,
    IEmulationConfiguration? Configuration = null,
    Func<IReadOnlyDictionary<string, string>>? ReadShortcutSettings = null,
    Func<bool>? ReadFullscreenStatistics = null,
    Action<bool>? WriteFullscreenStatistics = null,
    Func<int>? ReadOsdTransparency = null,
    Action<int>? WriteOsdTransparency = null);

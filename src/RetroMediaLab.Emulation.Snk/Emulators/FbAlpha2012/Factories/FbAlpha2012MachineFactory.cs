using RetroMediaLab.Emulation.Snk.Emulators.Common.Interop.Services;
using RetroMediaLab.Emulation.Snk.Emulators.Common.Interop.Functions;
using RetroMediaLab.Emulation.Snk.Emulators.Common.Interop.Factories;
using RetroMediaLab.Emulation.Snk.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Snk.Emulators.Common.Interop.Constants;
using System.IO;
using RetroMediaLab.Emulation.Snk.Emulators.FbAlpha2012.Constants;
using RetroMediaLab.Emulation.Snk.Emulators.FbAlpha2012.Dictionaries;
using NativeOptions = RetroMediaLab.Emulation.Snk.Emulators.FbAlpha2012.Functions.RuntimeOptionFunctions;
using NativeMedia = RetroMediaLab.Emulation.Snk.Emulators.FbAlpha2012.Functions.MediaRuntimeFunctions;
using NativeFirmware = RetroMediaLab.Emulation.Snk.Emulators.FbAlpha2012.Functions.FirmwareRuntimeFunctions;

namespace RetroMediaLab.Emulation.Snk.Emulators.FbAlpha2012.Factories;
internal sealed class FbAlpha2012MachineFactory() : LibretroMachineFactory(EmulatorConstants.Entry)
{
    public override IReadOnlyList<CoreOption> CoreOptions => OptionCatalog.Options;

    public override IReadOnlyList<CoreOption> GetSettingsOptions(MachineConfiguration? configuration = null, EmulatorManagementContext? context = null) => configuration is null ? CoreOptions : NativeOptions.Describe(configuration);
    protected override IReadOnlyList<FirmwareCategory> FirmwareRoles(MachineModel model) => NativeFirmware.Roles;
    protected override bool IsFirmwareRequired(MachineModel model, FirmwareCategory category) => category == FirmwareCategory.SystemBios;
    public override async ValueTask<IReadOnlyList<EmulationFirmwareCandidate>> ScanFirmwareFileAsync(MachineConfiguration configuration, string path, CancellationToken cancellationToken) => [await (NativeFirmware.Inspect(path, configuration.Model, cancellationToken)).ConfigureAwait(false)];
    public override void PrepareFirmware(MachineConfiguration configuration, string systemDirectory) => NativeFirmware.Prepare(configuration, systemDirectory);
    internal override LoadedContent? PrepareContent(MachineConfiguration configuration, string sessionDirectory, IReadOnlySet<string> supportedExtensions) => NativeMedia.Prepare(configuration, sessionDirectory, supportedExtensions);
    public override bool RequiresMediaRestart(MachineConfiguration configuration, EmulationMediaSlot slot, string? path) => true;
    public override IReadOnlyList<EmulationMediaDevice> DescribeMediaDevices(MachineConfiguration configuration) => NativeMedia.Devices;
    public override IReadOnlyList<string> ResolveInitialContentPaths(MachineConfiguration configuration) => NativeMedia.InitialContentPaths(configuration);
    public override IReadOnlyDictionary<string, string> GetRuntimeOptions(MachineConfiguration configuration) => NativeOptions.Prepare(configuration);
    public override bool IsDeferredOption(MachineConfiguration configuration, string key)
    {
        var path = configuration.InitialContent ?? configuration.Media.FirstOrDefault(media => media.Slot == EmulationMediaSlot.Cartridge0 && media.IsInserted)?.Path;
        var driver = path is null ? null : Path.GetFileNameWithoutExtension(path);
        return !string.IsNullOrEmpty(driver) && key.StartsWith(RuntimeOptionConstants.DipSwitchPrefix + driver + "_", StringComparison.Ordinal);
    }

    internal override bool LoadContent(MachineConfiguration configuration, ExternalCoreExports exports, LoadedContent? content)
    {
        var loaded = exports.LoadGame(content?.GameInfo ?? nint.Zero);
        if (loaded)
            ControllerPortFunctions.Configure(exports, configuration);
        return loaded;
    }

    internal override uint ControllerDeviceId(MachineConfiguration configuration, int port, PeripheralCategory peripheral) => peripheral == PeripheralCategory.None ? InputConstants.NoDevice : RuntimeOptionConstants.ClassicControllerDevice;
}
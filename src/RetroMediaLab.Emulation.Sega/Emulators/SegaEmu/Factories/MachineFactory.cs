using System.IO;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Factories;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.Sf7000;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Services;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Factories;

internal sealed class MachineFactory : CoreMachineFactory, IEmulatorCapabilities, IEmulatorMachineDefaults
{
    bool IEmulatorCapabilities.SupportsNoContent => true;
    public override Emulator Emulator => Emulator.SegaEmu;
    public bool IsDefaultFor(string machineId) => machineId is ModelConstants.MasterSystemII or ModelConstants.MegaDriveII or ModelConstants.MegaCdSecond;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
    public override IReadOnlyList<EmulationSettingsBlock> DescribeSettings(MachineConfiguration configuration, EmulatorManagementContext? context = null) =>
        base.DescribeSettings(configuration, context).Select(block => block with
        {
            Fields = block.Fields.Select(field => field.Id == PointerConstants.PortKey(0) || field.Id == PointerConstants.PortKey(1)
                ? field with { ControllerPortNumber = field.Id == PointerConstants.PortKey(0) ? 1 : 2,
                    AppliesToControllerIds = PointerConstants.ControllerIds, AutomaticValue = PointerConstants.Automatic }
                : field).ToArray()
        }).Concat(configuration.Model == ModelConstants.Sf7000 ? [DescribeSf7000Printer(configuration)] : []).ToArray();

    private static EmulationSettingsBlock DescribeSf7000Printer(MachineConfiguration configuration)
    {
        const string block = "sf7000.centronics";
        const EmulationMachineTab tab = EmulationMachineTab.General;
        return new(block, tab, "Emulation.Sega.Sf7000.Centronics",
        [
            new(Sf7000PrintFile.DirectoryOption, tab, block, "Read.Folder", EmulationSettingsEditor.DirectoryPath,
                configuration.Options?.GetValueOrDefault(Sf7000PrintFile.DirectoryOption) ?? Sf7000PrintFile.DefaultDirectory)
        ]);
    }
    public override bool TryHandleHostCommand(IReadOnlyList<string> arguments, out int exitCode) { exitCode = 0; return false; }
    public override ValueTask<EmulationEmulatorInstallation> GetInstallationAsync(EmulatorManagementContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new EmulationEmulatorInstallation(Definition, CoreConstants.Version) { CanSearchReleases = false });
    }
    public override ValueTask<IReadOnlyList<EmulationEmulatorRelease>> FindReleasesAsync(EmulatorManagementContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<IReadOnlyList<EmulationEmulatorRelease>>([]);
    }
    public override ValueTask<string> InstallAsync(EmulatorManagementContext context, EmulationEmulatorRelease release, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        progress?.Report(1);
        return ValueTask.FromResult(typeof(MachineFactory).Assembly.Location);
    }
    public override ValueTask<string?> FindInstalledCorePathAsync(EmulatorManagementContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<string?>(typeof(MachineFactory).Assembly.Location);
    }
    public override Machine Create(MachineConfiguration configuration, EmulatorCreationContext context)
    {
        MachineConfiguration configured = Configure(configuration.EnsureId());
        Guid id = Guid.NewGuid();
        string session = Path.Combine(context.SessionsDirectory, id.ToString(ConfigurationStoreConstants.MachineIdentifierFormat));
        string? saveDirectory = context.SaveDirectoryResolver?.Invoke(configured);
        ManagedCore? core = null;
        bool transferred = false;
        try
        {
            core = new();
            Machine machine = new(id, configured, core, configured.Media ?? [], session, saveDirectory: saveDirectory,
                inputMapper: this, audioOutputFactory: context.AudioOutputFactory, runtimePolicy: this);
            transferred = true;
            return machine;
        }
        finally { if (!transferred) core?.Dispose(); }
    }
    public override bool SupportsPointer(MachineConfiguration configuration) => true;
}

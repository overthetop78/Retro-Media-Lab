using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Common;
using RetroMediaLab.Chips.Common.Memory;
using System.IO;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Factories;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Services;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Factories;

internal sealed class MachineFactory : CoreMachineFactory
{
    public override Emulator Emulator => Emulator.SegaEmu;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;
    public override bool TryHandleHostCommand(IReadOnlyList<string> arguments, out int exitCode) { exitCode = 0; return false; }
    public override ValueTask<EmulationEmulatorInstallation> GetInstallationAsync(EmulatorManagementContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new EmulationEmulatorInstallation(Definition, CoreConstants.Version));
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

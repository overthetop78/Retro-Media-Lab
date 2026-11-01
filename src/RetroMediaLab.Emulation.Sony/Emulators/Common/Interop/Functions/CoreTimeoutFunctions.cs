using RetroMediaLab.Emulation.Sony.Emulators.Common.Interop.Constants;
using RetroMediaLab.Emulation.Sony.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Sony.Emulators.Common.Interop.Enums;

namespace RetroMediaLab.Emulation.Sony.Emulators.Common.Interop.Functions;

internal static class CoreTimeoutFunctions
{
    internal static TimeSpan For(CoreDefinition? definition, HostCommand command)
    {
        var fallback = TimeSpan.FromSeconds(CoreHostConstants.ResponseTimeoutSeconds);
        return command switch
        {
            HostCommand.Initialize or HostCommand.HardReset => definition?.StartupTimeout ?? fallback,
            HostCommand.Stop => definition?.ShutdownTimeout ?? fallback,
            HostCommand.Dispose => definition?.DisposalTimeout ?? definition?.ShutdownTimeout ?? fallback,
            _ => fallback
        };
    }
}

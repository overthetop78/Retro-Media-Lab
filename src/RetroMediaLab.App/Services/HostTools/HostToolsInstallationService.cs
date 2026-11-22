using RetroMediaLab.App.Services.Storage;
using RetroMediaLab.Infrastructure.HostTools;
using System.IO;
using System.Net.Http;

namespace RetroMediaLab.App.Services.HostTools;

internal static class HostToolsInstallationService
{
    private static readonly HttpClient Client = new();

    internal static IGwInstallationManager Create(string? dataDirectory = null) =>
        new GwInstallationManager(Client, dataDirectory is null
            ? StoragePaths.HostToolsDirectory
            : Path.Combine(dataDirectory, "Greaseweazle"));
}

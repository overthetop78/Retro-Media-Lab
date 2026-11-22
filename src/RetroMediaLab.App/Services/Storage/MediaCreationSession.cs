using System.IO;
using RetroMediaLab.App.Constants.Emulation;
using RetroMediaLab.App.Localization.Extensions;

namespace RetroMediaLab.App.Services.Storage;

internal sealed class MediaCreationSession(Func<string> destinationPath, Func<Task> create)
{
    internal string? CreatedPath { get; private set; }

    internal async Task<string> CreateAsync()
    {
        if (CreatedPath is { } existing) return existing;
        var path = destinationPath();
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException(LocExtension.Get(MediaCreationConstants.PathRequired));
        if (File.Exists(path))
            throw new IOException(LocExtension.Get(MediaCreationConstants.AlreadyExists, path));

        await create();
        CreatedPath = path;
        return path;
    }
}

using System.IO;
using RetroMediaLab.App.Services.Storage;
using RetroMediaLab.VideoPresentation.Constants;
using RetroMediaLab.VideoPresentation.Services;

namespace RetroMediaLab.App.Services.Emulation;

internal static class EmulationVideoPresentationProfiles
{
    internal static VideoPresentationProfileStore Store { get; } = new(Path.Combine(StoragePaths.EmulationDirectory, VideoPresentationStorageConstants.DirectoryName));
}
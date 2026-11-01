using MediaVolumeDescriptor = RetroMediaLab.MediaFileSystems.Contracts.MediaVolumeDescriptor;
using IMediaImageDocument = RetroMediaLab.MediaFileSystems.Interfaces.IMediaImageDocument;
using RetroMediaLab.MediaFileSystems;

namespace RetroMediaLab.MediaFileSystems.Interfaces.Exploration;

/// <summary>Probes and reads a file system from an addressable volume without assuming a physical media family.</summary>
public interface IMediaFileSystemReader
{
    string Id { get; }

    bool CanRead(IMediaImageDocument document, MediaVolumeDescriptor volume);

    FileSystemVolume Read(IMediaImageDocument document, MediaVolumeDescriptor volume);
}

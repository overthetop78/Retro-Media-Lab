using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Formats.Optical.BinCue;
using RetroMediaLab.MediaEngine.Images.Reading.Sources;
using RetroMediaLab.MediaEngine.Interfaces;

namespace RetroMediaLab.MediaEngine.Services;

/// <summary>Adapte les lecteurs CUE et convertisseurs SCP existants aux contrats de préparation.</summary>
internal sealed class MediaRuntimePreparationService : IMediaRuntimePreparation
{
    public event Action<IMediaDriveMechanics>? AudioAvailable;

    public IMediaDriveMechanics CreateDriveMechanics(string device, bool hostAudio = false)
    {
        var mechanics = new MediaDriveMechanics(device);
        if (hostAudio) mechanics.AudioAvailable += PublishAudio;
        return mechanics;
    }

    private void PublishAudio(IMediaDriveMechanics mechanics) => AudioAvailable?.Invoke(mechanics);

    public async Task<IMediaRuntimeDisk> OpenDiskAsync(string path, bool writable = false, string? formatId = null, CancellationToken cancellationToken = default, IMediaDriveMechanics? mechanics = null)
    {
        var engine = Conversion.Value;
        var document = await engine.ReadingService.ReadAsync(new(path, [], RequestedFormatId: formatId) { Drive = mechanics, TrackDriveAccesses = false }, cancellationToken).ConfigureAwait(false);
        var transferred = false;
        try
        {
            IMediaRuntimeDisk disk = document.Representation switch
            {
                Images.Formats.HardDisk.MfmEmu.MfmEmuImageRepresentation => new MediaRuntimeMfmDisk(engine, document, writable, mechanics is null),
                Images.Models.Flux.FluxMediaImageRepresentation => new MediaRuntimeFluxDisk(engine, document, writable, mechanics is null),
                _ => new MediaRuntimeDisk(engine, document, writable, mechanics is null)
            };
            transferred = true;
            return disk;
        }
        finally
        {
            if (!transferred && mechanics is null) document.Source.Drive?.Reset();
        }
    }
    public Task<RetroMediaLab.MediaEngine.Contracts.MediaCassetteSignal> ReadCassetteAsync(string path, CancellationToken cancellationToken = default, string? machineId = null, IReadOnlyDictionary<string, string>? parameters = null) => RetroMediaLab.MediaEngine.Functions.CassetteSignalFunctions.ReadAsync(path, cancellationToken, machineId, parameters);
    public IReadOnlyList<string> CassetteRecordingExtensions => RetroMediaLab.MediaEngine.Functions.CassetteSignalFunctions.WritableExtensions;
    public IReadOnlyList<string> MechanicalSoundProfiles(string device) => RetroMediaLab.MediaEngine.Functions.MechanicalSoundFunctions.Profiles(device);
    public string MechanicalSoundProfileDisplayName(string profile) => RetroMediaLab.MediaEngine.Dictionaries.MechanicalSoundProfiles.DisplayName(profile);
    public short[] RenderMechanicalSound(string device, string operation, int sampleRate, int sampleCount, long offset, string? profile = null, double volumePercent = RetroMediaLab.MediaEngine.Constants.MechanicalSoundConstants.DefaultVolumePercent) => RetroMediaLab.MediaEngine.Functions.MechanicalSoundFunctions.Render(device, operation, sampleRate, sampleCount, offset, profile, volumePercent);
    public double MechanicalSoundDurationSeconds(string device, string profile, string operation) => RetroMediaLab.MediaEngine.Functions.MechanicalSoundFunctions.Duration(device, profile, operation);
    public short[] ProtectMechanicalAudioMix(IReadOnlyList<double> samples) => RetroMediaLab.MediaEngine.Functions.MechanicalSoundFunctions.ProtectMix(samples);
    public Task WriteCassetteAsync(string path, MediaCassetteSignal signal, CancellationToken cancellationToken = default, IReadOnlyDictionary<string, string>? parameters = null) => RetroMediaLab.MediaEngine.Functions.CassetteSignalFunctions.WriteAsync(path, signal, cancellationToken, parameters);
    private static readonly Lazy<MediaEngineComposition> Conversion = new(MediaEngineComposition.CreateDefault);

    /// <inheritdoc/>
    public Task CreateHardDiskAsync(string path, string formatId, HardDiskGeometry geometry, string? fileSystemId = null, CancellationToken cancellationToken = default, bool preallocate = false)
    {
        ArgumentNullException.ThrowIfNull(geometry); cancellationToken.ThrowIfCancellationRequested();
        if (formatId == HardDiskImageFormatIds.MfmEmu)
            return Images.Formats.HardDisk.MfmEmu.MfmEmuCreation.CreateAsync(path, geometry, fileSystemId, cancellationToken, preallocate);
        if (formatId != HardDiskImageFormatIds.AcornWinchester) throw new NotSupportedException("No runtime HDD creator is registered for this format.");
        if (!RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Acorn.AcornWinchesterFormat.SupportsGeometry(geometry)) throw new ArgumentException("Acorn Winchester requires a supported DSC geometry.", nameof(geometry));
        var ranges = new List<MediaDataRange>();
        var position = 0L;
        if (fileSystemId is not null)
        {
            if (geometry.SectorSize != RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Acorn.AcornWinchesterFormat.SectorSize) throw new NotSupportedException("BBC ADFS preparation requires 256-byte logical blocks.");
            if (fileSystemId != RetroMediaLab.MediaFileSystems.Definitions.FileSystemIds.AcornAdfs) throw new NotSupportedException("The requested filesystem preparation is not registered for this disk.");
            var header = RetroMediaLab.MediaFileSystems.FileSystems.Acorn.Adfs.AcornAdfsFormatter.CreateHeader(checked((int)geometry.LogicalSectorCount));
            ranges.Add(new(0, header.Length, MediaDataRangeKind.Stored, new MemoryRandomAccessData(header))); position = header.Length;
        }
        if (position < geometry.Capacity) ranges.Add(new(position, geometry.Capacity - position, MediaDataRangeKind.Zero));
        var representation = new RetroMediaLab.MediaEngine.Images.Models.Blocks.BlockMediaImageRepresentation(geometry.Capacity, geometry.SectorSize, ranges, geometry);
        var document = MediaImageDocument.CreateUnexplored(new(path, []), formatId, MediaKind.HardDisk, representation, [], new Dictionary<string, string>());
        return new RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Acorn.AcornWinchesterWriter().CreateAsync(document, path, formatId, cancellationToken, preallocate);
    }

    /// <inheritdoc/>
    public Task CreateBlankDiskAsync(string path, string formatId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var image = RetroMediaLab.MediaEngine.Images.Creation.BlankSectorImageFactory.Create(formatId);
        var document = MediaImageDocument.CreateUnexplored(new(path, []), formatId, RetroMediaLab.MediaEngine.Enums.MediaKind.Floppy, new RetroMediaLab.MediaEngine.Images.Models.Sectors.SectorMediaImageRepresentation(image), [], new Dictionary<string, string>());
        return Conversion.Value.Writing.WritingService.WriteAsync(document, path, formatId, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> FileExtensions { get; } = new ReadOnlyDictionary<string, string>(typeof(DiskImageFileExtensions).GetFields(BindingFlags.Public | BindingFlags.Static).Where(field => field.IsLiteral && field.FieldType == typeof(string)).ToDictionary(field => field.Name, field => (string)field.GetRawConstantValue()!, StringComparer.Ordinal));
    /// <inheritdoc/>
    public int RawOpticalSectorSize => OpticalSectorConstants.RawSectorSize;
    /// <inheritdoc/>
    public int AudioSectorSize => OpticalSectorConstants.AudioSectorSize;
    /// <inheritdoc/>
    public int OpticalSubchannelSize => OpticalSectorConstants.SubchannelSize;

    /// <inheritdoc/>
    public Task ConvertScpAsync(string sourcePath, string outputPath, string outputExtension, CancellationToken cancellationToken = default)
    {
        if (outputExtension.Equals(DiskImageFileExtensions.Atr, StringComparison.OrdinalIgnoreCase))
            return Conversion.Value.Conversion.AtariScpRuntimeConverter.ConvertToAtrAsync(sourcePath, outputPath, cancellationToken);
        if (outputExtension.Equals(DiskImageFileExtensions.St, StringComparison.OrdinalIgnoreCase))
            return Conversion.Value.Conversion.AtariScpRuntimeConverter.ConvertToStAsync(sourcePath, outputPath, cancellationToken);
        if (outputExtension.Equals(DiskImageFileExtensions.Adf, StringComparison.OrdinalIgnoreCase))
            return Conversion.Value.Conversion.AmigaAdfRuntimeConverter.ConvertDetectedAsync(sourcePath, outputPath, cancellationToken);
        throw new NotSupportedException(outputExtension);
    }

    /// <inheritdoc/>
    public IMediaCueSheet ReadCueSheet(IEnumerable<string> lines) => new CueSheet(new CueSheetReader().Parse(lines));

    /// <inheritdoc/>
    public bool IsSingleBinaryRawAudioCue(IMediaCueSheet sheet, int maximumTracks)
    {
        var document = RequireDocument(sheet);
        if (document.Files.Count != 1 || document.Files[0].Kind != CueFileKind.Binary) return false;
        var tracks = document.Files[0].Tracks;
        if (tracks.Count == 0 || tracks.Count > maximumTracks) return false;
        for (var index = 0; index < tracks.Count; index++)
        {
            var track = tracks[index];
            if (track.Number != index + 1 || track.Mode is not (OpticalTrackMode.Mode1Raw2352 or OpticalTrackMode.Audio) || !track.Indexes.Any(position => position.Number == 1)) return false;
        }
        return true;
    }

    /// <inheritdoc/>
    public IMediaCueSheet WithSingleCueFile(IMediaCueSheet sheet, string declaredPath)
    {
        var document = RequireDocument(sheet);
        var file = document.Files[0];
        return new CueSheet(new CueSheetDocument([new CueFileDescriptor(declaredPath, file.Kind, file.Tracks)], document.CatalogNumber, document.Metadata));
    }

    /// <inheritdoc/>
    public Task WriteCueSheetAsync(IMediaCueSheet sheet, Stream destination, CancellationToken cancellationToken = default) => new CueSheetWriter().WriteAsync(RequireDocument(sheet), destination, cancellationToken);

    /// <inheritdoc/>
    public async Task PrepareCueWaveFilesAsync(string sourceCue, string stagedCue, Func<string, string> stageFile, Func<Exception> unsupported, int copyBufferBytes, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(copyBufferBytes);
        var sheet = new CueSheetReader().Parse(File.ReadLines(sourceCue));
        var sourceDirectory = Path.GetDirectoryName(sourceCue)!;
        var stagedDirectory = Path.GetDirectoryName(stagedCue)!;
        var files = new List<CueFileDescriptor>();
        var converted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in sheet.Files)
        {
            var original = Path.GetFullPath(file.DeclaredPath, sourceDirectory);
            var staged = stageFile(original);
            if (file.Kind == CueFileKind.WavePcm && converted.Add(staged))
            {
                if (file.Tracks.Any(track => track.Mode != OpticalTrackMode.Audio)) throw unsupported();
                var pcm = WavePcmRandomAccessData.Open(original, requireCompleteSectors: false);
                FileStream? output = null;
                try
                {
                    output = new FileStream(staged, FileMode.Create, FileAccess.Write, FileShare.None);
                    var buffer = new byte[copyBufferBytes];
                    for (long offset = 0; offset < pcm.Length;)
                    {
                        var count = (int)Math.Min(buffer.Length, pcm.Length - offset);
                        await pcm.ReadExactlyAsync(offset, buffer.AsMemory(0, count), cancellationToken).ConfigureAwait(false);
                        await output.WriteAsync(buffer.AsMemory(0, count), cancellationToken).ConfigureAwait(false);
                        offset += count;
                    }
                    var padding = (int)((OpticalSectorConstants.AudioSectorSize - pcm.Length % OpticalSectorConstants.AudioSectorSize) % OpticalSectorConstants.AudioSectorSize);
                    if (padding > 0) await output.WriteAsync(new byte[padding], cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    if (output is not null) await output.DisposeAsync().ConfigureAwait(false);
                }
            }
            files.Add(new CueFileDescriptor(Path.GetRelativePath(stagedDirectory, staged), file.Kind == CueFileKind.WavePcm ? CueFileKind.Binary : file.Kind, file.Tracks));
        }
        var prepared = new CueSheetDocument(files, sheet.CatalogNumber, sheet.Metadata);
        FileStream? cue = null;
        try
        {
            cue = new FileStream(stagedCue, FileMode.Create, FileAccess.Write, FileShare.None);
            await new CueSheetWriter().WriteAsync(prepared, cue, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (cue is not null) await cue.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static CueSheetDocument RequireDocument(IMediaCueSheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        return sheet is CueSheet parsed ? parsed.Document : throw new ArgumentException("The CUE document must come from this preparation service.", nameof(sheet));
    }

    private sealed class CueSheet(CueSheetDocument document) : IMediaCueSheet
    {
        internal CueSheetDocument Document { get; } = document;
        public IReadOnlyList<string> DeclaredFiles { get; } = Array.AsReadOnly(document.Files.Select(file => file.DeclaredPath).ToArray());
    }
}

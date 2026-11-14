using System.Buffers.Binary;
using System.Collections.Frozen;
using System.Globalization;
using System.IO;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Images.Models.Sequential;
using RetroMediaLab.MediaEngine.Images.Reading.Recognition;
using RetroMediaLab.MediaEngine.Images.Reading.Sources;
using RetroMediaLab.MediaEngine.Interfaces.Reading;
using NAudio.Wave;

namespace RetroMediaLab.MediaEngine.Images.Formats.Tape.Audio;

/// <summary>Décode les fichiers audio pris en charge par NAudio et Media Foundation en PCM séquentiel commun.</summary>
public sealed class AudioTapeReader : IMediaImageReader
{
    private static readonly IReadOnlySet<string> SupportedFormatIds = new[] { TapeImageFormatIds.Audio }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlySet<string> SupportedExtensions = new[]
    {
        DiskImageFileExtensions.Mp3,
        DiskImageFileExtensions.Ogg,
        DiskImageFileExtensions.Flac,
        DiskImageFileExtensions.Aac,
        DiskImageFileExtensions.M4a,
        DiskImageFileExtensions.Wma
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    private static readonly IReadOnlySet<MediaKind> SupportedMediaKinds = new[] { MediaKind.Tape }.ToFrozenSet();
    private static readonly IReadOnlySet<MediaRepresentationKind> SupportedRepresentationKinds = new[] { MediaRepresentationKind.Sequential }.ToFrozenSet();

    IReadOnlySet<string> IMediaImageReader.FormatIds => SupportedFormatIds;
    IReadOnlySet<string> IMediaImageReader.Extensions => SupportedExtensions;
    IReadOnlyList<ReadOnlyMemory<byte>> IMediaImageReader.Signatures => [];
    IReadOnlySet<string> IMediaImageReader.AssociatedFileExtensions => FrozenSet<string>.Empty;
    IReadOnlySet<MediaKind> IMediaImageReader.MediaKinds => SupportedMediaKinds;
    IReadOnlySet<MediaRepresentationKind> IMediaImageReader.RepresentationKinds => SupportedRepresentationKinds;
    bool IMediaImageReader.SupportsFormatId(string formatId) => SupportedFormatIds.Contains(formatId);

    ValueTask<bool> IMediaImageReader.CanReadAsync(MediaRecognitionContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult((context.RequestedFormatId is null || SupportedFormatIds.Contains(context.RequestedFormatId)) && SupportedExtensions.Contains(context.Extension));
    }

    Task<MediaImageDocument> IMediaImageReader.ReadAsync(MediaRecognitionContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (context.Extension.Equals(".ogg",StringComparison.OrdinalIgnoreCase))
        {
            VorbisPcmRandomAccessData? source=null; bool transferred=false;
            try
            {
                source=VorbisPcmRandomAccessData.Open(context.Source.PrimaryPath,cancellationToken);
                MediaImageDocument document=CreatePcmDocument(context,source,source.Length,source.Channels,source.SampleRate);
                transferred=true; return Task.FromResult(document);
            }
            finally { if (!transferred) source?.Dispose(); }
        }
        using var reader = new AudioFileReader(context.Source.PrimaryPath);
        var channels = reader.WaveFormat.Channels;
        var sampleRate = reader.WaveFormat.SampleRate;
        if (channels <= 0 || sampleRate <= 0) throw new InvalidDataException("The decoded audio profile is invalid.");

        var pcm = new MemoryStream();
        try
        {
            var samples = new float[Math.Max(sampleRate * channels, 4096)];
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var read = reader.Read(samples, 0, samples.Length);
                if (read == 0) break;
                var bytes = new byte[checked(read * sizeof(short))];
                for (var index = 0; index < read; index++)
                {
                    var sample = (short)Math.Clamp(Math.Round(samples[index] * short.MaxValue), short.MinValue, short.MaxValue);
                    BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(index * sizeof(short), sizeof(short)), sample);
                }
                pcm.Write(bytes, 0, bytes.Length);
            }

            var data = pcm.ToArray();
            return Task.FromResult(CreatePcmDocument(context,new MemoryRandomAccessData(data),data.Length,channels,sampleRate));
        }
        finally
        {
            pcm.Dispose();
        }
    }

    private static MediaImageDocument CreatePcmDocument(MediaRecognitionContext context,
        RetroMediaLab.MediaFileSystems.Interfaces.IMediaRandomAccessData source,long length,int channels,int sampleRate)
    {
            var blockAlign = checked((ushort)(channels * sizeof(short)));
            if (length % blockAlign != 0) throw new InvalidDataException("The decoded audio ends inside a PCM frame.");
            var frames = length / blockAlign;
            var duration = TimeSpan.FromSeconds(frames / (double)sampleRate);
            var segments = Enumerable.Range(0, channels).Select(channel => new SequentialMediaSegment(
                0,
                SequentialSegmentKind.Samples,
                frames,
                TimeSpan.Zero,
                duration,
                channelNumber: channel,
                direction: SequentialTravelDirection.Forward,
                dataRange: new MediaDataRange(0, length, MediaDataRangeKind.Stored, source, 0),
                metadata: new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["channel"] = channel.ToString(CultureInfo.InvariantCulture),
                    ["interleaved"] = bool.TrueString,
                    ["blockAlign"] = blockAlign.ToString(CultureInfo.InvariantCulture)
                })).ToArray();
            return new MediaImageDocument(
                context.Source,
                TapeImageFormatIds.Audio,
                MediaKind.Tape,
                new SequentialMediaImageRepresentation(length, duration, segments),
                [],
                [],
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["channels"] = channels.ToString(CultureInfo.InvariantCulture),
                    ["sampleRate"] = sampleRate.ToString(CultureInfo.InvariantCulture),
                    ["bitsPerSample"] = "16",
                    ["blockAlign"] = blockAlign.ToString(CultureInfo.InvariantCulture),
                    ["sourceExtension"] = context.Extension
                });
    }
}

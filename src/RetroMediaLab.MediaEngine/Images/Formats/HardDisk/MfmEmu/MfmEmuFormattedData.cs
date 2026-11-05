using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec;
using RetroMediaLab.MediaFileSystems.Interfaces;
using A = RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec.AdaptecMfmFormat;

namespace RetroMediaLab.MediaEngine.Images.Formats.HardDisk.MfmEmu;

/// <summary>Génère les pistes préparées lors de leur copie, sans conserver le disque entier.</summary>
internal sealed class MfmEmuFormattedData : IMediaRandomAccessData
{
    private readonly MfmEmuHeader header;
    private readonly HardDiskGeometry geometry;
    private readonly MfmEmuBlankData blank;
    private readonly byte[] logicalPrefix;
    private readonly object sync = new();
    private long cachedTrack = -1;
    private byte[]? cachedBytes;

    internal MfmEmuFormattedData(MfmEmuHeader header, HardDiskGeometry geometry, ReadOnlyMemory<byte> logicalPrefix)
    {
        this.header = header;
        this.geometry = geometry;
        blank = new(header);
        this.logicalPrefix = logicalPrefix.ToArray();
        if (logicalPrefix.Length > geometry.Capacity) throw new ArgumentException(nameof(logicalPrefix));
    }

    public long Length => blank.Length;

    public ValueTask ReadExactlyAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        blank.ReadExactlyAsync(offset, destination, cancellationToken).GetAwaiter().GetResult();
        lock (sync)
        {
            var first = Math.Max(offset, header.HeaderLength);
            var end = Math.Min(offset + destination.Length, header.EndMarkerOffset);
            if (end <= first) return ValueTask.CompletedTask;
            var firstTrack = (first - header.HeaderLength) / header.TrackRecordLength;
            var lastTrack = (end - 1 - header.HeaderLength) / header.TrackRecordLength;
            for (var track = firstTrack; track <= lastTrack; track++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var dataStart = header.HeaderLength + track * header.TrackRecordLength + header.TrackHeaderLength;
                var from = Math.Max(offset, dataStart);
                var until = Math.Min(offset + destination.Length, dataStart + header.TrackDataLength);
                if (until <= from) continue;
                if (cachedTrack != track)
                {
                    cachedBytes = CreateTrack(track);
                    cachedTrack = track;
                }
                cachedBytes!.AsMemory((int)(from - dataStart), (int)(until - from))
                    .CopyTo(destination[(int)(from - offset)..]);
            }
        }
        return ValueTask.CompletedTask;
    }

    private byte[] CreateTrack(long track)
    {
        var count = geometry.SectorsPerTrack;
        var size = geometry.SectorSize;
        var cylinder = (int)(track / geometry.Heads);
        var head = (int)(track % geometry.Heads);
        var sequential = (size, count) is (A.MinimumBlockSize, A.SmallSequentialSectors)
            or (A.MediumBlockSize, A.MediumSequentialSectors);
        var sectors = new MediaDiskSector[count];
        var position = 0;
        for (var logical = 0; logical < count; logical++)
        {
            while (sectors[position] is not null) if (++position == count) position = 0;
            var address = track * count + logical;
            var data = new byte[size];
            var prefixOffset = address * size;
            if (prefixOffset < logicalPrefix.Length)
                logicalPrefix.AsSpan((int)prefixOffset, Math.Min(size, logicalPrefix.Length - (int)prefixOffset)).CopyTo(data);
            sectors[position] = new(address, data, cylinder, head, checked((int)address));
            position = (position + (sequential ? 1 : A.DefaultInterleave)) % count;
        }
        Span<byte> flags = stackalloc byte[A.ParameterFlagCount];
        if (track == 0) new AdaptecMfmGeometry(size, checked((int)geometry.Cylinders), geometry.Heads, count).WriteInitialParameters(flags);
        var encoded = AdaptecMfmTrackCodec.Encode(sectors, sequential, A.NominalIndexNanoseconds,
            parameterFlags: track == 0 ? flags : default);
        uint time = 0;
        var transitions = encoded.Revolutions[0].FluxIntervalsNanoseconds.Select(interval => time += interval).ToArray();
        return MfmEmuTrackSignal.WriteWindow(new byte[header.TrackDataLength], header.SampleRate,
            header.StartNanoseconds, A.NominalIndexNanoseconds, 0, A.NominalIndexNanoseconds, transitions, false);
    }
}

using System.IO;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Images.Models.Flux;
using RetroMediaLab.MediaEngine.Images.Reading.Decoding;
using RetroMediaLab.MediaEngine.Images.Writing.Encoding;

namespace RetroMediaLab.Tests.Emulation.Wd1770;

/// <summary>Média MFM en mémoire : les fronts écrits restent la source de la relecture.</summary>
internal sealed class MemoryTrackDisk(byte seed = 0) : IEmulationDiskImage
{
    private const uint CellNanoseconds = 2_000;
    private const uint RevolutionNanoseconds = 200_000_000;
    private const int BytesPerSector = 256;
    private const int HeadCount = 2;
    private const int TrackCount = 3;
    private const int TrackSectors = 16;
    private static readonly IReadOnlyDictionary<string, int> PhysicalAttributes = new Dictionary<string, int>
        { [TrackEncodingAttributeKeys.MfmPhysicalLayout] = 1 };
    private readonly Dictionary<long, byte[]> _data = new();
    private readonly Dictionary<(int Cylinder, int Head), EmulationDiskTrack> _tracks = new();
    private bool _bad;

    public IEmulationDriveMechanics Mechanics { get; } = RetroMediaLab.Emulation.EmulationSdk.MediaProcessing.CreateDriveMechanics(
        RetroMediaLab.Emulation.Constants.EmulationMechanicalSoundConstants.Floppy);
    public string FormatId => "acorn.adfs.640";
    public int SectorSize => BytesPerSector;
    public long SectorCount => TrackCount * HeadCount * TrackSectors;
    public long Cylinders => TrackCount;
    public int Heads => HeadCount;
    public int SectorsPerTrack => TrackSectors;
    public bool IsReadOnly { get; init; }
    public bool HasChanges { get; private set; }
    public IReadOnlyDictionary<string, string> Metadata { get; } = new Dictionary<string, string>();
    public void UpdateMetadata(IReadOnlyDictionary<string, string> metadata) => throw new NotSupportedException();
    internal bool Disposed { get; private set; }
    internal bool Bad { get => _bad; set { _bad = value; _tracks.Clear(); } }

    internal byte[] Bytes(int cylinder, int head, int number)
    {
        var index = FindSector(cylinder, head, number)!.Value;
        if (!_data.TryGetValue(index, out var data))
            _data[index] = data = Enumerable.Repeat(unchecked((byte)(seed + index)), BytesPerSector).ToArray();
        return data;
    }
    public long? FindSector(int cylinder, int head, int number) => cylinder is >= 0 and < TrackCount
        && head is >= 0 and < HeadCount && number is >= 0 and < TrackSectors
            ? (cylinder * (long)HeadCount + head) * TrackSectors + number : null;
    public ValueTask<EmulationDiskSector> ReadSectorAsync(long index, CancellationToken cancellationToken = default)
    {
        var cylinder = (int)(index / (HeadCount * TrackSectors));
        var head = (int)(index / TrackSectors % HeadCount);
        var number = (int)(index % TrackSectors);
        return ValueTask.FromResult(new EmulationDiskSector(index, Bytes(cylinder, head, number), cylinder, head, number, !Bad));
    }
    public void WriteSector(long index, ReadOnlySpan<byte> data)
    {
        if (IsReadOnly) throw new UnauthorizedAccessException();
        if (data.Length != BytesPerSector) throw new ArgumentException(nameof(data));
        _data[index] = data.ToArray();
        _tracks.Remove(((int)(index / (HeadCount * TrackSectors)), (int)(index / TrackSectors % HeadCount)));
        HasChanges = true;
    }
    public EmulationDiskTrack ReadTrack(int cylinder, int head, string encodingId, uint bitCellNanoseconds,
        uint indexTimeNanoseconds, IReadOnlyDictionary<string, int>? attributes = null)
    {
        if (_tracks.TryGetValue((cylinder, head), out var track)) return track;
        // Changer DDEN sur le contrôleur ne transforme pas le codage déjà enregistré sur le média.
        var sectors = Enumerable.Range(0, TrackSectors).Select(number => new TrackSector(number,
            Bytes(cylinder, head, number)) { DataIntegrityValid = !Bad }).ToArray();
        var encoded = new IsoMfmTrackEncoder().Encode(new(cylinder, head, sectors,
            PhysicalAttributes, CellNanoseconds, RevolutionNanoseconds));
        return _tracks[(cylinder, head)] = new([new(RevolutionNanoseconds, encoded.Revolution.FluxIntervals)]);
    }
    public void WriteTrackWindow(int cylinder, int head, string encodingId, uint bitCellNanoseconds,
        uint indexTimeNanoseconds, uint startNanoseconds, uint durationNanoseconds,
        IReadOnlyList<uint> transitionOffsetsNanoseconds, IReadOnlyDictionary<string, int>? attributes = null,
        bool includeEndingInstant = false)
    {
        if (IsReadOnly) throw new UnauthorizedAccessException();
        var source = ReadTrack(cylinder, head, encodingId, bitCellNanoseconds, indexTimeNanoseconds, attributes).Revolutions[0];
        var written = MediaFluxTrackDataFunctions.ReplaceWindow(new MediaFluxRevolutionData(
            source.IndexTimeNanoseconds, source.FluxIntervalsNanoseconds), startNanoseconds,
            durationNanoseconds, transitionOffsetsNanoseconds, includeEndingInstant);
        _tracks[(cylinder, head)] = new([new(written.IndexTimeNanoseconds, written.FluxIntervalsNanoseconds)]);
        var decoded = new IsoMfmDecoder().Decode(new FluxRevolution(written.IndexTimeNanoseconds, written.FluxIntervalsNanoseconds));
        foreach (var sector in decoded.Sectors)
            if (sector.Data is { Count: BytesPerSector } data && FindSector(sector.Cylinder, sector.Head, sector.Number) is { } index)
                _data[index] = data.ToArray();
        HasChanges = true;
    }
    public Task FlushAsync(CancellationToken cancellationToken = default) { HasChanges = false; return Task.CompletedTask; }
    public void Dispose() { Mechanics.Reset(); Disposed = true; _data.Clear(); _tracks.Clear(); }
}

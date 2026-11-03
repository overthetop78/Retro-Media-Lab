using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.MediaEngine.Images.Writing.Encoding;
using RetroMediaLab.MediaEngine.Images.Reading.Decoding;
using RetroMediaLab.MediaEngine.Images.Models.Flux;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Functions;

namespace RetroMediaLab.Tests.Emulation.Intel8271;

internal class MemoryTrackDisk : IEmulationDiskImage
{
    private readonly Dictionary<(int Cylinder, int Head), EmulationDiskTrack> _tracks = new();
    internal List<EmulationDiskSector> Sectors { get; }
    internal Func<int, int>? HeaderCylinder;
    private int _physicalTrack;
    internal int RequestedHead;
    internal int WindowWrites { get; private set; }
    internal uint LastWriteStart { get; private set; }
    internal uint LastWriteDuration { get; private set; }
    internal IReadOnlyList<uint> LastWritePulses { get; private set; } = [];
    internal EmulationDiskRevolution? BeforeWrite { get; private set; }
    internal MediaFluxRevolutionData? AfterWrite { get; private set; }
    internal MemoryTrackDisk(int size = 128, byte? fill = null) => Sectors = [new(0, Enumerable.Range(0, size).Select(index => fill ?? (byte)index).ToArray(), 0, 0, 1)];
    public IEmulationDriveMechanics Mechanics { get; } = RetroMediaLab.Emulation.EmulationSdk.MediaProcessing.CreateDriveMechanics(RetroMediaLab.Emulation.Constants.EmulationMechanicalSoundConstants.Floppy);
    public virtual string FormatId => string.Empty;
    public virtual int SectorSize => Sectors.Count == 0 ? 128 : Sectors[0].Data.Length;
    public virtual long SectorCount => Sectors.Count;
    public virtual long Cylinders { get; init; } = 1;
    public virtual int Heads { get; init; } = 1;
    public virtual int SectorsPerTrack => Sectors.Count;
    public virtual bool IsReadOnly { get; init; }
    public bool HasChanges { get; protected set; }
    public IReadOnlyDictionary<string, string> Metadata { get; } = new Dictionary<string, string>();
    public void UpdateMetadata(IReadOnlyDictionary<string, string> metadata) => throw new NotSupportedException();
    public virtual IReadOnlyList<long> GetTrackSectors(int cylinder, int head)
    {
        _physicalTrack = cylinder;
        RequestedHead = head;
        return Sectors.Select(sector => sector.Index).ToArray();
    }
    public virtual long? FindSector(int cylinder, int head, int number) => throw new InvalidOperationException("The receiver must search the physical IDs.");
    public virtual ValueTask<EmulationDiskSector> ReadSectorAsync(long index, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(HeaderCylinder is null ? Sectors[(int)index] : Sectors[(int)index] with { Cylinder = HeaderCylinder(_physicalTrack) });
    public virtual void WriteSector(long index, ReadOnlySpan<byte> data) => Sectors[(int)index] = Sectors[(int)index] with { Data = data.ToArray() };
    public EmulationDiskTrack ReadTrack(int cylinder, int head, string encodingId, uint bitCellNanoseconds,
        uint indexTimeNanoseconds, IReadOnlyDictionary<string, int>? attributes = null)
    {
        if (_tracks.TryGetValue((cylinder, head), out var written)) return written;
        var sectors = GetTrackSectors(cylinder, head).Select(index => ReadSectorAsync(index).Result).Select(sector =>
            new TrackSector(sector.Number, sector.Data.ToArray(), sector.DeletedDataMark == true)
            {
                RecordedCylinder = sector.Cylinder,
                RecordedHead = sector.Head,
                RecordedSizeCode = sector.RecordedSizeCode,
                HeaderIntegrityValid = sector.HeaderIntegrityValid,
                DataIntegrityValid = sector.DataIntegrityValid ?? sector.IntegrityValid,
                RecordedErrorCheckCode = sector.RecordedErrorCheckCode
            }).ToArray();
        var encoded = new FluxEncoderRegistry().Encode(encodingId,
            new(cylinder, head, sectors, attributes, bitCellNanoseconds, indexTimeNanoseconds));
        return new([new(indexTimeNanoseconds, encoded.Revolution.FluxIntervals)]);
    }

    public void WriteTrackWindow(int cylinder, int head, string encodingId, uint bitCellNanoseconds,
        uint indexTimeNanoseconds, uint startNanoseconds, uint durationNanoseconds,
        IReadOnlyList<uint> transitionOffsetsNanoseconds, IReadOnlyDictionary<string, int>? attributes = null,
        bool includeEndingInstant = false)
    {
        LastWriteStart = startNanoseconds;
        LastWriteDuration = durationNanoseconds;
        LastWritePulses = transitionOffsetsNanoseconds.ToArray();
        var original = ReadTrack(cylinder, head, encodingId, bitCellNanoseconds, indexTimeNanoseconds, attributes).Revolutions[0];
        BeforeWrite = original;
        var revolution = MediaFluxTrackDataFunctions.ReplaceWindow(
            new MediaFluxRevolutionData(original.IndexTimeNanoseconds, original.FluxIntervalsNanoseconds),
            startNanoseconds, durationNanoseconds, transitionOffsetsNanoseconds, includeEndingInstant);
        AfterWrite = revolution;
        _tracks[(cylinder, head)] = new([new(indexTimeNanoseconds, revolution.FluxIntervalsNanoseconds)]);
        var decoded = new IsoFmDecoder().Decode(new FluxRevolution(indexTimeNanoseconds, revolution.FluxIntervalsNanoseconds), bitCellNanoseconds);
        var sectors = new List<EmulationDiskSector>();
        foreach (var sector in decoded.Sectors)
            sectors.Add(new(sectors.Count, (sector.Data ?? []).ToArray(), sector.Cylinder, sector.Head, sector.Number, sector.IntegrityValid)
            {
                RecordedSizeCode = sector.SizeCode,
                HeaderIntegrityValid = sector.HeaderIntegrityValid,
                DataIntegrityValid = sector.DataIntegrityValid,
                DeletedDataMark = sector.DeletedDataMark,
                RecordedErrorCheckCode = sector.RecordedErrorCheckCode
            });
        ReplaceTrack(cylinder, head, sectors);
        WindowWrites++;
        HasChanges = true;
    }

    protected virtual void ReplaceTrack(int cylinder, int head, IReadOnlyList<EmulationDiskSector> sectors)
    {
        Sectors.Clear();
        Sectors.AddRange(sectors);
    }

    public virtual void Dispose() => Mechanics.Reset();
    public Task FlushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

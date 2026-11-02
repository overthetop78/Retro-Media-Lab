using System.IO;
using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Interfaces;
namespace RetroMediaLab.Tests.Emulation;
internal sealed class AcornTestDisk(byte seed = 0, int sectorSize = 256, int recordSize = 256, int sectorsPerTrack = 4) : IEmulationDiskImage
{
    private readonly Dictionary<long, bool> _dataMarks = new();
    private readonly byte[] _bytes = Enumerable.Range(0, 3 * 2 * sectorsPerTrack * recordSize).Select(index => (byte)(seed + index * 13 + index / 256 * 7)).ToArray();
    public IEmulationDriveMechanics Mechanics { get; } = EmulationSdk.MediaProcessing.CreateDriveMechanics(RetroMediaLab.Emulation.Constants.EmulationMechanicalSoundConstants.HardDisk);
    public string FormatId => "test.st506";
    public int SectorSize => sectorSize;
    public long SectorCount => _bytes.Length / sectorSize;
    public long Cylinders => 3;
    public int Heads => 2;
    public int SectorsPerTrack => sectorsPerTrack * recordSize / sectorSize;
    public bool IsReadOnly { get; init; }
    public bool HasChanges { get; private set; }
    public IReadOnlyDictionary<string, string> Metadata { get; } = new Dictionary<string, string>();
    public bool Bad { get; init; }
    internal Func<EmulationDiskSector, EmulationDiskSector>? ReadTransform { get; init; }
    internal IReadOnlyList<int>? TrackOrder { get; init; }
    internal bool Disposed { get; private set; }

    internal byte[] Block(int block) => _bytes.AsSpan(block * 256, 256).ToArray();
    internal byte[] Record(int record) => _bytes.AsSpan(record * recordSize, recordSize).ToArray();
    public void UpdateMetadata(IReadOnlyDictionary<string, string> metadata) => throw new NotSupportedException();
    public long? FindSector(int cylinder, int head, int number) => cylinder >= 0 && cylinder < Cylinders && head >= 0 && head < Heads && number >= 0 && number < SectorsPerTrack ? (cylinder * Heads + head) * SectorsPerTrack + number : null;
    public IReadOnlyList<long> GetTrackSectors(int cylinder, int head) => cylinder >= 0 && cylinder < Cylinders && head >= 0 && head < Heads ? (TrackOrder ?? Enumerable.Range(0, SectorsPerTrack).ToArray()).Select(number => ((long)cylinder * Heads + head) * SectorsPerTrack + number).ToArray() : [];
    public ValueTask<EmulationDiskSector> ReadSectorAsync(long index, CancellationToken cancellationToken = default)
    {
        var sector = new EmulationDiskSector(index, _bytes.AsMemory(checked((int)(index * sectorSize)), sectorSize).ToArray(), (int)(index / SectorsPerTrack / Heads), (int)(index / SectorsPerTrack % Heads), (int)(index % SectorsPerTrack), !Bad)
        {
            DeletedDataMark = _dataMarks.TryGetValue(index, out var deleted) ? deleted : null
        };
        return ValueTask.FromResult(ReadTransform?.Invoke(sector) ?? sector);
    }

    public void WriteSector(long index, ReadOnlySpan<byte> data)
    {
        if (IsReadOnly)
            throw new UnauthorizedAccessException();
        if (data.Length != sectorSize)
            throw new ArgumentException(nameof(data));
        data.CopyTo(_bytes.AsSpan(checked((int)(index * sectorSize)), sectorSize));
        HasChanges = true;
    }

    public void WriteSector(long index, ReadOnlySpan<byte> data, bool deletedDataMark)
    {
        WriteSector(index, data);
        _dataMarks[index] = deletedDataMark;
    }

    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        HasChanges = false;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        Mechanics.Reset();
        Disposed = true;
    }
}

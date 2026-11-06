using System.IO;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Acorn;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Interfaces;

namespace RetroMediaLab.Tests.Emulation;

/// <summary>Adaptation des octets de la cible aux secteurs du conteneur, sans fichier ni sortie audio.</summary>
public sealed partial class AcornEmuWinchesterStorageTests
{
    [Fact]
    public void AcornAssemblyUsesOnlyTheSdkBridgeForMediaEngine()
    {
        var references = typeof(WinchesterStorage).Assembly.GetReferencedAssemblies();
        Assert.DoesNotContain(references, reference => reference.Name == "retromedialab.mediaengine"
            || reference.Name == "RetroMediaLab.MediaEngine");
    }

    [Theory]
    [InlineData(128)]
    [InlineData(256)]
    [InlineData(512)]
    [InlineData(1000)]
    [InlineData(1024)]
    public void UnalignedTransfersPreserveEveryNeighbouringByte(int sectorSize)
    {
        using var image = new Image(sectorSize);
        var storage = new WinchesterStorage(image);
        var expected = image.Data.ToArray();
        var offset = sectorSize - 7;
        var data = Enumerable.Range(0, sectorSize + 31).Select(index => (byte)(index * 73)).ToArray();
        data.CopyTo(expected, offset);
        storage.Write(offset, data);
        Assert.Equal(expected, image.Data);
        var read = new byte[data.Length];
        Assert.True(storage.Read(offset, read));
        Assert.Equal(data, read);
        Assert.False(image.Disposed);
    }

    [Fact]
    public void WholeSectorWriteDoesNotReadOldData()
    {
        using var image = new Image(512) { FailReads = true };
        var storage = new WinchesterStorage(image);
        var data = Enumerable.Repeat((byte)0x5a, 1024).ToArray();
        storage.Write(512, data);
        Assert.Equal(0, image.ReadCount);
        Assert.Equal(2, image.WriteCount);
        Assert.Equal(data, image.Data.AsSpan(512, 1024).ToArray());
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(2049, 0)]
    [InlineData(2048, 1)]
    [InlineData(2040, 9)]
    [InlineData(long.MaxValue, 1)]
    public void OutOfRangeAccessesDoNotTouchImage(long offset, int length)
    {
        using var image = new Image(256);
        var storage = new WinchesterStorage(image);
        var data = new byte[length];
        Assert.Throws<ArgumentOutOfRangeException>(() => storage.Read(offset, data));
        Assert.Throws<ArgumentOutOfRangeException>(() => storage.Write(offset, data));
        Assert.Equal(0, image.ReadCount);
        Assert.Equal(0, image.WriteCount);
    }

    [Fact]
    public void EmptyTransferAtEndOfImageIsValid()
    {
        using var image = new Image(256);
        var storage = new WinchesterStorage(image);
        Assert.True(storage.Read(storage.CapacityBytes, []));
        storage.Write(storage.CapacityBytes, []);
        Assert.Equal(0, image.ReadCount);
        Assert.Equal(0, image.WriteCount);
    }

    [Fact]
    public void IntegrityFailurePreservesBytesForTheTargetsRequiredErrorTransfer()
    {
        using var image = new Image(256) { IntegrityValid = false };
        var storage = new WinchesterStorage(image);
        var destination = Enumerable.Repeat((byte)0xa5, 256).ToArray();
        Assert.False(storage.Read(0, destination));
        Assert.Equal(image.Data.AsSpan(0, destination.Length).ToArray(), destination);
    }

    [Fact]
    public void PartialSectorReadFailureLeavesImageUnchanged()
    {
        using var image = new Image(512) { FailReads = true };
        var storage = new WinchesterStorage(image);
        var original = image.Data.ToArray();
        Assert.Throws<IOException>(() => storage.Write(7, new byte[256]));
        Assert.Equal(original, image.Data);
        Assert.Equal(0, image.WriteCount);
    }

    [Fact]
    public void WriteProtectionIsCheckedBeforeReadingOrWriting()
    {
        using var image = new Image(512) { IsReadOnly = true };
        var storage = new WinchesterStorage(image);
        Assert.True(storage.IsReadOnly);
        Assert.Throws<UnauthorizedAccessException>(() => storage.Write(7, new byte[256]));
        Assert.Equal(0, image.ReadCount);
        Assert.Equal(0, image.WriteCount);
    }

    [Fact]
    public void PersistentParameterEncodingBelongsToImageAdapter()
    {
        using var image = new Image(256);
        var storage = new WinchesterStorage(image);
        Assert.Null(storage.ReadParameters());
        var parameters = Enumerable.Range(0, 33).Select(value => (byte)value).ToArray();
        storage.WriteParameters(parameters);
        Assert.Equal(Convert.ToBase64String(parameters), image.Metadata[AcornWinchesterFormat.DescriptorMetadataKey]);
        Assert.Equal(parameters, storage.ReadParameters());
        storage.Flush();
        Assert.Equal(1, image.FlushCount);
        Assert.False(image.Disposed);
        image.SetParameters("?");
        Assert.Throws<FormatException>(() => storage.ReadParameters());
    }

    [Fact]
    public void SeekUsesPhysicalGeometryOfTheBorrowedImage()
    {
        using var image = new Image(512);
        var storage = new WinchesterStorage(image);
        Assert.Equal(1, storage.Seek(7 * 512));
        Assert.Equal((1, 1, 1, -1L), image.Drive.Position);
        Assert.Equal(image.Cylinders, storage.Cylinders);
        Assert.Equal(image.Heads, storage.Heads);
        Assert.Equal(image.SectorsPerTrack, storage.SectorsPerTrack);
    }

    [Fact]
    public void FormatPreparationRelaysGeometryWithoutPublishingOrOwningTheImage()
    {
        using var image = new Image(256);
        var before = image.Data.ToArray();
        new WinchesterStorage(image).PrepareFormat(306, 4, 18, 512);
        var geometry = Assert.IsType<RetroMediaLab.Emulation.HardDisks.DiskImageGeometry>(image.PreparedGeometry);
        Assert.Equal(306, geometry.Cylinders);
        Assert.Equal(4, geometry.Heads);
        Assert.Equal(18, geometry.SectorsPerTrack);
        Assert.Equal(512, geometry.SectorSize);
        Assert.Equal(before, image.Data);
        Assert.Empty(image.Metadata);
        Assert.Equal(0, image.FlushCount);
        Assert.False(image.Disposed);
    }

    private sealed class Image(int sectorSize, int sectorsPerTrack = 2) : IEmulationDiskImage
    {
        private readonly Dictionary<string, string> _metadata = new();
        internal byte[] Data { get; } = Enumerable.Range(0, sectorSize * sectorsPerTrack * 4).Select(index => (byte)(index * 17 + 3)).ToArray();
        internal DriveSignals Drive { get; } = new();
        public IEmulationDriveMechanics Mechanics => Drive;
        public string FormatId { get; init; } = string.Empty;
        public int SectorSize => sectorSize;
        public long SectorCount => sectorsPerTrack * 4;
        public long Cylinders { get; init; } = 2;
        public int Heads { get; init; } = 2;
        public int SectorsPerTrack => sectorsPerTrack;
        public bool IsReadOnly { get; init; }
        public bool HasChanges { get; private set; }
        public IReadOnlyDictionary<string, string> Metadata => _metadata;
        internal bool? IntegrityValid { get; init; }
        internal bool FailReads { get; init; }
        internal int ReadCount { get; private set; }
        internal int WriteCount { get; private set; }
        internal int FlushCount { get; private set; }
        internal bool Disposed { get; private set; }
        internal RetroMediaLab.Emulation.HardDisks.DiskImageGeometry? PreparedGeometry { get; private set; }
        public void PrepareBlockFormat(RetroMediaLab.Emulation.HardDisks.DiskImageGeometry geometry) => PreparedGeometry = geometry;
        internal void SetParameters(string value) => _metadata[AcornWinchesterFormat.DescriptorMetadataKey] = value;
        public void UpdateMetadata(IReadOnlyDictionary<string, string> metadata)
        {
            foreach (var pair in metadata) _metadata[pair.Key] = pair.Value;
            HasChanges = true;
        }
        public long? FindSector(int cylinder, int head, int number) => (cylinder * Heads + head) * SectorsPerTrack + number;
        public EmulationDiskTrack ReadTrack(int cylinder, int head, string encodingId, uint bitCellNanoseconds,
            uint indexTimeNanoseconds, IReadOnlyDictionary<string, int>? attributes = null)
        {
            Assert.Equal(AdaptecMfmFormat.EncodingId, encodingId);
            var first = (cylinder * Heads + head) * SectorsPerTrack;
            var sectors = Enumerable.Range(0, SectorsPerTrack).Select(index => new MediaDiskSector(first + index,
                Data.AsMemory((first + index) * SectorSize, SectorSize), cylinder, head, first + index)).ToArray();
            Span<byte> flags = stackalloc byte[9];
            var initial = cylinder == 0 && head == 0;
            if (initial) Assert.True(AcornWinchesterFormat.TryWritePhysicalParameters(
                Convert.FromBase64String(_metadata[AcornWinchesterFormat.DescriptorMetadataKey]), Data.Length, flags));
            var track = AdaptecMfmTrackCodec.Encode(sectors, false, indexTimeNanoseconds, SectorSize, initial ? flags : default);
            return new(track.Revolutions.Select(revolution => new EmulationDiskRevolution(
                revolution.IndexTimeNanoseconds, revolution.FluxIntervalsNanoseconds)).ToArray());
        }
        public ValueTask<EmulationDiskSector> ReadSectorAsync(long index, CancellationToken cancellationToken = default)
        {
            ReadCount++;
            if (FailReads) throw new IOException();
            return ValueTask.FromResult(new EmulationDiskSector(index, Data.AsMemory(checked((int)index * SectorSize), SectorSize), 0, 0, 0, IntegrityValid));
        }
        public void WriteSector(long index, ReadOnlySpan<byte> data)
        {
            WriteCount++;
            data.CopyTo(Data.AsSpan(checked((int)index * SectorSize), SectorSize));
            HasChanges = true;
        }
        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            FlushCount++;
            HasChanges = false;
            return Task.CompletedTask;
        }
        public void Dispose() { Disposed = true; Array.Clear(Data); }
    }

    private sealed class DriveSignals : IEmulationDriveMechanics
    {
        internal (int Cylinder, int Head, int Sector, long Block) Position { get; private set; }
        internal int SeekCalls { get; private set; }
        internal int MotorCalls { get; private set; }
        internal bool MotorRunning { get; private set; }
        public bool HasPendingAudio => false;
        public void Configure(string? profile, double volumePercent, bool enabled) { }
        public void SetTapeOperation(string operation) { }
        public void Insert() { }
        public void Eject() { }
        public void SetMotor(bool running) { MotorCalls++; MotorRunning = running; }
        public void SetActivity(bool reading, bool writing) { }
        public void Seek(int cylinder, int head, int sector = -1, long logicalBlock = -1)
        {
            SeekCalls++;
            Position = (cylinder, head, sector, logicalBlock);
        }
        public short[] RenderAudio(int sampleRate, int sampleCount) => [];
        public void Reset() { }
    }
}

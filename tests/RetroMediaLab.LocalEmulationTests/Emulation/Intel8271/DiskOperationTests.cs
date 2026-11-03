using System.IO;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Interfaces;

namespace RetroMediaLab.Tests.Emulation.Intel8271;

public sealed class DiskOperationTests
{
    private const byte HeadSelectOutput = 0x20;
    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    public void ReadMultipleSelectsDriveSideAndSectorAndAcknowledgesEveryByte(int drive, int side)
    {
        using var first = new MemoryDisk(); using var second = new MemoryDisk(0x80);
        using var controller = Controller(index => index == 0 ? first : second); FastTiming(controller);
        Command(controller, 0x7a, 0x23, (byte)(side << 5));
        Command(controller, (byte)(0x13 | (drive == 0 ? 0x40 : 0x80)), 1, 3, 0x22);
        var read = Transfer(controller);
        var disk = drive == 0 ? first : second;
        Assert.Equal(0, read.Result);
        Assert.Equal(disk.Data((1 * 2 + side) * 10 + 3).Concat(disk.Data((1 * 2 + side) * 10 + 4)), read.Bytes);
        Assert.False(controller.Nmi); Assert.Equal(0, controller.Read(0));
        Assert.True(controller.Motor);
    }

    [Fact]
    public void WriteRequestsEachByteAndPreservesAdjacentSectors()
    {
        using var disk = new MemoryDisk(); using var controller = Controller(_ => disk); FastTiming(controller);
        var before = disk.Data(4).ToArray();
        var bytes = Enumerable.Range(0, 512).Select(index => (byte)(index ^ 0xa5)).ToArray();
        Command(controller, 0x4b, 0, 2, 0x22);
        Assert.Equal(0, Transfer(controller, bytes).Result);
        Assert.Equal(bytes[..256], disk.Data(2)); Assert.Equal(bytes[256..], disk.Data(3)); Assert.Equal(before, disk.Data(4));
        Assert.True(disk.HasChanges);
        Command(controller, 0x53, 0, 2, 0x22); Assert.Equal(bytes, Transfer(controller).Bytes);
    }

    [Fact]
    public void FormatUsesTheSuppliedIdsAndProtectsTheOtherSide()
    {
        using var disk = new MemoryDisk(); using var controller = Controller(_ => disk); FastTiming(controller);
        var firstSide = disk.Data(2).ToArray();
        Command(controller, 0x7a, 0x23, 0x20);
        Command(controller, 0x63, 0, 0, 0x22, 0, 0);
        Assert.Equal(0, Transfer(controller, [0, 1, 2, 1, 0, 1, 3, 1]).Result);
        Assert.All(disk.Data(12), value => Assert.Equal(0xe5, value)); Assert.All(disk.Data(13), value => Assert.Equal(0xe5, value));
        Assert.Equal(firstSide, disk.Data(2));
    }

    [Fact]
    public void ReadIdAndVerifyUseTheSamePhysicalSectorAddresses()
    {
        using var disk = new MemoryDisk(); using var controller = Controller(_ => disk); FastTiming(controller);
        Command(controller, 0x7a, 0x23, 0x20);
        Command(controller, 0x5b, 1, 0, 2);
        Assert.Equal(new byte[] { 1, 1, 0, 1, 1, 1, 1, 1 }, Transfer(controller).Bytes);
        Command(controller, 0x5f, 1, 0, 0x22);
        var verify = Transfer(controller); Assert.Equal(0, verify.Result); Assert.Empty(verify.Bytes);
    }

    [Fact]
    public void ProtectionMissingMediaAndMissingSectorsHaveResultsInsteadOfHostFailures()
    {
        using var disk = new MemoryDisk { IsReadOnly = true }; using var controller = new Intel8271DriveFixture(_ => disk); FastTiming(controller);
        Command(controller, 0x4b, 0, 0, 0x21); Assert.True(controller.Nmi); Assert.Equal(0x12, Transfer(controller).Result); Assert.False(disk.HasChanges);
        Command(controller, 0x53, 0, 99, 0x21); Assert.Equal(0x18, Transfer(controller).Result);
        using var absent = new Intel8271DriveFixture(_ => null); Command(absent, 0x53, 0, 0, 0x21); Assert.Equal(0x10, Transfer(absent).Result);
    }

    [Fact]
    public void UnacknowledgedByteReportsLateDmaAndBadDataReportsCrcAfterTransfer()
    {
        using var disk = new MemoryDisk { BadSector = 0 }; using var controller = new Intel8271DriveFixture(_ => disk); FastTiming(controller);
        Command(controller, 0x53, 0, 0, 0x21);
        for (var tick = 0; tick < 400_000 && (controller.Read(0) & 4) == 0; tick++) controller.Tick();
        Assert.NotEqual(0, controller.Read(0) & 4);
        for (var tick = 0; tick < 128; tick++) controller.Tick();
        Assert.True(controller.Nmi); Assert.Equal(0x0a, controller.Read(1)); Assert.False(controller.Nmi);
        Command(controller, 0x53, 0, 0, 0x21); var read = Transfer(controller);
        Assert.Equal(0x0e, read.Result); Assert.Equal(256, read.Bytes.Length);
    }

    [Fact]
    public void SeekAndResetDoNotOwnOrDisposeTheBorrowedImage()
    {
        using var disk = new MemoryDisk(); using var controller = new Intel8271DriveFixture(_ => disk); FastTiming(controller);
        Command(controller, 0x69, 1); Assert.Equal(0, Transfer(controller).Result);
        Command(controller, 0x7d, 0x12); Assert.Equal(1, controller.Read(1)); Assert.False(controller.Nmi);
        controller.Write(2, 1); Assert.False(controller.Nmi); Assert.False(controller.Motor); Assert.Equal(0, controller.Read(0)); Assert.False(disk.Disposed);
    }

    [Fact]
    public void FirstReadWaitsForMechanicalLoadBeforeTheFirstDataRequest()
    {
        using var disk = new MemoryDisk(); using var controller = new Intel8271DriveFixture(_ => disk);
        Command(controller, 0x7a, 0x17, 0xc1);
        Command(controller, 0x53, 0, 0, 0x21);
        for (var tick = 0; tick < 240_000; tick++) controller.Tick();
        Assert.False(controller.Nmi); Assert.Equal(0x80, controller.Read(0));
        Assert.Equal(0, Transfer(controller).Result);
    }

    private static void FastTiming(Intel8271DriveFixture controller)
    {
        controller.MechanicalSpeedPercent = 0;
        Command(controller, 0x75, 0x0d, 1, 0, 0x10);
        Command(controller, 0x7a, 0x17, 0xc1);
    }

    private static Intel8271DriveFixture Controller(Func<int, IEmulationDiskImage?> disk) =>
        new(disk, headSelect: static output => (output & HeadSelectOutput) != 0 ? 1 : 0);
    private static void Command(Intel8271DriveFixture controller, byte command, params byte[] parameters) { controller.Write(0, command); foreach (var value in parameters) controller.Write(1, value); }
    private static (byte Result, byte[] Bytes) Transfer(Intel8271DriveFixture controller, byte[]? write = null)
    {
        var bytes = new List<byte>(); var written = 0;
        for (var tick = 0; tick < 2_000_000; tick++)
        {
            controller.Tick(); var status = controller.Read(0);
            if ((status & 4) != 0)
            {
                if (write is null) bytes.Add(controller.Read(4));
                else { Assert.True(written < write.Length); controller.Write(4, write[written++]); }
            }
            else if ((status & 0x90) == 0x10) { if (write is not null) Assert.Equal(write.Length, written); return (controller.Read(1), bytes.ToArray()); }
        }
        Assert.Fail("The controller did not complete its command."); return default;
    }

    private sealed class MemoryDisk(byte seed = 0) : MemoryTrackDisk
    {
        private readonly Dictionary<long, byte[]> _data = new();
        public override string FormatId => "acorn.dfs.ds";
        public override int SectorSize => 256;
        public override long SectorCount => DeclaredSectorCount ?? Cylinders * Heads * SectorsPerTrack;
        internal long? DeclaredSectorCount { get; init; }
        public override long Cylinders { get; init; } = 2;
        public override int Heads { get; init; } = 2;
        public override int SectorsPerTrack => 10;
        public override bool IsReadOnly { get; init; }
        internal long? BadSector { get; init; }
        internal bool Disposed { get; private set; }
        internal byte[] Data(long index) { if (!_data.TryGetValue(index, out var data)) _data[index] = data = Enumerable.Repeat((byte)(seed + index), SectorSize).ToArray(); return data; }
        public override long? FindSector(int cylinder, int head, int number) => cylinder is >= 0 and < 2 && head is >= 0 and < 2 && number is >= 0 and < 10 ? (cylinder * 2L + head) * 10 + number : null;
        public override IReadOnlyList<long> GetTrackSectors(int cylinder, int head) =>
            Enumerable.Range(0, SectorsPerTrack).Select(number => FindSector(cylinder, head, number))
                .Where(index => index.HasValue).Select(index => index!.Value).ToArray();
        public override ValueTask<EmulationDiskSector> ReadSectorAsync(long index, CancellationToken cancellationToken = default) => ValueTask.FromResult(new EmulationDiskSector(index, Data(index), (int)(index / 20), (int)(index / 10 % 2), (int)(index % 10), index != BadSector));
        public override void WriteSector(long index, ReadOnlySpan<byte> data) { if (IsReadOnly) throw new UnauthorizedAccessException(); if (data.Length != SectorSize) throw new ArgumentException(nameof(data)); _data[index] = data.ToArray(); HasChanges = true; }
        protected override void ReplaceTrack(int cylinder, int head, IReadOnlyList<EmulationDiskSector> sectors)
        {
            foreach (var sector in sectors)
            {
                var logical = FindSector(cylinder, head, sector.Number) ?? throw new ArgumentException(nameof(sectors));
                WriteSector(logical, sector.Data.Span);
            }
        }
        public override void Dispose() { Disposed = true; _data.Clear(); }
    }
}

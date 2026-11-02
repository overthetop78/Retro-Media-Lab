using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Upd765.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using RetroMediaLab.Emulation.Constants;
using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Interfaces;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.CpcDsk;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using MediaSector = RetroMediaLab.MediaEngine.Contracts.MediaDiskSector;
using static RetroMediaLab.Tests.Emulation.AcornEmuUpd765Tests;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuFloppyFormattingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FormatReceivesFourIdentifierBytesPerSectorAndWritesTheSuppliedIdentifiersAndFill(bool pio)
    {
        using var rig = new FormatRig();
        Specify(rig.Device, pio);
        Send(rig.Device, 0x4d, 0, 2, 3, 0x54, 0xe5);
        byte[][] identifiers = [[9, 1, 5, 3], [0, 0, 1, 2], [0, 0, 5, 2]];
        for (var field = 0; field < identifiers.Length; field++)
        {
            Identifier(rig.Device, identifiers[field], pio);
            Assert.Equal(field, rig.Image.FormatCalls);
            Assert.True(rig.Device.Writing);
            Event(rig.Device);
            Assert.Equal(field + 1, rig.Image.FormatCalls);
        }
        Assert.True(rig.Device.Active); // Gap 4 continues until the next index.
        Assert.False(rig.Device.Irq);
        var result = Result(rig.Device);
        Assert.Equal(new byte[] { 0, 0, 0, 0, 0, 6, 2 }, result);
        Assert.Equal(1536, rig.Device.WrittenBytes(0));
        var formatted = rig.Image.Track(0);
        Assert.Equal(new[] { 5, 1, 5 }, formatted.Select(field => field.Number));
        Assert.Equal(new[] { 9, 0, 0 }, formatted.Select(field => field.Cylinder));
        Assert.Equal(new[] { 1, 0, 0 }, formatted.Select(field => field.Head));
        Assert.Equal((byte)3, formatted[0].RecordedSizeCode);
        foreach (var field in formatted)
        {
            Assert.Equal(512, field.Data.Length); // Command N controls fill length, not the supplied ID N.
            Assert.All(field.Data.ToArray(), value => Assert.Equal(0xe5, value));
        }
        Assert.Equal(500000, rig.Image.DataRate);
        Assert.True(rig.Image.Mfm);
        Assert.All(rig.Image.Track(1).SelectMany(field => field.Data.ToArray()), value => Assert.InRange(value, (byte)0x14, (byte)0x16));
    }

    [Fact]
    public void ResetKeepsCompletedFieldsAndDoesNotPublishTheIncompleteFollowingIdentifier()
    {
        using var rig = new FormatRig();
        Specify(rig.Device, false);
        Send(rig.Device, 0x4d, 0, 2, 3, 0x54, 0xa5);
        Identifier(rig.Device, [0, 0, 0, 2], false);
        Event(rig.Device);
        WaitByte(rig.Device, false, true);
        rig.Device.WriteDma(0xff);
        rig.Device.Reset();
        rig.Device.Advance(1000000);
        Assert.Equal(1, rig.Image.FormatCalls);
        var track = rig.Image.Track(0);
        Assert.All(track[0].Data.ToArray(), value => Assert.Equal(0xa5, value));
        Assert.All(track[1].Data.ToArray(), value => Assert.Equal(0x12, value));
        Assert.All(track[2].Data.ToArray(), value => Assert.Equal(0x13, value));
        Assert.False(rig.Image.Disposed);
        Assert.False(rig.Device.Drq);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ProtectionAtStartOrDuringTheFieldPreventsItsPublication(bool duringField)
    {
        using var rig = new FormatRig();
        rig.Image.ReadOnly = !duringField;
        Specify(rig.Device, false);
        Send(rig.Device, 0x4d, 0, 2, 3, 0x54, 0xe5);
        if (duringField)
        {
            Identifier(rig.Device, [0, 0, 0, 2], false);
            rig.Image.ReadOnly = true;
        }
        Assert.Equal(2, Result(rig.Device)[1]);
        Assert.Equal(0, rig.Image.FormatCalls);
        Assert.False(rig.Image.HasChanges);
    }

    [Fact]
    public void AGeometryPreservingImageAcceptsCanonicalFieldsWithoutTouchingOtherTracks()
    {
        using var image = new TrackImage();
        var original = image.Image;
        var replacement = image.Track(0).Select(field => ToMedia(field with
        {
            Data = Enumerable.Repeat((byte)0xa5, 512).ToArray(), RecordedSizeCode = 2
        })).ToArray();
        var updated = SectorTrackFunctions.Replace(original, 0, 0, replacement, retainGeometry: true);
        Assert.Equal(original.Capacity, updated.Capacity);
        Assert.Equal(original.BlockCount, updated.BlockCount);
        Assert.All(updated.AvailableBlocks.Where(block => block.Address.Cylinder == 0).SelectMany(block => block.Data), value => Assert.Equal(0xa5, value));
        foreach (var block in original.AvailableBlocks)
        {
            Assert.All(block.Data, value => Assert.Equal((byte)(0x11 + block.LogicalBlock), value));
            if (block.Address.Cylinder == 1)
                Assert.Equal(block.Data, updated.AvailableBlocks.Single(other => other.LogicalBlock == block.LogicalBlock).Data);
        }
        var invalid = replacement.ToArray();
        invalid[^1] = invalid[^1] with { Number = invalid[0].Number };
        Assert.Throws<NotSupportedException>(() => SectorTrackFunctions.Replace(original, 0, 0, invalid, retainGeometry: true));
        invalid = replacement.ToArray();
        invalid[^1] = invalid[^1] with { Cylinder = 9 };
        Assert.Throws<NotSupportedException>(() => SectorTrackFunctions.Replace(original, 0, 0, invalid, retainGeometry: true));
    }

    [Fact]
    public void FmFormatUsesTheCommandSizeAndFillForEveryCompletedField()
    {
        using var rig = new FormatRig();
        Specify(rig.Device, false);
        Send(rig.Device, 0x0d, 0, 0, 3, 0x1b, 0x69);
        for (byte number = 0; number < 3; number++)
        {
            Identifier(rig.Device, [0, 0, number, 0], false);
            Event(rig.Device);
        }
        Assert.Equal(0, Result(rig.Device)[0]);
        Assert.False(rig.Image.Mfm);
        Assert.Equal(384, rig.Device.WrittenBytes(0));
        foreach (var field in rig.Image.Track(0))
        {
            Assert.Equal(128, field.Data.Length);
            Assert.All(field.Data.ToArray(), value => Assert.Equal(0x69, value));
        }
    }

    private static void Identifier(Upd765 device, byte[] values, bool pio)
    {
        foreach (var value in values)
        {
            WaitByte(device, pio, true);
            if (pio) device.WriteFifo(value); else device.WriteDma(value);
        }
    }

    private static MediaSector ToMedia(EmulationDiskSector field) => new(field.Index, field.Data,
        field.Cylinder, field.Head, field.Number, field.IntegrityValid)
    {
        RecordedSizeCode = field.RecordedSizeCode,
        HeaderIntegrityValid = field.HeaderIntegrityValid,
        DataIntegrityValid = field.DataIntegrityValid,
        DeletedDataMark = field.DeletedDataMark
    };

    private sealed class FormatRig : IDisposable
    {
        internal TrackImage Image { get; } = new();
        internal Upd765 Device { get; }

        internal FormatRig() => Device = new(unit => unit == 0 ? Image : null,
            unit => (byte)(unit == 0 ? 0x38 | (Image.ReadOnly ? 0x40 : 0) : 0x10),
            (_, _) => { }, _ => 0, boardClockHz: 1000000);

        public void Dispose()
        {
            try { Device.Reset(); }
            finally { Image.Dispose(); }
        }
    }

    private sealed class TrackImage : IEmulationDiskImage
    {
        internal SectorImage Image { get; private set; } = new(CpcDskFormat.FormatId, 512, 2, 1, 3,
            Enumerable.Range(0, 6).Select(index => new SectorBlock(index, new(index / 3, 0, index % 3),
                Enumerable.Repeat((byte)(0x11 + index), 512).ToArray())));
        internal int FormatCalls { get; private set; }
        internal int? DataRate { get; private set; }
        internal bool? Mfm { get; private set; }
        internal bool ReadOnly;
        internal bool Disposed { get; private set; }
        public IEmulationDriveMechanics Mechanics { get; } = EmulationSdk.MediaProcessing.CreateDriveMechanics(EmulationMechanicalSoundConstants.Floppy);
        public string FormatId => Image.FormatId;
        public int SectorSize => Image.BlockSize;
        public long SectorCount => Image.BlockCount;
        public long Cylinders => Image.Cylinders;
        public int Heads => Image.Heads;
        public int SectorsPerTrack => Image.SectorsPerTrack;
        public bool IsReadOnly => ReadOnly;
        public bool HasChanges { get; private set; }
        public IReadOnlyDictionary<string, string> Metadata { get; } = new Dictionary<string, string>();
        public void UpdateMetadata(IReadOnlyDictionary<string, string> metadata) => throw new NotSupportedException();
        public long? FindSector(int cylinder, int head, int number) => Image.AvailableBlocks
            .FirstOrDefault(block => block.Address == new SectorAddress(cylinder, head, number))?.LogicalBlock;
        public IReadOnlyList<long> GetTrackSectors(int cylinder, int head) => Image.AvailableBlocks
            .Where(block => block.Address.Cylinder == cylinder && block.Address.Head == head)
            .OrderBy(block => block.RecordedTrackOrder ?? block.LogicalBlock).Select(block => (long)block.LogicalBlock).ToArray();
        public ValueTask<EmulationDiskSector> ReadSectorAsync(long index, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Sector(index));

        private EmulationDiskSector Sector(long index)
        {
            var block = Image.AvailableBlocks.Single(block => block.LogicalBlock == index);
            var address = block.RecordedAddress ?? block.Address;
            return new EmulationDiskSector(index, block.Data.ToArray(), address.Cylinder, address.Head, address.Number, block.IntegrityValid)
            {
                RecordedSizeCode = block.RecordedSizeCode,
                HeaderIntegrityValid = block.HeaderIntegrityValid,
                DataIntegrityValid = block.DataIntegrityValid,
                DeletedDataMark = block.DeletedDataMark
            };
        }
        internal EmulationDiskSector[] Track(int cylinder) => GetTrackSectors(cylinder, 0)
            .Select(Sector).ToArray();
        public void WriteSector(long index, ReadOnlySpan<byte> data) => throw new NotSupportedException();
        public void FormatTrack(int cylinder, int head, IReadOnlyList<EmulationDiskSector> sectors, int? dataRate = null, bool? mfm = null)
        {
            Image = SectorTrackFunctions.Replace(Image, cylinder, head, sectors.Select(ToMedia).ToArray(), retainGeometry: false);
            FormatCalls++;
            HasChanges = true;
            DataRate = dataRate;
            Mfm = mfm;
        }
        public Task FlushAsync(CancellationToken cancellationToken = default) { HasChanges = false; return Task.CompletedTask; }
        public void Dispose() { Mechanics.Reset(); Disposed = true; }
    }
}

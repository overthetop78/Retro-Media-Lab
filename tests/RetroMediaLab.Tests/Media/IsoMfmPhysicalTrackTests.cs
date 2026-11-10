using RetroMediaLab.MediaEngine.Images.Writing.Encoding;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;

namespace RetroMediaLab.Tests.Media;

public sealed class IsoMfmPhysicalTrackTests
{
    [Theory]
    [InlineData("acorn.dfs.ss", 256)]
    [InlineData("ibm.360", 512)]
    public void ReconstructingAnOverfullPhysicalTrackRefusesToLoseSectorData(string format, int bytes)
    {
        var image = new SectorImage(format, bytes, 1, 1, 40,
            Enumerable.Range(0, 40).Select(number => new SectorBlock(number, new(0, 0, number), new byte[bytes])));
        Assert.Throws<NotSupportedException>(() => new SectorImageTrackEncoder().EncodePhysical(image));
    }

    [Fact]
    public void ReconstructedDfsFluxRecordsTheGapClocksInsteadOfVisualizationSilence()
    {
        var image = new SectorImage("acorn.dfs.ss", 256, 1, 1, 10,
            Enumerable.Range(0, 10).Select(number => new SectorBlock(number, new(0, 0, number), new byte[256])));
        var service = new RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp.Conversion.SectorImageScpConversionService(
            new SectorImageTrackEncoder(), new RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp.ScpEncodedTrackFluxService(),
            new RetroMediaLab.MediaEngine.Images.Formats.Floppy.Scp.ScpWriter());
        var flux = Assert.Single(Assert.Single(service.Create(image).Tracks).Revolutions).Flux;
        Assert.Equal(8_000_000u, flux.IndexTimeTicks); //200 ms in25 ns SCP ticks.
        Assert.All(flux.FluxIntervals.Take(32), interval => Assert.Equal(160u, interval)); //FF has both FM clock and data transitions.
        Assert.InRange(flux.FluxIntervals.Aggregate(0UL, (sum, interval) => sum + interval), 7_999_680UL, 8_000_000UL);
    }

    private static readonly IReadOnlyDictionary<string, int> Attributes =
        new Dictionary<string, int> { [TrackEncodingAttributeKeys.MfmPhysicalLayout] = 1 };

    [Theory]
    [InlineData("acorn.dfs.ssd", "iso.fm", 50_000)]
    [InlineData("acorn.adfs.640", "iso.mfm", 100_000)]
    public void PhysicalEncodingBelongsToTheMediaRatherThanTheController(string format, string codec, int bits)
    {
        var image = new SectorImage(format, 256, 1, 1, 1, [new SectorBlock(0, new(0, 0, 0), new byte[256])]);
        var track = new SectorImageTrackEncoder().EncodePhysicalTrack(image, 0, 0, 200_000_000);
        Assert.Equal(codec, track.EncoderId);
        Assert.Equal(bits, track.Bits.Count);
        Assert.Equal(200_000_000u, track.Revolution.IndexTimeTicks);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(40)]
    public void PhysicalTrackKeepsItsRevolutionLengthEvenWhenItOverflows(int count)
    {
        var sectors = Enumerable.Range(0, count).Select(number => new TrackSector(number, new byte[256])).ToArray();
        var track = new IsoMfmTrackEncoder().Encode(new(0, 0, sectors, Attributes, 2_000, 200_000_000));
        Assert.Equal(100_000, track.Bits.Count);
        Assert.Equal(200_000_000u, track.Revolution.IndexTimeTicks);
        Assert.True(track.Revolution.FluxIntervals.Aggregate(0UL, (total, value) => total + value) <= 200_000_000UL);
    }

    [Fact]
    public void RecordedChrnAndTheirCrcAreIndependentOfThePhysicalTrack()
    {
        var sector = new TrackSector(9, new byte[128]) { RecordedCylinder = 42, RecordedHead = 3, RecordedSizeCode = 0 };
        var track = new IsoMfmTrackEncoder().Encode(new(37, 1, [sector], Attributes, 2_000, 200_000_000));
        var mark = FindMark(track.Bits, 0xfe);
        Assert.Equal(new byte[] { 0xfe, 42, 3, 9, 0 }, ReadBytes(track.Bits, mark, 5));
        Assert.Equal(0, Crc(new byte[] { 0xa1, 0xa1, 0xa1 }.Concat(ReadBytes(track.Bits, mark, 7))));
    }

    [Fact]
    public void HeaderAndDataDamageRemainSeparateAndExplicitDataCrcIsNotRecomputed()
    {
        var sector = new TrackSector(1, new byte[128])
        {
            HeaderIntegrityValid = false,
            DataIntegrityValid = false,
            RecordedErrorCheckCode = new byte[] { 0x12, 0x34 }
        };
        var track = new IsoMfmTrackEncoder().Encode(new(0, 0, [sector], Attributes, 2_000, 200_000_000));
        var id = FindMark(track.Bits, 0xfe);
        var data = FindMark(track.Bits, 0xfb);
        Assert.NotEqual(0, Crc(new byte[] { 0xa1, 0xa1, 0xa1 }.Concat(ReadBytes(track.Bits, id, 7))));
        Assert.Equal(new byte[] { 0x12, 0x34 }, ReadBytes(track.Bits, data + 129 * 16, 2));
    }

    [Fact]
    public void ThePhysicalHeaderToDataGapContains22GapBytesAnd12ZeroSyncBytes()
    {
        var track = new IsoMfmTrackEncoder().Encode(new(0, 0, [new TrackSector(1, new byte[128])], Attributes, 2_000, 200_000_000));
        var id = FindMark(track.Bits, 0xfe);
        var data = FindMark(track.Bits, 0xfb);
        var gap = ReadBytes(track.Bits, id + 7 * 16, 34);
        Assert.Equal(Enumerable.Repeat((byte)0x4e, 22).Concat(new byte[12]), gap);
        Assert.Equal(id + (7 + 34 + 3) * 16, data);
    }

    private static int FindMark(IReadOnlyList<bool> bits, byte mark)
    {
        for (var position = 0; position + 64 <= bits.Count; position++)
            if (Word(bits, position) == 0x4489 && Word(bits, position + 16) == 0x4489
                && Word(bits, position + 32) == 0x4489 && ReadBytes(bits, position + 48, 1)[0] == mark)
                return position + 48;
        throw new InvalidOperationException("Expected MFM field is absent.");
    }
    private static ushort Word(IReadOnlyList<bool> bits, int offset)
    {
        var value = 0;
        for (var bit = 0; bit < 16; bit++) value = value << 1 | (bits[offset + bit] ? 1 : 0);
        return (ushort)value;
    }
    private static byte[] ReadBytes(IReadOnlyList<bool> bits, int offset, int count)
    {
        var bytes = new byte[count];
        for (var index = 0; index < count; index++)
            for (var bit = 0; bit < 8; bit++)
                bytes[index] = (byte)(bytes[index] << 1 | (bits[offset + index * 16 + bit * 2 + 1] ? 1 : 0));
        return bytes;
    }
    private static ushort Crc(IEnumerable<byte> bytes) => Crc16Calculator.Compute(bytes, 0x1021, 0xffff);
}

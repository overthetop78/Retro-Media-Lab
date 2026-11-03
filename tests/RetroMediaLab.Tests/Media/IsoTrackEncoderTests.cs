using System.Buffers.Binary;
using System.Security.Cryptography;
using RetroMediaLab.MediaEngine.Images.Writing.Encoding;
using RetroMediaLab.MediaEngine.Images.Reading.Decoding;
using RetroMediaLab.MediaEngine.Images.Models.Sectors;
using RetroMediaLab.MediaEngine.Images.Models.Flux;
using RetroMediaLab.MediaEngine.Constants;

namespace RetroMediaLab.Tests.Media;

public sealed class IsoTrackEncoderTests
{
    [Theory]
    [InlineData(false, 7296, 5239, "DC307D0ED0F1CD6C90D44427BF50AA6881727B7FD6BE745247E49C241D4192A2", "6703E5A17F84978457062C9178A63772A822ABA70AD93C7A3A341D5FCF56532D")]
    [InlineData(true, 7488, 2909, "1BF9E4D58DB1A11041FAF42EA99ED3432B5F53033B548A935BC0A94F4F66D8FF", "966C88652533BCABBE8014C79BBB77F8C1F6B1A875F4C2B1AF966086755A4BF8")]
    public void BitsAndFluxMatchTheReferenceBeforeExtraction(bool mfm, int bitCount, int intervalCount,
        string bitsSha256, string fluxSha256)
    {
        var first = Enumerable.Range(0, 128).Select(value => (byte)((value * 17 + 3) % 256)).ToArray();
        var second = Enumerable.Range(0, 256).Select(value => (byte)((value * 29 + 7) % 256)).ToArray();
        var track = Encoder(mfm).Encode(new(37, 1,
            [new TrackSector(3, first), new TrackSector(7, second, Deleted: true, SizeCode: 1)]));
        Assert.Equal(mfm ? "iso.mfm" : "iso.fm", track.EncoderId);
        Assert.Equal(bitCount, track.Bits.Count);
        Assert.Equal(bitsSha256, Convert.ToHexString(SHA256.HashData(
            track.Bits.Select(bit => bit ? (byte)1 : (byte)0).ToArray())));
        Assert.Equal(8_000_000u, track.Revolution.IndexTimeTicks);
        Assert.Equal(intervalCount, track.Revolution.FluxIntervals.Count);
        var bytes = new byte[intervalCount * sizeof(uint)];
        for (var index = 0; index < intervalCount; index++)
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(index * sizeof(uint), sizeof(uint)),
                track.Revolution.FluxIntervals[index]);
        Assert.Equal(fluxSha256, Convert.ToHexString(SHA256.HashData(bytes)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SectorAddressStillUsesItsFormatValidation(bool mfm)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Encoder(mfm).Encode(new(0, 0, [new TrackSector(256, new byte[128])])));
        Assert.Equal("Number", error.ParamName);
        Assert.Equal(256, error.ActualValue);
        Assert.Contains(mfm ? "ISO MFM" : "ISO FM", error.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MismatchedSectorSizeStillUsesItsFormatValidation(bool mfm)
    {
        var error = Assert.Throws<ArgumentException>(() =>
            Encoder(mfm).Encode(new(0, 0, [new TrackSector(1, new byte[128], SizeCode: 1)])));
        Assert.Contains(mfm ? "ISO MFM" : "ISO FM", error.Message);
    }

    [Fact]
    public void FmPreservesRecordedChrnIndependentlyOfThePhysicalTrackAndPayloadLength()
    {
        var sector = new TrackSector(9, new byte[128])
        {
            RecordedCylinder = 42,
            RecordedHead = 3,
            RecordedSizeCode = 7
        };
        var track = new IsoFmTrackEncoder().Encode(new(37, 1, [sector]));
        Assert.Equal(new byte[] { 42, 3, 9, 7 }, ReadBytes(track.Bits, 16, 4));
        Assert.Equal(0, Crc(new byte[] { 0xfe }.Concat(ReadBytes(track.Bits, 16, 6))));
    }

    [Fact]
    public void FmCanRepresentABadHeaderWithoutChangingTheDataField()
    {
        var sector = new TrackSector(1, new byte[128]);
        var encoder = new IsoFmTrackEncoder();
        var valid = encoder.Encode(new(0, 0, [sector]));
        var damaged = encoder.Encode(new(0, 0, [sector with { HeaderIntegrityValid = false }]));
        Assert.NotEqual(0, Crc(new byte[] { 0xfe }.Concat(ReadBytes(damaged.Bits, 16, 6))));
        Assert.Equal(valid.Bits.Skip(7 * 16), damaged.Bits.Skip(7 * 16));
    }

    [Fact]
    public void FmUsesTheRecordedDataCrcEvenWhenItDisagreesWithAnIntegrityFlag()
    {
        var sector = new TrackSector(1, new byte[128])
        {
            RecordedErrorCheckCode = new byte[] { 0x12, 0x34 },
            DataIntegrityValid = false
        };
        var track = new IsoFmTrackEncoder().Encode(new(0, 0, [sector]));
        var mark = FindWord(track.Bits, 0xf56f);
        Assert.Equal(new byte[] { 0x12, 0x34 }, ReadBytes(track.Bits, mark + (1 + 128) * 16, 2));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void FmPreservesDataIntegrityAndDeletedMarks(bool deleted, bool damaged)
    {
        var sector = new TrackSector(1, Enumerable.Range(0, 128).Select(value => (byte)value).ToArray(), Deleted: deleted)
        { DataIntegrityValid = !damaged };
        var track = new IsoFmTrackEncoder().Encode(new(0, 0, [sector]));
        var mark = FindWord(track.Bits, deleted ? (ushort)0xf56a : (ushort)0xf56f);
        var crc = Crc(new[] { deleted ? (byte)0xf8 : (byte)0xfb }.Concat(ReadBytes(track.Bits, mark + 16, 130)));
        if (damaged) Assert.NotEqual(0, crc);
        else Assert.Equal(0, crc);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(256)]
    public void FmRejectsUnrepresentableRecordedCylinder(int cylinder)
    {
        var sector = new TrackSector(1, new byte[128]) { RecordedCylinder = cylinder };
        var error = Assert.Throws<ArgumentOutOfRangeException>(() => new IsoFmTrackEncoder().Encode(new(0, 0, [sector])));
        Assert.Equal(nameof(TrackSector.RecordedCylinder), error.ParamName);
    }

    [Fact]
    public void FmKeepsRepeatedIdsInTheSuppliedPhysicalOrder()
    {
        var sectors = new[]
        {
            new TrackSector(7, new byte[128]) { RecordedCylinder = 4, RecordedSizeCode = 3 },
            new TrackSector(7, new byte[256]) { RecordedCylinder = 2, RecordedSizeCode = 0 }
        };
        var track = new IsoFmTrackEncoder().Encode(new(0, 0, sectors));
        var first = FindWord(track.Bits, 0xf57e);
        var second = FindWord(track.Bits, 0xf57e, first + 16);
        Assert.Equal(new byte[] { 4, 0, 7, 3 }, ReadBytes(track.Bits, first + 16, 4));
        Assert.Equal(new byte[] { 2, 0, 7, 0 }, ReadBytes(track.Bits, second + 16, 4));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PhysicalFmTrackIncludesSyncAndRoundTripsThroughTheExistingFluxDecoder(bool deleted)
    {
        var payload = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();
        var track = new IsoFmTrackEncoder().Encode(PhysicalRequest(
            [new TrackSector(2, payload, Deleted: deleted) { RecordedCylinder = 7 }]));
        Assert.Equal(50_000, track.Bits.Count);
        Assert.Equal(200_000u, track.Revolution.IndexTimeTicks);
        Assert.Equal(200_000L, track.Revolution.FluxIntervals.Sum(value => (long)value));
        var id = FindWord(track.Bits, 0xf57e);
        Assert.Equal(22 * 16, id);
        Assert.Equal(Enumerable.Repeat((byte)0xff, 16), ReadBytes(track.Bits, 0, 16));
        Assert.Equal(new byte[6], ReadBytes(track.Bits, id - 6 * 16, 6));
        var data = FindWord(track.Bits, deleted ? (ushort)0xf56a : (ushort)0xf56f);
        Assert.Equal(new byte[6], ReadBytes(track.Bits, data - 6 * 16, 6));
        var decoded = Assert.Single(new IsoFmDecoder().Decode(track.Revolution).Sectors);
        Assert.Equal(7, decoded.Cylinder);
        Assert.Equal(2, decoded.Number);
        Assert.Equal(deleted, decoded.DeletedDataMark);
        Assert.True(decoded.HeaderIntegrityValid);
        Assert.True(decoded.DataIntegrityValid);
        Assert.Equal(payload, decoded.Data);
    }

    [Fact]
    public void PhysicalFmTrackTruncatesAnOverfullDataFieldAtIndexInsteadOfSlowingRotation()
    {
        var track = new IsoFmTrackEncoder().Encode(PhysicalRequest(
            [new TrackSector(1, new byte[4096])]));
        Assert.Equal(50_000, track.Bits.Count);
        Assert.Equal(199_996L, track.Revolution.FluxIntervals.Sum(value => (long)value));
        var decoded = Assert.Single(new IsoFmDecoder().Decode(track.Revolution, 4).Sectors);
        Assert.True(decoded.HeaderIntegrityValid);
        Assert.Equal(4096, decoded.Data!.Count);
        Assert.False(decoded.DataIntegrityValid);
    }

    [Fact]
    public void PhysicalFmTrackPreservesARepeatedIdAndItsDamageAcrossFluxEncoding()
    {
        var track = new IsoFmTrackEncoder().Encode(PhysicalRequest(
        [
            new TrackSector(4, new byte[128]) { HeaderIntegrityValid = false },
            new TrackSector(4, new byte[128]) { DataIntegrityValid = false }
        ]));
        var decoded = new IsoFmDecoder().Decode(track.Revolution).Sectors;
        Assert.Equal(2, decoded.Count);
        Assert.All(decoded, sector => Assert.Equal(4, sector.Number));
        Assert.False(decoded[0].HeaderIntegrityValid);
        Assert.True(decoded[0].DataIntegrityValid);
        Assert.True(decoded[1].HeaderIntegrityValid);
        Assert.False(decoded[1].DataIntegrityValid);
    }

    [Fact]
    public void PhysicalFmCanRepresentAnEmptyTrackWithoutInventingASector()
    {
        var track = new IsoFmTrackEncoder().Encode(PhysicalRequest([]));
        Assert.Equal(50_000, track.Bits.Count);
        Assert.Empty(new IsoFmDecoder().Decode(track.Revolution, 4).Sectors);
        Assert.Equal(Enumerable.Repeat((byte)0xff, 3125), ReadBytes(track.Bits, 0, 3125));
        Assert.Throws<ArgumentException>(() => new IsoFmTrackEncoder().Encode(new(0, 0, [])));
    }

    [Fact]
    public void SectorImageEncodingUsesRecordedOrderAndIdentityInsteadOfLogicalBlockOrder()
    {
        var image = new SectorImage(DiskImageFormatIds.AcornDfsSingleSided, 128, 1, 1, 2,
        [
            new SectorBlock(0, new(0, 0, 0), new byte[128])
            { RecordedTrackOrder = 1, RecordedAddress = new(9, 3, 7), HeaderIntegrityValid = false },
            new SectorBlock(1, new(0, 0, 1), new byte[128])
            { RecordedTrackOrder = 0, RecordedAddress = new(5, 2, 7), DataIntegrityValid = false }
        ]);
        var request = PhysicalRequest([]);
        var track = new SectorImageTrackEncoder().EncodeTrack(image, 0, 0, "iso.fm",
            request.Attributes, request.BitCellTicks, request.IndexTimeTicks);
        var fields = new IsoFmDecoder().Decode(track.Revolution, 4).Sectors;
        Assert.Equal(2, fields.Count);
        Assert.Equal(5, fields[0].Cylinder);
        Assert.Equal(2, fields[0].Head);
        Assert.False(fields[0].DataIntegrityValid);
        Assert.Equal(9, fields[1].Cylinder);
        Assert.Equal(3, fields[1].Head);
        Assert.False(fields[1].HeaderIntegrityValid);
        Assert.All(fields, field => Assert.Equal(7, field.Number));
    }

    [Fact]
    public void TargetedSectorImageEncodingDoesNotBorrowFieldsFromAnotherPhysicalTrack()
    {
        var image = new SectorImage(DiskImageFormatIds.AcornDfsSingleSided, 128, 2, 1, 1,
            [new SectorBlock(0, new(0, 0, 0), new byte[128])]);
        var request = PhysicalRequest([]);
        var track = new SectorImageTrackEncoder().EncodeTrack(image, 1, 0, "iso.fm",
            request.Attributes, request.BitCellTicks, request.IndexTimeTicks);
        Assert.Empty(new IsoFmDecoder().Decode(track.Revolution, 4).Sectors);
    }

    [Fact]
    public void KnownClockDecodingReadsAnIdAndItsDataAcrossThePhysicalIndex()
    {
        var payload = Enumerable.Range(0, 256).Select(value => (byte)(value ^ 0x5a)).ToArray();
        var original = new IsoFmTrackEncoder().Encode(PhysicalRequest([new TrackSector(8, payload)]));
        var rotation = FindWord(original.Bits, 0xf57e) + 3 * 16;
        var rotated = original.Bits.Skip(rotation).Concat(original.Bits.Take(rotation)).ToArray();
        var revolution = FluxRevolutionFactory.Create(rotated, 4, 200_000, includeTrailingInterval: false);
        var decoded = Assert.Single(new IsoFmDecoder().Decode(revolution, 4).Sectors);
        Assert.Equal(8, decoded.Number);
        Assert.Equal(payload, decoded.Data);
        Assert.True(decoded.HeaderIntegrityValid);
        Assert.True(decoded.DataIntegrityValid);
    }

    [Fact]
    public void MissingFmClocksInvalidateAFieldEvenWhenItsDataAndCrcBytesRemainCorrect()
    {
        var original = new IsoFmTrackEncoder().Encode(PhysicalRequest([new TrackSector(1, new byte[128])]));
        var bits = original.Bits.ToArray();
        var mark = FindWord(bits, 0xf56f);
        bits[mark + 16] = false;
        var decoded = Assert.Single(new IsoFmDecoder().Decode(
            FluxRevolutionFactory.Create(bits, 4, 200_000), 4).Sectors);
        Assert.Equal(new byte[128], decoded.Data);
        Assert.True(decoded.HeaderIntegrityValid);
        Assert.False(decoded.DataIntegrityValid);
        Assert.Equal(0, Crc(new byte[] { 0xfb }.Concat(decoded.Data!).Concat(decoded.RecordedErrorCheckCode.ToArray())));
    }

    [Fact]
    public void DecoderRetainsTheActualFaultyIdAndDataCrcBytes()
    {
        var sector = new TrackSector(1, new byte[128])
        { HeaderIntegrityValid = false, RecordedErrorCheckCode = new byte[] { 0x12, 0x34 } };
        var encoded = new IsoFmTrackEncoder().Encode(PhysicalRequest([sector]));
        var decoded = Assert.Single(new IsoFmDecoder().Decode(encoded.Revolution, 4).Sectors);
        var id = FindWord(encoded.Bits, 0xf57e);
        Assert.Equal(ReadBytes(encoded.Bits, id + 5 * 16, 2), decoded.RecordedHeaderErrorCheckCode.ToArray());
        Assert.Equal(new byte[] { 0x12, 0x34 }, decoded.RecordedErrorCheckCode.ToArray());
        Assert.False(decoded.HeaderIntegrityValid);
        Assert.False(decoded.DataIntegrityValid);
    }

    private static TrackEncodeRequest PhysicalRequest(IReadOnlyList<TrackSector> sectors)
        => new(0, 0, sectors, new Dictionary<string, int>
        { [TrackEncodingAttributeKeys.FmPhysicalLayout] = 1 }, BitCellTicks: 4, IndexTimeTicks: 200_000);

    private static int FindWord(IReadOnlyList<bool> bits, ushort expected, int start = 0)
    {
        for (var offset = start; offset + 16 <= bits.Count; offset += 16)
        {
            var word = 0;
            for (var bit = 0; bit < 16; bit++) word = (word << 1) | (bits[offset + bit] ? 1 : 0);
            if (word == expected) return offset;
        }
        Assert.Fail("No requested FM address mark.");
        return -1;
    }

    private static byte[] ReadBytes(IReadOnlyList<bool> bits, int start, int count)
    {
        var result = new byte[count];
        for (var index = 0; index < count; index++)
            for (var bit = 0; bit < 8; bit++) result[index] = (byte)((result[index] << 1) | (bits[start + index * 16 + bit * 2 + 1] ? 1 : 0));
        return result;
    }

    private static ushort Crc(IEnumerable<byte> values)
    {
        ushort crc = 0xffff;
        foreach (var value in values)
        {
            crc ^= (ushort)(value << 8);
            for (var bit = 0; bit < 8; bit++) crc = (ushort)((crc & 0x8000) != 0 ? crc << 1 ^ 0x1021 : crc << 1);
        }
        return crc;
    }

    private static TrackEncoderBase Encoder(bool mfm) => mfm ? new IsoMfmTrackEncoder() : new IsoFmTrackEncoder();
}

using System.Buffers.Binary;
using System.Text;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.MfmEmu;

namespace RetroMediaLab.Tests.Media;

public sealed class MfmEmuHeaderTests
{
    [Fact]
    public void CurrentHeaderMatchesThePublishedByteLayout()
    {
        var header = MfmEmuHeader.Create(306, 4, 65536, 10000000);
        Assert.Equal(new byte[]
        {
            0xee, 0x4d, 0x46, 0x4d, 0x0d, 0x0a, 0x1a, 0,
            0, 2, 2, 2, 50, 0, 0, 0, 0, 0, 1, 0, 12, 0, 0, 0,
            0x32, 1, 0, 0, 4, 0, 0, 0, 0x80, 0x96, 0x98, 0,
            1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0
        }, header.Bytes.ToArray());
    }

    [Fact]
    public void StringsAndIndexOffsetRoundTripWithoutNormalizingTheirBytes()
    {
        var encoded = MfmEmuHeader.Create(1, 2, 4, 10000000, 123456, "mfm --read", "Disque é");
        Assert.True(MfmEmuHeader.TryDecode(encoded.Bytes.Span, out var decoded));
        Assert.Equal("mfm --read", Encoding.UTF8.GetString(decoded.CommandLine.Span));
        Assert.Equal("Disque é", Encoding.UTF8.GetString(decoded.Note.Span));
        Assert.Equal(123456u, decoded.StartNanoseconds);
    }

    [Theory]
    [InlineData(0x02010000u, 36)]
    [InlineData(0x02020000u, 46)]
    [InlineData(0x02020100u, 46)]
    [InlineData(0x02020200u, 50)]
    public void HistoricalVersionsUseTheirOwnHeaderFields(uint version, int length)
    {
        var bytes = Golden();
        Write(bytes, 8, version);
        Write(bytes, 12, (uint)length);
        Assert.True(MfmEmuHeader.TryDecode(bytes.AsSpan(0, length), out var header));
        Assert.Equal(length, header.HeaderLength);
        Assert.Equal(0u, header.StartNanoseconds);
    }

    [Fact]
    public void UnknownMinorHeaderExtensionsAreOwnedAndPreserved()
    {
        var bytes = new byte[57];
        Golden().CopyTo(bytes, 0);
        Write(bytes, 8, 0x02020300);
        Write(bytes, 12, (uint)bytes.Length);
        bytes[56] = 0xa7;
        var expected = bytes.ToArray();
        Assert.True(MfmEmuHeader.TryDecode(bytes, out var header));
        Array.Clear(bytes);
        Assert.Equal(expected, header.Bytes.ToArray());
    }

    [Theory]
    [InlineData(8, 0x01020200u)]
    [InlineData(8, 0x02030000u)]
    [InlineData(8, 0x02020201u)]
    [InlineData(12, 35u)]
    [InlineData(12, 51u)]
    [InlineData(16, 0u)]
    [InlineData(16, 5u)]
    [InlineData(16, 0x80000000u)]
    [InlineData(20, 11u)]
    [InlineData(24, 0u)]
    [InlineData(28, 0u)]
    [InlineData(32, 0u)]
    [InlineData(36, 0u)]
    [InlineData(36, uint.MaxValue)]
    [InlineData(41, 10u)]
    public void InvalidFieldsAreRejectedWithoutReturningAPartialHeader(int offset, uint value)
    {
        var bytes = Golden();
        Write(bytes, offset, value);
        Assert.False(MfmEmuHeader.TryDecode(bytes, out var header));
        Assert.Null(header);
    }

    [Fact]
    public void TruncationMissingTerminatorsAndArithmeticOverflowAreRejected()
    {
        var bytes = Golden();
        for (var length = 0; length < bytes.Length; length++)
            Assert.False(MfmEmuHeader.TryDecode(bytes.AsSpan(0, length), out _));
        bytes[40] = 1;
        Assert.False(MfmEmuHeader.TryDecode(bytes, out _));
        bytes = Golden();
        Write(bytes, 24, int.MaxValue);
        Write(bytes, 28, int.MaxValue);
        Write(bytes, 16, 0x7ffffffc);
        Assert.False(MfmEmuHeader.TryDecode(bytes, out _));
    }

    [Fact]
    public void TrackOffsetsRemainCorrectBeyondTwoGigabytes()
    {
        var header = MfmEmuHeader.Create(2048, 16, 65536, 10000000);
        Assert.Equal(50L + 32767L * 65548, header.TrackOffset(2047, 15));
        Assert.Equal(50L + 32768L * 65548, header.EndMarkerOffset);
        Assert.Throws<ArgumentOutOfRangeException>(() => header.TrackOffset(2048, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => header.TrackOffset(0, -1));
    }

    [Theory]
    [InlineData(0, 0, true, false)]
    [InlineData(305, 3, true, false)]
    [InlineData(-1, -1, true, true)]
    [InlineData(-1, 0, false, false)]
    [InlineData(306, 0, false, false)]
    [InlineData(0, 4, false, false)]
    public void TrackAddressesAndEndMarkerAreValidated(int cylinder, int head, bool valid, bool end)
    {
        var header = MfmEmuHeader.Create(306, 4, 4, 10000000);
        var bytes = new byte[12];
        Write(bytes, 0, 0x12345678);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4), cylinder);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8), head);
        Assert.Equal(valid, header.TryReadTrackAddress(bytes, out var c, out var h, out var eof));
        Assert.Equal(end, eof);
        if (valid) { Assert.Equal(cylinder, c); Assert.Equal(head, h); }
        bytes[0] = 0;
        Assert.False(header.TryReadTrackAddress(bytes, out _, out _, out _));
    }

    [Fact]
    public void ExtendedTrackHeaderMustBePresentExceptForTheEndMarker()
    {
        var bytes = Golden();
        Write(bytes, 20, 20);
        Assert.True(MfmEmuHeader.TryDecode(bytes, out var header));
        var track = new byte[20];
        Write(track, 0, 0x12345678);
        Assert.False(header.TryReadTrackAddress(track.AsSpan(0, 12), out _, out _, out _));
        Assert.True(header.TryReadTrackAddress(track, out _, out _, out _));
        Write(track, 4, uint.MaxValue);
        Write(track, 8, uint.MaxValue);
        Assert.True(header.TryReadTrackAddress(track.AsSpan(0, 12), out _, out _, out var end));
        Assert.True(end);
    }

    private static byte[] Golden() => MfmEmuHeader.Create(306, 4, 65536, 10000000).Bytes.ToArray();
    private static void Write(Span<byte> bytes, int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes[offset..], value);
}

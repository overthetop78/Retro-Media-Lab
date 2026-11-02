using System.Buffers.Binary;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Enums;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.AtomTap;
using RetroMediaLab.MediaEngine.Images.Models.Sequential;
using RetroMediaLab.MediaEngine.Images.Reading.Blocks;
using RetroMediaLab.MediaEngine.Images.Reading.Sources;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuAtomTapTests
{
    [Fact]
    public async Task MultipleProgramsRetainTheirNamesAddressesAndBlockChecksums()
    {
        var data = Enumerable.Range(0, 300).Select(value => (byte)value).ToArray();
        var bytes = File("FIRST", 0x2900, 0xce86, data).Concat(File("SECOND", 0x8000, 0x8020, [0x55])).ToArray();
        var document = AtomTapReader.ReadBytes(new MediaSourceDescriptor("memory.tap", []), bytes);
        Assert.Equal(TapeImageFormatIds.AtomTap, document.FormatId);
        var representation = Assert.IsType<SequentialMediaImageRepresentation>(document.Representation);
        var chunks = new List<byte[]>();
        foreach (var segment in representation.Segments!.Where(segment => segment.Kind == SequentialSegmentKind.DataBlock))
            chunks.Add((await BlockMediaDataReader.ReadRangeAsync(segment.DataRange, default))![3..]);
        Assert.Equal(6, chunks.Count);
        Assert.Equal(new byte[] { 0x2a, 0x2a, 0x2a, 0x2a }, chunks[0][..4]);
        Assert.Equal("FIRST", System.Text.Encoding.ASCII.GetString(chunks[0], 4, 5));
        Assert.Equal(new byte[] { 0xce, 0x86, 0x29, 0x00 }, chunks[0][^4..]);
        Assert.Equal(new byte[] { 0xce, 0x86, 0x2a, 0x00 }, chunks[2][^4..]);
        Assert.Equal("SECOND", System.Text.Encoding.ASCII.GetString(chunks[4], 4, 6));
        Assert.Equal(data, chunks[1][..^1].Concat(chunks[3][..^1]));
        for (var i = 0; i < chunks.Count; i += 2)
        {
            var checksum = chunks[i][4..].Sum(value => (int)value) + chunks[i + 1][..^1].Sum(value => (int)value);
            Assert.Equal(unchecked((byte)checksum), chunks[i + 1][^1]);
        }
    }

    [Fact]
    public async Task GeneratedSignalUsesAtomSerialFramesAndRealCarrierIntervals()
    {
        var document = AtomTapReader.ReadBytes(new MediaSourceDescriptor("memory.tap", []), File("A", 0x2900, 0x2900, [0xa5]));
        var representation = (SequentialMediaImageRepresentation)document.Representation;
        var positions = new Dictionary<long, int>();
        var signal = await CassetteSignalFunctions.UefAsync(representation, positions, default);
        Assert.Equal(48000, signal.SampleRate);
        var dataSegments = representation.Segments!.Where(segment => segment.Kind == SequentialSegmentKind.DataBlock).ToArray();
        Assert.Equal(2, positions.Count);
        var start = positions[dataSegments[1].Position];
        // 1 start + 8 données + 2 stops : 1760 échantillons par octet à 48 kHz/300 bauds.
        var samples = signal.Samples.Slice(start, 1760).ToArray().Select(sample => (int)sample).ToArray();
        var decoded = FskSerialFunctions.Decode(samples, signal.SampleRate, 300, 1200, 2400, 8, 2, 8);
        Assert.Equal(new byte[] { 0xa5 }, decoded.Bytes);
        Assert.All(signal.Samples.Span[..start].ToArray(), sample => Assert.NotEqual((short)0, sample));
    }

    [Fact]
    public void TruncatedAndOtherTapStructuresAreRejectedWithoutPartialInterpretation()
    {
        var bytes = File("A", 0x2900, 0x2900, [1, 2, 3]);
        Assert.True(AtomTapReader.Validate(bytes));
        Assert.False(AtomTapReader.Validate(bytes[..^1]));
        Assert.False(AtomTapReader.Validate(bytes.Concat(new byte[] { 0 }).ToArray()));
        Assert.False(AtomTapReader.Validate([3, 0, 0xff, 0, 0xff]));
        Assert.False(AtomTapReader.Validate(System.Text.Encoding.ASCII.GetBytes("C64-TAPE-RAW\0\0\0\0\0\0\0\0\0\0\0")));
    }

    private static byte[] File(string name, ushort load, ushort execute, byte[] data)
    {
        var result = new byte[22 + data.Length];
        System.Text.Encoding.ASCII.GetBytes(name).CopyTo(result, 0);
        BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(16), load);
        BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(18), execute);
        BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(20), checked((ushort)data.Length));
        data.CopyTo(result, 22);
        return result;
    }
}

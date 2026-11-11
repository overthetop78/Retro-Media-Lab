using System.IO;
using System.Buffers.Binary;
using RetroMediaLab.MediaEngine.Constants;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Images.Formats.Tape.AtomTap;
using RetroMediaLab.MediaEngine.Images.Models.Sequential;
using RetroMediaLab.MediaEngine.Images.Reading.Decoding.Sequential.Acorn;

namespace RetroMediaLab.MediaAudit.Acorn;

public sealed class AtomPcmUefTests
{
    [Theory]
    [InlineData(TapeImageFormatIds.Uef)]
    [InlineData(TapeImageFormatIds.Wav)]
    [InlineData(TapeImageFormatIds.Csw)]
    [InlineData(TapeImageFormatIds.Raw)]
    [InlineData(TapeImageFormatIds.Voc)]
    public async Task PcmRetainsAtomFileWithoutReadingChunkMetadata(string format)
    {
        byte[] payload = [0, 42, 255];
        var bytes = new byte[22 + payload.Length];
        bytes[0] = (byte)'P';
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(16), 0x2900);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(18), 0xc2b2);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(20), (ushort)payload.Length);
        payload.CopyTo(bytes, 22);
        var native = AtomTapReader.ReadBytes(new("memory.tap", []), bytes);
        var signal = await TapeSignalFunctions.UefAsync((SequentialMediaImageRepresentation)native.Representation, new(), CancellationToken.None);
        var pcm = TapePcmFunctions.Document(new("memory", []), format, signal);
        var result = await new AtomTapeDecoder().DecodeAsync(pcm);
        var block = Assert.Single(result.Blocks);
        Assert.True(block.IntegrityValid);
        Assert.Equal(payload, block.Data.ToArray());
        Assert.Equal("P", block.Metadata["fileName"]);
    }
}

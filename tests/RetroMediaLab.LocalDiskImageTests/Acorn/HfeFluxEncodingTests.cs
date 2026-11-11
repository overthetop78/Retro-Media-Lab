using System.IO;
using RetroMediaLab.MediaEngine.Images.Formats.Floppy.Hfe;
using RetroMediaLab.MediaEngine.Images.Models.Flux;
using RetroMediaLab.MediaEngine.Interfaces;

namespace RetroMediaLab.MediaAudit.Acorn;

public sealed class HfeFluxEncodingTests
{
    [Theory]
    [InlineData(1000, -1)]
    [InlineData(2000, -1)]
    [InlineData(4000, -1)]
    [InlineData(1000, 0)]
    [InlineData(2000, 1)]
    [InlineData(4000, 2)]
    public async Task HeaderEncodingDoesNotPreventWritingRepresentableCells(int nanoseconds, int encoding)
    {
        var bits = Enumerable.Range(0, 200000).Select(index => index % 3 != 0).ToArray();
        var track = new ProtectedTrack(0, 0, bits, [new(0, bits.Length, nanoseconds)], [], [], []);
        var metadata = new Dictionary<string, string>();
        if (encoding >= 0) metadata[HfeMetadataKeys.Encoding] = encoding.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var captured = new Capture();
        await new HfeWriter(captured).WriteAsync(new([track], false), metadata, "memory.hfe", CancellationToken.None);
        var decoded = new HfeReader().Read(captured.Bytes);
        Assert.Equal(encoding < 0 ? HfeFormat.UnknownEncoding : encoding, decoded.Encoding);
        Assert.Equal(bits, Assert.Single(decoded.Tracks).Bits);
        Assert.True(captured.Bytes.AsSpan().StartsWith(HfeFormat.Signature));
    }

    private sealed class Capture : IAtomicImageFileWriter
    {
        internal byte[] Bytes = [];
        public async Task WriteAsync(string path, Func<Stream, CancellationToken, Task> write, CancellationToken cancellationToken = default)
        {
            using var stream = new MemoryStream();
            await write(stream, cancellationToken);
            Bytes = stream.ToArray();
        }
    }
}

using System.Reflection;
using BbcVideo = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services.Video;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants.VideoConstants;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuVideoRenderTests
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    private const int RandomSeed = 6845;

    [Theory]
    [InlineData(200, false, 0, false)]
    [InlineData(200, true, 0, false)]
    [InlineData(200, true, 1, true)]
    [InlineData(256, false, 0, false)]
    [InlineData(256, true, 0, true)]
    [InlineData(256, true, 1, false)]
    public void RenderKeepsScalarFieldPixelsAndOwnedSnapshots(int height, bool interlaced, int parity, bool history)
    {
        var video = new BbcVideo(new byte[32768], ReadOnlySpan<byte>.Empty, height);
        var completed = ArrayField(video, "_completed");
        var previous = ArrayField(video, "_previousField");
        var colours = (uint[])typeof(BbcVideo).GetField("ScanoutPixels", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        var random = new Random(RandomSeed);
        for (var word = 0; word < completed.Length; word++)
        {
            completed[word] = word % 5 == 0 ? 0 : (uint)random.NextInt64((long)uint.MaxValue + 1);
            previous[word] = word % 7 == 0 ? 0 : (uint)random.NextInt64((long)uint.MaxValue + 1);
        }
        try
        {
            Check();
            completed[C.ScanoutWordsPerLine] = 0;
            previous[^1] = uint.MaxValue;
            Check();
            history = !history;
            parity ^= C.FieldParityMask;
            Check();
            interlaced = !interlaced;
            Check();
            Array.Clear(completed);
            Array.Clear(previous);
            Check();
            video.Reset();
            Assert.All(video.Render(), value => Assert.Equal(0, value));
        }
        finally { video.Reset(); }

        void Check()
        {
            Field("_completedInterlaced").SetValue(video, interlaced);
            Field("_completedParity").SetValue(video, parity);
            Field("_previousFieldValid").SetValue(video, history);
            var outputHeight = height;
            var expected = new byte[C.Width * outputHeight * C.PackedPixelBytes];
            for (var row = 0; row < outputHeight; row++)
            {
                var field = row & C.FieldParityMask;

                var source = completed;
                var sourceRow = row;
                for (var pixel = 0; pixel < C.Width; pixel++)
                {
                    var word = source[sourceRow * C.ScanoutWordsPerLine + pixel / C.CpuMasterDots];
                    var index = (word >> ((pixel % C.CpuMasterDots) * C.ScanoutPixelBits)) & C.ScanoutPixelMask;
                    Assert.True(BitConverter.TryWriteBytes(expected.AsSpan((row * C.Width + pixel) * C.PackedPixelBytes), colours[index]));
                }
            }
            var sourceCopy = completed.ToArray();
            var historyCopy = previous.ToArray();
            var frame = video.Frame;
            var first = video.Render();
            Assert.Equal(expected, first);
            var second = video.Render();
            Assert.NotSame(first, second);
            Assert.Equal(expected, second);
            first[0] ^= byte.MaxValue;
            Assert.Equal(expected, video.Render());
            Assert.Equal(frame, video.Frame);
            Assert.Equal(sourceCopy, completed);
            Assert.Equal(historyCopy, previous);
        }
    }

    private static FieldInfo Field(string name) => typeof(BbcVideo).GetField(name, Fields)!;
    private static uint[] ArrayField(BbcVideo video, string name) => (uint[])Field(name).GetValue(video)!;
}

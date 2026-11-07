using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Functions;
using RetroMediaLab.Emulation.Services;
using C = RetroMediaLab.Emulation.Constants.EmulationHostProtocolConstants;

namespace RetroMediaLab.Tests.Emulation;

public sealed class VideoFieldProtocolTests
{
    [Theory]
    [InlineData(EmulationVideoScanMode.Progressive)]
    [InlineData(EmulationVideoScanMode.InterlacedEvenField)]
    [InlineData(EmulationVideoScanMode.InterlacedOddField)]
    public void NativeFieldMetadataSurvivesEachExistingTransport(EmulationVideoScanMode mode)
    {
        var frame = new VideoFrame(Enumerable.Range(0, 24).Select(value => (byte)value).ToArray(),
            3, 3, 8, EmulationPixelFormat.Rgb565, 4f / 3f, 17, TimeSpan.FromTicks(123456)) { ScanMode = mode };
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        EmulationHostProtocolFunctions.WriteFrame(writer, frame);
        stream.Position = 0;
        Check(EmulationHostProtocolFunctions.ReadFrame(reader, "test")!);
        stream.SetLength(0);
        using var memory = MemoryMappedFile.CreateNew(null, C.VideoMapCapacity);
        using var view = memory.CreateViewAccessor();
        EmulationHostProtocolFunctions.WriteSharedFrame(writer, frame, view, "test");
        stream.Position = 0;
        Check(EmulationHostProtocolFunctions.ReadSharedFrame(reader, view, "test")!);
        stream.SetLength(0);
        using var resizable = new EmulationSharedVideoWriter("RmlVideoFieldTest", _ => 256, "-", "N");
        MemoryMappedFile? receivedMemory = null;
        MemoryMappedViewAccessor? receivedView = null;
        string? name = null;
        try
        {
            EmulationHostProtocolFunctions.WriteResizableSharedFrame(writer, frame, resizable);
            stream.Position = 0;
            Check(EmulationHostProtocolFunctions.ReadResizableSharedFrame(reader, ref receivedMemory, ref receivedView, ref name, 256)!);
        }
        finally
        {
            try { receivedView?.Dispose(); }
            finally { receivedMemory?.Dispose(); }
        }

        void Check(VideoFrame received)
        {
            Assert.Equal(frame, received with { Pixels = frame.Pixels });
            Assert.Equal(frame.Pixels.ToArray(), received.Pixels.ToArray());
            Assert.Equal(mode == EmulationVideoScanMode.Progressive ? 3 : 6, received.ImageHeight);
        }
    }

    [Fact]
    public void InvalidScanModeIsRejectedAtTheTransportBoundary()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        EmulationHostProtocolFunctions.WriteFrame(writer,
            new(new byte[4], 1, 1, 4, EmulationPixelFormat.Xrgb8888, 1, 1, TimeSpan.Zero) { ScanMode = (EmulationVideoScanMode)42 });
        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        Assert.Throws<InvalidDataException>(() => EmulationHostProtocolFunctions.ReadFrame(reader, "test"));
    }
}

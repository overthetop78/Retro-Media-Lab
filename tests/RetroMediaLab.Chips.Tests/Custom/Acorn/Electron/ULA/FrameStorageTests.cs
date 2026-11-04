using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

namespace RetroMediaLab.Chips.Tests.Custom.Acorn.Electron.ULA;

public sealed class FrameStorageTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void EveryPhysicalColorIsStoredAsBgrWithZeroReservedByteAndBlackHiddenRasters(int mode)
    {
        var video = new ElectronVideo(address => (byte)(address ^ (address >> 8)));
        for (var color = 0; color < 8; color++)
        {
            video.Reset();
            video.SetMode((byte)(mode << 3));
            var red = (color & 1) != 0;
            var green = (color & 2) != 0;
            var blue = (color & 4) != 0;
            var even = (byte)~((blue ? 0xf0 : 0) | (green ? 0x0c : 0));
            var odd = (byte)~((red ? 0x0f : 0) | (green ? 0x30 : 0));
            for (var pair = 0; pair < 8; pair += 2)
            {
                video.WritePalette(pair, even);
                video.WritePalette(pair + 1, odd);
            }
            while (video.Frame == 0) video.Tick();
            var expected = new byte[640 * 256 * 4];
            for (var line = 0; line < 256; line++)
            {
                if ((mode == 3 || mode == 6) && (line >= 250 || line % 10 >= 8)) continue;
                for (var pixel = 0; pixel < 640; pixel++)
                {
                    var offset = (line * 640 + pixel) * 4;
                    expected[offset] = blue ? (byte)255 : (byte)0;
                    expected[offset + 1] = green ? (byte)255 : (byte)0;
                    expected[offset + 2] = red ? (byte)255 : (byte)0;
                }
            }
            Assert.Equal(expected, video.CompletedFrame);
        }
    }
}

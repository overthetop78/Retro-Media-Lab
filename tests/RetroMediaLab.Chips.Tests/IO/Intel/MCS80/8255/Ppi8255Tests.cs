using RetroMediaLab.Chips.IO.Intel.MCS80.Chip8255.Services;
namespace RetroMediaLab.Chips.Tests.IO.Intel.MCS80.Chip8255;
public sealed partial class Ppi8255Tests
{
    [Fact]
    public void PpiDirectionsAndBitCommandsKeepInputsSeparateFromOutputLatches()
    {
        var ppi = new Ppi8255
        {
            ReadPortA = () => 0x55,
            ReadPortB = () => 0xaa,
            ReadPortC = () => 0xf0
        };
        Assert.Equal(0x55, ppi.Read(0));
        ppi.Write(3, 0x8a);
        ppi.Write(0, 0x12);
        ppi.Write(1, 0x34);
        ppi.Write(2, 0x05);
        Assert.Equal(0x12, ppi.Read(0));
        Assert.Equal(0xaa, ppi.Read(1));
        Assert.Equal(0xf5, ppi.Read(2));
        ppi.Write(3, 0x03);
        Assert.Equal(0xf7, ppi.Read(2));
        ppi.Write(3, 0x00);
        Assert.Equal(0xf6, ppi.Read(2));
        ppi.Write(3, 0x80);
        Assert.Equal(0, ppi.Read(0));
        Assert.Equal(0, ppi.Read(2));
    }
}

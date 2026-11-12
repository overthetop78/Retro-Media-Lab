using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.TexasInstruments.TMS99xx.TMS9918A;

public sealed class BackgroundPipelineTests
{
    [Fact]
    public void FourModeAddressesAndPixelsUseOnlyDeliveredBytesAndResumeTheirShifter()
    {
        var registers = new Tms9918Registers(new(0, 0x40, 6, 0x80, 1, 0, 0, 0xe3));
        var pipeline = new Tms9918BackgroundPipeline(default);
        Assert.Equal(new Tms9918FetchAddress(0x1aff, true), Tms9918BackgroundAddresses.Name(registers, 191, 31));
        Assert.Equal(new Tms9918FetchAddress(0x0915, true), Tms9918BackgroundAddresses.Pattern(registers, 5, 0x22, true));
        Assert.Equal(new Tms9918FetchAddress(0x2004, true), Tms9918BackgroundAddresses.Colour(registers, 5, 0x22, true));
        pipeline.LoadName(0x22, true); pipeline.LoadPattern(0xa1, true); pipeline.LoadColour(0x50, true);
        pipeline.BeginCell();
        Assert.Equal(new Tms9918Pixel(5, true), pipeline.ShiftPixel(registers));
        Assert.Equal(new Tms9918Pixel(3, true), pipeline.ShiftPixel(registers));
        pipeline = new(pipeline.Capture());
        Assert.Equal(new byte[] { 5, 3, 3, 3, 3, 5 }, Enumerable.Range(0, 6)
            .Select(_ => pipeline.ShiftPixel(registers).Colour));
        Assert.False(pipeline.ShiftPixel(registers).Known);

        registers.Write(0, 2); registers.Write(3, 0xff); registers.Write(4, 7);
        Assert.Equal(new Tms9918FetchAddress(0x3155, true), Tms9918BackgroundAddresses.Pattern(registers, 133, 0x2a, true));
        Assert.Equal(new Tms9918FetchAddress(0x3155, true), Tms9918BackgroundAddresses.Colour(registers, 133, 0x2a, true));
        registers.Write(4, 4);
        Assert.Equal(new Tms9918FetchAddress(0x2155, true), Tms9918BackgroundAddresses.Pattern(registers, 133, 0x2a, true));
        registers.Write(0, 0); registers.Write(1, 0x48); registers.Write(4, 1);
        Assert.Equal(new Tms9918FetchAddress(0x0915, true), Tms9918BackgroundAddresses.Pattern(registers, 20, 0x22, true));
        Assert.False(Tms9918BackgroundAddresses.Colour(registers, 20, 0x22, true).Qualified);
        pipeline.LoadPattern(0x27, true); pipeline.BeginCell();
        Assert.Equal(new byte[] { 2, 2, 2, 2, 7, 7, 7, 7 }, Enumerable.Range(0, 8)
            .Select(_ => pipeline.ShiftPixel(registers).Colour));

        registers.Write(1, 0x50);
        Assert.Equal(new Tms9918FetchAddress(0x1bbf, true), Tms9918BackgroundAddresses.Name(registers, 191, 39));
        pipeline.LoadPattern(0xa3, true); pipeline.BeginCell();
        Assert.Equal(new byte[] { 14, 3, 14, 3, 3, 3 }, Enumerable.Range(0, 6)
            .Select(_ => pipeline.ShiftPixel(registers).Colour));
        Assert.False(pipeline.ShiftPixel(registers).Known); // les deux LSB ne sont pas affichés
        pipeline.LoadPattern(0xa3, false); pipeline.BeginCell();
        Assert.False(pipeline.ShiftPixel(registers).Known);
        Assert.False(Tms9918BackgroundAddresses.Pattern(registers, 0, 0, false).Qualified);
    }

    [Fact]
    public void CombinedModeAddressesAndPixelsDistinguishTextMulticolorAndFixedBands()
    {
        var registers = new Tms9918Registers(new(2, 0xd0, 6, 0, 3, 0, 0, 0xe3));
        Assert.Equal(0x1bbf, Tms9918BackgroundAddresses.Name(registers, 191, 39).Address);
        Assert.Equal(new Tms9918FetchAddress(0x1115, true), Tms9918BackgroundAddresses.Pattern(registers, 133, 0x22, true));
        Assert.False(Tms9918BackgroundAddresses.Colour(registers, 133, 0x22, true).Qualified);
        var pipeline = new Tms9918BackgroundPipeline(default);
        pipeline.LoadPattern(0xa5, true); pipeline.BeginCell();
        Assert.Equal(new byte[] { 14, 3, 14, 3, 3, 14 }, Enumerable.Range(0, 6)
            .Select(_ => pipeline.ShiftPixel(registers).Colour));
        Assert.False(pipeline.ShiftPixel(registers).Known);

        registers.Write(1, 0xc8);
        Assert.Equal(new Tms9918FetchAddress(0x1111, true), Tms9918BackgroundAddresses.Pattern(registers, 133, 0x22, true));
        pipeline.LoadPattern(0x25, true); pipeline.BeginCell();
        Assert.Equal(new byte[] { 2, 2, 2, 2, 5, 5, 5, 5 }, Enumerable.Range(0, 8)
            .Select(_ => pipeline.ShiftPixel(registers).Colour));
        foreach (byte r0 in new byte[] { 0, 2 })
        {
            registers.Write(0, r0); registers.Write(1, 0xd8);
            pipeline.LoadPattern(0, false); pipeline.LoadColour(0, false); pipeline.BeginCell();
            var pixels = Enumerable.Range(0, 6).Select(_ => pipeline.ShiftPixel(registers)).ToArray();
            Assert.All(pixels, pixel => Assert.True(pixel.Known));
            Assert.Equal(new byte[] { 14, 14, 14, 14, 3, 3 }, pixels.Select(pixel => pixel.Colour));
        }
    }

    [Fact]
    public void GraphicsIIMasksShareOrSeparateThirdsAndPreserveAlreadyFetchedPixels()
    {
        var registers = new Tms9918Registers(new(2, 0xc0, 14, 0xff, 0, 0x76, 3, 15));
        int[][] banks = [[0, 0, 0], [0, 0x800, 0], [0, 0, 0x1000], [0, 0x800, 0x1000]];
        for (int half = 0; half < 2; half++)
        for (int mask = 0; mask < 4; mask++)
        for (int third = 0; third < 3; third++)
        {
            registers.Write(4, (byte)(half * 4 + mask));
            Assert.Equal(new Tms9918FetchAddress((ushort)(half * 0x2000 + banks[mask][third] + 0x113), true),
                Tms9918BackgroundAddresses.Pattern(registers, third * 64 + 3, 0x22, true));
        }
        // Cas TI 1984 §8.4.2 et autres masques du modèle d'adressage retenu.
        (byte R3, byte R4, int Line, byte Name, ushort Pattern, ushort Colour)[] vectors =
        [
            (0x9f, 0, 131, 0x22, 0x0113, 0x2113),
            (0xff, 1, 67, 0x22, 0x0913, 0x2913),
            (0x5f, 6, 131, 0x22, 0x3113, 0x1113),
            (0, 3, 131, 0xff, 0x103b, 0x003b),
            (0x80, 7, 67, 0xff, 0x283b, 0x203b),
            (2, 3, 67, 0xff, 0x08bb, 0x00bb),
            (4, 3, 131, 0xff, 0x113b, 0x013b),
            (0x1f, 4, 191, 0xff, 0x27ff, 0x07ff),
        ];
        foreach (var vector in vectors)
        {
            registers.Write(3, vector.R3); registers.Write(4, vector.R4);
            var restored = new Tms9918Registers(registers.Capture());
            foreach (var source in new[] { registers, restored })
            {
                Assert.Equal(new Tms9918FetchAddress(vector.Pattern, true),
                    Tms9918BackgroundAddresses.Pattern(source, vector.Line, vector.Name, true));
                Assert.Equal(new Tms9918FetchAddress(vector.Colour, true),
                    Tms9918BackgroundAddresses.Colour(source, vector.Line, vector.Name, true));
                Assert.False(Tms9918BackgroundAddresses.Pattern(source, vector.Line, vector.Name, false).Qualified);
                Assert.False(Tms9918BackgroundAddresses.Colour(source, vector.Line, vector.Name, false).Qualified);
            }
        }
        var pipeline = new Tms9918BackgroundPipeline(default);
        pipeline.LoadPattern(0x80, true); pipeline.LoadColour(0x23, true); pipeline.BeginCell();
        registers.Write(3, 0); registers.Write(4, 0);
        Assert.Equal(new Tms9918Pixel(2, true), pipeline.ShiftPixel(registers));
        Assert.Equal(new Tms9918Pixel(3, true), pipeline.ShiftPixel(registers));
    }
}

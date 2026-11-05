using System.Buffers.Binary;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.MfmEmu;

namespace RetroMediaLab.Tests.Media;

public sealed class MfmEmuTrackSignalTests
{
    [Fact]
    public void LittleEndianWordsAreReadFromBit31AndIndexPulseIsAtTheEnd()
    {
        var track = MfmEmuTrackSignal.Read(Words(0x80000001, 0x40000000), 10000000, 0, 6400);
        Assert.Equal(new uint[] { 3100, 200, 3100 }, track.Revolutions[0].FluxIntervalsNanoseconds);
        Assert.Equal(6400u, track.Revolutions[0].IndexTimeNanoseconds);
    }

    [Fact]
    public void CaptureStartRotatesTheSignalAndPaddingDoesNotAddARevolution()
    {
        var shifted = MfmEmuTrackSignal.Read(Words(0x80000001), 10000000, 200, 3200);
        Assert.Equal(new uint[] { 100, 100 }, shifted.Revolutions[0].FluxIntervalsNanoseconds);
        var padded = MfmEmuTrackSignal.Read(Words(uint.MaxValue), 10000000, 0, 1600);
        Assert.Equal(Enumerable.Repeat(100u, 16), padded.Revolutions[0].FluxIntervalsNanoseconds);
    }

    [Theory]
    [InlineData(10000000u, 0u, 6400u)]
    [InlineData(10000000u, 173u, 6400u)]
    [InlineData(8600000u, 0u, 7442u)]
    [InlineData(8600000u, 173u, 7442u)]
    [InlineData(15000000u, 0u, 4267u)]
    [InlineData(15000000u, 173u, 4267u)]
    public void ReadingThenWritingOneRevolutionPreservesAllStoredCells(uint rate, uint phase, uint period)
    {
        var bytes = Words(0x89abcdef, 0x01234567);
        var revolution = MfmEmuTrackSignal.Read(bytes, rate, phase, period).Revolutions[0];
        uint time = 0;
        var offsets = revolution.FluxIntervalsNanoseconds.Select(interval => time += interval).ToArray();
        var written = MfmEmuTrackSignal.WriteWindow(bytes, rate, phase, period, 0, period, offsets, true);
        Assert.Equal(bytes, written);
    }

    [Fact]
    public void PartialWindowPreservesOtherCellsAndContainerPadding()
    {
        var bytes = Words(uint.MaxValue);
        var written = MfmEmuTrackSignal.WriteWindow(bytes, 10000000, 0, 1600, 100, 200, [], false);
        Assert.Equal(Words(0x9fffffff), written);
        Assert.Equal(Words(uint.MaxValue), bytes);
    }

    [Fact]
    public void WindowAcrossIndexKeepsOnlyItsEmittedPulses()
    {
        var written = MfmEmuTrackSignal.WriteWindow(Words(uint.MaxValue), 10000000, 0, 3200,
            3000, 400, new uint[] { 100, 300 }, false);
        Assert.Equal(Words(0x7ffffffd), written);
    }

    [Fact]
    public void LaterPassesEraseEarlierPulsesEvenWithoutAReplacement()
    {
        var written = MfmEmuTrackSignal.WriteWindow(Words(0), 10000000, 0, 3200,
            0, 6500, new uint[] { 50, 3250 }, false);
        Assert.Equal(Words(0), written);
        var last = MfmEmuTrackSignal.WriteWindow(Words(0), 10000000, 0, 3200,
            0, 6500, new uint[] { 0, 3200, 6400 }, false);
        Assert.Equal(Words(0x80000000), last);
    }

    [Fact]
    public void EndingInstantCanBeIncludedAndSubcellPulseSamplesAtTheNextCell()
    {
        var included = MfmEmuTrackSignal.WriteWindow(Words(uint.MaxValue), 10000000, 0, 3200, 100, 200, [], true);
        Assert.Equal(Words(0x8fffffff), included);
        var quantized = MfmEmuTrackSignal.WriteWindow(Words(0), 10000000, 0, 3200, 100, 200, new uint[] { 1 }, false);
        Assert.Equal(Words(0x20000000), quantized);
        Assert.Throws<ArgumentOutOfRangeException>(() => MfmEmuTrackSignal.WriteWindow(Words(0), 10000000,
            0, 3200, 100, 200, new uint[] { 200 }, false));
    }

    [Fact]
    public void InvalidWindowsAndUncapturedCellsCannotModifyTheInput()
    {
        var bytes = Words(0x89abcdef);
        Assert.Throws<ArgumentOutOfRangeException>(() => MfmEmuTrackSignal.WriteWindow(bytes, 10000000,
            0, 3200, 0, 1000, new uint[] { 500, 400 }, false));
        Assert.Throws<NotSupportedException>(() => MfmEmuTrackSignal.WriteWindow(bytes, 10000000,
            0, 6400, 4000, 500, new uint[] { 100 }, false));
        Assert.Equal(Words(0x89abcdef), bytes);
    }

    [Fact]
    public void EmptySignalAndZeroDurationDoNotInventPulses()
    {
        Assert.Empty(MfmEmuTrackSignal.Read(Words(0), 10000000, 0, 3200).Revolutions[0].FluxIntervalsNanoseconds);
        Assert.Equal(Words(0x89abcdef), MfmEmuTrackSignal.WriteWindow(Words(0x89abcdef), 10000000, 0, 3200, 0, 0, [], false));
        Assert.Throws<ArgumentOutOfRangeException>(() => MfmEmuTrackSignal.Read(Words(0), 0, 0, 3200));
        Assert.Throws<ArgumentOutOfRangeException>(() => MfmEmuTrackSignal.Read(Words(0), 10000000, 0, 0));
        Assert.Throws<NotSupportedException>(() => MfmEmuTrackSignal.Read(Words(0), 1000000001, 0, 3200));
        Assert.Throws<ArgumentException>(() => MfmEmuTrackSignal.Read(new byte[3], 10000000, 0, 3200));
    }

    private static byte[] Words(params uint[] words)
    {
        var bytes = new byte[words.Length * sizeof(uint)];
        for (var index = 0; index < words.Length; index++)
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(index * sizeof(uint)), words[index]);
        return bytes;
    }
}

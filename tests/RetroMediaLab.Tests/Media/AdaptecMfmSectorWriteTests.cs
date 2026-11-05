using System.IO;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec;
using RetroMediaLab.MediaEngine.Images.Models.Flux;

namespace RetroMediaLab.Tests.Media;

public sealed class AdaptecMfmSectorWriteTests
{
    private const uint Period = 16_666_667;
    private const uint Cell = 100;

    [Fact]
    public void WriteRepairsDataAndPreservesIdsOtherSectorsAndUntraversedFlux()
    {
        var broken = new MediaDiskSector(0, new byte[256], 0, 0, 0) { DataIntegrityValid = false };
        var untouched = new MediaDiskSector(1, Enumerable.Repeat((byte)0x37, 256).ToArray(), 0, 0, 1);
        var original = AdaptecMfmTrackCodec.Encode([broken, untouched], false, Period).Revolutions[0];
        var before = AdaptecMfmTrackCodec.Decode(original, 0, 0, 256);
        Assert.False(before[0].Sector.DataIntegrityValid);
        var payload = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();
        var window = AdaptecMfmTrackCodec.CreateSectorWrite(original, 0, 0, 0, payload);
        var rewritten = MediaFluxTrackDataFunctions.ReplaceWindow(original, window.StartNanoseconds,
            window.DurationNanoseconds, window.Transitions);
        var after = AdaptecMfmTrackCodec.Decode(rewritten, 0, 0, 256);
        Assert.Equal(2, after.Count);
        Assert.Equal(payload, after[0].Sector.Data.ToArray());
        Assert.True(after[0].Sector.DataIntegrityValid);
        Assert.Equal(before[0].Id.ToArray(), after[0].Id.ToArray());
        Assert.Equal(before[1].Id.ToArray(), after[1].Id.ToArray());
        Assert.Equal(untouched.Data.ToArray(), after[1].Sector.Data.ToArray());
        Assert.Equal(Outside(original, window), Outside(rewritten, window));
    }

    [Fact]
    public void ChangingTheLastDataBitRecomputesTheFollowingMfmClock()
    {
        var original = AdaptecMfmTrackCodec.Encode([new(0, new byte[256], 0, 0, 0)], false, Period).Revolutions[0];
        var oldBits = Reconstruct(original);
        byte[] payload = new byte[256];
        AdaptecMfmWriteWindow window = default;
        bool[]? newBits = null;
        var clockOffset = 0;
        var found = false;
        for (var value = 1; value <= byte.MaxValue; value++)
        {
            payload[0] = (byte)value;
            window = AdaptecMfmTrackCodec.CreateSectorWrite(original, 0, 0, 0, payload);
            clockOffset = checked((int)((window.StartNanoseconds + window.DurationNanoseconds) / Cell) - 2);
            var rewritten = MediaFluxTrackDataFunctions.ReplaceWindow(original, window.StartNanoseconds,
                window.DurationNanoseconds, window.Transitions);
            newBits = Reconstruct(rewritten);
            if (oldBits[clockOffset - 1] == newBits[clockOffset - 1]) continue;
            found = true;
            break;
        }
        Assert.True(found);
        Assert.NotNull(newBits);
        Assert.Equal(!newBits[clockOffset - 1] && !newBits[clockOffset + 1], newBits[clockOffset]);
    }

    [Fact]
    public void DuplicateIdentityMissingDataAndWrongSizesAreRejected()
    {
        var sector = new MediaDiskSector(0, new byte[256], 0, 0, 0);
        var duplicates = AdaptecMfmTrackCodec.Encode([sector, sector], false, Period).Revolutions[0];
        Assert.Throws<InvalidDataException>(() => AdaptecMfmTrackCodec.CreateSectorWrite(duplicates, 0, 0, 0, new byte[256]));
        var blank = AdaptecMfmTrackCodec.Encode([sector with { Data = ReadOnlyMemory<byte>.Empty }], false,
            Period, 256).Revolutions[0];
        Assert.Throws<InvalidDataException>(() => AdaptecMfmTrackCodec.CreateSectorWrite(blank, 0, 0, 0, new byte[256]));
        Assert.Throws<ArgumentOutOfRangeException>(() => AdaptecMfmTrackCodec.CreateSectorWrite(duplicates, 0, 0, 0, new byte[128]));
    }

    private static bool[] Reconstruct(MediaFluxRevolutionData revolution) => FluxTransitionDecoder.Reconstruct(
        revolution.FluxIntervalsNanoseconds, Cell, checked((int)(Period / Cell)), adaptClock: false).Bits.ToArray();

    private static uint[] Outside(MediaFluxRevolutionData revolution, AdaptecMfmWriteWindow window)
    {
        uint time = 0;
        return revolution.FluxIntervalsNanoseconds.Select(interval => time += interval)
            .Where(position => (position % Period + Period - window.StartNanoseconds) % Period >= window.DurationNanoseconds)
            .ToArray();
    }
}

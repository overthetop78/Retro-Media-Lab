using System.IO;
using RetroMediaLab.MediaEngine.Contracts;
using RetroMediaLab.MediaEngine.Functions;
using RetroMediaLab.MediaEngine.Images.Formats.HardDisk.Adaptec;

namespace RetroMediaLab.Tests.Media;

public sealed class AdaptecMfmSectorReaderTests
{
    [Fact]
    public void CompleteThirtyThreeSectorTrackRetainsEveryIdAndDataCheck()
    {
        var sectors = Enumerable.Range(0, 33).Select(address => new MediaDiskSector(address,
            Enumerable.Repeat((byte)0xe5, 256).ToArray(), 0, 0, address)).ToArray();
        Span<byte> flags = stackalloc byte[9];
        new AdaptecMfmGeometry(256, 1, 1, 33).WriteInitialParameters(flags, 128, 0, 0);
        var track = AdaptecMfmTrackCodec.Encode(sectors, false, Period, 256, flags);
        var decoded = AdaptecMfmTrackCodec.Decode(track.Revolutions[0], 0, 0, 256);
        Assert.Equal(33, decoded.Count);
        foreach (var field in decoded)
        {
            Assert.True(field.Sector.HeaderIntegrityValid, $"ID {field.Sector.Number}");
            Assert.True(field.Sector.DataIntegrityValid, $"DATA {field.Sector.Number}");
            Assert.Equal(sectors[(int)field.Sector.Number].Data.ToArray(), field.Sector.Data.ToArray());
        }
    }

    private const int SectorSize = 1024;
    private const int SectorCount = 9;
    private const uint Period = 16_666_667;

    [Fact]
    public void AddressMarkShapedTransitionsWithoutSyncPreambleDoNotHideTheFollowingData()
    {
        var payload = Enumerable.Repeat((byte)0xe5, 256).ToArray();
        var original = AdaptecMfmTrackCodec.Encode([new(0, payload, 0, 0, 0)], false, Period).Revolutions[0];
        // Two words in the gap immediately after ID, without a 16-zero phase-up preamble.
        ushort[] words = [0x4489, 0x2aaa];
        var pulses = new List<uint>();
        for (var bit = 0; bit < 32; bit++)
            if ((words[bit / 16] & (1 << (15 - bit % 16))) != 0) pulses.Add((uint)bit * 100);
        var start = (9u + 12u + 10u) * 16u * 100u + 100u;
        var changed = MediaFluxTrackDataFunctions.ReplaceWindow(original, start, 32 * 100, pulses);
        var field = Assert.Single(AdaptecMfmTrackCodec.Decode(changed, 0, 0, 256));
        Assert.True(field.Sector.HeaderIntegrityValid);
        Assert.True(field.Sector.DataIntegrityValid);
        Assert.Equal(payload, field.Sector.Data.ToArray());
    }

    [Fact]
    public async Task PartialReadsCrossSectorBoundariesInLogicalOrder()
    {
        var reader = Reader();
        Assert.Equal(SectorSize, reader.SectorSize);
        Assert.Equal(SectorCount, reader.SectorCount);
        var bytes = new byte[50];
        await reader.ReadExactlyAsync(SectorSize - 20, bytes, CancellationToken.None);
        Assert.All(bytes[..20], value => Assert.Equal(0, value));
        Assert.All(bytes[20..], value => Assert.Equal(1, value));
        Assert.Equal(7, reader.FindSector(0, 0, 7));
        Assert.Equal(new long[] { 0, 2, 4, 6, 8, 1, 3, 5, 7 }, reader.GetTrackSectors(0, 0));
    }

    [Fact]
    public async Task FileSystemReadsRejectBadEccWhileRawSectorReadsRetainIt()
    {
        var reader = Reader(corruptLogicalSector: 1);
        var raw = await reader.ReadSectorAsync(1);
        Assert.False(raw.DataIntegrityValid);
        Assert.Equal(SectorSize, raw.Data.Length);
        await Assert.ThrowsAsync<InvalidDataException>(async () =>
            await reader.ReadExactlyAsync(SectorSize, new byte[20], CancellationToken.None));
    }

    [Fact]
    public async Task BoundsAndCancellationAreCheckedWithoutInventingData()
    {
        var reader = Reader();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await reader.ReadExactlyAsync(reader.Capacity - 1, new byte[2], CancellationToken.None));
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await reader.ReadExactlyAsync(0, new byte[1], new CancellationToken(true)));
        await reader.ReadExactlyAsync(reader.Capacity, Memory<byte>.Empty, CancellationToken.None);
    }

    private static AdaptecMfmSectorReader Reader(int corruptLogicalSector = -1)
    {
        int[] addresses = [0, 2, 4, 6, 8, 1, 3, 5, 7];
        var sectors = addresses.Select(address => new MediaDiskSector(address,
            Enumerable.Repeat((byte)address, SectorSize).ToArray(), 0, 0, address)
            { DataIntegrityValid = address != corruptLogicalSector }).ToArray();
        var track = AdaptecMfmTrackCodec.Encode(sectors, false, Period, parameterFlags: [0, 8, 0, 0, 0, 0, 0, 0, 0]);
        return new((_, _) => ValueTask.FromResult(track), 1, 1);
    }
}

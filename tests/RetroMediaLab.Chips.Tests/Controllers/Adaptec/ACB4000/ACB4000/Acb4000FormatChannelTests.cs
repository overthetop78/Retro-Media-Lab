using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed class Acb4000FormatChannelTests
{
    [Fact]
    public void FormatGatePublishesIndexOpeningTailClosingAndResetWithoutStableNotifications()
    {
        var channel = new Acb4000FormatChannel();
        var levels = new List<bool>();
        channel.WriteGateChanged += gate => { Assert.Equal(channel.WriteGate, gate); levels.Add(gate); };
        channel.BeginTrack([0, 0x41], 0, 256, false, 0x6c);
        for (var i = 0; i < 100; i++) channel.ClockCell(false);
        Assert.Empty(levels);
        channel.ClockCell(true);
        for (var i = 0; i < 20000; i++) channel.ClockCell(false);
        Assert.Equal(new[] { true }, levels);
        channel.ClockCell(true);
        Assert.True(channel.Complete);
        Assert.Equal(new[] { true, false }, levels);
        channel.Reset(); channel.ClockCell(false);
        Assert.Equal(2, levels.Count);
        channel.BeginTrack([0, 0x41], 0, 256, false, 0x6c);
        channel.ClockCell(true); channel.Reset();
        Assert.Equal(new[] { true, false }, levels.Skip(2));
    }

    [Fact]
    public void FirstNinePhysicalIdsCarryParametersThenResumeTheDefectCounter()
    {
        byte[] flags = [0, 2, 0x31, 0x22, 0x16, 1, 0x16, 1, 0];
        byte[] map = [0, 1, 2, 3, 4, 5, 6, 7, 8, 0x49];
        var channel = new Acb4000FormatChannel();
        channel.BeginTrack(map, 0, 256, false, 0x6c, physicalParameters: flags);
        // BeginTrack owns a copy; subsequent changes in the caller cannot change an ID.
        Array.Fill(flags, (byte)0x7f);
        var reader = new Acb4000ReadChannel(256);
        var ids = new List<Acb4000IdField>();
        foreach (var word in ReadWords(channel, 10 * 310))
            for (var bit = 15; bit >= 0; bit--)
                if (reader.Feed((word & (1 << bit)) != 0) == Acb4000FieldKind.Id)
                {
                    Assert.True(Acb4000FieldCodec.TryReadId(reader.Field, false, out var id, out _));
                    ids.Add(id);
                }
        Assert.Equal(new byte[] { 0, 2, 0x31, 0x22, 0x16, 1, 0x16, 1, 0, 0x80 },
            ids.Select(id => id.Flags).ToArray());
        Assert.Equal(Enumerable.Range(0, 10).Select(value => (uint)value), ids.Select(id => id.LogicalAddress));
        channel.Reset();
        channel.BeginTrack(map, 10, 256, false, 0x6c);
        var nextTrack = ReadWords(channel, 31).Select(DataBits).ToArray();
        Assert.Equal(0, nextTrack[26]);
    }

    [Fact]
    public void FirstSectorMatchesTheFirmwareByteSequenceAndIndependentEccVectors()
    {
        var channel = new Acb4000FormatChannel();
        channel.BeginTrack([0, 0x41], 0, 256, false, 0x6c);
        var words = ReadWords(channel, 310);
        var expected = Enumerable.Repeat((byte)0x4e, 9).Concat(new byte[12])
            .Concat(new byte[] { 0xa1, 0xfe, 0, 0, 0, 0, 0x99, 0xb7, 0xf5, 0x3e })
            .Concat(new byte[15]).Concat(new byte[] { 0xa1, 0xf8 })
            .Concat(Enumerable.Repeat((byte)0x6c, 256))
            .Concat(new byte[] { 0x5c, 0x89, 0xf9, 0xfa, 0, 0 }).ToArray();
        Assert.Equal(310, expected.Length);
        Assert.Equal(expected, words.Select(DataBits).ToArray());
        Assert.Equal(0x4489, words[21]);
        Assert.Equal(0x4489, words[46]);
        Assert.Equal(2, words.Count(word => word == 0x4489));
    }

    [Theory]
    [InlineData(256, false, 33, 54)]
    [InlineData(256, true, 32, 64)]
    [InlineData(512, false, 18, 54)]
    [InlineData(512, true, 17, 64)]
    [InlineData(1024, false, 9, 54)]
    [InlineData(1024, true, 9, 64)]
    public void EverySupportedLayoutIsReadableWithItsActualInterleaveAndExactStride(ushort size, bool sequential, int slots, int overhead)
    {
        var parameters = Acb4000DriveParameters.Default with { BlockSize = size, Cylinders = 2, Heads = 2 };
        Assert.True(Acb4000FormatGeometry.TryCreate(parameters, 0, sequential ? (byte)1 : (byte)2, out var geometry));
        var map = new byte[slots];
        Assert.True(Acb4000TrackLayout.TryBuild(parameters, geometry, 1, 0, default, map, out _));
        var channel = new Acb4000FormatChannel();
        channel.BeginTrack(map, 256, size, sequential, 0x37);
        var length = slots * (size + overhead) - (sequential ? 10 : 0);
        var words = ReadWords(channel, length + 8);
        var reader = new Acb4000ReadChannel(size);
        var ids = new List<Acb4000IdField>();
        var data = new List<byte[]>();
        foreach (var word in words)
        {
            for (var bit = 15; bit >= 0; bit--)
            {
                var kind = reader.Feed((word & (1 << bit)) != 0);
                if (kind == Acb4000FieldKind.Id)
                {
                    Assert.True(reader.FieldClockValid);
                    Assert.True(Acb4000FieldCodec.TryReadId(reader.Field, false, out var id, out _));
                    ids.Add(id);
                }
                else if (kind == Acb4000FieldKind.Data)
                {
                    Assert.True(reader.FieldClockValid);
                    var payload = new byte[size];
                    Assert.True(Acb4000FieldCodec.TryReadData(reader.Field, payload, false, out var valid, out _));
                    Assert.True(valid);
                    data.Add(payload);
                }
            }
        }
        Assert.Equal(slots, ids.Count);
        Assert.Equal(slots, data.Count);
        Assert.Equal(map.Select(entry => 256U + (uint)(entry & 0x3f)), ids.Select(id => id.LogicalAddress));
        Assert.Single(ids, id => id.IsLastSector);
        Assert.Equal(256U + (uint)slots - 1, ids.Single(id => id.IsLastSector).LogicalAddress);
        Assert.All(data, bytes => Assert.All(bytes, value => Assert.Equal(0x37, value)));
        Assert.All(words[length..], word => Assert.Equal(0x9254, word));
        channel.ClockCell(index: true);
        Assert.True(channel.Complete);
        Assert.False(channel.WriteGate);
    }

    [Theory]
    [InlineData(false, 9)]
    [InlineData(true, 19)]
    public void SequentialSelectionChangesOnlyTheGapsAfterTheFirstSector(bool sequential, int gap)
    {
        var channel = new Acb4000FormatChannel();
        channel.BeginTrack([0, 0x41], 0, 256, sequential, 0x6c);
        var words = ReadWords(channel, 310 + gap + 12 + 10);
        var bytes = words.Select(DataBits).ToArray();
        Assert.All(bytes[..9], value => Assert.Equal(0x4e, value));
        Assert.All(bytes.AsSpan(310, gap).ToArray(), value => Assert.Equal(0x4e, value));
        Assert.All(bytes.AsSpan(310 + gap, 12).ToArray(), value => Assert.Equal(0, value));
        Assert.Equal(0xa1, bytes[310 + gap + 12]);
        Assert.Equal(0xfe, bytes[310 + gap + 13]);
    }

    [Fact]
    public void DefectiveSlotsKeepTheirMarkAndAdvanceTheRecordedDefectCounter()
    {
        var channel = new Acb4000FormatChannel();
        byte[] map = [0, 0x80, 0x41];
        channel.BeginTrack(map, 100, 256, false, 0x6c, precedingDefects: 3);
        // The channel owns its copy, not a reference to the caller's mutable table.
        map.AsSpan().Fill(0xff);
        var words = ReadWords(channel, 3 * 310);
        var reader = new Acb4000ReadChannel();
        var fields = new List<Acb4000IdField>();
        foreach (var word in words)
            for (var bit = 15; bit >= 0; bit--)
                if (reader.Feed((word & (1 << bit)) != 0) == Acb4000FieldKind.Id)
                {
                    Assert.True(Acb4000FieldCodec.TryReadId(reader.Field, false, out var id, out _));
                    fields.Add(id);
                }
        Assert.Equal(new[] { new Acb4000IdField(100, 3, false), new Acb4000IdField(100, 4, true),
            new Acb4000IdField(101, 0x84, false) }, fields);
    }

    [Fact]
    public void IndexStartsWritingButDoesNotInterruptTheSectorSequence()
    {
        var channel = new Acb4000FormatChannel();
        channel.BeginTrack([0x40], 0, 256, false, 0x6c);
        for (var cell = 0; cell < 1000; cell++) Assert.False(channel.ClockCell(index: false));
        Assert.True(channel.WaitingForIndex);
        Assert.False(channel.WriteGate);
        for (var cell = 0; cell < 310 * 16; cell++)
        {
            channel.ClockCell(index: cell == 0 || cell == 1000);
            Assert.True(channel.WriteGate);
            Assert.False(channel.Complete);
        }
        // Hold the tail over several complete words and a partial word, then stop at INDEX.
        for (var cell = 0; cell < 7 * 16 + 5; cell++) channel.ClockCell(index: false);
        channel.ClockCell(index: true);
        Assert.True(channel.Complete);
        Assert.False(channel.WriteGate);
        for (var cell = 0; cell < 20; cell++) Assert.False(channel.ClockCell(index: true));
    }

    [Fact]
    public void ResetDropsTheGateAtEverySectionOfTheWrittenRecord()
    {
        var channel = new Acb4000FormatChannel();
        foreach (var cells in new[] { 0, 1, 9 * 16, 21 * 16, 31 * 16, 46 * 16, 308 * 16, 310 * 16 + 7 })
        {
            channel.BeginTrack([0x40], 0, 256, false, 0x6c);
            for (var cell = 0; cell < cells; cell++) channel.ClockCell(cell == 0);
            channel.Reset();
            Assert.False(channel.WriteGate);
            Assert.False(channel.Complete);
            Assert.False(channel.WaitingForIndex);
            Assert.False(channel.ClockCell(index: true));
        }
    }

    [Fact]
    public void InvalidNewTrackLeavesAnExistingWriteUntouched()
    {
        var channel = new Acb4000FormatChannel();
        var control = new Acb4000FormatChannel();
        channel.BeginTrack([0x40], 0, 256, false, 0x6c);
        control.BeginTrack([0x40], 0, 256, false, 0x6c);
        Assert.Equal(control.ClockCell(true), channel.ClockCell(true));
        foreach (var invalid in new byte[][] { [], [0x80], [0], [0x40, 0x41], [0, 0x40], [0, 0x42], [0xc0], new byte[34] })
            Assert.ThrowsAny<ArgumentException>(() => channel.BeginTrack(invalid, 0, 256, false, 0x6c));
        Assert.Throws<ArgumentOutOfRangeException>(() => channel.BeginTrack([0x40], 0, 128, false, 0x6c));
        Assert.Throws<ArgumentOutOfRangeException>(() => channel.BeginTrack([0, 0x41], 0x7fffff, 256, false, 0x6c));
        Assert.Throws<ArgumentOutOfRangeException>(() => channel.BeginTrack([0x80, 0x40], 0, 256, false, 0x6c, 127));
        for (var cell = 0; cell < 310 * 16; cell++) Assert.Equal(control.ClockCell(false), channel.ClockCell(false));
    }

    [Fact]
    public void WritingAndPreparingTheNextTrackDoNotAllocateAfterConstruction()
    {
        var channel = new Acb4000FormatChannel();
        byte[] map = [0, 0x41];
        channel.BeginTrack(map, 0, 256, false, 0x6c);
        for (var cell = 0; cell < 1000; cell++) channel.ClockCell(cell == 0);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var track = 0; track < 4; track++)
        {
            channel.BeginTrack(map, (uint)(track * 2), 1024, true, 0x6c);
            for (var cell = 0; cell < 2200 * 16; cell++) channel.ClockCell(cell == 0);
            channel.ClockCell(true);
            channel.Reset();
        }
        Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
    }

    private static ushort[] ReadWords(Acb4000FormatChannel channel, int count)
    {
        var words = new ushort[count];
        for (var cell = 0; cell < count * 16; cell++)
        {
            var value = channel.ClockCell(index: cell == 0);
            Assert.True(channel.WriteGate);
            words[cell / 16] = (ushort)((words[cell / 16] << 1) | (value ? 1 : 0));
        }
        return words;
    }

    private static byte DataBits(ushort word)
    {
        byte value = 0;
        for (var position = 14; position >= 0; position -= 2)
            value = (byte)((value << 1) | (word >> position & 1));
        return value;
    }
}

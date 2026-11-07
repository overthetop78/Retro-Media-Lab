using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed class Acb4000WriteChannelTests
{
    [Fact]
    public void WriteGatePublishesOnlyItsOpeningClosingAndResetEdges()
    {
        var channel = new Acb4000WriteChannel();
        var edges = new List<(int Cell, bool Gate)>();
        var cell = 0;
        channel.WriteGateChanged += gate => { Assert.Equal(channel.WriteGate, gate); edges.Add((cell, gate)); };
        channel.Begin(new byte[256]);
        while (channel.WriteGate && cell < 5000) { cell++; channel.ClockCell(); }
        Assert.Equal(new[] { (0, true), (4465, false) }, edges);
        channel.ClockCell(); channel.Reset();
        Assert.Equal(2, edges.Count);
        channel.Begin(new byte[256]);
        channel.Reset();
        Assert.Equal(new[] { (4465, true), (4465, false) }, edges.Skip(2));
    }

    [Fact]
    public void SegmentMatchesTheIndependentDataEccVectorAndExactGateLength()
    {
        var channel = new Acb4000WriteChannel();
        channel.Begin(Enumerable.Repeat((byte)0x6c, 256).ToArray());
        var words = ReadWords(channel, 15 + 262 + 2);
        var previous = false;
        var bytes = new List<byte>();
        for (var index = 0; index < words.Length; index++)
        {
            if (index == 15)
            {
                Assert.Equal(0x4489, words[index]);
                bytes.Add(0xa1);
                previous = true;
                continue;
            }
            bytes.Add(Acb4000MfmEncoding.Decode(words[index], ref previous, out var valid));
            Assert.True(valid);
        }
        Assert.Equal(new byte[15].Concat(new byte[] { 0xa1, 0xf8 })
            .Concat(Enumerable.Repeat((byte)0x6c, 256))
            .Concat(new byte[] { 0x5c, 0x89, 0xf9, 0xfa, 0, 0 }), bytes);
        Assert.True(channel.WriteGate);
        Assert.False(channel.Complete);
        Assert.False(channel.ClockCell());
        Assert.False(channel.WriteGate);
        Assert.True(channel.Complete);
        Assert.False(channel.ClockCell());
    }

    [Theory]
    [InlineData(256)]
    [InlineData(512)]
    [InlineData(1024)]
    public void EveryPayloadSizeHasAReadableFieldAndDoesNotBorrowTheCallersBuffer(int size)
    {
        var payload = Enumerable.Range(0, size).Select(index => (byte)index).ToArray();
        var expected = payload.ToArray();
        var channel = new Acb4000WriteChannel();
        channel.Begin(payload);
        Array.Fill(payload, (byte)0);
        var reader = new Acb4000ReadChannel(size);
        var fields = 0;
        foreach (var word in ReadWords(channel, 15 + size + 6 + 2))
            for (var bit = 15; bit >= 0; bit--)
                if (reader.Feed((word & (1 << bit)) != 0) == Acb4000FieldKind.Data)
                {
                    var actual = new byte[size];
                    Assert.True(Acb4000FieldCodec.TryReadData(reader.Field, actual, false, out var valid, out _));
                    Assert.True(valid);
                    Assert.Equal(expected, actual);
                    fields++;
                }
        Assert.Equal(1, fields);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(240)]
    [InlineData(4208)]
    [InlineData(4463)]
    public void ResetInterruptsAnySegmentWithoutEmittingAnotherCell(int cells)
    {
        var channel = new Acb4000WriteChannel();
        channel.Begin(new byte[256]);
        for (var index = 0; index < cells; index++) channel.ClockCell();
        channel.Reset();
        Assert.False(channel.WriteGate);
        Assert.False(channel.Complete);
        Assert.False(channel.ClockCell());
        channel.Begin(new byte[256]);
        Assert.Equal((ushort)0xaaaa, ReadWords(channel, 1)[0]);
    }

    [Fact]
    public void InvalidSizeOrRestartDoesNotChangeThePendingCells()
    {
        var channel = new Acb4000WriteChannel();
        var reference = new Acb4000WriteChannel();
        var payload = Enumerable.Repeat((byte)0xff, 256).ToArray();
        channel.Begin(payload);
        reference.Begin(payload);
        for (var index = 0; index < 251; index++) Assert.Equal(reference.ClockCell(), channel.ClockCell());
        Assert.Throws<ArgumentOutOfRangeException>(() => channel.Begin(new byte[257]));
        Assert.Throws<InvalidOperationException>(() => channel.Begin(payload));
        for (var index = 251; index <= 279 * 16; index++)
        {
            Assert.Equal(reference.ClockCell(), channel.ClockCell());
            Assert.Equal(reference.WriteGate, channel.WriteGate);
        }
        Assert.True(channel.Complete);
    }

    [Fact]
    public void ClockingDoesNotAllocateAfterConstructionAndPreparation()
    {
        var channel = new Acb4000WriteChannel();
        var payload = new byte[1024];
        channel.Begin(payload);
        while (channel.WriteGate) channel.ClockCell();
        channel.Begin(payload);
        var before = GC.GetAllocatedBytesForCurrentThread();
        while (channel.WriteGate) channel.ClockCell();
        var after = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(before, after);
    }

    private static ushort[] ReadWords(Acb4000WriteChannel channel, int count)
    {
        var words = new ushort[count];
        for (var index = 0; index < count; index++)
            for (var cell = 0; cell < 16; cell++)
            {
                words[index] = (ushort)((words[index] << 1) | (channel.ClockCell() ? 1 : 0));
                Assert.True(channel.WriteGate);
            }
        return words;
    }
}

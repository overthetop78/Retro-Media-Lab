using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Interfaces;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed class Acb4000SequencerTests
{
    [Fact]
    public void AllTwentyFourInstructionsAndFourColumnsHaveIndependentReadableStorage()
    {
        var (sequencer, _, _) = Create();
        for (var column = 0; column < 4; column++)
            for (var state = 0; state < 24; state++)
                Assert.True(sequencer.TryWriteProgram((byte)(0x80 + column * 0x20 + state), (byte)(column * 29 + state)));
        for (var column = 0; column < 4; column++)
        {
            for (var state = 0; state < 24; state++)
            {
                Assert.True(sequencer.TryReadProgram((byte)(0x80 + column * 0x20 + state), out var value));
                Assert.Equal(column * 29 + state, value);
            }
            Assert.False(sequencer.TryReadProgram((byte)(0x98 + column * 0x20), out _));
        }
        Assert.False(sequencer.TryWriteProgram(0x7f, 0));
        Assert.False(sequencer.TryWriteProgram(0xff, 0));
    }

    [Theory]
    [InlineData(false, 0, 1)]
    [InlineData(false, 31, 32)]
    [InlineData(true, 0, 1)]
    [InlineData(true, 255, 256)]
    public void CounterIsInclusiveAndTransferUsesAllEightBits(bool transfer, byte count, int bytes)
    {
        var (sequencer, _, buffer) = Create();
        Program(sequencer, 0, 0x1f, (byte)(0x40 | (transfer ? 1 : 0)), count);
        sequencer.Start(0);
        for (var clock = 0; clock < bytes * 8 - 1; clock++) sequencer.ClockBit(false);
        Assert.False(sequencer.Stopped);
        Assert.Equal(1, sequencer.RemainingBytes);
        sequencer.ClockBit(false);
        Assert.True(sequencer.Stopped);
        Assert.Equal(transfer ? bytes : 0, buffer.Writes);
    }

    [Fact]
    public void EditingCountDoesNotReplaceTheAlreadyLoadedCounter()
    {
        var (sequencer, _, _) = Create();
        Program(sequencer, 0, 0x1f, 0x40, 2);
        sequencer.Start(0);
        ClockByte(sequencer, 0);
        sequencer.TryWriteProgram(0xc0, 0);
        ClockByte(sequencer, 0);
        Assert.False(sequencer.Stopped);
        ClockByte(sequencer, 0);
        Assert.True(sequencer.Stopped);
    }

    [Theory]
    [InlineData(1, true, false, 31)]
    [InlineData(2, false, true, 31)]
    [InlineData(5, true, false, 1)]
    [InlineData(6, false, true, 1)]
    public void InputAndIndexConditionsBranchWithoutWaitingForByteCompletion(byte condition, bool input, bool index, byte destination)
    {
        var (sequencer, _, _) = Create();
        Program(sequencer, 0, (byte)(condition << 5), 0, 31);
        Program(sequencer, 1, 1, 0, 31);
        sequencer.WriteBranch(1);
        sequencer.Start(0);
        if (input) sequencer.SetInput(true);
        if (index) sequencer.SetIndex(true);
        Assert.Equal(destination, sequencer.State);
        Assert.NotEqual(0, sequencer.ReadStatus() & 0x20);
        Assert.Equal(0, sequencer.ReadStatus() & 0x20);
    }

    [Theory]
    [InlineData(3, 31)]
    [InlineData(7, 1)]
    public void MismatchConditionsBranchAfterTheFirstUnequalByte(byte condition, byte destination)
    {
        var (sequencer, _, _) = Create();
        Program(sequencer, 0, (byte)(condition << 5), 0x42, 2, 0x55);
        Program(sequencer, 1, 1, 0, 31);
        sequencer.WriteBranch(1);
        sequencer.Start(0);
        ClockByte(sequencer, 0x54);
        sequencer.ClockBit(false);
        Assert.Equal(destination, sequencer.State);
    }

    [Fact]
    public void CarryConditionBranchesOnlyAfterTheInclusiveCount()
    {
        var (sequencer, _, _) = Create();
        Program(sequencer, 0, 0x80, 0, 1);
        Program(sequencer, 1, 1, 0, 31);
        sequencer.WriteBranch(1);
        sequencer.Start(0);
        ClockByte(sequencer, 0);
        Assert.Equal(0, sequencer.State);
        ClockByte(sequencer, 0);
        Assert.Equal(1, sequencer.State);
    }

    public static IEnumerable<object[]> ReadConditions()
    {
        byte[][] destinations =
        [
            [4, 4, 4, 4], [4, 31, 4, 31], [4, 4, 31, 31], [4, 31, 31, 31],
            [5, 4, 4, 4], [4, 5, 4, 5], [4, 4, 5, 5], [4, 5, 5, 5]
        ];
        for (byte condition = 0; condition < 8; condition++)
            for (var flags = 0; flags < 4; flags++)
                yield return [condition, (flags & 1) != 0, (flags & 2) != 0, destinations[condition][flags]];
    }

    [Theory]
    [MemberData(nameof(ReadConditions))]
    public void AllReadBranchConditionsAreEvaluatedAtTheEndOfEcc(byte condition, bool badEcc, bool mismatch, byte destination)
    {
        var (sequencer, _, buffer) = Create();
        Program(sequencer, 0, 1, 0x42, 0x80, 0xa1);
        Program(sequencer, 1, 2, 0x02, 0, 0xfe);
        Program(sequencer, 2, 3, 0x01, 0);
        Program(sequencer, 3, (byte)(condition << 5 | 4), 0, 0x43);
        Program(sequencer, 4, 4, 0, 31);
        Program(sequencer, 5, 5, 0, 31);
        sequencer.WriteBranch(5);
        var record = new byte[] { 0xa1, mismatch ? (byte)0xfc : (byte)0xfe, 0x5a, 0, 0, 0, 0 };
        Acb4000ErrorCheck.Write(record);
        if (badEcc) record[^1] ^= 1;
        sequencer.Start(0);
        foreach (var value in record) ClockByte(sequencer, value);
        Assert.Equal(destination, sequencer.State);
        Assert.False(sequencer.ReadGate);
        Assert.Equal(new byte[] { 0x5a }, buffer.Data.AsSpan(0, buffer.Writes).ToArray());
        Assert.Equal(badEcc, (sequencer.ReadStatus() & 4) != 0);
    }

    [Fact]
    public void IndexAndSectorEventsRemainSetWhileTheirInputIsActive()
    {
        var (sequencer, ecc, _) = Create();
        sequencer.SetIndex(true);
        Assert.Equal(9, sequencer.ReadOperation());
        Assert.Equal(9, sequencer.ReadOperation());
        sequencer.SetIndex(false);
        Assert.Equal(9, sequencer.ReadOperation());
        Assert.Equal(8, sequencer.ReadOperation());
        Program(sequencer, 0, 0xc0, 0, 31);
        Program(sequencer, 1, 1, 0, 31);
        sequencer.WriteBranch(1);
        sequencer.Start(0);
        sequencer.SetSector(true);
        Assert.Equal(0, sequencer.State);
        ecc.WriteControl(0xd0);
        sequencer.ClockBit(false);
        Assert.Equal(1, sequencer.State);
        Assert.NotEqual(0, sequencer.ReadOperation() & 2);
    }

    [Fact]
    public void StackReturnsTheLastEightReceivedBytesInReverseOrder()
    {
        var (sequencer, _, _) = Create();
        Program(sequencer, 0, 0x1f, 0x50, 7);
        sequencer.Start(0);
        for (byte value = 1; value <= 8; value++) ClockByte(sequencer, value);
        for (var expected = 8; expected > 0; expected--)
        {
            Assert.True(sequencer.TryPop(out var actual));
            Assert.Equal(expected, actual);
        }
        Assert.False(sequencer.TryPop(out _));
    }

    [Fact]
    public void SearchReadsBufferWithoutOverwritingItsPatternAndKeepsFirstDifferencePriority()
    {
        var (sequencer, _, buffer) = Create();
        buffer.Data[0] = 0x80;
        buffer.Data[1] = 0x80;
        Program(sequencer, 0, 0x1f, 0x43, 1);
        sequencer.WriteOperation(0x10);
        sequencer.Start(0);
        ClockByte(sequencer, 0x7f);
        ClockByte(sequencer, 0xff);
        Assert.Equal(2, buffer.Reads);
        Assert.Equal(0, buffer.Writes);
        Assert.Equal(2, sequencer.ReadStatus() & 3);
    }

    [Fact]
    public void InhibitedDataCarryAddsOneFullCounterWrapThenClearsTheInhibitBit()
    {
        var (sequencer, _, buffer) = Create();
        Program(sequencer, 0, 0x1f, 0x41, 0);
        sequencer.WriteOperation(0x80);
        sequencer.Start(0);
        ClockByte(sequencer, 0);
        Assert.False(sequencer.Stopped);
        Assert.Equal(256, sequencer.RemainingBytes);
        Assert.Equal(0, sequencer.ReadOperation() & 0x80);
        for (var value = 0; value < 256; value++) ClockByte(sequencer, (byte)value);
        Assert.True(sequencer.Stopped);
        Assert.Equal(257, buffer.Writes);
    }

    [Fact]
    public void WriteGateOpensAtBitRingFourAndReadsEveryTransferByteFromBuffer()
    {
        var (sequencer, _, buffer) = Create();
        buffer.Data[0] = 0xa5;
        buffer.Data[1] = 0x5a;
        Program(sequencer, 0, 0x1f, 0x81, 1);
        sequencer.Start(0);
        for (var bit = 0; bit < 3; bit++)
        {
            sequencer.ClockBit(false);
            Assert.False(sequencer.WriteGate);
        }
        sequencer.ClockBit(false);
        Assert.True(sequencer.WriteGate);
        for (var bit = 4; bit < 16; bit++) sequencer.ClockBit(false);
        Assert.Equal(2, buffer.Reads);
        Assert.True(sequencer.Stopped);
        Assert.False(sequencer.WriteGate);
    }

    [Fact]
    public void ExternalResetHoldsTheSequencerButPreservesProgramAndPolynomial()
    {
        var (sequencer, ecc, _) = Create();
        Program(sequencer, 0, 0x1f, 0x40, 0);
        sequencer.Start(0);
        sequencer.Reset();
        Assert.True(ecc.ResetAsserted);
        Assert.Equal(0, sequencer.ReadStatus());
        sequencer.Start(0);
        Assert.True(sequencer.Stopped);
        Assert.True(sequencer.TryReadProgram(0xa0, out var control));
        Assert.Equal(0x40, control);
        Assert.Equal(0x41044185u, ecc.Polynomial);
        ecc.WriteControl(0xc0);
        sequencer.Start(0);
        ClockByte(sequencer, 0);
        Assert.True(sequencer.Stopped);
    }

    [Fact]
    public void UndocumentedStateHasNoFallbackInstructionOrGate()
    {
        var (sequencer, _, _) = Create();
        sequencer.Start(24);
        Assert.False(sequencer.StateIsDocumented);
        for (var bit = 0; bit < 32; bit++) sequencer.ClockBit(true);
        Assert.Equal(24, sequencer.State);
        Assert.False(sequencer.ReadGate);
        Assert.False(sequencer.WriteGate);
    }

    [Fact]
    public void ClockLoopDoesNotAllocate()
    {
        var (sequencer, _, _) = Create();
        Program(sequencer, 0, 0, 0x41, 0xff);
        sequencer.Start(0);
        // Include complete 256-byte counter loops before measuring the hot loop.
        for (var bit = 0; bit < 2 * 256 * 8; bit++) sequencer.ClockBit(false);
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var bit = 0; bit < 10000; bit++) sequencer.ClockBit(false);
        Assert.Equal(allocated, GC.GetAllocatedBytesForCurrentThread());
    }

    [Fact]
    public void SebReadStateWaitsForAddressMarkWithoutConsumingItsCounter()
    {
        var (sequencer, _, _) = Create();
        Program(sequencer, 0, 1, 0x42, 0x80, 0xa1);
        Program(sequencer, 1, 0x1f, 0, 0x40);
        sequencer.Start(0);
        for (var clock = 0; clock < 100; clock++) sequencer.ClockBit((clock & 1) != 0);
        Assert.True(sequencer.WaitingForReadSync);
        Assert.Equal(0, sequencer.State);
        Assert.Equal(1, sequencer.RemainingBytes);
        Assert.False(sequencer.ReadFieldCompleted);
        Assert.True(sequencer.AcceptReadAddressMark(0xa1));
        Assert.False(sequencer.WaitingForReadSync);
        Assert.Equal(1, sequencer.State);
        ClockByte(sequencer, 0);
        Assert.True(sequencer.Stopped);
        Assert.True(sequencer.ReadFieldCompleted);
        Assert.False(sequencer.ReadFieldEccValid);
    }

    private static void Program(Acb4000Sequencer sequencer, byte state, byte next, byte control, byte count, byte data = 0)
    {
        Assert.True(sequencer.TryWriteProgram((byte)(0x80 + state), next));
        Assert.True(sequencer.TryWriteProgram((byte)(0xa0 + state), control));
        Assert.True(sequencer.TryWriteProgram((byte)(0xc0 + state), count));
        Assert.True(sequencer.TryWriteProgram((byte)(0xe0 + state), data));
    }

    private static void ClockByte(Acb4000Sequencer sequencer, byte value)
    {
        if (sequencer.WaitingForReadSync)
        {
            Assert.True(sequencer.AcceptReadAddressMark(value));
            return;
        }
        for (var bit = 7; bit >= 0; bit--) sequencer.ClockBit((value & (1 << bit)) != 0);
    }

    private static (Acb4000Sequencer Sequencer, Acb4000EccRegisters Ecc, Buffer Buffer) Create()
    {
        var ecc = new Acb4000EccRegisters();
        var taps = 0x41044185u >> 1;
        for (var register = 0x74; register <= 0x77; register++, taps >>= 8) ecc.WritePolynomial(register, (byte)taps);
        ecc.WriteControl(0xc0);
        var buffer = new Buffer();
        return (new Acb4000Sequencer(ecc, buffer), ecc, buffer);
    }

    private sealed class Buffer : IAcb4000SequencerBus
    {
        internal byte[] Data { get; } = new byte[65536];
        internal int Reads;
        internal int Writes;
        public byte ReadBuffer() => Data[Reads++];
        public void WriteBuffer(byte value) => Data[Writes++] = value;
    }
}

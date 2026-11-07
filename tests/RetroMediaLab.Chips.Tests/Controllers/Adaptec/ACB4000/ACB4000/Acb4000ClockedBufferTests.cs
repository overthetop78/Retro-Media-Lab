using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;
using System.Diagnostics;
using Xunit.Abstractions;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed class Acb4000ClockedBufferTests(ITestOutputHelper output)
{
    [Fact]
    public void RamRequestsPublishTheirChangedLevelsAndCancellationIdentities()
    {
        var ram = new byte[1024];
        var buffer = CreateBuffer(ram);
        buffer.TryWrite(0x53, 0xc4);
        buffer.ConfigureClockedTransfers(true);
        var peripheral = new List<(bool Pending, uint Epoch)>();
        var host = new List<(bool Pending, uint Epoch)>();
        buffer.PeripheralCycleChanged += (pending, epoch) =>
        {
            Assert.Equal(buffer.PeripheralCyclePending, pending);
            Assert.Equal(buffer.PeripheralCycleEpoch, epoch);
            peripheral.Add((pending, epoch));
        };
        buffer.HostCycleChanged += (pending, epoch) =>
        {
            Assert.Equal(buffer.HostCyclePending, pending);
            Assert.Equal(buffer.HostCycleEpoch, epoch);
            host.Add((pending, epoch));
        };
        var a = buffer.PeripheralCycleEpoch;
        buffer.RequestBufferWrite(0xa5);
        buffer.RequestBufferWrite(0x3c);
        Assert.Single(peripheral);
        Assert.Equal(0, buffer.WritePointer);
        Assert.True(buffer.CompletePeripheralCycle());
        Assert.True(buffer.TryCompleteBufferWrite());
        Assert.Equal(0x3c, ram[0]);
        buffer.CancelBufferTransfers();
        buffer.CancelBufferTransfers();
        Assert.Equal(new[] { (true, a), (false, a), (false, a + 1), (false, a + 2) }, peripheral);

        var b = buffer.HostCycleEpoch;
        Assert.True(buffer.RequestHostWrite(0x5a));
        Assert.False(buffer.RequestHostWrite(0x6b));
        Assert.Single(host);
        Assert.True(buffer.CompleteHostCycle());
        Assert.True(buffer.TryTakeHostCycle(out _));
        Assert.Equal(0x5a, ram[1]);
        buffer.CancelHostTransfer();
        Assert.False(buffer.RequestHostWrite(0x6b));
        Assert.Equal(new[] { (true, b), (false, b), (false, b + 1) }, host);
    }

    [Fact]
    public void WiringReceivesAHostRequestAlreadyPresentBeforeTheAicIsConnected()
    {
        var ram = new byte[1024];
        var buffer = CreateBuffer(ram);
        buffer.TryWrite(0x53, 0xc4);
        Assert.True(buffer.RequestHostWrite(0xa5));
        var aic = new Acb4000AicRegisters(Acb4000EccRegisters.CreateFirmwareConfiguration(), buffer);
        aic.ConfigureClockedBufferTransfers(true);
        aic.AdvanceSystemHalfCycles(4);
        Assert.Equal(0, buffer.WritePointer);
        aic.AdvanceSystemHalfCycles(8);
        Assert.Equal(1, buffer.WritePointer);
        Assert.Equal(0xa5, ram[0]);
        Assert.True(buffer.TryTakeHostCycle(out _));
    }

    [Fact]
    public void ClockedRamLoopHasNoAllocationAndPreservesWrap()
    {
        const int iterations = 1_000_000;
        const int warmup = 50_000;
        const int cellsPerByte = 16;
        var ram = new byte[1024];
        var buffer = CreateBuffer(ram);
        var aic = new Acb4000AicRegisters(Acb4000EccRegisters.CreateFirmwareConfiguration(), buffer);
        aic.ConfigureClockedBufferTransfers(true);
        aic.Clocks.SelectReference(true);
        for (var index = 0; index < warmup; index++) Cycle(index);
        var previousPointer = buffer.WritePointer;
        var failed = false;
        var allocations = GC.GetAllocatedBytesForCurrentThread();
        var start = Stopwatch.GetTimestamp();
        for (var index = 0; index < iterations; index++) failed |= !Cycle(index);
        var elapsed = Stopwatch.GetTimestamp() - start;
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocations;
        Assert.False(failed);
        Assert.Equal(0, allocated);
        Assert.Equal((previousPointer + iterations) % ram.Length, buffer.WritePointer);
        Assert.Equal(unchecked((byte)(iterations - 1)), ram[(buffer.WritePointer - 1) & (ram.Length - 1)]);
        output.WriteLine("Clocked RAM: {0:F3} ns/byte, {1:F3} MB/s, {2} allocated bytes",
            elapsed * 1_000_000_000d / Stopwatch.Frequency / iterations,
            iterations * (double)Stopwatch.Frequency / elapsed / 1_000_000d, allocated);

        bool Cycle(int index)
        {
            buffer.RequestBufferWrite((byte)index);
            aic.AdvanceReferenceHalfCycles(cellsPerByte);
            return buffer.TryCompleteBufferWrite();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BufferResetAndImmediateNewRequestCannotReuseTheOldSampledAddress(bool host)
    {
        var ram = new byte[1024];
        var buffer = CreateBuffer(ram);
        buffer.TryWrite(0x53, 0xc4);
        var aic = new Acb4000AicRegisters(Acb4000EccRegisters.CreateFirmwareConfiguration(), buffer);
        aic.ConfigureClockedBufferTransfers(true);
        Request(0xa5);
        aic.AdvanceSystemHalfCycles(4);
        buffer.TryWrite(0x59, 0);
        Request(0x3c);
        aic.AdvanceSystemHalfCycles(8);
        Assert.Equal(0, buffer.WritePointer);
        Assert.Equal(0, ram[0]);
        aic.AdvanceSystemHalfCycles(8);
        Assert.Equal(1, buffer.WritePointer);
        Assert.Equal(0x3c, ram[0]);
        if (host) Assert.True(buffer.TryTakeHostCycle(out _));
        else Assert.True(buffer.TryCompleteBufferWrite());

        void Request(byte value)
        {
            if (host) Assert.True(buffer.RequestHostWrite(value));
            else buffer.RequestBufferWrite(value);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(23333)]
    [InlineData(100001)]
    public void AccumulatedIdleClockPreservesTheFollowingRamCycle(int idleTicks)
    {
        var grouped = new Acb4000(_ => null, 2_000_003);
        var individual = new Acb4000(_ => null, 2_000_003);
        grouped.AdvanceIdleTicks((uint)idleTicks);
        for (var tick = 0; tick < idleTicks; tick++) individual.Tick();
        Assert.Equal(individual.Aic.Clocks.ClockA, grouped.Aic.Clocks.ClockA);
        grouped.BufferController.RequestBufferWrite(0xa5);
        individual.BufferController.RequestBufferWrite(0xa5);
        Assert.Throws<InvalidOperationException>(() => grouped.AdvanceIdleTicks(1));
        for (var tick = 0; tick < 16; tick++)
        {
            grouped.Tick();
            individual.Tick();
            Assert.Equal(individual.Aic.Clocks.ClockA, grouped.Aic.Clocks.ClockA);
            Assert.Equal(individual.Aic.Clocks.ClockB, grouped.Aic.Clocks.ClockB);
            Assert.Equal(individual.BufferController.WritePointer, grouped.BufferController.WritePointer);
            Assert.Equal(individual.BufferController.PeripheralCyclePending, grouped.BufferController.PeripheralCyclePending);
        }
        Assert.Equal(1, grouped.BufferController.WritePointer);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PeripheralRequestHasPriorityAndHostUsesTheFollowingRamSlot(bool hostWrite)
    {
        var ram = new byte[1024];
        ram[0] = 0x5a;
        var buffer = CreateBuffer(ram);
        buffer.TryWrite(0x53, (byte)(hostWrite ? 0xc4 : 0xd8));
        var aic = new Acb4000AicRegisters(Acb4000EccRegisters.CreateFirmwareConfiguration(), buffer);
        aic.ConfigureClockedBufferTransfers(true);
        if (hostWrite)
        {
            buffer.RequestBufferRead();
            Assert.True(buffer.RequestHostWrite(0xa5));
        }
        else
        {
            buffer.RequestBufferWrite(0xa5);
            Assert.True(buffer.RequestHostRead());
        }
        aic.AdvanceSystemHalfCycles(4);
        Assert.Equal(0, buffer.ReadPointer);
        Assert.Equal(0, buffer.WritePointer);
        Assert.False(buffer.HostTransferPending);
        aic.AdvanceSystemHalfCycles(8);
        Assert.False(buffer.PeripheralCyclePending);
        Assert.True(buffer.HostCyclePending);
        Assert.False(buffer.HostTransferPending);
        if (hostWrite)
        {
            Assert.True(buffer.TryCompleteBufferRead(out var oldValue));
            Assert.Equal(0x5a, oldValue);
            Assert.Equal(0x5a, ram[0]);
        }
        else Assert.Equal(0xa5, ram[0]);
        aic.AdvanceSystemHalfCycles(8);
        Assert.False(buffer.HostCyclePending);
        Assert.True(buffer.HostTransferPending);
        Assert.True(buffer.TryTakeHostCycle(out var hostValue));
        if (hostWrite) Assert.Equal(0xa5, ram[0]);
        else Assert.Equal(0xa5, hostValue);
        Assert.False(buffer.TryTakeHostCycle(out _));
        buffer.TryRead(0x53, out var control);
        Assert.Equal(0, control & 0x20);
        Assert.True(buffer.CompleteHostAcknowledgement());
        buffer.TryRead(0x53, out control);
        Assert.NotEqual(0, control & 0x20);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancelHostRequestBeforeRamCompletionPreservesMemory(bool reset)
    {
        var ram = Enumerable.Repeat((byte)0x6c, 1024).ToArray();
        var buffer = CreateBuffer(ram);
        buffer.TryWrite(0x53, 0xc4);
        var aic = new Acb4000AicRegisters(Acb4000EccRegisters.CreateFirmwareConfiguration(), buffer);
        aic.ConfigureClockedBufferTransfers(true);
        Assert.True(buffer.RequestHostWrite(0xa5));
        aic.AdvanceSystemHalfCycles(4);
        if (reset) buffer.TryWrite(0x59, 1);
        else buffer.CancelHostTransfer();
        aic.AdvanceSystemHalfCycles(100);
        Assert.Equal(0, buffer.WritePointer);
        Assert.False(buffer.HostTransferPending);
        Assert.False(buffer.HostCyclePending);
        Assert.False(buffer.TryTakeHostCycle(out _));
        Assert.All(ram, value => Assert.Equal(0x6c, value));
    }

    [Theory]
    [InlineData(false, 0x00, 4)]
    [InlineData(false, 0x40, 2)]
    [InlineData(false, 0x80, 1)]
    [InlineData(true, 0x00, 4)]
    [InlineData(true, 0x10, 2)]
    public void AicCommitsRamAtTheFollowingClockEdge(bool reference, byte control, int divider)
    {
        var ram = new byte[1024];
        var buffer = CreateBuffer(ram);
        var aic = new Acb4000AicRegisters(Acb4000EccRegisters.CreateFirmwareConfiguration(), buffer);
        aic.ConfigureClockedBufferTransfers(true);
        aic.TryWrite(0x7f, control);
        aic.Clocks.SelectReference(reference);
        buffer.RequestBufferWrite(0xa5);
        Advance(aic, !reference, 100);
        Assert.Equal(0, buffer.WritePointer);
        Advance(aic, reference, divider);
        Assert.Equal(0, buffer.WritePointer);
        Assert.Equal(0, ram[0]);
        Advance(aic, reference, divider * 2 - 1);
        Assert.Equal(0, buffer.WritePointer);
        Advance(aic, reference, 1);
        Assert.Equal(1, buffer.WritePointer);
        Assert.Equal(0xa5, ram[0]);
        Assert.True(buffer.TryCompleteBufferWrite());
        Assert.False(buffer.TryCompleteBufferWrite());
        buffer.RequestBufferRead();
        Advance(aic, reference, divider * 6);
        Assert.Equal(1, buffer.ReadPointer);
        Assert.True(buffer.TryCompleteBufferRead(out var value));
        Assert.Equal(0xa5, value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GroupedAicClocksPreserveIndividualEdgesAndIdlePhase(bool reference)
    {
        for (var count = 0; count < 128; count++)
        {
            var groupedRam = new byte[1024];
            var individualRam = new byte[1024];
            var groupedBuffer = CreateBuffer(groupedRam);
            var individualBuffer = CreateBuffer(individualRam);
            var grouped = new Acb4000AicRegisters(Acb4000EccRegisters.CreateFirmwareConfiguration(), groupedBuffer);
            var individual = new Acb4000AicRegisters(Acb4000EccRegisters.CreateFirmwareConfiguration(), individualBuffer);
            grouped.ConfigureClockedBufferTransfers(true);
            individual.ConfigureClockedBufferTransfers(true);
            grouped.Clocks.SelectReference(reference);
            individual.Clocks.SelectReference(reference);
            groupedBuffer.RequestBufferWrite(0x3c);
            individualBuffer.RequestBufferWrite(0x3c);
            Advance(grouped, reference, count);
            for (var edge = 0; edge < count; edge++) Advance(individual, reference, 1);
            Assert.Equal(individualRam, groupedRam);
            Assert.Equal(individualBuffer.WritePointer, groupedBuffer.WritePointer);
            Assert.Equal(individual.Clocks.ClockA, grouped.Clocks.ClockA);
            Assert.Equal(individual.Clocks.ClockB, grouped.Clocks.ClockB);
            Assert.Equal(individual.Clocks.TransferPending, grouped.Clocks.TransferPending);
        }
    }

    [Fact]
    public void HighImpedanceAndResetCannotCompleteAnUnfinishedRamCycle()
    {
        var ram = new byte[1024];
        var buffer = CreateBuffer(ram);
        var aic = new Acb4000AicRegisters(Acb4000EccRegisters.CreateFirmwareConfiguration(), buffer);
        aic.ConfigureClockedBufferTransfers(true);
        buffer.RequestBufferWrite(0xa5);
        aic.AdvanceSystemHalfCycles(4);
        aic.TryWrite(0x7f, 8);
        aic.AdvanceSystemHalfCycles(100);
        Assert.Equal(0, buffer.WritePointer);
        Assert.Equal(0, ram[0]);
        aic.Reset();
        aic.TryWrite(0x7f, 0);
        aic.AdvanceSystemHalfCycles(100);
        Assert.False(buffer.PeripheralCyclePending);
        Assert.Equal(0, buffer.WritePointer);
        Assert.Equal(0, ram[0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => aic.AdvanceSystemHalfCycles(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => aic.AdvanceReferenceHalfCycles(-1));
    }

    private static void Advance(Acb4000AicRegisters aic, bool reference, int count)
    {
        if (reference) aic.AdvanceReferenceHalfCycles(count);
        else aic.AdvanceSystemHalfCycles(count);
    }

    [Fact]
    public void NormalStopKeepsTheLastRequestedRamCycleButResetCancelsIt()
    {
        var ram = new byte[1024];
        var buffer = CreateBuffer(ram);
        var sequencer = new Acb4000Sequencer(Acb4000EccRegisters.CreateFirmwareConfiguration(), buffer);
        sequencer.ConfigureClockedBufferTransfers(true);
        Program(sequencer, 0, 0x1f, 0x41, 0, 0);
        sequencer.Start(0);
        for (var bit = 7; bit >= 0; bit--) sequencer.ClockBit((0xa5 & (1 << bit)) != 0);
        Assert.True(sequencer.Stopped);
        Assert.True(buffer.PeripheralCyclePending);
        Assert.Equal(0, buffer.WritePointer);
        Assert.True(buffer.CompletePeripheralCycle());
        sequencer.ClockBit(false);
        Assert.Equal(0xa5, ram[0]);
        Assert.Equal(1ul, sequencer.TransferByteCount);
        sequencer.Start(0);
        for (var bit = 7; bit >= 0; bit--) sequencer.ClockBit((0x3c & (1 << bit)) != 0);
        Assert.True(buffer.PeripheralCyclePending);
        sequencer.ResetEngine();
        Assert.False(buffer.CompletePeripheralCycle());
        Assert.Equal(1, buffer.WritePointer);
        Assert.Equal(0, ram[1]);
    }

    [Fact]
    public void RequestsDoNotTouchRamOrPointersUntilThePeripheralCycleIsGranted()
    {
        var ram = new byte[1024];
        var buffer = CreateBuffer(ram);
        buffer.RequestBufferWrite(0xa5);
        Assert.Equal(0, ram[0]);
        Assert.Equal(0, buffer.WritePointer);
        Assert.False(buffer.TryCompleteBufferWrite());
        Assert.True(buffer.CompletePeripheralCycle());
        Assert.Equal(0xa5, ram[0]);
        Assert.Equal(1, buffer.WritePointer);
        Assert.True(buffer.TryCompleteBufferWrite());
        Assert.False(buffer.TryCompleteBufferWrite());
        buffer.RequestBufferRead();
        Assert.Equal(0, buffer.ReadPointer);
        Assert.False(buffer.TryCompleteBufferRead(out _));
        Assert.True(buffer.CompletePeripheralCycle());
        Assert.Equal(1, buffer.ReadPointer);
        Assert.True(buffer.TryCompleteBufferRead(out var value));
        Assert.Equal(0xa5, value);
        Assert.False(buffer.TryCompleteBufferRead(out _));
    }

    [Fact]
    public void UnservicedInputIsALatchRatherThanAnInventedSectorQueue()
    {
        var ram = new byte[1024];
        var buffer = CreateBuffer(ram);
        buffer.RequestBufferWrite(0x11);
        buffer.RequestBufferWrite(0x22);
        Assert.True(buffer.CompletePeripheralCycle());
        Assert.Equal(0x22, ram[0]);
        Assert.Equal(1, buffer.WritePointer);
        Assert.False(buffer.CompletePeripheralCycle());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancelAndResetDiscardPendingCyclesWithoutChangingRam(bool reset)
    {
        var ram = Enumerable.Repeat((byte)0x5a, 1024).ToArray();
        var buffer = CreateBuffer(ram);
        buffer.RequestBufferWrite(0xa5);
        if (reset) buffer.TryWrite(0x59, 1);
        else buffer.CancelBufferTransfers();
        Assert.False(buffer.PeripheralCyclePending);
        Assert.False(buffer.CompletePeripheralCycle());
        Assert.All(ram, value => Assert.Equal(0x5a, value));
        Assert.Equal(0, buffer.WritePointer);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WritePreloadsTheNextByteWithoutStoppingTheSerialClock(bool late)
    {
        byte[] ram = new byte[1024];
        ram[0] = 0xa5;
        ram[1] = 0x3c;
        var buffer = CreateBuffer(ram);
        var sequencer = new Acb4000Sequencer(Acb4000EccRegisters.CreateFirmwareConfiguration(), buffer);
        sequencer.ConfigureClockedBufferTransfers(true);
        Program(sequencer, 0, 1, 0x80, 0, 0);
        Program(sequencer, 1, 2, 0, 0, 0xf8);
        Program(sequencer, 2, 3, 0x05, 1, 0);
        Program(sequencer, 3, 0x1f, 0x20, 0, 0);
        sequencer.Start(0);
        var data = new List<byte>();
        byte current = 0;
        for (var bit = 0; bit < 32; bit++)
        {
            var state = sequencer.State;
            if (late && state == 2 && sequencer.FieldDataByteCount == 1 && sequencer.BitRing == 0)
                buffer.CompletePeripheralCycle();
            sequencer.ClockBit(false);
            if (!late && bit % 8 == 4) buffer.CompletePeripheralCycle();
            if (state == 2)
            {
                current = (byte)((current << 1) | (sequencer.NrzOutput ? 1 : 0));
                if (sequencer.OutputBitRing == 7) data.Add(current);
            }
        }
        Assert.Equal(late ? new byte[] { 0, 0xa5 } : new byte[] { 0xa5, 0x3c }, data);
        Assert.Equal(late ? 1 : 2, buffer.ReadPointer);
        Assert.Equal(late ? 1ul : 2ul, sequencer.TransferByteCount);
        Assert.False(buffer.PeripheralCyclePending);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReadDepositsOnlyCompletedBytesAtTheGrantedClock(bool suppress)
    {
        var ram = new byte[1024];
        var buffer = CreateBuffer(ram);
        var sequencer = new Acb4000Sequencer(Acb4000EccRegisters.CreateFirmwareConfiguration(), buffer);
        sequencer.ConfigureClockedBufferTransfers(true);
        Program(sequencer, 0, 1, 0x41, 1, 0);
        Program(sequencer, 1, 0x1f, 0, 1, 0);
        if (suppress) sequencer.WriteOperation(0x20);
        sequencer.Start(0);
        foreach (var value in new byte[] { 0xa5, 0x3c })
        {
            var before = buffer.WritePointer;
            for (var bit = 7; bit >= 0; bit--) sequencer.ClockBit((value & (1 << bit)) != 0);
            Assert.Equal(before, buffer.WritePointer);
            Assert.Equal(!suppress, buffer.PeripheralCyclePending);
            Assert.Equal(!suppress, buffer.CompletePeripheralCycle());
        }
        sequencer.ClockBit(false);
        Assert.Equal(suppress ? 0 : 2, buffer.WritePointer);
        Assert.Equal(suppress ? 0ul : 2ul, sequencer.TransferByteCount);
        Assert.Equal(suppress ? new byte[] { 0, 0 } : new byte[] { 0xa5, 0x3c }, ram.AsSpan(0, 2).ToArray());
    }

    private static Acb4000BufferController CreateBuffer(byte[] ram)
    {
        var result = new Acb4000BufferController(ram);
        result.TryWrite(0x59, 0);
        result.TryWrite(0x54, 3);
        result.ConfigureClockedTransfers(true);
        return result;
    }

    private static void Program(Acb4000Sequencer sequencer, byte state, byte next, byte control, byte count, byte data)
    {
        sequencer.TryWriteProgram((byte)(0x80 + state), next);
        sequencer.TryWriteProgram((byte)(0xa0 + state), control);
        sequencer.TryWriteProgram((byte)(0xc0 + state), count);
        sequencer.TryWriteProgram((byte)(0xe0 + state), data);
    }
}

using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed partial class Acb4000CommandTests
{
    [Fact]
    public void NativeHighImpedanceClocksPreventRamTransfersUntilOutputsAreRestored()
    {
        var target = CreateManualTarget();
        SendCommand(target, Short(8, count: 1));
        target.Aic.TryWrite(0x7f, 0x0f);
        var payload = Enumerable.Repeat((byte)0xa5, 256).ToArray();
        PhysicalPair(target, PhysicalId(0), PhysicalData(payload));
        Assert.Equal(0, target.BufferController.WritePointer);
        Assert.False(target.Bus.Request);
        Assert.True(target.ReadGate);
        target.Aic.TryWrite(0x7f, 0x07);
        PhysicalPair(target, PhysicalId(0), PhysicalData(payload));
        Assert.Equal(payload, DrainRead(target, out var status));
        Assert.Equal(0, status);
    }

    [Theory]
    [InlineData(256)]
    [InlineData(512)]
    [InlineData(1024)]
    public void ReadWaitsForPhysicalCellsAndTransfersTheirPayloadInsteadOfLogicalStorage(ushort size)
    {
        var disk = new Disk(size);
        var target = CreateManualTarget(disk);
        SendCommand(target, Short(8, address: 2, count: 1));
        Assert.True(target.ReadGate);
        Assert.False(target.Bus.Request);
        for (var tick = 0; tick < 100; tick++) target.Tick();
        Assert.False(target.Bus.Request);
        var data = Enumerable.Range(0, size).Select(i => (byte)(i * 7 + 3)).ToArray();
        PhysicalPair(target, PhysicalId(2), PhysicalData(data));
        Assert.False(target.ReadGate);
        Assert.Equal(data, DrainRead(target, out var status));
        Assert.Equal(0, status);
        Assert.Equal(0, disk.Reads);
        Assert.NotEqual(disk.Bytes.AsSpan(2 * size, size).ToArray(), data);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReadCorrectsOnlyAfterSixFailedPhysicalDataReads(bool extended)
    {
        var target = CreateManualTarget();
        SendCommand(target, extended ? Long(0x28, address: 3, count: 1) : Short(8, address: 3, count: 1));
        var payload = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
        var damaged = PhysicalData(payload);
        damaged[5] ^= 0x80;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            PhysicalPair(target, PhysicalId(3), damaged);
            Assert.True(target.ReadGate);
            Assert.False(target.Bus.Request);
        }
        PhysicalPair(target, PhysicalId(3), damaged);
        Assert.Equal(payload, DrainRead(target, out var status));
        Assert.Equal(0, status);
        Assert.Equal((byte)(payload[3] ^ 0x80), damaged[5]);
    }

    [Fact]
    public void UncorrectablePhysicalDataIsTransferredBeforeCheck()
    {
        var target = CreateManualTarget();
        SendCommand(target, Short(8, address: 4, count: 1));
        var payload = new byte[256];
        var damaged = PhysicalData(payload);
        damaged[2] ^= 0x80;
        damaged[7] ^= 0x80;
        for (var attempt = 0; attempt < 6; attempt++) PhysicalPair(target, PhysicalId(4), damaged);
        Assert.Equal(damaged.AsSpan(2, 256).ToArray(), DrainRead(target, out var status));
        Assert.Equal(2, status);
        Assert.Equal(new byte[] { 0x91, 0, 0, 4 }, Run(target, Short(3, count: 4)).Data);
    }

    [Fact]
    public void VerifyRejectsEvenCorrectableEccWithoutRetryOrHostData()
    {
        var target = CreateManualTarget();
        SendCommand(target, Long(0x2f, address: 4, count: 1));
        target.Tick();
        var damaged = PhysicalData(new byte[256]);
        damaged[5] ^= 0x80;
        PhysicalPair(target, PhysicalId(4), damaged);
        Assert.Empty(DrainRead(target, out var status));
        Assert.Equal(2, status);
        Assert.Equal(new byte[] { 0x99, 0, 0, 4 }, Run(target, Short(3, count: 4)).Data);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void DataRequiresAnImmediatelyPrecedingValidMatchingNondefectiveId(int interruption)
    {
        var target = CreateManualTarget();
        SendCommand(target, Short(8, address: 5, count: 1));
        PhysicalField(target, PhysicalId(5));
        var intervening = PhysicalId(6);
        if (interruption == 1) intervening = PhysicalId(5, defective: true);
        if (interruption == 2) { intervening = PhysicalId(5); intervening[^1] ^= 1; }
        if (interruption == 3) intervening = PhysicalId(5)[..4];
        PhysicalField(target, intervening);
        PhysicalField(target, PhysicalData(new byte[256]));
        Assert.True(target.ReadGate);
        Assert.False(target.Bus.Request);
        var payload = Enumerable.Repeat((byte)0x6c, 256).ToArray();
        PhysicalPair(target, PhysicalId(5), PhysicalData(payload));
        Assert.Equal(payload, DrainRead(target, out var status));
        Assert.Equal(0, status);
    }

    [Fact]
    public void ResetDropsAPartialPhysicalFieldAndClosesTheReadGate()
    {
        var target = CreateManualTarget();
        SendCommand(target, Short(8, count: 1));
        PhysicalField(target, PhysicalId(0));
        PhysicalField(target, PhysicalData(new byte[256])[..40]);
        target.Reset();
        Assert.False(target.ReadGate);
        Assert.False(target.Bus.Busy);
        PhysicalPair(target, PhysicalId(0), PhysicalData(new byte[256]));
        Assert.False(target.Bus.Busy);
        SendCommand(target, Short(8, count: 1));
        PhysicalField(target, PhysicalData(new byte[256]));
        Assert.True(target.ReadGate);
        PhysicalPair(target, PhysicalId(0), PhysicalData(new byte[256]));
        Assert.Equal(new byte[256], DrainRead(target, out var status));
        Assert.Equal(0, status);
    }

    private static byte[] PhysicalId(uint address, bool defective = false)
    {
        var bytes = new byte[10];
        Acb4000FieldCodec.WriteId(bytes, address, 0, defective);
        return bytes;
    }

    private static byte[] PhysicalData(byte[] payload)
    {
        var bytes = new byte[payload.Length + 6];
        Acb4000FieldCodec.WriteData(bytes, payload);
        return bytes;
    }

    private static void PhysicalPair(Acb4000 target, byte[] id, byte[] data)
    {
        PhysicalField(target, id);
        PhysicalField(target, data);
    }

    private static void PhysicalField(Acb4000 target, ReadOnlySpan<byte> bytes)
    {
        const int preambleWords = 8;
        const int postambleWords = 1;
        const int cellsPerControllerTick = 5;
        var words = new ushort[bytes.Length + preambleWords + postambleWords];
        words.AsSpan(0, preambleWords).Fill(0xaaaa);
        words.AsSpan(preambleWords + bytes.Length).Fill(0xaaaa);
        var previous = false;
        Acb4000MfmEncoding.WriteField(bytes, words.AsSpan(preambleWords), ref previous);
        var cells = 0;
        foreach (var word in words)
            for (var bit = 15; bit >= 0; bit--)
            {
                target.ClockReadCell((word & (1 << bit)) != 0);
                if (++cells % cellsPerControllerTick == 0) target.Tick();
            }
    }

    private static byte[] DrainRead(Acb4000 target, out byte status)
    {
        var bytes = new List<byte>();
        while (true)
        {
            WaitRequest(target);
            if (target.Bus.Phase != Acb4000BusPhase.DataIn) break;
            bytes.Add(target.Bus.OutputData);
            Transfer(target, 0);
        }
        Assert.Equal(Acb4000BusPhase.Status, target.Bus.Phase);
        status = target.Bus.OutputData;
        Transfer(target, 0);
        WaitRequest(target);
        Assert.Equal(Acb4000BusPhase.MessageIn, target.Bus.Phase);
        Transfer(target, 0);
        Assert.False(target.Bus.Busy);
        return bytes.ToArray();
    }
}

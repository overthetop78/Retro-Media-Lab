using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Interfaces;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed class Acb4000AicRegisterTests
{
    [Fact]
    public void RegisterDirectionsAndProgramRamDoNotAliasExternalPortsOrHoles()
    {
        var aic = Create();
        foreach (var register in new byte[] { 0x50, 0x51, 0x6e, 0x6f, 0x70, 0x98, 0xb8, 0xd8, 0xf8, 0xff })
        {
            Assert.False(aic.TryRead(register, out _));
            Assert.False(aic.TryWrite(register, 0xa5));
        }
        foreach (var register in new byte[] { 0x71, 0x74, 0x75, 0x76, 0x77, 0x7b, 0x7c, 0x7d })
            Assert.False(aic.TryRead(register, out _));
        foreach (var register in new byte[] { 0x80, 0x97, 0xa0, 0xb7, 0xc0, 0xd7, 0xe0, 0xf7 })
        {
            Assert.True(aic.TryWrite(register, register));
            Assert.True(aic.TryRead(register, out var value));
            Assert.Equal(register, value);
        }
    }

    [Fact]
    public void GpioReadsPhysicalLevelsAndKeepsOutputLatchesWhenDirectionChanges()
    {
        var aic = Create();
        aic.SetGpioInputs(0x0a);
        Assert.True(aic.TryWrite(0x7e, 0x05));
        Assert.True(aic.TryRead(0x7e, out var value));
        Assert.Equal(0x0a, value);
        aic.TryWrite(0x7d, 0x0f);
        Assert.Equal(0x0f, aic.GpioOutputEnable);
        Assert.Equal(0x05, aic.GpioOutputLevels);
        aic.TryRead(0x7e, out value);
        Assert.Equal(0x05, value);
        aic.TryWrite(0x7d, 0x00);
        aic.TryRead(0x7e, out value);
        Assert.Equal(0x0a, value);
        aic.TryWrite(0x7d, 0x0f);
        Assert.Equal(0x05, aic.GpioOutputLevels);
    }

    [Theory]
    [InlineData(0x6e, true, 0)]
    [InlineData(0x6e, false, 1)]
    [InlineData(0x6f, true, 2)]
    [InlineData(0x6f, false, 3)]
    public void ExternalDecodesNeedBothDirectionAndDecodeEnable(byte register, bool writing, int pin)
    {
        var aic = Create();
        var mask = 1 << pin;
        aic.TryWrite(0x7d, (byte)(mask << 4));
        Assert.Equal(0, aic.DecodeExternalCycle(register, writing));
        aic.TryWrite(0x7d, (byte)mask);
        Assert.Equal(0, aic.DecodeExternalCycle(register, writing));
        aic.TryWrite(0x7d, (byte)(mask | (mask << 4)));
        Assert.Equal(mask, aic.DecodeExternalCycle(register, writing));
        Assert.Equal(0, aic.GpioOutputEnable & mask);
        Assert.Equal(0, aic.DecodeExternalCycle(register, !writing));
        Assert.Equal(0, aic.DecodeExternalCycle(0x70, writing));
        Assert.Equal(mask, aic.PinOutputEnable & mask);
        Assert.Equal(mask, aic.PinOutputLevels & mask);
        aic.BeginExternalCycle(register, writing);
        Assert.Equal(0, aic.PinOutputLevels & mask);
        aic.TryRead(0x7e, out var sampled);
        Assert.Equal(0, sampled & mask);
        aic.EndExternalCycle();
        Assert.Equal(mask, aic.PinOutputLevels & mask);
        aic.TryRead(0x7e, out sampled);
        Assert.Equal(mask, sampled & mask);
        aic.TryWrite(0x71, 0xe0);
        Assert.Equal(0, aic.PinOutputEnable);
        Assert.Equal(0, aic.PinOutputLevels);
    }

    [Fact]
    public void BranchAndSequencerOutputAreInputStatusRatherThanGpioLatches()
    {
        var aic = Create();
        aic.TryWrite(0x80, 0x1f);
        aic.TryWrite(0xa0, 0x04);
        aic.TryWrite(0x7e, 0xff);
        aic.SetBranchInput(true);
        aic.TryWrite(0x79, 0);
        aic.TryRead(0x7e, out var value);
        Assert.Equal(0x30, value);
        for (var bit = 0; bit < 8; bit++) aic.Sequencer.ClockBit(false);
        aic.TryRead(0x7e, out value);
        Assert.Equal(0x10, value);
        aic.SetBranchInput(false);
        aic.TryRead(0x7e, out value);
        Assert.Equal(0, value);
    }

    [Fact]
    public void EccResetStopsEngineWithoutOverwritingWrittenLengthOrPolynomial()
    {
        var aic = Create();
        aic.Clocks.SelectReference(true);
        aic.Clocks.TryRequestTransfer();
        aic.TryWrite(0x80, 0);
        aic.TryWrite(0x79, 0);
        Assert.False(aic.Sequencer.Stopped);
        aic.TryWrite(0x71, 0xe0);
        Assert.False(aic.Clocks.ReferenceSelected);
        Assert.False(aic.Clocks.TransferPending);
        Assert.True(aic.Clocks.ClockB);
        Assert.Equal(0xe0, aic.Ecc.Control);
        Assert.True(aic.Sequencer.Stopped);
        Assert.Equal(0x41044185u, aic.Ecc.Polynomial);
        aic.TryWrite(0x79, 0);
        Assert.True(aic.Sequencer.Stopped);
        aic.TryWrite(0x71, 0xc0);
        aic.TryWrite(0x79, 0);
        Assert.False(aic.Sequencer.Stopped);
    }

    [Fact]
    public void StackReadDoesNotReturnClockControlAndPopsOnlyAvailableBytes()
    {
        var aic = Create();
        aic.TryWrite(0x80, 0x1f);
        aic.TryWrite(0xa0, 0x50);
        aic.TryWrite(0x7f, 0xdb);
        Assert.False(aic.TryRead(0x7f, out _));
        aic.TryWrite(0x79, 0);
        for (var bit = 7; bit >= 0; bit--) aic.Sequencer.ClockBit((0x93 & (1 << bit)) != 0);
        Assert.True(aic.TryRead(0x7f, out var value));
        Assert.Equal(0x93, value);
        Assert.False(aic.TryRead(0x7f, out _));
        Assert.Equal(0xdb, aic.ClockControl);
    }

    [Fact]
    public void ClockAndMarkControlsRemainSeparateAndExposeOnlyDocumentedSelections()
    {
        var aic = Create();
        aic.TryWrite(0x7b, 0xa5);
        aic.TryWrite(0x7c, 0x5a);
        for (var start = 0; start < 8; start++)
        {
            aic.TryWrite(0x7f, (byte)start);
            Assert.Equal(start, aic.CompareStartBit);
            Assert.True(aic.ClockOutputsEnabled);
            Assert.Equal(2, aic.TransferClocksPerByte);
        }
        aic.TryWrite(0x7f, 0xd8);
        Assert.False(aic.ClockOutputsEnabled);
        Assert.Equal(4, aic.TransferClocksPerByte);
        Assert.False(aic.HasStableNonTransferClock);
        Assert.Equal(0xa5, aic.WriteAddressMarkControl);
        Assert.Equal(0x5a, aic.DetectAddressMarkControl);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResetClearsGpioAndMarksButKeepsProgramTapsClockAndBorrowedPins(bool external)
    {
        var aic = Create();
        aic.TryWrite(0x80, 0x1f);
        aic.TryWrite(0xa0, 0x04);
        aic.TryWrite(0x7b, 0x10);
        aic.TryWrite(0x7c, 0xa1);
        aic.TryWrite(0x7d, 0x0f);
        aic.TryWrite(0x7e, 0x0f);
        aic.TryWrite(0x7f, 0x87);
        aic.SetGpioInputs(0x05);
        aic.SetBranchInput(true);
        aic.TryWrite(0x79, 0);
        if (external) aic.Reset();
        else aic.TryWrite(0x71, 0xe0);
        Assert.True(aic.Ecc.ResetAsserted);
        Assert.True(aic.Sequencer.Stopped);
        Assert.Equal(0, aic.GpioOutputEnable);
        Assert.Equal(0, aic.GpioOutputLevels);
        Assert.Equal(0, aic.WriteAddressMarkControl);
        Assert.Equal(0, aic.DetectAddressMarkControl);
        Assert.Equal(0x87, aic.ClockControl);
        Assert.Equal(0x41044185u, aic.Ecc.Polynomial);
        aic.TryRead(0x80, out var value);
        Assert.Equal(0x1f, value);
        aic.TryRead(0x7e, out value);
        Assert.Equal(0x15, value);
    }

    [Fact]
    public void HeldResetRejectsOutputAndControlLatchesUntilReleaseButProgramIsWritable()
    {
        var aic = Create();
        aic.TryWrite(0x71, 0xe0);
        foreach (var register in new byte[] { 0x78, 0x79, 0x7a, 0x7b, 0x7c, 0x7d, 0x7e })
            Assert.True(aic.TryWrite(register, 0xff));
        aic.TryWrite(0x80, 0x1f);
        aic.TryWrite(0xa0, 0x04);
        aic.TryWrite(0x7f, 0x87);
        Assert.Equal(0, aic.WriteAddressMarkControl);
        Assert.Equal(0, aic.DetectAddressMarkControl);
        Assert.Equal(0, aic.GpioOutputEnable);
        aic.TryRead(0x7a, out var value);
        Assert.Equal(8, value); // Fixed unused bit; no writable operation bits survived.
        aic.TryWrite(0x71, 0xc0);
        aic.TryWrite(0x79, 0);
        Assert.False(aic.Sequencer.Stopped);
        Assert.True(aic.Sequencer.Output);
        Assert.Equal(0x87, aic.ClockControl);
    }

    [Fact]
    public void AddressMarkNeedsAmdAndOnlyTheProgrammedCompareBitsAreSignificant()
    {
        var aic = Create();
        aic.TryWrite(0x80, 0x1f);
        aic.TryWrite(0xa0, 0x42);
        aic.TryWrite(0xc0, 0x80);
        aic.TryWrite(0xe0, 0xa1);
        aic.TryWrite(0x7c, 0xa1);
        aic.TryWrite(0x79, 0);
        Assert.False(aic.TrySynchronizeRead(0xa1, addressMarkDetected: false));
        Assert.False(aic.TrySynchronizeRead(0xaf, addressMarkDetected: true));
        Assert.True(aic.Sequencer.WaitingForReadSync);
        Assert.True(aic.TrySynchronizeRead(0xa1, addressMarkDetected: true));
        Assert.True(aic.Sequencer.Stopped);
        aic.TryWrite(0x79, 0);
        aic.TryWrite(0x7f, 7);
        Assert.False(aic.TrySynchronizeRead(0x21, addressMarkDetected: true));
        Assert.True(aic.TrySynchronizeRead(0xaf, addressMarkDetected: true));
    }

    private static Acb4000AicRegisters Create() =>
        new(Acb4000EccRegisters.CreateFirmwareConfiguration(), new Buffer());

    private sealed class Buffer : IAcb4000SequencerBus
    {
        public byte ReadBuffer() => throw new InvalidOperationException("Unexpected transfer");
        public void WriteBuffer(byte value) => throw new InvalidOperationException("Unexpected transfer");
    }
}

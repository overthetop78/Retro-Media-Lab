using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000BufferConstants;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed class Acb4000HostInterfaceTests
{
    [Fact]
    public void ReadCombinesOutputLatchesWithBorrowedInputsAndBusyReadback()
    {
        var controller = Create();
        controller.SetHostBusInputs(true, true);
        controller.TryWrite(C.HostInterfaceControl, byte.MaxValue);
        Assert.Equal(0xfd, Read(controller));
        Assert.True(controller.HostArbitrationPending);
        Assert.True(controller.HostSelectOutput);
        Assert.True(controller.HostBusyOutput);
        Assert.True(controller.HostInputEnabled);
        Assert.True(controller.HostOutputEnabled);
        controller.TryWrite(C.HostInterfaceControl, 0);
        Assert.Equal(C.HostSelectInput | C.HostBusyInput, Read(controller));
        Assert.False(controller.HostArbitrationPending);
        Assert.False(controller.HostSelectOutput);
        Assert.False(controller.HostBusyOutput);
        controller.SetHostBusInputs(false, false);
        Assert.Equal(0, Read(controller));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void ArbitrationWaitsForBothInputsThenForTheFullDelay(bool select, bool busy)
    {
        var controller = Create();
        controller.SetHostBusInputs(select, busy);
        controller.TryWrite(C.HostInterfaceControl, C.HostArbitrate);
        controller.AdvanceHostInterfaceNanoseconds(uint.MaxValue);
        Assert.True(controller.HostArbitrationPending);
        Assert.False(controller.HostBusyOutput);
        Assert.Equal((select ? C.HostSelectInput : 0) | (busy ? C.HostBusyInput : 0), Read(controller));
        controller.SetHostBusInputs(false, false);
        controller.AdvanceHostInterfaceNanoseconds(99);
        Assert.True(controller.HostArbitrationPending);
        Assert.Equal(0, Read(controller));
        controller.AdvanceHostInterfaceNanoseconds(1);
        Assert.False(controller.HostArbitrationPending);
        Assert.Equal(C.HostBusyOutput | C.HostOutputEnable | C.HostArbitrate, Read(controller));
        Assert.True(controller.HostOutputEnabled);
        Assert.False(controller.HostSelectOutput);
    }

    [Fact]
    public void ActivityDuringTheDelayRequiresANewUninterruptedInterval()
    {
        var controller = Create();
        controller.TryWrite(C.HostInterfaceControl, C.HostArbitrate);
        controller.AdvanceHostInterfaceNanoseconds(99);
        controller.SetHostBusInputs(false, true);
        controller.AdvanceHostInterfaceNanoseconds(uint.MaxValue);
        controller.SetHostBusInputs(false, false);
        controller.AdvanceHostInterfaceNanoseconds(1);
        Assert.False(controller.HostBusyOutput);
        controller.AdvanceHostInterfaceNanoseconds(99);
        Assert.True(controller.HostBusyOutput);
    }

    [Fact]
    public void ReprogrammingCancelsThePendingArbitrationAndLargeDurationsDoNotOverflow()
    {
        var controller = Create();
        controller.TryWrite(C.HostInterfaceControl, C.HostArbitrate);
        controller.AdvanceHostInterfaceNanoseconds(99);
        controller.TryWrite(C.HostInterfaceControl, C.HostSelectOutput);
        controller.AdvanceHostInterfaceNanoseconds(uint.MaxValue);
        Assert.Equal(C.HostSelectOutput, Read(controller));
        controller.TryWrite(C.HostInterfaceControl, C.HostArbitrate);
        controller.AdvanceHostInterfaceNanoseconds(0);
        Assert.False(controller.HostBusyOutput);
        controller.AdvanceHostInterfaceNanoseconds(uint.MaxValue);
        Assert.True(controller.HostBusyOutput);
    }

    [Fact]
    public void PointerResetPreservesControlButHeldResetClearsOutputsAndKeepsInputs()
    {
        var controller = Create();
        controller.SetHostBusInputs(true, true, true);
        controller.TryWrite(C.HostInterfaceControl, C.HostSelectOutput | C.HostInputEnable | C.HostArbitrate);
        controller.TryWrite(C.ResetControl, 0);
        Assert.True(controller.HostSelectOutput);
        Assert.True(controller.HostInputEnabled);
        Assert.True(controller.HostArbitrationPending);
        controller.TryWrite(C.ResetControl, C.HoldReset);
        controller.TryWrite(C.HostInterfaceControl, byte.MaxValue);
        controller.AdvanceHostInterfaceNanoseconds(uint.MaxValue);
        Assert.False(controller.HostSelectOutput);
        Assert.False(controller.HostBusyOutput);
        Assert.False(controller.HostInputEnabled);
        Assert.False(controller.HostOutputEnabled);
        Assert.False(controller.HostArbitrationPending);
        Assert.Equal(C.HostSelectInput | C.HostBusyInput, Read(controller));
        controller.TryWrite(C.ResetControl, 0);
        Assert.Equal(C.HostSelectInput | C.HostBusyInput, Read(controller));
    }

    [Fact]
    public void DmaOutputEnablesAfterRamReadAndReleasesOnAcknowledgeUnlessForced()
    {
        var controller = Create();
        controller.TryWrite(C.DmaControl, C.ReadPeripheral | C.ReadLatch);
        Assert.False(controller.HostOutputEnabled);
        Assert.True(controller.TryBeginHostRead(out _));
        Assert.True(controller.HostOutputEnabled);
        controller.SetHostBusInputs(false, false, true);
        Assert.False(controller.HostOutputEnabled);
        controller.SetHostBusInputs(false, false, false);
        Assert.False(controller.HostOutputEnabled);
        controller.TryWrite(C.HostInterfaceControl, C.HostOutputEnable);
        Assert.True(controller.HostOutputEnabled);
        controller.TryWrite(C.HostInterfaceControl, 0);
        controller.CompleteHostAcknowledgement();
        controller.SetHostBusInputs(false, false);
        Assert.False(controller.HostOutputEnabled);
    }

    [Fact]
    public void ManualInputEnableSurvivesTheEndOfDmaUntilExplicitlyCleared()
    {
        var controller = Create();
        controller.TryWrite(C.DmaControl, C.WriteLatch);
        Assert.True(controller.HostInputEnabled);
        controller.TryWrite(C.HostInterfaceControl, C.HostInputEnable);
        Assert.True(controller.TryBeginHostWrite(0xa5));
        controller.CompleteHostAcknowledgement();
        Assert.True(controller.HostInputEnabled);
        controller.TryWrite(C.HostInterfaceControl, 0);
        Assert.False(controller.HostInputEnabled);
    }

    private static Acb4000BufferController Create()
    {
        var controller = new Acb4000BufferController(new byte[1024]);
        Assert.True(controller.TryWrite(C.ResetControl, 0));
        return controller;
    }

    private static byte Read(Acb4000BufferController controller)
    {
        Assert.True(controller.TryRead(C.HostInterfaceControl, out var value));
        return value;
    }
}

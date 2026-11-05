using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;

namespace RetroMediaLab.Tests.Emulation;

public sealed partial class AcornEmuWinchesterStorageTests
{
    [Fact]
    public void RelayWithNoAttachedMechanismReportsNotReady()
    {
        var target = new Acb4000(_ => null, 2_000_000);
        var relay = new WinchesterTrackSignals(target, _ => null);
        try
        {
            Assert.Equal(2, RunSignalCommand(target, relay, [0, 0, 0, 0, 0, 0]));
        }
        finally { target.Reset(); relay.Reset(); }
    }

    [Fact]
    public void InvalidLunDoesNotIndexAStateForANonexistentPhysicalPort()
    {
        var target = new Acb4000(_ => null, 2_000_000);
        var relay = new WinchesterTrackSignals(target, _ => null);
        try { Assert.Equal(2, RunSignalCommand(target, relay, [0, 0xe0, 0, 0, 0, 0])); }
        finally { target.Reset(); relay.Reset(); }
    }

    [Fact]
    public void SeekMovesTheHeadOnceAndLeavesThePortBusyUntilTheMechanismSettles()
    {
        using var image = new Image(256, 33);
        var storage = SignalStorage(image);
        var target = new Acb4000(unit => unit == 0 ? storage : null, 2_000_000);
        var relay = new WinchesterTrackSignals(target, unit => unit == 0 ? storage : null);
        try
        {
            Assert.Equal(0, RunSignalCommand(target, relay, [0x1a, 0, 0, 0, 22, 0]));
            var calibration = image.Drive.SeekCalls;
            Assert.Equal(0, RunSignalCommand(target, relay, [0x0b, 0, 0, 66, 0, 0]));
            Assert.Equal(1, storage.Cylinder);
            Assert.Equal(1, image.Drive.Position.Cylinder);
            Assert.Equal(calibration + 1, image.Drive.SeekCalls);
            Assert.Equal(8, RunSignalCommand(target, relay, [0, 0, 0, 0, 0, 0]));
            for (var tick = 0; tick < 30_000; tick++) relay.Tick();
            Assert.Equal(0, RunSignalCommand(target, relay, [0, 0, 0, 0, 0, 0]));
            Assert.Equal(calibration + 1, image.Drive.SeekCalls);
            Assert.Equal(1, image.Drive.MotorCalls);
            Assert.False(image.Disposed);
        }
        finally { target.Reset(); relay.Reset(); }
    }

    [Fact]
    public void RezeroConsumesRealDirectionAndTrackZeroSignalsWithoutOwningTheImage()
    {
        using var image = new Image(256, 33);
        var storage = SignalStorage(image);
        var target = new Acb4000(unit => unit == 0 ? storage : null, 2_000_000);
        var relay = new WinchesterTrackSignals(target, unit => unit == 0 ? storage : null);
        try
        {
            Assert.Equal(0, RunSignalCommand(target, relay, [1, 0, 0, 0, 0, 0]));
            Assert.Equal(0, storage.Cylinder);
            Assert.Equal(32, image.Drive.SeekCalls);
            relay.Reset();
            Assert.False(image.Disposed);
        }
        finally { target.Reset(); relay.Reset(); }
    }

    [Fact]
    public void ResetPreservesAnIssuedDisplacementAndItsSettlingOnOnlyTheAffectedPort()
    {
        using var first = new Image(256, 33);
        using var second = new Image(256, 33);
        WinchesterStorage[] stores = [SignalStorage(first), SignalStorage(second)];
        var target = new Acb4000(unit => stores[unit], 2_000_000);
        var relay = new WinchesterTrackSignals(target, unit => stores[unit]);
        try
        {
            Assert.Equal(0, RunSignalCommand(target, relay, [0x1a, 0x20, 0, 0, 22, 0]));
            var calibration = second.Drive.SeekCalls;
            Assert.Equal(0, RunSignalCommand(target, relay, [0x0b, 0x20, 0, 66, 0, 0]));
            target.Reset();
            relay.Reset();
            Assert.Equal(1, stores[1].Cylinder);
            Assert.Equal(0, stores[0].Cylinder);
            Assert.Equal(0, RunSignalCommand(target, relay, [0, 0, 0, 0, 0, 0]));
            SendSignalCommand(target, relay, [0x0b, 0x20, 0, 0, 0, 0]);
            for (var tick = 0; tick < 100; tick++) relay.Tick();
            Assert.Equal(1, stores[1].Cylinder);
            Assert.Equal(calibration + 1, second.Drive.SeekCalls);
            Assert.Equal(0, ReceiveSignalStatus(target, relay));
            Assert.Equal(0, stores[1].Cylinder);
            Assert.Equal(calibration + 2, second.Drive.SeekCalls);
            Assert.False(first.Disposed);
            Assert.False(second.Disposed);
        }
        finally { target.Reset(); relay.Reset(); }
    }

    private static WinchesterStorage SignalStorage(Image image)
    {
        var parameters = new byte[22];
        (Acb4000DriveParameters.Default with { Cylinders = 2, Heads = 2 }).Write(parameters);
        image.SetParameters(Convert.ToBase64String(parameters));
        return new WinchesterStorage(image);
    }

    private static byte RunSignalCommand(Acb4000 target, WinchesterTrackSignals relay, byte[] command)
    {
        SendSignalCommand(target, relay, command);
        return ReceiveSignalStatus(target, relay);
    }

    private static void SendSignalCommand(Acb4000 target, WinchesterTrackSignals relay, byte[] command)
    {
        target.SetInitiator(1, true, false, false);
        for (var tick = 0; tick < 10 && !target.Bus.Busy; tick++) relay.Tick();
        Assert.True(target.Bus.Busy);
        target.SetInitiator(0, false, false, false);
        foreach (var value in command)
        {
            WaitSignalRequest(target, relay);
            Assert.Equal(Acb4000BusPhase.Command, target.Bus.Phase);
            TransferSignalByte(target, relay, value);
        }
    }

    private static byte ReceiveSignalStatus(Acb4000 target, WinchesterTrackSignals relay)
    {
        WaitSignalRequest(target, relay);
        while (target.Bus.Phase == Acb4000BusPhase.DataIn)
        {
            TransferSignalByte(target, relay, 0);
            WaitSignalRequest(target, relay);
        }
        Assert.Equal(Acb4000BusPhase.Status, target.Bus.Phase);
        var status = target.Bus.OutputData;
        TransferSignalByte(target, relay, 0);
        WaitSignalRequest(target, relay);
        Assert.Equal(Acb4000BusPhase.MessageIn, target.Bus.Phase);
        TransferSignalByte(target, relay, 0);
        Assert.False(target.Bus.Busy);
        return status;
    }

    private static void WaitSignalRequest(Acb4000 target, WinchesterTrackSignals relay)
    {
        for (var tick = 0; tick < 2_000_000 && !target.Bus.Request; tick++) relay.Tick();
        Assert.True(target.Bus.Request);
    }

    private static void TransferSignalByte(Acb4000 target, WinchesterTrackSignals relay, byte value)
    {
        target.SetInitiator(value, false, true, false);
        target.SetInitiator(value, false, false, false);
        relay.Tick();
    }
}

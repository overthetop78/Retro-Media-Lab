using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Enums;
using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;
using C = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Constants.Via6522Constants;

namespace RetroMediaLab.Chips.Tests.IO.MOS.MCS6500.Chip6522;

public sealed class Via6522PinTests
{
    [Fact]
    public void EveryPcrCombinationExposesWhichControlPinsAreDriven()
    {
        foreach (var a in Enum.GetValues<ViaControlMode>())
        foreach (var b in Enum.GetValues<ViaControlMode>())
        {
            var via = new Via6522();
            var notifications = new List<ViaControlOutputs>();
            via.ControlOutputsChanged = notifications.Add;
            via.Write(C.Peripheral, (byte)((int)a << C.Ca2ModeShift | (int)b << C.Cb2ModeShift));
            var expected = (a >= ViaControlMode.HandshakeOutput ? ViaControlOutputs.Ca2 : ViaControlOutputs.None)
                | (b >= ViaControlMode.HandshakeOutput ? ViaControlOutputs.Cb2 : ViaControlOutputs.None);
            Assert.Equal(expected, via.ControlOutputs);
            if (expected == ViaControlOutputs.None) Assert.Empty(notifications);
            else Assert.Equal(new[] { expected }, notifications);
        }
    }

    [Fact]
    public void SerialModesSelectTheClockAndDataDirectionsWithoutChangingPortA()
    {
        foreach (var mode in Enum.GetValues<ViaShiftMode>())
        {
            var via = new Via6522();
            via.Write(C.Peripheral, (byte)((int)ViaControlMode.HighOutput << C.Ca2ModeShift));
            via.Write(C.Auxiliary, (byte)((int)mode << C.ShiftModeShift));
            via.Write(C.Shift, 0);
            var clockOutput = mode is ViaShiftMode.InputTimer2 or ViaShiftMode.InputSystemClock
                or ViaShiftMode.OutputTimer2Continuous or ViaShiftMode.OutputTimer2 or ViaShiftMode.OutputSystemClock;
            var dataOutput = mode >= ViaShiftMode.OutputTimer2Continuous;
            var expected = ViaControlOutputs.Ca2 | (clockOutput ? ViaControlOutputs.Cb1 : ViaControlOutputs.None)
                | (dataOutput ? ViaControlOutputs.Cb2 : ViaControlOutputs.None);
            Assert.Equal(expected, via.ControlOutputs);
        }
    }

    [Fact]
    public void ReleasingPcrOutputsRestoresTheLevelsStillAppliedByTheExternalCircuit()
    {
        var via = new Via6522();
        via.SetCa2(true);
        via.SetCb2(true);
        via.Write(C.Peripheral, (byte)((int)ViaControlMode.LowOutput << C.Ca2ModeShift
            | (int)ViaControlMode.LowOutput << C.Cb2ModeShift));
        Assert.False(via.Ca2);
        Assert.False(via.Cb2);
        var released = false;
        via.ControlOutputsChanged = outputs =>
        {
            Assert.Equal(ViaControlOutputs.None, outputs);
            Assert.True(via.Ca2);
            Assert.True(via.Cb2);
            released = true;
        };
        via.Write(C.Peripheral, 0);
        Assert.True(released);
    }

    [Fact]
    public void FiniteSerialOutputPublishesTheReturnOfCb2ToAnInputPcr()
    {
        var via = new Via6522();
        via.SetCb2(true);
        via.Write(C.Auxiliary, (byte)((int)ViaShiftMode.OutputSystemClock << C.ShiftModeShift));
        var notifications = new List<ViaControlOutputs>();
        via.ControlOutputsChanged = notifications.Add;
        via.Write(C.Shift, 0);
        for (var cycle = 0; cycle < 18; cycle++) via.Tick();
        Assert.Equal(new[] { ViaControlOutputs.Cb1 | ViaControlOutputs.Cb2, ViaControlOutputs.Cb1 }, notifications);
        Assert.True(via.Cb2);
    }

    [Fact]
    public void ResetReleasesAllControlOutputsWithoutClearingTheExternalInputs()
    {
        var via = new Via6522();
        via.Write(C.Peripheral, (byte)((int)ViaControlMode.LowOutput << C.Ca2ModeShift));
        via.Write(C.Auxiliary, (byte)((int)ViaShiftMode.OutputSystemClock << C.ShiftModeShift));
        via.Write(C.Shift, 0);
        for (var cycle = 0; cycle < 3; cycle++) via.Tick();
        via.SetCa2(true);
        via.SetCb2(true);
        via.SetCb1(true);
        var notifications = new List<ViaControlOutputs>();
        via.ControlOutputsChanged = notifications.Add;
        via.Reset();
        Assert.Equal(new[] { ViaControlOutputs.None }, notifications);
        Assert.Equal(ViaControlOutputs.None, via.ControlOutputs);
        Assert.True(via.Ca2);
        Assert.True(via.Cb1);
        Assert.True(via.Cb2);
        Assert.Equal(0, via.Read(C.Flags));
    }
}

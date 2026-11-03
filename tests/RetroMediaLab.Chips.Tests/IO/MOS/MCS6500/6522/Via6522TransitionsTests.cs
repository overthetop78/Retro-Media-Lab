using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;
using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Enums;
using C = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Constants.Via6522Constants;

namespace RetroMediaLab.Chips.Tests.IO.MOS.MCS6500.Chip6522;

public sealed class Via6522TransitionsTests
{
    [Theory]
    [InlineData(ViaControlMode.HandshakeOutput, false)]
    [InlineData(ViaControlMode.HandshakeOutput, true)]
    [InlineData(ViaControlMode.PulseOutput, false)]
    [InlineData(ViaControlMode.PulseOutput, true)]
    public void PortAReadReturnsThePreviousByteBeforeItsAcknowledgementMakesThePeerSendAnother(ViaControlMode mode, bool latch)
    {
        const byte first = 0xa5;
        const byte next = 0x59;
        var data = first;
        var via = new Via6522 { ReadPortA = () => data };
        via.Write(C.Auxiliary, latch ? C.LatchA : (byte)0);
        via.Write(C.Peripheral, (byte)(C.Ca1Rising | (int)mode << C.Ca2ModeShift));
        via.SetCa1(true);
        via.SetCa1(false);
        var acknowledgements = 0;
        via.Ca2PinsChanged = level =>
        {
            if (level) return;
            acknowledgements++;
            data = next;
            via.SetCa1(true);
        };
        Assert.Equal(first, via.Read(C.PortANoHandshake));
        Assert.Equal(0, acknowledgements);
        Assert.Equal(first, via.Read(C.PortA));
        Assert.Equal(1, acknowledgements);
        Assert.NotEqual(0, via.Read(C.Flags) & C.Ca1Interrupt);
        Assert.Equal(next, via.Read(C.PortANoHandshake));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WritingAcrAfterTheOneShotDoesNotRearmTimer1(bool continuous)
    {
        var via = new Via6522();
        via.Write(C.Auxiliary, C.Timer1Pb7);
        via.Write(C.Enable, C.InterruptSummary | C.Timer1Interrupt);
        via.Write(C.Timer1Low, 1);
        via.Write(C.Timer1High, 0);
        for (var cycle = 0; cycle < 3; cycle++) via.Tick();
        Assert.True(via.Irq);
        via.Write(C.Flags, C.Timer1Interrupt);
        via.Write(C.Auxiliary, continuous ? (byte)(C.Timer1Pb7 | C.Timer1Continuous) : C.Timer1Pb7);
        for (var cycle = 0; cycle < 12; cycle++)
        {
            via.Tick();
            Assert.False(via.Irq);
            Assert.NotEqual(0, via.Read(C.PortB) & C.Pb7);
        }
        via.Write(C.Timer1High, 0);
        for (var cycle = 0; cycle < 3; cycle++) via.Tick();
        Assert.True(via.Irq);
    }

    [Fact]
    public void ExternalInputUsesTheDataHeldDuringTheSystemCycleAfterTheClockEdge()
    {
        var via = new Via6522();
        via.Write(C.Auxiliary, (byte)((int)ViaShiftMode.InputExternalClock << C.ShiftModeShift));
        via.Write(C.Shift, 0);
        for (var bit = 0; bit < C.ShiftBitCount; bit++)
        {
            via.SetCb2(false);
            via.SetCb1(false);
            via.SetCb1(true);
            via.SetCb2(true);
            via.Tick();
        }
        Assert.Equal(byte.MaxValue, via.Read(C.Shift));
    }

    [Fact]
    public void ExternalSerialInputSamplesCb2OnTheFollowingSystemCycleAndContinuesAcrossBytes()
    {
        var via = new Via6522();
        via.Write(C.Auxiliary, (byte)((int)ViaShiftMode.InputExternalClock << C.ShiftModeShift));
        via.Write(C.Enable, C.InterruptSummary | C.ShiftInterrupt);
        via.Write(C.Shift, 0);
        for (var bit = 0; bit < C.ShiftBitCount; bit++)
        {
            via.SetCb2(false);
            via.SetCb1(false);
            via.SetCb1(true);
            Assert.False(via.Irq);
            via.SetCb2(true);
            via.Tick();
            Assert.Equal(bit == C.ShiftBitCount - 1, via.Irq);
        }

        // Clear only IFR: external input must continue without rearming SR.
        via.Write(C.Flags, C.ShiftInterrupt);
        const byte secondByte = 0xa6;
        for (var bit = C.ShiftBitCount - 1; bit >= 0; bit--)
        {
            via.SetCb1(false);
            via.SetCb2((secondByte & (1 << bit)) != 0);
            via.SetCb1(true);
            Assert.False(via.Irq);
            via.Tick();
            Assert.Equal(bit == 0, via.Irq);
        }
        Assert.Equal(secondByte, via.Read(C.Shift));
        Assert.False(via.Irq);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void ResetReadWriteAndModeChangeCancelAnUnconsumedExternalShiftEdge(int operation)
    {
        const byte initialValue = 0xaa;
        const byte replacementValue = 0x55;
        var via = new Via6522();
        via.Write(C.Auxiliary, (byte)((int)ViaShiftMode.InputExternalClock << C.ShiftModeShift));
        via.Write(C.Shift, initialValue);
        via.SetCb2(true);
        via.SetCb1(true);
        switch (operation)
        {
            case 0: via.Reset(); break;
            case 1: Assert.Equal(initialValue, via.Read(C.Shift)); break;
            case 2: via.Write(C.Shift, replacementValue); break;
            case 3: via.Write(C.Auxiliary, 0); break;
        }
        via.Tick();
        Assert.Equal(operation == 2 ? replacementValue : initialValue, via.Read(C.Shift));
        Assert.Equal(0, via.Read(C.Flags) & C.ShiftInterrupt);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Cb1InterruptAndPortBLatchRemainOperationalInEverySerialMode(bool rising)
    {
        foreach (var mode in Enum.GetValues<ViaShiftMode>())
        {
            const byte sampledInputs = 0xa6;
            var inputs = sampledInputs;
            var via = new Via6522 { ReadPortB = () => inputs };
            via.Write(C.Peripheral, rising ? C.Cb1Rising : (byte)0);
            via.Write(C.Auxiliary, (byte)(((int)mode << C.ShiftModeShift) | C.LatchB));
            via.Write(C.Enable, C.InterruptSummary | C.Cb1Interrupt);
            via.Write(C.Timer2Low, 0);
            via.Write(C.Shift, 0);
            via.Write(C.Flags, C.InterruptMask);

            if (mode is ViaShiftMode.Disabled or ViaShiftMode.InputExternalClock or ViaShiftMode.OutputExternalClock)
            {
                via.SetCb1(!rising);
                via.Write(C.Flags, C.InterruptMask);
                via.SetCb1(rising);
            }
            else
            {
                var ticks = 0;
                while (!via.Irq && ticks++ < 10) via.Tick();
                Assert.InRange(ticks, 1, 9);
            }

            Assert.True(via.Irq);
            Assert.NotEqual(0, via.Read(C.Flags) & C.Cb1Interrupt);
            inputs = 0;
            Assert.Equal(sampledInputs, via.Read(C.PortB));
            Assert.False(via.Irq);
        }
    }

    [Fact]
    public void ExternalCb1ChangesDoNotTriggerFlagsWhileTheViaDrivesTheClock()
    {
        foreach (var mode in new[] { ViaShiftMode.InputTimer2, ViaShiftMode.InputSystemClock,
                     ViaShiftMode.OutputTimer2Continuous, ViaShiftMode.OutputTimer2, ViaShiftMode.OutputSystemClock })
        {
            var via = new Via6522();
            via.Write(C.Auxiliary, (byte)((int)mode << C.ShiftModeShift));
            via.Write(C.Flags, C.InterruptMask);
            via.SetCb1(true);
            via.SetCb1(false);
            Assert.Equal(0, via.Read(C.Flags));
            Assert.True(via.Cb1);
        }
    }

    [Theory]
    [InlineData(ViaControlMode.HandshakeOutput)]
    [InlineData(ViaControlMode.PulseOutput)]
    public void EveryShiftModeTransitionPreservesThePortAControlSequence(ViaControlMode controlMode)
    {
        foreach (var previous in Enum.GetValues<ViaShiftMode>())
        foreach (var next in Enum.GetValues<ViaShiftMode>())
        {
            var via = new Via6522();
            via.Write(C.Peripheral, (byte)(((int)controlMode << C.Ca2ModeShift) | C.Ca1Rising));
            via.Write(C.Auxiliary, (byte)((int)previous << C.ShiftModeShift));
            via.Read(C.PortA);
            Assert.False(via.Ca2);

            via.Write(C.Auxiliary, (byte)((int)next << C.ShiftModeShift));
            Assert.False(via.Ca2);
            via.Tick();
            Assert.False(via.Ca2);
            via.Tick();
            Assert.Equal(controlMode == ViaControlMode.PulseOutput, via.Ca2);
            via.SetCa1(true);
            Assert.True(via.Ca2);
        }
    }

    [Fact]
    public void CapturedPortBInputsRemainSeparateFromItsLiveOutputLatch()
    {
        const byte capturedInputs = 0xa5;
        const byte initialOutput = 0x3c;
        const byte changedOutput = 0xc3;
        for (var direction = 0; direction <= byte.MaxValue; direction++)
        {
            var inputs = capturedInputs;
            var via = new Via6522 { ReadPortA = () => inputs, ReadPortB = () => inputs };
            via.Write(C.DirectionA, (byte)direction);
            via.Write(C.DirectionB, (byte)direction);
            via.Write(C.PortANoHandshake, initialOutput);
            via.Write(C.PortB, initialOutput);
            via.Write(C.Auxiliary, C.LatchA | C.LatchB);
            via.Write(C.Peripheral, C.Ca1Rising | C.Cb1Rising);
            via.SetCa1(true);
            via.SetCb1(true);

            inputs = unchecked((byte)~capturedInputs);
            via.Write(C.PortANoHandshake, changedOutput);
            via.Write(C.PortB, changedOutput);
            Assert.Equal((byte)(capturedInputs & (initialOutput | ~direction)), via.Read(C.PortANoHandshake));
            Assert.Equal((byte)((capturedInputs & ~direction) | (changedOutput & direction)), via.Read(C.PortB));

            // A new active edge captures new inputs even if the previous CA1 flag remains set.
            via.SetCa1(false);
            via.SetCb1(false);
            via.SetCa1(true);
            via.SetCb1(true);
            Assert.Equal((byte)(inputs & (changedOutput | ~direction)), via.Read(C.PortANoHandshake));
            Assert.Equal((byte)((inputs & ~direction) | (changedOutput & direction)), via.Read(C.PortB));
        }
    }

    [Fact]
    public void EveryInterruptMaskCanBeEnabledDisabledAndAcknowledgedIndependently()
    {
        for (var mask = 0; mask <= C.InterruptMask; mask++)
        {
            var via = CreateWithEveryInterruptFlag();
            via.Write(C.Enable, (byte)(C.InterruptSummary | mask));
            Assert.Equal((byte)(C.InterruptSummary | mask), via.Read(C.Enable));
            Assert.Equal(mask != 0, via.Irq);
            via.Write(C.Enable, (byte)mask);
            Assert.False(via.Irq);
            Assert.Equal(C.InterruptMask, via.Read(C.Flags));

            via.Write(C.Enable, byte.MaxValue);
            via.Write(C.Flags, (byte)mask);
            var remaining = C.InterruptMask & ~mask;
            Assert.Equal(remaining != 0, via.Irq);
            Assert.Equal((byte)(remaining | (remaining != 0 ? C.InterruptSummary : 0)), via.Read(C.Flags));
            via.Write(C.Flags, C.InterruptSummary);
            Assert.Equal(remaining != 0, via.Irq);
        }
    }

    private static Via6522 CreateWithEveryInterruptFlag()
    {
        var via = new Via6522();
        via.SetCa1(true);
        via.SetCa1(false);
        via.SetCa2(true);
        via.SetCa2(false);
        via.SetCb1(true);
        via.SetCb1(false);
        via.SetCb2(true);
        via.SetCb2(false);
        via.Write(C.Auxiliary, (byte)((int)ViaShiftMode.OutputSystemClock << C.ShiftModeShift));
        via.Write(C.Shift, 0);
        via.Write(C.Timer1Low, 0);
        via.Write(C.Timer1High, 0);
        via.Write(C.Timer2Low, 0);
        via.Write(C.Timer2High, 0);
        for (var tick = 0; tick < 20; tick++) via.Tick();
        Assert.Equal(C.InterruptMask, via.Read(C.Flags));
        return via;
    }

    [Fact]
    public void DisablingTheShifterHoldsItsInterruptFlagLow()
    {
        var via = new Via6522();
        via.Write(C.Auxiliary, 6 << C.ShiftModeShift);
        via.Write(C.Shift, 0xa6);
        for (var tick = 0; tick < 20; tick++) via.Tick();
        Assert.NotEqual(0, via.Read(C.Flags) & C.ShiftInterrupt);
        via.Write(C.Auxiliary, 0);
        Assert.Equal(0, via.Read(C.Flags) & C.ShiftInterrupt);
        for (var tick = 0; tick < 20; tick++) via.Tick();
        Assert.Equal(0, via.Read(C.Flags) & C.ShiftInterrupt);
    }

    [Theory]
    [InlineData(5, 6)]
    [InlineData(5, 7)]
    [InlineData(6, 6)]
    [InlineData(6, 7)]
    public void FiniteShiftOutputReturnsCb2ToThePcrLevel(int shiftMode, int controlMode)
    {
        var via = new Via6522();
        via.Write(C.Peripheral, (byte)(controlMode << C.Cb2ModeShift));
        via.Write(C.Auxiliary, (byte)(shiftMode << C.ShiftModeShift));
        via.Write(C.Timer2Low, 0);
        via.Write(C.Shift, controlMode == 7 ? (byte)0 : byte.MaxValue);
        var ticks = 0;
        while ((via.Read(C.Flags) & C.ShiftInterrupt) == 0 && ticks++ < 100) via.Tick();
        Assert.InRange(ticks, 1, 99);
        Assert.Equal(controlMode == 7, via.Cb2);
    }

    [Fact]
    public void ShiftCompletionDoesNotCancelAParallelPortAPulse()
    {
        var via = new Via6522();
        via.Write(C.Peripheral, (5 << C.Ca2ModeShift) | (7 << C.Cb2ModeShift));
        via.Write(C.Auxiliary, 6 << C.ShiftModeShift);
        var rising = 0;
        via.Cb1PinsChanged = high => { if (high && ++rising == C.ShiftBitCount) via.Write(C.PortA, 0); };
        via.Write(C.Shift, 0);
        var ticks = 0;
        while ((via.Read(C.Flags) & C.ShiftInterrupt) == 0 && ticks++ < 100) via.Tick();
        Assert.False(via.Ca2);
        Assert.True(via.Cb2);
        via.Tick();
        Assert.False(via.Ca2);
        via.Tick();
        Assert.True(via.Ca2);
    }

    [Fact]
    public void SwitchingToOneShotOnTheTimeoutDoesNotProduceAnExtraInterrupt()
    {
        var via = new Via6522();
        via.Write(C.Auxiliary, C.Timer1Continuous);
        via.Write(C.Timer1Low, 0);
        via.Write(C.Timer1High, 0);
        via.Tick(); via.Tick();
        Assert.NotEqual(0, via.Read(C.Flags) & C.Timer1Interrupt);
        via.Write(C.Auxiliary, 0);
        via.Tick();
        via.Read(C.Timer1Low);
        for (var tick = 0; tick < 5; tick++) via.Tick();
        Assert.Equal(0, via.Read(C.Flags) & C.Timer1Interrupt);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ResetDoesNotChangeTheVoltageStillAppliedToControlInputs(bool portA)
    {
        var via = new Via6522();
        if (portA) { via.SetCa1(true); via.SetCa2(true); }
        else { via.SetCb1(true); via.SetCb2(true); }
        via.Reset();
        Assert.Equal(0, via.Read(C.Flags));
        if (portA) { via.SetCa1(false); via.SetCa2(false); }
        else { via.SetCb1(false); via.SetCb2(false); }
        Assert.Equal(portA ? C.Ca1Interrupt | C.Ca2Interrupt : C.Cb1Interrupt | C.Cb2Interrupt,
            via.Read(C.Flags) & C.InterruptMask);
    }

    [Fact]
    public void PortAPinNotificationsIncludeDirectionChangesAndReset()
    {
        var via = new Via6522();
        var values = new List<byte>();
        via.PortAPinsChanged = values.Add;
        via.Write(C.PortANoHandshake, 5);
        via.Write(C.DirectionA, byte.MaxValue);
        via.Write(C.DirectionA, 15);
        via.Reset();
        Assert.Equal(new byte[] { 255, 5, 245, 255 }, values);
    }
}

using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;
using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Enums;
using C = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Constants.Via6522Constants;

namespace RetroMediaLab.Chips.Tests.IO.MOS.MCS6500.Chip6522;

public sealed class Via6522ClockTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Timer1ReloadsAfterUnderflowInBothModesWithoutRearmingTheOneShot(bool continuous)
    {
        var via = new Via6522();
        via.Write(C.Auxiliary, continuous ? C.Timer1Continuous : (byte)0);
        via.Write(C.Enable, C.InterruptSummary | C.Timer1Interrupt);
        via.Write(C.Timer1Low, 4);
        via.Write(C.Timer1High, 0);
        via.Tick(); // Chargement.
        for (var cycle = 0; cycle < 3; cycle++) via.Tick();
        Assert.Equal(1, Counter(via, C.Timer1Low));
        via.Tick();
        Assert.Equal(0, Counter(via, C.Timer1Low));
        via.Tick();
        Assert.Equal(C.CounterMask, Counter(via, C.Timer1Low));
        Assert.True(via.Irq);
        via.Tick();
        Assert.Equal(4, Counter(via, C.Timer1Low));
        Assert.False(via.Irq);
        via.Tick();
        Assert.Equal(3, Counter(via, C.Timer1Low));
        via.Tick();
        Assert.Equal(2, Counter(via, C.Timer1Low));
        for (var cycle = 0; cycle < 3; cycle++) via.Tick();
        Assert.Equal(continuous, via.Irq);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(255)]
    [InlineData(256)]
    [InlineData(65535)]
    public void Timer2PulseCounterInterruptsOnPulseNPlusOneAtTheFollowingLeadingEdge(int count)
    {
        var via = new Via6522();
        via.SetPb6(true);
        via.Write(C.Auxiliary, C.Timer2PulseCount);
        via.Write(C.Enable, C.InterruptSummary | C.Timer2Interrupt);
        via.Write(C.Timer2Low, (byte)count);
        via.Write(C.Timer2High, (byte)(count >> C.HighByteShift));
        for (var pulse = 0; pulse <= count; pulse++)
        {
            via.SetPb6(true);
            via.Tick();
            via.SetPb6(false);
            via.Phi2TrailingEdge();
            via.Phi2LeadingEdge();
            via.Phi2TrailingEdge();
            Assert.False(via.Irq);
            Assert.Equal((count - pulse - 1) & C.CounterMask, Counter(via, C.Timer2Low));
            via.Phi2LeadingEdge();
            Assert.Equal(pulse == count, via.Irq);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Timer2SwitchesBetweenIntervalAndPulseSourcesWithoutReloading(bool switchAfterTrailing)
    {
        var via = new Via6522();
        via.SetPb6(true);
        via.Write(C.Timer2Low, 5);
        via.Write(C.Timer2High, 0);
        for (var cycle = 0; cycle < 3; cycle++) via.Tick();
        if (switchAfterTrailing) via.Phi2TrailingEdge();
        var before = Counter(via, C.Timer2Low);
        via.Write(C.Auxiliary, C.Timer2PulseCount);
        if (switchAfterTrailing) via.Phi2LeadingEdge();
        for (var cycle = 0; cycle < 4; cycle++) via.Tick();
        Assert.Equal(before, Counter(via, C.Timer2Low));
        via.SetPb6(false);
        via.Tick();
        via.Tick();
        Assert.Equal(before - 1, Counter(via, C.Timer2Low));
        via.Write(C.Auxiliary, 0);
        via.Tick();
        Assert.Equal(before - 2, Counter(via, C.Timer2Low));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(255)]
    public void Timer2SerialClockHalfPeriodsFollowItsLowLatchPlusTwo(int lowLatch)
    {
        var via = new Via6522();
        via.Write(C.Auxiliary, (byte)((int)ViaShiftMode.OutputTimer2Continuous << C.ShiftModeShift));
        via.Write(C.Timer2Low, (byte)lowLatch);
        via.Write(C.Timer2High, 0);
        via.Write(C.Shift, 0xa6);
        for (var cycle = 0; cycle < lowLatch + 1; cycle++)
        {
            via.Tick();
            Assert.True(via.Cb1);
        }
        via.Tick();
        Assert.False(via.Cb1);
        for (var cycle = 0; cycle < lowLatch + 1; cycle++)
        {
            via.Tick();
            Assert.False(via.Cb1);
        }
        via.Tick();
        Assert.True(via.Cb1);
    }

    [Fact]
    public void Pb6SamplingPrecedesTheSerialOutputReleaseOnTheSameLeadingEdge()
    {
        var via = new Via6522();
        via.ReadPortB = () => via.Cb2 ? byte.MaxValue : (byte)(byte.MaxValue & ~C.Pb6);
        via.Write(C.Peripheral, (byte)((int)ViaControlMode.LowOutput << C.Cb2ModeShift));
        via.Write(C.Auxiliary, (byte)(((int)ViaShiftMode.OutputSystemClock << C.ShiftModeShift) | C.Timer2PulseCount));
        via.Write(C.Timer2Low, 3);
        via.Write(C.Timer2High, 0);
        via.Write(C.Shift, 1);
        for (var cycle = 0; cycle < 17; cycle++) via.Tick();
        Assert.True(via.Cb2);
        via.Tick(); // Last rising CB1: CB2 is released after this edge samples PB6.
        Assert.False(via.Cb2);
        via.Phi2TrailingEdge();
        Assert.Equal(3, Counter(via, C.Timer2Low));
        via.Phi2LeadingEdge(); // The new low level now reaches a sampling edge.
        via.Phi2TrailingEdge();
        Assert.Equal(2, Counter(via, C.Timer2Low));
    }

    [Fact]
    public void SystemClockSerialOutputMatchesTheMosFigure12StartAndDataSetup()
    {
        var via = new Via6522();
        via.Write(C.Peripheral, (byte)((int)ViaControlMode.HighOutput << C.Cb2ModeShift));
        via.Write(C.Auxiliary, (byte)((int)ViaShiftMode.OutputSystemClock << C.ShiftModeShift));
        via.Write(C.Shift, 0x40);
        for (var cycle = 0; cycle < 2; cycle++)
        {
            via.Tick();
            Assert.True(via.Cb1);
            Assert.True(via.Cb2);
        }
        via.Phi2TrailingEdge(); // First bit at +2 periods, before CB1 falls at +2.5.
        Assert.False(via.Cb2);
        Assert.True(via.Cb1);
        via.Phi2LeadingEdge();
        Assert.False(via.Cb1);
        via.Tick();
        Assert.True(via.Cb1);
        via.Phi2TrailingEdge(); // Second bit is also set up half a cycle before its clock.
        Assert.True(via.Cb2);
        Assert.True(via.Cb1);
        via.Phi2LeadingEdge();
        Assert.False(via.Cb1);
    }

    [Fact]
    public void LastSerialBitIsHeldThroughTheEighthRisingClockBeforeReturningToPcr()
    {
        var via = new Via6522();
        via.Write(C.Peripheral, (byte)((int)ViaControlMode.LowOutput << C.Cb2ModeShift));
        via.Write(C.Auxiliary, (byte)((int)ViaShiftMode.OutputSystemClock << C.ShiftModeShift));
        via.Write(C.Shift, 1);
        for (var cycle = 0; cycle < 17; cycle++) via.Tick();
        Assert.False(via.Cb1);
        Assert.True(via.Cb2);
        Assert.Equal(0, via.Read(C.Flags) & C.ShiftInterrupt);
        var lastBitSeen = false;
        via.Cb1PinsChanged = level => { if (level) lastBitSeen = via.Cb2; };
        via.Phi2TrailingEdge();
        Assert.True(via.Cb2);
        Assert.False(via.Cb1);
        Assert.Equal(0, via.Read(C.Flags) & C.ShiftInterrupt);
        via.Phi2LeadingEdge();
        Assert.True(lastBitSeen);
        Assert.False(via.Cb2);
        Assert.NotEqual(0, via.Read(C.Flags) & C.ShiftInterrupt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(255)]
    [InlineData(256)]
    [InlineData(65535)]
    public void Timer2IntervalHasTheSameInitialHalfCycleTimeoutOffset(int count)
    {
        var via = new Via6522();
        via.Write(C.Enable, C.InterruptSummary | C.Timer2Interrupt);
        via.Write(C.Timer2Low, (byte)count);
        via.Write(C.Timer2High, (byte)(count >> C.HighByteShift));
        for (var cycle = 0; cycle < count + 1; cycle++) via.Tick();
        Assert.False(via.Irq);
        via.Phi2TrailingEdge();
        Assert.Equal(ushort.MaxValue, Counter(via, C.Timer2Low));
        Assert.False(via.Irq);
        via.Phi2LeadingEdge();
        Assert.True(via.Irq);
        via.Tick();
        via.Read(C.Timer2Low);
        for (var cycle = 0; cycle < ushort.MaxValue + 2; cycle++) via.Tick();
        Assert.False(via.Irq);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void ControlPulseLastsOnePeriodAfterTheAccessCompletes(bool portA, bool read)
    {
        var via = new Via6522();
        via.Write(C.Peripheral, (byte)((int)ViaControlMode.PulseOutput << (portA ? C.Ca2ModeShift : C.Cb2ModeShift)));
        var register = portA ? C.PortA : C.PortB;
        if (read) via.Read(register); else via.Write(register, 0);
        Assert.False(portA ? via.Ca2 : via.Cb2);
        via.Phi2TrailingEdge();
        Assert.False(portA ? via.Ca2 : via.Cb2);
        via.Phi2LeadingEdge();
        Assert.False(portA ? via.Ca2 : via.Cb2);
        via.Phi2TrailingEdge();
        Assert.True(portA ? via.Ca2 : via.Cb2);
    }

    [Theory]
    [InlineData(true, ViaControlMode.PulseOutput)]
    [InlineData(false, ViaControlMode.PulseOutput)]
    [InlineData(true, ViaControlMode.HandshakeOutput)]
    [InlineData(false, ViaControlMode.HandshakeOutput)]
    public void RewritingPcrForTheOtherPortDoesNotResetAnUnchangedControl(bool portA, ViaControlMode mode)
    {
        var via = new Via6522();
        var shift = portA ? C.Ca2ModeShift : C.Cb2ModeShift;
        var otherShift = portA ? C.Cb2ModeShift : C.Ca2ModeShift;
        var pcr = (byte)((int)mode << shift);
        via.Write(C.Peripheral, pcr);
        via.Write(portA ? C.PortA : C.PortB, 0);
        via.Phi2TrailingEdge();
        via.Phi2LeadingEdge();
        via.Write(C.Peripheral, pcr);
        Assert.False(portA ? via.Ca2 : via.Cb2);
        via.Write(C.Peripheral, (byte)(pcr | (int)ViaControlMode.HighOutput << otherShift));
        Assert.False(portA ? via.Ca2 : via.Cb2);
        via.Phi2TrailingEdge();
        Assert.Equal(mode == ViaControlMode.PulseOutput, portA ? via.Ca2 : via.Cb2);
    }

    [Theory]
    [InlineData(ViaShiftMode.InputTimer2)]
    [InlineData(ViaShiftMode.InputSystemClock)]
    [InlineData(ViaShiftMode.InputExternalClock)]
    public void EnablingSerialInputCancelsAnyPendingCb2OutputPulse(ViaShiftMode mode)
    {
        var via = new Via6522();
        via.Write(C.Peripheral, (byte)((int)ViaControlMode.PulseOutput << C.Cb2ModeShift));
        via.Write(C.PortB, 0);
        var levels = new List<bool>();
        via.Cb2PinsChanged = levels.Add;
        via.Write(C.Auxiliary, (byte)((int)mode << C.ShiftModeShift));
        via.Write(C.Shift, 0);
        for (var cycle = 0; cycle < 4; cycle++) via.Tick();
        Assert.Empty(levels);
    }

    [Fact]
    public void ReadingPortBDoesNotTriggerItsOutputPulse()
    {
        var via = new Via6522();
        via.Write(C.Peripheral, (byte)((int)ViaControlMode.PulseOutput << C.Cb2ModeShift));
        via.Read(C.PortB);
        via.Tick();
        Assert.True(via.Cb2);
    }

    [Fact]
    public void Timer2SamplesPb6AtTheLeadingEdgeBeforeCountingItAtTheTrailingEdge()
    {
        var via = PulseCounter(1);
        via.Phi2TrailingEdge();
        via.Phi2LeadingEdge();
        via.SetPb6(false);
        via.Phi2TrailingEdge();
        Assert.Equal(1, Counter(via, C.Timer2Low));
        via.Phi2LeadingEdge();
        Assert.False(via.Irq);
        via.Phi2TrailingEdge();
        Assert.Equal(0, Counter(via, C.Timer2Low));
        Assert.False(via.Irq);
        via.Phi2LeadingEdge();
        Assert.False(via.Irq);
        via.SetPb6(true);
        via.Tick();
        via.SetPb6(false);
        via.Phi2TrailingEdge();
        via.Phi2LeadingEdge();
        via.Phi2TrailingEdge();
        Assert.Equal(C.CounterMask, Counter(via, C.Timer2Low));
        Assert.False(via.Irq);
        via.Phi2LeadingEdge();
        Assert.True(via.Irq);
    }

    [Fact]
    public void Timer2DoesNotCountAPb6PulseThatNeverReachesASamplingEdge()
    {
        var via = PulseCounter(1);
        via.Tick();
        via.SetPb6(false);
        via.Phi2TrailingEdge();
        via.SetPb6(true);
        via.Phi2LeadingEdge();
        via.Tick();
        Assert.Equal(1, Counter(via, C.Timer2Low));
        Assert.False(via.Irq);
    }

    [Fact]
    public void ReloadingTimer2CancelsAPulseSampledBeforeTheWrite()
    {
        var via = PulseCounter(1);
        via.Tick();
        via.SetPb6(false);
        via.Phi2LeadingEdge();
        via.Write(C.Timer2Low, 3);
        via.Write(C.Timer2High, 0);
        via.Phi2TrailingEdge();
        via.Phi2LeadingEdge();
        Assert.Equal(3, Counter(via, C.Timer2Low));
        Assert.False(via.Irq);
    }

    private static Via6522 PulseCounter(byte count)
    {
        var via = new Via6522();
        via.SetPb6(true);
        via.Write(C.Auxiliary, C.Timer2PulseCount);
        via.Write(C.Enable, C.InterruptSummary | C.Timer2Interrupt);
        via.Write(C.Timer2Low, count);
        via.Write(C.Timer2High, 0);
        return via;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(255)]
    [InlineData(256)]
    [InlineData(65535)]
    public void Timer1InterruptFollowsTheUnderflowByHalfAPeriod(int count)
    {
        var via = new Via6522();
        via.Write(C.Enable, C.InterruptSummary | C.Timer1Interrupt);
        via.Write(C.Timer1Low, (byte)count);
        via.Write(C.Timer1High, (byte)(count >> C.HighByteShift));
        // The write completes at falling edge 0. IRQ is expected at rising edge N+1.5.
        via.Phi2TrailingEdge();
        Assert.Equal(count, Counter(via, C.Timer1Low));
        via.Phi2LeadingEdge();
        Assert.False(via.Irq);
        for (var remaining = count; remaining > 0; remaining--)
        {
            via.Phi2TrailingEdge();
            Assert.Equal(remaining - 1, Counter(via, C.Timer1Low));
            via.Phi2LeadingEdge();
            Assert.False(via.Irq);
        }
        via.Phi2TrailingEdge();
        Assert.Equal(ushort.MaxValue, Counter(via, C.Timer1Low));
        Assert.False(via.Irq);
        via.Phi2LeadingEdge();
        Assert.True(via.Irq);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(255)]
    public void FreeRunningTimer1HasNPlusTwoPeriodsBetweenInterrupts(int count)
    {
        var via = new Via6522();
        via.Write(C.Auxiliary, C.Timer1Continuous);
        via.Write(C.Enable, C.InterruptSummary | C.Timer1Interrupt);
        via.Write(C.Timer1Low, (byte)count);
        via.Write(C.Timer1High, 0);
        for (var period = 0; period < count + 2; period++) via.Tick();
        Assert.True(via.Irq);
        via.Write(C.Flags, C.Timer1Interrupt);
        for (var period = 1; period <= count + 2; period++)
        {
            via.Phi2TrailingEdge();
            Assert.False(via.Irq);
            via.Phi2LeadingEdge();
            Assert.Equal(period == count + 2, via.Irq);
        }
    }

    [Fact]
    public void Timer1Pb7ChangesOnTheLeadingEdgeRatherThanTheCounterUnderflow()
    {
        var via = new Via6522();
        via.Write(C.Auxiliary, C.Timer1Pb7);
        via.Write(C.Timer1Low, 0);
        via.Write(C.Timer1High, 0);
        via.Tick();
        var levels = new List<byte>();
        via.PortBPinsChanged = levels.Add;
        via.Phi2TrailingEdge();
        Assert.Empty(levels);
        Assert.Equal(0, via.Read(C.PortB) & C.Pb7);
        via.Phi2LeadingEdge();
        Assert.Single(levels);
        Assert.NotEqual(0, levels[0] & C.Pb7);
    }

    [Fact]
    public void TimerTimeoutsArePublishedIndependentlyAndSurviveAnEarlierAcknowledge()
    {
        var via = new Via6522();
        via.Write(C.Enable, C.InterruptSummary | C.Timer1Interrupt | C.Timer2Interrupt);
        via.Write(C.Timer1Low, 0);
        via.Write(C.Timer1High, 0);
        via.Write(C.Timer2Low, 1);
        via.Write(C.Timer2High, 0);
        via.Tick();
        via.Phi2TrailingEdge();
        via.Read(C.Timer1Low);
        via.Write(C.Flags, C.Timer1Interrupt);
        Assert.False(via.Irq);
        via.Phi2LeadingEdge();
        Assert.Equal(C.InterruptSummary | C.Timer1Interrupt, via.Read(C.Flags));
        via.Write(C.Flags, C.Timer1Interrupt);
        via.Phi2TrailingEdge();
        Assert.False(via.Irq);
        via.Phi2LeadingEdge();
        Assert.Equal(C.InterruptSummary | C.Timer2Interrupt, via.Read(C.Flags));
    }

    [Fact]
    public void ResetCancelsTimeoutsWaitingForTheLeadingEdge()
    {
        var via = new Via6522();
        via.Write(C.Enable, byte.MaxValue);
        via.Write(C.Timer1Low, 0);
        via.Write(C.Timer1High, 0);
        via.Write(C.Timer2Low, 0);
        via.Write(C.Timer2High, 0);
        via.Tick();
        via.Phi2TrailingEdge();
        Assert.False(via.Irq);
        via.Reset();
        via.Phi2LeadingEdge();
        Assert.Equal(0, via.Read(C.Flags));
    }

    private static int Counter(Via6522 via, int lowRegister) =>
        via.Read(lowRegister) | via.Read(lowRegister + 1) << C.HighByteShift;
}

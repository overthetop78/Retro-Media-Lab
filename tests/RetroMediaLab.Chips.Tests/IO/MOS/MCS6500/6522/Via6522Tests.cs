using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;

namespace RetroMediaLab.Chips.Tests.IO.MOS.MCS6500.Chip6522;

public sealed class Via6522Tests
{
    [Theory]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [InlineData(true, 3)]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(false, 3)]
    public void ControlInputFlagsRespectEdgesAndIndependentAcknowledgement(bool portA, int mode)
    {
        var via = new Via6522();
        via.Reset();
        via.Write(12, (byte)(mode << (portA ? 1 : 5)));
        var flag = portA ? 1 : 8;
        via.Write(14, (byte)(0x80 | flag));
        Set(true);
        Set(false);
        Assert.True(via.Irq);
        Assert.Equal(flag, via.Read(13) & flag);
        via.Read(portA ? 1 : 0);
        Assert.Equal((mode & 1) != 0, via.Irq);
        via.Write(13, (byte)flag);
        Assert.False(via.Irq);
        Set(false);
        Assert.False(via.Irq);
        void Set(bool level) { if (portA) via.SetCa2(level); else via.SetCb2(level); }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HandshakePublishesDataBeforeStrobeAndWaitsForTheSelectedControlEdge(bool portA)
    {
        var via = new Via6522();
        via.Reset();
        via.Write(12, portA ? (byte)0x09 : (byte)0x90); // Sortie handshake ; CA1/CB1 montant.
        var events = new List<string>();
        if (portA) { via.WritePortA = value => events.Add(value.ToString()); via.Ca2PinsChanged = level => events.Add(level.ToString()); }
        else { via.WritePortB = value => events.Add(value.ToString()); via.Cb2PinsChanged = level => events.Add(level.ToString()); }
        via.Write(portA ? 1 : 0, 0x55);
        Assert.Equal(new[] { "85", "False" }, events);
        for (var tick = 0; tick < 4; tick++) via.Tick();
        Assert.False(portA ? via.Ca2 : via.Cb2);
        if (portA) via.SetCa1(true); else via.SetCb1(true);
        Assert.True(portA ? via.Ca2 : via.Cb2);
        Assert.Equal("True", events[^1]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PulseOutputReturnsHighAfterOneViaClockAndInputEdgesCannotRaiseItsInterrupt(bool portA)
    {
        var via = new Via6522();
        via.Reset();
        via.Write(12, (byte)(5 << (portA ? 1 : 5)));
        via.Write(portA ? 1 : 0, 0);
        Assert.False(portA ? via.Ca2 : via.Cb2);
        if (portA) { via.SetCa2(true); via.SetCa2(false); }
        else { via.SetCb2(true); via.SetCb2(false); }
        Assert.Equal(0, via.Read(13) & (portA ? 1 : 8));
        via.Phi2TrailingEdge(); // Fin de l'accès qui déclenche l'impulsion.
        Assert.False(portA ? via.Ca2 : via.Cb2);
        via.Phi2LeadingEdge();
        via.Tick();
        Assert.True(portA ? via.Ca2 : via.Cb2);
        via.Write(12, (byte)(6 << (portA ? 1 : 5)));
        Assert.False(portA ? via.Ca2 : via.Cb2);
        via.Tick();
        Assert.False(portA ? via.Ca2 : via.Cb2);
        via.Write(12, (byte)(7 << (portA ? 1 : 5)));
        Assert.True(portA ? via.Ca2 : via.Cb2);
    }

    [Fact]
    public void PortANoHandshakeDoesNotAcknowledgeOrPulseCa2()
    {
        var via = new Via6522();
        via.Reset();
        via.Write(12, 0x0a);
        via.SetCa1(true); via.SetCa1(false);
        Assert.NotEqual(0, via.Read(13) & 2);
        via.Write(15, 0x55);
        via.Read(15);
        Assert.True(via.Ca2);
        Assert.NotEqual(0, via.Read(13) & 2);
        via.Read(1);
        Assert.False(via.Ca2);
        Assert.Equal(0, via.Read(13) & 2);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InputLatchCapturesOnControlEdgeAndRetainsValueUntilTheNextEdge(bool portA)
    {
        var value = (byte)0x55;
        var via = new Via6522();
        via.Reset();
        if (portA) via.ReadPortA = () => value; else via.ReadPortB = () => value;
        via.Write(11, portA ? (byte)1 : (byte)2);
        via.Write(12, portA ? (byte)1 : (byte)0x10);
        if (portA) via.SetCa1(true); else via.SetCb1(true);
        value = 0xaa;
        Assert.Equal(0x55, via.Read(portA ? 1 : 0));
        if (portA) { via.SetCa1(false); via.SetCa1(true); }
        else { via.SetCb1(false); via.SetCb1(true); }
        Assert.Equal(0xaa, via.Read(portA ? 1 : 0));
    }

    [Fact]
    public void Timer2PulseModeCountsOnlyPb6FallingEdgesAndRequiresReloadForAnotherInterrupt()
    {
        var via = new Via6522();
        via.Reset();
        via.SetPb6(true);
        via.Write(11, 0x20);
        via.Write(14, 0xa0);
        via.Write(8, 2); via.Write(9, 0);
        for (var tick = 0; tick < 20; tick++) via.Tick();
        Assert.Equal(2, via.Read(8));
        Fall();
        Assert.Equal(1, via.Read(8));
        for (var tick = 0; tick < 20; tick++) via.Tick();
        Assert.False(via.Irq);
        Fall();
        Assert.False(via.Irq);
        Assert.Equal(0, via.Read(8));
        Fall();
        Assert.True(via.Irq);
        Assert.Equal(0xff, via.Read(8));
        via.Tick();
        via.Read(8);
        Assert.False(via.Irq);
        Fall();
        Assert.Equal(0xfe, via.Read(8));
        Assert.Equal(0xff, via.Read(9));
        Assert.False(via.Irq);
        via.Write(8, 1); via.Write(9, 0);
        Fall();
        Assert.False(via.Irq);
        Fall();
        Assert.True(via.Irq);
        void Fall() { via.SetPb6(true); via.Tick(); via.SetPb6(false); via.Tick(); via.Tick(); }
    }

    [Fact]
    public void Timer1OneShotReloadsItsLatchWithoutRaisingAnotherInterrupt()
    {
        var via = new Via6522();
        via.Reset();
        via.Write(14, 0xc0);
        via.Write(4, 1); via.Write(5, 0);
        via.Tick(); via.Tick();
        Assert.False(via.Irq);
        via.Tick();
        Assert.True(via.Irq);
        Assert.Equal(0xff, via.Read(4));
        via.Tick();
        Assert.Equal(1, via.Read(4));
        Assert.False(via.Irq);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Timer1Pb7OverridesDirectionAndTogglesAtTimeout(bool continuous)
    {
        var via = new Via6522();
        via.Reset();
        via.Write(11, continuous ? (byte)0xc0 : (byte)0x80);
        via.Write(4, 1); via.Write(5, 0);
        Assert.Equal(0, via.Read(0) & 0x80);
        via.Tick(); via.Tick(); via.Tick();
        Assert.Equal(0x80, via.Read(0) & 0x80);
        via.Tick(); via.Tick(); via.Tick();
        Assert.Equal(continuous ? 0 : 0x80, via.Read(0) & 0x80);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PortAReadsActualOutputPinWhilePortBReadsOutputLatch(bool portA)
    {
        var via = new Via6522();
        via.Reset();
        if (portA) via.ReadPortA = () => 0; else via.ReadPortB = () => 0;
        via.Write(portA ? 3 : 2, 0xff);
        via.Write(portA ? 1 : 0, 0xff);
        Assert.Equal(portA ? 0 : 0xff, via.Read(portA ? 1 : 0));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(2)]
    [InlineData(1)]
    public void ShiftInputCollectsEightBitsWithSelectedClock(int mode)
    {
        var via = new Via6522();
        via.Reset();
        via.Write(11, (byte)(mode << 2));
        via.Write(14, 0x84);
        via.Write(8, 0);
        via.Write(10, 0);
        const byte data = 0xa6;
        for (var bit = 7; bit >= 0; bit--)
        {
            via.SetCb2((data & (1 << bit)) != 0);
            if (mode == 3) { via.SetCb1(false); via.SetCb1(true); via.Tick(); }
            else
            {
                if (via.Cb1) WaitForClock(false);
                WaitForClock(true);
                via.Tick(); // CB2 reste stable pendant le cycle suivant le front CB1.
            }
            if (bit != 0) Assert.False(via.Irq);
        }
        if (mode != 3) via.Tick();
        Assert.True(via.Irq);
        Assert.Equal(data, via.Read(10));
        Assert.False(via.Irq);
        void WaitForClock(bool level)
        {
            var ticks = 0;
            do { via.Tick(); ticks++; } while (via.Cb1 != level && ticks < 100);
            Assert.InRange(ticks, 1, 99);
        }
    }

    [Theory]
    [InlineData(7)]
    [InlineData(6)]
    [InlineData(5)]
    [InlineData(4)]
    public void ShiftOutputUsesMsbFirstAndContinuousModeDoesNotInterrupt(int mode)
    {
        var via = new Via6522();
        via.Reset();
        via.Write(11, (byte)(mode << 2));
        via.Write(14, 0x84);
        via.Write(8, 0);
        var bits = new List<bool>();
        via.Cb1PinsChanged = level => { if (level) bits.Add(via.Cb2); };
        via.Write(10, 0xa6);
        if (mode == 7)
        {
            for (var bit = 0; bit < 8; bit++)
            {
                via.SetCb1(true); via.SetCb1(false);
                bits.Add(via.Cb2);
            }
        }
        else for (var tick = 0; tick < 100 && bits.Count < 8; tick++) via.Tick();
        Assert.Equal(new[] { true, false, true, false, false, true, true, false }, bits);
        Assert.Equal(mode != 4, via.Irq);
        if (mode == 4)
        {
            for (var tick = 0; tick < 32; tick++) via.Tick();
            Assert.False(via.Irq);
        }
        Assert.Equal(0xa6, via.Read(10));
        Assert.False(via.Irq);
    }

    [Fact]
    public void StaticShiftModeSamplesCb2WithoutTakingItsDirectionFromPcr()
    {
        var via = new Via6522();
        via.Reset();
        via.Write(10, 0xa6);
        via.Write(12, 0xe0);
        for (var tick = 0; tick < 32; tick++)
        {
            via.SetCb1((tick & 1) != 0);
            via.Tick();
        }
        Assert.True(via.Cb2);
        Assert.Equal(byte.MaxValue, via.Read(10));
        Assert.Equal(0, via.Read(13) & 4);
    }

    [Fact]
    public void ResetClearsControlAndInterruptsWithoutErasingTimerAndShiftContents()
    {
        var via = new Via6522();
        via.Reset();
        via.Write(10, 0xa6);
        via.Write(4, 0x34); via.Write(5, 0x12);
        via.Write(14, 0xff);
        via.Reset();
        Assert.Equal(0xa6, via.Read(10));
        Assert.Equal(0x34, via.Read(4));
        Assert.Equal(0x12, via.Read(5));
        Assert.Equal(0, via.Read(11));
        Assert.Equal(0x80, via.Read(14));
        Assert.False(via.Irq);
        via.Tick();
        Assert.Equal(0x33, via.Read(4));
    }

    [Theory]
    [InlineData(4, 0x40)]
    [InlineData(8, 0x20)]
    public void TimerInterruptCannotBeAcknowledgedOnItsExpiryClock(int lowRegister, int flag)
    {
        var via = new Via6522();
        via.Reset();
        via.Write(14, (byte)(0x80 | flag));
        via.Write(lowRegister, 0); via.Write(lowRegister + 1, 0);
        via.Tick(); via.Tick();
        Assert.True(via.Irq);
        via.Read(lowRegister);
        Assert.True(via.Irq);
        via.Tick();
        via.Read(lowRegister);
        Assert.False(via.Irq);
    }

    [Fact]
    public void Timer2ShiftClockReloadsLowByteWhileHighByteRetainsItsInterruptCounter()
    {
        var via = new Via6522();
        via.Reset();
        via.Write(11, 0x10);
        via.Write(14, 0xa0);
        via.Write(8, 0); via.Write(9, 1);
        via.Write(10, 0xa6);
        via.Tick(); via.Tick();
        Assert.False(via.Irq);
        Assert.Equal(0, via.Read(9));
        via.Tick(); via.Tick();
        Assert.True(via.Irq);
        Assert.Equal(0xff, via.Read(9));
        Assert.Equal(0, via.Read(13) & 4);
        via.Tick(); via.Read(8);
        Assert.False(via.Irq);
    }
}

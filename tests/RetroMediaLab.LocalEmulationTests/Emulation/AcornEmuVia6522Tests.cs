using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6522.Services;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuVia6522Tests
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
        Assert.True(via.Irq);
        Assert.Equal(0, via.Read(8));
        Assert.False(via.Irq);
        Fall();
        Assert.Equal(0xff, via.Read(8));
        Assert.Equal(0xff, via.Read(9));
        Assert.False(via.Irq);
        via.Write(8, 1); via.Write(9, 0);
        Fall();
        Assert.True(via.Irq);
        void Fall() { via.SetPb6(true); via.Tick(); via.SetPb6(false); via.Tick(); }
    }
}

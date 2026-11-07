using RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Constants;
using Ula = RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Services.BbcSerialUla;

namespace RetroMediaLab.Chips.Tests.IO.Acorn.SerialUla.BbcSerialUla;

public sealed class OutputNotificationTests
{
    private const int Clock = 2_000_000;

    [Theory]
    [InlineData(null)]
    [InlineData(false)]
    [InlineData(true)]
    public void StableSilenceKeepsFreeRunningClocksAndResumesCarrierOnInputChanges(bool? level)
    {
        var ula = new Ula(Clock);
        var freeRunning = new Ula(Clock);
        ula.SetTapeInput(level);
        for (var tick = 0; tick < Clock / 100; ++tick)
        {
            ula.Tick(true, false);
            freeRunning.Tick(true, false);
        }
        var pins = 0;
        var clocks = 0;
        ula.SerialPinsChanged = (_, _, _, _) => ++pins;
        ula.SerialClocksRaised = (_, _, _) => ++clocks;
        for (var tick = 0; tick < Clock / 100; ++tick)
        {
            ula.SetTapeInput(level); // Représenter le même niveau ne crée aucun front.
            var data = (tick / 137 & 1) != 0;
            ula.Tick(data, false);
            freeRunning.Tick(data, false);
            Assert.Equal(freeRunning.SourceClockPhase, ula.SourceClockPhase);
            Assert.Equal(freeRunning.TransmitClockEdge, ula.TransmitClockEdge);
            Assert.Equal(freeRunning.ReceiveClockEdge, ula.ReceiveClockEdge);
            Assert.Equal(freeRunning.TapeOutput, ula.TapeOutput);
        }
        Assert.Equal(0, pins);
        Assert.True(clocks > 0);

        ula.SetTapeInput(null);
        ula.Tick(true, false);
        for (var tick = 0; tick < Clock / 4; ++tick)
        {
            ula.SetTapeInput(((long)tick * 4800 / Clock & 1) != 0);
            ula.Tick(true, false);
            if (tick > Clock / 5) Assert.True(ula.DataCarrierDetectHigh);
        }
        Assert.True(pins > 0);
        ula.SetTapeInput(null);
        for (var tick = 0; tick < Clock / 500; ++tick) ula.Tick(true, false);
        Assert.False(ula.DataCarrierDetectHigh);
        Assert.True(ula.ReceiveData);
    }

    [Fact]
    public void SelectedRs423InputsPropagateWithoutAdvancingTheClock()
    {
        var ula = new Ula(Clock);
        ula.Write(BbcSerialUlaConstants.Rs423Mask);
        var levels = new List<(bool Cts, bool Dcd, bool Data, bool Selected)>();
        var clocks = 0;
        ula.SerialPinsChanged = (cts, dcd, data, selected) => levels.Add((cts, dcd, data, selected));
        ula.SerialClocksRaised = (_, _, _) => ++clocks;
        var phase = ula.SourceClockPhase;
        ula.Rs423ClearToSendHigh = false;
        Assert.Equal(new[] { (false, false, true, true) }, levels);
        ula.Rs423ReceiveData = false;
        Assert.Equal((false, false, false, true), levels[^1]);
        Assert.Equal(2, levels.Count);
        ula.Rs423ReceiveData = false;
        ula.Rs423ClearToSendHigh = false;
        Assert.Equal(2, levels.Count);
        Assert.Equal(phase, ula.SourceClockPhase);
        Assert.Equal(0, clocks);
    }

    [Fact]
    public void TapeSelectionMasksRs423InputsUntilTheMuxIsSwitched()
    {
        var ula = new Ula(Clock);
        var levels = new List<(bool Cts, bool Dcd, bool Data, bool Selected)>();
        ula.SerialPinsChanged = (cts, dcd, data, selected) => levels.Add((cts, dcd, data, selected));
        ula.Rs423ReceiveData = false;
        ula.Rs423ClearToSendHigh = false;
        Assert.Empty(levels);
        ula.Write(BbcSerialUlaConstants.Rs423Mask);
        Assert.Equal(new[] { (false, false, false, true) }, levels);
        ula.Write(BbcSerialUlaConstants.Rs423Mask);
        Assert.Single(levels);
        ula.Write(0);
        Assert.Equal((false, false, true, false), levels[^1]);
    }

    [Fact]
    public void InputAndControlWritesDoNotReplayTheLastSerialClockEdge()
    {
        var ula = new Ula(Clock);
        ula.Write(BbcSerialUlaConstants.Rs423Mask);
        var clocks = 0;
        ula.SerialClocksRaised = (_, _, _) => ++clocks;
        for (var tick = 0; tick < BbcSerialUlaConstants.ExternalClockDivider && clocks == 0; ++tick)
            ula.Tick(true, true);
        Assert.True(clocks > 0);
        var previousClocks = clocks;
        var phase = ula.SourceClockPhase;
        ula.Rs423ReceiveData = false;
        ula.Rs423ClearToSendHigh = false;
        ula.Write(0);
        ula.Write(BbcSerialUlaConstants.Rs423Mask);
        Assert.Equal(previousClocks, clocks);
        Assert.Equal(phase, ula.SourceClockPhase);
    }
}

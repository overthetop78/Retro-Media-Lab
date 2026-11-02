using RetroMediaLab.Chips.Memory.Generic.I2C.EEPROM128.Services;
using RetroMediaLab.Chips.IO.Acorn.Tube.ULA.Services;
using RetroMediaLab.Chips.Custom.Acorn.Master512.Refresh.Services;
using RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Services;

namespace RetroMediaLab.Chips.Tests.Custom.Acorn.Master512.Refresh;
public sealed class RefreshTests
{
    [Fact]
    public void Master512RefreshLatchesTimerEdgeUntilHldaAndIgnoresRequestsDuringAcknowledge()
    {
        var refresh = new Master512Refresh();
        refresh.TimerOutput(false);
        Assert.True(refresh.HoldRequested);
        refresh.TimerOutput(true);
        Assert.True(refresh.HoldRequested);
        refresh.HoldAcknowledged(true);
        Assert.False(refresh.HoldRequested);
        refresh.TimerOutput(false);
        Assert.False(refresh.HoldRequested);
        refresh.HoldAcknowledged(false);
        Assert.False(refresh.HoldRequested);
        refresh.TimerOutput(true);
        refresh.TimerOutput(false);
        Assert.True(refresh.HoldRequested);
    }

    [Fact]
    public void Master512RefreshSamplesHldaOnClockAndRomSelectPresetsItsSecondFlipFlop()
    {
        var refresh = new Master512Refresh();
        refresh.TimerOutput(false);
        refresh.HoldAcknowledged(true);
        Assert.False(refresh.RefreshActive);
        refresh.AdvanceClocks(1);
        Assert.True(refresh.RefreshActive);
        refresh.UpperMemorySelected(true);
        Assert.False(refresh.RefreshActive);
        refresh.AdvanceClocks(1);
        Assert.False(refresh.RefreshActive);
        refresh.UpperMemorySelected(false);
        Assert.False(refresh.RefreshActive);
        refresh.AdvanceClocks(1);
        Assert.True(refresh.RefreshActive);
        refresh.HoldAcknowledged(false);
        Assert.True(refresh.RefreshActive);
        refresh.AdvanceClocks(1);
        Assert.False(refresh.RefreshActive);
    }

    [Fact]
    public void Master512RefreshResetClearsBothFlipFlopsAndRestoresTimerEdgeDetection()
    {
        var refresh = new Master512Refresh();
        refresh.HoldAcknowledged(true);
        refresh.AdvanceClocks(1);
        refresh.Reset();
        Assert.False(refresh.HoldRequested);
        Assert.False(refresh.RefreshActive);
        refresh.TimerOutput(false);
        Assert.True(refresh.HoldRequested);
    }
}

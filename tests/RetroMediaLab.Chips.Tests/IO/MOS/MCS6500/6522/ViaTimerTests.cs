using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;
using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;
using RetroMediaLab.Chips.CPU.Common.Interfaces;
using System.IO;

namespace RetroMediaLab.Chips.Tests.IO.MOS.MCS6500.Chip6522;
public sealed class ViaTimerTests
{
    [Fact]
    public void ViaAcknowledgesTimerInterruptOnlyThroughItsRegister()
    {
        var via = new Via6522();
        via.Reset();
        via.Write(14, 0xc0);
        via.Write(4, 1);
        via.Write(5, 0);
        via.Tick();
        via.Tick();
        via.Tick(); // Chargement puis N+1 décrémentations.
        Assert.True(via.Irq);
        Assert.Equal(0xc0, via.Read(13));
        via.Tick();
        via.Read(4);
        Assert.False(via.Irq);
    }
}

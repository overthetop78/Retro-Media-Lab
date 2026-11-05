using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;
using C = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Constants.Via6522Constants;

namespace RetroMediaLab.Chips.Tests.IO.MOS.MCS6500.Chip6522;

public sealed class InterruptNotificationsTests
{
    [Fact]
    public void NotificationsMatchIfrIerAfterEveryBusAndPinOperation()
    {
        var via = new Via6522();
        var observed = false;
        var changes = 0;
        via.IrqChanged = level =>
        {
            Assert.NotEqual(observed, level);
            Assert.Equal(level, via.Irq);
            observed = level;
            changes++;
        };
        var random = new Random(6522);
        for (var operation = 0; operation < 20000; operation++)
        {
            var value = (byte)random.Next(256);
            switch (random.Next(10))
            {
                case 0: via.Write(random.Next(C.RegisterCount), value); break;
                case 1: via.Read(random.Next(C.RegisterCount)); break;
                case 2: via.SetCa1((value & 1) != 0); break;
                case 3: via.SetCa2((value & 1) != 0); break;
                case 4: via.SetCb1((value & 1) != 0); break;
                case 5: via.SetCb2((value & 1) != 0); break;
                case 6: via.SetPb6((value & 1) != 0); break;
                case 7: via.Phi2LeadingEdge(); break;
                case 8: via.Phi2TrailingEdge(); break;
                default:
                    if (value == 0) via.Reset();
                    else via.Tick();
                    break;
            }
            var expected = (via.Read(C.Flags) & via.Read(C.Enable) & C.InterruptMask) != 0;
            Assert.Equal(expected, via.Irq);
            Assert.Equal(expected, observed);
        }
        Assert.True(changes > 100);
        via.Reset();
        Assert.False(observed);
    }

    [Fact]
    public void TimerIrqIsPublishedAtTheLeadingEdgeAndOnlyOnLevelChanges()
    {
        var via = new Via6522();
        var levels = new List<bool>();
        via.IrqChanged = levels.Add;
        via.Write(C.Enable, C.InterruptSummary | C.Timer1Interrupt);
        via.Write(C.Timer1Low, 0);
        via.Write(C.Timer1High, 0);
        for (var cycle = 0; cycle < 4 && !via.Irq; cycle++)
        {
            via.Phi2TrailingEdge();
            Assert.Empty(levels);
            via.Phi2LeadingEdge();
        }
        Assert.Equal(new[] { true }, levels);
        via.Write(C.Enable, C.InterruptSummary | C.Timer1Interrupt);
        Assert.Equal(new[] { true }, levels);
        via.Write(C.Flags, C.Timer1Interrupt);
        Assert.Equal(new[] { true, false }, levels);
        via.Reset();
        Assert.Equal(new[] { true, false }, levels);
    }
}

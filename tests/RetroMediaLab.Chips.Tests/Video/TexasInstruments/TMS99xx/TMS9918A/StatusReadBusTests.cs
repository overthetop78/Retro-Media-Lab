using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.TexasInstruments.TMS99xx.TMS9918A;

public sealed class StatusReadBusTests
{
    [Fact]
    public void ProvisionalRisingCsrAcknowledgesStatusAndProtocolExactlyOnceAfterRestore()
    {
        var status = new Tms9918Status(0);
        status.FifthSpriteDetected(9);
        status.SpriteCoincidence();
        status.EndOfActiveDisplay();
        var protocol = new Tms9918CpuProtocol(new(0, false));
        protocol.AcceptControlByte(0x55);
        var pins = new Tms9918CpuPins(0, false, true, true, true);
        var bus = new Tms9918StatusReadBus(new(0, pins, false, false), status, protocol);
        bus.ReceivePins(100, pins with { CsrHigh = false });
        Assert.Equal(0xe9, status.Value);
        Assert.True(status.InterruptAsserted(true));
        Assert.True(protocol.Capture().AwaitingSecondByte);
        bus = new(bus.Capture(), status, protocol);
        int acknowledgments = 0;
        try
        {
            bus.StatusAcknowledged = time =>
            {
                acknowledgments++;
                Assert.Equal(300, time);
                Assert.Equal(9, status.Value);
                Assert.False(status.InterruptAsserted(true));
                Assert.False(protocol.Capture().AwaitingSecondByte);
                bus.ReceivePins(time, pins);
                Assert.Throws<InvalidOperationException>(() => bus.ReceivePins(time + 1, pins));
            };
            bus.ReceivePins(250, pins with { CsrHigh = false });
            Assert.Equal(0xe9, status.Value);
            bus.ReceivePins(300, pins);
            bus.ReceivePins(400, pins);
            Assert.Equal(1, acknowledgments);
        }
        finally { bus.StatusAcknowledged = null; }

        status.EndOfActiveDisplay();
        protocol.AcceptControlByte(0x66);
        // MODE=0 : une lecture de données ne doit pas acquitter le status.
        pins = pins with { Mode = false };
        bus.ReceivePins(500, pins with { CsrHigh = false });
        bus.ReceivePins(700, pins);
        Assert.True(status.InterruptAsserted(true));
        Assert.True(protocol.Capture().AwaitingSecondByte);
        // Sélections simultanées : pas de résultat matériel inventé.
        pins = pins with { Mode = true };
        bus.ReceivePins(800, pins with { CsrHigh = false, CswHigh = false });
        bus.ReceivePins(1000, pins);
        Assert.False(bus.Capture().SelectionQualified);
        Assert.True(status.InterruptAsserted(true));
        Assert.True(protocol.Capture().AwaitingSecondByte);
    }
}

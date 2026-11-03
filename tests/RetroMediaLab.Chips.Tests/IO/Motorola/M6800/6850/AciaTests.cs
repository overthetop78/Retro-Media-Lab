using RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Services;

namespace RetroMediaLab.Chips.Tests.IO.Motorola.M6800.Chip6850;
public sealed class AciaTests
{
    [Fact]
    public void AciaPreservesUnreadDataOnOverrunAndAcknowledgesInterrupts()
    {
        var acia = new Acia6850(2_000_000, 19200);
        acia.Write(false, 0x95);
        void Receive(byte value)
        {
            acia.BeginReceive();
            for (var bit = 0; bit < 8; bit++)
                acia.ReceiveBit((value & (1 << bit)) != 0);
            acia.ReceiveBit(true);
        }

        Receive(42);
        Receive(99);
        Assert.True(acia.Irq);
        Assert.Equal(0, acia.Read(false) & 0x20); // La perte est encore latente.
        Assert.Equal(42, acia.Read(true));
        Assert.Equal(0x21, acia.Read(false) & 0x21);
        Assert.True(acia.Irq);
        acia.Read(true);
        Assert.False(acia.Irq);
        acia.SetDcd(true);
        acia.ClockReceive(true);
        acia.SetDcd(false);
        acia.ClockReceive(true);
        Assert.True(acia.Irq);
        acia.Read(false);
        acia.Read(true);
        Assert.False(acia.Irq);
    }
}

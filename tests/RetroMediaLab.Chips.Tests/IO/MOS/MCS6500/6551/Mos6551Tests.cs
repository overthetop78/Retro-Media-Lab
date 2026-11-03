using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6551.Services;
namespace RetroMediaLab.Chips.Tests.IO.MOS.MCS6500.Chip6551;

public sealed class Mos6551Tests
{
    [Theory]
    [InlineData(0x9f, 8, 33)]
    [InlineData(0xff, 5, 28)]
    public void ReceiveRegisterIsNotReleasedBeforeConfiguredStopTime(int control, int bits, int stopClocks)
    {
        var receiver = Device((byte)control);
        receiver.SetRxd(false);
        receiver.Advance(6 * 16);
        for (var bit = 0; bit < bits; bit++)
        {
            receiver.SetRxd((0x15 & (1 << bit)) != 0);
            receiver.Advance(6 * 16);
        }
        receiver.SetRxd(true);
        receiver.Advance(6 * (stopClocks - 8 - 1));
        Assert.Equal(0, receiver.Read(1) & 8);
        receiver.Advance(6);
        Assert.Equal(8, receiver.Read(1) & 8);
        Assert.Equal(0x15, receiver.Read(0));
    }

    [Theory]
    [InlineData(0x1f, 0x09, 0xa5)] // 8N1
    [InlineData(0x3f, 0x29, 0x65)] // 7O1
    [InlineData(0x5f, 0x69, 0x25)] // 6E1
    [InlineData(0xff, 0x09, 0x15)] // 5N1,5
    [InlineData(0x9f, 0xa9, 0xa5)] // 8M1 malgré le bit d'arrêt supplémentaire
    [InlineData(0x3f, 0xe9, 0x65)] // 7S1
    public void RealSerialPinsTransferConfiguredWords(int control, int command, int data)
    {
        var sender = Device((byte)control, (byte)command);
        var receiver = Device((byte)control, (byte)command);
        sender.TransmitChanged = receiver.SetRxd;
        sender.Write(0, (byte)data);
        ClockPair(sender, receiver, 240);
        var status = receiver.Read(1);
        Assert.Equal(0x88, status & 0x8f);
        Assert.Equal((byte)data, receiver.Read(0));
        Assert.False(receiver.Irq); // Le statut acquitte IRQ, pas la lecture des données.
        Assert.Equal(0, receiver.Read(1) & 0x0f);
    }

    [Fact]
    public void TransmitHoldingRegisterQueuesASecondCharacterWhileFirstIsShifting()
    {
        var sender = Device(0x1f, 0x05);
        var receiver = Device();
        sender.TransmitChanged = receiver.SetRxd;
        sender.Write(0, 0x41);
        ClockPair(sender, receiver, 2);
        Assert.True(sender.Irq);
        Assert.Equal(0x90, sender.Read(1) & 0x90);
        sender.Write(0, 0x42);
        Assert.Equal(0, sender.Read(1) & 0x10);
        ClockPair(sender, receiver, 153);
        Assert.Equal(0x41, receiver.Read(0));
        ClockPair(sender, receiver, 161);
        Assert.Equal(0x42, receiver.Read(0));
        Assert.Equal(0x10, sender.Read(1) & 0x10);
    }

    [Fact]
    public void ExternalReceiveClockDoesNotDependOnCrystalBaudSelection()
    {
        var receiver = Device(0x01); // 50 bauds TX ; RxC externe.
        Send(receiver, 0x93, external: true);
        Assert.Equal(0x88, receiver.Read(1) & 0x8f);
        Assert.Equal(0x93, receiver.Read(0));
    }

    [Fact]
    public void ParityFramingAndOverrunBelongToReceivedCharacters()
    {
        var receiver = Device(0x1f, 0x29); // 8 bits, parité impaire.
        Send(receiver, 0x55, parity: false, stop: false);
        Assert.Equal(0x0b, receiver.Read(1) & 0x0f);
        Assert.Equal(0x55, receiver.Read(0));
        Assert.Equal(0, receiver.Read(1) & 0x0f);
        // Le débordement se vérifie sans break : deux caractères valides sans lecture RDR.
        receiver = Device();
        Send(receiver, 0x31);
        Send(receiver, 0x32);
        Assert.Equal(0x0c, receiver.Read(1) & 0x0f);
        Assert.Equal(0x31, receiver.Read(0));
        Assert.Equal(0, receiver.Read(1) & 0x0f);
    }

    [Fact]
    public void ReadingDataDoesNotAcknowledgeTheInterruptLatch()
    {
        var receiver = Device();
        Send(receiver, 0x77);
        Assert.Equal(0x77, receiver.Read(0));
        Assert.True(receiver.Irq);
        Assert.Equal(0x80, receiver.Read(1) & 0x80);
        Assert.False(receiver.Irq);
    }

    [Fact]
    public void DisabledReceiverCarrierAndFalseStartsDoNotDeliverCharacters()
    {
        var receiver = Device();
        receiver.Write(2, 0);
        Send(receiver, 0x77);
        Assert.Equal(0, receiver.Read(1) & 0x88);
        receiver.Write(2, 0x09);
        receiver.SetDcd(true);
        receiver.Read(1);
        Send(receiver, 0x77);
        Assert.Equal(0, receiver.Read(1) & 0x08);
        receiver.SetDcd(false);
        receiver.Read(1);
        receiver.SetRxd(false);
        receiver.Advance(6 * 4);
        receiver.SetRxd(true);
        receiver.Advance(6 * 200);
        Assert.Equal(0, receiver.Read(1) & 0x08);
    }

    [Fact]
    public void ModemChangesInterruptOnlyWhenEnabledAndCtsGatesTransmission()
    {
        var device = new Mos6551(1843200);
        Assert.True(device.Dtr);
        Assert.True(device.Rts);
        Assert.Equal(0x60, device.Read(1));
        device.SetDsr(false);
        Assert.False(device.Irq);
        device.Write(2, 0x09);
        device.SetDcd(false);
        Assert.True(device.Irq);
        Assert.Equal(0x80, device.Read(1));
        Assert.False(device.Irq);
        device.Write(3, 0x1f);
        device.Write(0, 0x55);
        device.Advance(6 * 40);
        Assert.True(device.Txd);
        device.SetCts(false);
        device.Advance(6);
        Assert.False(device.Txd);
        Assert.Equal(0x10, device.Read(1) & 0x10);
    }

    [Fact]
    public void ProgramResetPreservesParityControlAndUnreadData()
    {
        var device = Device(0x1f, 0xa9);
        Send(device, 0x56, parity: true);
        device.Write(1, 0xff);
        Assert.Equal(0xa2, device.Read(2));
        Assert.Equal(0x1f, device.Read(3));
        Assert.Equal(0x56, device.Read(0));
        Assert.True(device.Irq); // Program reset disables new RX IRQ but does not acknowledge its existing latch.
        Assert.Equal(0x80, device.Read(1) & 0x80);
        Assert.False(device.Irq);
        device.Reset();
        Assert.Equal(2, device.Read(2));
        Assert.Equal(0, device.Read(3));
        Assert.Equal(0x10, device.Read(1));
    }

    [Fact]
    public void BreakAndEchoDriveTheSerialPin()
    {
        var device = Device(0x1f, 0x0d);
        device.Advance(6);
        Assert.False(device.Txd);
        device.Write(2, 0x11);
        device.SetRxd(true);
        device.Advance(6);
        Assert.True(device.Txd);
        device.SetRxd(false);
        device.Advance(6 * 10);
        Assert.False(device.Txd);
    }

    private static Mos6551 Device(byte control = 0x1f, byte command = 0x09)
    {
        var device = new Mos6551(1843200);
        device.SetCts(false);
        device.SetDcd(false);
        device.SetDsr(false);
        device.Write(3, control);
        device.Write(2, command);
        return device;
    }

    private static void ClockPair(Mos6551 sender, Mos6551 receiver, int clocks)
    {
        for (var tick = 0; tick < clocks; tick++)
        {
            sender.Advance(6);
            receiver.Advance(6);
        }
    }

    private static void Send(Mos6551 device, byte data, bool? parity = null, bool stop = true, bool external = false)
    {
        Bit(false);
        for (var bit = 0; bit < 8; bit++) Bit((data & (1 << bit)) != 0);
        if (parity.HasValue) Bit(parity.Value);
        Bit(stop);
        void Bit(bool high)
        {
            device.SetRxd(high);
            if (external) device.AdvanceReceiveClock(32);
            else device.Advance(6 * 16);
        }
    }
}

using RetroMediaLab.Chips.IO.ChipsAndTechnologies.SuperIO.Chip82C711.Enums;
using RetroMediaLab.Chips.IO.ChipsAndTechnologies.SuperIO.Chip82C711.Services;
namespace RetroMediaLab.Chips.Tests.IO.ChipsAndTechnologies.SuperIO.Chip82C711;

public sealed class Upc82C711Tests
{
    [Fact]
    public void ConfigurationRequiresTwoKeysAndAnInterruptedSequenceDoesNotUnlock()
    {
        var device = new Upc82C711Configuration();
        device.Write(0x3f1, 0);
        Assert.Equal(0xff, device.Read(0x3f1));
        device.Write(0x3f0, 0x55);
        Assert.False(device.IsUnlocked);
        device.Write(0x3f0, 0x12);
        device.Write(0x3f0, 0x55);
        Assert.False(device.IsUnlocked);
        device.Write(0x3f0, 0x55);
        Assert.True(device.IsUnlocked);
        device.Write(0x3f0, 0);
        Assert.Equal(0x3f, device.Read(0x3f1));
        device.Write(0x3f0, 0xaa);
        Assert.False(device.IsUnlocked);
        Assert.Equal(0xff, device.Read(0x3f1));
    }

    [Fact]
    public void IndexUsesOnlyFourBitsAndRelockingPreservesConfiguration()
    {
        var device = Unlocked();
        device.Write(0x3f0, 0x73);
        device.Write(0x3f1, 0x63);
        device.Write(0x3f0, 0xaa);
        device.Write(0x3f1, 0);
        device.Write(0x3f0, 0x55);
        device.Write(0x3f0, 0x55);
        device.Write(0x3f0, 3);
        Assert.Equal(0x63, device.Read(0x3f1));
        device.PowerOn();
        Assert.False(device.IsUnlocked);
        device.Write(0x3f0, 0x55);
        device.Write(0x3f0, 0x55);
        device.Write(0x3f0, 3);
        Assert.Equal(0, device.Read(0x3f1));
    }

    [Fact]
    public void DisabledReadbackAndUnimplementedIndicesDoNotExposeConfiguration()
    {
        var device = Unlocked();
        Configure(device, 1, 0x1f);
        device.Write(0x3f0, 0);
        Assert.Equal(0xff, device.Read(0x3f1));
        Configure(device, 1, 0x9f);
        device.Write(0x3f0, 0);
        Assert.Equal(0x3f, device.Read(0x3f1));
        Configure(device, 4, 0x63);
        Assert.Equal(0xff, device.Read(0x3f1));
        device.Write(0x3f0, 0);
        Assert.Equal(0x3f, device.Read(0x3f1));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnotherIoAccessBreaksAnIncompleteUnlock(bool read)
    {
        var device = new Upc82C711Configuration();
        device.Write(0x3f0, 0x55);
        if (read) device.Read(0x278);
        else device.Write(0x278, 0x55);
        device.Write(0x3f0, 0x55);
        Assert.False(device.IsUnlocked);
        device.Write(0x3f0, 0x55);
        Assert.True(device.IsUnlocked);
    }

    [Theory]
    [InlineData(0x1f0, (int)UpcPeripheral.Ide, 0)]
    [InlineData(0x1f7, (int)UpcPeripheral.Ide, 7)]
    [InlineData(0x3f6, (int)UpcPeripheral.IdeControl, 6)]
    [InlineData(0x3f4, (int)UpcPeripheral.Floppy, 4)]
    [InlineData(0x3f7, (int)UpcPeripheral.Floppy, 7)]
    [InlineData(0x278, (int)UpcPeripheral.Parallel, 0)]
    [InlineData(0x3f8, (int)UpcPeripheral.Serial1, 0)]
    [InlineData(0x2ff, (int)UpcPeripheral.Serial2, 7)]
    [InlineData(0x3f0, (int)UpcPeripheral.Configuration, 0)]
    [InlineData(0x3f1, (int)UpcPeripheral.Configuration, 1)]
    public void ResetDecodesTheDocumentedPowerOnPorts(int port, int expected, int expectedRegister)
    {
        var device = new Upc82C711Configuration();
        Assert.True(device.TryDecode(port, out var peripheral, out var register));
        Assert.Equal(expected, (int)peripheral);
        Assert.Equal(expectedRegister, register);
    }

    [Fact]
    public void DisabledBlocksLeaveTheirPortsUndecoded()
    {
        var device = Unlocked();
        Configure(device, 0, 0);
        Configure(device, 1, 0);
        Configure(device, 2, 0);
        foreach (var port in new[] { 0x1f0, 0x3f6, 0x3f4, 0x278, 0x3f8, 0x2f8 })
            Assert.False(device.TryDecode(port, out _, out _));
        Assert.True(device.TryDecode(0x3f0, out var peripheral, out _));
        Assert.Equal(UpcPeripheral.Configuration, peripheral);
    }

    [Theory]
    [InlineData(0x08)]
    [InlineData(0x10)]
    public void FloppyRequiresBothConfigurationEnables(int enable)
    {
        var device = Unlocked();
        Configure(device, 0, (byte)enable);
        Assert.False(device.TryDecode(0x3f4, out _, out _));
        Configure(device, 0, 0x18);
        Assert.True(device.TryDecode(0x3f4, out var peripheral, out _));
        Assert.Equal(UpcPeripheral.Floppy, peripheral);
    }

    [Theory]
    [InlineData(0, 0x338, 0x238)]
    [InlineData(1, 0x3e8, 0x2e8)]
    [InlineData(2, 0x2e8, 0x2e0)]
    [InlineData(3, 0x220, 0x228)]
    public void AuxiliarySerialAddressPairsAreSelectedTogether(int pair, int com3, int com4)
    {
        var device = Unlocked();
        Configure(device, 1, (byte)(pair << 5));
        Configure(device, 2, 0xfe);
        Assert.False(device.TryDecode(0x3f8, out _, out _));
        Assert.True(device.TryDecode(com3, out var first, out var firstRegister));
        Assert.True(device.TryDecode(com4 + 7, out var second, out var secondRegister));
        Assert.Equal(UpcPeripheral.Serial1, first);
        Assert.Equal(0, firstRegister);
        Assert.Equal(UpcPeripheral.Serial2, second);
        Assert.Equal(7, secondRegister);
    }

    [Theory]
    [InlineData(0x05, 0x3bc)]
    [InlineData(0x06, 0x378)]
    [InlineData(0x07, 0x278)]
    public void ParallelPortMovesWithoutKeepingItsOldAlias(int control, int port)
    {
        var device = Unlocked();
        Configure(device, 1, (byte)control);
        foreach (var candidate in new[] { 0x3bc, 0x378, 0x278 })
            Assert.Equal(candidate == port, device.TryDecode(candidate, out _, out _));
        Assert.True(device.TryDecode(port + 2, out var peripheral, out var register));
        Assert.Equal(UpcPeripheral.Parallel, peripheral);
        Assert.Equal(2, register);
        Configure(device, 1, 0x04);
        Assert.False(device.TryDecode(port, out _, out _));
    }

    [Fact]
    public void SerialPortCollisionKeepsTheSecondPortDecodePriority()
    {
        var device = Unlocked();
        Configure(device, 2, 0xcc);
        Assert.True(device.TryDecode(0x3ff, out var peripheral, out var register));
        Assert.Equal(UpcPeripheral.Serial2, peripheral);
        Assert.Equal(7, register);
        Assert.False(device.Write(0x3ff, 0));
    }

    private static Upc82C711Configuration Unlocked()
    {
        var device = new Upc82C711Configuration();
        device.Write(0x3f0, 0x55);
        device.Write(0x3f0, 0x55);
        return device;
    }

    private static void Configure(Upc82C711Configuration device, byte index, byte value)
    {
        device.Write(0x3f0, index);
        device.Write(0x3f1, value);
    }
}

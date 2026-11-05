using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed class Acb4000EccRegisterTests
{
    [Theory]
    [InlineData("A1FE00000000", 0x99b7f53eu)]
    [InlineData("A1FE00123480", 0xcfffce8fu)]
    [InlineData("313233343536373839", 0xffe64cebu)]
    public void ProgrammedFirmwareTapsProduceIndependentAdaptecVectors(string hex, uint expected)
    {
        var ecc = Create(0x41044185);
        foreach (var value in Convert.FromHexString(hex)) Assert.True(ecc.TryClockDataByte(value));
        Assert.Equal(expected, ReadFull(ecc));
    }

    [Theory]
    [InlineData(0x00, 0x10210000u, 0x31c30000u)]
    [InlineData(0x40, 0x864cfb00u, 0xcde70300u)]
    [InlineData(0xc0, 0x41044185u, 0xffe64cebu)]
    public void DocumentedLengthsMatchKnownZeroInitialisedCrcVectors(byte control, uint polynomial, uint expected)
    {
        var ecc = Create(polynomial);
        ecc.WriteControl(control);
        foreach (var value in "123456789"u8) Assert.True(ecc.TryClockDataByte(value));
        Assert.Equal(expected, ReadFull(ecc));
    }

    [Theory]
    [InlineData(0u, 0)]
    [InlineData(1u, 1)]
    [InlineData(0x10000u, 1)]
    [InlineData(0x20000u, 2)]
    [InlineData(0x12345678u, 0x35)]
    [InlineData(0xff000000u, 0)]
    public void SerialLoadingAndMiddleReadCombineUpperByteWithLowSeventeenBitOr(uint syndrome, byte expectedMiddle)
    {
        var ecc = new Acb4000EccRegisters();
        LoadSerial(ecc, syndrome);
        Assert.Equal(expectedMiddle, ecc.ReadMiddleSyndrome());
        Assert.Equal((byte)(syndrome >> 24), ecc.ReadHighSyndrome());
        Assert.Equal(syndrome, ReadFull(ecc));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void SoftwareShiftRequiresInactiveReadAndWriteGates(bool readGate, bool writeGate)
    {
        var ecc = Create(0x41044185);
        ecc.WriteControl(0xc7);
        Assert.False(ecc.TryClockReference(readGate, writeGate));
        Assert.Equal(0u, ReadFull(ecc));
    }

    [Fact]
    public void SoftwareShiftWaitsForReferenceAndGeneratesOnlyOnePulsePerRequest()
    {
        var ecc = Create(0x41044185);
        ecc.WriteControl(0xc7);
        Assert.Equal(0, ecc.ReadMiddleSyndrome());
        Assert.True(ecc.TryClockReference(false, false));
        Assert.False(ecc.TryClockReference(false, false));
        Assert.Equal(1u, ReadFull(ecc));
    }

    [Fact]
    public void ClearHoldsSyndromeAtZeroAcrossDataAndSoftwareClocksUntilReleased()
    {
        var ecc = Create(0x41044185);
        Assert.True(ecc.TryClockDataByte(0xff));
        ecc.WriteControl(0xcc);
        Assert.True(ecc.TryClockDataByte(0xff));
        ecc.WriteControl(0xcf);
        Assert.True(ecc.TryClockReference(false, false));
        Assert.Equal(0u, ReadFull(ecc));
        ecc.WriteControl(0xc0);
        Assert.True(ecc.TryClockDataByte(0xff));
        Assert.NotEqual(0u, ReadFull(ecc));
    }

    [Fact]
    public void ExternalResetPreservesTapsAndHoldsDataflowUntilControlReleasesReset()
    {
        var ecc = Create(0x41044185);
        Assert.True(ecc.TryClockDataByte(0x55));
        ecc.Reset();
        Assert.True(ecc.ResetAsserted);
        Assert.Equal(0x41044185u, ecc.Polynomial);
        Assert.Equal(0, ecc.ReadHighSyndrome());
        Assert.False(ecc.TryClockDataByte(0xff));
        ecc.WriteControl(0xc0);
        foreach (var value in "123456789"u8) Assert.True(ecc.TryClockDataByte(value));
        Assert.Equal(0xffe64cebu, ReadFull(ecc));
    }

    [Fact]
    public void HighestPolynomialRegisterIgnoresItsUnusedHighBitAndOtherBytesArePreserved()
    {
        var ecc = Create(0x41044185);
        ecc.WritePolynomial(0x77, 0xa0);
        Assert.Equal(0x41044185u, ecc.Polynomial);
        ecc.WritePolynomial(0x75, 0);
        Assert.Equal(0x41040185u, ecc.Polynomial);
        Assert.Throws<ArgumentOutOfRangeException>(() => ecc.WritePolynomial(0x73, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => ecc.WritePolynomial(0x78, 0));
    }

    [Fact]
    public void UndocumentedLengthIsExplicitlyReportedWithoutInventingNewSyndromeBits()
    {
        var ecc = Create(0x41044185);
        LoadSerial(ecc, 0x12345678);
        ecc.WriteControl(0x80);
        Assert.False(ecc.IsLengthDocumented);
        Assert.False(ecc.TryClockDataByte(0xff));
        Assert.Equal(0x12345678u, ReadFull(ecc));
    }

    [Fact]
    public void FeedbackDisabledClocksAreAnActualShiftRegister()
    {
        var ecc = Create(0x41044185);
        ecc.WriteControl(0xc4);
        foreach (var value in new byte[] { 0x12, 0x34, 0x56, 0x78 }) Assert.True(ecc.TryClockDataByte(value));
        Assert.Equal(0x12345678u, ReadFull(ecc));
    }

    [Fact]
    public void DataClockDoesNotAllocate()
    {
        var ecc = Create(0x41044185);
        for (var i = 0; i < 1000; i++) ecc.TryClockDataByte((byte)i);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 10000; i++) ecc.TryClockDataByte((byte)i);
        Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
    }

    [Theory]
    [InlineData(0x41044185u)]
    [InlineData(0x04c11db7u)]
    public void PhysicalReadChannelFeedsTheSameProgrammableEccRegisters(uint polynomial)
    {
        var ecc = Create(polynomial);
        var channel = new Acb4000ReadChannel(ecc);
        var record = new byte[262];
        Acb4000FieldCodec.WriteData(record, Enumerable.Range(0, 256).Select(index => (byte)index).ToArray(), polynomial);
        FeedRecord(channel, record);
        Assert.True(channel.IntegrityValid);
        Assert.True(ecc.CheckPassed);
        record[17] ^= 0x08;
        FeedRecord(channel, record);
        Assert.False(channel.IntegrityValid);
        Assert.False(ecc.CheckPassed);
        Assert.Equal(Acb4000ErrorCheck.Compute(record, polynomial), ReadFull(ecc));
    }

    [Fact]
    public void SharedRegistersCannotBeBypassedByASecondFixedPolynomialCheck()
    {
        var ecc = Create(0x04c11db7);
        var channel = new Acb4000ReadChannel(ecc);
        var record = new byte[262];
        Acb4000FieldCodec.WriteData(record, new byte[256]);
        FeedRecord(channel, record);
        Assert.False(channel.IntegrityValid);
        Assert.False(ecc.CheckPassed);
    }

    private static void FeedRecord(Acb4000ReadChannel channel, byte[] record)
    {
        var words = new ushort[record.Length];
        var previous = false;
        Acb4000MfmEncoding.WriteField(record, words, ref previous);
        foreach (var word in words)
            for (var bit = 15; bit >= 0; bit--) channel.Feed((word & (1 << bit)) != 0);
    }

    private static Acb4000EccRegisters Create(uint polynomial)
    {
        var ecc = new Acb4000EccRegisters();
        var taps = polynomial >> 1;
        for (var register = 0x74; register <= 0x77; register++, taps >>= 8)
            ecc.WritePolynomial(register, (byte)taps);
        ecc.WriteControl(0xc0);
        return ecc;
    }

    private static void LoadSerial(Acb4000EccRegisters ecc, uint value)
    {
        ecc.WriteControl(0xc8);
        for (var bit = 31; bit >= 0; bit--)
        {
            ecc.WriteControl((byte)(0xc6 | ((value >> bit) & 1)));
            Assert.True(ecc.TryClockReference(false, false));
        }
    }

    private static uint ReadFull(Acb4000EccRegisters ecc)
    {
        ecc.WriteControl(0xc4);
        uint result = 0;
        for (var index = 0; index < 4; index++)
        {
            result = (result << 8) | ecc.ReadHighSyndrome();
            for (var shift = 0; shift < 8; shift++)
            {
                ecc.WriteControl(0xc6);
                Assert.True(ecc.TryClockReference(false, false));
            }
        }
        return result;
    }
}

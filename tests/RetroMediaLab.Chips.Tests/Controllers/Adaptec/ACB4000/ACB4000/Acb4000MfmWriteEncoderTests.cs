using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Interfaces;
using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Adaptec.ACB4000.ACB4000;

public sealed class Acb4000MfmWriteEncoderTests
{
    [Fact]
    public void EveryByteAndBothPreviousBitsMatchMfmEncodingAfterThreeBits()
    {
        for (var previousBit = 0; previousBit < 2; previousBit++)
            for (var value = 0; value <= byte.MaxValue; value++)
            {
                var encoder = new Acb4000MfmWriteEncoder();
                encoder.ClockBit(previousBit != 0, false);
                var previous = previousBit != 0;
                var expected = Acb4000MfmEncoding.Encode((byte)value, ref previous);
                Assert.Equal(expected, EncodeByte(encoder, (byte)value));
            }
    }

    [Fact]
    public void FirmwareWamPulseCreatesTheMissingClockA1()
    {
        var encoder = new Acb4000MfmWriteEncoder();
        Assert.Equal(Acb4000MfmConstants.MissingClockA1,
            EncodeByte(encoder, Acb4000FieldConstants.SyncByte, Acb4000AicFirmwareConstants.InitialWriteAddressMark));
    }

    [Fact]
    public void WamSuppressesOnlyTheSelectedFollowingClock()
    {
        for (var ring = 0; ring < 7; ring++)
        {
            var encoder = new Acb4000MfmWriteEncoder();
            var previous = false;
            var normal = Acb4000MfmEncoding.Encode(0, ref previous);
            var expected = (ushort)(normal & ~(1 << (15 - (ring + 1) * 2)));
            Assert.Equal(expected, EncodeByte(encoder, 0, (byte)(1 << ring)));
        }
    }

    [Fact]
    public void ResetClearsPipelineAndPreviousDataAndMark()
    {
        var encoder = new Acb4000MfmWriteEncoder();
        for (var bit = 0; bit < 8; bit++) encoder.ClockBit(true, true);
        encoder.Reset();
        for (var bit = 0; bit < Acb4000MfmWriteConstants.DelayBits; bit++)
            Assert.Equal(0, encoder.ClockBit(false, false));
        Assert.Equal(2, encoder.ClockBit(false, false));
    }

    [Theory]
    [InlineData(Acb4000SequencerConstants.AddressMark)]
    [InlineData(Acb4000SequencerConstants.EnableBitRing)]
    [InlineData(Acb4000SequencerConstants.AddressMark | Acb4000SequencerConstants.EnableBitRing)]
    public void ProgrammableWamIsLimitedToTheAddressMarkState(byte countType)
    {
        var aic = new Acb4000AicRegisters(Acb4000EccRegisters.CreateFirmwareConfiguration(), new Buffer());
        var seq = aic.Sequencer;
        seq.TryWriteProgram(Acb4000SequencerConstants.RamBase, Acb4000SequencerConstants.StopState);
        seq.TryWriteProgram((byte)(Acb4000SequencerConstants.RamBase + Acb4000SequencerConstants.ColumnStride),
            Acb4000SequencerConstants.SetWriteGate);
        seq.TryWriteProgram((byte)(Acb4000SequencerConstants.RamBase + 2 * Acb4000SequencerConstants.ColumnStride),
            countType);
        aic.TryWrite(Acb4000AicRegisterConstants.WriteAddressMark, 1 << 4);
        seq.Start(0);
        for (var bit = 0; bit < 8; bit++)
        {
            seq.ClockBit(false);
            Assert.Equal(bit == 4, aic.WriteAddressMarkOutput);
        }
        seq.ClockBit(false);
        Assert.False(aic.WriteAddressMarkOutput);
        seq.Reset();
        Assert.False(aic.WriteAddressMarkOutput);
    }

    private static ushort EncodeByte(Acb4000MfmWriteEncoder encoder, byte value, byte mark = 0)
    {
        ushort result = 0;
        for (var bit = 0; bit < 8 + Acb4000MfmWriteConstants.DelayBits; bit++)
        {
            var pair = encoder.ClockBit(bit < 8 && (value & (1 << (7 - bit))) != 0,
                bit < 8 && (mark & (1 << bit)) != 0);
            if (bit >= Acb4000MfmWriteConstants.DelayBits) result = (ushort)((result << 2) | pair);
        }
        return result;
    }

    private sealed class Buffer : IAcb4000SequencerBus
    {
        public byte ReadBuffer() => 0;
        public void WriteBuffer(byte value) { }
    }
}

using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.NEC.UPD765.UPD765;

public sealed class ReadChannelTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void BothWindowPolaritiesRecoverAllByteValuesAndOrdinaryMarkBytes(bool mfm, bool inverted)
    {
        var input = new Input(mfm, inverted);
        input.Address(0xfe);
        input.Bytes.Clear();
        for (var value = 0; value <= byte.MaxValue; value++) input.Data((byte)value);
        Assert.Equal(256, input.Bytes.Count);
        Assert.Equal(Enumerable.Range(0, 256).Select(value => (byte)value), input.Bytes.Select(item => item.Data));
        Assert.All(input.Bytes, item =>
        {
            Assert.Equal(Upd765ReadWordKind.Data, item.Kind);
            Assert.True(item.Valid);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryInitialHalfCellOffsetRecoversThePhysicalAddressMark(bool mfm)
    {
        for (var offset = 0; offset < 16; offset++)
        {
            var input = new Input(mfm, inverted: (offset & 1) != 0);
            for (var bit = 0; bit < offset; bit++) input.Cell((bit & 1) == 0);
            input.Address(0xfb);
            input.Data(0x95);
            Assert.Contains(input.Bytes, item => item is (0xfb, Upd765ReadWordKind.AddressMark, true));
            Assert.Equal((0x95, Upd765ReadWordKind.Data, true), input.Bytes[^1]);
        }
    }

    [Theory]
    [InlineData(false, 0xfe)]
    [InlineData(false, 0xfb)]
    [InlineData(false, 0xf8)]
    [InlineData(true, 0xfe)]
    [InlineData(true, 0xfb)]
    [InlineData(true, 0xf8)]
    public void IdNormalAndDeletedMarksRequireTheirRecordedClockPatterns(bool mfm, byte mark)
    {
        var input = new Input(mfm);
        input.Address(mark);
        Assert.Equal((mark, Upd765ReadWordKind.AddressMark, true), input.Bytes[^1]);
        input.Data(mark);
        Assert.Equal((mark, Upd765ReadWordKind.Data, true), input.Bytes[^1]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void IndexMarkUsesD7ClocksInFmAndThreeMissingClockC2WordsInMfm(bool mfm)
    {
        var input = new Input(mfm);
        for (var index = 0; index < 12; index++) input.Data(0);
        if (mfm) for (var index = 0; index < 3; index++) input.Word(0x5224);
        input.Data(0xfc, clocks: mfm ? null : (byte)0xd7);
        Assert.Equal((0xfc, Upd765ReadWordKind.IndexMark, true), input.Bytes[^1]);
        input.Data(0x4e);
        Assert.Equal((0x4e, Upd765ReadWordKind.Data, true), input.Bytes[^1]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void OnlyThreeConsecutiveA1SyncWordsQualifyAnMfmAddressMark(int count)
    {
        var input = new Input(true);
        for (var index = 0; index < 12; index++) input.Data(0);
        for (var index = 0; index < count; index++) input.Word(0x4489);
        input.Data(0xfe);
        Assert.Equal(count == 3 ? Upd765ReadWordKind.AddressMark : Upd765ReadWordKind.Data, input.Bytes[^1].Kind);
        Assert.Equal(0xfe, input.Bytes[^1].Data);
    }

    [Fact]
    public void OrdinaryMfmDataMustNotCreateFalseC2OrA1Synchronization()
    {
        var input = new Input(true);
        input.Address(0xfe);
        input.Bytes.Clear();
        byte[] bytes = [0x01, 0xfe, 0x29, 0xa1, 0xc2, 0xfb, 0xf8, 0xfc];
        foreach (var value in bytes) input.Data(value);
        Assert.Equal(bytes, input.Bytes.Select(item => item.Data));
        Assert.All(input.Bytes, item => Assert.Equal(Upd765ReadWordKind.Data, item.Kind));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BadClockBitsAreReportedWithoutRewritingTheData(bool mfm)
    {
        var input = new Input(mfm);
        input.Address(0xfe);
        var word = input.Encode(0x37);
        input.Word((ushort)(word ^ 0x8000));
        Assert.Equal((0x37, Upd765ReadWordKind.Data, false), input.Bytes[^1]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AReadDataPulseHeldAcrossWindowsProducesOneTransition(bool inverted)
    {
        var input = new Input(false, inverted);
        input.Address(0xfe);
        input.Bytes.Clear();
        // C=10000000 D=00000000 : seul le tout premier créneau contient un front.
        input.Pins.SetInputs(input.Window, true);
        for (var index = 0; index < 16; index++)
        {
            input.Window = !input.Window;
            input.Pins.SetInputs(input.Window, true);
        }
        input.Pins.SetInputs(input.Window, false);
        Assert.Single(input.Bytes);
        Assert.Equal((0, Upd765ReadWordKind.Data, false), input.Bytes[0]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ResetAndDensityChangeDiscardAnIncompleteWord(bool mfm)
    {
        var input = new Input(mfm);
        input.Address(0xfe);
        input.Bytes.Clear();
        for (var index = 0; index < 7; index++) input.Cell(true);
        input.Pins.Reset();
        input.Pins.DoubleDensity = !mfm;
        for (var index = 0; index < 16; index++) input.Cell(false);
        Assert.Empty(input.Bytes);
        input.Pins.DoubleDensity = mfm;
        input.Address(0xf8);
        Assert.Equal((0xf8, Upd765ReadWordKind.AddressMark, true), input.Bytes[^1]);
    }

    private sealed class Input
    {
        internal readonly Upd765ReadChannel Pins;
        internal readonly List<(byte Data, Upd765ReadWordKind Kind, bool Valid)> Bytes = [];
        internal bool Window;
        private readonly bool _mfm;
        private bool _previousData;

        internal Input(bool mfm, bool inverted = false)
        {
            _mfm = mfm;
            Window = inverted;
            Pins = new() { DoubleDensity = mfm };
            Pins.ByteReceived += (data, kind, valid) => Bytes.Add((data, kind, valid));
            Pins.SetInputs(Window, false);
        }

        internal void Address(byte mark)
        {
            for (var index = 0; index < 12; index++) Data(0);
            if (_mfm) for (var index = 0; index < 3; index++) Word(0x4489);
            Data(mark, _mfm ? null : (byte)0xc7);
        }

        internal void Data(byte data, byte? clocks = null) => Word(Encode(data, clocks));

        internal ushort Encode(byte data, byte? clocks = null)
        {
            var word = 0;
            for (var bit = 7; bit >= 0; bit--)
            {
                var value = (data & 1 << bit) != 0;
                var clock = clocks is { } lane ? (lane & 1 << bit) != 0 : !_mfm || !(_previousData || value);
                word = word << 2 | (clock ? 2 : 0) | (value ? 1 : 0);
                _previousData = value;
            }
            return (ushort)word;
        }

        internal void Word(ushort word)
        {
            for (var bit = 15; bit >= 0; bit--) Cell((word & 1 << bit) != 0);
            _previousData = (word & 1) != 0;
        }

        internal void Cell(bool pulse)
        {
            Pins.SetInputs(Window, pulse);
            Pins.SetInputs(Window, pulse);
            Pins.SetInputs(Window, false);
            Window = !Window;
            Pins.SetInputs(Window, false);
        }
    }
}

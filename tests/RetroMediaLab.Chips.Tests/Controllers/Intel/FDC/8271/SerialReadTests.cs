using RetroMediaLab.Chips.Controllers.Intel.FDC.Chip8271.Services;

namespace RetroMediaLab.Chips.Tests.Controllers.Intel.FDC.Chip8271;

public sealed class SerialReadTests
{
    public static TheoryData<int, byte> SectorSizes
    {
        get
        {
            var cases = new TheoryData<int, byte>();
            foreach (var clock in new[] { 2_000_000, 4_000_000 })
                for (byte size = 0; size <= 7; size++) cases.Add(clock, size);
            return cases;
        }
    }

    [Theory]
    [MemberData(nameof(SectorSizes))]
    public void AllRecordedSizeCodesTransferExactlyTheRequestedLengthAcrossIndex(int clock, byte size)
    {
        using var wire = new Wire(clock);
        wire.Command(0x53, 0, 1, (byte)((size << 5) | 1));
        wire.Field(0xfe, [0, 0, 1, size]);
        wire.BeforeRequest = count => { if (count != 0 && count % 200 == 0) wire.Index(); };
        var data = Enumerable.Range(0, 128 << size).Select(index => (byte)(index ^ (index >> 8))).ToArray();
        wire.Field(0xfb, data);
        Assert.Equal(data, wire.Bytes);
        Assert.Equal(data.Length, wire.Chip.ReadBytes(0));
        Assert.Equal(0, wire.Result());
    }
    [Theory]
    [InlineData(2_000_000, false, false)]
    [InlineData(2_000_000, true, false)]
    [InlineData(2_000_000, false, true)]
    [InlineData(2_000_000, true, true)]
    [InlineData(4_000_000, false, false)]
    [InlineData(4_000_000, true, false)]
    [InlineData(4_000_000, false, true)]
    [InlineData(4_000_000, true, true)]
    public void PloSeparatesBothClockPolaritiesInDmaAndNonDma(int clock, bool inverted, bool nonDma)
    {
        using var wire = new Wire(clock, nonDma, phaseLocked: true) { Inverted = inverted };
        wire.Command(0x52, 0, 1);
        wire.StartOnDataHalfCell();
        wire.Field(0xfe, [0, 0, 1, 0]);
        var data = Enumerable.Range(0, 128).Select(value => (byte)(value ^ 0x96)).ToArray();
        wire.Field(0xfb, data);
        Assert.Equal(data, wire.Bytes);
        Assert.Equal(0, wire.Result());
    }

    [Theory]
    [InlineData(2_000_000)]
    [InlineData(4_000_000)]
    public void PloResamplesClockPolarityForTheNextField(int clock)
    {
        using var wire = new Wire(clock, phaseLocked: true);
        wire.Command(0x52, 0, 1);
        wire.Field(0xfe, [0, 0, 1, 0]);
        wire.Inverted = true;
        wire.Delay(3);
        wire.Field(0xfb, new byte[128]);
        Assert.Equal(new byte[128], wire.Bytes);
        Assert.Equal(0, wire.Result());
    }

    [Theory]
    [InlineData(2_000_000, 124, false)]
    [InlineData(4_000_000, 62, false)]
    [InlineData(2_000_000, 124, true)]
    [InlineData(4_000_000, 62, true)]
    public void DmaReadHasAnExactControllerClockDeadline(int clock, int deadline, bool acknowledge)
    {
        using var wire = new Wire(clock) { ServiceRequests = false };
        wire.Command(0x52, 0, 1);
        wire.Field(0xfe, [0, 0, 1, 0]);
        wire.Byte(0, 0xff); wire.Byte(0, 0xff); wire.Byte(0xfb, 0xc7); wire.Byte(0x5a, 0xff);
        Assert.NotNull(wire.FirstRequestTick);
        wire.Delay(deadline - (wire.TickCount - wire.FirstRequestTick.Value) - 1);
        Assert.True(wire.Chip.Drq);
        Assert.False(wire.Chip.Nmi);
        if (acknowledge) Assert.Equal(0x5a, wire.Chip.Read(4));
        wire.Delay(1);
        if (acknowledge) { Assert.True(wire.Chip.Active); Assert.False(wire.Chip.Nmi); }
        else { Assert.False(wire.Chip.Drq); Assert.Equal(0x0a, wire.Result()); }
    }
    [Theory]
    [InlineData(2_000_000, false)]
    [InlineData(2_000_000, true)]
    [InlineData(4_000_000, false)]
    public void ReadsPhysicalFmPulsesWithValidIdDataAndFcs(int clock, bool nonDma)
    {
        var wire = new Wire(clock, nonDma);
        wire.Command(0x52, 0, 1);
        wire.Field(0xfe, [0, 0, 1, 0]);
        var data = Enumerable.Range(0, 128).Select(value => (byte)value).ToArray();
        wire.Field(0xfb, data);
        Assert.Equal(data, wire.Bytes);
        Assert.Equal(0, wire.Result());
        Assert.False(wire.Chip.InSync);
    }

    [Theory]
    [InlineData(2_000_000, false)]
    [InlineData(2_000_000, true)]
    [InlineData(4_000_000, false)]
    [InlineData(4_000_000, true)]
    public void SingleShotAcquiresTheClockWhenTheFirstReceivedPulseWasData(int clock, bool nonDma)
    {
        var wire = new Wire(clock, nonDma);
        wire.Command(0x52, 0, 1);
        wire.StartOnDataHalfCell();
        wire.Field(0xfe, [0, 0, 1, 0]);
        var data = Enumerable.Range(0, 128).Select(value => (byte)(value ^ 0xa5)).ToArray();
        wire.Field(0xfb, data);
        Assert.Equal(data, wire.Bytes);
        Assert.Equal(0, wire.Result());
    }

    [Theory]
    [InlineData(2_000_000, 1)]
    [InlineData(2_000_000, 4)]
    [InlineData(4_000_000, 1)]
    [InlineData(4_000_000, 3)]
    public void SingleShotResamplesTheClockAfterAPhaseChangeBetweenIdAndData(int clock, int delay)
    {
        var wire = new Wire(clock);
        wire.Command(0x52, 0, 1);
        wire.Field(0xfe, [0, 0, 1, 0]);
        wire.Delay(delay);
        var data = Enumerable.Range(0, 128).Select(value => (byte)(value ^ 0x5a)).ToArray();
        wire.Field(0xfb, data);
        Assert.Equal(data, wire.Bytes);
        Assert.Equal(0, wire.Result());
    }

    [Fact]
    public void OneZeroByteCannotAcquireSyncButTwoCan()
    {
        var wire = new Wire();
        wire.Command(0x52, 0, 1);
        wire.Byte(0, 0xff);
        wire.Byte(0xfe, 0xc7);
        Assert.False(wire.Chip.InSync);
        wire.Byte(0xff, 0xff);
        wire.Byte(0, 0xff); wire.Byte(0, 0xff);
        wire.Byte(0xfe, 0xc7);
        Assert.True(wire.Chip.InSync);
    }

    [Fact]
    public void BadIdFcsPreventsAnyDataRequest()
    {
        var wire = new Wire();
        wire.Command(0x52, 0, 1);
        wire.Field(0xfe, [0, 0, 1, 0], corruptCrc: true);
        Assert.Equal(0x0c, wire.Result());
        Assert.Empty(wire.Bytes);
    }

    [Fact]
    public void BadDataFcsReportsErrorAfterPayloadTransfer()
    {
        var wire = new Wire();
        wire.Command(0x52, 0, 1);
        wire.Field(0xfe, [0, 0, 1, 0]);
        wire.Field(0xfb, new byte[128], corruptCrc: true);
        Assert.Equal(128, wire.Bytes.Count);
        Assert.Equal(0x0e, wire.Result());
    }

    [Fact]
    public void MissingClockInAnOrdinaryHeaderByteIsAClockError()
    {
        var wire = new Wire();
        wire.Command(0x52, 0, 1);
        wire.Byte(0, 0xff); wire.Byte(0, 0xff);
        wire.Byte(0xfe, 0xc7);
        wire.Byte(0, 0xdf);
        Assert.Equal(0x08, wire.Result());
    }

    [Fact]
    public void ADataMarkWithNormalClocksIsNotAnAddressMark()
    {
        var wire = new Wire();
        wire.Command(0x52, 0, 1);
        wire.Byte(0, 0xff); wire.Byte(0, 0xff);
        wire.Byte(0xfe, 0xff);
        Assert.False(wire.Chip.InSync);
        Assert.False(wire.Chip.Nmi);
    }

    [Theory]
    [InlineData(0x52, 0)]
    [InlineData(0x56, 128)]
    [InlineData(0x5e, 0)]
    public void DeletedDataMarkControlsTransferAndResult(byte command, int count)
    {
        var wire = new Wire();
        wire.Command(command, 0, 1);
        wire.Field(0xfe, [0, 0, 1, 0]);
        wire.Field(0xf8, new byte[128]);
        Assert.Equal(count, wire.Bytes.Count);
        Assert.Equal(0x20, wire.Result());
    }

    [Fact]
    public void VerifyChecksTheFcsWithoutDmaRequests()
    {
        var wire = new Wire();
        wire.Command(0x5e, 0, 1);
        wire.Field(0xfe, [0, 0, 1, 0]);
        wire.Field(0xfb, new byte[128], corruptCrc: true);
        Assert.Empty(wire.Bytes);
        Assert.Equal(0x0e, wire.Result());
    }

    [Fact]
    public void ReadIdWaitsForIndexThenReturnsTheRecordedFields()
    {
        var wire = new Wire();
        wire.Command(0x5b, 0, 0, 1);
        wire.Field(0xfe, [0, 1, 9, 3]);
        Assert.Empty(wire.Bytes);
        wire.Index();
        wire.Field(0xfe, [0, 1, 9, 3]);
        Assert.Equal(new byte[] { 0, 1, 9, 3 }, wire.Bytes);
        Assert.Equal(0, wire.Result());
    }

    [Fact]
    public void HeaderForAnotherSectorCannotLendItsDataToTheMatchedSector()
    {
        var wire = new Wire();
        wire.Command(0x52, 0, 1);
        wire.Field(0xfe, [0, 0, 1, 0]);
        wire.Field(0xfe, [0, 0, 2, 0]);
        wire.Field(0xfb, new byte[128]);
        Assert.Empty(wire.Bytes);
        wire.Index(); wire.Index();
        Assert.Equal(0x18, wire.Result());
    }

    [Fact]
    public void MultipleSectorsUseConsecutiveIdsAndTransferInWireOrder()
    {
        var wire = new Wire();
        wire.Command(0x53, 0, 1, 2);
        wire.Field(0xfe, [0, 0, 1, 0]);
        wire.Field(0xfb, Enumerable.Repeat((byte)0xaa, 128).ToArray());
        wire.Field(0xfe, [0, 0, 2, 0]);
        wire.Field(0xfb, Enumerable.Repeat((byte)0x55, 128).ToArray());
        Assert.Equal(Enumerable.Repeat((byte)0xaa, 128).Concat(Enumerable.Repeat((byte)0x55, 128)), wire.Bytes);
        Assert.Equal(0, wire.Result());
    }

    [Fact]
    public void UnservicedDataRequestCausesLateDmaOnThePhysicalByteDeadline()
    {
        var wire = new Wire { ServiceRequests = false };
        wire.Command(0x52, 0, 1);
        wire.Field(0xfe, [0, 0, 1, 0]);
        wire.Field(0xfb, new byte[128]);
        Assert.Equal(0x0a, wire.Result());
    }

    [Fact]
    public void ResetAbortsPhysicalSearchAndDropsSynchronization()
    {
        var wire = new Wire();
        wire.Command(0x52, 0, 1);
        wire.Byte(0, 0xff); wire.Byte(0, 0xff); wire.Byte(0xfe, 0xc7);
        Assert.True(wire.Chip.InSync);
        wire.Chip.SetReset(true);
        wire.Byte(0, 0xff);
        Assert.False(wire.Chip.InSync);
        Assert.False(wire.Chip.Nmi);
        Assert.False(wire.Chip.Drq);
        Assert.Equal(0, wire.Chip.Read(0));
    }

    [Theory]
    [InlineData(false, false, 2)]
    [InlineData(true, false, 2)]
    [InlineData(false, true, 0x0e)]
    public void ScanConsumesCpuKeysOnTheWireAndStillChecksTheTrailingCrc(bool nonDma, bool corruptCrc, byte result)
    {
        var wire = new Wire(nonDma: nonDma) { Key = index => index % 2 == 0 ? (byte)0xaa : (byte)0x55 };
        wire.Command(0x40, 0, 1, 1, 1, 2);
        wire.Field(0xfe, [0, 0, 1, 0]);
        var data = new byte[128];
        data[0] = 0xaa;
        data[1] = 0x55;
        wire.Field(0xfb, data, corruptCrc);
        Assert.Equal(2, wire.KeysWritten);
        Assert.Empty(wire.Bytes);
        Assert.Equal(result, wire.Result());
    }

    [Fact]
    public void IndexDuringLongDataFieldDoesNotAbortTheTransfer()
    {
        var wire = new Wire();
        wire.Command(0x53, 0, 1, 0x41);
        wire.Field(0xfe, [0, 0, 1, 2]);
        wire.BeforeRequest = index => { if (index == 200) { wire.Index(); wire.Index(); } };
        var data = Enumerable.Range(0, 512).Select(index => (byte)index).ToArray();
        wire.Field(0xfb, data);
        Assert.Equal(data, wire.Bytes);
        Assert.Equal(0, wire.Result());
    }

    private sealed class Wire : IDisposable
    {
        internal readonly Intel8271 Chip;
        internal readonly List<byte> Bytes = [];
        internal bool ServiceRequests = true;
        internal Func<int, byte>? Key;
        internal Action<int>? BeforeRequest;
        internal int KeysWritten;
        private readonly int _halfBitTicks;
        private int _tick;
        private int? _lastPulse;
        private readonly bool _phaseLocked;
        private bool _clockHalf;
        internal bool Inverted;
        internal int TickCount => _tick;
        internal int? FirstRequestTick;

        internal Wire(int clock = 2_000_000, bool nonDma = false, bool phaseLocked = false)
        {
            Chip = new Intel8271(chipClock: clock);
            _phaseLocked = phaseLocked;
            Chip.SetPhaseLockedSeparator(phaseLocked);
            Chip.SetReady(0, true);
            Chip.SetTrackZero(0, true);
            Chip.SetIndex(0, false);
            _halfBitTicks = clock == 2_000_000 ? 8 : 4;
            Command(0x35, 0x0d, 1, 0, 0x10);
            Command(0x3a, 0x17, nonDma ? (byte)1 : (byte)0);
        }

        internal void Command(byte command, params byte[] parameters)
        {
            Chip.Write(0, command);
            foreach (var value in parameters) Chip.Write(1, value);
            Tick(); // délai de chargement programmé nul, entrée dans le séquenceur de lecture.
        }

        internal void Index() { Chip.SetIndex(0, true); Chip.SetIndex(0, false); }
        internal byte Result()
        {
            Assert.Equal(0x10, Chip.Read(0) & 0x90);
            Assert.True(Chip.Nmi);
            return Chip.Read(1);
        }

        internal void Byte(byte data, byte clocks)
        {
            for (var bit = 7; bit >= 0; bit--)
            {
                _clockHalf = true;
                if ((clocks & (1 << bit)) != 0) Pulse();
                for (var t = 0; t < _halfBitTicks; t++) Tick();
                _clockHalf = false;
                if ((data & (1 << bit)) != 0) Pulse();
                for (var t = 0; t < _halfBitTicks; t++) Tick();
            }
        }

        internal void Delay(int ticks)
        {
            for (var tick = 0; tick < ticks; tick++) Tick();
        }

        internal void StartOnDataHalfCell()
        {
            _clockHalf = false;
            Pulse();
            for (var tick = 0; tick < _halfBitTicks; tick++) Tick();
        }

        private void Pulse()
        {
            Chip.SetDataWindow(_phaseLocked ? _clockHalf != Inverted
                : _lastPulse is null || _tick - _lastPulse >= 2 * _halfBitTicks);
            _lastPulse = _tick;
            Chip.SetUnseparatedData(true);
            Chip.SetUnseparatedData(false);
        }
        private void Tick()
        {
            _tick++;
            Chip.Tick();
            if (FirstRequestTick is null && (Chip.Drq || (Chip.Read(0) & 4) != 0)) FirstRequestTick = _tick;
            if (!ServiceRequests || !Chip.Drq && (Chip.Read(0) & 4) == 0) return;
            BeforeRequest?.Invoke(Bytes.Count);
            if (Key is null) Bytes.Add(Chip.Read(4));
            else Chip.Write(4, Key(KeysWritten++));
        }

        public void Dispose() => Chip.SetReset(true);

        internal void Field(byte mark, byte[] data, bool corruptCrc = false)
        {
            Byte(0xff, 0xff);
            Byte(0, 0xff); Byte(0, 0xff);
            Byte(mark, 0xc7);
            ushort crc = 0xffff;
            foreach (var value in new[] { mark }.Concat(data))
            {
                crc ^= (ushort)(value << 8);
                for (var bit = 0; bit < 8; bit++) crc = (ushort)((crc & 0x8000) != 0 ? (crc << 1) ^ 0x1021 : crc << 1);
            }
            foreach (var value in data) Byte(value, 0xff);
            Byte((byte)(crc >> 8), 0xff);
            Byte((byte)(crc ^ (corruptCrc ? 1 : 0)), 0xff);
        }
    }
}

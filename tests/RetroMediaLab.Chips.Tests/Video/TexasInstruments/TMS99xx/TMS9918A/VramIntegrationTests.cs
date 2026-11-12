using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Models;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.Video.TexasInstruments.TMS99xx.TMS9918A;

public sealed class VramIntegrationTests
{
    [Fact]
    public void ExternalSyncFinishesPartialReadOrWriteWithinOnePixelAndResumesOneCpuRequest()
    {
        // NoteTI Macourek p.3 : fin du cycle partiel dans un pixel,
        // sans reset des registres ni arrêt du maître pendant l'impulsion.
        foreach (bool read in new[] { false, true })
        foreach (int offset in new[] { 0, 160, 340 })
        foreach (int width in new[] { 4700, 8000 })
        {
            using var circuit = new Circuit(new(0, 0xc0, 6, 0x80, 0, 0x7e, 7, 3));
            circuit.Memory[0x3f00] = 0xd0;
            circuit.Memory[0x13] = 0xa5;
            circuit.Write(0x13, true); circuit.Write((byte)(read ? 0 : 0x40), true);
            if (!read) circuit.Write(0x5a, false);
            long limit = circuit.Time + 10_000;
            while (!circuit.Chip.CaptureState().VramSequencer.CpuInService && circuit.Time < limit)
                circuit.RunTo(circuit.Time + 50);
            Assert.True(circuit.Chip.CaptureState().VramSequencer.CpuInService);
            circuit.RunTo(circuit.Time + offset);
            Assert.True(circuit.Chip.HasCpuVramRequest);
            var registers = circuit.Chip.RegisterState;
            long start = circuit.Time;
            long halfEdges = circuit.Chip.ClockState.HalfEdges;
            long? released = null;
            int cpuEdges = 0, gromEdges = 0;
            var receive = circuit.Chip.VramPinsChanged;
            try
            {
                circuit.Chip.CpuClockChanged = _ => cpuEdges++;
                circuit.Chip.GromClockChanged = _ => gromEdges++;
                circuit.Chip.VramPinsChanged = signal =>
                {
                    receive!(signal);
                    if (signal.Pins.RasHigh && signal.Pins.CasHigh && signal.Pins.ReadWriteHigh)
                        released ??= signal.TimeNanoseconds;
                };
                circuit.Chip.SetResetSyncVoltage(start, 12, true);
                circuit.RunTo(start + width);
                Assert.NotNull(released);
                Assert.InRange(released!.Value - start, 0, 187); // dot = 186,243...ns
                Assert.Equal(width > 7200, circuit.Chip.CaptureState().ExternalVideo.VerticalSyncRecognized);
                if (width > 7200) Assert.Equal(219, circuit.Chip.RasterState.Line);
                circuit.Chip.SetResetSyncVoltage(circuit.Time, 5, true);
                circuit.RunTo(circuit.Time + 10_000);
                Assert.False(circuit.Chip.HasCpuVramRequest);
                Assert.Equal((ushort?)0x14, circuit.Chip.CpuMemoryState.Address);
                Assert.True(circuit.Chip.CpuMemoryState.DataKnown);
                Assert.Equal(read ? 0xa5 : 0x5a, circuit.Chip.CpuMemoryState.Data);
                Assert.Equal(read ? 0xa5 : 0x5a, circuit.Memory[0x13]);
                Assert.Equal(registers, circuit.Chip.RegisterState);
                Assert.True(circuit.Chip.ClockState.HalfEdges > halfEdges);
                Assert.True(cpuEdges > 0); Assert.True(gromEdges > 0);
            }
            finally
            {
                circuit.Chip.VramPinsChanged = receive;
                circuit.Chip.CpuClockChanged = null; circuit.Chip.GromClockChanged = null;
            }
        }
    }

    [Fact]
    public void OverrunBurstProducesOneUnqualifiedTransferInsteadOfQueuedWrites()
    {
        using var circuit = new Circuit(new(0, 0xc0, 6, 0x80, 0, 0x7e, 7, 3));
        circuit.Memory[0x3f00] = 0xd0;
        circuit.Write(0x13, true); circuit.Write(0x40, true);
        int writes = 0;
        bool writeKnown = true;
        var receive = circuit.Chip.VramPinsChanged;
        var previous = circuit.Chip.VramPins;
        try
        {
            circuit.Chip.VramPinsChanged = signal =>
            {
                receive!(signal);
                if (previous.ReadWriteHigh && !signal.Pins.ReadWriteHigh)
                { writes++; writeKnown &= signal.Pins.AddressDataKnown; }
                previous = signal.Pins;
            };
            foreach (byte value in new byte[] { 0x11, 0x22, 0x33 })
                circuit.Write(value, false, gapNanoseconds: 40);
            Assert.Equal(0x33, circuit.Chip.CpuMemoryState.Data);
            Assert.False(circuit.Chip.CpuMemoryState.RequestKnown);
            Assert.False(circuit.Chip.CaptureState().CpuWriteBus.QualificationLost);
            circuit.RunTo(circuit.Time + 10_000);
            Assert.Equal(1, writes);
            Assert.False(writeKnown);
            Assert.False(circuit.Chip.HasCpuVramRequest);
            Assert.Null(circuit.Chip.CpuMemoryState.Address);
            Assert.False(circuit.Chip.CpuMemoryState.DataKnown);
        }
        finally { circuit.Chip.VramPinsChanged = receive; }
    }

    [Fact]
    public void CrossingReadAndWriteRequestsDuringPhysicalServiceCompletesTheStartedCycle()
    {
        foreach (bool firstRead in new[] { false, true })
        {
            using var circuit = new Circuit(new(0, 0xc0, 6, 0x80, 0, 0x7e, 7, 3));
            circuit.Memory[0x3f00] = 0xd0;
            circuit.Write(0x13, true); circuit.Write((byte)(firstRead ? 0 : 0x40), true);
            if (!firstRead) circuit.Write(0x11, false);
            long limit = circuit.Time + 10_000;
            while (!circuit.Chip.CaptureState().VramSequencer.CpuInService && circuit.Time < limit)
                circuit.RunTo(circuit.Time + 50);
            Assert.True(circuit.Chip.CaptureState().VramSequencer.CpuInService);
            long start = circuit.Time;
            var pins = circuit.Chip.CaptureState().CpuInterface.Pins with { Mode = false, Data = 0x77 };
            circuit.Chip.ReceiveCpuPins(start, pins);
            if (firstRead)
            {
                circuit.RunTo(start + 40);
                circuit.Chip.ReceiveCpuPins(circuit.Time, pins with { CswHigh = false });
                circuit.RunTo(start + 300);
                circuit.Chip.ReceiveCpuPins(circuit.Time, pins);
                Assert.Equal(0x77, circuit.Chip.CpuMemoryState.Data);
            }
            else
            {
                circuit.Chip.ReceiveCpuPins(start, pins with { CsrHigh = false });
                circuit.RunTo(start + 200);
                circuit.Chip.ReceiveCpuPins(circuit.Time, pins);
            }
            Assert.False(circuit.Chip.CpuMemoryState.RequestKnown);
            circuit.RunTo(circuit.Time + 2000);
            Assert.False(circuit.Chip.HasCpuVramRequest);
            Assert.False(circuit.Chip.CaptureState().VramSequencer.CpuInService);
            Assert.False(circuit.Chip.CpuMemoryState.DataKnown);
            Assert.Null(circuit.Chip.CpuMemoryState.Address);
        }
    }

    [Fact]
    public void CombinedModesUseTheirTablesOrFixedPatternAcrossAllThreeThirds()
    {
        foreach (int mode in Enumerable.Range(0, 4))
        {
            bool fixedPattern = mode >= 2, text = mode != 1;
            var registers = new Tms9918RegisterState((byte)(mode == 2 ? 0 : 2),
                (byte)(fixedPattern ? 0xd8 : text ? 0xd0 : 0xc8), 6, 0, 3, 0x7e, 7, 0xe3);
            using var circuit = new Circuit(registers);
            Array.Fill(circuit.Memory, (byte)0x22, 0x1800, text ? 960 : 768);
            byte[] patterns = text ? [0xa5, 0x5a, 0xf0] : [0x25, 0x47, 0x69];
            for (int bank = 0; bank < 3; bank++)
                Array.Fill(circuit.Memory, patterns[bank], bank * 0x800 + 0x110, 8);
            circuit.Memory[0x3f00] = 0xd0;
            if (fixedPattern) circuit.UnknownAddresses.UnionWith(Enumerable.Range(0, 16384));
            var pixels = new List<Tms9918VideoSignal>();
            try
            {
                circuit.Chip.VideoSignalProduced = signal =>
                {
                    if (signal.Line is 3 or 67 or 131 && signal.Active) pixels.Add(signal);
                };
                circuit.RunTo(8_500_000);
                Assert.Equal((text ? 240 : 256) * 3, pixels.Count);
                Assert.All(pixels, pixel => Assert.True(pixel.ColourKnown));
                for (int bank = 0; bank < 3; bank++)
                {
                    byte[] expected = fixedPattern ? [14, 14, 14, 14, 3, 3]
                        : text ? bank switch
                        {
                            0 => [14, 3, 14, 3, 3, 14],
                            1 => [3, 14, 3, 14, 14, 3],
                            _ => [14, 14, 14, 14, 3, 3],
                        }
                        : Enumerable.Repeat((byte)(2 + bank * 2), 4)
                            .Concat(Enumerable.Repeat((byte)(5 + bank * 2), 4)).ToArray();
                    var row = pixels.Where(pixel => pixel.Line == 3 + bank * 64).ToArray();
                    Assert.Equal(text ? 6 : 0, row[0].Dot);
                    Assert.Equal(expected, row.Take(expected.Length).Select(pixel => pixel.Colour));
                }
                if (text) Assert.Equal(0, circuit.Read(true) & 0x60); // pas de flags sprites
            }
            finally { circuit.Chip.VideoSignalProduced = null; }
        }
    }

    [Fact]
    public void Ti1984ReducedGraphicsIITablesFeedAllThreeThirdsThroughTheExternalBus()
    {
        // TI Video Display Processors Programmer's Guide 1984, table 8-2.
        using var circuit = new Circuit(new(2, 0xc2, 14, 0x9f, 0, 0x76, 3, 15));
        Array.Fill(circuit.Memory, (byte)0x22, 0x3800, 768);
        for (int row = 0; row < 8; row++)
        {
            circuit.Memory[0x110 + row] = 0xa5;
            circuit.Memory[0x2110 + row] = 0x23;
            // Les anciennes adresses non masquées produiraient d'autres pixels.
            circuit.Memory[0x910 + row] = circuit.Memory[0x1110 + row] = 0;
            circuit.Memory[0x2910 + row] = circuit.Memory[0x3110 + row] = 0xf1;
        }
        circuit.Memory[0x3b00] = 0xd0;
        var pixels = new List<Tms9918VideoSignal>();
        try
        {
            circuit.Chip.VideoSignalProduced = signal =>
            {
                if (signal.Line is 3 or 67 or 131 && signal.Active) pixels.Add(signal);
            };
            circuit.RunTo(8_500_000);
            Assert.Equal(768, pixels.Count);
            Assert.All(pixels, pixel => Assert.True(pixel.ColourKnown));
            foreach (int line in new[] { 3, 67, 131 })
                Assert.Equal(new byte[] { 2, 3, 2, 3, 3, 2, 3, 2 },
                    pixels.Where(pixel => pixel.Line == line).Take(8).Select(pixel => pixel.Colour));
        }
        finally { circuit.Chip.VideoSignalProduced = null; }
    }

    [Fact]
    public void CpuAccessesUseExternalMultiplexedMemoryAndAdvanceAddressOnlyAfterService()
    {
        using var circuit = new Circuit(new(0, 0x80, 6, 0x80, 0, 0x7e, 7, 0xe3));
        circuit.Write(0xfe, true); circuit.Write(0x7f, true);
        circuit.Write(0x9a, false);
        Assert.True(circuit.Chip.HasCpuVramRequest);
        Assert.Equal((ushort?)0x3ffe, circuit.Chip.CpuMemoryState.Address);
        circuit.RunTo(circuit.Time + 8000);
        Assert.False(circuit.Chip.HasCpuVramRequest);
        Assert.Equal(0x9a, circuit.Memory[0x3ffe]);
        Assert.Equal((ushort?)0x3fff, circuit.Chip.CpuMemoryState.Address);
        circuit.Write(0xbc, false); circuit.RunTo(circuit.Time + 8000);
        Assert.Equal(0xbc, circuit.Memory[0x3fff]);
        Assert.Equal((ushort?)0, circuit.Chip.CpuMemoryState.Address);
        circuit.Write(0xfe, true); circuit.Write(0x3f, true);
        Assert.True(circuit.Chip.HasCpuVramRequest);
        circuit.RunTo(circuit.Time + 8000);
        Assert.Equal(0x9a, circuit.Read());
        circuit.RunTo(circuit.Time + 8000);
        Assert.Equal(0xbc, circuit.Read());
        circuit.RunTo(circuit.Time + 8000);
        Assert.Equal((ushort?)1, circuit.Chip.CpuMemoryState.Address);
        Assert.True(circuit.Chip.CpuMemoryState.DataKnown);
        Assert.True(circuit.Chip.IsCpuInterfaceQualified);
        Assert.True(circuit.Refreshes > 0);
        circuit.Chip.SetReset(circuit.Time, true);
        int refreshes = circuit.Refreshes;
        circuit.RunTo(circuit.Time + 4000);
        Assert.Equal(refreshes, circuit.Refreshes);
        Assert.True(circuit.Chip.VramPins.RasHigh);
        Assert.True(circuit.Chip.VramPins.CasHigh);
        circuit.Chip.SetReset(circuit.Time, false);
        circuit.RunTo(circuit.Time + 4000);
        Assert.True(circuit.Refreshes > refreshes);
    }

    [Fact]
    public void FourKilobyteWiringAliasesUpperAddressBitsWithoutChangingCpuCounter()
    {
        using var circuit = new Circuit(new(0, 0, 0, 0, 0, 0, 0, 3));
        circuit.Write(0x34, true); circuit.Write(0x52, true); // écriture à 1234
        circuit.Write(0xd7, false); circuit.RunTo(circuit.Time + 8000);
        Assert.Equal(0xd7, circuit.Memory[0x234]);
        Assert.Equal(0, circuit.Memory[0x1234]);
        Assert.Equal((ushort?)0x1235, circuit.Chip.CpuMemoryState.Address);
        circuit.Write(0x34, true); circuit.Write(0x12, true);
        circuit.RunTo(circuit.Time + 8000);
        Assert.Equal(0xd7, circuit.Read());
    }

    [Fact]
    public void AllFourModesProducePixelsFromBytesDeliveredByExternalMemory()
    {
        foreach (int mode in Enumerable.Range(0, 4))
        {
            var registers = new Tms9918RegisterState((byte)(mode == 1 ? 2 : 0),
                (byte)(mode == 2 ? 0xc8 : mode == 3 ? 0xd0 : 0xc0), 6,
                (byte)(mode == 1 ? 0xff : 0x80), (byte)(mode == 1 ? 3 : 0), 0x7e, 7, 0xe3);
            using var circuit = new Circuit(registers);
            Array.Fill(circuit.Memory, (byte)0x22, 0x1800, mode == 3 ? 960 : 768);
            for (int row = 0; row < 8; row++)
            {
                for (int bank = 0; bank < (mode == 1 ? 3 : 1); bank++)
                {
                    circuit.Memory[bank * 2048 + 0x22 * 8 + row] = 0xa5;
                    circuit.Memory[0x2000 + bank * 2048 + 0x22 * 8 + row] = (byte)(0x23 + bank * 0x22);
                }
            }
            circuit.Memory[0x2004] = 0x23;
            circuit.Memory[0x3f00] = 0xd0;
            var pixels = new List<Tms9918VideoSignal>();
            try
            {
                circuit.Chip.VideoSignalProduced = signal =>
                {
                    if (signal.Line is 3 or 67 or 131 && signal.Active) pixels.Add(signal);
                };
                circuit.RunTo(8_500_000);
                Assert.Equal((mode == 3 ? 240 : 256) * 3, pixels.Count);
                Assert.All(pixels, pixel => Assert.True(pixel.ColourKnown));
                byte[] expected = mode switch
                {
                    2 => [10, 10, 10, 10, 5, 5, 5, 5],
                    3 => [14, 3, 14, 3, 3, 14],
                    _ => [2, 3, 2, 3, 3, 2, 3, 2],
                };
                Assert.Equal(expected, pixels.Take(expected.Length).Select(x => x.Colour));
                for (int bank = 1; bank < 3; bank++)
                {
                    int line = 3 + bank * 64;
                    var colours = pixels.Where(x => x.Line == line).Take(expected.Length).Select(x => x.Colour);
                    Assert.Equal(mode == 1 ? expected.Select(x => (byte)(x + bank * 2)) : expected, colours);
                }
                Assert.True(circuit.Reads > 0);
            }
            finally { circuit.Chip.VideoSignalProduced = null; }
        }
    }

    [Fact]
    public void UnconnectedRamRemainsUnknownAndStableRdKeepsScalarAndGroupedBusTracesIdentical()
    {
        var initial = new Tms9918InitialState(new(0, 0xc0, 6, 0x80, 0, 0x7e, 7, 3), 0,
            new(0, false), new(0, 0, Tms9918CpuVramOperation.None), new(0, true, true, true, true));
        var scalar = new Tms9918A(initial, Tms9918CpuTiming.SpecifiedMaximum);
        var grouped = new Tms9918A(initial, Tms9918CpuTiming.SpecifiedMaximum);
        var absent = new Tms9918A(initial with { Memory = new(0, 0, Tms9918CpuVramOperation.Read) }, Tms9918CpuTiming.SpecifiedMaximum);
        var expected = new List<Tms9918VramSignal>();
        var actual = new List<Tms9918VramSignal>();
        var expectedVideo = new List<Tms9918VideoSignal>();
        var actualVideo = new List<Tms9918VideoSignal>();
        var absentVideo = new List<Tms9918VideoSignal>();
        try
        {
            scalar.SetVramReadData(0, 0xa5, true); grouped.SetVramReadData(0, 0xa5, true);
            scalar.VramPinsChanged = expected.Add; grouped.VramPinsChanged = actual.Add;
            scalar.VideoSignalProduced = expectedVideo.Add; grouped.VideoSignalProduced = actualVideo.Add;
            absent.VideoSignalProduced = absentVideo.Add;
            bool high = false;
            const int count = 342 * 4 * 3;
            for (int i = 0; i < count; i++) scalar.ReceiveMasterClock(Date(i), high = !high);
            grouped.ReceiveMasterClockTrain(new(0, 1_000_000_000, 21_477_270, 0, count));
            absent.ReceiveMasterClockTrain(new(0, 1_000_000_000, 21_477_270, 0, count));
            Assert.Equal(expected, actual);
            Assert.Equal(expectedVideo, actualVideo);
            Assert.All(absentVideo.Where(x => x.Active), x => Assert.False(x.ColourKnown));
            Assert.Contains(actualVideo, x => x.Line == 2 && x.Active && x.ColourKnown);
            Assert.False(absent.CpuMemoryState.DataKnown);
            absent.SetReset(absent.TimeNanoseconds, true);
            Assert.False(absent.CpuMemoryState.DataKnown);
        }
        finally
        {
            scalar.VramPinsChanged = null; grouped.VramPinsChanged = null;
            scalar.VideoSignalProduced = null; grouped.VideoSignalProduced = null;
            absent.VideoSignalProduced = null;
        }
    }

    private static long Date(long index) => (long)((Int128)index * 1_000_000_000 / 21_477_270);

    /// <summary>Mémoire synthétique externe, oracle des broches. Le banc livre
    /// ses changements RD à ses échéances ; ce n'est pas un modèle de MB8818.</summary>
    internal sealed class Circuit : IDisposable
    {
        internal readonly Tms9918A Chip;
        internal readonly byte[] Memory = new byte[16 * 1024];
        internal readonly HashSet<int> UnknownAddresses = [];
        internal long Time => Chip.TimeNanoseconds;
        internal int Reads, Refreshes;
        private Tms9918VramPins _pins = new(0, false, true, true, true);
        private Tms9918CpuPins _cpu = new(0, true, true, true, true);
        private int _row, _column;
        private bool _rowKnown, _columnKnown, _clockHigh;
        private long _nextEdge;
        private long? _captureColumn, _readReady;
        private readonly bool _sixteenKilobytes;
        private int Address => _sixteenKilobytes ? (_column << 7) | _row : ((_column & 63) << 6) | (_row & 63);

        internal Circuit(Tms9918RegisterState registers)
        {
            _sixteenKilobytes = (registers.R1 & 0x80) != 0;
            Chip = new(new(registers, 0, new(0, false), new(0, 0, Tms9918CpuVramOperation.None), _cpu),
                Tms9918CpuTiming.SpecifiedMaximum);
            Chip.VramPinsChanged = ReceivePins;
        }

        private void ReceivePins(Tms9918VramSignal signal)
        {
            var previous = _pins;
            _pins = signal.Pins;
            if (previous.RasHigh && !_pins.RasHigh)
            { _row = _pins.AddressData & 127; _rowKnown = _pins.AddressDataKnown; Refreshes++; }
            if (previous.CasHigh && !_pins.CasHigh)
            { _captureColumn = signal.TimeNanoseconds + 2; _readReady = signal.TimeNanoseconds + 100; }
            if (previous.ReadWriteHigh && !_pins.ReadWriteHigh && !_pins.CasHigh)
            {
                if (_rowKnown && _columnKnown && _pins.AddressDataKnown)
                    Memory[Address] = _pins.AddressData;
            }
            if (!previous.CasHigh && _pins.CasHigh)
            {
                _captureColumn = _readReady = null;
                Chip.SetVramReadData(signal.TimeNanoseconds, 0, false);
            }
        }

        internal void RunTo(long time)
        {
            while (Date(_nextEdge) <= time)
            {
                long date = Date(_nextEdge++);
                Pump(date);
                Chip.ReceiveMasterClock(date, _clockHigh = !_clockHigh);
            }
            Pump(time);
        }

        private void Pump(long time)
        {
            while (true)
            {
                long due = Math.Min(Chip.NextEventNanoseconds ?? long.MaxValue,
                    Math.Min(_captureColumn ?? long.MaxValue, _readReady ?? long.MaxValue));
                if (due > time) break;
                Chip.AdvanceTo(due);
                if (_captureColumn == due)
                { _captureColumn = null; _column = _pins.AddressData & 127; _columnKnown = _pins.AddressDataKnown; }
                if (_readReady == due)
                {
                    _readReady = null; Reads++;
                    Chip.SetVramReadData(due, Memory[Address], _rowKnown && _columnKnown && !UnknownAddresses.Contains(Address));
                }
            }
            Chip.AdvanceTo(time);
        }

        internal void Write(byte value, bool mode, int gapNanoseconds = 2000)
        {
            long start = Time + gapNanoseconds;
            RunTo(start);
            _cpu = _cpu with { Mode = mode, Data = value };
            Chip.ReceiveCpuPins(start, _cpu);
            RunTo(start + 40); Chip.ReceiveCpuPins(Time, _cpu with { CswHigh = false });
            RunTo(start + 300); Chip.ReceiveCpuPins(Time, _cpu);
        }

        internal byte Read(bool status = false)
        {
            long start = Time + 2000;
            RunTo(start);
            _cpu = _cpu with { Mode = status, CsrHigh = false };
            Chip.ReceiveCpuPins(start, _cpu);
            RunTo(start + 200);
            Assert.True(Chip.CpuOutputs.DataKnown);
            byte result = Chip.CpuOutputs.Data;
            _cpu = _cpu with { CsrHigh = true };
            Chip.ReceiveCpuPins(Time, _cpu);
            RunTo(start + 400);
            return result;
        }

        public void Dispose()
        {
            Chip.VramPinsChanged = null; Chip.VideoSignalProduced = null;
            Chip.CpuOutputsChanged = null; Chip.CpuClockChanged = null;
            Chip.NextEventNanosecondsChanged = null; Chip.FrameStarted = null;
        }
    }
}

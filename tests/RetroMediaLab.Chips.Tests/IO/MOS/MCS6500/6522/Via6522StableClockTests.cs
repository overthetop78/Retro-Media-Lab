using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Enums;
using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;
using C = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Constants.Via6522Constants;

namespace RetroMediaLab.Chips.Tests.IO.MOS.MCS6500.Chip6522;

public sealed class Via6522StableClockTests
{
    [Fact]
    public void RefusalDistinguishesTheNextBoundaryFromAnUnprofitableRetryLoop()
    {
        var via = new Via6522();
        via.Write(C.Timer1Low, 100); via.Write(C.Timer1High, 0);
        Assert.Equal(0, via.TryAdvanceStableClocks(100, out var retry));
        Assert.True(retry); // Chargement à terminer par le front normal.
        via.Tick();
        Assert.Equal(10, via.TryAdvanceStableClocks(10, out retry));
        Assert.True(retry);
        Assert.Equal(90, via.Read(C.Timer1Low));
        via.Write(C.Timer1Low, 1); via.Write(C.Timer1High, 0);
        Assert.Equal(0, via.TryAdvanceStableClocks(100, out retry));
        Assert.False(retry);
        Assert.Equal(1, via.Read(C.Timer1Low));
        via.Write(C.Timer1Low, 100); via.Write(C.Timer1High, 0);
        via.Write(C.Auxiliary, 0x18); via.Write(C.Shift, 0xa6);
        Assert.Equal(0, via.TryAdvanceStableClocks(100, out retry));
        Assert.False(retry);
        via.Write(C.Auxiliary, 0);
        via.Tick();
        Assert.Equal(10, via.TryAdvanceStableClocks(10, out retry));
        Assert.True(retry);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    public void StablePrefixesKeepTheUnitEdgesAcrossModesBoundariesAndNewInputs(int shiftMode)
    {
        foreach (var pulse in new[] { false, true })
        foreach (var continuous in new[] { false, true })
        foreach (var latch in new[] { 0, 1, 255, 256, 65535 })
        {
            using var pair = new Pair();
            pair.Both(via =>
            {
                via.SetPb6(true);
                via.Write(C.Auxiliary, (byte)(shiftMode << 2 | (pulse ? 0x20 : 0) | (continuous ? 0xc0 : 0)));
                via.Write(C.Enable, 0xff);
                via.Write(C.Timer1Low, (byte)latch); via.Write(C.Timer1High, (byte)(latch >> 8));
                via.Write(C.Timer2Low, (byte)latch); via.Write(C.Timer2High, (byte)(latch >> 8));
                via.Write(C.Shift, 0xa6);
            });
            var index = 0;
            foreach (var clocks in new[] { 1, 2, 7, 31, 256, 4096, 65537 })
            {
                pair.Both(via => { via.SetPb6((index & 1) == 0); via.SetCb2((index & 2) == 0); via.SetCb1((index & 1) == 0); });
                pair.Advance(clocks);
                pair.Check();
                index++;
            }
            pair.Both(via => via.Write(C.Auxiliary, 0));
            pair.Advance(1234); pair.Check();
            Assert.Equal(pair.Unit.Read(C.Shift), pair.Grouped.Read(C.Shift));
        }
    }

    [Fact]
    public void CounterPrefixStopsBeforeUnderflowAndKeepsItsLeadingEdgeInterrupt()
    {
        var via = new Via6522();
        via.Write(C.Enable, 0xc0);
        via.Write(C.Timer1Low, 0x88); via.Write(C.Timer1High, 0x13); // 5000.
        Assert.Equal(0, via.TryAdvanceStableClocks(1000)); // Cycle de chargement réel.
        via.Tick();
        Assert.Equal(1000, via.TryAdvanceStableClocks(1000));
        Assert.Equal(4000, via.Read(C.Timer1Low) | via.Read(C.Timer1High) << 8);
        Assert.Equal(4000, via.TryAdvanceStableClocks(10000));
        Assert.Equal(0, via.TryAdvanceStableClocks(1));
        Assert.False(via.Irq);
        via.Phi2TrailingEdge();
        Assert.False(via.Irq);
        Assert.Equal(0, via.TryAdvanceStableClocks(10000)); // Demi-période non achevée.
        via.Phi2LeadingEdge();
        Assert.True(via.Irq);
        Assert.Equal(65535, via.Read(C.Timer1Low) | via.Read(C.Timer1High) << 8);
    }

    [Fact]
    public void ControlPulsesAndIrqCallbacksKeepTheirDatesWhenTheCallbackReloadsTimers()
    {
        using var pair = new Pair();
        foreach (var via in new[] { pair.Unit, pair.Grouped })
        {
            var reloads = 0;
            var record = via.IrqChanged;
            via.IrqChanged = level =>
            {
                record!(level);
                if (!level || ++reloads > 3) return;
                via.Write(C.Flags, C.Timer1Interrupt);
                via.Write(C.Timer1Low, 7); via.Write(C.Timer1High, 0);
                via.SetPb6(false);
            };
        }
        pair.Both(via =>
        {
            via.SetPb6(true);
            via.Write(C.Auxiliary, 0xe0); via.Write(C.Enable, 0xe0);
            via.Write(C.Peripheral, (byte)((int)ViaControlMode.PulseOutput << 1 | (int)ViaControlMode.PulseOutput << 5));
            via.Write(C.PortA, 0x5a); via.Write(C.PortB, 0xa5);
            via.Write(C.Timer1Low, 3); via.Write(C.Timer1High, 0);
            via.Write(C.Timer2Low, 0); via.Write(C.Timer2High, 0);
        });
        pair.Advance(1000); pair.Check();
        Assert.Contains(pair.UnitEvents, item => item.Pin == "CA2" && item.Value == 1 && item.Clock == 2);
        pair.Both(via => via.Reset()); pair.Advance(1000); pair.Check();
    }

    [Fact]
    public void LegacyPb6ProviderIsNotInvokedToPredictAStablePrefix()
    {
        var via = new Via6522();
        var reads = 0;
        via.ReadPortB = () => { reads++; return 0xff; };
        try
        {
            via.Write(C.Auxiliary, C.Timer2PulseCount);
            via.Write(C.Timer2Low, 20); via.Write(C.Timer2High, 0);
            var before = reads;
            Assert.Equal(0, via.TryAdvanceStableClocks(1000));
            Assert.Equal(before, reads);
            via.Tick(); Assert.Equal(before + 1, reads);
            via.SetPb6(true);
            Assert.Equal(1000, via.TryAdvanceStableClocks(1000));
            Assert.Equal(before + 1, reads);
            Assert.Equal(20, via.Read(C.Timer2Low));
        }
        finally { via.ReadPortB = null; }
    }

    [Fact]
    public void IdlePrefixHandlesLongCountsWithoutCreatingEventsAndRejectsNegativeCounts()
    {
        var via = new Via6522();
        var callbacks = 0;
        via.IrqChanged = _ => callbacks++;
        via.PortBPinsChanged = _ => callbacks++;
        try
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => via.TryAdvanceStableClocks(-1));
            Assert.Equal(0, via.TryAdvanceStableClocks(0));
            Assert.Equal(long.MaxValue, via.TryAdvanceStableClocks(long.MaxValue));
            Assert.Equal(0, callbacks);
            Assert.Equal(0, via.Read(C.Flags));
            Assert.Equal(65535, via.Read(C.Timer1Low) | via.Read(C.Timer1High) << 8);
        }
        finally { via.IrqChanged = null; via.PortBPinsChanged = null; }
    }

    private readonly record struct PinEvent(long Clock, string Pin, int Value);
    private sealed class Pair : IDisposable
    {
        internal readonly Via6522 Unit = new();
        internal readonly Via6522 Grouped = new();
        internal readonly List<PinEvent> UnitEvents = [];
        private readonly List<PinEvent> _groupedEvents = [];
        private long _unitClock, _groupedClock;
        private bool _inPrefix;
        internal Pair()
        {
            Attach(Unit, UnitEvents, () => _unitClock);
            Attach(Grouped, _groupedEvents, () => _groupedClock);
        }
        private void Attach(Via6522 via, List<PinEvent> events, Func<long> clock)
        {
            void Record(string pin, int value)
            {
                Assert.False(_inPrefix, "Un préfixe stable ne doit publier aucun effet anticipé.");
                events.Add(new(clock(), pin, value));
            }
            via.IrqChanged = high => Record("IRQ", high ? 1 : 0);
            via.PortBPinsChanged = value => Record("PB", value);
            via.Ca2PinsChanged = high => Record("CA2", high ? 1 : 0);
            via.Cb1PinsChanged = high => Record("CB1", high ? 1 : 0);
            via.Cb2PinsChanged = high => Record("CB2", high ? 1 : 0);
            via.ControlOutputsChanged = outputs => Record("DRIVE", (int)outputs);
        }
        internal void Both(Action<Via6522> action) { action(Unit); action(Grouped); }
        internal void Advance(int count)
        {
            for (var i = 0; i < count; i++) { _unitClock++; Unit.Tick(); }
            long remaining = count;
            while (remaining != 0)
            {
                _inPrefix = true;
                long represented;
                try { represented = Grouped.TryAdvanceStableClocks(remaining); }
                finally { _inPrefix = false; }
                Assert.InRange(represented, 0, remaining);
                if (represented == 0) { _groupedClock++; Grouped.Tick(); remaining--; }
                else { _groupedClock += represented; remaining -= represented; }
            }
        }
        internal void Check()
        {
            Assert.Equal(_unitClock, _groupedClock);
            Assert.Equal(UnitEvents, _groupedEvents);
            Assert.Equal((Unit.Irq, Unit.Ca2, Unit.Cb1, Unit.Cb2, Unit.ControlOutputs, Unit.OutputA),
                (Grouped.Irq, Grouped.Ca2, Grouped.Cb1, Grouped.Cb2, Grouped.ControlOutputs, Grouped.OutputA));
            for (var register = 0; register < 16; register++)
                if (register != C.Shift) Assert.Equal(Unit.Read(register), Grouped.Read(register));
            Assert.Equal(UnitEvents, _groupedEvents);
        }
        public void Dispose()
        {
            foreach (var via in new[] { Unit, Grouped })
            {
                via.IrqChanged = via.Ca2PinsChanged = via.Cb1PinsChanged = via.Cb2PinsChanged = null;
                via.PortBPinsChanged = null; via.ControlOutputsChanged = null;
            }
        }
    }
}

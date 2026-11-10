using RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Services;
using RetroMediaLab.Chips.Controllers.Acorn.SystemClock.Interfaces;

namespace RetroMediaLab.Chips.Tests.Controllers.Acorn;

public sealed class SystemClockGeneratorTests
{
    [Fact]
    public void CompleteReferencePeriodsAreQualifiedOnlyAtTheirPhysicalLowBoundary()
    {
        var source = new SystemClockGenerator(); source.Clock12MHzReceiver = new IndependentReceiver();
        Assert.False(source.CanRepresentReferencePeriods);
        for (var clock = 1; clock <= 200; clock++)
        {
            source.Clock();
            if (clock >= 96) Assert.Equal(clock % 12 == 0, source.CanRepresentReferencePeriods);
        }
        source.Clock4MHzChanged = _ => { };
        for (var clock = 0; clock < 20; clock++) { source.Clock(); Assert.False(source.CanRepresentReferencePeriods); }
    }

    [Theory]
    [InlineData(2, 0, 0)]
    [InlineData(0, 7, 13)]
    [InlineData(3, 15, 7)]
    public void LongRepresentedSourceRangesMatchUnitaryClocksAndResumeAllOutputs(byte a, byte b, byte c)
    {
        var actual = new SystemClockGenerator(a, b, c); var expected = new SystemClockGenerator(a, b, c);
        var receiver = new IndependentReceiver(); receiver.SetClock(actual.Clock12MHz);
        actual.Clock12MHzReceiver = receiver;
        actual.AdvanceClocks(128); expected.AdvanceClocks(128);
        foreach (var length in new long[] { 1, 7, 47, 48, 49, 1_001, 20_003 })
        {
            // The wire owner has represented these clock levels independently.
            receiver.AdvancePeriods((int)(length / 2), receiver.High);
            if ((length & 1) != 0) receiver.SetClock(!receiver.High);
            actual.AdvanceRepresentedNetworkClocks(length);
            expected.AdvanceClocks(length);
            Assert.Equal(Levels(expected), Levels(actual));
            Assert.Equal(expected.InputClockCount, actual.InputClockCount);
            Assert.False(actual.OscillatorHigh);
            foreach (var pair in new[] { (actual.Ic2, expected.Ic2), (actual.Ic3, expected.Ic3), (actual.Ic4, expected.Ic4) })
            {
                Assert.Equal(pair.Item2.Count, pair.Item1.Count); Assert.Equal(pair.Item2.Carry, pair.Item1.Carry);
                Assert.Equal(pair.Item2.InputClockCount, pair.Item1.InputClockCount); Assert.False(pair.Item1.ClockHigh);
            }
        }
        actual.Clock12MHzReceiver = null;
        var actualPins = new List<(long, int, bool)>(); var expectedPins = new List<(long, int, bool)>();
        Connect(actual, (frequency, high) => actualPins.Add((actual.InputClockCount, frequency, high)));
        Connect(expected, (frequency, high) => expectedPins.Add((expected.InputClockCount, frequency, high)));
        actual.AdvanceClocks(101); expected.AdvanceClocks(101);
        Assert.Equal(expectedPins, actualPins);
    }

    [Fact]
    public void RepresentedRangeRejectsUnqualifiedNetworksAndUnrepresentedObservers()
    {
        var source = new SystemClockGenerator(); var receiver = new IndependentReceiver();
        source.Clock12MHzReceiver = receiver;
        Assert.Throws<InvalidOperationException>(() => source.AdvanceRepresentedNetworkClocks(12));
        Assert.Equal(0, source.InputClockCount);
        source.AdvanceClocks(128); source.Clock4MHzChanged = _ => { };
        Assert.Throws<InvalidOperationException>(() => source.AdvanceRepresentedNetworkClocks(12));
        Assert.Equal(128, source.InputClockCount);
        source.Clock4MHzChanged = null;
        Assert.Throws<OverflowException>(() => source.AdvanceRepresentedNetworkClocks(long.MaxValue));
        Assert.Equal(128, source.InputClockCount);
    }

    [Theory]
    [InlineData(2, 0, 0)]
    [InlineData(3, 15, 7)]
    [InlineData(0, 3, 1)]
    public void IndependentPeriodsPreserveChangingPixelsAndEveryOtherDomainEdge(byte a, byte b, byte c)
    {
        var fast = new SystemClockGenerator(a, b, c); var unit = new SystemClockGenerator(a, b, c);
        var video = new IndependentReceiver(); var reference = new IndependentReceiver();
        var actual = new List<(long, bool, int, byte, byte, byte)>();
        var expected = new List<(long, bool, int, byte, byte, byte)>();
        var actualTwo = new List<(long, bool, long)>(); var expectedTwo = new List<(long, bool, long)>();
        void Wire(SystemClockGenerator source, IndependentReceiver receiver,
            List<(long, bool, int, byte, byte, byte)> fours, List<(long, bool, long)> twos)
        {
            receiver.SetClock(source.Clock12MHz);
            source.Clock12MHzReceiver = receiver;
            // Intentionally never qualifies as output-stable.
            source.Clock12MHzCanGroup = false;
            source.Clock4MHzChanged = high => fours.Add((source.InputClockCount, high, Levels(source),
                source.Ic2.Count, source.Ic3.Count, source.Ic4.Count));
            source.Clock4MHzCanChangeClock12Inputs = false;
            source.Clock2MHzChanged = high => twos.Add((source.InputClockCount, high, receiver.Edges));
        }
        Wire(fast, video, actual, actualTwo); Wire(unit, reference, expected, expectedTwo);
        var bindingRejected = false;
        video.Changed = () =>
        {
            if (!video.InPeriods) return;
            Assert.Throws<InvalidOperationException>(() => fast.Clock4MHzChanged = null);
            bindingRejected = true;
        };
        foreach (var span in new[] { 1, 2, 5, 12, 31, 97, 301, 1001 })
        {
            fast.AdvanceClocks(span);
            for (var i = 0; i < span; i++) unit.Clock();
            Assert.Equal(expected, actual); Assert.Equal(expectedTwo, actualTwo);
            Assert.Equal(reference.Edges, video.Edges); Assert.Equal(reference.High, video.High);
            Assert.Equal(unit.Ic2.InputClockCount, fast.Ic2.InputClockCount);
            Assert.Equal(unit.Ic3.InputClockCount, fast.Ic3.InputClockCount);
            Assert.Equal(unit.Ic4.InputClockCount, fast.Ic4.InputClockCount);
            Assert.Equal(reference.Trace, video.Trace);
        }
        Assert.True(video.Periods > 0); Assert.True(bindingRejected);
    }

    private sealed class IndependentReceiver : IIndependentClockPeriodReceiver
    {
        internal bool High, InPeriods;
        internal long Edges, Periods;
        internal Action? Changed;
        internal readonly List<(long, bool)> Trace = [];
        public void SetClock(bool high)
        {
            if (High == high) return;
            High = high; Edges++; Trace.Add((Edges, high)); Changed?.Invoke();
        }
        public int AdvanceStablePeriods(int maximumPeriods) => 0;
        public void AdvancePeriods(int periods, bool startHigh)
        {
            Assert.Equal(startHigh, High);
            InPeriods = true;
            try { for (var i = 0; i < periods; i++) { SetClock(!startHigh); SetClock(startHigh); } }
            finally { InPeriods = false; }
            Periods += periods;
        }
    }

    [Theory]
    [InlineData(2, 0, 0, 1)]
    [InlineData(2, 0, 0, 2)]
    [InlineData(2, 0, 0, 3)]
    [InlineData(3, 15, 7, 1)]
    [InlineData(3, 15, 7, 2)]
    [InlineData(3, 15, 7, 3)]
    [InlineData(0, 3, 1, 1)]
    [InlineData(0, 3, 1, 2)]
    [InlineData(0, 3, 1, 3)]
    public void IndependentFourMhzKeepsEveryBoundaryAndPartialReceiverPeriod(byte a, byte b, byte c, int maximum)
    {
        var fast = new SystemClockGenerator(a, b, c);
        var unit = new SystemClockGenerator(a, b, c);
        var video = new StableReceiver(true, maximum);
        var reference = new StableReceiver(true);
        var actualFour = new List<(long, bool, int, byte, byte, byte)>();
        var expectedFour = new List<(long, bool, int, byte, byte, byte)>();
        var actualTwo = new List<(long, bool, long, long, bool)>();
        var expectedTwo = new List<(long, bool, long, long, bool)>();
        void Wire(SystemClockGenerator source, StableReceiver receiver, bool grouped,
            List<(long, bool, int, byte, byte, byte)> fours, List<(long, bool, long, long, bool)> twos)
        {
            receiver.SetClock(source.Clock12MHz);
            source.Clock12MHzReceiver = receiver; source.Clock12MHzCanGroup = grouped;
            source.Clock4MHzChanged = high =>
            {
                // This independent receiver sees the real source state, but
                // neither reads nor changes the preadvanced video receiver.
                fours.Add((source.InputClockCount, high, Levels(source), source.Ic2.Count, source.Ic3.Count, source.Ic4.Count));
                Assert.Equal(source.InputClockCount, source.Ic2.InputClockCount);
                Assert.Equal(source.InputClockCount, source.Ic3.InputClockCount);
                Assert.Equal(source.InputClockCount, source.Ic4.InputClockCount);
            };
            source.Clock2MHzChanged = high => twos.Add((source.InputClockCount, high, receiver.Edges, receiver.Rises, receiver.High));
            source.Clock4MHzCanChangeClock12Inputs = !grouped;
        }
        Wire(fast, video, true, actualFour, actualTwo);
        Wire(unit, reference, false, expectedFour, expectedTwo);
        foreach (var span in new[] { 1, 2, 5, 12, 31, 97, 301, 1001 })
        {
            fast.AdvanceClocks(span);
            for (var i = 0; i < span; i++) unit.Clock();
            Assert.Equal(expectedFour, actualFour); Assert.Equal(expectedTwo, actualTwo);
            Assert.Equal(reference.Edges, video.Edges); Assert.Equal(reference.Rises, video.Rises);
            Assert.Equal(reference.High, video.High); Assert.Equal(Levels(unit), Levels(fast));
            Assert.Equal(unit.Ic2.Count, fast.Ic2.Count); Assert.Equal(unit.Ic3.Count, fast.Ic3.Count);
            Assert.Equal(unit.Ic4.Count, fast.Ic4.Count);
        }
        Assert.True(video.Accepted > 0);
    }

    [Fact]
    public void IndependentWindowRejectsBindingChangesBeforeItsCausalBoundary()
    {
        var source = new SystemClockGenerator();
        var video = new StableReceiver(true);
        source.Clock12MHzReceiver = video; source.Clock12MHzCanGroup = true;
        var rejected = false; var allowed = false;
        source.Clock4MHzChanged = _ =>
        {
            if (source.InputClockCount != 51) return;
            Assert.Throws<InvalidOperationException>(() => source.Clock12MHzChanged = _ => { });
            Assert.Null(source.Clock12MHzChanged);
            rejected = true;
        };
        source.Clock2MHzChanged = _ =>
        {
            if (source.InputClockCount != 54) return;
            source.Clock8MHzChanged = _ => { };
            allowed = true;
        };
        source.Clock4MHzCanChangeClock12Inputs = false;
        source.AdvanceClocks(96);
        Assert.True(rejected); Assert.True(allowed);
    }

    [Theory]
    [InlineData(2, 0, 0)]
    [InlineData(3, 15, 7)]
    [InlineData(0, 3, 1)]
    public void HighPhasePeriodsKeepFourAndTwoMhzBoundariesAndEveryCounter(byte a, byte b, byte c)
    {
        var fast = new SystemClockGenerator(a, b, c);
        var unit = new SystemClockGenerator(a, b, c);
        var receiver = new StableReceiver(true); var reference = new StableReceiver();
        var actual = new List<(long, int, bool, long, long, int)>();
        var expected = new List<(long, int, bool, long, long, int)>();
        void Wire(SystemClockGenerator source, StableReceiver video,
            List<(long, int, bool, long, long, int)> log, bool grouped)
        {
            video.SetClock(source.Clock12MHz);
            source.Clock12MHzReceiver = video; source.Clock12MHzCanGroup = grouped;
            source.Clock4MHzChanged = high => log.Add((source.InputClockCount, 4, high, video.Edges, video.Rises, Levels(source)));
            source.Clock2MHzChanged = high => log.Add((source.InputClockCount, 2, high, video.Edges, video.Rises, Levels(source)));
        }
        Wire(fast, receiver, actual, true); Wire(unit, reference, expected, false);
        foreach (var span in new[] { 1, 2, 5, 12, 31, 97, 301, 1001 })
        {
            fast.AdvanceClocks(span);
            for (var i = 0; i < span; i++) unit.Clock();
            Assert.Equal(expected, actual);
            Assert.Equal(reference.High, receiver.High);
            Assert.Equal(reference.Edges, receiver.Edges);
            Assert.Equal(reference.Rises, receiver.Rises);
            Assert.Equal(Levels(unit), Levels(fast));
            Assert.Equal(unit.Ic2.Count, fast.Ic2.Count);
            Assert.Equal(unit.Ic3.Count, fast.Ic3.Count);
            Assert.Equal(unit.Ic4.Count, fast.Ic4.Count);
            Assert.Equal(unit.Ic2.InputClockCount, fast.Ic2.InputClockCount);
            Assert.Equal(unit.Ic3.InputClockCount, fast.Ic3.InputClockCount);
            Assert.Equal(unit.Ic4.InputClockCount, fast.Ic4.InputClockCount);
        }
        Assert.True(receiver.HighAccepted > 0);
    }

    [Theory]
    [InlineData(2, 0, 0)]
    [InlineData(2, 0, 7)]
    [InlineData(3, 15, 7)]
    [InlineData(5, 15, 0)]
    [InlineData(15, 7, 13)]
    [InlineData(0, 3, 1)]
    [InlineData(1, 12, 10)]
    public void StablePeriodDeliveryPreservesClockBoundariesAndCounterPhases(byte a, byte b, byte c)
    {
        var fast = new SystemClockGenerator(a, b, c);
        var unit = new SystemClockGenerator(a, b, c);
        var fastReceiver = new StableReceiver();
        var unitReceiver = new StableReceiver();
        var first = new List<(long, bool, long, long, int)>();
        var second = new List<(long, bool, long, long, int)>();
        void Wire(SystemClockGenerator source, StableReceiver receiver,
            List<(long, bool, long, long, int)> edges, bool grouped)
        {
            receiver.SetClock(source.Clock12MHz);
            source.Clock12MHzReceiver = receiver;
            source.Clock12MHzCanGroup = grouped;
            source.Clock2MHzChanged = high =>
            {
                edges.Add((source.InputClockCount, high, receiver.Rises, receiver.Edges, Levels(source)));
                if (source.InputClockCount == 120)
                    source.Clock8MHzChanged = level => edges.Add((source.InputClockCount, level,
                        receiver.Rises, receiver.Edges, Levels(source)));
                if (source.InputClockCount == 240) source.Clock8MHzChanged = null;
            };
        }
        Wire(fast, fastReceiver, first, true);
        Wire(unit, unitReceiver, second, false);
        foreach (var count in new[] { 1, 2, 5, 12, 31, 97, 301, 1001 })
        {
            fast.AdvanceClocks(count);
            for (var clock = 0; clock < count; clock++)
            {
                unit.SetOscillator(true); unit.SetOscillator(false);
            }
            Assert.Equal(second, first);
            Assert.Equal(unitReceiver.Rises, fastReceiver.Rises);
            Assert.Equal(unitReceiver.Edges, fastReceiver.Edges);
            Assert.Equal(unitReceiver.High, fastReceiver.High);
            Assert.Equal(Levels(unit), Levels(fast));
            Assert.Equal(unit.Ic2.Count, fast.Ic2.Count);
            Assert.Equal(unit.Ic3.Count, fast.Ic3.Count);
            Assert.Equal(unit.Ic4.Count, fast.Ic4.Count);
            Assert.Equal(unit.Ic4.InputClockCount, fast.Ic4.InputClockCount);
            Assert.False(fast.Ic2.ClockHigh); Assert.False(fast.Ic3.ClockHigh); Assert.False(fast.Ic4.ClockHigh);
        }
        Assert.True(fastReceiver.Accepted > 0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AdditionalClockObserversRetainEveryEdgeAndDisableGrouping(bool observeTwelve)
    {
        var source = new SystemClockGenerator();
        var receiver = new StableReceiver();
        source.Clock12MHzReceiver = receiver;
        source.Clock12MHzCanGroup = true;
        var observations = 0;
        if (observeTwelve) source.Clock12MHzChanged = _ => observations++;
        else source.Clock8MHzChanged = _ => observations++;
        source.AdvanceClocks(3600);
        Assert.Equal(0, receiver.Accepted);
        Assert.Equal(3600, receiver.Edges);
        Assert.Equal(observeTwelve ? 3600 : 2400, observations);
        Assert.Equal(3600, source.Ic2.InputClockCount);
    }

    [Theory]
    [InlineData(2, 0, 0)]
    [InlineData(2, 0, 7)]
    [InlineData(3, 15, 7)]
    [InlineData(5, 15, 0)]
    [InlineData(15, 7, 13)]
    [InlineData(0, 3, 1)]
    [InlineData(1, 12, 10)]
    public void FourMhzReceiverPreservesEveryFrontWithoutDisablingStableVideo(byte a, byte b, byte c)
    {
        var fast = new SystemClockGenerator(a, b, c);
        var unit = new SystemClockGenerator(a, b, c);
        var fastVideo = new StableReceiver();
        var unitVideo = new StableReceiver();
        var actual = new List<(long, int, bool, long, long, int)>();
        var expected = new List<(long, int, bool, long, long, int)>();
        void Wire(SystemClockGenerator source, StableReceiver video,
            List<(long, int, bool, long, long, int)> log, bool group)
        {
            video.SetClock(source.Clock12MHz);
            source.Clock12MHzReceiver = video;
            source.Clock12MHzCanGroup = group;
            source.Clock4MHzChanged = high =>
            {
                log.Add((source.InputClockCount, 4, high, video.Rises, video.Edges, Levels(source)));
                // A physical receiver can invalidate the next stable interval.
                if (group && source.InputClockCount == 90) source.Clock12MHzCanGroup = false;
                if (group && source.InputClockCount == 150) source.Clock12MHzCanGroup = true;
            };
            source.Clock2MHzChanged = high => log.Add((source.InputClockCount, 2, high,
                video.Rises, video.Edges, Levels(source)));
        }
        Wire(fast, fastVideo, actual, true);
        Wire(unit, unitVideo, expected, false);
        foreach (var size in new[] { 1, 2, 3, 5, 12, 31, 97, 301, 1001 })
        {
            fast.AdvanceClocks(size);
            for (var i = 0; i < size; i++) unit.Clock();
            Assert.Equal(expected, actual);
            Assert.Equal(unitVideo.Edges, fastVideo.Edges);
            Assert.Equal(unitVideo.Rises, fastVideo.Rises);
            Assert.Equal(unitVideo.High, fastVideo.High);
            Assert.Equal(Levels(unit), Levels(fast));
            Assert.Equal(unit.Ic2.Count, fast.Ic2.Count);
            Assert.Equal(unit.Ic3.Count, fast.Ic3.Count);
            Assert.Equal(unit.Ic4.Count, fast.Ic4.Count);
            Assert.Equal(unit.InputClockCount, fast.InputClockCount);
            Assert.Equal(unit.Ic2.InputClockCount, fast.Ic2.InputClockCount);
            Assert.Equal(unit.Ic3.InputClockCount, fast.Ic3.InputClockCount);
            Assert.Equal(unit.Ic4.InputClockCount, fast.Ic4.InputClockCount);
            Assert.False(fast.OscillatorHigh);
            Assert.False(fast.Ic2.ClockHigh);
            Assert.False(fast.Ic3.ClockHigh);
            Assert.False(fast.Ic4.ClockHigh);
        }
        Assert.True(fastVideo.Accepted > 0);
    }

    [Fact]
    public void FourMhzSubscriptionChangesDuringAFrontRetainTheNextPhysicalBoundary()
    {
        var fast = new SystemClockGenerator();
        var unit = new SystemClockGenerator();
        var actual = new List<(long, int, bool, long)>();
        var expected = new List<(long, int, bool, long)>();
        void Wire(SystemClockGenerator source, List<(long, int, bool, long)> log, bool group)
        {
            var video = new StableReceiver();
            source.Clock12MHzReceiver = video;
            source.Clock12MHzCanGroup = group;
            source.Clock2MHzChanged = high =>
            {
                log.Add((source.InputClockCount, 2, high, video.Edges));
                if (source.InputClockCount == 120)
                    source.Clock4MHzChanged = level => log.Add((source.InputClockCount, 4, level, video.Edges));
                if (source.InputClockCount == 240) source.Clock4MHzChanged = null;
                if (source.InputClockCount == 360)
                    source.Clock4MHzChanged = level => log.Add((source.InputClockCount, 4, level, video.Edges));
            };
        }
        Wire(fast, actual, true); Wire(unit, expected, false);
        fast.AdvanceClocks(1200);
        for (var i = 0; i < 1200; i++) unit.Clock();
        Assert.Equal(expected, actual);
        Assert.Equal(unit.Ic2.Count, fast.Ic2.Count);
        Assert.Equal(unit.Ic3.Count, fast.Ic3.Count);
        Assert.Equal(unit.Ic4.Count, fast.Ic4.Count);
    }

    private sealed class StableReceiver(bool highPeriods = false, int maximumAccepted = int.MaxValue) : IStableClockPeriodReceiver
    {
        public bool High { get; private set; }
        public long Rises { get; private set; }
        public long Edges { get; private set; }
        public long Accepted { get; private set; }
        public long HighAccepted { get; private set; }
        public int AdvanceStablePeriods(int maximumPeriods, bool startHigh)
        {
            if (!startHigh) return AdvanceStablePeriods(maximumPeriods);
            if (!highPeriods) return 0;
            Assert.True(High);
            maximumPeriods = Math.Min(maximumPeriods, maximumAccepted);
            Rises += maximumPeriods; Edges += maximumPeriods * 2L;
            Accepted += maximumPeriods; HighAccepted += maximumPeriods;
            return maximumPeriods;
        }
        public void SetClock(bool high)
        {
            if (high == High) return;
            High = high; Edges++;
            if (high) Rises++;
        }
        public int AdvanceStablePeriods(int maximumPeriods)
        {
            Assert.False(High);
            maximumPeriods = Math.Min(maximumPeriods, maximumAccepted);
            Rises += maximumPeriods;
            Edges += maximumPeriods * 2L;
            Accepted += maximumPeriods;
            return maximumPeriods;
        }
    }

    [Fact]
    public void AttachingEarlierOutputsDuringALaterPublicationDoesNotReplayTheSameEdge()
    {
        var circuit = new SystemClockGenerator();
        var edges = new List<(long, int, bool)>();
        circuit.Clock6MHzChanged = high =>
        {
            edges.Add((circuit.InputClockCount, 6, high));
            circuit.Clock12MHzChanged = level => edges.Add((circuit.InputClockCount, 12, level));
            circuit.Clock8MHzChanged = level => edges.Add((circuit.InputClockCount, 8, level));
            circuit.Clock6MHzChanged = null;
        };
        circuit.AdvanceClocks(3);
        Assert.Equal(new[] { (2L, 6, true), (3L, 12, true), (3L, 8, false) }, edges);
    }

    [Fact]
    public void ReceiversCanChangeDuringTheSameResolvedEdgeWithoutLosingLaterOutputs()
    {
        var circuit = new SystemClockGenerator();
        var edges = new List<(long, bool)>();
        circuit.Clock12MHzChanged = _ =>
        {
            if (circuit.InputClockCount == 2)
                circuit.Clock8MHzChanged = high => edges.Add((circuit.InputClockCount, high));
            if (circuit.InputClockCount == 3)
            {
                circuit.Clock8MHzChanged = null;
                circuit.Clock12MHzChanged = null;
            }
        };
        circuit.AdvanceClocks(4);
        Assert.Equal(new[] { (2L, true) }, edges);
        circuit.Clock8MHzChanged = high => edges.Add((circuit.InputClockCount, high));
        Assert.Single(edges); // Wiring alone does not fabricate an edge.
        circuit.AdvanceClocks(2);
        Assert.Equal(new[] { (2L, true), (5L, true), (6L, false) }, edges);
        Assert.Equal(ExpectedLevels(6), Levels(circuit));
    }

    [Fact]
    public void FallingEdgeReachesEveryCounterWithoutChangingResolvedOutputs()
    {
        var circuit = new SystemClockGenerator();
        var edges = new List<(long, int, bool)>();
        Connect(circuit, (frequency, high) => edges.Add((circuit.InputClockCount, frequency, high)));
        for (var period = 1; period <= 96; period++)
        {
            circuit.SetOscillator(true);
            var outputs = Levels(circuit);
            var notifications = edges.Count;
            var counts = new[] { circuit.Ic2.Count, circuit.Ic3.Count, circuit.Ic4.Count };
            circuit.SetOscillator(false);
            Assert.Equal(outputs, Levels(circuit));
            Assert.Equal(notifications, edges.Count);
            var index = 0;
            foreach (var counter in new[] { circuit.Ic2, circuit.Ic3, circuit.Ic4 })
            {
                Assert.False(counter.ClockHigh);
                Assert.Equal(period, counter.InputClockCount);
                Assert.Equal(counts[index++], counter.Count);
            }
        }
    }

    [Fact]
    public void EveryMasterEdgeMatchesFigureNineIncludingTheEightMhzDutyCycle()
    {
        var circuit = new SystemClockGenerator();
        Assert.Equal(24_000_000L, circuit.OscillatorFrequency);
        for (var time = 1; time <= 96; time++)
        {
            circuit.Clock();
            Assert.Equal((time + 2) % 3, circuit.Ic2.Count);
            Assert.Equal(time / 3 % 16, circuit.Ic3.Count);
            Assert.Equal(time % 16, circuit.Ic4.Count);
            Assert.Equal(time % 48 == 47, circuit.Ic3.Carry);
            Assert.Equal(ExpectedLevels(time), Levels(circuit));
            foreach (var counter in new[] { circuit.Ic2, circuit.Ic3, circuit.Ic4 })
            {
                Assert.Equal(time, counter.InputClockCount);
                Assert.Equal(24_000_000L, counter.ClockFrequency);
                Assert.False(counter.ClockHigh);
            }
        }
    }

    [Fact]
    public void CarrySynchronizesAnInitiallyOutOfPhaseIcFourAtTheRealSharedEdge()
    {
        var circuit = new SystemClockGenerator(2, 0, 7);
        circuit.AdvanceClocks(47);
        Assert.True(circuit.Ic3.Carry);
        Assert.Equal(6, circuit.Ic4.Count);
        circuit.Clock();
        Assert.Equal(0, circuit.Ic4.Count);
        Assert.Equal(0, circuit.Ic3.Count);
        Assert.Equal(ExpectedLevels(48), Levels(circuit));
    }

    [Fact]
    public void OutputReceiversSeeAllLevelsResolvedForTheSamePhysicalEdge()
    {
        var circuit = new SystemClockGenerator();
        var edges = new List<(long, int, bool)>();
        Connect(circuit, (frequency, high) =>
        {
            Assert.True(circuit.OscillatorHigh);
            Assert.Equal(ExpectedLevels(circuit.InputClockCount), Levels(circuit));
            edges.Add((circuit.InputClockCount, frequency, high));
        });
        circuit.AdvanceClocks(48);
        (int Frequency, int FallingEdges)[] counts = [(12, 24), (8, 16), (6, 12), (4, 8), (3, 6), (2, 4), (1, 2)];
        foreach (var (frequency, expected) in counts)
            Assert.Equal(expected, edges.Count(edge => edge.Item2 == frequency && !edge.Item3));
        Assert.Equal(new long[] { 12, 24, 36, 48 }, edges.Where(edge => edge.Item2 == 2 && !edge.Item3).Select(edge => edge.Item1));
        Assert.Equal(new long[] { 24, 48 }, edges.Where(edge => edge.Item2 == 1 && !edge.Item3).Select(edge => edge.Item1));
    }

    [Fact]
    public void ARepeatedClockLevelDoesNotInventAnExtraEdge()
    {
        var circuit = new SystemClockGenerator();
        var changes = 0;
        circuit.Clock12MHzChanged = _ => changes++;
        circuit.SetOscillator(true); circuit.SetOscillator(true);
        Assert.Equal(1, circuit.InputClockCount);
        Assert.Equal(1, changes);
        Assert.Throws<InvalidOperationException>(() => circuit.Clock());
        circuit.SetOscillator(false); circuit.SetOscillator(false);
        Assert.Equal(1, changes);
        circuit.Clock();
        Assert.Equal(2, circuit.InputClockCount);
        Assert.Equal(2, changes);
        Assert.Throws<ArgumentOutOfRangeException>(() => circuit.AdvanceClocks(-1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(17)]
    [InlineData(48)]
    [InlineData(1001)]
    public void GroupedSourcePeriodsPreserveAllConnectedOutputEdges(int periods)
    {
        var grouped = new SystemClockGenerator();
        var unit = new SystemClockGenerator();
        var levels = new SystemClockGenerator();
        var first = new List<(long, int, bool)>();
        var second = new List<(long, int, bool)>();
        var third = new List<(long, int, bool)>();
        Connect(grouped, (frequency, high) => first.Add((grouped.InputClockCount, frequency, high)));
        Connect(unit, (frequency, high) => second.Add((unit.InputClockCount, frequency, high)));
        Connect(levels, (frequency, high) => third.Add((levels.InputClockCount, frequency, high)));
        grouped.AdvanceClocks(periods);
        for (var i = 0; i < periods; i++)
        {
            unit.Clock();
            levels.SetOscillator(true);
            levels.SetOscillator(false);
        }
        Assert.Equal(second, first);
        Assert.Equal(third, first);
        Assert.Equal(Levels(unit), Levels(grouped));
        Assert.Equal(Levels(levels), Levels(grouped));
        Assert.Equal(levels.InputClockCount, grouped.InputClockCount);
        Assert.Equal(unit.Ic3.Count, grouped.Ic3.Count);
        Assert.Equal(unit.Ic4.Count, grouped.Ic4.Count);
    }

    [Theory]
    [InlineData(2, 0, 0)]
    [InlineData(0, 7, 13)]
    [InlineData(1, 15, 5)]
    public void StableFourMhzDescriptionMatchesAllSubsequentPhysicalEdges(byte ic2, byte ic3, byte ic4)
    {
        var circuit = new SystemClockGenerator(ic2, ic3, ic4);
        long firstFall = 0, period = 0, highLength = 0, publishedAt = 0;
        var announcements = 0;
        var actual = new List<(long, bool)>();
        circuit.Clock4MHzChanged = high =>
        {
            if (announcements != 0) actual.Add((circuit.InputClockCount, high));
        };
        circuit.Clock4MHzPeriodicAvailable = (fall, length, high) =>
        {
            firstFall = fall; period = length; highLength = high;
            publishedAt = circuit.InputClockCount; announcements++;
        };
        circuit.AdvanceClocks(512);
        Assert.Equal(1, announcements);
        Assert.Equal(6, period); Assert.Equal(3, highLength);
        var expected = new List<(long, bool)>();
        for (var fall = firstFall; fall <= 512; fall += period)
        {
            if (fall - highLength > publishedAt) expected.Add((fall - highLength, true));
            expected.Add((fall, false));
        }
        // Include a last rising edge whose following fall lies beyond the window.
        var lastRise = firstFall + ((512 - firstFall) / period + 1) * period - highLength;
        if (lastRise <= 512) expected.Add((lastRise, true));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RepresentedNetworkCanReturnToAllExplicitClockOutputsWithoutAStateJump()
    {
        var grouped = new SystemClockGenerator(); var scalar = new SystemClockGenerator();
        var receiver = new IndependentReceiver();
        receiver.SetClock(grouped.Clock12MHz); grouped.Clock12MHzReceiver = receiver;
        grouped.Clock2MHzChanged = _ => { };
        grouped.AdvanceClocks(10_001); scalar.AdvanceClocks(10_001);
        Assert.True(receiver.Periods > 0);
        grouped.Clock12MHzReceiver = null;
        var actual = new List<(long, int, bool)>(); var expected = new List<(long, int, bool)>();
        Connect(grouped, (clock, high) => actual.Add((grouped.InputClockCount, clock, high)));
        Connect(scalar, (clock, high) => expected.Add((scalar.InputClockCount, clock, high)));
        grouped.AdvanceClocks(1_001); scalar.AdvanceClocks(1_001);
        Assert.Equal(expected, actual);
        Assert.Equal(scalar.Ic2.Count, grouped.Ic2.Count);
        Assert.Equal(scalar.Ic3.Count, grouped.Ic3.Count);
        Assert.Equal(scalar.Ic4.Count, grouped.Ic4.Count);
        Assert.Equal(scalar.Ic4.Carry, grouped.Ic4.Carry);
        Assert.Equal(scalar.Ic4.InputClockCount, grouped.Ic4.InputClockCount);
    }

    private static int ExpectedLevels(long sourceTime) =>
        (sourceTime % 2 != 0 ? 1 : 0) | (sourceTime % 3 == 2 ? 2 : 0)
        | (sourceTime % 4 >= 2 ? 4 : 0) | (sourceTime / 3 % 2 != 0 ? 8 : 0)
        | (sourceTime % 8 >= 4 ? 16 : 0) | (sourceTime / 6 % 2 != 0 ? 32 : 0)
        | (sourceTime / 12 % 2 != 0 ? 64 : 0);

    private static int Levels(SystemClockGenerator circuit) =>
        (circuit.Clock12MHz ? 1 : 0) | (circuit.Clock8MHz ? 2 : 0) | (circuit.Clock6MHz ? 4 : 0)
        | (circuit.Clock4MHz ? 8 : 0) | (circuit.Clock3MHz ? 16 : 0)
        | (circuit.Clock2MHz ? 32 : 0) | (circuit.Clock1MHz ? 64 : 0);

    private static void Connect(SystemClockGenerator circuit, Action<int, bool> edge)
    {
        circuit.Clock12MHzChanged = high => edge(12, high); circuit.Clock8MHzChanged = high => edge(8, high);
        circuit.Clock6MHzChanged = high => edge(6, high); circuit.Clock4MHzChanged = high => edge(4, high);
        circuit.Clock3MHzChanged = high => edge(3, high); circuit.Clock2MHzChanged = high => edge(2, high);
        circuit.Clock1MHzChanged = high => edge(1, high);
    }
}

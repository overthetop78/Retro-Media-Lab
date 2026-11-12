using System.Text.Json;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Contracts;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Models;
using RetroMediaLab.Chips.Memory.Toshiba.TMM20xx.TMM2016.Services;
using Xunit.Abstractions;

namespace RetroMediaLab.Chips.Tests.Memory.Toshiba.TMM20xx.TMM2016;

[Collection("Asynchronous SRAM measurements")]
public sealed class Tmm2016Tests(ITestOutputHelper output)
{
    private static readonly Tmm2016Timing[] Profiles = [Tmm2016Timing.PD,Tmm2016Timing.PD1,Tmm2016Timing.PD2,
        Tmm2016Timing.AP90,Tmm2016Timing.AP10,Tmm2016Timing.AP12,Tmm2016Timing.AP15];
    public static IEnumerable<object[]> Grades => Profiles.Select(profile => new object[] { profile });
    [Theory]
    [MemberData(nameof(Grades))]
    public void EveryAddressStoresBothEndOfWritePathsAndKnownBitMasks(Tmm2016Timing timing)
    {
        var bus = new Bus(timing);
        Assert.Equal(0,bus.Read(0).Known);
        for (int address = 0; address < 2048; address++)
        {
            byte value = (byte)(address ^ (address >> 8)), known = (byte)(address >> 3);
            bus.Write(address,value,known,(address & 1) == 0);
            Assert.Equal(new((byte)(value & known),known,true),bus.Read(address));
        }
        Assert.True(bus.Chip.IsTimingQualified);
        var state = bus.Chip.CaptureState();
        for (int address = 0; address < 2048; address++)
        {
            byte known = (byte)(address >> 3);
            Assert.Equal(known,state.Known[address]);
            Assert.Equal((byte)((address ^ (address >> 8)) & known),state.Cells[address]);
        }
    }

    [Fact]
    public void ReadAccessOldDataHoldAndThreeStateBoundariesKeepTheirDates()
    {
        var bus = new Bus(Tmm2016Timing.AP12);
        bus.Write(0,0x12,255,true); bus.Write(1,0xa5,255,true);
        long start = bus.Time;
        var pins = new Tmm2016Pins(0,true,false,false,true,0,0);
        var edges = new List<(long,Tmm2016Output)>();
        bus.Chip.OutputChanged = value => edges.Add((bus.Chip.TimeNanoseconds,value));
        try
        {
            bus.Chip.ReceivePins(start,pins);
            bus.Chip.AdvanceTo(start + 9); Assert.False(bus.Chip.Output.Driven);
            bus.Chip.AdvanceTo(start + 10); Assert.Equal(new(0,0,true),bus.Chip.Output);
            bus.Chip.AdvanceTo(start + 120); Assert.Equal(new(0x12,255,true),bus.Chip.Output);
            bus.Chip.ReceivePins(start + 200,pins with { Address = 1 });
            bus.Chip.AdvanceTo(start + 209); Assert.Equal(new(0x12,255,true),bus.Chip.Output);
            bus.Chip.AdvanceTo(start + 210); Assert.Equal(new(0,0,true),bus.Chip.Output);
            bus.Chip.AdvanceTo(start + 320); Assert.Equal(new(0xa5,255,true),bus.Chip.Output);
            bus.Chip.ReceivePins(start + 400,pins with { Address = 1,OeHigh = true });
            bus.Chip.AdvanceTo(start + 434); Assert.True(bus.Chip.Output.Driven);
            bus.Chip.AdvanceTo(start + 435); Assert.False(bus.Chip.Output.Driven);
            bus.Chip.ReceivePins(start + 500,pins with { Address = 1 });
            bus.Chip.AdvanceTo(start + 505); Assert.Equal(new(0,0,true),bus.Chip.Output);
            bus.Chip.AdvanceTo(start + 550); Assert.Equal(new(0xa5,255,true),bus.Chip.Output);
            Assert.Equal(7,edges.Count);
            bus.Chip.ReceivePins(start + 600,pins with { Address = 1,CsHigh = true });
            bus.Chip.AdvanceTo(start + 640);
            bus.Chip.ReceivePins(start + 700,pins with { Address = 1 });
            bus.Chip.AdvanceTo(start + 710);
            Assert.Equal(new(0,0,true),bus.Chip.Output); // each CS read must wait tCO, even for the same byte
            bus.Chip.AdvanceTo(start + 820);
            Assert.Equal(new(0xa5,255,true),bus.Chip.Output);
            Assert.True(bus.Chip.IsTimingQualified);
        }
        finally { bus.Chip.OutputChanged = null; }
    }

    [Fact]
    public void CommonCsWeEdgesNeverEnableDriversAndStandbyHidesInternalReadDeadlines()
    {
        var chip = new Tmm2016(Tmm2016Timing.AP12);
        var pins = new Tmm2016Pins(3,true,true,false,true,0x69,255);
        chip.ReceivePins(0,pins);
        Assert.Null(chip.NextEventNanoseconds); // address decoding can settle without waking any receiver
        int edges = 0;
        chip.OutputChanged = _ => edges++;
        try
        {
            chip.ReceivePins(30,pins with { CsHigh = false,WeHigh = false });
            chip.AdvanceTo(200);
            Assert.False(chip.Output.Driven);
            chip.ReceivePins(210,pins); // CS and WE rise together: no transient low-Z
            chip.AdvanceTo(300);
            Assert.Equal(0,edges);
            Assert.True(chip.IsTimingQualified);
            Assert.Equal(0x69,chip.CaptureState().Cells[3]);
            chip.ReceivePins(400,pins with { CsHigh = false });
            chip.AdvanceTo(520);
            Assert.Equal(new(0x69,255,true),chip.Output);
            Assert.Equal(2,edges); // low-Z unknown, then valid data
        }
        finally { chip.OutputChanged = null; }
    }

    [Fact]
    public void ViolationsInvalidateWrittenBitsAndWeRecoveryDoesNotInventADataDeadline()
    {
        var bus = new Bus(Tmm2016Timing.PD);
        bus.Write(7,0x93,255,false);
        Assert.Equal(0,bus.Chip.Output.Known); // no specified WE-to-data maximum
        Assert.Equal(new(0x93,255,true),bus.Read(7));
        long start = bus.Time;
        var pins = new Tmm2016Pins(7,true,true,true,true,0x55,255);
        bus.Chip.ReceivePins(start,pins);
        bus.Chip.ReceivePins(start + 30,pins with { CsHigh = false });
        bus.Chip.ReceivePins(start + 60,pins with { CsHigh = false,WeHigh = false });
        bus.Chip.ReceivePins(start + 300,pins with { CsHigh = false });
        bus.Chip.ReceivePins(start + 301,pins with { CsHigh = false,Data = 0xaa });
        Assert.False(bus.Chip.IsTimingQualified);
        Assert.Equal(0,bus.Chip.CaptureState().Known[7]); // legacy grade requires15ns data hold
        var shortWrite = new Bus(Tmm2016Timing.AP12);
        start = shortWrite.Time;
        shortWrite.Chip.ReceivePins(start,pins);
        shortWrite.Chip.ReceivePins(start + 30,pins with { CsHigh = false,WeHigh = false });
        shortWrite.Chip.ReceivePins(start + 31,pins);
        Assert.False(shortWrite.Chip.IsTimingQualified);
        Assert.Equal(0,shortWrite.Chip.CaptureState().Known[7]);
    }

    [Fact]
    public void SnapshotsContinueReadWriteAndDisablePhasesWithoutCallbacksOrPartialMutation()
    {
        foreach (int phase in new[] { 0,1,2 })
        {
            var bus = new Bus(Tmm2016Timing.AP12);
            bus.Write(19,0x5a,255,true);
            long start = bus.Time;
            var pins = new Tmm2016Pins(19,true,false,false,true,0x71,255);
            bus.Chip.ReceivePins(start,pins);
            bus.Chip.AdvanceTo(start + 50);
            if (phase == 1) bus.Chip.ReceivePins(start + 60,pins with { WeHigh = false });
            if (phase == 2)
            {
                bus.Chip.AdvanceTo(start + 150);
                bus.Chip.ReceivePins(start + 160,pins with { CsHigh = true });
            }
            var state = JsonSerializer.Deserialize<Tmm2016State>(Snapshot(bus.Chip))!;
            var restored = new Tmm2016(Tmm2016Timing.AP12);
            var expected = new List<(long,Tmm2016Output)>();
            var actual = new List<(long,Tmm2016Output)>();
            bus.Chip.OutputChanged = value => expected.Add((bus.Chip.TimeNanoseconds,value));
            restored.OutputChanged = value => actual.Add((restored.TimeNanoseconds,value));
            try
            {
                restored.RestoreState(state); Assert.Empty(actual);
                foreach (var invalid in new[] { state with { Version = 1 },state with { Version = 2 },state with { Time = -1 },
                    state with { Cells = [] },state with { Pins = state.Pins with { Address = 2048 } },
                    state with { DriveTarget = !state.DriveTarget } })
                {
                    Assert.Throws<ArgumentException>(() => restored.RestoreState(invalid));
                    Assert.Equal(Snapshot(bus.Chip),Snapshot(restored));
                }
                Assert.Throws<ArgumentException>(() => new Tmm2016(Tmm2016Timing.AP10).RestoreState(state));
                if (phase == 1)
                {
                    bus.Chip.ReceivePins(start + 350,pins); restored.ReceivePins(start + 350,pins);
                }
                bus.Chip.AdvanceTo(start + 600); restored.AdvanceTo(start + 600);
                Assert.Equal(expected,actual);
                Assert.Equal(Snapshot(bus.Chip),Snapshot(restored));
                state.Cells[19] = 0;
                Assert.NotEqual(0,restored.CaptureState().Cells[19]);
            }
            finally { bus.Chip.OutputChanged = null; restored.OutputChanged = null; }
        }
    }

    [Fact]
    public void CompleteReadBusThroughputIncludesDataHoldAndDriverTransitions()
        => RetroMediaLab.Chips.Tests.Memory.Common.AsynchronousSramThroughput.Measure(Tmm2016Timing.AP12,output);

    [Fact]
    public void ImmediateResponsesAndMaximumTimestampRemainDistinctFromNoDeadline()
    {
        // Synthetic zero delays exercise the generic timing contract, not a claim
        // that a physical Toshiba or NEC part has zero propagation time.
        var instant = new Tmm2016(Tmm2016Timing.AP12 with
        { Access = 1,OeAccess = 0,CsEnable = 0,OeEnable = 0,CsDisable = 0,OeDisable = 0 });
        var initial = instant.CaptureState();
        initial.Cells[0] = 0x5a; initial.Known[0] = 255;
        instant.RestoreState(initial);
        var changes = new List<(long,Tmm2016Output)>();
        instant.OutputChanged = value => changes.Add((instant.TimeNanoseconds,value));
        var pins = new Tmm2016Pins(0,true,false,false,true,0,0);
        try
        {
            instant.ReceivePins(100,pins);
            Assert.Equal(new(0,0,true),instant.Output);
            Assert.Equal(101,instant.NextEventNanoseconds);
            instant.AdvanceTo(101);
            Assert.Equal(new(0x5a,255,true),instant.Output);
            instant.ReceivePins(102,pins with { CsHigh = true,OeHigh = true });
            Assert.Equal(default,instant.Output); Assert.Null(instant.NextEventNanoseconds);
            Assert.Equal(new long[] { 100,101,102 },changes.Select(item => item.Item1));
        }
        finally { instant.OutputChanged = null; }

        var maximum = new Tmm2016(Tmm2016Timing.AP12);
        initial = maximum.CaptureState(); initial.Cells[0] = 0xa6; initial.Known[0] = 255;
        maximum.RestoreState(initial);
        maximum.ReceivePins(long.MaxValue - 120,pins);
        maximum.AdvanceTo(long.MaxValue - 100);
        Assert.Equal(long.MaxValue,maximum.NextEventNanoseconds);
        var resumed = new Tmm2016(Tmm2016Timing.AP12);
        resumed.RestoreState(JsonSerializer.Deserialize<Tmm2016State>(Snapshot(maximum))!);
        maximum.AdvanceTo(long.MaxValue); resumed.AdvanceTo(long.MaxValue);
        Assert.Equal(new(0xa6,255,true),maximum.Output);
        Assert.Null(maximum.NextEventNanoseconds);
        Assert.Equal(Snapshot(maximum),Snapshot(resumed));
    }

    private static byte[] Snapshot(Tmm2016 chip) => JsonSerializer.SerializeToUtf8Bytes(chip.CaptureState());
    private sealed class Bus(Tmm2016Timing timing)
    {
        public Tmm2016 Chip { get; } = new(timing);
        public long Time { get; private set; } = 1000;
        public void Write(int address,byte data,byte known,bool endWithCs)
        {
            var pins = new Tmm2016Pins((ushort)address,true,true,true,true,data,known);
            Chip.ReceivePins(Time,pins);
            Chip.ReceivePins(Time + 30,pins with { CsHigh = false });
            Chip.ReceivePins(Time + 60,pins with { CsHigh = false,WeHigh = false });
            Chip.ReceivePins(Time + 400,pins with { CsHigh = endWithCs,WeHigh = !endWithCs });
            Chip.ReceivePins(Time + 450,pins);
            Chip.AdvanceTo(Time + 500); Time += 600;
        }
        public Tmm2016Output Read(int address)
        {
            var pins = new Tmm2016Pins((ushort)address,true,true,true,true,0,0);
            Chip.ReceivePins(Time,pins);
            Chip.ReceivePins(Time + 30,pins with { CsHigh = false,OeHigh = false });
            Chip.AdvanceTo(Time + 250); var data = Chip.Output;
            Chip.ReceivePins(Time + 270,pins); Chip.AdvanceTo(Time + 340);
            Time += 600; return data;
        }
    }
}

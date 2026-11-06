using System.Text.Json;
using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Contracts;
using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;
using C = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Constants.Via6522Constants;

namespace RetroMediaLab.Tests.Chips.IO.MOS.MCS6500.Chip6522;

public sealed class StateTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryShiftModeAndPhi2PhaseContinuesWithIdenticalInterruptsAndPinChanges(bool pulseCounter)
    {
        for (var mode = 0; mode < 8; mode++)
        for (var offset = 0; offset < 24; offset++)
        {
            var trace = new List<string>();
            var via = Create(trace);
            via.Write(C.Auxiliary, (byte)(0xc0 | mode << 2 | (pulseCounter ? 0x20 : 0)));
            via.Write(C.Enable, 0xff);
            via.Write(C.Timer1Low, 5);
            via.Write(C.Timer1High, 0);
            via.Write(C.Timer2Low, 3);
            via.Write(C.Timer2High, 0);
            via.Write(C.Shift, 0xa5);
            for (var phase = 0; phase < offset; phase++) Advance(via, phase);
            var snapshot = JsonSerializer.Deserialize<Via6522State>(JsonSerializer.SerializeToUtf8Bytes(via.CaptureState()))!;
            var continuedTrace = new List<string>();
            var continued = Create(continuedTrace);
            continued.RestoreState(snapshot);
            Assert.Empty(continuedTrace);
            trace.Clear();
            for (var phase = offset; phase < offset + 80; phase++)
            {
                Advance(via, phase);
                Advance(continued, phase);
                Assert.Equal(JsonSerializer.SerializeToUtf8Bytes(via.CaptureState()), JsonSerializer.SerializeToUtf8Bytes(continued.CaptureState()));
            }
            Assert.Equal(trace, continuedTrace);
        }
    }

    [Fact]
    public void RegisterCopiesAreIndependentAndMalformedStatesDoNotChangeTheChip()
    {
        var via = Create([]);
        via.Write(C.DirectionA, 0xa5);
        var snapshot = via.CaptureState();
        snapshot.Registers[C.DirectionA] = 0;
        Assert.Equal(0xa5, via.Read(C.DirectionA));
        var correct = via.CaptureState();
        Assert.Throws<ArgumentException>(() => via.RestoreState(correct with { Registers = [] }));
        Assert.Equal(JsonSerializer.SerializeToUtf8Bytes(correct), JsonSerializer.SerializeToUtf8Bytes(via.CaptureState()));
        via.RestoreState(correct);
        correct.Registers[C.DirectionA] = 0;
        Assert.Equal(0xa5, via.Read(C.DirectionA));
    }

    private static Via6522 Create(List<string> trace) => new()
    {
        ReadPortA = () => 0xd3,
        ReadPortB = () => 0x4f,
        IrqChanged = value => trace.Add("IRQ:" + value),
        Ca2PinsChanged = value => trace.Add("CA2:" + value),
        Cb1PinsChanged = value => trace.Add("CB1:" + value),
        Cb2PinsChanged = value => trace.Add("CB2:" + value),
        PortAPinsChanged = value => trace.Add("PA:" + value),
        PortBPinsChanged = value => trace.Add("PB:" + value),
        ControlOutputsChanged = value => trace.Add("OWN:" + value)
    };

    private static void Advance(Via6522 via, int phase)
    {
        via.SetPb6(phase % 3 == 0);
        via.SetCa1(phase % 5 == 0);
        via.SetCa2(phase % 7 == 0);
        via.SetCb1(phase % 4 == 0);
        via.SetCb2(phase % 3 != 0);
        if ((phase & 1) == 0) via.Phi2TrailingEdge();
        else via.Phi2LeadingEdge();
        if (phase % 17 == 0) via.Read(C.PortA);
    }
}

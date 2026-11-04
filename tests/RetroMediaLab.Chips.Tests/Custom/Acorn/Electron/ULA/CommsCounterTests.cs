using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

namespace RetroMediaLab.Chips.Tests.Custom.Acorn.Electron.ULA;

public sealed class CommsCounterTests
{
    // Deliberately step every 16 MHz clock, independently of the batched implementation.
    private sealed class Reference
    {
        internal int Value;
        internal byte Divider;
        internal bool Sound;
        internal bool ReceivePhase;
        internal bool Tick(int mode)
        {
            var frame = false;
            ReceivePhase = false;
            for (var point = 0; point < 8; point++)
            {
                if (mode is 1 or 2) ReceivePhase |= Value % 4096 == 0;
                if (mode == 1)
                {
                    frame |= Value % 16384 == 0;
                    if (Value == 0)
                    {
                        Value = 256 * (Divider + 1) - 1;
                        Sound = !Sound;
                    }
                    else Value--;
                }
                else if (mode == 2)
                {
                    if (Value % 4096 == 0)
                    {
                        frame |= Value % 16384 == 0;
                        Value = (Value - 769) & 65535;
                    }
                    else Value--;
                }
                else if (mode == 0) Value = Math.Min(Value + 1, 65535);
            }
            return frame;
        }
    }

    [Fact]
    public void EverySoundDividerMatchesTheMasterClockOracle()
    {
        var actual = new ElectronCommsCounter();
        var expected = new Reference();
        for (var divider = 0; divider < 256; divider++)
        {
            actual.Reset();
            actual.WriteDivider((byte)divider);
            expected.Value = 0;
            expected.Divider = (byte)divider;
            expected.Sound = false;
            for (var clock = 0; clock < 64 * (divider + 1) + 1; clock++)
            {
                Assert.Equal(expected.Tick(1), actual.TickSound());
                Assert.Equal(expected.Value, actual.Value);
                Assert.Equal(expected.ReceivePhase, actual.ReceiveClockEdge);
                Assert.Equal(expected.Sound, actual.SoundLevel);
            }
        }
    }

    [Fact]
    public void OutputWavesAndFrameEdgesMatchTheMasterClockOracle()
    {
        var actual = new ElectronCommsCounter();
        var expected = new Reference();
        for (var clock = 0; clock < 32 * 1664; clock++)
        {
            Assert.Equal(expected.Tick(2), actual.TickOutput());
            Assert.Equal(expected.ReceivePhase, actual.ReceiveClockEdge);
            Assert.Equal(expected.Value, actual.Value);
            Assert.Equal((expected.Value & 4096) != 0, actual.MarkLevel);
            Assert.Equal((expected.Value & 8192) != 0, actual.SpaceLevel);
        }
    }

    [Fact]
    public void ModeChangesUseTheSameCounterAndDividerWritesDoNotResetIt()
    {
        var actual = new ElectronCommsCounter();
        var expected = new Reference();
        for (var cycle = 0; cycle < 256; cycle++)
        {
            actual.WriteDivider((byte)cycle);
            expected.Divider = (byte)cycle;
            Assert.Equal(expected.Value, actual.Value);
            foreach (var mode in new[] { 2, 1, 3, 0, 1, 2, 0, 3 })
            for (var clock = 0; clock < 257; clock++)
            {
                var frame = false;
                if (mode == 0) actual.TickInput();
                else if (mode == 1) frame = actual.TickSound();
                else if (mode == 2) frame = actual.TickOutput();
                Assert.Equal(expected.Tick(mode), frame);
                Assert.Equal(expected.Value, actual.Value);
                Assert.Equal(expected.Sound, actual.SoundLevel);
            }
            Assert.Equal(expected.Value / 8, actual.TakeInputPeriod());
            expected.Value = 0;
            Assert.Equal(0, actual.Value);
        }
    }

    [Fact]
    public void LongInputLevelsSaturateInsteadOfAliasingAValidHalfWave()
    {
        var counter = new ElectronCommsCounter();
        for (var clock = 0; clock < 100000; clock++) counter.TickInput();
        Assert.Equal(65535, counter.Value);
        Assert.Equal(8191, counter.TakeInputPeriod());
        Assert.Equal(0, counter.Value);
    }

    [Fact]
    public void QuietInputPhasesContinueAfterTheDurationHasSaturatedAndAnEdgeRealignsThem()
    {
        var counter = new ElectronCommsCounter();
        for (var clock = 1; clock <= 100000; clock++)
            Assert.Equal(clock > 832 && clock % 416 == 0, counter.TickInput());
        Assert.Equal(8191, counter.TakeInputPeriod());
        for (var clock = 1; clock <= 1248; clock++)
            Assert.Equal(clock == 1248, counter.TickInput());
    }
}

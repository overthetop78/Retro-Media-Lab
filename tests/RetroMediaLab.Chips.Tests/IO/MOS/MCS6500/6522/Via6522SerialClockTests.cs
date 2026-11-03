using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Enums;
using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;
using C = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Constants.Via6522Constants;

namespace RetroMediaLab.Chips.Tests.IO.MOS.MCS6500.Chip6522;

public sealed class Via6522SerialClockTests
{
    [Theory]
    [InlineData(ViaShiftMode.InputTimer2, false)]
    [InlineData(ViaShiftMode.InputTimer2, true)]
    [InlineData(ViaShiftMode.InputSystemClock, false)]
    [InlineData(ViaShiftMode.InputSystemClock, true)]
    [InlineData(ViaShiftMode.OutputTimer2, false)]
    [InlineData(ViaShiftMode.OutputTimer2, true)]
    [InlineData(ViaShiftMode.OutputSystemClock, false)]
    [InlineData(ViaShiftMode.OutputSystemClock, true)]
    public void AccessingAnActiveInternalShifterRearmsEightBitsWithoutRestartingItsClock(ViaShiftMode mode, bool read)
    {
        for (var split = 1; split < C.ShiftBitCount; split++)
        foreach (var lowClock in new[] { false, true })
        {
            var via = ActiveShifter(mode);
            var reference = ActiveShifter(mode);
            for (var bit = 0; bit < split; bit++)
            {
                WaitForClock(via, false);
                WaitForClock(via, true);
                WaitForClock(reference, false);
                WaitForClock(reference, true);
            }
            if (lowClock)
            {
                WaitForClock(via, false);
                WaitForClock(reference, false);
            }
            var before = via.Cb1;
            var timer = via.Read(C.Timer2Low) | via.Read(C.Timer2High) << C.HighByteShift;
            var edges = new List<bool>();
            via.Cb1PinsChanged = edges.Add;
            if (read) via.Read(C.Shift);
            else via.Write(C.Shift, byte.MaxValue);
            Assert.Equal(before, via.Cb1);
            Assert.Empty(edges);
            Assert.Equal(timer, via.Read(C.Timer2Low) | via.Read(C.Timer2High) << C.HighByteShift);
            for (var cycle = 0; cycle < 32; cycle++)
            {
                via.Tick();
                reference.Tick();
                Assert.Equal(reference.Cb1, via.Cb1);
                if (via.Cb1 != before) break;
            }
            Assert.NotEqual(before, via.Cb1);
            var complete = false;
            for (var cycle = 0; cycle < 128 && !complete; cycle++)
            {
                via.Tick();
                complete = (via.Read(C.Flags) & C.ShiftInterrupt) != 0;
            }
            Assert.True(complete);
            var output = mode is ViaShiftMode.OutputTimer2 or ViaShiftMode.OutputSystemClock;
            Assert.Equal(C.ShiftBitCount, edges.Count(level => output ? !level : level));
            Assert.Equal(byte.MaxValue, via.Read(C.Shift));
        }
    }

    private static Via6522 ActiveShifter(ViaShiftMode mode)
    {
        var via = new Via6522();
        Mode(via, mode);
        via.SetCb2(true);
        via.Write(C.Timer2Low, 3);
        via.Write(C.Shift, byte.MaxValue);
        return via;
    }

    [Theory]
    [InlineData(ViaShiftMode.InputTimer2)]
    [InlineData(ViaShiftMode.InputSystemClock)]
    public void InternalInputCapturesDataBeforeTheFollowingShiftPhase(ViaShiftMode mode)
    {
        var via = new Via6522();
        Mode(via, mode);
        via.Write(C.Timer2Low, 0);
        via.Write(C.Shift, 0);
        const byte value = 0xa6;
        for (var bit = C.ShiftBitCount - 1; bit >= 0; bit--)
        {
            var level = (value & (1 << bit)) != 0;
            via.SetCb2(level);
            var previousClock = via.Cb1;
            var captured = false;
            for (var cycle = 0; cycle < 16 && !captured; cycle++)
            {
                via.Tick();
                captured = !previousClock && via.Cb1;
                previousClock = via.Cb1;
            }
            Assert.True(captured);
            // Le dispositif extérieur peut changer CB2 après le front de capture.
            via.SetCb2(!level);
            via.Phi2TrailingEdge();
            via.Phi2LeadingEdge();
            Assert.Equal(bit == 0, (via.Read(C.Flags) & C.ShiftInterrupt) != 0);
        }
        Assert.Equal(value, via.Read(C.Shift));
    }

    [Fact]
    public void StaticModeReceivesEveryByteWithoutAnSrAccessOrAnInterrupt()
    {
        var via = new Via6522();
        via.Write(C.Enable, C.InterruptSummary | C.ShiftInterrupt);
        for (var value = 0; value <= byte.MaxValue; value++)
        {
            for (var bit = C.ShiftBitCount - 1; bit >= 0; bit--)
            {
                via.SetCb1(false);
                via.SetCb2((value & (1 << bit)) != 0);
                via.SetCb1(true);
            }
            Assert.Equal(value, via.Read(C.Shift));
            Assert.Equal(0, via.Read(C.Flags) & C.ShiftInterrupt);
            Assert.False(via.Irq);
            Assert.Equal(ViaControlOutputs.None, via.ControlOutputs);
        }
    }

    [Fact]
    public void StaticModeShiftsOnlyOnPositiveExternalEdgesAndReadsDoNotArmTheClock()
    {
        var via = new Via6522();
        via.Write(C.Shift, 0);
        via.SetCb2(true);
        for (var bit = 0; bit < C.ShiftBitCount; bit++)
        {
            via.SetCb1(true);
            var expected = (1 << (bit + 1)) - 1;
            Assert.Equal(expected, via.Read(C.Shift));
            via.SetCb1(true);
            via.SetCb1(false);
            for (var cycle = 0; cycle < 4; cycle++) via.Tick();
            Assert.Equal(expected, via.Read(C.Shift));
            Assert.Equal(0, via.Read(C.Flags) & C.ShiftInterrupt);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DisablingAroundTheLastRisingEdgePreservesCompletionUntilTheClockIsReenabled(bool afterTrailing)
    {
        var via = LastOutputBit();
        if (afterTrailing) via.Phi2TrailingEdge();
        Mode(via, ViaShiftMode.Disabled);
        for (var cycle = 0; cycle < 4; cycle++) via.Tick();
        Assert.Equal(0, via.Read(C.Flags) & C.ShiftInterrupt);
        Mode(via, ViaShiftMode.OutputSystemClock);
        via.Tick();
        Assert.NotEqual(0, via.Read(C.Flags) & C.ShiftInterrupt);
        Assert.Equal(1, via.Read(C.Shift));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AccessingSrBeforeTheLastClockIsPublishedCancelsThePreviousCompletion(bool read)
    {
        var via = LastOutputBit();
        via.Phi2TrailingEdge();
        if (read) Assert.Equal(1, via.Read(C.Shift));
        else via.Write(C.Shift, 0x80);
        via.Phi2LeadingEdge();
        Assert.Equal(0, via.Read(C.Flags) & C.ShiftInterrupt);
        for (var cycle = 0; cycle < 18; cycle++) via.Tick();
        Assert.NotEqual(0, via.Read(C.Flags) & C.ShiftInterrupt);
        Assert.Equal(read ? 1 : 0x80, via.Read(C.Shift));
    }

    [Fact]
    public void AWriteFromTheLastClockCallbackStartsTheNextByteWithoutAnOldCompletionInterrupt()
    {
        var via = LastOutputBit();
        var restarted = false;
        via.Cb1PinsChanged = level =>
        {
            if (!level || restarted) return;
            restarted = true;
            via.Write(C.Shift, 0x80);
        };
        via.Tick();
        Assert.True(restarted);
        Assert.Equal(0, via.Read(C.Flags) & C.ShiftInterrupt);
        for (var cycle = 0; cycle < 18; cycle++) via.Tick();
        Assert.NotEqual(0, via.Read(C.Flags) & C.ShiftInterrupt);
        Assert.Equal(0x80, via.Read(C.Shift));
    }

    private static Via6522 LastOutputBit()
    {
        var via = new Via6522();
        Mode(via, ViaShiftMode.OutputSystemClock);
        via.Write(C.Shift, 1);
        for (var cycle = 0; cycle < 17; cycle++) via.Tick();
        Assert.Equal(0, via.Read(C.Flags) & C.ShiftInterrupt);
        Assert.False(via.Cb1);
        return via;
    }

    [Fact]
    public void ChangingTheT2LatchDoesNotReloadTheClockCounterDuringASerialSourceChange()
    {
        var via = new Via6522();
        Mode(via, ViaShiftMode.OutputSystemClock);
        via.Write(C.Timer2Low, byte.MaxValue);
        via.Write(C.Timer2High, 0);
        via.Write(C.Shift, 0xa6);
        for (var bit = 0; bit < 3; bit++) SendBit(via, ViaShiftMode.OutputSystemClock);
        var counter = via.Read(C.Timer2Low);
        via.Write(C.Timer2Low, 0);
        Mode(via, ViaShiftMode.OutputTimer2);
        for (var cycle = 0; cycle < 8; cycle++)
        {
            via.Tick();
            Assert.True(via.Cb1);
        }
        Assert.Equal(counter - 8, via.Read(C.Timer2Low));
        Assert.Equal(0, via.Read(C.Flags) & C.ShiftInterrupt);

        via.Write(C.Timer2High, 0);
        for (var bit = 0; bit < 5; bit++) SendBit(via, ViaShiftMode.OutputTimer2);
        Assert.NotEqual(0, via.Read(C.Flags) & C.ShiftInterrupt);
        Assert.Equal(0xa6, via.Read(C.Shift));
    }

    [Fact]
    public void InputClockChangesPreserveEveryPartialByteAndTheModuloEightCounter()
    {
        ViaShiftMode[] modes = [ViaShiftMode.InputTimer2, ViaShiftMode.InputSystemClock, ViaShiftMode.InputExternalClock];
        foreach (var first in modes)
        foreach (var second in modes)
        for (var split = 1; split < C.ShiftBitCount; split++)
        foreach (var value in new byte[] { 0xa6, 0x59 })
        {
            var via = new Via6522();
            Mode(via, first);
            via.Write(C.Timer2Low, 0);
            via.Write(C.Shift, 0);
            for (var bit = 7; bit >= 0; bit--)
            {
                if (bit == C.ShiftBitCount - split - 1) Mode(via, second);
                ReceiveBit(via, bit >= C.ShiftBitCount - split ? first : second, (value & (1 << bit)) != 0);
                if (bit != 0) Assert.Equal(0, via.Read(C.Flags) & C.ShiftInterrupt);
            }
            if (second != ViaShiftMode.InputExternalClock) via.Tick();
            Assert.NotEqual(0, via.Read(C.Flags) & C.ShiftInterrupt);
            Assert.Equal(value, via.Read(C.Shift));
        }
    }

    [Fact]
    public void OutputClockChangesPreserveTheByteAndItsCompletionPoint()
    {
        ViaShiftMode[] modes = [ViaShiftMode.OutputTimer2, ViaShiftMode.OutputSystemClock, ViaShiftMode.OutputExternalClock];
        foreach (var first in modes)
        foreach (var second in modes)
        for (var split = 1; split < C.ShiftBitCount; split++)
        foreach (var value in new byte[] { 0xa6, 0x59 })
        {
            var via = new Via6522();
            Mode(via, first);
            via.Write(C.Timer2Low, 0);
            via.Write(C.Shift, value);
            var actual = 0;
            for (var bit = 7; bit >= 0; bit--)
            {
                if (bit == C.ShiftBitCount - split - 1) Mode(via, second);
                actual = actual << 1 | (SendBit(via, bit >= C.ShiftBitCount - split ? first : second) ? 1 : 0);
                Assert.Equal(bit == 0, (via.Read(C.Flags) & C.ShiftInterrupt) != 0);
            }
            Assert.Equal(value, actual);
        }
    }

    [Fact]
    public void DisablingSerialInputPausesItsInterruptCounterButNotStaticShifting()
    {
        var via = new Via6522();
        Mode(via, ViaShiftMode.InputExternalClock);
        via.Write(C.Shift, 0);
        for (var bit = 7; bit >= 4; bit--) ReceiveBit(via, ViaShiftMode.InputExternalClock, (0xa6 & (1 << bit)) != 0);
        Mode(via, ViaShiftMode.Disabled);
        for (var bit = 0; bit < 4; bit++) ReceiveBit(via, ViaShiftMode.InputExternalClock, true);
        Assert.Equal(0, via.Read(C.Flags) & C.ShiftInterrupt);
        // Les quatre fronts en mode statique modifient SR sans avancer le compteur.
        Mode(via, ViaShiftMode.InputExternalClock);
        for (var bit = 3; bit >= 0; bit--) ReceiveBit(via, ViaShiftMode.InputExternalClock, (0xa6 & (1 << bit)) != 0);
        Assert.NotEqual(0, via.Read(C.Flags) & C.ShiftInterrupt);
        Assert.Equal(0xf6, via.Read(C.Shift));
    }

    [Fact]
    public void FreeRunningOutputDoesNotAdvanceTheSerialInterruptCounter()
    {
        var via = new Via6522();
        Mode(via, ViaShiftMode.OutputExternalClock);
        via.Write(C.Timer2Low, 0);
        via.Write(C.Shift, 0xa6);
        for (var bit = 0; bit < 3; bit++) SendBit(via, ViaShiftMode.OutputExternalClock);
        Mode(via, ViaShiftMode.OutputTimer2Continuous);
        for (var bit = 0; bit < 5; bit++) SendBit(via, ViaShiftMode.OutputTimer2Continuous);
        Assert.Equal(0, via.Read(C.Flags) & C.ShiftInterrupt);
        Mode(via, ViaShiftMode.OutputExternalClock);
        for (var bit = 0; bit < 5; bit++)
        {
            SendBit(via, ViaShiftMode.OutputExternalClock);
            Assert.Equal(bit == 4, (via.Read(C.Flags) & C.ShiftInterrupt) != 0);
        }
    }

    private static void Mode(Via6522 via, ViaShiftMode mode) => via.Write(C.Auxiliary, (byte)((int)mode << C.ShiftModeShift));

    private static void ReceiveBit(Via6522 via, ViaShiftMode mode, bool data)
    {
        via.SetCb2(data);
        if (mode == ViaShiftMode.InputExternalClock)
        {
            via.SetCb1(false);
            via.SetCb1(true);
            via.Tick();
        }
        else
        {
            if (via.Cb1) WaitForClock(via, false);
            WaitForClock(via, true);
            via.Tick(); // Maintien de la donnée jusqu'à son échantillonnage.
        }
    }

    private static bool SendBit(Via6522 via, ViaShiftMode mode)
    {
        if (mode == ViaShiftMode.OutputExternalClock)
        {
            via.SetCb1(true);
            via.SetCb1(false);
            var bit = via.Cb2;
            via.SetCb1(true);
            return bit;
        }
        WaitForClock(via, false);
        var outgoing = via.Cb2;
        WaitForClock(via, true);
        return outgoing;
    }

    private static void WaitForClock(Via6522 via, bool level)
    {
        var ticks = 0;
        do { via.Tick(); ticks++; } while (via.Cb1 != level && ticks < 100);
        Assert.InRange(ticks, 1, 99);
        Assert.Equal(level, via.Cb1);
    }
}

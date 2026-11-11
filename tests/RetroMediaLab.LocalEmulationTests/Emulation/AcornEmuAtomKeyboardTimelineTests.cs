using System.Reflection;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;
using RetroMediaLab.Emulation.Acorn.Common.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornAtom.Common.Services;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuAtomKeyboardTimelineTests
{
    [Fact]
    public void BothEdgesOfShortPressesSurviveOneFrameAndKeepTheirDurations()
    {
        var clock = new Clock();
        var history = new KeyboardInputHistory(clock);
        clock.Now = 1000; history.Update(Keys(EmulationKey.A), true);
        clock.Now = 4000; history.Update(Keys(), true);
        clock.Now = 5000; history.Update(Keys(EmulationKey.B), true);
        clock.Now = 8000; history.Update(Keys(), true);
        clock.Now = 16000;
        var frame = history.Consume();
        Assert.Equal(new long[] { 1000, 4000, 5000, 8000 }, frame.Changes.Select(x => x.Timestamp));
        var seen = new List<string>();
        long? next = null;
        var playback = new KeyboardInputPlayback(1_000_000, state => seen.Add(Name(state)), value => next = value);
        playback.Append(frame, 200, 16_689);
        Assert.Equal(new[] { "" }, seen);
        Assert.Equal(17_889, next);
        playback.Deliver(17_888); Assert.Single(seen);
        playback.Deliver(17_889); Assert.Equal("A", seen[^1]);
        playback.Deliver(20_888); Assert.Equal("A", seen[^1]);
        playback.Deliver(20_889); Assert.Equal("", seen[^1]);
        playback.Deliver(21_889); Assert.Equal("B", seen[^1]);
        playback.Deliver(24_889); Assert.Equal("", seen[^1]);
        Assert.Equal(new[] { "", "A", "", "B", "" }, seen);
        Assert.Null(next);
    }

    [Fact]
    public void SourceClockKeepsItsEpochAcrossFrameBoundaries()
    {
        var clock = new Clock();
        var history = new KeyboardInputHistory(clock);
        var seen = new List<string>();
        long? next = null;
        var playback = new KeyboardInputPlayback(1_000_000, state => seen.Add(Name(state)), value => next = value);
        clock.Now = 16000; playback.Append(history.Consume(), 0, 16_689);
        clock.Now = 20000; history.Update(Keys(EmulationKey.C), true);
        clock.Now = 24000; history.Update(Keys(), true);
        clock.Now = 32000; playback.Append(history.Consume(), 16_688, 16_689);
        Assert.Equal(36_689, next);
        playback.Deliver(36_688); Assert.Equal(new[] { "" }, seen);
        playback.Deliver(36_689); Assert.Equal("C", seen[^1]);
        playback.Deliver(40_689); Assert.Equal("", seen[^1]);
    }

    [Fact]
    public void HostCollectionsAreCopiedBeforeTheirOwnerChangesThem()
    {
        var clock = new Clock();
        var history = new KeyboardInputHistory(clock);
        var keys = new HashSet<EmulationKey> { EmulationKey.A };
        var characters = new Dictionary<EmulationKey, char> { [EmulationKey.A] = 'A' };
        history.Update(Keys() with { Keys = keys, HostCharacters = characters }, true);
        keys.Clear(); characters.Clear();
        var change = Assert.Single(history.Consume().Changes);
        Assert.Contains(EmulationKey.A, change.Snapshot.Keys);
        Assert.Equal('A', change.Snapshot.HostCharacters![EmulationKey.A]);
    }

    [Fact]
    public void RepeatedSnapshotsDoNotGenerateAdditionalContactChanges()
    {
        var history = new KeyboardInputHistory(new Clock());
        for (var i = 0; i < 20; i++) history.Update(Keys(EmulationKey.A), true);
        Assert.Single(history.Consume().Changes);
        history.Update(Keys(EmulationKey.A), true);
        Assert.Empty(history.Consume().Changes);
    }

    [Fact]
    public void PauseKeepsTheCurrentContactsWithoutReplayingPausedTyping()
    {
        var clock = new Clock();
        var history = new KeyboardInputHistory(clock);
        history.Update(Keys(EmulationKey.A), true);
        history.Reset();
        clock.Now = 1_000_000; history.Update(Keys(EmulationKey.B), false);
        clock.Now = 2_000_000; history.Update(Keys(), false);
        var frame = history.Consume();
        Assert.True(frame.Restart);
        Assert.Empty(frame.Initial.Keys);
        Assert.Empty(frame.Changes);
    }

    [Fact]
    public void ResetDropsFutureEdgesAndTheOldClockEpoch()
    {
        var seen = new List<string>();
        long? next = null;
        var playback = new KeyboardInputPlayback(1_000_000, state => seen.Add(Name(state)), value => next = value);
        playback.Append(new(0, 20, 1_000_000, Keys(), [new(10, Keys(EmulationKey.A))], true), 0, 10);
        playback.Reset();
        Assert.Null(next);
        playback.Deliver(100); Assert.Equal(new[] { "" }, seen);
        playback.Append(new(500, 520, 1_000_000, Keys(), [new(510, Keys(EmulationKey.B))], false), 1000, 10);
        Assert.Equal(1020, next);
        playback.Deliver(1020); Assert.Equal("B", seen[^1]);
        Assert.DoesNotContain("A", seen);
    }

    [Fact]
    public void SameTimestampKeepsHostOrderAndSubcycleDatesRoundUp()
    {
        var seen = new List<string>();
        var playback = new KeyboardInputPlayback(2, state => seen.Add(Name(state)), _ => { });
        playback.Append(new(0, 3, 3, Keys(), [new(1, Keys(EmulationKey.A)), new(1, Keys()), new(2, Keys(EmulationKey.B))], true), 0, 0);
        playback.Deliver(0); Assert.Equal(new[] { "" }, seen);
        playback.Deliver(1); Assert.Equal(new[] { "", "A", "" }, seen);
        playback.Deliver(2); Assert.Equal("B", seen[^1]);
    }

    [Fact]
    public void AtomContactsChangeOnTheScheduledCpuCycles()
    {
        var rom = Enumerable.Repeat((byte)0xea, 8192).ToArray();
        rom[^4] = 0; rom[^3] = 0xf0;
        var board = new AtomMachine(rom, new byte[280], 1024, 1024);
        var cpu = Component<Cpu6502>(board, "_cpu");
        var keyboard = Component<AtomKeyboard>(board, "_keyboard");
        var observed = new List<(long Cycle, byte Pins)>();
        KeyboardInputPlayback? playback = null;
        try
        {
            keyboard.Select(6);
            playback = new(1_000_000, state =>
            {
                board.SetInput(state);
                observed.Add((cpu.Cycles, keyboard.Read()));
            }, next => board.SetKeyboardInputDeadline(next, value => playback!.Deliver(value)));
            var start = cpu.Cycles;
            playback.Append(new(0, 10, 1_000_000, Keys(), [new(2, Keys(EmulationKey.A)), new(5, Keys())], true), start, 0);
            for (var i = 0; i < 12; i++) cpu.Clock();
            Assert.Equal(new[] { (start, (byte)0xff), (start + 2, (byte)0xf7), (start + 5, (byte)0xff) }, observed);
        }
        finally
        {
            playback?.Reset();
            board.SetKeyboardInputDeadline(null, null);
            board.Reset(true);
        }
    }

    private static T Component<T>(object owner, string field) => (T)owner.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(owner)!;
    private static EmulationInputSnapshot Keys(params EmulationKey[] keys) => EmulationInputSnapshot.Empty with { Keys = keys.ToHashSet() };
    private static string Name(EmulationInputSnapshot state) => string.Join(',', state.Keys.Order());
    private sealed class Clock : TimeProvider
    {
        internal long Now;
        public override long TimestampFrequency => 1_000_000;
        public override long GetTimestamp() => Now;
    }
}

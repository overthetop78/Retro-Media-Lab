using System.Collections.Frozen;

namespace RetroMediaLab.Emulation.Acorn.Common.Services;

internal sealed record KeyboardInputChange(long Timestamp, EmulationInputSnapshot Snapshot);
internal sealed record KeyboardInputFrame(long Start, long End, long Frequency,
    EmulationInputSnapshot Initial, IReadOnlyList<KeyboardInputChange> Changes, bool Restart);

/// <summary>Transport hôte des changements de contacts, indépendant du clavier émulé.</summary>
internal sealed class KeyboardInputHistory(TimeProvider? time = null)
{
    private readonly object gate = new();
    private readonly TimeProvider clock = time ?? TimeProvider.System;
    private readonly List<KeyboardInputChange> changes = [];
    private EmulationInputSnapshot initial = EmulationInputSnapshot.Empty;
    private EmulationInputSnapshot current = EmulationInputSnapshot.Empty;
    private long start = (time ?? TimeProvider.System).GetTimestamp();
    private bool restart = true;

    internal void Update(EmulationInputSnapshot snapshot, bool running)
    {
        lock (gate)
        {
            if (!running) { Reset(snapshot); return; }
            if (SameKeys(current, snapshot)) return;
            current = Copy(snapshot);
            changes.Add(new(clock.GetTimestamp(), current));
        }
    }

    internal void Reset(EmulationInputSnapshot? snapshot = null)
    {
        lock (gate)
        {
            if (snapshot is not null) current = Copy(snapshot);
            initial = current;
            changes.Clear();
            start = clock.GetTimestamp();
            restart = true;
        }
    }

    internal KeyboardInputFrame Consume()
    {
        lock (gate)
        {
            var end = clock.GetTimestamp();
            var result = new KeyboardInputFrame(start, end, clock.TimestampFrequency, initial, changes.ToArray(), restart);
            start = end; initial = current; changes.Clear(); restart = false;
            return result;
        }
    }

    private static bool SameKeys(EmulationInputSnapshot left, EmulationInputSnapshot right)
        => left.Keys.SetEquals(right.Keys) && (ReferenceEquals(left.HostCharacters, right.HostCharacters)
            || left.HostCharacters is { } a && right.HostCharacters is { } b && a.Count == b.Count
                && a.All(pair => b.TryGetValue(pair.Key, out var value) && value == pair.Value));

    private static EmulationInputSnapshot Copy(EmulationInputSnapshot source) => EmulationInputSnapshot.Empty with
    {
        Keys = source.Keys.ToFrozenSet(),
        HostCharacters = source.HostCharacters?.ToFrozenDictionary()
    };
}

/// <summary>Dates hôtes converties en cycles, sans modifier les durées des contacts.</summary>
internal sealed class KeyboardInputPlayback(long frequency, Action<EmulationInputSnapshot> apply, Action<long?> deadline)
{
    private readonly Queue<(long Cycle, EmulationInputSnapshot Snapshot)> pending = new();
    private long originTimestamp;
    private long originCycle;
    private bool initialized;

    internal void Append(KeyboardInputFrame frame, long currentCycle, long inputLatencyCycles)
    {
        if (!initialized || frame.Restart)
        {
            Reset();
            originTimestamp = frame.Start;
            originCycle = checked(currentCycle + inputLatencyCycles);
            initialized = true;
            apply(frame.Initial);
        }
        foreach (var change in frame.Changes)
        {
            var elapsed = checked(change.Timestamp - originTimestamp);
            var cycles = checked((long)(((Int128)elapsed * frequency + frame.Frequency - 1) / frame.Frequency));
            pending.Enqueue((checked(originCycle + cycles), change.Snapshot));
        }
        Announce();
    }

    internal void Deliver(long cycle)
    {
        while (pending.TryPeek(out var next) && next.Cycle <= cycle)
        {
            pending.Dequeue();
            // Publier l'échéance suivante avant le destinataire : BREAK peut effectuer un reset réentrant.
            Announce();
            apply(next.Snapshot);
        }
    }

    internal void Reset()
    {
        pending.Clear(); initialized = false;
        deadline(null);
    }

    private void Announce() => deadline(pending.TryPeek(out var next) ? next.Cycle : null);
}

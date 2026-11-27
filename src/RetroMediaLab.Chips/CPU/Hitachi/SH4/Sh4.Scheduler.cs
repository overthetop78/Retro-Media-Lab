using System.IO;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed partial class Sh4Scheduler(Sh4Context context)
{
    private const int SaveVersion19 = 814;
    private const int SaveVersion31 = 826;
    internal const int SaveVersion58 = 853;

    private record struct Entry(Sh4SchedulerCallback? Callback, nint Argument, int Tag, int Start, int End);

    private readonly List<Entry> _entries = [];
    private ulong _futureBase;
    private int _nextId = -1;

    private static uint Remaining(Entry entry, uint reference)
        => entry.End == -1 ? uint.MaxValue : unchecked((uint)entry.End - reference);

    internal void RecomputeNext()
    {
        uint difference = uint.MaxValue;
        int slot = -1;
        uint now = Now32();
        for (int id = 0; id < _entries.Count; id++)
        {
            uint remaining = Remaining(_entries[id], now);
            if (remaining < difference)
            {
                difference = remaining;
                slot = id;
            }
        }

        _futureBase = unchecked(_futureBase - (ulong)context.SchedulerNext);
        _nextId = slot;
        if (slot != -1)
            context.SchedulerNext = unchecked((int)difference);
        else
            context.SchedulerNext = MainClock;
        _futureBase = unchecked(_futureBase + (ulong)context.SchedulerNext);
    }

    internal int Register(int tag, Sh4SchedulerCallback callback, nint argument = 0)
    {
        var entry = new Entry(callback, argument, tag, -1, -1);
        for (int id = 0; id < _entries.Count; id++)
        {
            if (_entries[id].Callback is null)
            {
                _entries[id] = entry;
                return id;
            }
        }

        _entries.Add(entry);
        return _entries.Count - 1;
    }

    internal void Unregister(int id)
    {
        if (id == -1)
            return;

        Sh4HostRuntime.Verify(id < _entries.Count, "id < (int)sch_list.size()");
        if (id == _entries.Count - 1)
            _entries.RemoveAt(id);
        else
        {
            ref Entry entry = ref CollectionsMarshal.AsSpan(_entries)[id];
            entry.Callback = null;
            entry.End = -1;
        }
        RecomputeNext();
    }

    private uint Now32() => unchecked((uint)Now64());

    internal ulong Now64() => unchecked(_futureBase - (ulong)context.SchedulerNext);

    internal void Request(int id, int cycles)
    {
        Sh4HostRuntime.Verify(cycles == -1 || (cycles >= 0 && cycles <= MainClock),
            "cycles == -1 || (cycles >= 0 && cycles <= SH4_MAIN_CLOCK)");
        ref Entry entry = ref CollectionsMarshal.AsSpan(_entries)[id];
        entry.Start = unchecked((int)Now32());
        if (cycles == -1)
        {
            entry.End = -1;
        }
        else
        {
            entry.End = unchecked(entry.Start + cycles);
            if (entry.End == -1)
                entry.End++;
        }

        RecomputeNext();
    }

    internal bool IsScheduled(int id) => _entries[id].End != -1;

    private int Elapsed(ref Entry entry)
    {
        if (entry.End == -1)
            return -1;

        int elapsed = unchecked((int)(Now32() - (uint)entry.Start));
        entry.Start = unchecked((int)Now32());
        return elapsed;
    }

    private void HandleCallback(int id)
    {
        ref Entry entry = ref CollectionsMarshal.AsSpan(_entries)[id];
        int interval = unchecked(entry.End - entry.Start);
        int jitter = unchecked(Elapsed(ref entry) - interval);

        entry.End = -1;
        int next = entry.Callback!(entry.Tag, interval, jitter, entry.Argument);
        if (next > 0)
            Request(id, Math.Max(0, next - jitter));
    }

    internal void Tick(int cycles)
    {
        if (context.SchedulerNext >= 0)
            return;

        uint sliceStart = unchecked(Now32() - (uint)cycles);
        if (_nextId != -1)
        {
            int entryCount = _entries.Count;
            for (int id = 0; id < entryCount; id++)
            {
                int remaining = unchecked((int)Remaining(_entries[id], sliceStart));
                if (remaining >= 0 && remaining <= cycles)
                    HandleCallback(id);
            }
        }

        RecomputeNext();
    }

    internal void Reset(bool hard)
    {
        if (hard)
        {
            _futureBase = 0;
            _nextId = -1;
            foreach (ref Entry entry in CollectionsMarshal.AsSpan(_entries))
                entry.Start = entry.End = -1;
            context.SchedulerNext = 0;
        }
    }

    internal void Serialize(BinaryWriter writer, int id)
    {
        Entry entry = _entries[id];
        writer.Write(entry.Tag);
        writer.Write(entry.Start);
        writer.Write(entry.End);
    }

    internal void Deserialize(BinaryReader reader, int id)
    {
        ref Entry entry = ref CollectionsMarshal.AsSpan(_entries)[id];
        entry.Tag = reader.ReadInt32();
        entry.Start = reader.ReadInt32();
        entry.End = reader.ReadInt32();
    }

    internal void Serialize(BinaryWriter writer)
    {
        writer.Write(_futureBase);
        Serialize(writer, SaveSlots.Aica);
        Serialize(writer, SaveSlots.AicaRtc);
        Serialize(writer, SaveSlots.GdRom);
        Serialize(writer, SaveSlots.Maple);
        Serialize(writer, SaveSlots.AicaDma);
        Serialize(writer, SaveSlots.RenderEnd);
        Serialize(writer, SaveSlots.VBlank);
    }

    internal void DeserializeState(BinaryReader reader, int version)
    {
        _futureBase = reader.ReadUInt64();
        if (version is >= SaveVersion19 and <= SaveVersion31)
            reader.ReadUInt32();

        Deserialize(reader, SaveSlots.Aica);
        Deserialize(reader, SaveSlots.AicaRtc);
        Deserialize(reader, SaveSlots.GdRom);
        Deserialize(reader, SaveSlots.Maple);
        Deserialize(reader, SaveSlots.AicaDma);
        if (version < SaveVersion58)
        {
            foreach (int id in context.AddressSpace.TimerRegisters.SchedulerIds)
                Deserialize(reader, id);
        }

        Deserialize(reader, SaveSlots.RenderEnd);
        Deserialize(reader, SaveSlots.VBlank);
    }
}

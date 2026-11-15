namespace RetroMediaLab.Chips.CPU.Hitachi.SH2.Contracts;

/// <summary>Borrowed big-endian CPU bus. Timings use SH-2 clocks.</summary>
public interface ISh2Bus
{
    byte Read8(uint address);
    ushort Read16(uint address);
    ushort Fetch16(uint address) => Read16(address);
    ushort Fetch16(uint address, out int waitStates) { waitStates = WaitStates(address, false); return Fetch16(address); }
    /// <summary>Cache line fill, without treating an instruction fetch as a polling data read.</summary>
    uint Fetch32(uint address) => (uint)Fetch16(address) << 16 | Fetch16(address + 2);
    uint FetchMapVersion => 0;
    bool TryMapFetch(uint address, out ReadOnlyMemory<byte> memory, out uint start, out int waitStates)
    { memory = default; start = 0; waitStates = 0; return false; }
    uint Read32(uint address);
    void Write8(uint address, byte value);
    void Write16(uint address, ushort value);
    void Write32(uint address, uint value);
    int WaitStates(uint address, bool write) => 0;
    /// <summary>Upper bound for one opcode fetch or data access; negative disables prevalidated payloads.</summary>
    int MaximumWaitStates => -1;
    int CyclesUntilEvent => int.MaxValue;
    /// <summary>Version of executable memory containing this address; -1 disables block caching.</summary>
    long CodeVersion(uint address) => -1;
    /// <summary>Nonzero code write/mapping epoch for this address. Zero requires checking individual code versions.</summary>
    ulong CodeEpoch(uint address) => 0;
    /// <summary>Mark an executed/compiled opcode range for the bus's code-write barrier. Length is in bytes.</summary>
    void MarkCode(uint start, int length) { }
    bool DeferClockUpdates => false;
    ulong NextEventCycle => ulong.MaxValue;
    bool IsTimedAccess(uint address) => true;
    void AdvanceCycles(int cycles) { }
}

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Buses;

/// <summary>Borrowed bank map; the machine owns its memory and mapped devices.</summary>
internal abstract class MappedCpuBus(CartridgeMemoryBank[] banks) : ICpu68000Bus, ICpu68000StableCodeBus, ICpu68000DirectMemoryBus, ICpu68000NativeRefreshBus, ICpu68000ObservedCodeBus
{
    private long _writableBackingRevision = long.MinValue;
    private readonly HashSet<byte[]> _writableBackings = [];
    public uint Read8(uint address)
    {
        CartridgeMemoryBank bank = banks[(address >> 16) & 255];
        return bank.read8 is { } read ? read(address) : bank.Data.Span[(int)((address & 65535) ^ 1)];
    }
    public uint Read16(uint address)
    {
        CartridgeMemoryBank bank = banks[(address >> 16) & 255];
        return bank.read16 is { } read ? read(address) : BinaryPrimitives.ReadUInt16LittleEndian(bank.Data.Span[(int)(address & 65534)..]);
    }
    public void Write8(uint address, uint value)
    {
        CartridgeMemoryBank bank = banks[(address >> 16) & 255];
        if (bank.write8 is { } write) write(address, value);
        else bank.Data.Span[(int)((address & 65535) ^ 1)] = (byte)value;
    }
    public void Write16(uint address, uint value)
    {
        CartridgeMemoryBank bank = banks[(address >> 16) & 255];
        if (bank.write16 is { } write) write(address, value);
        else BinaryPrimitives.WriteUInt16LittleEndian(bank.Data.Span[(int)(address & 65534)..], (ushort)value);
    }
    public uint ReadMapped8(uint address) => banks[(address >> 16) & 255].MappedReadsUseHandlers
        ? banks[(address >> 16) & 255].CodeWindow is null || banks[(address >> 16) & 255].CodeFetchObserver is not null ? Read8(address) : ReadMappedHandler8(address)
        : banks[(address >> 16) & 255].Data.Span[(int)((address & 65535) ^ 1)];
    public uint ReadMapped16(uint address) => banks[(address >> 16) & 255].MappedReadsUseHandlers
        ? banks[(address >> 16) & 255].CodeWindow is null || banks[(address >> 16) & 255].CodeFetchObserver is not null ? Read16(address) : ReadMappedHandler16(address)
        : BinaryPrimitives.ReadUInt16LittleEndian(banks[(address >> 16) & 255].Data.Span[(int)(address & 65534)..]);
    public bool RequiresCodeFetchObservation(uint address) => banks[(address >> 16) & 255].CodeWindow is not null
        && banks[(address >> 16) & 255].CodeFetchObserver is not null;
    public void ObserveCodeFetch(uint address) => banks[(address >> 16) & 255].CodeFetchObserver?.Invoke(address);
    private uint ReadMappedHandler8(uint address)
    {
        var provider = banks[(address >> 16) & 255].CodeWindow;
        return provider is not null && provider(address, out var memory, out uint start)
            ? memory.Span[(int)(((address & 0x00ffffff) - start) ^ 1)] : Read8(address);
    }
    private uint ReadMappedHandler16(uint address)
    {
        var provider = banks[(address >> 16) & 255].CodeWindow;
        return provider is not null && provider(address, out var memory, out uint start)
            ? BinaryPrimitives.ReadUInt16LittleEndian(memory.Span[(int)(((address & 0x00ffffff) - start) & ~1u)..]) : Read16(address);
    }
    public bool TryGetCodeMemory(uint address, out ReadOnlyMemory<byte> memory, out uint logicalBase)
    {
        CartridgeMemoryBank bank = banks[(address >> 16) & 255];
        logicalBase = address & 0x00ff0000;
        if (!bank.MappedReadsUseHandlers && bank.Data.Length >= 65536)
        {
            memory = bank.Data[..65536];
            return true;
        }
        if (bank.CodeWindow is { } provider) return provider(address, out memory, out logicalBase);
        memory = default;
        return false;
    }
    public abstract void AcknowledgeInterrupt(uint level);
    public bool IsCodeContentStable(uint address, ReadOnlyMemory<byte> memory, uint logicalBase)
    {
        if (!TryGetCodeMemory(address, out var current, out uint currentBase)
            || currentBase != logicalBase || !current.Equals(memory)
            || !CartridgeRomCode.TryGetStableBacking(memory, out var backing)) return false;
        // A protected read alias does not make the backing array immutable if
        // another bank can write it directly, bypassing mapper notifications.
        long revision = CodeMemoryVersion;
        if (_writableBackingRevision != revision)
        {
            _writableBackings.Clear();
            foreach (var bank in banks)
            {
                if (bank.write8 is not null && bank.write16 is not null) continue;
                if (MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)bank.Data, out ArraySegment<byte> segment)
                    && segment.Array is not null) _writableBackings.Add(segment.Array);
            }
            _writableBackingRevision = revision;
        }
        return !_writableBackings.Contains(backing!);
    }
    public long GetStableCodeContentVersion(uint address, ReadOnlyMemory<byte> memory, uint logicalBase)
        => CartridgeRomCode.ContentVersion(memory);
    public virtual long DirectMemoryVersion => CartridgeMemoryBank.MappingVersion;
    public long CodeMemoryVersion => CartridgeMemoryBank.MappingVersion;
    public virtual void GetDirectMemory(int page, out ReadOnlyMemory<byte> read, out Memory<byte> write)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(page);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(page, 255);
        CartridgeMemoryBank bank = banks[page];
        if (bank.Data.Length < 65536) { read = default; write = default; return; }
        read = bank.read8 is null && bank.read16 is null ? bank.Data[..65536] : default;
        write = bank.write8 is null && bank.write16 is null ? bank.Data[..65536] : default;
    }
    public virtual bool IsRefreshWindowElapsed(int cycles, int deadline) => cycles >= deadline;
    public virtual Cpu68000NativeRefreshMode NativeRefreshMode => Cpu68000NativeRefreshMode.OwnState;
}

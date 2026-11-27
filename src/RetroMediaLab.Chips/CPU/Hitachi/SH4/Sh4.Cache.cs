

using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal static class Sh4Cache
{
    private static readonly bool[] CachedAreas = [true, true, true, true, true, false, true, false];
    private static readonly bool[] TranslatedAreas = [true, true, true, true, false, false, true, false];

    [InlineArray(32)]
    internal struct LineData
    {
        private byte _first;
    }

    internal static bool IsCachedArea(uint area) => CachedAreas[area];
    internal static bool IsTranslatedArea(uint area) => TranslatedAreas[area];
}

internal sealed class Sh4InstructionCache
{
    [StructLayout(LayoutKind.Explicit, Size = 40)]
    private struct Line
    {
        [FieldOffset(0)] internal bool Valid;
        [FieldOffset(4)] internal uint Tag;
        [FieldOffset(8)] internal Sh4Cache.LineData Data;
    }

    private readonly Line[] _lines = new Line[256];

    private readonly Sh4Cycles Cycles = new();
    private Sh4Context Context { get; set; } = null!;
    private Sh4CacheAndTlbController Registers { get; set; } = null!;
    private Sh4AddressSpace Memory { get; set; } = null!;
    private Sh4Mmu Mmu { get; set; } = null!;

    internal void Initialize(Sh4Context context, Sh4CacheAndTlbController registers, Sh4AddressSpace memory, Sh4Mmu mmu)
    {
        Context = context;
        Registers = registers;
        Memory = memory;
        Mmu = mmu;
        Cycles.Initialize(context);
    }

    internal unsafe ushort ReadMemory(uint address)
    {
        Sh4MmuError error = TranslateAddress(address, out uint physical, out bool cached);
        if (error != Sh4MmuError.None)
            Mmu.RaiseException(address, error, Sh4MmuAccess.InstructionRead);
        if (!cached)
        {
            Cycles.AddReadAccessCycles(physical, 2);
            return Memory.Read16(physical);
        }

        uint index = (Registers.CacheControl & 0x8000) != 0
            ? (address >> 5 & 0x7F) | (address >> 18 & 0x80)
            : address >> 5 & 0xFF;
        uint tag = physical >> 10 & 0x7FFFF;
        ref Line line = ref _lines[index];
        if (!line.Valid || line.Tag != tag)
        {
            line.Valid = true;
            line.Tag = tag;
            uint lineAddress = physical & ~31u;
            byte* pointer = Memory.GetMemoryPointer(lineAddress, 32);
            Span<byte> lineData = line.Data;
            if (pointer != null)
                new ReadOnlySpan<byte>(pointer, 32).CopyTo(lineData);
            else
                for (int offset = 0; offset < 32; offset += 4)
                    BinaryPrimitives.WriteUInt32LittleEndian(lineData[offset..], Memory.Read32(lineAddress + (uint)offset));
            Cycles.AddReadAccessCycles(physical, 32);
        }

        ReadOnlySpan<byte> data = line.Data;
        return BinaryPrimitives.ReadUInt16LittleEndian(data[(int)(physical & 31)..]);
    }

    internal void Invalidate()
    {
        foreach (ref Line line in _lines.AsSpan())
            line.Valid = false;
    }

    internal void Reset(bool hard)
    {
        if (hard)
            Array.Clear(_lines);
    }

    internal void Serialize(BinaryWriter writer) =>
        writer.Write(MemoryMarshal.AsBytes(_lines.AsSpan()));

    internal void Deserialize(BinaryReader reader) =>
        reader.BaseStream.ReadExactly(MemoryMarshal.AsBytes(_lines.AsSpan()));

    internal uint ReadAddressArray(uint address)
    {
        ref Line line = ref _lines[address >> 5 & 0xFF];
        return (line.Tag << 10) | (line.Valid ? 1u : 0);
    }

    internal void WriteAddressArray(uint address, uint value)
    {
        ref Line line = ref _lines[address >> 5 & 0xFF];
        if ((address & 8) == 0)
        {
            line.Valid = (value & 1) != 0;
            line.Tag = value >> 10 & 0x7FFFF;
            return;
        }

        uint virtualAddress = value & ~0x3FFu;
        Sh4MmuError error = TranslateAddress(virtualAddress, out uint physical, out _);
        if (error == Sh4MmuError.TlbMiss)
            return;
        if (error != Sh4MmuError.None)
            Mmu.RaiseException(virtualAddress, error, Sh4MmuAccess.InstructionRead);
        if (line.Valid && (physical >> 10 & 0x7FFFF) == line.Tag)
            line.Valid = (value & 1) != 0;
    }

    internal uint ReadDataArray(uint address)
    {
        ReadOnlySpan<byte> data = _lines[address >> 5 & 0xFF].Data;
        return BinaryPrimitives.ReadUInt32LittleEndian(data[(int)(address & 28)..]);
    }

    internal void WriteDataArray(uint address, uint value)
    {
        Span<byte> data = _lines[address >> 5 & 0xFF].Data;
        BinaryPrimitives.WriteUInt32LittleEndian(data[(int)(address & 28)..], value);
    }

    private Sh4MmuError TranslateAddress(uint address, out uint physical, out bool cached)
    {
        physical = address;
        cached = false;
        uint area = address >> 29;
        bool userMode = !Context.Status.MD;
        if ((address & 1) != 0 || (userMode ? (address & 0x80000000) != 0 : area == 7))
            return Sh4MmuError.BadAddress;

        cached = (Registers.CacheControl & 0x100) != 0 && Sh4Cache.IsCachedArea(area);
        if ((Registers.MmuControl & 1) == 0 || !Sh4Cache.IsTranslatedArea(area) || (address & 0xFC000000) == 0x7C000000)
            return Sh4MmuError.None;

        Sh4MmuError error = Mmu.LookupInstruction(address, out Sh4TlbEntry entry, out physical);
        if (error != Sh4MmuError.None)
            return error;
        if (userMode && (entry.Protection & 2) == 0)
            return Sh4MmuError.Protected;
        cached &= (entry.Data & 8) != 0;
        return Sh4MmuError.None;
    }
}

internal sealed class Sh4OperandCache
{
    [StructLayout(LayoutKind.Explicit, Size = 40)]
    private struct Line
    {
        [FieldOffset(0)] internal bool Valid;
        [FieldOffset(1)] internal bool Dirty;
        [FieldOffset(4)] internal uint Tag;
        [FieldOffset(8)] internal Sh4Cache.LineData Data;
    }

    private readonly Line[] _lines = new Line[512];
    private ulong _writeBackBufferCycles;
    private ulong _writeThroughBufferCycles;

    private readonly Sh4Cycles Cycles = new();
    private Sh4Context Context { get; set; } = null!;
    private Sh4CacheAndTlbController Registers { get; set; } = null!;
    private Sh4AddressSpace Memory { get; set; } = null!;
    private Sh4Mmu Mmu { get; set; } = null!;

    internal void Initialize(Sh4Context context, Sh4CacheAndTlbController registers, Sh4AddressSpace memory, Sh4Mmu mmu)
    {
        Context = context;
        Registers = registers;
        Memory = memory;
        Mmu = mmu;
        Cycles.Initialize(context);
    }

    internal T ReadMemory<T>(uint address) where T : unmanaged
    {
        int size = Unsafe.SizeOf<T>();
        Sh4MmuError error = TranslateAddress(address, size, false, out uint physical, out bool cached, out _);
        if (error != Sh4MmuError.None)
            Mmu.RaiseException(address, error, Sh4MmuAccess.DataRead);
        if (!cached)
        {
            Cycles.AddReadAccessCycles(physical, (uint)size);
            return Memory.Read<T>(physical);
        }

        uint index = GetLineIndex(address);
        ref Line line = ref _lines[index];
        uint tag = physical >> 10 & 0x7FFFF;
        if (!line.Valid || line.Tag != tag)
        {
            if (line.Dirty && line.Valid)
                WriteBackLine(index, ref line);
            line.Tag = tag;
            ReadCacheLine(physical, ref line);
        }

        ReadOnlySpan<byte> data = line.Data;
        return MemoryMarshal.Read<T>(data[(int)(physical & 31)..]);
    }

    internal void WriteMemory<T>(uint address, T value) where T : unmanaged
    {
        int size = Unsafe.SizeOf<T>();
        Sh4MmuError error = TranslateAddress(address, size, true, out uint physical, out bool cached, out bool copyBack);
        if (error != Sh4MmuError.None)
            Mmu.RaiseException(address, error, Sh4MmuAccess.DataWrite);
        if (!cached)
        {
            AddWriteThroughCycles(physical, size);
            Memory.Write(physical, value);
            return;
        }

        uint index = GetLineIndex(address);
        ref Line line = ref _lines[index];
        uint tag = physical >> 10 & 0x7FFFF;
        if (!line.Valid || line.Tag != tag)
        {
            if (copyBack)
            {
                if (line.Dirty && line.Valid)
                    WriteBackLine(index, ref line);
                line.Tag = tag;
                ReadCacheLine(physical, ref line);
            }
        }
        else if (!copyBack)
        {
            Span<byte> data = line.Data;
            MemoryMarshal.Write(data[(int)(physical & 31)..], in value);
        }

        if (copyBack)
        {
            line.Dirty = true;
            Span<byte> data = line.Data;
            MemoryMarshal.Write(data[(int)(physical & 31)..], in value);
        }
        else
        {
            Memory.Write(physical, value);
            AddWriteThroughCycles(physical, size);
        }
    }

    internal void WriteBack(uint address, bool writeBack, bool invalidate)
    {
        Sh4MmuError error = TranslateAddress(address, 1, true, out uint physical, out bool cached, out _);
        if (error != Sh4MmuError.None)
            Mmu.RaiseException(address, error, Sh4MmuAccess.DataWrite);
        if (!cached)
            return;

        uint index = GetLineIndex(address);
        ref Line line = ref _lines[index];
        if (!line.Valid || (physical >> 10 & 0x7FFFF) != line.Tag)
            return;
        if (writeBack && line.Dirty)
            WriteBackLine(index, ref line);
        line.Valid = !invalidate;
        line.Dirty = false;
    }

    internal void Prefetch(uint address)
    {
        address &= ~31u;
        Sh4MmuError error = TranslateAddress(address, 1, false, out uint physical, out bool cached, out _);
        if (error != Sh4MmuError.None || !cached)
            return;
        uint index = GetLineIndex(address);
        ref Line line = ref _lines[index];
        uint tag = physical >> 10 & 0x7FFFF;
        if (line.Valid && line.Tag == tag)
            return;
        if (line.Valid && line.Dirty)
            WriteBackLine(index, ref line);
        line.Tag = tag;
        ReadCacheLine(physical, ref line);
    }

    internal void Invalidate()
    {
        foreach (ref Line line in _lines.AsSpan())
        {
            line.Dirty = false;
            line.Valid = false;
        }
    }

    internal void Reset(bool hard)
    {
        if (hard)
            Array.Clear(_lines);
    }

    internal void Serialize(BinaryWriter writer)
    {
        writer.Write(MemoryMarshal.AsBytes(_lines.AsSpan()));
        writer.Write(_writeBackBufferCycles);
        writer.Write(_writeThroughBufferCycles);
    }

    internal void Deserialize(BinaryReader reader, int version)
    {
        reader.BaseStream.ReadExactly(MemoryMarshal.AsBytes(_lines.AsSpan()));
        const int saveVersion55 = 850;
        if (version >= saveVersion55)
        {
            _writeBackBufferCycles = reader.ReadUInt64();
            _writeThroughBufferCycles = reader.ReadUInt64();
        }
    }

    internal uint ReadAddressArray(uint address)
    {
        ref Line line = ref _lines[address >> 5 & 0x1FF];
        return (line.Tag << 10) | (line.Valid ? 1u : 0) | (line.Dirty ? 2u : 0);
    }

    internal void WriteAddressArray(uint address, uint value)
    {
        uint index = address >> 5 & 0x1FF;
        ref Line line = ref _lines[index];
        if ((address & 8) == 0)
        {
            if (line.Valid && line.Dirty)
                WriteBackLine(index, ref line);
            line.Tag = value >> 10 & 0x7FFFF;
        }
        else
        {
            uint virtualAddress = value & ~0x3FFu;
            Sh4MmuError error = TranslateAddress(virtualAddress, 1, false, out uint physical, out _, out _);
            if (error == Sh4MmuError.TlbMiss)
                return;
            if (error != Sh4MmuError.None)
                Mmu.RaiseException(virtualAddress, error, Sh4MmuAccess.DataRead);
            if (!line.Valid || (physical >> 10 & 0x7FFFF) != line.Tag)
                return;
            if ((value & 3) != 0 && line.Dirty)
                WriteBackLine(index, ref line);
        }

        line.Valid = (value & 1) != 0;
        line.Dirty = (value & 2) != 0;
    }

    internal uint ReadDataArray(uint address)
    {
        ReadOnlySpan<byte> data = _lines[address >> 5 & 0x1FF].Data;
        return BinaryPrimitives.ReadUInt32LittleEndian(data[(int)(address & 28)..]);
    }

    internal void WriteDataArray(uint address, uint value)
    {
        Span<byte> data = _lines[address >> 5 & 0x1FF].Data;
        BinaryPrimitives.WriteUInt32LittleEndian(data[(int)(address & 28)..], value);
    }

    internal void WriteBackAll()
    {
        for (uint index = 0; index < _lines.Length; index++)
        {
            ref Line line = ref _lines[index];
            if (line.Valid && line.Dirty)
                WriteBackLine(index, ref line);
            line.Valid = false;
            line.Dirty = false;
        }
    }

    private uint GetLineIndex(uint address)
    {
        uint control = Registers.CacheControl;
        bool ram = (control & 0x20) != 0;
        uint index = (control & 0x80) != 0
            ? (address >> 17 & 0x100) | (address >> 5 & (ram ? 0x7Fu : 0xFFu))
            : address >> 5 & (ram ? 0x17Fu : 0x1FFu);
        return ram && address >> 29 == 3 ? index | 0x80 : index;
    }

    private unsafe void ReadCacheLine(uint address, ref Line line)
    {
        line.Valid = true;
        line.Dirty = false;
        uint lineAddress = address & ~31u;
        byte* pointer = Memory.GetMemoryPointer(lineAddress, 32);
        Span<byte> data = line.Data;
        if (pointer != null)
            new ReadOnlySpan<byte>(pointer, 32).CopyTo(data);
        else
            for (int offset = 0; offset < 32; offset += 4)
                BinaryPrimitives.WriteUInt32LittleEndian(data[offset..], Memory.Read32(lineAddress + (uint)offset));
        Cycles.AddReadAccessCycles(address, 32);
    }

    private unsafe void WriteBackLine(uint index, ref Line line)
    {
        if ((Registers.CacheControl & 0x20) != 0 && (index & 0x80) != 0)
            return;

        uint address = (line.Tag << 10) | ((index & 0x1F) << 5);
        byte* pointer = Memory.GetMemoryPointer(address, 32);
        ReadOnlySpan<byte> data = line.Data;
        if (pointer != null)
            data.CopyTo(new Span<byte>(pointer, 32));
        else
            for (int offset = 0; offset < 32; offset += 4)
                Memory.Write32(address + (uint)offset, BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]));
        AddWriteBackCycles(address);
    }

    private Sh4MmuError TranslateAddress(uint address, int size, bool write, out uint physical, out bool cached, out bool copyBack)
    {
        physical = address;
        cached = false;
        copyBack = false;
        if ((address & (uint)(size - 1)) != 0)
            return Sh4MmuError.BadAddress;
        if (write && (address & 0xFC000000) == 0xE0000000)
            return Mmu.TranslateStoreQueue(address, Sh4MmuAccess.DataWrite, out _);

        uint area = address >> 29;
        bool userMode = !Context.Status.MD;
        if (userMode && (address & 0x80000000) != 0)
            return Sh4MmuError.BadAddress;
        uint control = Registers.CacheControl;
        cached = (control & 1) != 0 && Sh4Cache.IsCachedArea(area);
        copyBack = write && (area == 4 ? (control & 4) != 0 : (control & 2) == 0);
        if ((Registers.MmuControl & 1) == 0 || !Sh4Cache.IsTranslatedArea(area) || (address & 0xFC000000) == 0x7C000000)
            return Sh4MmuError.None;

        Sh4MmuError error = Mmu.LookupFull(address, out Sh4TlbEntry entry, out physical);
        if (error != Sh4MmuError.None)
            return error;
        if (userMode && (entry.Protection & 2) == 0)
            return Sh4MmuError.Protected;
        if (write)
        {
            if ((entry.Protection & 1) == 0)
                return Sh4MmuError.Protected;
            if (!entry.Dirty)
                return Sh4MmuError.FirstWrite;
            copyBack &= (entry.Data & 1) == 0;
        }
        cached &= (entry.Data & 8) != 0;
        if ((physical & 0x1C000000) == 0x1C000000)
            physical |= 0xF0000000;
        return Sh4MmuError.None;
    }

    private void AddWriteBackCycles(uint address)
    {
        ulong now = Cycles.Now();
        if (_writeBackBufferCycles > now)
            Cycles.AddCycles((int)(_writeBackBufferCycles - now));
        _writeBackBufferCycles = now + (ulong)Cycles.WriteAccessCycles(address, 32);
    }

    private void AddWriteThroughCycles(uint address, int size)
    {
        ulong now = Cycles.Now();
        if (_writeThroughBufferCycles > now)
            Cycles.AddCycles((int)(_writeThroughBufferCycles - now));
        int cycles = Cycles.WriteAccessCycles(address, (uint)Math.Min(size, 8));
        if (size == 32)
            Cycles.AddCycles(cycles * 3);
        _writeThroughBufferCycles = now + (ulong)cycles;
    }
}

internal sealed partial class Sh4AddressSpace
{
    private static readonly Sh4InstructionCache InstructionCacheInstance = new();
    private static readonly Sh4OperandCache OperandCacheInstance = new();

    internal Sh4InstructionCache InstructionCache => InstructionCacheInstance;
    internal Sh4OperandCache OperandCache => OperandCacheInstance;

    private static T ReadCachedMemory<T>(uint address) where T : unmanaged =>
        OperandCacheInstance.ReadMemory<T>(address);

    private static void WriteCachedMemory<T>(uint address, T value) where T : unmanaged =>
        OperandCacheInstance.WriteMemory(address, value);

    private static ushort ReadCachedInstruction16(uint address) =>
        InstructionCacheInstance.ReadMemory(address);
}

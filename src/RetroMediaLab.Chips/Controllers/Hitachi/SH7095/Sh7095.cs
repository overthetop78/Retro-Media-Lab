using System.Buffers.Binary;
using RetroMediaLab.Chips.CPU.Hitachi.SH2;
using RetroMediaLab.Chips.CPU.Hitachi.SH2.Contracts;

namespace RetroMediaLab.Chips.Controllers.Hitachi.SH7095;

/// <summary>Host memory policy. It is not a hardware register or part of the chip context.</summary>
public enum Sh7095MemoryMode { Cached, DirectMapped }

/// <summary>SH-2 SoC peripherals and cache. External bus and serial receiver are borrowed.</summary>
public sealed partial class Sh7095 : ISh2Bus, ISh2NativeMappedBus
{
    private readonly ISh2Bus _external;
    private readonly bool? _busSlave;
    private readonly byte[] _io = new byte[512], _cache = new byte[4096], _valid = new byte[256], _lru = new byte[64];
    private readonly uint[] _tags = new uint[256];
    private ushort _ocra = 0xffff, _ocrb = 0xffff;
    private int _frtRemainder, _wdtRemainder, _dmaBudget, _externalLevel, _externalVector;
    private ulong _serialReady;
    private bool _inDma;
    private ulong _peripheralTicks, _nextPeripheralEvent;
    private readonly bool[] _dreq = new bool[2];
    private readonly uint[] _codeVersions = new uint[256];
    private readonly byte[] _compiledCacheWords = new byte[256];
    private uint _codeEpochCounter;
    private uint _fetchMapVersion;
    private Sh7095MemoryMode _memoryMode;
    private readonly byte[] _nativeReadPolicy = [1];
    public Sh2<CpuBus> Cpu { get; }
    /// <summary>Cached preserves the cache model; DirectMapped uses the Gens/PicoDrive external maps.</summary>
    public Sh7095MemoryMode MemoryMode
    {
        get => _memoryMode;
        set
        {
            if ((uint)value > (uint)Sh7095MemoryMode.DirectMapped) throw new ArgumentOutOfRangeException(nameof(value));
            if (_memoryMode == value) return;
            Cpu.ClearCodeCache(); _fetchMapVersion++; _memoryMode = value;
            _nativeReadPolicy[0] = value == Sh7095MemoryMode.Cached ? (byte)1 : (byte)0;
        }
    }
    public ReadOnlySpan<bool> Dreq => _dreq;
    public ulong Ticks { get; private set; }
    public Action<byte>? SerialTransmitted { get; set; }
    public Sh7095(ISh2Bus external) : this(external, null) { }
    public Sh7095(ISh2Bus external, bool? busSlave) { _external = external; _busSlave = busSlave; Cpu = Sh2<CpuBus>.CreateNative(new CpuBus(this), external.GetType()); Reset(); }
    /// <summary>Value-type bus lets the JIT specialize the interpreter for this SoC.</summary>
    public readonly struct CpuBus(Sh7095 owner) : ISh2Bus, ISh2NativeReadCacheBus, ISh2NativeIdleBus, ISh2NativeMappedBus
    {
        public Sh2NativeReadMap GetNativeReadMap() => owner.GetNativeReadMap();
        public bool TryGetNativeIdleClocks(uint firstAddress, int words, out int clocks)
        {
            clocks = 0;
            if (words is < 2 or > 3 || owner._memoryMode != Sh7095MemoryMode.Cached
                // Instruction-inhibited cached aliases may still get zero
                // data waits from an existing line. Keep that case in the helper.
                || firstAddress < 0x20000000 && (owner._io[0x92] & 3) == 3
                || !owner.TryMapFetch(firstAddress, out var memory, out uint start, out _)
                || firstAddress < start || (ulong)(firstAddress - start) + (uint)(words * 2) > (ulong)memory.Length) return false;
            if ((firstAddress >> 29) == 6
                || firstAddress < 0x20000000 && (owner._io[0x92] & 3) == 1)
            { clocks = words + 1; return true; }
            return owner._external is ISh2NativeIdleBus provider
                && provider.TryGetNativeIdleClocks(firstAddress & 0x1fffffff, words, out clocks);
        }
        public Sh2NativeReadCache GetNativeReadCache() => new(owner._cache, owner._tags, owner._valid, owner._io, owner._nativeReadPolicy);
        public byte Read8(uint address) => owner.Read8(address);
        public ushort Read16(uint address) => owner.Read16(address);
        public uint Read32(uint address) => owner.Read32(address);
        public ushort Fetch16(uint address) => owner.Fetch16(address);
        public ushort Fetch16(uint address, out int waitStates) => owner.Fetch16(address, out waitStates);
        public void Write8(uint address, byte value) => owner.Write8(address, value);
        public void Write16(uint address, ushort value) => owner.Write16(address, value);
        public void Write32(uint address, uint value) => owner.Write32(address, value);
        public int WaitStates(uint address, bool write) => owner.WaitStates(address, write);
        public int MaximumWaitStates => owner._external.MaximumWaitStates;
        public int CyclesUntilEvent => (int)Math.Min(int.MaxValue, owner._nextPeripheralEvent > owner.Ticks ? owner._nextPeripheralEvent - owner.Ticks : 0);
        public long CodeVersion(uint address)
        {
            if ((address >> 29) == 6) return owner._codeVersions[address >> 4 & 255];
            if (address >= 0x40000000) return -1;
            uint physical = address & 0x1fffffff;
            long external = owner._external.CodeVersion(physical);
            if (owner._memoryMode == Sh7095MemoryMode.DirectMapped) return external;
            if (external < 0) return -1;
            long mode = (long)owner._io[0x92] << 48;
            if (address < 0x20000000 && (owner._io[0x92] & 3) == 1)
            {
                int line = owner.FindLine(physical);
                return line < 0 ? mode | (1L << 47) | (external & 0xffffffffL)
                    : mode | (long)line << 32 | owner._codeVersions[line];
            }
            return mode | (external & 0xffffffffL);
        }
        public ulong CodeEpoch(uint address)
        {
            if ((address >> 29) == 6) return owner.CacheCodeEpoch;
            if (address >= 0x40000000) return 0;
            if (owner._memoryMode == Sh7095MemoryMode.DirectMapped) return owner._external.CodeEpoch(address & 0x1fffffff);
            if (address < 0x20000000 && (owner._io[0x92] & 3) == 1)
                return owner.CacheCodeEpoch;
            return owner._external.CodeEpoch(address & 0x1fffffff);
        }
        public void MarkCode(uint start, int length) => owner.MarkCode(start, length);
        public bool DeferClockUpdates => true;
        public ulong NextEventCycle => owner._nextPeripheralEvent;
        public bool IsTimedAccess(uint address)
        {
            if ((address >> 29) == 6) return false;
            uint physical = address & 0x1fffffff;
            return !(address < 0x40000000 && (physical < 0x4000 || physical is >= 0x02000000 and < 0x06040000));
        }
        public void AdvanceCycles(int clocks) => owner.AdvanceCycles(clocks);
        public void AcknowledgeInterrupt(int level, int vector)
        {
            // On-chip interrupt flags are level-sensitive and cleared by software.
            // Only the external interrupt controller owns an external acknowledge.
            if (level == owner._externalLevel && vector == owner._externalVector)
                owner._external.AcknowledgeInterrupt(level, vector);
        }
        public uint FetchMapVersion => owner._fetchMapVersion;
        public bool TryMapFetch(uint address, out ReadOnlyMemory<byte> memory, out uint start, out int waitStates)
            => owner.TryMapFetch(address, out memory, out start, out waitStates);
    }
    public void Reset(bool warm = false)
    {
        ResetCodeTracking();
        _fetchMapVersion++;
        Array.Clear(_io); Array.Clear(_valid); Array.Clear(_cache); Array.Clear(_lru); Array.Clear(_tags); Array.Clear(_dreq); Array.Clear(_codeVersions);
        _io[4] = 0x84; _io[1] = 0xff; _io[0x10] = 1; _io[0x17] = 0xe0;
        _io[0x1e2] = _busSlave == true ? (byte)0x80 : (byte)0;
        _ocra = _ocrb = 0xffff; _frtRemainder = _wdtRemainder = _dmaBudget = _externalLevel = _externalVector = 0;
        Ticks = _serialReady = _peripheralTicks = 0; _nextPeripheralEvent = ulong.MaxValue;
        _inDma = false; Cpu.Reset(warm); SchedulePeripheralEvent(); Cpu.SynchronizeBusClock();
    }
    public void SetExternalInterrupt(int level, int vector)
    {
        if (_externalLevel == level && _externalVector == vector) return;
        _externalLevel = level; _externalVector = vector; UpdateInterrupt();
    }
    public void SetDreq(int channel, bool asserted)
    {
        if (_dreq[channel] == asserted) return;
        SynchronizePeripherals(); _dreq[channel] = asserted; SchedulePeripheralEvent();
    }
    /// <summary>Latch the free-running counter on the board's input-capture strobe.</summary>
    public void CaptureTimerInput()
    {
        Cpu.SynchronizeBusClock(); SynchronizePeripherals();
        _io[0x18] = _io[0x12]; _io[0x19] = _io[0x13]; _io[0x11] |= 0x80;
        UpdateInterrupt(); SchedulePeripheralEvent();
    }
    public void ReceiveSerial(byte data)
    {
        if ((_io[2] & 0x10) == 0) return;
        if ((_io[4] & 0x40) != 0) _io[4] |= 0x20;
        _io[5] = data; _io[4] |= 0x40; UpdateInterrupt();
    }
    public int Execute(int clocks) => Cpu.Execute(clocks);
    public Sh2NativeReadMap GetNativeReadMap()
    {
        Sh2NativeReadMap map = _external is ISh2NativeMappedBus provider ? provider.GetNativeReadMap() : default;
        return map.Pages.IsEmpty ? default : new(map.Pages, _io, _nativeReadPolicy);
    }
    private ulong CacheCodeEpoch => (((ulong)_fetchMapVersion << 32) | _codeEpochCounter) + 1;
    private void ResetCodeTracking()
    {
        Array.Clear(_compiledCacheWords); _codeEpochCounter++;
    }
    private void MarkCode(uint address, int length)
    {
        if (length <= 0) return;
        if ((address >> 29) == 6)
        {
            while (length > 0)
            {
                int offset = (int)(address & 4095), bytes = Math.Min(length, 16 - (offset & 15));
                MarkCacheWords(offset >> 4, offset & 15, bytes);
                address += (uint)bytes; length -= bytes;
            }
            return;
        }
        if (address >= 0x40000000) return;
        if (_memoryMode == Sh7095MemoryMode.Cached && address < 0x20000000 && (_io[0x92] & 3) == 1)
        {
            while (length > 0 && address < 0x20000000)
            {
                int offset = (int)(address & 15), bytes = Math.Min(length, 16 - offset);
                int line = FindLine(address & 0x1fffffff);
                if (line >= 0) MarkCacheWords(line, offset, bytes);
                address += (uint)bytes; length -= bytes;
            }
            if (length == 0) return;
        }
        _external.MarkCode(address & 0x1fffffff, length);
    }
    private void MarkCacheWords(int line, int byteOffset, int length)
    {
        int first = byteOffset >> 1, last = (byteOffset + length - 1) >> 1;
        _compiledCacheWords[line] |= (byte)(((1 << (last - first + 1)) - 1) << first);
    }
    private bool TryMapFetch(uint address, out ReadOnlyMemory<byte> memory, out uint start, out int waitStates)
    {
        waitStates = 0; uint area = address >> 29, physical = address & 0x1fffffff;
        if (area == 6) { memory = _cache; start = address & ~4095u; return true; }
        if (area < 2)
        {
            if (_memoryMode == Sh7095MemoryMode.DirectMapped)
            {
                if (_external.TryMapFetch(physical, out memory, out start, out waitStates))
                {
                    start |= address & 0xe0000000;
                    if (area == 0 && (_io[0x92] & 3) == 1) waitStates = 0;
                    return true;
                }
                memory = default; start = 0; return false;
            }
            if (area == 0 && (_io[0x92] & 3) == 1)
            {
                int line = FindLine(physical);
                if (line >= 0) { memory = _cache.AsMemory(line * 16, 16); start = address & ~15u; return true; }
            }
            else if (_external.TryMapFetch(physical, out memory, out start, out waitStates))
            { start |= address & 0xe0000000; return true; }
        }
        memory = default; start = 0; return false;
    }
    public ushort Fetch16(uint address) => Fetch16(address, out _);
    public ushort Fetch16(uint address, out int waitStates)
    {
        uint area = address >> 29, physical = address & 0x1fffffff;
        if (area == 6) { waitStates = 0; return BinaryPrimitives.ReadUInt16BigEndian(_cache.AsSpan((int)(address & 4094), 2)); }
        if (area < 2)
        {
            if (_memoryMode == Sh7095MemoryMode.DirectMapped)
            {
                waitStates = area == 0 && (_io[0x92] & 3) == 1 ? 0 : _external.WaitStates(physical, false);
                return _external.Fetch16(physical);
            }
            if (area == 0 && (_io[0x92] & 3) == 1)
            {
                int line = FindLine(physical);
                if (line >= 0) { waitStates = 0; return BinaryPrimitives.ReadUInt16BigEndian(_cache.AsSpan(line * 16 + (int)(physical & 14), 2)); }
                waitStates = _external.WaitStates(physical, false);
                line = LoadCacheLine(physical);
                return BinaryPrimitives.ReadUInt16BigEndian(_cache.AsSpan(line * 16 + (int)(physical & 14), 2));
            }
            waitStates = _external.WaitStates(physical, false); return _external.Fetch16(physical);
        }
        waitStates = 0; return Read16(address);
    }
    public int WaitStates(uint address, bool write)
    {
        if (address >= 0xfffffe00 || (address >> 29) >= 2) return 0;
        uint physical = address & 0x1fffffff;
        if (_memoryMode == Sh7095MemoryMode.DirectMapped)
            return !write && address < 0x20000000 && (_io[0x92] & 5) == 1 ? 0 : _external.WaitStates(physical, write);
        if (!write && address < 0x20000000 && (_io[0x92] & 1) != 0 && FindLine(physical) >= 0) return 0;
        return _external.WaitStates(physical, write);
    }
    public byte Read8(uint address)
    {
        if (address >= 0xfffffe00)
        {
            int offset = (int)(address & 511);
            SynchronizeInternalRead(offset, 1);
            return ReadInternalByte(offset);
        }
        uint area = address >> 29;
        if (area == 6) return _cache[address & 4095];
        if (area == 3) return (byte)(ReadAddressArray(address) >> ((3 - (int)(address & 3)) * 8));
        uint physical = address & 0x1fffffff;
        if (_memoryMode == Sh7095MemoryMode.DirectMapped && area < 2) return _external.Read8(physical);
        if (area == 0 && (_io[0x92] & 5) == 1)
        {
            int line = FindLine(physical);
            if (line < 0) line = LoadCacheLine(physical);
            return _cache[line * 16 + (physical & 15)];
        }
        return _external.Read8(physical);
    }
    public ushort Read16(uint a)
    {
        if (a is >= 0xfffffe00 and <= 0xfffffffe)
        {
            int offset = (int)(a & 511); SynchronizeInternalRead(offset, 2);
            return (ushort)(ReadInternalByte(offset) << 8 | ReadInternalByte(offset + 1));
        }
        if ((a >> 29) == 6) return BinaryPrimitives.ReadUInt16BigEndian(_cache.AsSpan((int)(a & 4094), 2));
        if (a < 0x40000000 && (_memoryMode == Sh7095MemoryMode.DirectMapped || a >= 0x20000000 || (_io[0x92] & 5) != 1)) return _external.Read16(a & 0x1fffffff);
        if (a < 0x20000000 && (a & 15) <= 14)
        {
            int line = FindLine(a); if (line < 0) line = LoadCacheLine(a);
            return BinaryPrimitives.ReadUInt16BigEndian(_cache.AsSpan(line * 16 + (int)(a & 15), 2));
        }
        return (ushort)((Read8(a) << 8) | Read8(a + 1));
    }
    public uint Read32(uint a)
    {
        if (a is >= 0xfffffe00 and <= 0xfffffffc)
        {
            int offset = (int)(a & 511); SynchronizeInternalRead(offset, 4);
            return (uint)(ReadInternalByte(offset) << 24 | ReadInternalByte(offset + 1) << 16 | ReadInternalByte(offset + 2) << 8 | ReadInternalByte(offset + 3));
        }
        if ((a >> 29) == 6) return BinaryPrimitives.ReadUInt32BigEndian(_cache.AsSpan((int)(a & 4092), 4));
        if (a < 0x40000000 && (_memoryMode == Sh7095MemoryMode.DirectMapped || a >= 0x20000000 || (_io[0x92] & 5) != 1)) return _external.Read32(a & 0x1fffffff);
        if (a < 0x20000000 && (a & 15) <= 12)
        {
            int line = FindLine(a); if (line < 0) line = LoadCacheLine(a);
            return BinaryPrimitives.ReadUInt32BigEndian(_cache.AsSpan(line * 16 + (int)(a & 15), 4));
        }
        return (uint)Read16(a) << 16 | Read16(a + 2);
    }
    public void Write8(uint address, byte value)
    {
        if (address >= 0xfffffe00) { SynchronizePeripherals(); WriteInternalByte((int)(address & 511), value); SchedulePeripheralEvent(); return; }
        uint area = address >> 29;
        if (area == 6)
        {
            int offset = (int)(address & 4095);
            if (_cache[offset] != value)
            {
                _codeVersions[address >> 4 & 255]++;
                if ((_compiledCacheWords[offset >> 4] & (1 << (offset >> 1 & 7))) != 0) _codeEpochCounter++;
            }
            _cache[offset] = value; return;
        }
        if (area == 2) { Purge(address); return; }
        if (area == 3)
        {
            int line = ((int)(_io[0x92] >> 6) * 64) + (int)(address >> 4 & 63);
            _fetchMapVersion++; _tags[line] = address >> 10 & 0x7ffff; _valid[line] = (byte)((address >> 2) & 1); return;
        }
        uint physical = address & 0x1fffffff;
        if (_memoryMode == Sh7095MemoryMode.DirectMapped && area < 2) { _external.Write8(physical, value); return; }
        int cached = FindLine(physical);
        if (cached >= 0)
        {
            int offset = cached * 16 + (int)(physical & 15);
            if (_cache[offset] != value)
            {
                _codeVersions[cached]++;
                if ((_compiledCacheWords[cached] & (1 << (offset >> 1 & 7))) != 0) _codeEpochCounter++;
            }
            _cache[offset] = value;
        }
        _external.Write8(physical, value);
    }
    public void Write16(uint a, ushort v)
    {
        if (a == 0xfffffe80)
        {
            SynchronizePeripherals();
            if ((v >> 8) == 0xa5) _io[0x80] = (byte)v;
            if ((v >> 8) == 0x5a) _io[0x81] = (byte)v;
            UpdateInterrupt(); SchedulePeripheralEvent();
            return;
        }
        if (a < 0x40000000 && a < 0xfffffe00)
        {
            if (_memoryMode == Sh7095MemoryMode.DirectMapped) { _external.Write16(a & 0x1fffffff, v); return; }
            uint p = a & 0x1fffffff; UpdateCached(p, (byte)(v >> 8)); UpdateCached(p + 1, (byte)v); _external.Write16(p, v); return;
        }
        Write8(a, (byte)(v >> 8)); Write8(a + 1, (byte)v);
    }
    public void Write32(uint a, uint v)
    {
        if (a < 0x40000000)
        {
            if (_memoryMode == Sh7095MemoryMode.DirectMapped) { _external.Write32(a & 0x1fffffff, v); return; }
            uint p = a & 0x1fffffff; for (int i = 0; i < 4; i++) UpdateCached(p + (uint)i, (byte)(v >> ((3 - i) * 8)));
            _external.Write32(p, v); return;
        }
        Write16(a, (ushort)(v >> 16)); Write16(a + 2, (ushort)v);
    }
    private void UpdateCached(uint address, byte value)
    {
        int line = FindLine(address);
        if (line >= 0)
        {
            int offset = line * 16 + (int)(address & 15);
            _codeVersions[line]++;
            if (_cache[offset] != value && (_compiledCacheWords[line] & (1 << (offset >> 1 & 7))) != 0) _codeEpochCounter++;
            _cache[offset] = value;
        }
    }
    private int LoadCacheLine(uint physical)
    {
        _fetchMapVersion++;
        int set = (int)(physical >> 4 & 63), first = (_io[0x92] & 8) != 0 ? 2 : 0;
        int way = first + _lru[set] % (4 - first); _lru[set] = (byte)((_lru[set] + 1) & 3);
        int line = way * 64 + set;
        uint start = physical & 0x1ffffff0;
        // ares SH7604 Cache::read: fill a cache line with four long accesses.
        for (int i = 0; i < 16; i += 4)
            BinaryPrimitives.WriteUInt32BigEndian(_cache.AsSpan(line * 16 + i, 4), _external.Fetch32(start + (uint)i));
        _tags[line] = physical >> 10; _valid[line] = 1; _codeVersions[line]++;
        return line;
    }
    private int FindLine(uint address)
    {
        int set = (int)(address >> 4 & 63); uint tag = address >> 10;
        // Same four-way lookup order as ares Cache::read; no byte-level lookup.
        int line = 192 + set; if (_valid[line] != 0 && _tags[line] == tag) return line;
        line = 128 + set; if (_valid[line] != 0 && _tags[line] == tag) return line;
        if ((_io[0x92] & 8) != 0) return -1;
        line = 64 + set; if (_valid[line] != 0 && _tags[line] == tag) return line;
        if (_valid[set] != 0 && _tags[set] == tag) return set;
        return -1;
    }
    private void SynchronizeInternalRead(int offset, int size)
    {
        // Gens sh2_io.asm dispatches register reads directly. Only live timer,
        // serial and DMA state needs materializing in this deadline-based model.
        int end = offset + size - 1;
        if ((offset <= 5 && end >= 4) || (offset <= 0x19 && end >= 0x10)
            || (offset <= 0x81 && end >= 0x80) || (offset <= 0x19f && end >= 0x180))
            SynchronizePeripherals();
    }
    private byte ReadInternalByte(int offset)
    {
        if (offset is 0x14 or 0x15) return (byte)(((_io[0x17] & 0x10) != 0 ? _ocrb : _ocra) >> ((1 - (offset & 1)) * 8));
        if (offset is 0x92 or 0x93) return _io[0x92];
        return _io[offset];
    }
    private uint ReadAddressArray(uint address)
    {
        int line = (int)(_io[0x92] >> 6) * 64 + (int)(address >> 4 & 63);
        return _tags[line] << 10 | (uint)_valid[line] << 2;
    }
    private void Purge(uint address) { _fetchMapVersion++; int set = (int)(address >> 4 & 63); for (int way = 0; way < 4; way++) if (_tags[way * 64 + set] == (address & 0x1fffffff) >> 10) _valid[way * 64 + set] = 0; }
    private uint Io32(int offset) => BinaryPrimitives.ReadUInt32BigEndian(_io.AsSpan(offset, 4));
    private void SetIo32(int offset, uint value) => BinaryPrimitives.WriteUInt32BigEndian(_io.AsSpan(offset, 4), value);
    private void WriteInternalByte(int offset, byte value)
    {
        if (offset == 0x1e2 && _busSlave is bool slave) value = (byte)((value & 0x7f) | (slave ? 0x80 : 0));
        if (offset is 0x92 or 0x93)
        {
            _fetchMapVersion++;
            byte next = (byte)(value & 0xcf);
            if (_memoryMode == Sh7095MemoryMode.DirectMapped && ((_io[0x92] ^ next) & 3) != 0) Cpu.ClearCodeCache();
            if ((value & 0x10) != 0) Array.Clear(_valid); _io[0x92] = next; return;
        }
        if (offset is 0x80 or 0x81) return;
        if (offset == 0x11) { _io[offset] = (byte)((_io[offset] & value & 0x8e) | (value & 1)); UpdateInterrupt(); return; }
        if (offset is 0x14 or 0x15)
        {
            int shift = (1 - (offset & 1)) * 8;
            if ((_io[0x17] & 0x10) != 0) _ocrb = (ushort)((_ocrb & ~(255 << shift)) | value << shift);
            else _ocra = (ushort)((_ocra & ~(255 << shift)) | value << shift);
            return;
        }
        _io[offset] = value;
        if (offset == 3 && (_io[2] & 0x20) != 0) { _io[4] &= 0x7b; _serialReady = Ticks + (ulong)(32 * (_io[1] + 1) * 10 * (1 << ((_io[0] & 3) * 2))); }
        if (offset == 0x107) Divide(false);
        if (offset == 0x117) Divide(true);
        UpdateInterrupt();
    }
    private void Divide(bool wide)
    {
        long divisor = (int)Io32(0x100);
        long dividend = wide ? unchecked((long)((ulong)Io32(0x110) << 32 | Io32(0x114))) : (int)Io32(0x104);
        bool overflow = divisor == 0 || (dividend == long.MinValue && divisor == -1);
        long quotient = overflow ? (dividend < 0 ? int.MinValue : int.MaxValue) : dividend / divisor;
        long remainder = overflow ? dividend : dividend % divisor;
        overflow |= quotient > int.MaxValue || quotient < int.MinValue;
        if (overflow) { _io[0x10b] |= 1; quotient = dividend < 0 ? int.MinValue : int.MaxValue; }
        SetIo32(0x104, unchecked((uint)quotient)); SetIo32(0x114, unchecked((uint)quotient)); SetIo32(0x110, unchecked((uint)remainder));
        SetIo32(0x118, unchecked((uint)remainder)); SetIo32(0x11c, unchecked((uint)quotient));
    }
    public void AdvanceCycles(int cycles)
    {
        Ticks += (uint)cycles;
        if (Ticks >= _nextPeripheralEvent) SynchronizePeripherals();
    }
    private bool _useCoalescedLatchedTimerEvents;
    /// <summary>Skip FRT wakeups that cannot change a latched flag. Select on the owner outside Execute.</summary>
    public bool UseCoalescedLatchedTimerEvents
    {
        get => _useCoalescedLatchedTimerEvents;
        set
        {
            if (value == _useCoalescedLatchedTimerEvents) return;
            Cpu.SynchronizeBusClock(); SynchronizePeripherals();
            _useCoalescedLatchedTimerEvents = value;
            SchedulePeripheralEvent(); Cpu.SynchronizeBusClock();
        }
    }
    private void SynchronizePeripherals()
    {
        if (Ticks == _peripheralTicks) return;
        int cycles = checked((int)(Ticks - _peripheralTicks)); _peripheralTicks = Ticks;
        byte timerFlags = _io[0x11], watchdogFlags = _io[0x80], serialFlags = _io[4];
        uint dma0 = Io32(0x18c), dma1 = Io32(0x19c);
        int select = _io[0x16] & 3;
        if (select != 3)
        {
            int divisor = select == 0 ? 8 : select == 1 ? 32 : 128;
            _frtRemainder += cycles; int steps = _frtRemainder / divisor; _frtRemainder %= divisor;
            if (steps != 0)
            {
                int counter = (_io[0x12] << 8) | _io[0x13];
                int Distance(int target) { int distance = (target - counter) & 65535; return distance == 0 ? 65536 : distance; }
                int a = Distance(_ocra), b = Distance(_ocrb), overflow = 65536 - counter;
                bool clear = (_io[0x11] & 1) != 0 && _ocra != 0;
                int beforeClear = clear ? Math.Min(steps, a) : steps;
                if (beforeClear >= overflow) _io[0x11] |= 2;
                if (beforeClear >= b || (clear && steps >= a && _ocrb > 0 && _ocrb <= _ocra && steps - a >= _ocrb)) _io[0x11] |= 4;
                if (steps >= a) _io[0x11] |= 8;
                counter = clear && steps >= a ? (steps - a) % _ocra : counter + steps;
                _io[0x12] = (byte)(counter >> 8); _io[0x13] = (byte)counter;
            }
        }
        if ((_io[0x80] & 0x20) != 0)
        {
            int divisor = 2 << ((_io[0x80] & 7) * 2); _wdtRemainder += cycles;
            int count = _io[0x81] + _wdtRemainder / divisor; _wdtRemainder %= divisor;
            if (count > 255) _io[0x80] |= 0x80; _io[0x81] = (byte)count;
        }
        if (_serialReady != 0 && Ticks >= _serialReady) { _serialReady = 0; _io[4] |= 0x84; SerialTransmitted?.Invoke(_io[3]); }
        if (!_inDma && DmaReady())
        {
            RunDma(cycles);
        }
        else _dmaBudget = 0;
        if (timerFlags != _io[0x11] || watchdogFlags != _io[0x80] || serialFlags != _io[4] || dma0 != Io32(0x18c) || dma1 != Io32(0x19c)) UpdateInterrupt();
        SchedulePeripheralEvent();
    }
    private void RunDma(int cycles)
    {
        _dmaBudget += cycles; _inDma = true;
        try { while (_dmaBudget >= 2) { if (!TransferDma()) { _dmaBudget = 0; break; } _dmaBudget -= 2; } }
        finally { _inDma = false; }
    }
    private bool DmaReady()
    {
        if ((_io[0x1b3] & 7) != 1) return false;
        for (int channel = 0; channel < 2; channel++)
        {
            uint control = Io32(0x18c + channel * 16);
            if ((control & 3) == 1 && ((control & 0x200) != 0 || _dreq[channel])) return true;
        }
        return false;
    }
    private void SchedulePeripheralEvent()
    {
        ulong next = ulong.MaxValue;
        int select = _io[0x16] & 3;
        if (select != 3)
        {
            int divisor = select == 0 ? 8 : select == 1 ? 32 : 128;
            int counter = _io[0x12] << 8 | _io[0x13];
            int a = (_ocra - counter) & 65535, b = (_ocrb - counter) & 65535;
            int steps = Math.Min(65536 - counter, Math.Min(a == 0 ? 65536 : a, b == 0 ? 65536 : b));
            // No flag transition or interrupt update can result while all three
            // periodic flags remain latched. Register reads/writes and state
            // capture materialize the counter. Keep one full-period checkpoint
            // so deferred elapsed-clock arithmetic stays bounded.
            if (_useCoalescedLatchedTimerEvents && (_io[0x11] & 0x0e) == 0x0e) steps = 65536;
            next = Ticks + (ulong)(steps * divisor - _frtRemainder);
        }
        if ((_io[0x80] & 0x20) != 0) next = Math.Min(next, Ticks + (ulong)((256 - _io[0x81]) * (2 << ((_io[0x80] & 7) * 2)) - _wdtRemainder));
        if (_serialReady != 0) next = Math.Min(next, _serialReady);
        if (!_inDma && DmaReady()) next = Math.Min(next, Ticks + (ulong)Math.Max(1, 2 - _dmaBudget));
        _nextPeripheralEvent = next;
    }
    private bool TransferDma()
    {
        for (int channel = 0; channel < 2; channel++)
        {
            int offset = 0x180 + channel * 16; uint control = Io32(offset + 12);
            if ((control & 3) != 1 || ((control & 0x200) == 0 && !_dreq[channel])) continue;
            uint source = Io32(offset), destination = Io32(offset + 4), count = Io32(offset + 8) & 0xffffff;
            if (count == 0) count = 0x1000000;
            int sizeCode = (int)(control >> 10 & 3), size = sizeCode == 3 ? 4 : 1 << sizeCode;
            int burst = sizeCode == 3 ? (int)Math.Min(4u, count) : 1;
            for (int i = 0; i < burst; i++)
            {
                if (((source | destination) & (uint)(size - 1)) != 0) { _io[0x1b3] |= 4; return false; }
                uint data = size == 1 ? _external.Read8(source & 0x1fffffff) : size == 2 ? _external.Read16(source & 0x1fffffff) : _external.Read32(source & 0x1fffffff);
                if (size == 1) _external.Write8(destination & 0x1fffffff, (byte)data);
                else if (size == 2) _external.Write16(destination & 0x1fffffff, (ushort)data); else _external.Write32(destination & 0x1fffffff, data);
                int sm = (int)(control >> 12 & 3), dm = (int)(control >> 14 & 3);
                source = unchecked(source + (uint)(sm == 1 ? size : sm == 2 ? -size : 0));
                destination = unchecked(destination + (uint)(dm == 1 ? size : dm == 2 ? -size : 0)); count--;
            }
            SetIo32(offset, source); SetIo32(offset + 4, destination); SetIo32(offset + 8, count);
            if (count == 0) SetIo32(offset + 12, control | 2);
            return true;
        }
        return false;
    }
    private void UpdateInterrupt()
    {
        int level = _externalLevel, vector = _externalVector;
        void Consider(int candidate, int irqVector) { if (candidate > level) { level = candidate; vector = irqVector & 127; } }
        // VCRC high = capture, VCRC low = compare A/B, VCRD high = overflow.
        // 0x65 is the SCI transmit-end vector, not an FRT vector.
        if ((_io[0x11] & _io[0x10] & 0x8e) != 0) Consider(_io[0x60] & 15, (_io[0x11] & 0x80) != 0 ? _io[0x66] : (_io[0x11] & 0x0c) != 0 ? _io[0x67] : _io[0x68]);
        for (int channel = 0; channel < 2; channel++) if ((Io32(0x18c + channel * 16) & 6) == 6) Consider(_io[0xe2] & 15, (int)Io32(0x1a0 + channel * 8));
        if ((_io[0x80] & 0x80) != 0) Consider(_io[0xe3] >> 4, _io[0xe4]);
        if ((_io[4] & _io[2] & 0xc0) != 0) Consider(_io[0x60] >> 4, (_io[4] & 0x40) != 0 ? _io[0x63] : _io[0x64]);
        if ((_io[0x10b] & 3) == 3) Consider(_io[0xe2] >> 4, (int)Io32(0x10c));
        Cpu.SetInterrupt(level, vector);
    }
}


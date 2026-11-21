using System.Buffers.Binary;
using RetroMediaLab.Chips.CPU.Hitachi.SH2;
using RetroMediaLab.Chips.CPU.Hitachi.SH2.Contracts;

namespace RetroMediaLab.Chips.Tests.CPU.Hitachi;

public sealed class Sh2MappedMemoryTests
{
    [Theory]
    [InlineData(1, 0x00200020u)]
    [InlineData(2, 0x00200020u)]
    [InlineData(4, 0x00200020u)]
    [InlineData(1, 0x20200020u)]
    [InlineData(2, 0x20200020u)]
    [InlineData(4, 0x20200020u)]
    [InlineData(1, 0x00200ffcu)]
    [InlineData(2, 0x00200ffcu)]
    [InlineData(4, 0x00200ffcu)]
    [InlineData(1, 0x00201000u)]
    [InlineData(2, 0x00201000u)]
    [InlineData(4, 0x00201000u)]
    public void WidthsAliasesAndUnmappedPagesMatchTheInterpreter(int size, uint address)
        => Compare(size, address, cacheEnabled: false, dueEvent: false, forceGc: false);

    [Theory]
    [InlineData(0x00200020u)]
    [InlineData(0x20200020u)]
    public void CacheEnabledP0UsesTheControllerWhileP1RemainsUncached(uint address)
        => Compare(4, address, cacheEnabled: true, dueEvent: false, forceGc: false);

    [Fact]
    public void AnEventCanChangeTheBackingBeforeTheRead()
        => Compare(4, 0x00200020, cacheEnabled: false, dueEvent: true, forceGc: false);

    [Fact]
    public void BackingPinsSurviveCollectionDuringNativeExecution()
        => Compare(4, 0x00200020, cacheEnabled: false, dueEvent: false, forceGc: true);

    private static void Compare(int size, uint address, bool cacheEnabled, bool dueEvent, bool forceGc)
    {
        Bus? referenceBus = null, nativeBus = null; Sh2<Bus>? reference = null, native = null;
        try
        {
            referenceBus = new(size, address, cacheEnabled, dueEvent, forceGc);
            nativeBus = new(size, address, cacheEnabled, dueEvent, forceGc);
            reference = new(referenceBus); native = new(nativeBus);
            reference.Reset(); native.Reset(); reference.R[1] = native.R[1] = address;
            native.UseCompiledBlocks = native.UseFullX64Blocks = native.UseMultiInstructionX64Blocks = true;
            native.UseNativeMappedReads = native.UseNativeTimingProfile = true;
            reference.Execute(20); native.Execute(20);
            Assert.Equal(reference.R, native.R);
            Assert.Equal(reference.ProgramCounter, native.ProgramCounter);
            Assert.Equal(reference.StatusRegister, native.StatusRegister);
            Assert.Equal(reference.Cycles, native.Cycles);
            Assert.Equal(referenceBus.Clocks, nativeBus.Clocks);
            Assert.Equal(referenceBus.EventDelivered, nativeBus.EventDelivered);
            if (dueEvent)
            {
                Assert.True(nativeBus.EventDelivered);
                Assert.Equal(0x10203040u, native.R[0]);
                return;
            }
            bool mapped = (address & 0x1fffffff) < 0x201000 && !(cacheEnabled && address < 0x20000000);
            if (mapped)
            {
                Assert.Equal(0, nativeBus.DataReads);
                Assert.True(native.GetNativeTimingProfile().MappedReadHits > 0);
            }
            else
            {
                Assert.True(nativeBus.DataReads > 0);
                Assert.Equal(0ul, native.GetNativeTimingProfile().MappedReadHits);
            }
        }
        finally
        {
            try { reference?.ClearCodeCache(); }
            finally
            {
                try { native?.ClearCodeCache(); }
                finally { try { referenceBus?.Dispose(); } finally { nativeBus?.Dispose(); } }
            }
        }
    }

    private sealed class Bus : ISh2Bus, ISh2NativeMappedBus, IDisposable
    {
        private readonly byte[] _rom = new byte[65536], _ram = new byte[8192], _control = new byte[512], _policy = [1];
        private readonly Sh2ReadPageMap _map = new();
        private readonly bool _dueEvent, _forceGc;
        private bool _collected;
            public int DataReads;
        public int Clocks;
        public bool EventDelivered;
        public bool DeferClockUpdates => true;
        public ulong NextEventCycle => _dueEvent && !EventDelivered ? 1ul : ulong.MaxValue;
        public Bus(int size, uint address, bool cacheEnabled, bool dueEvent, bool forceGc)
        {
            _dueEvent = dueEvent; _forceGc = forceGc; _control[0x92] = cacheEnabled ? (byte)1 : (byte)0;
            bool initialized = false;
            try
            {
                BinaryPrimitives.WriteUInt32BigEndian(_rom, 0x100); BinaryPrimitives.WriteUInt32BigEndian(_rom.AsSpan(4), 0x8000);
                BinaryPrimitives.WriteUInt16BigEndian(_rom.AsSpan(0x100), (ushort)(0x6010 + (size == 1 ? 0 : size == 2 ? 1 : 2)));
                BinaryPrimitives.WriteUInt16BigEndian(_rom.AsSpan(0x102), 0x001b);
                BinaryPrimitives.WriteUInt32BigEndian(_ram.AsSpan((int)(address & 8191)), 0x89abcdef);
                _map.MapRead(0x200000, 4096, _ram.AsMemory(0, 4096)); initialized = true;
            }
            finally { if (!initialized) _map.Dispose(); }
        }
        public Sh2NativeReadMap GetNativeReadMap() => new(_map.Pages, _control, _policy);
        private ReadOnlySpan<byte> Read(uint address)
        {
            address &= 0x1fffffff;
            if (address is >= 0x200000 and < 0x202000) { DataReads++; return _ram.AsSpan((int)(address - 0x200000)); }
            return _rom.AsSpan((int)(address & 65535));
        }
        public byte Read8(uint address) => Read(address)[0];
        public ushort Read16(uint address) => BinaryPrimitives.ReadUInt16BigEndian(Read(address));
        public uint Read32(uint address) => BinaryPrimitives.ReadUInt32BigEndian(Read(address));
        public ushort Fetch16(uint address) => BinaryPrimitives.ReadUInt16BigEndian(_rom.AsSpan((int)(address & 65534)));
        public bool TryMapFetch(uint address, out ReadOnlyMemory<byte> memory, out uint start, out int waitStates)
        { memory = _rom; start = 0; waitStates = 0; return address < _rom.Length; }
        public long CodeVersion(uint address) => address < _rom.Length ? 0 : -1;
        public void Write8(uint address, byte value) => throw new InvalidOperationException("This program only reads memory.");
        public void Write16(uint address, ushort value) => throw new InvalidOperationException("This program only reads memory.");
        public void Write32(uint address, uint value) => throw new InvalidOperationException("This program only reads memory.");
        public void AdvanceCycles(int clocks)
        {
            Clocks += clocks;
            if (_dueEvent && !EventDelivered && Clocks >= 1)
            { EventDelivered = true; BinaryPrimitives.WriteUInt32BigEndian(_ram.AsSpan(0x20), 0x10203040); }
            if (_forceGc && !_collected) { _collected = true; GC.Collect(); GC.WaitForPendingFinalizers(); }
        }
        public void Dispose() => _map.Dispose();
    }
}

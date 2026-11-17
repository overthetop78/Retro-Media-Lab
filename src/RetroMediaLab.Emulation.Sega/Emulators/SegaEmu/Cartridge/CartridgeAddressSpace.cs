using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

internal delegate bool CartridgeCodeWindow(uint address, out ReadOnlyMemory<byte> memory, out uint logicalBase);

internal sealed class CartridgeMemoryBank
{
    private static long _mappingVersion;
    internal static long MappingVersion => System.Threading.Volatile.Read(ref _mappingVersion);
    private static void Changed() => System.Threading.Interlocked.Increment(ref _mappingVersion);
    internal static void NotifyCodeContentChanged() => Changed();
    private bool _mappedReadsUseHandlers;
    private Memory<byte> _data;
    private Func<uint, uint>? _read8, _read16;
    private Action<uint, uint>? _write8, _write16;
    private CartridgeCodeWindow? _codeWindow;
    private Action<uint>? _codeFetchObserver;
    internal Action<uint>? CodeFetchObserver
    { get => _codeFetchObserver; set { if (ReferenceEquals(_codeFetchObserver, value)) return; _codeFetchObserver = value; Changed(); } }
    /// <summary>Optional pure instruction view. Data accesses keep their handlers.</summary>
    internal CartridgeCodeWindow? CodeWindow
    { get => _codeWindow; set { if (ReferenceEquals(_codeWindow, value)) return; _codeWindow = value; Changed(); } }
    public bool MappedReadsUseHandlers
    { get => _mappedReadsUseHandlers; set { if (_mappedReadsUseHandlers == value) return; _mappedReadsUseHandlers = value; Changed(); } }
    internal Memory<byte> Data
    { get => _data; set { if (_data.Equals(value)) return; _data = value; Changed(); } }
    internal ArrayPointer<byte> @base { get => new(Data); set => Data = value.IsNull ? default : value.Memory; }
    internal Func<uint, uint>? read8
    { get => _read8; set { if (ReferenceEquals(_read8, value)) return; _read8 = value; Changed(); } }
    internal Func<uint, uint>? read16
    { get => _read16; set { if (ReferenceEquals(_read16, value)) return; _read16 = value; Changed(); } }
    internal Action<uint, uint>? write8
    { get => _write8; set { if (ReferenceEquals(_write8, value)) return; _write8 = value; Changed(); } }
    internal Action<uint, uint>? write16
    { get => _write16; set { if (ReferenceEquals(_write16, value)) return; _write16 = value; Changed(); } }
}

internal sealed class CartridgeZ80Bank
{
    internal Func<uint, uint>? read;
    internal Action<uint, uint>? write;
}

internal sealed class CartridgeHardware
{
    internal readonly byte[] regs = new byte[4];
    internal readonly uint[] mask = new uint[4], addr = new uint[4];
    internal ushort realtec, bankshift;
    internal Func<uint, uint>? time_r, regs_r;
    internal Action<uint, uint>? time_w, regs_w;
}

internal sealed class CartridgeState
{
    internal const int MaximumRomCapacity = 0xa00000;
    private byte[] _rom;
    internal byte[] rom => _rom;
    private bool _nativeRomCodeReadOnly;
    private long _nativeRomContentVersion;
    internal long NativeRomContentVersion => System.Threading.Volatile.Read(ref _nativeRomContentVersion);
    internal void NotifyRomCodeContentChanged()
    {
        System.Threading.Interlocked.Increment(ref _nativeRomContentVersion);
        CartridgeMemoryBank.NotifyCodeContentChanged();
    }
    internal bool NativeRomCodeReadOnly
    {
        get => _nativeRomCodeReadOnly;
        set
        {
            if (_nativeRomCodeReadOnly == value) return;
            _nativeRomCodeReadOnly = value;
            CartridgeMemoryBank.NotifyCodeContentChanged();
        }
    }
    internal CartridgeState(int capacity = MaximumRomCapacity)
    {
        _rom = new byte[capacity]; CartridgeRomCode.Register(_rom, this);
    }
    internal void EnsureRomCapacity(int minimum)
    {
        if (minimum > MaximumRomCapacity) throw new ArgumentOutOfRangeException(nameof(minimum));
        if (minimum > _rom.Length)
        {
            NativeRomCodeReadOnly = false;
            Array.Resize(ref _rom, minimum);
            CartridgeRomCode.Register(_rom, this);
            CartridgeMemoryBank.NotifyCodeContentChanged();
        }
    }
    internal readonly byte[] lockrom = new byte[0x10000];
    internal readonly CartridgeHardware hw = new();
    internal Memory<byte> Base;
    internal ArrayPointer<byte> @base { get => new(Base); set => Base = value.IsNull ? default : value.Memory; }
    internal uint romsize, mask;
    internal byte special;

    internal byte ReadByte(uint address) => rom[address ^ 1];
    internal uint ReadLong(uint address) =>
        ((uint)rom[address + 1] << 24) | ((uint)rom[address] << 16) | ((uint)rom[address + 3] << 8) | rom[address + 2];
}

internal sealed class CartridgeAddressSpace
{
    private CartridgeMemoryBank[]? _main;
    public CartridgeMemoryBank[] Main => _main ??= CreateMain();
    private CartridgeZ80Bank[]? _z80;
    public CartridgeZ80Bank[] Z80 => _z80 ??= CreateZ80();
    public void Clear()
    {
        if (_main is not null)
            foreach (var bank in _main) { bank.Data = default; bank.read8 = bank.read16 = null; bank.write8 = bank.write16 = null; }
        if (_z80 is not null)
            foreach (var bank in _z80) { bank.read = null; bank.write = null; }
    }
    private static CartridgeMemoryBank[] CreateMain()
    {
        CartridgeMemoryBank[] banks = new CartridgeMemoryBank[256];
        for (int i = 0; i < banks.Length; i++) banks[i] = new();
        return banks;
    }
    private static CartridgeZ80Bank[] CreateZ80()
    {
        CartridgeZ80Bank[] banks = new CartridgeZ80Bank[256];
        for (int i = 0; i < banks.Length; i++) banks[i] = new();
        return banks;
    }
}

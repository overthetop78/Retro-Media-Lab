using System.Buffers;

namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction.Compilation;

internal sealed class Z80X64MemoryMap : IDisposable
{
    internal const int ReadFunction = 192, WriteFunction = 193, InFunction = 194, OutFunction = 195;
    internal const int Sz = 196, SzBit = 197, Szp = 198, Inc = 199, Dec = 200, Add = 201, Sub = 202, Daa = 203;
    internal const int Links = 204, LastFetch = 205, Fault = 206;
    internal const int OpcodeFunction = 207;
    private readonly nint[] _table = new nint[208];
    private readonly MemoryHandle[] _pins = new MemoryHandle[192];
    private readonly ReadOnlyMemory<byte>[] _windows = new ReadOnlyMemory<byte>[192];
    private readonly bool[] _pinned = new bool[192];
    private readonly List<MemoryHandle> _auxiliaryPins = [];
    private MemoryHandle _tablePin;
    private bool _disposed, _versionKnown;
    private long _version;
    public unsafe nint Pointer => (nint)_tablePin.Pointer;
    public byte FetchByte { get => unchecked((byte)_table[LastFetch]); set => _table[LastFetch] = value; }
    public void MarkFault() => _table[Fault] = 1;
    public void ClearFault() => _table[Fault] = 0;
    public Z80X64MemoryMap(nint[] functions, byte[][] flags, nint links)
    {
        _tablePin = _table.AsMemory().Pin();
        bool ready = false;
        try
        {
            for (int i = 0; i < 4; i++) _table[ReadFunction + i] = functions[i];
            _table[OpcodeFunction] = functions[4];
            for (int i = 0; i < 7; i++) Pin(flags[i].AsMemory(), Sz + i);
            Pin(BuildDaa(flags[2]).AsMemory(), Daa); _table[Links] = links; ready = true;
        }
        finally { if (!ready) Dispose(); }
    }
    private unsafe void Pin<T>(Memory<T> memory, int slot) where T : unmanaged
    {
        MemoryHandle pin = memory.Pin(); bool owned = false;
        try { _auxiliaryPins.Add(pin); owned = true; _table[slot] = (nint)pin.Pointer; }
        finally { if (!owned) pin.Dispose(); }
    }
    public void Refresh(ICpuZ80Bus bus, Memory<byte>[] readMap)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var native = bus as ICpuZ80NativeMemoryBus;
        long version = 0;
        bool versioned = native is not null && native.TryGetNativeMemoryVersion(out version);
        if (versioned && _versionKnown && version == _version) return;
        _versionKnown = false;
        for (int page = 0; page < 64; page++)
        {
            SetWindow(page, readMap[page]);
            ReadOnlyMemory<byte> read = default; Memory<byte> write = default;
            native?.GetNativeMemory(page, out read, out write);
            SetWindow(64 + page, read); SetWindow(128 + page, write);
        }
        _version = version; _versionKnown = versioned;
    }
    private unsafe void SetWindow(int slot, ReadOnlyMemory<byte> memory)
    {
        if (memory.Length >= 1024) memory = memory[..1024]; else memory = default;
        if (_windows[slot].Equals(memory)) return;
        if (_pinned[slot]) { _pinned[slot] = false; _pins[slot].Dispose(); }
        _pins[slot] = default; _windows[slot] = default; _table[slot] = 0;
        if (memory.IsEmpty) return;
        var pin = memory.Pin(); _pins[slot] = pin; _pinned[slot] = true; _windows[slot] = memory;
        _table[slot] = (nint)pin.Pointer;
    }
    private static ushort[] BuildDaa(byte[] szp)
    {
        var result = new ushort[65536];
        for (int a = 0; a < 256; a++) for (int f = 0; f < 256; f++)
        {
            int value = a;
            if ((f & 2) != 0)
            {
                if ((f & 16) != 0 || (a & 15) > 9) value = (value - 6) & 255;
                if ((f & 1) != 0 || a > 0x99) value = (value - 0x60) & 255;
            }
            else
            {
                if ((f & 16) != 0 || (a & 15) > 9) value = (value + 6) & 255;
                if ((f & 1) != 0 || a > 0x99) value = (value + 0x60) & 255;
            }
            int flags = (f & 3) | (a > 0x99 ? 1 : 0) | ((a ^ value) & 16) | szp[value];
            result[(a << 8) | f] = unchecked((ushort)((value << 8) | flags));
        }
        return result;
    }
    public void Dispose()
    {
        if (_disposed) return; _disposed = true;
        List<Exception>? errors = null;
        try
        {
            for (int i = 0; i < _pins.Length; i++) if (_pinned[i])
            {
                _pinned[i] = false;
                try { _pins[i].Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
            }
            foreach (var pin in _auxiliaryPins)
                try { pin.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
        }
        finally { Array.Clear(_windows); Array.Clear(_table); _auxiliaryPins.Clear(); _tablePin.Dispose(); }
        if (errors is not null) throw new AggregateException("Unable to release every native Z80 pin.", errors);
    }
}

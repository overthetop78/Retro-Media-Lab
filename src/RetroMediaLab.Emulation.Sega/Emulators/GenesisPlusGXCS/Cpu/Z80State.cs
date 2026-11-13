namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cpu;

internal struct Z80Pair
{
    private uint _data;
    public int Data { readonly get => unchecked((int)_data); set => _data = unchecked((uint)value); }
    public int Word { readonly get => (int)(_data & 0xffff); set => _data = (_data & 0xffff0000) | ((uint)value & 0xffff); }
    public int Low { readonly get => (int)(_data & 255); set => _data = (_data & 0xffffff00) | ((uint)value & 255); }
    public int High { readonly get => (int)(_data >> 8 & 255); set => _data = (_data & 0xffff00ff) | (((uint)value & 255) << 8); }
}

internal struct Z80State
{
    public Z80Pair pc, sp, af, bc, de, hl, ix, iy, wz;
    public Z80Pair af2, bc2, de2, hl2;
    public int r, r2, iff1, iff2, halt, im, i;
    public int nmi_state, nmi_pending, irq_state, after_ei;
    public uint cycles;
    public object? daisy;
    public Func<int, int>? irq_callback;
}

internal interface IZ80Bus
{
    Memory<byte>[] ReadMap { get; }
    byte ReadMemory(int address);
    void WriteMemory(int address, byte value);
    byte ReadPort(int port);
    void WritePort(int port, byte value);
}

using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

internal sealed class CartridgeMemoryBank
{
    internal Memory<byte> Data;
    internal ArrayPointer<byte> @base { get => new(Data); set => Data = value.IsNull ? default : value.Memory; }
    internal Func<uint, uint>? read8, read16;
    internal Action<uint, uint>? write8, write16;
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
    internal CartridgeState(int capacity = MaximumRomCapacity) { _rom = new byte[capacity]; }
    internal void EnsureRomCapacity(int minimum)
    {
        if (minimum > MaximumRomCapacity) throw new ArgumentOutOfRangeException(nameof(minimum));
        if (minimum > _rom.Length) Array.Resize(ref _rom, minimum);
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

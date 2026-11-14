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
    internal readonly byte[] rom = new byte[10485760];
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
    public readonly CartridgeMemoryBank[] Main = CreateMain();
    public readonly CartridgeZ80Bank[] Z80 = CreateZ80();
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


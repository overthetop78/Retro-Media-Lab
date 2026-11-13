using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed partial class Svp
{
    public const uint PmcHaveAddress = 1, PmcSet = 2, Hang = 0x1000, WaitPm0 = 0x2000, Wait30Fe06 = 0x4000, Wait30Fe08 = 0x8000, WaitMask = 0xf000;
    private const uint FlagL = 0x1000, FlagZ = 0x2000, FlagV = 0x4000, FlagN = 0x8000;
    private readonly byte[] _cartridge;
    private int _pc;
    private int _cycles;
    private Span<uint> GeneralRegisters => MemoryMarshal.Cast<byte, uint>(_cartridge.AsSpan(0x240400, 64));
    private Span<byte> Pointers => _cartridge.AsSpan(0x240440, 8);
    private Span<ushort> InternalRam => MemoryMarshal.Cast<byte, ushort>(_cartridge.AsSpan(0x240000, 1024));
    private Span<ushort> Stack => MemoryMarshal.Cast<byte, ushort>(_cartridge.AsSpan(0x240448, 12));
    private Span<uint> Pmac => MemoryMarshal.Cast<byte, uint>(_cartridge.AsSpan(0x240454, 48));
    public Memory<byte> InstructionRamRom => _cartridge.AsMemory(0x200000, 0x20000);
    public Memory<byte> Dram => _cartridge.AsMemory(0x220000, 0x20000);
    public Memory<byte> ProcessorState => _cartridge.AsMemory(0x240000, 1280);
    public uint EmulationStatus
    {
        get => BinaryPrimitives.ReadUInt32LittleEndian(_cartridge.AsSpan(0x240484, 4));
        set => BinaryPrimitives.WriteUInt32LittleEndian(_cartridge.AsSpan(0x240484, 4), value);
    }

    public Svp(byte[] cartridgeBuffer)
    {
        if (cartridgeBuffer.Length < 0x240500) throw new ArgumentException("The cartridge buffer must include the SVP memory area.", nameof(cartridgeBuffer));
        _cartridge = cartridgeBuffer;
        _cartridge.AsSpan(0x200000, 0x40500).Clear();
    }

    public void Reset()
    {
        _cartridge.AsSpan(0x800, 0x20000 - 0x800).CopyTo(_cartridge.AsSpan(0x200800));
        EmulationStatus = 0;
        GeneralRegisters[0] = 0xffff0000;
        SetHigh(6, 0x400);
        SetHigh(5, 0);
        SetHigh(4, 0);
    }

    public void WriteDram(uint address, uint data)
    {
        WriteWord(_cartridge.AsSpan(0x220000), (int)(address & 0x1fffe) >> 1, data);
        if (data != 0)
        {
            if (address == 0x30fe06) EmulationStatus &= ~Wait30Fe06;
            else if (address == 0x30fe08) EmulationStatus &= ~Wait30Fe08;
        }
    }

    public uint ReadCell1(uint address)
    {
        address = (address & 0xe002) | ((address & 0x7c) << 6) | ((address & 0x1f80) >> 5);
        return ReadWord(_cartridge.AsSpan(0x220000), (int)address >> 1);
    }
    public uint ReadCell2(uint address)
    {
        address = (address & 0xf002) | ((address & 0x3c) << 6) | ((address & 0xfc0) >> 4);
        return ReadWord(_cartridge.AsSpan(0x220000), (int)address >> 1);
    }
    public uint ReadCellByte(uint address)
    {
        uint data = address >> 16 == 0x39 ? ReadCell1(address) : ReadCell2(address);
        return (address & 1) != 0 ? data & 255 : data >> 8;
    }

    public uint HostReadExchange() => GetHigh(11);
    public uint HostReadStatus() { uint data = GetHigh(8); SetHigh(8, data & ~1u); return data; }
    public void HostWriteExchange(uint data)
    {
        SetHigh(11, data);
        SetHigh(8, GetHigh(8) | 2);
        EmulationStatus &= ~WaitPm0;
    }

    private uint GetHigh(int index) => GeneralRegisters[index] >> 16;
    private void SetHigh(int index, uint data) => GeneralRegisters[index] = (GeneralRegisters[index] & 0xffff) | (data << 16);
    private uint Accumulator { get => GeneralRegisters[3]; set => GeneralRegisters[3] = value; }
    private uint Status { get => GetHigh(4); set => SetHigh(4, value); }
    private uint Pmc { get => GeneralRegisters[14]; set => GeneralRegisters[14] = value; }
    private int PreviousByteOffset => _pc * 2 - 2;
    private uint Fetch() => ReadWord(_cartridge.AsSpan(0x200000), _pc++);
    private uint PreviousOpcode => ReadWord(_cartridge.AsSpan(0x200000), _pc - 1);
    private static uint ReadWord(ReadOnlySpan<byte> memory, int index) => BinaryPrimitives.ReadUInt16LittleEndian(memory.Slice(index * 2, 2));
    private static void WriteWord(Span<byte> memory, int index, uint value) => BinaryPrimitives.WriteUInt16LittleEndian(memory.Slice(index * 2, 2), unchecked((ushort)value));

    private uint ReadStack()
    {
        SetHigh(5, unchecked(GetHigh(5) - 1));
        if (unchecked((short)GetHigh(5)) < 0) SetHigh(5, 5);
        return Stack[(int)GetHigh(5)];
    }
    private void WriteStack(uint data)
    {
        if (GetHigh(5) >= 6) SetHigh(5, 0);
        int index = (int)GetHigh(5);
        SetHigh(5, (uint)(index + 1));
        Stack[index] = unchecked((ushort)data);
    }
    private uint ReadProduct()
    {
        GeneralRegisters[7] = unchecked((uint)((short)GetHigh(1) * (short)GetHigh(2) * 2));
        return GetHigh(7);
    }
    private void WritePc(uint value) { _pc = unchecked((int)value); _cycles--; }
    private uint ReadPmc()
    {
        if ((EmulationStatus & PmcHaveAddress) != 0)
        {
            EmulationStatus = (EmulationStatus | PmcSet) & ~PmcHaveAddress;
            uint low = Pmc & 0xffff;
            return ((low << 4) & 0xfff0) | ((low >> 4) & 15);
        }
        EmulationStatus |= PmcHaveAddress;
        return Pmc & 0xffff;
    }
    private void WritePmc(uint value)
    {
        if ((EmulationStatus & PmcHaveAddress) != 0)
        {
            EmulationStatus = (EmulationStatus | PmcSet) & ~PmcHaveAddress;
            Pmc = (Pmc & 0xffff) | (value << 16);
        }
        else
        {
            EmulationStatus |= PmcHaveAddress;
            Pmc = (Pmc & 0xffff0000) | (value & 0xffff);
        }
    }
    private uint ReadAccumulatorLow()
    {
        if (PreviousOpcode == 15) EmulationStatus &= ~(PmcSet | PmcHaveAddress);
        return Accumulator & 0xffff;
    }

    private uint ReadRegister(int index)
    {
        if (index <= 4) return GetHigh(index);
        return index switch
        {
            5 => ReadStack(), 6 => (uint)_pc, 7 => ReadProduct(),
            8 => ReadPm(0), 9 => ReadPm(1), 10 => ReadPm(2), 11 => ReadPm(3), 12 => ReadPm(4),
            14 => ReadPmc(), 15 => ReadAccumulatorLow(), _ => 0
        };
    }
    private void WriteRegister(int index, uint data)
    {
        switch (index)
        {
            case 1: case 2: case 3: case 4: SetHigh(index, data); break;
            case 5: WriteStack(data); break;
            case 6: WritePc(data); break;
            case 8: case 9: case 10: case 11: case 12: WritePm(index - 8, data); break;
            case 14: WritePmc(data); break;
            case 15: Accumulator = (Accumulator & 0xffff0000) | (data & 0xffff); break;
        }
    }
}

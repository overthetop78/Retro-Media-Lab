using System.Buffers.Binary;
using System.Runtime.InteropServices;
using RetroMediaLab.Chips.CPU.Samsung.SSP1601;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

/// <summary>SVP cartridge memory controller and host mappings around a reusable SSP1601.</summary>
internal sealed partial class Svp : ISsp1601Bus
{
    public const uint PmcHaveAddress = 1, PmcSet = 2, Hang = 0x1000, WaitPm0 = 0x2000, Wait30Fe06 = 0x4000, Wait30Fe08 = 0x8000, WaitMask = 0xf000;
    private readonly byte[] _cartridge;
    private readonly Ssp1601 _cpu;
    private Span<uint> GeneralRegisters => MemoryMarshal.Cast<byte,uint>(_cartridge.AsSpan(0x240400,64));
    private Span<uint> Pmac => MemoryMarshal.Cast<byte,uint>(_cartridge.AsSpan(0x240454,48));
    public Memory<byte> InstructionRamRom => _cartridge.AsMemory(0x200000,0x20000);
    public Memory<byte> Dram => _cartridge.AsMemory(0x220000,0x20000);
    public Memory<byte> ProcessorState
    {
        get
        {
            // External PM registers belong to this board; do not overwrite them with CPU-reserved slots.
            _cpu.RegisterState.Span[..0x420].CopyTo(_cartridge.AsSpan(0x240000));
            _cpu.RegisterState.Span.Slice(0x440,0x14).CopyTo(_cartridge.AsSpan(0x240440));
            return _cartridge.AsMemory(0x240000,1280);
        }
    }
    public uint EmulationStatus
    {
        get => BinaryPrimitives.ReadUInt32LittleEndian(_cartridge.AsSpan(0x240484,4));
        set => BinaryPrimitives.WriteUInt32LittleEndian(_cartridge.AsSpan(0x240484,4),value);
    }
    public Svp(byte[] cartridgeBuffer)
    {
        if (cartridgeBuffer.Length < 0x240500) throw new ArgumentException("The cartridge buffer does not include the SVP area.",nameof(cartridgeBuffer));
        _cartridge = cartridgeBuffer; _cartridge.AsSpan(0x200000,0x40500).Clear(); _cpu = new(this);
    }
    public void Reset()
    {
        _cartridge.AsSpan(0x800,0x20000 - 0x800).CopyTo(_cartridge.AsSpan(0x200800));
        EmulationStatus = 0; _cpu.Reset(0x400);
    }
    public void Run(int cycles) => _cpu.Run(cycles);
    public void RestoreProcessorState() => _cpu.LoadContext(_cartridge.AsSpan(0x240000,Ssp1601.ContextSize));
    public void WriteDram(uint address, uint data)
    {
        WriteWord(_cartridge.AsSpan(0x220000),(int)(address & 0x1fffe) >> 1,data);
        if (data == 0) return;
        if (address == 0x30fe06) EmulationStatus &= ~Wait30Fe06;
        else if (address == 0x30fe08) EmulationStatus &= ~Wait30Fe08;
    }
    public uint ReadCell1(uint address)
    {
        address = (address & 0xe002) | (address & 0x7c) << 6 | (address & 0x1f80) >> 5;
        return ReadWord(_cartridge.AsSpan(0x220000),(int)address >> 1);
    }
    public uint ReadCell2(uint address)
    {
        address = (address & 0xf002) | (address & 0x3c) << 6 | (address & 0xfc0) >> 4;
        return ReadWord(_cartridge.AsSpan(0x220000),(int)address >> 1);
    }
    public uint ReadCellByte(uint address)
    {
        uint data = address >> 16 == 0x39 ? ReadCell1(address) : ReadCell2(address);
        return (address & 1) != 0 ? data & 255 : data >> 8;
    }
    public uint HostReadExchange() => GetHigh(11);
    public uint HostReadStatus() { uint data = GetHigh(8); SetHigh(8,data & ~1u); return data; }
    public void HostWriteExchange(uint data) { SetHigh(11,data); SetHigh(8,GetHigh(8) | 2); EmulationStatus &= ~WaitPm0; }
    private uint GetHigh(int index) => GeneralRegisters[index] >> 16;
    private void SetHigh(int index, uint data) => GeneralRegisters[index] = (GeneralRegisters[index] & 0xffff) | data << 16;
    private uint Status => _cpu.StatusRegister;
    private uint Pmc { get => GeneralRegisters[14]; set => GeneralRegisters[14] = value; }
    private int PreviousByteOffset => _cpu.ProgramCounter * 2 - 2;
    private uint PreviousOpcode => ReadWord(InstructionRamRom.Span,_cpu.PreviousProgramCounter);
    private static uint ReadWord(ReadOnlySpan<byte> memory, int index) => BinaryPrimitives.ReadUInt16LittleEndian(memory.Slice(index * 2,2));
    private static void WriteWord(Span<byte> memory, int index, uint value) => BinaryPrimitives.WriteUInt16LittleEndian(memory.Slice(index * 2,2),(ushort)value);
    private uint ReadPmc()
    {
        if ((EmulationStatus & PmcHaveAddress) != 0)
        {
            EmulationStatus = (EmulationStatus | PmcSet) & ~PmcHaveAddress;
            uint low = Pmc & 0xffff; return low << 4 & 0xfff0 | low >> 4 & 15;
        }
        EmulationStatus |= PmcHaveAddress; return Pmc & 0xffff;
    }
    private void WritePmc(uint value)
    {
        if ((EmulationStatus & PmcHaveAddress) != 0)
        {
            EmulationStatus = (EmulationStatus | PmcSet) & ~PmcHaveAddress; Pmc = Pmc & 0xffff | value << 16;
        }
        else { EmulationStatus |= PmcHaveAddress; Pmc = Pmc & 0xffff0000 | value & 0xffff; }
    }
    ushort ISsp1601Bus.ReadProgramWord(ushort address) => (ushort)ReadWord(InstructionRamRom.Span,address);
    uint ISsp1601Bus.ReadExternalRegister(int register) => register == 14 ? ReadPmc() : register is >= 8 and <= 12 ? ReadPm(register - 8) : 0;
    void ISsp1601Bus.WriteExternalRegister(int register, uint value)
    {
        if (register == 14) WritePmc(value); else if (register is >= 8 and <= 12) WritePm(register - 8,value);
    }
    void ISsp1601Bus.AccumulatorLowRead(ushort previousInstruction) { if (previousInstruction == 15) EmulationStatus &= ~(PmcSet | PmcHaveAddress); }
    bool ISsp1601Bus.ShouldYield => (EmulationStatus & WaitMask) != 0;
}

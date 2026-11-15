using System.Buffers.Binary;
using RetroMediaLab.Chips.CPU.Motorola.M68000.Execution;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Buses;

/// <summary>Borrowed bank map; the machine owns its memory and mapped devices.</summary>
internal abstract class MappedCpuBus(CartridgeMemoryBank[] banks) : ICpu68000Bus
{
    public uint Read8(uint address)
    {
        CartridgeMemoryBank bank = banks[(address >> 16) & 255];
        return bank.read8 is { } read ? read(address) : bank.Data.Span[(int)((address & 65535) ^ 1)];
    }
    public uint Read16(uint address)
    {
        CartridgeMemoryBank bank = banks[(address >> 16) & 255];
        return bank.read16 is { } read ? read(address) : BinaryPrimitives.ReadUInt16LittleEndian(bank.Data.Span[(int)(address & 65534)..]);
    }
    public void Write8(uint address, uint value)
    {
        CartridgeMemoryBank bank = banks[(address >> 16) & 255];
        if (bank.write8 is { } write) write(address, value);
        else bank.Data.Span[(int)((address & 65535) ^ 1)] = (byte)value;
    }
    public void Write16(uint address, uint value)
    {
        CartridgeMemoryBank bank = banks[(address >> 16) & 255];
        if (bank.write16 is { } write) write(address, value);
        else BinaryPrimitives.WriteUInt16LittleEndian(bank.Data.Span[(int)(address & 65534)..], (ushort)value);
    }
    public uint ReadMapped8(uint address) => banks[(address >> 16) & 255].Data.Span[(int)((address & 65535) ^ 1)];
    public uint ReadMapped16(uint address) => BinaryPrimitives.ReadUInt16LittleEndian(banks[(address >> 16) & 255].Data.Span[(int)(address & 65534)..]);
    public abstract void AcknowledgeInterrupt(uint level);
    public virtual bool IsRefreshWindowElapsed(int cycles, int deadline) => cycles >= deadline;
}

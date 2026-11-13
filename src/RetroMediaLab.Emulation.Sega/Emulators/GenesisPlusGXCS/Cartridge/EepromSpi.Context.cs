using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed partial class EepromSpi
{
    public const int ContextSize = 16;
    private static void WriteT_EEPROM_SPI(T_EEPROM_SPI value, Span<byte> state)
    {
        state[0] = unchecked((byte)value.cs);
        state[1] = unchecked((byte)value.clk);
        state[2] = unchecked((byte)value.sampleOutput);
        state[3] = unchecked((byte)value.status);
        state[4] = unchecked((byte)value.opcode);
        state[5] = unchecked((byte)value.buffer);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(6), value.addr);
        BinaryPrimitives.WriteUInt32LittleEndian(state.Slice(8), value.cycles);
        BinaryPrimitives.WriteInt32LittleEndian(state.Slice(12), value.state);
    }
    private static void ReadT_EEPROM_SPI(T_EEPROM_SPI value, ReadOnlySpan<byte> state)
    {
        value.cs = unchecked((byte)state[0]);
        value.clk = unchecked((byte)state[1]);
        value.sampleOutput = unchecked((byte)state[2]);
        value.status = unchecked((byte)state[3]);
        value.opcode = unchecked((byte)state[4]);
        value.buffer = unchecked((byte)state[5]);
        value.addr = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(6));
        value.cycles = BinaryPrimitives.ReadUInt32LittleEndian(state.Slice(8));
        value.state = BinaryPrimitives.ReadInt32LittleEndian(state.Slice(12));
    }
}

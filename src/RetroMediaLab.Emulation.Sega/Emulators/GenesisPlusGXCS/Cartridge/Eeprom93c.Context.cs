using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed partial class Eeprom93c
{
    public const int ContextSize = 16;
    private static void WriteT_EEPROM_93C(T_EEPROM_93C value, Span<byte> state)
    {
        state[0] = unchecked((byte)value.enabled);
        state[1] = unchecked((byte)value.cs);
        state[2] = unchecked((byte)value.clk);
        state[3] = unchecked((byte)value.data);
        state[4] = unchecked((byte)value.cycles);
        state[5] = unchecked((byte)value.we);
        state[6] = unchecked((byte)value.opcode);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(8), value.buffer);
        BinaryPrimitives.WriteInt32LittleEndian(state.Slice(12), value.state);
    }
    private static void ReadT_EEPROM_93C(T_EEPROM_93C value, ReadOnlySpan<byte> state)
    {
        value.enabled = unchecked((byte)state[0]);
        value.cs = unchecked((byte)state[1]);
        value.clk = unchecked((byte)state[2]);
        value.data = unchecked((byte)state[3]);
        value.cycles = unchecked((byte)state[4]);
        value.we = unchecked((byte)state[5]);
        value.opcode = unchecked((byte)state[6]);
        value.buffer = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(8));
        value.state = BinaryPrimitives.ReadInt32LittleEndian(state.Slice(12));
    }
}

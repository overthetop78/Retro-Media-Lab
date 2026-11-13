using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed partial class EepromI2c
{
    public const int ContextSize = 28;
    private static void WriteT_I2C_SPEC(T_I2C_SPEC value, Span<byte> state)
    {
        state[0] = unchecked((byte)value.address_bits);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(2), value.size_mask);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(4), value.pagewrite_mask);
    }
    private static void ReadT_I2C_SPEC(T_I2C_SPEC value, ReadOnlySpan<byte> state)
    {
        value.address_bits = unchecked((byte)state[0]);
        value.size_mask = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(2));
        value.pagewrite_mask = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(4));
    }
    private static void WriteI2cState(I2cState value, Span<byte> state)
    {
        state[0] = unchecked((byte)value.sda);
        state[1] = unchecked((byte)value.scl);
        state[2] = unchecked((byte)value.old_sda);
        state[3] = unchecked((byte)value.old_scl);
        state[4] = unchecked((byte)value.cycles);
        state[5] = unchecked((byte)value.rw);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(6), value.device_address);
        BinaryPrimitives.WriteUInt16LittleEndian(state.Slice(8), value.word_address);
        state[10] = unchecked((byte)value.buffer);
        BinaryPrimitives.WriteInt32LittleEndian(state.Slice(12), value.state);
        WriteT_I2C_SPEC(value.spec, state.Slice(16, 6));
        state[22] = unchecked((byte)value.scl_in_bit);
        state[23] = unchecked((byte)value.sda_in_bit);
        state[24] = unchecked((byte)value.sda_out_bit);
    }
    private static void ReadI2cState(I2cState value, ReadOnlySpan<byte> state)
    {
        value.sda = unchecked((byte)state[0]);
        value.scl = unchecked((byte)state[1]);
        value.old_sda = unchecked((byte)state[2]);
        value.old_scl = unchecked((byte)state[3]);
        value.cycles = unchecked((byte)state[4]);
        value.rw = unchecked((byte)state[5]);
        value.device_address = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(6));
        value.word_address = BinaryPrimitives.ReadUInt16LittleEndian(state.Slice(8));
        value.buffer = unchecked((byte)state[10]);
        value.state = BinaryPrimitives.ReadInt32LittleEndian(state.Slice(12));
        ReadT_I2C_SPEC(value.spec, state.Slice(16, 6));
        value.scl_in_bit = unchecked((byte)state[22]);
        value.sda_in_bit = unchecked((byte)state[23]);
        value.sda_out_bit = unchecked((byte)state[24]);
    }
}

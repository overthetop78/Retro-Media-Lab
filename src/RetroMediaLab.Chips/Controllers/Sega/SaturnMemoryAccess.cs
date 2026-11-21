// Adapted from Yabause memory.h.
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Buffers.Binary;
using System.IO;

namespace RetroMediaLab.Chips.Controllers.Sega;

internal static class SaturnMemoryAccess
{
    internal static byte T1ReadByte(ReadOnlySpan<byte> memory, uint address) => memory[(int)address];
    internal static ushort T1ReadWord(ReadOnlySpan<byte> memory, uint address) =>
        BinaryPrimitives.ReadUInt16BigEndian(memory[(int)address..]);
    internal static uint T1ReadLong(ReadOnlySpan<byte> memory, uint address) =>
        BinaryPrimitives.ReadUInt32BigEndian(memory[(int)address..]);

    internal static void T1WriteByte(Span<byte> memory, uint address, byte value) => memory[(int)address] = value;
    internal static void T1WriteWord(Span<byte> memory, uint address, ushort value) =>
        BinaryPrimitives.WriteUInt16BigEndian(memory[(int)address..], value);
    internal static void T1WriteLong(Span<byte> memory, uint address, uint value) =>
        BinaryPrimitives.WriteUInt32BigEndian(memory[(int)address..], value);

    internal static byte T2ReadByte(ReadOnlySpan<byte> memory, uint address) =>
        memory[(int)(BitConverter.IsLittleEndian ? address ^ 1 : address)];

    internal static ushort T2ReadWord(ReadOnlySpan<byte> memory, uint address) =>
        BitConverter.ToUInt16(memory[(int)address..]);

    internal static uint T2ReadLong(ReadOnlySpan<byte> memory, uint address)
    {
        uint value = BitConverter.ToUInt32(memory[(int)address..]);
        return BitConverter.IsLittleEndian ? (value << 16) | (value >> 16) : value;
    }

    internal static void T2WriteByte(Span<byte> memory, uint address, byte value) =>
        memory[(int)(BitConverter.IsLittleEndian ? address ^ 1 : address)] = value;

    internal static void T2WriteWord(Span<byte> memory, uint address, ushort value) =>
        BitConverter.TryWriteBytes(memory.Slice((int)address, sizeof(ushort)), value);

    internal static void T2WriteLong(Span<byte> memory, uint address, uint value)
    {
        if (BitConverter.IsLittleEndian)
            value = (value << 16) | (value >> 16);
        BitConverter.TryWriteBytes(memory.Slice((int)address, sizeof(uint)), value);
    }

    internal static byte T3ReadByte(ReadOnlySpan<byte> memory, uint address) =>
        memory[BitConverter.IsLittleEndian ? memory.Length - (int)address - 1 : (int)address];

    internal static ushort T3ReadWord(ReadOnlySpan<byte> memory, uint address) =>
        BitConverter.ToUInt16(memory[(BitConverter.IsLittleEndian ? memory.Length - (int)address - 2 : (int)address)..]);

    internal static uint T3ReadLong(ReadOnlySpan<byte> memory, uint address) =>
        BitConverter.ToUInt32(memory[(BitConverter.IsLittleEndian ? memory.Length - (int)address - 4 : (int)address)..]);

    internal static void T3WriteByte(Span<byte> memory, uint address, byte value) =>
        memory[BitConverter.IsLittleEndian ? memory.Length - (int)address - 1 : (int)address] = value;

    internal static void T3WriteWord(Span<byte> memory, uint address, ushort value)
    {
        int offset = BitConverter.IsLittleEndian ? memory.Length - (int)address - 2 : (int)address;
        BitConverter.TryWriteBytes(memory.Slice(offset, sizeof(ushort)), value);
    }

    internal static void T3WriteLong(Span<byte> memory, uint address, uint value)
    {
        int offset = BitConverter.IsLittleEndian ? memory.Length - (int)address - 4 : (int)address;
        BitConverter.TryWriteBytes(memory.Slice(offset, sizeof(uint)), value);
    }

    internal static bool T123Load(Span<byte> memory, int type, Stream? input)
    {
        if (input is null || type is < 1 or > 3)
            return false;

        try
        {
            long size = input.Length;
            if (size > memory.Length)
                return false;

            byte[] buffer = new byte[(int)size];
            input.Position = 0;
            input.ReadExactly(buffer);

            switch (type)
            {
                case 1:
                    for (uint i = 0; i < buffer.Length; i++)
                        T1WriteByte(memory, i, buffer[i]);
                    break;
                case 2:
                    for (uint i = 0; i < buffer.Length; i++)
                        T2WriteByte(memory, i, buffer[i]);
                    break;
                case 3:
                    for (uint i = 0; i < buffer.Length; i++)
                        T3WriteByte(memory, i, buffer[i]);
                    break;
            }
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    internal static bool T123Save(ReadOnlySpan<byte> memory, int type, Stream? output)
    {
        if (output is null)
            return true;

        byte[] buffer = new byte[memory.Length];
        switch (type)
        {
            case 1:
                for (uint i = 0; i < buffer.Length; i++)
                    buffer[i] = T1ReadByte(memory, i);
                break;
            case 2:
                for (uint i = 0; i < buffer.Length; i++)
                    buffer[i] = T2ReadByte(memory, i);
                break;
            case 3:
                for (uint i = 0; i < buffer.Length; i++)
                    buffer[i] = T3ReadByte(memory, i);
                break;
            default:
                return false;
        }

        try
        {
            output.Write(buffer);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }
}

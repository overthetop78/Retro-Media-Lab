// Copyright 2003-2004 Stephane Dallongeville
// Translated from Yabause c68k/c68kmac.inc.
// SPDX-License-Identifier: GPL-2.0-or-later

namespace RetroMediaLab.Chips.CPU.Motorola.M68000;

internal sealed partial class SaturnC68k
{
    private static uint Lsl(uint value, int count) => value << count;
    private static uint Lsr(uint value, int count) => value >> count;
    private static uint Lsl32(uint value, int count) => count < 32 ? value << count : 0;
    private static uint Lsr32(uint value, int count) => count < 32 ? value >> count : 0;

    private static uint Rol8(uint value, int count) => Lsl(value, count) | Lsr(value, 8 - count);
    private static uint Rol9(uint value, int count) => Lsl(value, count) | Lsr(value, 9 - count);
    private static uint Rol16(uint value, int count) => Lsl(value, count) | Lsr(value, 16 - count);
    private static uint Rol17(uint value, int count) => Lsl(value, count) | Lsr(value, 17 - count);
    private static uint Rol32(uint value, int count) => Lsl32(value, count) | Lsr32(value, 32 - count);
    private static uint Rol33(uint value, int count) => Lsl32(value, count) | Lsr32(value, 33 - count);

    private static uint Ror8(uint value, int count) => Lsr(value, count) | Lsl(value, 8 - count);
    private static uint Ror9(uint value, int count) => Lsr(value, count) | Lsl(value, 9 - count);
    private static uint Ror16(uint value, int count) => Lsr(value, count) | Lsl(value, 16 - count);
    private static uint Ror17(uint value, int count) => Lsr(value, count) | Lsl(value, 17 - count);
    private static uint Ror32(uint value, int count) => Lsr32(value, count) | Lsl32(value, 32 - count);
    private static uint Ror33(uint value, int count) => Lsr32(value, count) | Lsl32(value, 33 - count);

    private ushort Next(ref uint pc, int cycles)
    {
        PreIo(cycles);
        ushort opcode = FetchWord(pc);
        pc = unchecked(pc + 2);
        return opcode;
    }

    private static bool Ret(ref int cycles, int instructionCycles)
    {
        cycles -= instructionCycles;
        return cycles > 0;
    }

    private void SetExecutionProgramCounter(ref uint pc, uint value)
    {
        FetchPointer bank = _fetch[(value >> FetchShift) & FetchMask];
        _basePc = new FetchPointer(bank.Buffer, unchecked(bank.Bias - (value & 0xFF000000)));
        pc = unchecked(value + _basePc.Bias);
    }

    private void PreIo(int cycles) => _cycleIo = cycles;
    private void PostIo(ref int cycles) => cycles = _cycleIo;

    private uint ReadByteFast(uint address) => _readByte(address) & 0xFF;
    private uint ReadWordFast(uint address) => _readWord(address) & 0xFFFF;

    private uint ReadLongFast(uint address)
    {
        uint value = _readWord(address) << 16;
        return value | (_readWord(unchecked(address + 2)) & 0xFFFF);
    }

    private uint ReadLongDecrementFast(uint address)
    {
        uint value = _readWord(unchecked(address + 2)) & 0xFFFF;
        return value | (_readWord(address) << 16);
    }

    private int ReadSignedByteFast(uint address) => unchecked((sbyte)_readByte(address));
    private int ReadSignedWordFast(uint address) => unchecked((short)_readWord(address));

    private int ReadSignedLongFast(uint address)
    {
        uint value = _readWord(address) << 16;
        value |= _readWord(unchecked(address + 2)) & 0xFFFF;
        return unchecked((int)value);
    }

    private int ReadSignedLongDecrementFast(uint address)
    {
        uint value = _readWord(unchecked(address + 2)) & 0xFFFF;
        value |= _readWord(address) << 16;
        return unchecked((int)value);
    }

    private void WriteByteFast(uint address, uint data) => _writeByte(address, data);
    private void WriteWordFast(uint address, uint data) => _writeWord(address, data);

    private void WriteLongFast(uint address, uint data)
    {
        _writeWord(address, data >> 16);
        _writeWord(unchecked(address + 2), data & 0xFFFF);
    }

    private void WriteLongDecrementFast(uint address, uint data)
    {
        _writeWord(unchecked(address + 2), data & 0xFFFF);
        _writeWord(address, data >> 16);
    }

    private void PushWordFast(uint data)
    {
        _address[7] = unchecked(_address[7] - 2);
        _writeWord(_address[7], data);
    }

    private ushort PopWordFast()
    {
        ushort data = unchecked((ushort)_readWord(_address[7]));
        _address[7] = unchecked(_address[7] + 2);
        return data;
    }

    private void PushLongFast(uint data)
    {
        _address[7] = unchecked(_address[7] - 4);
        _writeWord(unchecked(_address[7] + 2), data & 0xFFFF);
        _writeWord(_address[7], data >> 16);
    }

    private uint PopLongFast()
    {
        uint data = _readWord(_address[7]) << 16;
        data |= _readWord(unchecked(_address[7] + 2)) & 0xFFFF;
        _address[7] = unchecked(_address[7] + 4);
        return data;
    }

    private uint FetchByte(uint pc) => (uint)(FetchWord(pc) & 0xFF);

    private ushort FetchWord(uint pc)
    {
        if (_basePc.Buffer is { } buffer && (ulong)pc + 1 < (uint)buffer.Length)
            return BitConverter.ToUInt16(buffer.AsSpan((int)pc));

        return (ushort)ReadWordFast(unchecked(pc - _basePc.Bias) & 0xFFFFFF);
    }

    private uint FetchLong(uint pc)
        => ((uint)FetchWord(pc) << 16) | FetchWord(unchecked(pc + 2));

    private void DecodeExtensionWord(ref uint pc, ref uint address)
    {
        uint extension = FetchWord(pc);
        pc = unchecked(pc + 2);
        address = unchecked(address + (uint)(sbyte)extension);

        // C68K indexes the consecutive D and A register storage as a single array.
        uint index = extension >> 12;
        uint register = index < 8 ? _data[index] : _address[index - 8];
        if ((extension & 0x0800) != 0)
            address = unchecked(address + register);
        else
            address = unchecked(address + (uint)(short)register);
    }

    private uint GetConditionCodes()
        => ((_carry >> CarryShift) & 1)
            | ((_overflow >> (OverflowShift - 1)) & 2)
            | ((_notZero == 0 ? 1u : 0u) << 2)
            | ((_negative >> (NegativeShift - 3)) & 8)
            | ((_extend >> (ExtendShift - 4)) & 0x10);

    private uint GetStatusRegisterBits() => _supervisor | (_interruptMask << 8) | GetConditionCodes();

    private void SetConditionCodes(uint value)
    {
        _carry = value << CarryShift;
        _overflow = value << (OverflowShift - 1);
        _notZero = ~value & 4;
        _negative = value << (NegativeShift - 3);
        _extend = value << (ExtendShift - 4);
    }

    private void SetStatusRegisterBits(uint value)
    {
        SetConditionCodes(value);
        _interruptMask = (value >> 8) & 7;
        _supervisor = value & Supervisor;
    }

    private void CheckInterrupt(ref uint pc, ref int cycles, ReadOnlySpan<int> exceptionCycles)
    {
        int line = _irqLine;
        if (line == 7 || line > (int)_interruptMask)
        {
            _irqLine = 0;
            int vector = _interruptCallback(line);
            if (vector == InterruptAckAutovector)
                vector = InterruptAutovectorException + (line & 7);

            cycles -= exceptionCycles[vector];
            if (_supervisor == 0)
            {
                uint originalUsp = _usp;
                _usp = _address[7];
                _address[7] = originalUsp;
            }

            PreIo(cycles);
            PushLongFast(unchecked(pc - _basePc.Bias));
            PushWordFast(GetStatusRegisterBits());
            _supervisor = Supervisor;
            _interruptMask = (uint)line;

            uint interruptPc = ReadLongFast(unchecked((uint)(vector * 4)));
            SetExecutionProgramCounter(ref pc, interruptPc);
            PostIo(ref cycles);
        }
    }

}

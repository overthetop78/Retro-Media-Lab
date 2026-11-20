// Copyright 2003-2004 Stephane Dallongeville
// Translated from Yabause c68k/c68k.h.
// SPDX-License-Identifier: GPL-2.0-or-later

namespace RetroMediaLab.Chips.CPU.Motorola.M68000;

internal sealed partial class SaturnC68k
{
    private const int FetchBits = 8;
    private const int AddressBits = 24;
    private const int FetchShift = AddressBits - FetchBits;
    private const int FetchBankCount = 1 << FetchBits;
    private const int FetchMask = FetchBankCount - 1;

    private const int CarryShift = 8;
    private const int OverflowShift = 7;
    private const int ZeroShift = 0;
    private const int NegativeShift = 7;
    private const int ExtendShift = 8;
    private const int SupervisorShift = 13;

    private const uint Carry = 1u << CarryShift;
    private const uint Overflow = 1u << OverflowShift;
    private const uint Zero = 0;
    private const uint Negative = 1u << NegativeShift;
    private const uint Extend = 1u << ExtendShift;
    private const uint Supervisor = 1u << SupervisorShift;
    private const uint ConditionCodeMask = 0x1F;
    private const uint StatusRegisterMask = 0x2700 | ConditionCodeMask;

    private const int ResetException = 1;
    private const int BusErrorException = 2;
    private const int IllegalInstructionException = 4;
    private const int ZeroDivideException = 5;
    private const int CheckException = 6;
    private const int TrapOverflowException = 7;
    private const int PrivilegeViolationException = 8;
    private const int TraceException = 9;
    private const int LineAException = 10;
    private const int LineFException = 11;
    private const int FormatErrorException = 14;
    private const int UninitializedInterruptException = 15;
    private const int SpuriousInterruptException = 24;
    private const int InterruptAutovectorException = 24;
    private const int TrapBaseException = 32;
    private const int InterruptAckAutovector = -1;

    private const uint Running = 0x01;
    private const uint Halted = 0x02;
    private const uint Waiting = 0x04;
    private const uint Disabled = 0x10;
    private const uint Faulted = 0x40;

    // Managed representation of a fetch pointer and its address bias.
    private readonly record struct FetchPointer(byte[]? Buffer, uint Bias);

    private readonly uint[] _data = new uint[8];
    private readonly uint[] _address = new uint[8];
    private uint _carry;
    private uint _overflow;
    private uint _notZero;
    private uint _negative;
    private uint _extend;
    private uint _interruptMask;
    private uint _supervisor;
    private uint _usp;
    private uint _pc;
    private FetchPointer _basePc;
    private uint _status;
    private int _irqLine;
    private int _cycleToDo;
    private int _cycleIo;
    private int _cycleSup;
    private uint _dirty1;

    private Func<uint, uint> _readByte = ReadDummy;
    private Func<uint, uint> _readWord = ReadDummy;
    private Action<uint, uint> _writeByte = WriteDummy;
    private Action<uint, uint> _writeWord = WriteDummy;
    private Func<int, int> _interruptCallback = InterruptAckDummy;
    private Action _resetCallback = ResetDummy;
    private readonly FetchPointer[] _fetch = new FetchPointer[FetchBankCount];
}

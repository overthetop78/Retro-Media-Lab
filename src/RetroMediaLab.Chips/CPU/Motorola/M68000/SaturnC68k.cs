// Copyright 2003-2004 Stephane Dallongeville
// Translated from Yabause c68k/c68k.c.
// SPDX-License-Identifier: GPL-2.0-or-later

namespace RetroMediaLab.Chips.CPU.Motorola.M68000;

internal sealed partial class SaturnC68k
{
    internal void Initialize(Func<int, int>? interruptCallback)
    {
        ClearProcessorState();
        _dirty1 = 0;
        Array.Clear(_fetch);

        SetReadByte(ReadDummy);
        SetReadWord(ReadDummy);
        SetWriteByte(WriteDummy);
        SetWriteWord(WriteDummy);
        _interruptCallback = interruptCallback ?? InterruptAckDummy;
        _resetCallback = ResetDummy;

        _status |= Disabled;
        Execute(0);
        _status &= ~Disabled;
    }

    internal int Reset()
    {
        ClearProcessorState();
        _notZero = 1;
        _interruptMask = 7;
        _supervisor = Supervisor;

        _address[7] = ReadLong(0);
        SetProgramCounter(ReadLong(4));
        return unchecked((int)_status);
    }

    private void ClearProcessorState()
    {
        Array.Clear(_data);
        Array.Clear(_address);
        _carry = 0;
        _overflow = 0;
        _notZero = 0;
        _negative = 0;
        _extend = 0;
        _interruptMask = 0;
        _supervisor = 0;
        _usp = 0;
        _pc = 0;
        _basePc = default;
        _status = 0;
        _irqLine = 0;
        _cycleToDo = 0;
        _cycleIo = 0;
        _cycleSup = 0;
    }

    internal void SetInterruptLevel(int level)
    {
        _irqLine = level;
        if ((_status & Running) != 0)
        {
            _cycleSup = _cycleIo;
            _cycleIo = 0;
        }
        _status &= ~(Halted | Waiting);
    }

    internal int GetCyclesToDo()
    {
        if ((_status & Running) == 0)
            return -1;

        return _cycleToDo;
    }

    internal int GetCyclesRemaining()
    {
        if ((_status & Running) == 0)
            return -1;

        return _cycleIo + _cycleSup;
    }

    internal int GetCyclesDone()
    {
        if ((_status & Running) == 0)
            return -1;

        return _cycleToDo - (_cycleIo + _cycleSup);
    }

    internal void ReleaseCycles()
    {
        if ((_status & Running) != 0)
            _cycleIo = _cycleSup = 0;
    }

    internal void AddCycles(int cycles)
    {
        if ((_status & Running) != 0)
            _cycleIo -= cycles;
    }

    private static uint ReadDummy(uint address) => 0;

    private static void WriteDummy(uint address, uint data)
    {
    }

    private static int InterruptAckDummy(int level) => InterruptAutovectorException + level;

    private static void ResetDummy()
    {
    }

    private uint ReadByte(uint address) => _readByte(address);

    private uint ReadWord(uint address) => _readWord(address);

    private uint ReadLong(uint address)
        => (_readWord(address) << 16) | (_readWord(unchecked(address + 2)) & 0xFFFF);

    private void WriteByte(uint address, uint data) => _writeByte(address, data);

    private void WriteWord(uint address, uint data) => _writeWord(address, data);

    private void WriteLong(uint address, uint data)
    {
        _writeWord(address, data >> 16);
        _writeWord(unchecked(address + 2), data & 0xFFFF);
    }

    internal void SetFetch(uint lowAddress, uint highAddress, byte[] buffer)
    {
        uint first = (lowAddress >> FetchShift) & FetchMask;
        uint last = (highAddress >> FetchShift) & FetchMask;
        var fetchPointer = new FetchPointer(buffer, unchecked(0u - (first << FetchShift)));
        while (first <= last)
            _fetch[first++] = fetchPointer;
    }

    internal void SetReadByte(Func<uint, uint> read) => _readByte = read;

    internal void SetReadWord(Func<uint, uint> read) => _readWord = read;

    internal void SetWriteByte(Action<uint, uint> write) => _writeByte = write;

    internal void SetWriteWord(Action<uint, uint> write) => _writeWord = write;

    internal uint GetDataRegister(uint number) => _data[number];

    internal uint GetAddressRegister(uint number) => _address[number];

    internal uint GetProgramCounter() => unchecked(_pc - _basePc.Bias);

    internal uint GetStatusRegister() => GetStatusRegisterBits();

    internal uint GetUserStackPointer() => _supervisor != 0 ? _usp : _address[7];

    internal uint GetSupervisorStackPointer() => _supervisor != 0 ? _address[7] : _usp;

    internal void SetDataRegister(uint number, uint value) => _data[number] = value;

    internal void SetAddressRegister(uint number, uint value) => _address[number] = value;

    internal void SetProgramCounter(uint value)
    {
        _basePc = _fetch[(value >> FetchShift) & FetchMask];
        _pc = unchecked(value + _basePc.Bias);
    }

    internal void SetStatusRegister(uint value) => SetStatusRegisterBits(value);

    internal void SetUserStackPointer(uint value)
    {
        if (_supervisor != 0)
            _usp = value;
        else
            _address[7] = value;
    }

    internal void SetSupervisorStackPointer(uint value)
    {
        if (_supervisor != 0)
            _address[7] = value;
        else
            _usp = value;
    }
}

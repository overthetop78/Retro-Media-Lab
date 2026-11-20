// Copyright 2014-2016 James Laird-Wah
// Copyright 2004-2006, 2013 Theo Berkau
// Translated from Yabause ygr.c.
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Diagnostics;

namespace RetroMediaLab.Chips.Controllers.Sega;

public sealed class SaturnYgr(
    Func<uint, ushort> readMpegWord,
    Action<uint, ushort> writeMpegWord,
    Func<ushort> readSh1InterruptPriorityB,
    Action<int, int> sendSh1Interrupt)
{
    private const int FifoSize = 4096;
    private const int FifoMask = FifoSize - 1;
    private readonly ushort[] _fifo = new ushort[FifoSize];
    private int _fifoReadPointer;
    private int _fifoWritePointer;
    private int _fifoCount;
    private ushort _transferControl;
    private ushort _cdIrqFlags;
    private ushort _mailboxStatus;
    private ushort _unknown;
    private ushort _hirq;
    private ushort _hirqMask;
    private ushort _cr1;
    private ushort _cr2;
    private ushort _cr3;
    private ushort _cr4;
    private ushort _rr1;
    private ushort _rr2;
    private ushort _rr3;
    private ushort _rr4;
    private ushort _mpegRgb;
    private ushort _register1A;
    private ushort _register1C = 0;

    internal ushort Register0C { get; private set; }

    private ushort ReadFifo()
    {
        int pointer = _fifoReadPointer;
        if ((_transferControl & 4) != 0)
            Debug.Assert(_fifoCount > 0);

        _fifoReadPointer++;
        _fifoReadPointer &= FifoMask;
        _fifoCount--;
        if (_fifoCount < 0)
            _fifoCount = 0;

        return _fifo[pointer];
    }

    private void WriteFifo(ushort value)
    {
        Debug.Assert(_fifoCount != FifoSize);
        _fifo[_fifoWritePointer++] = value;
        _fifoWritePointer &= FifoMask;
        _fifoCount++;
    }

    private bool FifoFull() => _fifoCount == FifoSize;

    private bool FifoEmpty() => _fifoCount == 0;

    public bool DreqAsserted()
    {
        if (FifoEmpty() && (_transferControl & 5) == 5)
            return false;
        else if (FifoFull())
            return false;
        else
            return ((_transferControl >> 2) & 1) != 0;
    }

    public bool CheckABusWait(uint address, int size)
    {
        if ((address & 0x7FFF) <= 0xFFF && (address & 0x3F) <= 2)
        {
            if (size == 1 && _fifoCount != 0)
                return false;
            else if (size == 2 && _fifoCount >= 2)
                return false;
            else
                return ((_transferControl >> 2) & 1) != 0;
        }

        return false;
    }

    public byte Sh1ReadByte(uint address)
    {
        Debug.Assert(false);
        return 0;
    }

    public ushort Sh1ReadWord(uint address)
    {
        if ((address & 0xF00000) == 0x100000)
            return readMpegWord(address);

        switch (address & 0xFFFF)
        {
            case 0: return ReadFifo();
            case 2: return _transferControl;
            case 4: return _mailboxStatus;
            case 0x06: return _cdIrqFlags;
            case 8: return _unknown;
            case 0x0A: return _hirqMask;
            case 0x10: return _cr1;
            case 0x12: return _cr2;
            case 0x14: return _cr3;
            case 0x16: return _cr4;
            case 0x1A: return _register1A;
            case 0x1C: return _register1C;
        }

        Debug.Assert(false);
        return 0;
    }

    public uint Sh1ReadLong(uint address) => 0;

    public void Sh1WriteByte(uint address, byte value)
    {
        Debug.Assert(false);
    }

    public void Sh1WriteWord(uint address, ushort value)
    {
        if ((address & 0xF00000) == 0x100000)
        {
            writeMpegWord(address, value);
            return;
        }

        switch (address & 0xFFFF)
        {
            case 0:
                WriteFifo(value);
                return;
            case 2:
                if ((value & 2) != 0)
                {
                    _fifoReadPointer = 0;
                    _fifoWritePointer = 0;
                    _fifoCount = 0;
                }
                _transferControl = value;
                return;
            case 4:
                _mailboxStatus = value;
                return;
            case 0x06:
                _cdIrqFlags = value;
                return;
            case 8:
                _unknown = (ushort)(value & 3);
                return;
            case 0x0A:
                _hirqMask = (ushort)(value & 0x70);
                return;
            case 0x0C:
                Register0C = value;
                return;
            case 0x10:
                _rr1 = value;
                return;
            case 0x12:
                _rr2 = value;
                return;
            case 0x14:
                _rr3 = value;
                return;
            case 0x16:
                _rr4 = value;
                return;
            case 0x1E:
                _hirq |= value;
                return;
            case 0x1A:
                _register1A = value;
                return;
        }

        Debug.Assert(false);
    }

    public void Sh1WriteLong(uint address, uint value) { }

    public ushort ABusReadWord(uint address)
    {
        if ((address & 0x7FFF) <= 0xFFF)
        {
            address &= 0x3F;
            switch (address)
            {
                case 0x00:
                case 0x02:
                    return ReadFifo();
                case 0x08:
                case 0x0A:
                    return _hirq;
                case 0x0C:
                case 0x0E:
                    return _hirqMask;
                case 0x18:
                case 0x1A:
                    return _rr1;
                case 0x1C:
                case 0x1E:
                    return _rr2;
                case 0x20:
                case 0x22:
                    return _rr3;
                case 0x24:
                case 0x26:
                    _mailboxStatus |= 2;
                    return _rr4;
                case 0x28:
                case 0x2A:
                    return _mpegRgb;
            }
        }

        Debug.Assert(false);
        return 0;
    }

    public void ABusWriteWord(uint address, ushort value)
    {
        if ((address & 0x7FFF) <= 0xFFF)
        {
            address &= 0x3F;
            switch (address)
            {
                case 0x00:
                    if ((_transferControl & 1) != 0)
                        WriteFifo(value);
                    break;
                case 0x08:
                case 0x0A:
                    _hirq &= value;
                    return;
                case 0x0C:
                case 0x0E:
                    _hirqMask = value;
                    return;
                case 0x18:
                case 0x1A:
                    _cr1 = value;
                    return;
                case 0x1C:
                case 0x1E:
                    _cr2 = value;
                    return;
                case 0x20:
                case 0x22:
                    _cr3 = value;
                    return;
                case 0x24:
                case 0x26:
                    _cr4 = value;
                    sendSh1Interrupt(70, (readSh1InterruptPriorityB() >> 4) & 0xF);
                    return;
                case 0x28:
                case 0x2A:
                    _mpegRgb = value;
                    return;
            }
        }
    }

    public uint ABusReadLong(uint address)
    {
        if ((address & 0x7FFF) <= 0xFFF)
        {
            address &= 0x3F;
            switch (address)
            {
                case 0x00:
                {
                    uint value = ReadFifo();
                    value <<= 16;
                    value |= ReadFifo();
                    return value;
                }
                case 0x08:
                    return ((uint)_hirq << 16) | _hirq;
                case 0x0C:
                    return ((uint)_hirqMask << 16) | _hirqMask;
                case 0x18:
                    return ((uint)_rr1 << 16) | _rr1;
                case 0x1C:
                    return ((uint)_rr2 << 16) | _rr2;
                case 0x20:
                    return ((uint)_rr3 << 16) | _rr3;
                case 0x24:
                    _mailboxStatus |= 2;
                    return ((uint)_rr4 << 16) | _rr4;
                case 0x28:
                    return ((uint)_mpegRgb << 16) | _mpegRgb;
            }
        }

        return 0;
    }

    public void ABusWriteLong(uint address, uint value)
    {
        if ((address & 0x7FFF) <= 0xFFF)
        {
            address &= 0x3F;
            switch (address)
            {
                case 0x00:
                    if ((_transferControl & 1) != 0)
                    {
                        WriteFifo((ushort)(value >> 16));
                        WriteFifo(unchecked((ushort)value));
                    }
                    break;
                case 0x0C:
                    _hirqMask = unchecked((ushort)value);
                    break;
                case 0x18000:
                    break;
            }
        }
    }

    public void CdIrq(byte flags)
    {
        _cdIrqFlags |= flags;
        if ((_cdIrqFlags & _hirqMask) != 0)
            sendSh1Interrupt(71, readSh1InterruptPriorityB() & 0xF);
    }
}

// Copyright 2003-2005 Guillaume Duhamel
// Copyright 2004-2005, 2013 Theo Berkau
// Translated from Yabause sh2core.c.
// SPDX-License-Identifier: GPL-2.0-or-later

using RetroMediaLab.Chips.Controllers.Sega;
using RetroMediaLab.Chips.Controllers.Sega.CD;
using static RetroMediaLab.Chips.Controllers.Sega.SaturnMemoryAccess;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH2;

public sealed class SaturnSh2Core(bool cacheEmulationEnabled, bool dmaTimingEnabled, bool isSlave = false)
{
    private readonly uint[] _addressArray = new uint[0x100];
    private readonly byte[] _dataArray = new byte[0x1000];
    public SaturnSh2State State { get; } = new();

    internal SaturnSh2CacheState Cache { get; } = new();
    internal bool CacheEmulationEnabled => cacheEmulationEnabled;
    internal byte CacheControl => _onchip.CCR;

    private SaturnSh2State.OnchipRegisters _onchip = new()
    {
        BCR1 = isSlave ? (ushort)0x8000 : (ushort)0,
    };
    private int _frcShift = 3;
    private uint _frcLeftover;
    private int _wdtShift = 1;
    private uint _wdtLeftover;
    private bool _wdtEnabled;
    private bool _wdtInterval = true;

    public void Reset()
    {
        for (int register = 0; register < 15; register++)
            State.R[register] = 0;

        State.SR.All = 0xF0;
        State.GBR = 0;
        State.VBR = 0;
        State.MACH = 0;
        State.MACL = 0;
        State.PR = 0;
        State.Delay = 0;
        State.Cycles = 0;
        State.IsIdle = false;

        _frcLeftover = 0;
        _frcShift = 3;
        _wdtEnabled = false;
        _wdtInterval = true;
        _wdtShift = 1;
        _wdtLeftover = 0;

        Array.Clear(State.Interrupts);
        State.NumberOfInterrupts = 0;

        OnChipReset();
        SaturnSh2Cache.Clear(Cache);
    }

    public void PowerOn(SaturnMemory.Sh2MemoryMap memory)
    {
        uint vectorAddress = State.VBR;
        State.PC = memory.Read32(vectorAddress);
        State.R[15] = memory.Read32(vectorAddress + 4);
    }

    public void Execute(SaturnMemory.Sh2MemoryMap memory, uint cycles)
    {
        SaturnSh2Interpreter.Execute(State, memory, cycles);
        memory.ExecuteSh2Peripherals(cycles);
        State.Cycles = State.Cycles < cycles ? 0 : State.Cycles - cycles;
    }

    public void ExecuteSh1(SaturnMemory.Sh2MemoryMap memory, uint cycles, SaturnCdDrive drive)
    {
        SaturnSh2Interpreter.ExecuteSh1(State, memory, cycles);
        memory.Sh1!.RunOnchipCycles((int)cycles, drive);
        WdtExec(cycles);
        State.Cycles = State.Cycles < cycles ? 0 : State.Cycles - cycles;
    }

    public void SendInterrupt(int vector, int level) =>
        SaturnSh2Interpreter.SendInterrupt(State, (byte)vector, (byte)level);

    public void Nmi()
    {
        _onchip.ICR |= 0x8000;
        SendInterrupt(0x0B, 0x10);
    }

    public void OnChipReset()
    {
        _onchip.SMR = 0;
        _onchip.BRR = 0xFF;
        _onchip.SCR = 0;
        _onchip.TDR = 0xFF;
        _onchip.SSR = 0x84;
        _onchip.RDR = 0;
        _onchip.TIER = 0x01;
        _onchip.FTCSR = 0;
        _onchip.FRC = 0;
        _onchip.OCRA = 0xFFFF;
        _onchip.OCRB = 0xFFFF;
        _onchip.TCR = 0;
        _onchip.TOCR = 0xE0;
        _onchip.FICR = 0;
        _onchip.IPRB = 0;
        _onchip.VCRA = 0;
        _onchip.VCRB = 0;
        _onchip.VCRC = 0;
        _onchip.VCRD = 0;
        _onchip.DRCR0 = 0;
        _onchip.DRCR1 = 0;
        _onchip.WTCSR = 0x18;
        _onchip.WTCNT = 0;
        _onchip.RSTCSR = 0x1F;
        _onchip.SBYCR = 0x60;
        _onchip.CCR = 0;
        _onchip.ICR = 0;
        _onchip.IPRA = 0;
        _onchip.VCRWDT = 0;
        _onchip.DVCR = 0;
        _onchip.VCRDIV = 0;
        _onchip.BARA = 0;
        _onchip.BAMRA = 0;
        _onchip.BBRA = 0;
        _onchip.BARB = 0;
        _onchip.BAMRB = 0;
        _onchip.BDRB = 0;
        _onchip.BDMRB = 0;
        _onchip.BBRB = 0;
        _onchip.BRCR = 0;
        _onchip.CHCR0 = 0;
        _onchip.CHCR1 = 0;
        _onchip.DMAOR = 0;
        _onchip.BCR1 &= 0x8000;
        _onchip.BCR1 |= 0x03F0;
        _onchip.BCR2 = 0x00FC;
        _onchip.WCR = 0xAAFF;
        _onchip.MCR = 0;
        _onchip.RTCSR = 0;
        _onchip.RTCNT = 0;
        _onchip.RTCOR = 0;
    }

    internal byte OnChipReadByte(uint address)
    {
        switch (address)
        {
            case 0x000:
                return _onchip.SMR;
            case 0x001:
                return _onchip.BRR;
            case 0x002:
                return _onchip.SCR;
            case 0x003:
                return _onchip.TDR;
            case 0x004:
                return _onchip.SSR;
            case 0x005:
                return _onchip.RDR;
            case 0x010:
                return _onchip.TIER;
            case 0x011:
                return _onchip.FTCSR;
            case 0x012:
                return (byte)(_onchip.FRC >> 8);
            case 0x013:
                return unchecked((byte)_onchip.FRC);
            case 0x014:
                if ((_onchip.TOCR & 0x10) == 0)
                    return (byte)(_onchip.OCRA >> 8);
                else
                    return (byte)(_onchip.OCRB >> 8);
            case 0x015:
                if ((_onchip.TOCR & 0x10) == 0)
                    return (byte)(_onchip.OCRA & 0xFF);
                else
                    return (byte)(_onchip.OCRB & 0xFF);
            case 0x016:
                return _onchip.TCR;
            case 0x017:
                return _onchip.TOCR;
            case 0x018:
                return (byte)(_onchip.FICR >> 8);
            case 0x019:
                return unchecked((byte)_onchip.FICR);
            case 0x060:
                return (byte)(_onchip.IPRB >> 8);
            case 0x062:
                return (byte)(_onchip.VCRA >> 8);
            case 0x063:
                return (byte)(_onchip.VCRA & 0xFF);
            case 0x064:
                return (byte)(_onchip.VCRB >> 8);
            case 0x065:
                return (byte)(_onchip.VCRB & 0xFF);
            case 0x066:
                return (byte)(_onchip.VCRC >> 8);
            case 0x067:
                return (byte)(_onchip.VCRC & 0xFF);
            case 0x068:
                return (byte)(_onchip.VCRD >> 8);
            case 0x080:
                return _onchip.WTCSR;
            case 0x081:
                return _onchip.WTCNT;
            case 0x092:
                return CacheControl;
            case 0x0E0:
                return (byte)(_onchip.ICR >> 8);
            case 0x0E1:
                return (byte)(_onchip.ICR & 0xFF);
            case 0x0E2:
                return (byte)(_onchip.IPRA >> 8);
            case 0x0E3:
                return (byte)(_onchip.IPRA & 0xFF);
            case 0x0E4:
                return (byte)(_onchip.VCRWDT >> 8);
            case 0x0E5:
                return (byte)(_onchip.VCRWDT & 0xFF);
            default:
                break;
        }

        return 0;
    }

    internal ushort OnChipReadWord(uint address)
    {
        switch (address)
        {
            case 0x060:
                return _onchip.IPRB;
            case 0x062:
                return _onchip.VCRA;
            case 0x064:
                return _onchip.VCRB;
            case 0x066:
                return _onchip.VCRC;
            case 0x068:
                return _onchip.VCRD;
            case 0x0E0:
                return _onchip.ICR;
            case 0x0E2:
                return _onchip.IPRA;
            case 0x0E4:
                return _onchip.VCRWDT;
            case 0x1E2:
                return (ushort)(_onchip.BCR1);
            case 0x1E6:
                return (ushort)(_onchip.BCR2);
            case 0x1EA:
                return (ushort)(_onchip.WCR);
            case 0x1EE:
                return (ushort)(_onchip.MCR);
            case 0x1F2:
                return (ushort)(_onchip.RTCSR);
            case 0x1F6:
                return (ushort)(_onchip.RTCNT);
            case 0x1FA:
                return (ushort)(_onchip.RTCOR);
            default:
                return 0;
        }
    }

    internal void OnChipWriteByte(uint address, byte value)
    {
        switch (address)
        {
            case 0x000:
                _onchip.SMR = value;
                return;
            case 0x001:
                _onchip.BRR = value;
                return;
            case 0x002:
                if ((value & 0x20) == 0)
                    _onchip.SSR |= 0x80;

                _onchip.SCR = value;
                return;
            case 0x003:
                _onchip.TDR = value;
                return;
            case 0x004:
                if ((_onchip.SCR & 0x20) != 0)
                {
                    if ((value & 0x80) == 0)
                        SCITransmitByte(_onchip.TDR);

                }
                return;
            case 0x010:
                _onchip.TIER = (byte)((value & 0x8E) | 0x1);
                return;
            case 0x011:
                _onchip.FTCSR = (byte)((_onchip.FTCSR & (value & 0xFE)) | (value & 1));
                return;
            case 0x012:
                _onchip.FRC = (ushort)((value << 8) | (_onchip.FRC & 0xFF));
                return;
            case 0x013:
                _onchip.FRC = (ushort)((_onchip.FRC & 0xFF00) | value);
                return;
            case 0x014:
                if ((_onchip.TOCR & 0x10) == 0)
                    _onchip.OCRA = (ushort)((value << 8) | (_onchip.OCRA & 0xFF));
                else
                    _onchip.OCRB = (ushort)((value << 8) | (_onchip.OCRB & 0xFF));
                return;
            case 0x015:
                if ((_onchip.TOCR & 0x10) == 0)
                    _onchip.OCRA = (ushort)((_onchip.OCRA & 0xFF00) | value);
                else
                    _onchip.OCRB = (ushort)((_onchip.OCRB & 0xFF00) | value);
                return;
            case 0x016:
                _onchip.TCR = (byte)(value & 0x83);

                switch (value & 3)
                {
                    case 0:
                        _frcShift = 3;
                        break;
                    case 1:
                        _frcShift = 5;
                        break;
                    case 2:
                        _frcShift = 7;
                        break;
                    case 3:
                        break;
                }
                return;
            case 0x017:
                _onchip.TOCR = (byte)(0xE0 | (value & 0x13));
                return;
            case 0x060:
                _onchip.IPRB = (ushort)((value << 8));
                return;
            case 0x061:
                return;
            case 0x062:
                _onchip.VCRA = (ushort)(((value & 0x7F) << 8) | (_onchip.VCRA & 0x00FF));
                return;
            case 0x063:
                _onchip.VCRA = (ushort)((_onchip.VCRA & 0xFF00) | (value & 0x7F));
                return;
            case 0x064:
                _onchip.VCRB = (ushort)(((value & 0x7F) << 8) | (_onchip.VCRB & 0x00FF));
                return;
            case 0x065:
                _onchip.VCRB = (ushort)((_onchip.VCRB & 0xFF00) | (value & 0x7F));
                return;
            case 0x066:
                _onchip.VCRC = (ushort)(((value & 0x7F) << 8) | (_onchip.VCRC & 0x00FF));
                return;
            case 0x067:
                _onchip.VCRC = (ushort)((_onchip.VCRC & 0xFF00) | (value & 0x7F));
                return;
            case 0x068:
                _onchip.VCRD = (ushort)((value & 0x7F) << 8);
                return;
            case 0x069:
                return;
            case 0x071:
                _onchip.DRCR0 = (byte)(value & 0x3);
                return;
            case 0x072:
                _onchip.DRCR1 = (byte)(value & 0x3);
                return;
            case 0x091:
                _onchip.SBYCR = (byte)(value & 0xDF);
                return;
            case 0x092:
                _onchip.CCR = (byte)(value & 0xCF);
                if ((value & 0x10) != 0)
                    SaturnSh2Cache.Clear(Cache);
                if ((CacheControl & 1) != 0)
                    SaturnSh2Cache.Enable(Cache);
                else
                    SaturnSh2Cache.Disable(Cache);
                return;
            case 0x0E0:
                _onchip.ICR = (ushort)(((value & 0x1) << 8) | (_onchip.ICR & 0xFEFF));
                return;
            case 0x0E1:
                _onchip.ICR = (ushort)((_onchip.ICR & 0xFFFE) | (value & 0x1));
                return;
            case 0x0E2:
                _onchip.IPRA = (ushort)((value << 8) | (_onchip.IPRA & 0x00FF));
                return;
            case 0x0E3:
                _onchip.IPRA = (ushort)((_onchip.IPRA & 0xFF00) | (value & 0xF0));
                return;
            case 0x0E4:
                _onchip.VCRWDT = (ushort)(((value & 0x7F) << 8) | (_onchip.VCRWDT & 0x00FF));
                return;
            case 0x0E5:
                _onchip.VCRWDT = (ushort)((_onchip.VCRWDT & 0xFF00) | (value & 0x7F));
                return;
            default:
                return;
        }
    }

    internal void OnChipWriteWord(uint address, ushort value)
    {
        switch (address)
        {
            case 0x060:
                _onchip.IPRB = (ushort)(value & 0xFF00);
                return;
            case 0x062:
                _onchip.VCRA = (ushort)(value & 0x7F7F);
                return;
            case 0x064:
                _onchip.VCRB = (ushort)(value & 0x7F7F);
                return;
            case 0x066:
                _onchip.VCRC = (ushort)(value & 0x7F7F);
                return;
            case 0x068:
                _onchip.VCRD = (ushort)(value & 0x7F7F);
                return;
            case 0x080:
                if (value >> 8 == 0xA5)
                {
                    switch (value & 7)
                    {
                        case 0:
                            _wdtShift = 1;
                            break;
                        case 1:
                            _wdtShift = 6;
                            break;
                        case 2:
                            _wdtShift = 7;
                            break;
                        case 3:
                            _wdtShift = 8;
                            break;
                        case 4:
                            _wdtShift = 9;
                            break;
                        case 5:
                            _wdtShift = 10;
                            break;
                        case 6:
                            _wdtShift = 12;
                            break;
                        case 7:
                            _wdtShift = 13;
                            break;
                    }

                    _wdtEnabled = (value & 0x20) != 0;
                    _wdtInterval = (~value & 0x40) != 0;

                    _onchip.WTCSR = unchecked((byte)(value | 0x18));
                }
                else if (value >> 8 == 0x5A)
                {
                    _onchip.WTCNT = unchecked((byte)value);
                }
                return;
            case 0x082:
                if (value == 0xA500)
                    _onchip.RSTCSR &= 0x7F;
                else if (value >> 8 == 0x5A)
                    _onchip.RSTCSR = (byte)((_onchip.RSTCSR & 0x80) | (value & 0x60) | 0x1F);
                return;
            case 0x092:
                _onchip.CCR = (byte)(value & 0xCF);
                if ((value & 0x10) != 0)
                    SaturnSh2Cache.Clear(Cache);
                if ((CacheControl & 1) != 0)
                    SaturnSh2Cache.Enable(Cache);
                else
                    SaturnSh2Cache.Disable(Cache);
                return;
            case 0x0E0:
                _onchip.ICR = (ushort)(value & 0x0101);
                return;
            case 0x0E2:
                _onchip.IPRA = (ushort)(value & 0xFFF0);
                return;
            case 0x0E4:
                _onchip.VCRWDT = (ushort)(value & 0x7F7F);
                return;
            case 0x108:
            case 0x128:
                _onchip.DVCR = (uint)value & 3;
                return;
            case 0x148:
                _onchip.BBRA = (uint)value & 0xFF;
                return;
            case 0x178:
                _onchip.BRCR = (uint)value & 0xF4DC;
                return;
            default:
                return;
        }
    }

    internal uint OnChipReadLong(uint address)
    {
        switch (address)
        {
            case 0x100:
            case 0x120:
                return _onchip.DVSR;
            case 0x104:
            case 0x124:
                return _onchip.DVDNTL;
            case 0x108:
            case 0x128:
                return _onchip.DVCR;
            case 0x10C:
            case 0x12C:
                return _onchip.VCRDIV;
            case 0x110:
            case 0x130:
                return _onchip.DVDNTH;
            case 0x114:
            case 0x134:
                return _onchip.DVDNTL;
            case 0x118:
            case 0x138:
                return _onchip.DVDNTUH;
            case 0x11C:
            case 0x13C:
                return _onchip.DVDNTUL;
            case 0x180:
                return _onchip.SAR0;
            case 0x184:
                return _onchip.DAR0;
            case 0x188:
                return _onchip.TCR0;
            case 0x18C:
                return _onchip.CHCR0;
            case 0x190:
                return _onchip.SAR1;
            case 0x194:
                return _onchip.DAR1;
            case 0x198:
                return _onchip.TCR1;
            case 0x19C:
                return _onchip.CHCR1;
            case 0x1A0:
                return _onchip.VCRDMA0;
            case 0x1A8:
                return _onchip.VCRDMA1;
            case 0x1B0:
                return _onchip.DMAOR;
            case 0x1E0:
                return _onchip.BCR1;
            case 0x1E4:
                return _onchip.BCR2;
            case 0x1E8:
                return _onchip.WCR;
            case 0x1EC:
                return _onchip.MCR;
            case 0x1F0:
                return _onchip.RTCSR;
            case 0x1F4:
                return _onchip.RTCNT;
            case 0x1F8:
                return _onchip.RTCOR;
            default:
                return 0;
        }
    }

    internal void OnChipWriteLong(
        uint address, uint value, SaturnMemory.Sh2MemoryMap memory,
        Action<int, int> sendInterrupt, Action<uint, uint> writeNotify)
    {
        switch (address)
        {
            case 0x100:
            case 0x120:
                _onchip.DVSR = value;
                return;
            case 0x104:
            case 0x124:
            {
                int divisor = unchecked((int)_onchip.DVSR);
                if (divisor == 0)
                {
                    if ((value & 0x80000000) != 0)
                    {
                        _onchip.DVDNTL = 0x80000000;
                        _onchip.DVDNTH = 0xFFFFFFFC | ((value >> 29) & 3);
                    }
                    else
                    {
                        _onchip.DVDNTL = 0x7FFFFFFF;
                        _onchip.DVDNTH = value >> 29;
                    }

                    _onchip.DVDNTUL = _onchip.DVDNTL;
                    _onchip.DVDNTUH = _onchip.DVDNTH;
                    _onchip.DVCR |= 1;
                    if ((_onchip.DVCR & 2) != 0)
                        sendInterrupt((int)(_onchip.VCRDIV & 0x7F), (memory.MasterCore._onchip.IPRA >> 12) & 0xF);
                }
                else
                {
                    int dividend = unchecked((int)value);
                    // Mednafen sh7095.inc, DIVU_S32_S32.
                    bool wrappedQuotient = divisor == -1 && dividend == int.MinValue;
                    int quotient = wrappedQuotient ? int.MinValue : dividend / divisor;
                    int remainder = wrappedQuotient ? 0 : dividend % divisor;
                    _onchip.DVDNTL = unchecked((uint)quotient);
                    _onchip.DVDNTUL = unchecked((uint)quotient);
                    _onchip.DVDNTH = unchecked((uint)remainder);
                    _onchip.DVDNTUH = unchecked((uint)remainder);
                }

                return;
            }
            case 0x108:
            case 0x128:
                _onchip.DVCR = value & 3;
                return;
            case 0x10C:
            case 0x12C:
                _onchip.VCRDIV = value & 0xFFFF;
                return;
            case 0x110:
            case 0x130:
                _onchip.DVDNTH = value;
                return;
            case 0x114:
            case 0x134:
            {
                int divisor = unchecked((int)_onchip.DVSR);
                long dividend = ((long)_onchip.DVDNTH << 32) | value;
                if (divisor == 0)
                {
                    if ((_onchip.DVDNTH & 0x80000000) != 0)
                    {
                        _onchip.DVDNTL = 0x80000000;
                        _onchip.DVDNTH <<= 3;
                    }
                    else
                    {
                        _onchip.DVDNTL = 0x7FFFFFFF;
                        _onchip.DVDNTH <<= 3;
                    }

                    _onchip.DVDNTUL = _onchip.DVDNTL;
                    _onchip.DVDNTUH = _onchip.DVDNTH;
                    _onchip.DVCR |= 1;
                    if ((_onchip.DVCR & 2) != 0)
                        sendInterrupt((int)(_onchip.VCRDIV & 0x7F), (memory.MasterCore._onchip.IPRA >> 12) & 0xF);
                }
                else if (divisor == -1 && dividend == long.MinValue)
                {
                    // Mednafen sh7095.inc, DIVU_S64_S32 overflow path.
                    _onchip.DVCR |= 1;
                    ulong partial = Divide64Partial(unchecked((ulong)dividend), _onchip.DVSR);
                    _onchip.DVDNTH = (uint)(partial >> 32);
                    _onchip.DVDNTL = (_onchip.DVCR & 2) != 0 ? unchecked((uint)partial) : 0x7FFFFFFF;
                    _onchip.DVDNTUH = _onchip.DVDNTH;
                    _onchip.DVDNTUL = _onchip.DVDNTL;
                    if ((_onchip.DVCR & 2) != 0)
                        sendInterrupt((int)(_onchip.VCRDIV & 0x7F), (memory.MasterCore._onchip.IPRA >> 12) & 0xF);
                }
                else
                {
                    long quotient = dividend / divisor;
                    int remainder = (int)(dividend % divisor);
                    if (quotient > 0x7FFFFFFF)
                    {
                        _onchip.DVCR |= 1;
                        _onchip.DVDNTL = 0x7FFFFFFF;
                        _onchip.DVDNTH = 0xFFFFFFFE;
                        if ((_onchip.DVCR & 2) != 0)
                            sendInterrupt((int)(_onchip.VCRDIV & 0x7F), (memory.MasterCore._onchip.IPRA >> 12) & 0xF);
                    }
                    else if ((int)(quotient >> 32) < -1)
                    {
                        _onchip.DVCR |= 1;
                        _onchip.DVDNTL = 0x80000000;
                        _onchip.DVDNTH = 0xFFFFFFFE;
                        if ((_onchip.DVCR & 2) != 0)
                            sendInterrupt((int)(_onchip.VCRDIV & 0x7F), (memory.MasterCore._onchip.IPRA >> 12) & 0xF);
                    }
                    else
                    {
                        _onchip.DVDNTL = unchecked((uint)quotient);
                        _onchip.DVDNTH = unchecked((uint)remainder);
                    }

                    _onchip.DVDNTUL = _onchip.DVDNTL;
                    _onchip.DVDNTUH = _onchip.DVDNTH;
                }

                return;
            }
            case 0x118:
            case 0x138:
                _onchip.DVDNTUH = value;
                return;
            case 0x11C:
            case 0x13C:
                _onchip.DVDNTUL = value;
                return;
            case 0x140:
                _onchip.BARA = value;
                return;
            case 0x144:
                _onchip.BAMRA = value;
                return;
            case 0x180:
                _onchip.SAR0 = value;
                return;
            case 0x184:
                _onchip.DAR0 = value;
                return;
            case 0x188:
                _onchip.TCR0 = value & 0xFFFFFF;
                return;
            case 0x18C:
                _onchip.CHCR0 = value & 0xFFFF;
                if ((_onchip.DMAOR & 7) == 1 && (value & 3) == 1)
                {
                    if (dmaTimingEnabled)
                        _onchip.Dma0Active = true;
                    else
                        DmaExec(memory, sendInterrupt, writeNotify);
                }
                return;
            case 0x190:
                _onchip.SAR1 = value;
                return;
            case 0x194:
                _onchip.DAR1 = value;
                return;
            case 0x198:
                _onchip.TCR1 = value & 0xFFFFFF;
                return;
            case 0x19C:
                _onchip.CHCR1 = value & 0xFFFF;
                if ((_onchip.DMAOR & 7) == 1 && (value & 3) == 1)
                {
                    if (dmaTimingEnabled)
                        _onchip.Dma1Active = true;
                    else
                        DmaExec(memory, sendInterrupt, writeNotify);
                }
                return;
            case 0x1A0:
                _onchip.VCRDMA0 = value & 0xFFFF;
                return;
            case 0x1A8:
                _onchip.VCRDMA1 = value & 0xFFFF;
                return;
            case 0x1B0:
                _onchip.DMAOR = value & 0xF;
                if ((value & 7) == 1)
                {
                    if (dmaTimingEnabled)
                    {
                        if ((_onchip.CHCR0 & 3) == 1)
                            _onchip.Dma0Active = true;
                        if ((_onchip.CHCR1 & 3) == 1)
                            _onchip.Dma1Active = true;
                    }
                    else
                        DmaExec(memory, sendInterrupt, writeNotify);
                }
                return;
            case 0x1E0:
                _onchip.BCR1 &= 0x8000;
                _onchip.BCR1 |= (ushort)(value & 0x1FF7);
                return;
            case 0x1E4:
                _onchip.BCR2 = (ushort)(value & 0xFC);
                return;
            case 0x1E8:
                _onchip.WCR = unchecked((ushort)value);
                return;
            case 0x1EC:
                _onchip.MCR = (ushort)(value & 0xFEFC);
                return;
            case 0x1F0:
                _onchip.RTCSR = (ushort)(value & 0xF8);
                return;
            case 0x1F8:
                _onchip.RTCOR = (ushort)(value & 0xFF);
                return;
            default:
                return;
        }
    }

    // DIVU64_Partial from Mednafen sh7095.inc, Copyright (C) 2015-2020 Mednafen Team.
    private static ulong Divide64Partial(ulong dividend, uint divisor)
    {
        ulong quotientSign = dividend >> 63;
        ulong divisorSign = divisor >> 31;
        for (int step = 0; step < 3; step++)
        {
            if (quotientSign == divisorSign)
                dividend = unchecked(dividend - ((ulong)divisor << 32));
            else
                dividend = unchecked(dividend + ((ulong)divisor << 32));

            quotientSign = dividend >> 63;
            dividend <<= 1;
            dividend |= quotientSign ^ 1 ^ divisorSign;
        }

        return dividend;
    }

    public uint AddressArrayReadLong(uint address)
    {
        if (cacheEmulationEnabled)
        {
            int way = (CacheControl >> 6) & 3;
            int entry = (int)((address & 0x3FC) >> 4);
            uint data = Cache.Way[way, entry].Tag;
            data |= Cache.Lru[entry] << 4;
            data |= Cache.Way[way, entry].Valid << 2;
            return data;
        }

        return _addressArray[(address & 0x3FC) >> 2];
    }

    public void AddressArrayWriteLong(uint address, uint value)
    {
        if (cacheEmulationEnabled)
        {
            int way = (CacheControl >> 6) & 3;
            int entry = (int)((address & 0x3FC) >> 4);
            Cache.Way[way, entry].Tag = address & 0x1FFFFC00;
            Cache.Way[way, entry].Valid = (address >> 2) & 1;
            Cache.Lru[entry] = (value >> 4) & 0x3F;
        }
        else
            _addressArray[(address & 0x3FC) >> 2] = value;
    }

    public byte DataArrayReadByte(uint address)
    {
        if (cacheEmulationEnabled)
        {
            int way = (int)((address >> 10) & 3);
            int entry = (int)((address >> 4) & 0x3F);
            return Cache.Way[way, entry].Data[address & 0xF];
        }

        return T2ReadByte(_dataArray, address & 0xFFF);
    }

    public ushort DataArrayReadWord(uint address)
    {
        if (cacheEmulationEnabled)
        {
            int way = (int)((address >> 10) & 3);
            int entry = (int)((address >> 4) & 0x3F);
            uint offset = address & 0xF;
            byte[] data = Cache.Way[way, entry].Data;
            return (ushort)((data[offset] << 8) | data[offset + 1]);
        }

        return T2ReadWord(_dataArray, address & 0xFFF);
    }

    public uint DataArrayReadLong(uint address)
    {
        if (cacheEmulationEnabled)
        {
            int way = (int)((address >> 10) & 3);
            int entry = (int)((address >> 4) & 0x3F);
            uint offset = address & 0xF;
            byte[] data = Cache.Way[way, entry].Data;
            return ((uint)data[offset] << 24) |
                ((uint)data[offset + 1] << 16) |
                ((uint)data[offset + 2] << 8) |
                data[offset + 3];
        }

        return T2ReadLong(_dataArray, address & 0xFFF);
    }

    public void DataArrayWriteByte(uint address, byte value)
    {
        if (cacheEmulationEnabled)
        {
            int way = (int)((address >> 10) & 3);
            int entry = (int)((address >> 4) & 0x3F);
            Cache.Way[way, entry].Data[address & 0xF] = value;
        }
        else
            T2WriteByte(_dataArray, address & 0xFFF, value);
    }

    public void DataArrayWriteWord(uint address, ushort value)
    {
        if (cacheEmulationEnabled)
        {
            int way = (int)((address >> 10) & 3);
            int entry = (int)((address >> 4) & 0x3F);
            uint offset = address & 0xF;
            byte[] data = Cache.Way[way, entry].Data;
            data[offset] = (byte)(value >> 8);
            data[offset + 1] = unchecked((byte)value);
        }
        else
            T2WriteWord(_dataArray, address & 0xFFF, value);
    }

    public void DataArrayWriteLong(uint address, uint value)
    {
        if (cacheEmulationEnabled)
        {
            int way = (int)((address >> 10) & 3);
            int entry = (int)((address >> 4) & 0x3F);
            uint offset = address & 0xF;
            byte[] data = Cache.Way[way, entry].Data;
            data[offset] = (byte)((value >> 24) & 0xFF);
            data[offset + 1] = (byte)((value >> 16) & 0xFF);
            data[offset + 2] = (byte)((value >> 8) & 0xFF);
            data[offset + 3] = (byte)(value & 0xFF);
        }
        else
            T2WriteLong(_dataArray, address & 0xFFF, value);
    }

    public void FrtExec(uint cycles)
    {
        uint previous = _onchip.FRC;
        uint counter = previous;
        uint mask = (1u << _frcShift) - 1;
        counter += (cycles + _frcLeftover) >> _frcShift;
        _frcLeftover = (cycles + _frcLeftover) & mask;

        if (counter >= _onchip.OCRA && previous < _onchip.OCRA)
        {
            if ((_onchip.TIER & 8) != 0)
                SendInterrupt(_onchip.VCRC & 0x7F, (_onchip.IPRB & 0xF00) >> 8);
            if ((_onchip.FTCSR & 1) != 0)
            {
                counter = 0;
                _frcLeftover = 0;
            }
            _onchip.FTCSR |= 8;
        }

        if (counter >= _onchip.OCRB && previous < _onchip.OCRB)
        {
            if ((_onchip.TIER & 4) != 0)
                SendInterrupt(_onchip.VCRC & 0x7F, (_onchip.IPRB & 0xF00) >> 8);
            _onchip.FTCSR |= 4;
        }

        if (counter > 0xFFFF)
        {
            if ((_onchip.TIER & 2) != 0)
                SendInterrupt((_onchip.VCRD >> 8) & 0x7F, (_onchip.IPRB & 0xF00) >> 8);
            _onchip.FTCSR |= 2;
        }

        _onchip.FRC = unchecked((ushort)counter);
    }

    public void WdtExec(uint cycles)
    {
        if (!_wdtEnabled || (_onchip.WTCSR & 0x80) != 0 || (_onchip.RSTCSR & 0x80) != 0)
            return;

        uint counter = _onchip.WTCNT;
        uint mask = (1u << _wdtShift) - 1;
        counter += (cycles + _wdtLeftover) >> _wdtShift;
        _wdtLeftover = (cycles + _wdtLeftover) & mask;

        if (counter > 0xFF)
        {
            if (_wdtInterval)
            {
                _onchip.WTCSR |= 0x80;
                SendInterrupt((_onchip.VCRWDT >> 8) & 0x7F, (_onchip.IPRA >> 4) & 0xF);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Watchdog timer(WDT mode) overflow not implemented");
            }
        }

        _onchip.WTCNT = unchecked((byte)counter);
    }

    private static uint Sh2DmaAccess(SaturnMemory.Sh2MemoryMap master, uint address, uint data, bool read, int size)
    {
        address &= 0x0FFFFFFF;

        if (address <= 0x00FFFFF)
        {
            // BIOS
            if (read)
            {
                if (size == 0)
                    return master.InstructionMemory.BiosRomMemoryReadByte(address);
                else if (size == 1)
                    return master.InstructionMemory.BiosRomMemoryReadWord(address);
                else
                    return master.InstructionMemory.BiosRomMemoryReadLong(address);
            }
            else
            {
                if (size == 0)
                    master.InstructionMemory.BiosRomMemoryWriteByte(address, unchecked((byte)data));
                else if (size == 1)
                    master.InstructionMemory.BiosRomMemoryWriteWord(address, unchecked((ushort)data));
                else
                    master.InstructionMemory.BiosRomMemoryWriteLong(address, data);
                return 0;
            }
        }
        else if (address is >= 0x0100000 and <= 0x017FFFF)
        {
            // SMPC
            if (read)
            {
                if (size == 0)
                    return master.DmaPeripherals!.Smpc.ReadByte(address);
                else if (size == 1)
                    return master.DmaPeripherals!.Smpc.ReadWord(address);
                else
                    return master.DmaPeripherals!.Smpc.ReadLong(address);
            }
            else
            {
                if (size == 0)
                    master.DmaPeripherals!.Smpc.WriteByte(address, unchecked((byte)data));
                else if (size == 1)
                    master.DmaPeripherals!.Smpc.WriteWord(address, unchecked((ushort)data));
                else
                    master.DmaPeripherals!.Smpc.WriteLong(address, data);
                return 0;
            }
        }
        else if (address is >= 0x0180000 and <= 0x01FFFFF)
        {
            // Backup RAM
            if (read)
            {
                if (size == 0)
                    return master.InstructionMemory.BupRamMemoryReadByte(address);
                else if (size == 1)
                    return master.InstructionMemory.BupRamMemoryReadWord(address);
                else
                    return master.InstructionMemory.BupRamMemoryReadLong(address);
            }
            else
            {
                if (size == 0)
                    master.InstructionMemory.BupRamMemoryWriteByte(address, unchecked((byte)data));
                else if (size == 1)
                    master.InstructionMemory.BupRamMemoryWriteWord(address, unchecked((ushort)data));
                else
                    master.InstructionMemory.BupRamMemoryWriteLong(address, data);
                return 0;
            }
        }
        else if (address is >= 0x0200000 and <= 0x02FFFFF)
        {
            // Low work RAM
            if (read)
            {
                if (size == 0)
                    return master.InstructionMemory.LowWramMemoryReadByte(address);
                else if (size == 1)
                    return master.InstructionMemory.LowWramMemoryReadWord(address);
                else
                    return master.InstructionMemory.LowWramMemoryReadLong(address);
            }
            else
            {
                if (size == 0)
                    master.InstructionMemory.LowWramMemoryWriteByte(address, unchecked((byte)data));
                else if (size == 1)
                    master.InstructionMemory.LowWramMemoryWriteWord(address, unchecked((ushort)data));
                else
                    master.InstructionMemory.LowWramMemoryWriteLong(address, data);
                return 0;
            }
        }
        else if (address is >= 0x1000000 and <= 0x17FFFFF)
        {
            // Slave SH-2 input capture
            if (!read && size == 1)
                master.DmaSlaveCore.InputCaptureWriteWord();
            return 0;
        }
        else if (address is >= 0x1800000 and <= 0x1FFFFFF)
        {
            // Master SH-2 input capture
            if (!read && size == 1)
                master.Core.InputCaptureWriteWord();
            return 0;
        }
        else if (address is >= 0x2000000 and <= 0x3FFFFFF)
        {
            // Cartridge CS0
            if (read)
            {
                if (size == 0)
                    return master.DmaCartridgeCs0.ReadByte(address);
                else if (size == 1)
                    return master.DmaCartridgeCs0.ReadWord(address);
                else
                    return master.DmaCartridgeCs0.ReadLong(address);
            }
            else
            {
                if (size == 0)
                    master.DmaCartridgeCs0.WriteByte(address, unchecked((byte)data));
                else if (size == 1)
                    master.DmaCartridgeCs0.WriteWord(address, unchecked((ushort)data));
                else
                    master.DmaCartridgeCs0.WriteLong(address, data);
                return 0;
            }
        }
        else if (address is >= 0x4000000 and <= 0x4FFFFFF)
        {
            // Cartridge CS1
            if (read)
            {
                if (size == 0)
                    return master.DmaCartridgeCs1.ReadByte(address);
                else if (size == 1)
                    return master.DmaCartridgeCs1.ReadWord(address);
                else
                    return master.DmaCartridgeCs1.ReadLong(address);
            }
            else
            {
                if (size == 0)
                    master.DmaCartridgeCs1.WriteByte(address, unchecked((byte)data));
                else if (size == 1)
                    master.DmaCartridgeCs1.WriteWord(address, unchecked((ushort)data));
                else
                    master.DmaCartridgeCs1.WriteLong(address, data);
                return 0;
            }
        }
        else if (address is >= 0x5000000 and <= 0x57FFFFF)
        {
            // Dummy
        }
        else if (address is >= 0x5800000 and <= 0x58FFFFF)
        {
            // CD block CS2
            if (master.DmaPeripherals!.Ygr is { } ygr)
            {
                if (read)
                {
                    if (size == 0)
                        return master.DmaReadCs2Byte(address);
                    else if (size == 1)
                        return ygr.ABusReadWord(address);
                    else
                        return ygr.ABusReadLong(address);
                }
                else
                {
                    if (size == 0)
                        master.DmaWriteCs2Byte(address, unchecked((byte)data));
                    else if (size == 1)
                        ygr.ABusWriteWord(address, unchecked((ushort)data));
                    else
                        ygr.ABusWriteLong(address, data);
                    return 0;
                }
            }
            else
            {
                if (read)
                {
                    if (size == 0)
                        return master.DmaReadCs2Byte(address);
                    else if (size == 1)
                        return master.DmaPeripherals.Cs2.ReadWord(address);
                    else
                        return master.DmaPeripherals.Cs2.ReadLong(address);
                }
                else
                {
                    if (size == 0)
                        master.DmaWriteCs2Byte(address, unchecked((byte)data));
                    else if (size == 1)
                        master.DmaPeripherals.Cs2.WriteWord(address, unchecked((ushort)data));
                    else
                        master.DmaPeripherals.Cs2.WriteLong(address, data);
                    return 0;
                }
            }
        }
        else if (address is >= 0x5A00000 and <= 0x5AFFFFF)
        {
            // Sound RAM
            if (read)
            {
                if (size == 0)
                    return master.DmaPeripherals!.SoundRam.ReadByte(address);
                else if (size == 1)
                    return master.DmaPeripherals!.SoundRam.ReadWord(address);
                else
                    return master.DmaPeripherals!.SoundRam.ReadLong(address);
            }
            else
            {
                if (size == 0)
                    master.DmaPeripherals!.SoundRam.WriteByte(address, unchecked((byte)data));
                else if (size == 1)
                    master.DmaPeripherals!.SoundRam.WriteWord(address, unchecked((ushort)data));
                else
                    master.DmaPeripherals!.SoundRam.WriteLong(address, data);
                return 0;
            }
        }
        else if (address is >= 0x5B00000 and <= 0x5BFFFFF)
        {
            // SCSP registers
            if (read)
            {
                if (size == 0)
                    return master.DmaPeripherals!.Scsp.ReadByte(address);
                else if (size == 1)
                    return master.DmaPeripherals!.Scsp.ReadWord(address);
                else
                    return master.DmaPeripherals!.Scsp.ReadLong(address);
            }
            else
            {
                if (size == 0)
                    master.DmaPeripherals!.Scsp.WriteByte(address, unchecked((byte)data));
                else if (size == 1)
                    master.DmaPeripherals!.Scsp.WriteWord(address, unchecked((ushort)data));
                else
                    master.DmaPeripherals!.Scsp.WriteLong(address, data);
                return 0;
            }
        }
        else if (address is >= 0x5C00000 and <= 0x5C7FFFF)
        {
            // VDP1 RAM
            if (read)
            {
                if (size == 0)
                    return master.DmaPeripherals!.Vdp1.RamReadByte(address);
                else if (size == 1)
                    return master.DmaPeripherals!.Vdp1.RamReadWord(address);
                else
                    return master.DmaPeripherals!.Vdp1.RamReadLong(address);
            }
            else
            {
                if (size == 0)
                    master.DmaPeripherals!.Vdp1.RamWriteByte(address, unchecked((byte)data));
                else if (size == 1)
                    master.DmaPeripherals!.Vdp1.RamWriteWord(address, unchecked((ushort)data));
                else
                    master.DmaPeripherals!.Vdp1.RamWriteLong(address, data);
                return 0;
            }
        }
        else if (address is >= 0x5C80000 and <= 0x5CFFFFF)
        {
            // VDP1 framebuffer
            if (read)
            {
                if (size == 0)
                    return master.DmaPeripherals!.Vdp1.FrameBufferReadByte(address);
                else if (size == 1)
                    return master.DmaPeripherals!.Vdp1.FrameBufferReadWord(address);
                else
                    return master.DmaPeripherals!.Vdp1.FrameBufferReadLong(address);
            }
            else
            {
                if (size == 0)
                    master.DmaPeripherals!.Vdp1.FrameBufferWriteByte(address, unchecked((byte)data));
                else if (size == 1)
                    master.DmaPeripherals!.Vdp1.FrameBufferWriteWord(address, unchecked((ushort)data));
                else
                    master.DmaPeripherals!.Vdp1.FrameBufferWriteLong(address, data);
                return 0;
            }
        }
        else if (address is >= 0x5D00000 and <= 0x5D7FFFF)
        {
            // VDP1 registers
            if (read)
            {
                if (size == 0)
                    return master.DmaPeripherals!.Vdp1.ReadByte(address);
                else if (size == 1)
                    return master.DmaPeripherals!.Vdp1.ReadWord(address);
                else
                    return master.DmaPeripherals!.Vdp1.ReadLong(address);
            }
            else
            {
                if (size == 0)
                    master.DmaPeripherals!.Vdp1.WriteByte(address, unchecked((byte)data));
                else if (size == 1)
                    master.DmaPeripherals!.Vdp1.WriteWord(address, unchecked((ushort)data));
                else
                    master.DmaPeripherals!.Vdp1.WriteLong(address, data);
                return 0;
            }
        }
        else if (address is >= 0x5E00000 and <= 0x5EFFFFF)
        {
            // VDP2 RAM
            if (read)
            {
                if (size == 0)
                    return master.DmaPeripherals!.Vdp2.RamReadByte(address);
                else if (size == 1)
                    return master.DmaPeripherals!.Vdp2.RamReadWord(address);
                else
                    return master.DmaPeripherals!.Vdp2.RamReadLong(address);
            }
            else
            {
                if (size == 0)
                    master.DmaPeripherals!.Vdp2.RamWriteByte(address, unchecked((byte)data));
                else if (size == 1)
                    master.DmaPeripherals!.Vdp2.RamWriteWord(address, unchecked((ushort)data));
                else
                    master.DmaPeripherals!.Vdp2.RamWriteLong(address, data);
                return 0;
            }
        }
        else if (address is >= 0x5F00000 and <= 0x5F7FFFF)
        {
            // VDP2 color RAM
            if (read)
            {
                if (size == 0)
                    return master.DmaPeripherals!.Vdp2.ColorRamReadByte(address);
                else if (size == 1)
                    return master.DmaPeripherals!.Vdp2.ColorRamReadWord(address);
                else
                    return master.DmaPeripherals!.Vdp2.ColorRamReadLong(address);
            }
            else
            {
                if (size == 0)
                    master.DmaPeripherals!.Vdp2.ColorRamWriteByte(address, unchecked((byte)data));
                else if (size == 1)
                    master.DmaPeripherals!.Vdp2.ColorRamWriteWord(address, unchecked((ushort)data));
                else
                    master.DmaPeripherals!.Vdp2.ColorRamWriteLong(address, data);
                return 0;
            }
        }
        else if (address is >= 0x5F80000 and <= 0x5FBFFFF)
        {
            // VDP2 registers
            if (read)
            {
                if (size == 0)
                    return master.DmaPeripherals!.Vdp2.ReadByte(address);
                else if (size == 1)
                    return master.DmaPeripherals!.Vdp2.ReadWord(address);
                else
                    return master.DmaPeripherals!.Vdp2.ReadLong(address);
            }
            else
            {
                if (size == 0)
                    master.DmaPeripherals!.Vdp2.WriteByte(address, unchecked((byte)data));
                else if (size == 1)
                    master.DmaPeripherals!.Vdp2.WriteWord(address, unchecked((ushort)data));
                else
                    master.DmaPeripherals!.Vdp2.WriteLong(address, data);
                return 0;
            }
        }
        else if (address is >= 0x5FE0000 and <= 0x5FEFFFF)
        {
            // SCU registers
            if (read)
            {
                if (size == 0)
                    return master.DmaPeripherals!.Scu.ReadByte(address);
                else if (size == 1)
                    return master.DmaPeripherals!.Scu.ReadWord(address);
                else
                    return master.DmaPeripherals!.Scu.ReadLong(address);
            }
            else
            {
                if (size == 0)
                    master.DmaPeripherals!.Scu.WriteByte(address, unchecked((byte)data));
                else if (size == 1)
                    master.DmaPeripherals!.Scu.WriteWord(address, unchecked((ushort)data));
                else
                    master.DmaPeripherals!.Scu.WriteLong(address, data);
                return 0;
            }
        }
        else if (address is >= 0x6000000 and <= 0x7FFFFFF)
        {
            // High work RAM
            if (read)
            {
                if (size == 0)
                    return master.InstructionMemory.HighWramMemoryReadByte(address);
                else if (size == 1)
                    return master.InstructionMemory.HighWramMemoryReadWord(address);
                else
                    return master.InstructionMemory.HighWramMemoryReadLong(address);
            }
            else
            {
                if (size == 0)
                    master.InstructionMemory.HighWramMemoryWriteByte(address, unchecked((byte)data));
                else if (size == 1)
                    master.InstructionMemory.HighWramMemoryWriteWord(address, unchecked((ushort)data));
                else
                    master.InstructionMemory.HighWramMemoryWriteLong(address, data);
                return 0;
            }
        }

        System.Diagnostics.Debug.Assert(false, "Unhandled SH-2 DMA address.");
        return 0;
    }

    private void DmaTick(
        SaturnMemory.Sh2MemoryMap master, ref uint control, ref uint source, ref uint destination,
        ref uint count, uint vector, ref bool active, Func<uint, int, bool> checkABusWait,
        Action<int, int> sendInterrupt, Action<uint, uint> writeNotify)
    {
        int sourceIncrement = 0;
        int destinationIncrement = 0;
        uint destinationMode = (control >> 14) & 3;
        uint sourceMode = (control >> 12) & 3;
        int size = (int)((control >> 10) & 3);
        int checkSize = size;

        if (checkSize > 2)
            checkSize = 2;

        if (SaturnSh2Interpreter.CheckWait(source, checkSize, checkABusWait))
            return;

        if (destinationMode == 1)
            destinationIncrement = 1;
        else if (destinationMode == 2)
            destinationIncrement = -1;

        if (sourceMode == 1)
            sourceIncrement = 1;
        else if (sourceMode == 2)
            sourceIncrement = -1;

        if (size == 0)
        {
            byte data = unchecked((byte)Sh2DmaAccess(master, source, 0, true, 0));
            Sh2DmaAccess(master, destination, data, false, 0);
        }
        else if (size == 1)
        {
            ushort data = unchecked((ushort)Sh2DmaAccess(master, source, 0, true, 1));
            Sh2DmaAccess(master, destination, data, false, 1);
            sourceIncrement *= 2;
            destinationIncrement *= 2;
        }
        else if (size is 2 or 3)
        {
            uint data = Sh2DmaAccess(master, source, 0, true, 2);
            Sh2DmaAccess(master, destination, data, false, 2);
            sourceIncrement *= 4;
            destinationIncrement *= 4;
        }

        uint nextDestination = unchecked(destination + (uint)destinationIncrement);
        if (destinationIncrement > 0)
            writeNotify(destination, nextDestination);
        else
            writeNotify(nextDestination, destination);

        count = unchecked(count - 1);
        source = unchecked(source + (uint)sourceIncrement);
        destination = nextDestination;

        if (count == 0)
        {
            active = false;

            if (((control >> 2) & 1) != 0)
                sendInterrupt((int)vector, (_onchip.IPRA & 0xF00) >> 8);

            control |= 2;
        }
    }

    private void TickDma0(
        SaturnMemory.Sh2MemoryMap master, Func<uint, int, bool> checkABusWait,
        Action<int, int> sendInterrupt, Action<uint, uint> writeNotify) =>
        DmaTick(master, ref _onchip.CHCR0, ref _onchip.SAR0, ref _onchip.DAR0, ref _onchip.TCR0, _onchip.VCRDMA0,
            ref _onchip.Dma0Active, checkABusWait, sendInterrupt, writeNotify);

    private void TickDma1(
        SaturnMemory.Sh2MemoryMap master, Func<uint, int, bool> checkABusWait,
        Action<int, int> sendInterrupt, Action<uint, uint> writeNotify) =>
        DmaTick(master, ref _onchip.CHCR1, ref _onchip.SAR1, ref _onchip.DAR1, ref _onchip.TCR1, _onchip.VCRDMA1,
            ref _onchip.Dma1Active, checkABusWait, sendInterrupt, writeNotify);

    internal void Sh2DmaExec(
        uint cycles, SaturnMemory.Sh2MemoryMap master, Func<uint, int, bool> checkABusWait,
        Action<int, int> sendInterrupt, Action<uint, uint> writeNotify)
    {
        if (!dmaTimingEnabled || (!_onchip.Dma0Active && !_onchip.Dma1Active))
            return;

        for (uint cycle = 0; cycle < cycles; cycle++)
        {
            if (((_onchip.DMAOR >> 3) & 1) != 0)
            {
                if (_onchip.Dma0Active && _onchip.Dma1Active)
                {
                    if (_onchip.DmaRobin)
                    {
                        _onchip.DmaRobin = false;
                        TickDma0(master, checkABusWait, sendInterrupt, writeNotify);
                    }
                    else
                    {
                        TickDma1(master, checkABusWait, sendInterrupt, writeNotify);
                    }
                }
                else
                {
                    if (_onchip.Dma0Active)
                        TickDma0(master, checkABusWait, sendInterrupt, writeNotify);
                    if (_onchip.Dma1Active)
                        TickDma1(master, checkABusWait, sendInterrupt, writeNotify);
                }
            }
            else
            {
                if (_onchip.Dma0Active)
                    TickDma0(master, checkABusWait, sendInterrupt, writeNotify);
                else if (_onchip.Dma1Active)
                    TickDma1(master, checkABusWait, sendInterrupt, writeNotify);
            }
        }
    }

    private void DmaExec(SaturnMemory.Sh2MemoryMap memory, Action<int, int> sendInterrupt, Action<uint, uint> writeNotify)
    {
        if ((_onchip.DMAOR & 6) != 0)
            return;

        if ((_onchip.CHCR0 & 3) == 1 && (_onchip.CHCR1 & 3) == 1)
        {
            if ((_onchip.DMAOR & 8) != 0)
            {
                System.Diagnostics.Debug.WriteLine("dma\t: FIXME: two channel dma - round robin priority not properly implemented");
                DmaTransfer(memory, ref _onchip.CHCR0, ref _onchip.SAR0, ref _onchip.DAR0, ref _onchip.TCR0, _onchip.VCRDMA0, sendInterrupt, writeNotify);
                DmaTransfer(memory, ref _onchip.CHCR1, ref _onchip.SAR1, ref _onchip.DAR1, ref _onchip.TCR1, _onchip.VCRDMA1, sendInterrupt, writeNotify);
            }
            else
            {
                DmaTransfer(memory, ref _onchip.CHCR0, ref _onchip.SAR0, ref _onchip.DAR0, ref _onchip.TCR0, _onchip.VCRDMA0, sendInterrupt, writeNotify);
                DmaTransfer(memory, ref _onchip.CHCR1, ref _onchip.SAR1, ref _onchip.DAR1, ref _onchip.TCR1, _onchip.VCRDMA1, sendInterrupt, writeNotify);
            }
        }
        else
        {
            if ((_onchip.CHCR0 & 3) == 1)
            {
                DmaTransfer(memory, ref _onchip.CHCR0, ref _onchip.SAR0, ref _onchip.DAR0, ref _onchip.TCR0, _onchip.VCRDMA0, sendInterrupt, writeNotify);
                return;
            }
            if ((_onchip.CHCR1 & 3) == 1)
            {
                DmaTransfer(memory, ref _onchip.CHCR1, ref _onchip.SAR1, ref _onchip.DAR1, ref _onchip.TCR1, _onchip.VCRDMA1, sendInterrupt, writeNotify);
                return;
            }
        }
    }

    private void DmaTransfer(
        SaturnMemory.Sh2MemoryMap memory, ref uint control, ref uint source, ref uint destination,
        ref uint count, uint vector, Action<int, int> sendInterrupt, Action<uint, uint> writeNotify)
    {
        if ((control & 2) == 0)
        {
            int sourceIncrement;
            switch (control & 0x3000)
            {
                case 0x0000: sourceIncrement = 0; break;
                case 0x1000: sourceIncrement = 1; break;
                case 0x2000: sourceIncrement = -1; break;
                default: sourceIncrement = 0; break;
            }

            int destinationIncrement;
            switch (control & 0xC000)
            {
                case 0x0000: destinationIncrement = 0; break;
                case 0x4000: destinationIncrement = 1; break;
                case 0x8000: destinationIncrement = -1; break;
                default: destinationIncrement = 0; break;
            }

            uint index;
            switch ((control & 0x0C00) >> 10)
            {
                case 0:
                    for (index = 0; index < count; index++)
                    {
                        memory.MappedMemoryWriteByteNocache(destination, memory.MappedMemoryReadByteNocache(source));
                        source += unchecked((uint)sourceIncrement);
                        destination += unchecked((uint)destinationIncrement);
                    }
                    count = 0;
                    break;
                case 1:
                    destinationIncrement *= 2;
                    sourceIncrement *= 2;
                    for (index = 0; index < count; index++)
                    {
                        memory.MappedMemoryWriteWordNocache(destination, memory.MappedMemoryReadWordNocache(source));
                        source += unchecked((uint)sourceIncrement);
                        destination += unchecked((uint)destinationIncrement);
                    }
                    count = 0;
                    break;
                case 2:
                    destinationIncrement *= 4;
                    sourceIncrement *= 4;
                    for (index = 0; index < count; index++)
                    {
                        memory.MappedMemoryWriteLongNocache(destination, memory.MappedMemoryReadLongNocache(source));
                        destination = unchecked(destination + (uint)destinationIncrement);
                        source = unchecked(source + (uint)sourceIncrement);
                    }
                    count = 0;
                    break;
                case 3:
                    destinationIncrement *= 4;
                    sourceIncrement *= 4;
                    for (index = 0; index < count; index += 4)
                    {
                        for (int word = 0; word < 4; word++)
                        {
                            memory.MappedMemoryWriteLongNocache(destination, memory.MappedMemoryReadLongNocache(source));
                            destination += unchecked((uint)destinationIncrement);
                            source += unchecked((uint)sourceIncrement);
                        }
                    }
                    count = 0;
                    break;
                default:
                    index = 0;
                    break;
            }
            uint start = destinationIncrement < 0 ? destination : destination - index * (uint)destinationIncrement;
            writeNotify(start, index * (uint)Math.Abs(destinationIncrement));
        }

        if ((control & 4) != 0)
            sendInterrupt((int)vector, (_onchip.IPRA & 0xF00) >> 8);

        control |= 2;
    }

    internal void InputCaptureWriteWord()
    {
        _onchip.FTCSR |= 0x80;
        _onchip.FICR = _onchip.FRC;
        if ((_onchip.TIER & 0x80) != 0)
            SendInterrupt((_onchip.VCRC >> 8) & 0x7F, (_onchip.IPRB >> 8) & 0xF);
    }

    private static void SCITransmitByte(byte value) { }
}

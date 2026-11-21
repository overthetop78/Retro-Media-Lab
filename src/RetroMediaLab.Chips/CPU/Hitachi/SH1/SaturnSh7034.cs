// Copyright 2003-2005 Guillaume Duhamel
// Copyright 2004-2006, 2013 Theo Berkau
// Copyright 2016 James Laird-Wah
// Translated from Yabause sh7034.c.
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Diagnostics;
using RetroMediaLab.Chips.Controllers.Sega;
using RetroMediaLab.Chips.Controllers.Sega.CD;
using RetroMediaLab.Chips.CPU.Hitachi.SH2.Contracts;
using static RetroMediaLab.Chips.Controllers.Sega.SaturnMemoryAccess;
using static RetroMediaLab.Chips.CPU.Hitachi.SH1.SaturnSh7034State;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH1;

public sealed class SaturnSh7034(SaturnMemory memory, SaturnYgr ygr, Action<int, int> sendInterrupt) : ISh2Bus
{
    private readonly SaturnSh7034State _itu = new();
    private InterruptController _intc;
    private readonly DmaChannel[] _dmac = new DmaChannel[4];
    private ushort _dmaor;
    private ushort _padr, _pbdr, _pcdr, _cascr;
    private PortFunctionController _pfc;
    private BusStateController _bsc;
    private UserBreakController _ubc;
    private AnalogToDigitalConverter _adc;
    private TimingPatternController _tpc;
    private byte _standbyControl;
    private readonly SerialChannel[] _sci = new SerialChannel[2];
    private const byte SciTie = 0x80, SciRie = 0x40, SciTe = 0x20, SciRe = 0x10;
    private const byte SciTdre = 0x80, SciRdrf = 0x40, SciTend = 0x04;

    internal ushort Iprc => _intc.Iprc;
    public ushort Iprb => _intc.Iprb;
    internal SaturnMemory Memory => memory;
    internal ushort Iprd => _intc.Iprd;
    internal ref DmaChannel DmaChannel0 => ref _dmac[0];
    internal void SendDmaChannel0Interrupt() => sendInterrupt(72, (Iprc >> 12) & 0xF);

    public void Initialize()
    {
        Array.Clear(_itu.Ram);
        Array.Clear(_itu.Channels);
        Array.Clear(_dmac);
        _dmaor = 0;
        _intc = default;
        InitializeSci();
        _adc = new AnalogToDigitalConverter
        {
            Addra = 0,
            Addrb = 0,
            Addrc = 0,
            Addrd = 0,
            Adcsr = 0,
            Adcr = 0x7F
        };
        InitializeItu();
        InitializeIntc();
        _ubc = default;
        _bsc = default;
        _standbyControl = 0;
        _padr = 0;
        _pcdr = 0;
        _pfc = new PortFunctionController { Pacr1 = 0x3302, Pacr2 = 0xFF95 };
        _cascr = 0x5FFF;
        _tpc = new TimingPatternController { Tpmr = 0xF0, Tpcr = 0xFF };
        memory.Sh1Dram.Clear();
        _pbdr = 0x040C;
    }

    public byte Read8(uint address)
    {
        bool a27 = (address & 0x08000000) != 0;
        switch ((address >> 24) & 7)
        {
            case 0: return T2ReadByte(memory.Sh1Rom, address & 0xFFFF);
            case 1 when a27: return T2ReadByte(memory.Sh1Dram, address & 0x7FFFF);
            case 2 or 3 or 4 when a27: return ygr.Sh1ReadByte(address);
            case 5 when !a27: return ReadOnchipByte(address);
            case 6 when a27: return T2ReadByte(memory.Sh1MpegRom, address & 0x7FFFF);
            case 7: return T2ReadByte(_itu.Ram, address & 0x1FFF);
        }
        Debug.Fail("Unmapped SH-1 memory read.");
        return 0;
    }

    public void Write8(uint address, byte data)
    {
        bool a27 = (address & 0x08000000) != 0;
        switch ((address >> 24) & 7)
        {
            case 1 when a27: T2WriteByte(memory.Sh1Dram, address & 0x7FFFF, data); return;
            case 2 or 3 or 4 when a27: ygr.Sh1WriteByte(address, data); return;
            case 5 when !a27: WriteOnchipByte(address, data); return;
            case 6 when a27: return;
            case 7: T2WriteByte(_itu.Ram, address & 0x1FFF, data); return;
        }
        Debug.Fail("Unmapped SH-1 memory write.");
    }

    private byte ReadOnchipByte(uint address)
    {
        if (address is >= 0x05FFFE00 and <= 0x05FFFEBF)
            return 0;
        if (address is >= 0x05FFFEC0 and <= 0x05FFFEC5)
            return ReadSciByte(_sci[0], address - 0x05FFFEC0);
        if (address is >= 0x05FFFEC8 and <= 0x05FFFECD)
            return ReadSciByte(_sci[1], address - 0x05FFFEC8);
        if (address is 0x05FFFEC6 or 0x05FFFEC7 or (>= 0x05FFFECE and <= 0x05FFFEDF))
            return 0;
        if (address is >= 0x05FFFEE0 and <= 0x05FFFEE9)
        {
            uint offset = address - 0x05FFFEE0;
            ushort data = ReadAdcWord(offset & ~1u);
            return (offset & 1) == 0 ? (byte)(data >> 8) : unchecked((byte)data);
        }
        if (address is >= 0x05FFFEEA and <= 0x05FFFEFF)
            return 0;
        if (address is >= 0x05FFFF00 and <= 0x05FFFF3F)
            return ReadItuByte(address);
        if (address == 0x05FFFF48)
            return (byte)(_dmaor >> 8);
        if (address == 0x05FFFF49)
            return (byte)_dmaor;
        if (address is >= 0x05FFFF40 and <= 0x05FFFF7F && (address & 0xF) is 0xE or 0xF)
            return (byte)(_dmac[(address - 0x05FFFF40) >> 4].Chcr >> ((address & 1) == 0 ? 8 : 0));
        if (address is >= 0x05FFFF40 and <= 0x05FFFF7F)
            return 0;
        if (address is >= 0x05FFFF80 and <= 0x05FFFF8F)
            return ReadIntcByte(address);
        if (address is >= 0x05FFFF90 and <= 0x05FFFF99)
            return ReadUbcByte(address - 0x05FFFF90);
        if (address is >= 0x05FFFF9A and <= 0x05FFFF9F)
            return 0;
        if (address is >= 0x05FFFFA0 and <= 0x05FFFFB3)
            return unchecked((byte)(BusRegister((address - 0x05FFFFA0) & ~1u) >> ((address & 1) == 0 ? 8 : 0)));
        if (address == 0x05FFFFBC)
            return _standbyControl;
        if (address is >= 0x05FFFFB4 and <= 0x05FFFFBF)
            return 0;
        if (address is >= 0x05FFFFC0 and <= 0x05FFFFD1)
            return (byte)(PortRegister((address - 0x05FFFFC0) & ~1u) >> ((address & 1) == 0 ? 8 : 0));
        if (address is >= 0x05FFFFD2 and <= 0x05FFFFED)
            return 0;
        if (address == 0x05FFFFEE)
            return (byte)(_cascr >> 8);
        if (address == 0x05FFFFEF)
            return unchecked((byte)_cascr);
        if (address is >= 0x05FFFFF0 and <= 0x05FFFFF7)
            return TpcRegister(address - 0x05FFFFF0);
        if (address is >= 0x05FFFFF8 and <= 0x05FFFFFF)
            return 0;
        Debug.Fail($"SH-1 on-chip byte read at 0x{address:X8}.");
        return 0;
    }

    private void WriteOnchipByte(uint address, byte data)
    {
        if (address is >= 0x05FFFE00 and <= 0x05FFFEBF)
            return;
        if (address is >= 0x05FFFEC0 and <= 0x05FFFECD)
        {
            WriteSciByte(address - 0x05FFFEC0, data);
            return;
        }
        if (address is >= 0x05FFFECE and <= 0x05FFFEFF)
            return;
        if (address is >= 0x05FFFF00 and <= 0x05FFFF3F)
        {
            WriteItuByte(address, data);
            return;
        }
        if (address is >= 0x05FFFF40 and <= 0x05FFFF7F && (address & 0xF) is 0xE or 0xF)
        {
            ref DmaChannel channel = ref _dmac[(address - 0x05FFFF40) >> 4];
            channel.Chcr = (address & 1) == 0
                ? (ushort)((channel.Chcr & 0x00FF) | (data << 8))
                : (ushort)((channel.Chcr & 0xFF00) | (data & 0xFD));
            return;
        }
        if (address is >= 0x05FFFF40 and <= 0x05FFFF7F)
            return;
        if (address is >= 0x05FFFF80 and <= 0x05FFFF8F)
        {
            WriteIntcByte(address, data);
            return;
        }
        if (address is >= 0x05FFFF90 and <= 0x05FFFF99)
        {
            WriteUbcByte(address - 0x05FFFF90, data);
            return;
        }
        if (address is >= 0x05FFFF9A and <= 0x05FFFF9F)
            return;
        if (address is >= 0x05FFFFA0 and <= 0x05FFFFB3)
        {
            ref ushort register = ref BusRegister((address - 0x05FFFFA0) & ~1u);
            register = (address & 1) == 0
                ? (ushort)((register & 0x00FF) | (data << 8))
                : (ushort)((register & 0xFF00) | data);
            return;
        }
        if (address == 0x05FFFFBC)
        {
            _standbyControl = data;
            return;
        }
        if (address is >= 0x05FFFFB4 and <= 0x05FFFFBF)
            return;
        if (address is >= 0x05FFFFC0 and <= 0x05FFFFD1)
        {
            ref ushort register = ref PortRegister((address - 0x05FFFFC0) & ~1u);
            register = (address & 1) == 0
                ? (ushort)((register & 0x00FF) | (data << 8))
                : (ushort)((register & 0xFF00) | data);
            return;
        }
        if (address is >= 0x05FFFFD2 and <= 0x05FFFFED)
            return;
        if (address == 0x05FFFFEE)
        {
            _cascr = data;
            return;
        }
        if (address == 0x05FFFFEF)
            return;
        if (address is >= 0x05FFFFF0 and <= 0x05FFFFF7)
        {
            TpcRegister(address - 0x05FFFFF0) = data;
            return;
        }
        if (address is >= 0x05FFFFF8 and <= 0x05FFFFFF)
            return;
        Debug.Fail($"SH-1 on-chip byte write at 0x{address:X8}.");
    }

    public ushort Read16(uint address)
    {
        bool a27 = (address & 0x08000000) != 0;
        switch ((address >> 24) & 7)
        {
            case 0: return T2ReadWord(memory.Sh1Rom, address & 0xFFFF);
            case 1 when a27: return T2ReadWord(memory.Sh1Dram, address & 0x7FFFF);
            case 2 or 3 or 4 when a27: return ygr.Sh1ReadWord(address);
            case 5 when !a27: return ReadOnchipWord(address);
            case 6 when a27: return T2ReadWord(memory.Sh1MpegRom, address & 0x7FFFF);
            case 7: return T2ReadWord(_itu.Ram, address & 0x1FFF);
        }
        Debug.Fail("Unmapped SH-1 memory read.");
        return 0;
    }

    public void Write16(uint address, ushort data)
    {
        bool a27 = (address & 0x08000000) != 0;
        switch ((address >> 24) & 7)
        {
            case 1 when a27: T2WriteWord(memory.Sh1Dram, address & 0x7FFFF, data); return;
            case 2 or 3 or 4 when a27: ygr.Sh1WriteWord(address, data); return;
            case 5 when !a27: WriteOnchipWord(address, data); return;
            case 6 when a27: return;
            case 7: T2WriteWord(_itu.Ram, address & 0x1FFF, data); return;
        }
        Debug.Fail("Unmapped SH-1 memory write.");
    }

    private ushort ReadOnchipWord(uint address)
    {
        if (address is >= 0x05FFFE00 and <= 0x05FFFEBF)
            return 0;
        if (address is >= 0x05FFFEC0 and <= 0x05FFFEC5)
            return ReadSciWord(_sci[0], address - 0x05FFFEC0);
        if (address is >= 0x05FFFEC8 and <= 0x05FFFECD)
            return ReadSciWord(_sci[1], address - 0x05FFFEC8);
        if (address is 0x05FFFEC6 or 0x05FFFEC7 or (>= 0x05FFFECE and <= 0x05FFFEDF))
            return 0;
        if (address is 0x05FFFEE0 or 0x05FFFEE2 or 0x05FFFEE4 or 0x05FFFEE6 or 0x05FFFEE8)
            return ReadAdcWord(address - 0x05FFFEE0);
        if (address is >= 0x05FFFEEA and <= 0x05FFFEFF)
            return 0;
        if (address is >= 0x05FFFF00 and <= 0x05FFFF3F)
            return ReadItuWord(address);
        if (address is >= 0x05FFFF40 and <= 0x05FFFF7E)
            return ReadDmacWord(address);
        if (address is >= 0x05FFFF80 and <= 0x05FFFF8F)
            return ReadIntcWord(address);
        if (address is >= 0x05FFFF90 and <= 0x05FFFF99)
            return ReadUbcWord(address - 0x05FFFF90);
        if (address is >= 0x05FFFF9A and <= 0x05FFFF9F)
            return 0;
        if (address is >= 0x05FFFFA0 and <= 0x05FFFFB3 && (address & 1) == 0)
            return BusRegister(address - 0x05FFFFA0);
        if (address == 0x05FFFFBC)
            return (ushort)(_standbyControl << 8);
        if (address is >= 0x05FFFFB4 and <= 0x05FFFFBF)
            return 0;
        if (address is >= 0x05FFFFC0 and <= 0x05FFFFD0 && (address & 1) == 0)
            return PortRegister(address - 0x05FFFFC0);
        if (address is >= 0x05FFFFD2 and <= 0x05FFFFED)
            return 0;
        if (address == 0x05FFFFEE)
            return _cascr;
        if (address is 0x05FFFFF0 or 0x05FFFFF2 or 0x05FFFFF4 or 0x05FFFFF6)
        {
            uint offset = address - 0x05FFFFF0;
            return (ushort)((TpcRegister(offset) << 8) | TpcRegister(offset + 1));
        }
        if (address is >= 0x05FFFFF8 and <= 0x05FFFFFF)
            return 0;
        Debug.Fail($"SH-1 on-chip word read at 0x{address:X8}.");
        return 0;
    }

    private void WriteOnchipWord(uint address, ushort data)
    {
        if (address is >= 0x05FFFE00 and <= 0x05FFFEBF)
            return;
        if (address is >= 0x05FFFEC0 and <= 0x05FFFEC5)
        {
            WriteSciWord(ref _sci[0], address - 0x05FFFEC0, data);
            return;
        }
        if (address is >= 0x05FFFEC8 and <= 0x05FFFECD)
        {
            WriteSciWord(ref _sci[1], address - 0x05FFFEC8, data);
            return;
        }
        if (address is 0x05FFFEC6 or 0x05FFFEC7 or (>= 0x05FFFECE and <= 0x05FFFEFF))
            return;
        if (address is >= 0x05FFFF00 and <= 0x05FFFF3F)
        {
            WriteItuWord(address, data);
            return;
        }
        if (address is >= 0x05FFFF40 and <= 0x05FFFF7E)
        {
            WriteDmacWord(address, data);
            return;
        }
        if (address is >= 0x05FFFF80 and <= 0x05FFFF8F)
        {
            WriteIntcWord(address, data);
            return;
        }
        if (address is >= 0x05FFFF90 and <= 0x05FFFF99)
        {
            WriteUbcWord(address - 0x05FFFF90, data);
            return;
        }
        if (address is >= 0x05FFFF9A and <= 0x05FFFF9F)
            return;
        if (address is >= 0x05FFFFA0 and <= 0x05FFFFB3 && (address & 1) == 0)
        {
            BusRegister(address - 0x05FFFFA0) = data;
            return;
        }
        if (address == 0x05FFFFBC)
        {
            _standbyControl = (byte)(data >> 8);
            return;
        }
        if (address is >= 0x05FFFFB4 and <= 0x05FFFFBF)
            return;
        if (address is >= 0x05FFFFC0 and <= 0x05FFFFD0 && (address & 1) == 0)
        {
            PortRegister(address - 0x05FFFFC0) = data;
            return;
        }
        if (address is >= 0x05FFFFD2 and <= 0x05FFFFED)
            return;
        if (address == 0x05FFFFEE)
        {
            _cascr = data;
            return;
        }
        if (address is 0x05FFFFF0 or 0x05FFFFF2 or 0x05FFFFF4 or 0x05FFFFF6)
        {
            uint offset = address - 0x05FFFFF0;
            TpcRegister(offset) = (byte)(data >> 8);
            TpcRegister(offset + 1) = unchecked((byte)data);
            return;
        }
        if (address is >= 0x05FFFFF8 and <= 0x05FFFFFF)
            return;
        Debug.Fail($"SH-1 on-chip word write at 0x{address:X8}.");
    }

    public uint Read32(uint address)
    {
        bool a27 = (address & 0x08000000) != 0;
        switch ((address >> 24) & 7)
        {
            case 0: return T2ReadLong(memory.Sh1Rom, address & 0xFFFF);
            case 1 when a27: return T2ReadLong(memory.Sh1Dram, address & 0x7FFFF);
            case 2 or 3 or 4 when a27: return ygr.Sh1ReadLong(address);
            case 5 when !a27: return ReadOnchipLong(address);
            case 6 when a27: return T2ReadLong(memory.Sh1MpegRom, address & 0x7FFFF);
            case 7: return T2ReadLong(_itu.Ram, address & 0x1FFF);
        }
        return 0;
    }

    public void Write32(uint address, uint data)
    {
        bool a27 = (address & 0x08000000) != 0;
        switch ((address >> 24) & 7)
        {
            case 1 when a27: T2WriteLong(memory.Sh1Dram, address & 0x7FFFF, data); return;
            case 2 or 3 or 4 when a27: ygr.Sh1WriteLong(address, data); return;
            case 5 when !a27: WriteOnchipLong(address, data); return;
            case 6 when a27: return;
            case 7: T2WriteLong(_itu.Ram, address & 0x1FFF, data); return;
        }
    }

    private uint ReadOnchipLong(uint address)
    {
        if (address is >= 0x05FFFE00 and <= 0x05FFFEFF)
            return 0;
        if (address is >= 0x05FFFF00 and <= 0x05FFFF3F)
            return ReadItuLong(address);
        if (address is >= 0x05FFFF40 and <= 0x05FFFF7E)
            return ReadDmacLong(address);
        if (address is >= 0x05FFFF80 and <= 0x05FFFF8F)
            return ReadIntcLong(address);
        if (address is >= 0x05FFFF90 and <= 0x05FFFF99)
            return ReadUbcLong(address - 0x05FFFF90);
        if (address is >= 0x05FFFF9A and <= 0x05FFFF9F)
            return 0;
        if (address is >= 0x05FFFFA0 and <= 0x05FFFFB3 && (address & 1) == 0)
        {
            uint offset = address - 0x05FFFFA0;
            uint high = (uint)BusRegister(offset) << 16;
            return offset == 0x12 ? high : high | BusRegister(offset + 2);
        }
        if (address == 0x05FFFFBC)
            return (uint)_standbyControl << 24;
        if (address is >= 0x05FFFFB4 and <= 0x05FFFFBF)
            return 0;
        if (address is >= 0x05FFFFC0 and <= 0x05FFFFD0)
            return ReadPortLong(address - 0x05FFFFC0);
        if (address is >= 0x05FFFFD2 and <= 0x05FFFFED)
            return 0;
        if (address == 0x05FFFFEE)
            return (uint)_cascr << 16;
        if (address is >= 0x05FFFFF0 and <= 0x05FFFFFF)
            return 0;
        Debug.Fail($"SH-1 on-chip long read at 0x{address:X8}.");
        return 0;
    }

    private void WriteOnchipLong(uint address, uint data)
    {
        if (address is >= 0x05FFFE00 and <= 0x05FFFEFF)
            return;
        if (address is >= 0x05FFFF00 and <= 0x05FFFF3F)
        {
            WriteItuLong(address, data);
            return;
        }
        if (address is >= 0x05FFFF40 and <= 0x05FFFF7E)
        {
            WriteDmacLong(address, data);
            return;
        }
        if (address is >= 0x05FFFF80 and <= 0x05FFFF8F)
        {
            WriteIntcLong(address, data);
            return;
        }
        if (address is >= 0x05FFFF90 and <= 0x05FFFF99)
        {
            WriteUbcLong(address - 0x05FFFF90, data);
            return;
        }
        if (address is >= 0x05FFFF9A and <= 0x05FFFF9F)
            return;
        if (address is >= 0x05FFFFA0 and <= 0x05FFFFB3)
        {
            uint offset = address - 0x05FFFFA0;
            switch (offset)
            {
                case 0 or 2 or 4 or 6 or 8 or 0xA:
                    BusRegister(offset) = (ushort)(data >> 16);
                    BusRegister(offset + 2) = unchecked((ushort)data);
                    return;
                case 0xC or 0xE or 0x10 or 0x12:
                    return;
            }
        }
        if (address == 0x05FFFFBC)
        {
            _standbyControl = (byte)(data >> 24);
            return;
        }
        if (address is >= 0x05FFFFB4 and <= 0x05FFFFBF)
            return;
        if (address is >= 0x05FFFFC0 and <= 0x05FFFFD0)
        {
            WritePortLong(address - 0x05FFFFC0, data);
            return;
        }
        if (address is >= 0x05FFFFD2 and <= 0x05FFFFED)
            return;
        if (address == 0x05FFFFEE)
        {
            _cascr = (ushort)(data >> 16);
            return;
        }
        if (address is >= 0x05FFFFF0 and <= 0x05FFFFFF)
            return;
        Debug.Fail($"SH-1 on-chip long write at 0x{address:X8}.");
    }

    private ushort ReadAdcWord(uint offset) => offset switch
    {
        0 => _adc.Addra,
        2 => _adc.Addrb,
        4 => _adc.Addrc,
        6 => _adc.Addrd,
        8 => (ushort)((_adc.Adcsr << 8) | _adc.Adcr),
        _ => throw new NotSupportedException($"SH-1 A/D word read at offset 0x{offset:X}.")
    };

    private byte ReadUbcByte(uint offset)
    {
        // In sh7034.c the shift applies to the mask, so each BAR/BAMR byte read uses the low byte.
        return offset switch
        {
            <= 3 => unchecked((byte)_ubc.Bar),
            <= 7 => unchecked((byte)_ubc.Bamr),
            8 => (byte)(_ubc.Bbr >> 8),
            9 => unchecked((byte)_ubc.Bbr),
            _ => 0
        };
    }

    private void WriteUbcByte(uint offset, byte data)
    {
        int shift = (3 - (int)(offset & 3)) * 8;
        switch (offset)
        {
            case <= 3: _ubc.Bar = (_ubc.Bar & ~(0xFFu << shift)) | ((uint)data << shift); return;
            case <= 7: _ubc.Bamr = (_ubc.Bamr & ~(0xFFu << shift)) | ((uint)data << shift); return;
            case 8: _ubc.Bbr = (ushort)((_ubc.Bar & 0x00FF) | ((uint)data << 8)); return;
            case 9: _ubc.Bbr = (ushort)((_ubc.Bar & 0xFF00) | data); return;
        }
    }

    private ushort ReadUbcWord(uint offset)
    {
        switch (offset)
        {
            case 0: return (ushort)(_ubc.Bar >> 16);
            case 2: return unchecked((ushort)_ubc.Bar);
            case 4: return (ushort)(_ubc.Bamr >> 16);
            case 6: return unchecked((ushort)_ubc.Bamr);
            case 8: return _ubc.Bbr;
        }
        Debug.Fail($"SH-1 UBC word read at offset 0x{offset:X}.");
        return 0;
    }

    private void WriteUbcWord(uint offset, ushort data)
    {
        switch (offset)
        {
            case 0: _ubc.Bar = (_ubc.Bar & 0xFFFF) | ((uint)data << 16); return;
            case 2: _ubc.Bar = (_ubc.Bar & 0xFFFF0000) | data; return;
            case 4: _ubc.Bamr = (_ubc.Bamr & 0xFFFF) | ((uint)data << 16); return;
            case 6: _ubc.Bamr = (_ubc.Bamr & 0xFFFF0000) | data; return;
            case 8: _ubc.Bbr = data; return;
        }
        Debug.Fail($"SH-1 UBC word write at offset 0x{offset:X}.");
    }

    private uint ReadUbcLong(uint offset)
    {
        switch (offset)
        {
            case 0: return _ubc.Bar;
            case 2 or 6: return 0;
            case 4: return _ubc.Bamr;
            case 8: return _ubc.Bar << 16;
        }
        Debug.Fail($"SH-1 UBC long read at offset 0x{offset:X}.");
        return 0;
    }

    private void WriteUbcLong(uint offset, uint data)
    {
        switch (offset)
        {
            case 0: _ubc.Bar = data; return;
            case 2 or 6: return;
            case 4: _ubc.Bamr = data; return;
            case 8: _ubc.Bbr = (ushort)(data >> 16); return;
        }
        Debug.Fail($"SH-1 UBC long write at offset 0x{offset:X}.");
    }

    private ref byte TpcRegister(uint offset)
    {
        switch (offset)
        {
            case 0: return ref _tpc.Tpmr;
            case 1: return ref _tpc.Tpcr;
            case 2: return ref _tpc.Nderb;
            case 3: return ref _tpc.Ndera;
            case 4 or 6: return ref _tpc.Ndrb;
            case 5 or 7: return ref _tpc.Ndra;
        }
        throw new NotSupportedException($"SH-1 TPC register at offset 0x{offset:X}.");
    }

    private ref ushort BusRegister(uint offset)
    {
        switch (offset)
        {
            case 0x00: return ref _bsc.Bcr;
            case 0x02: return ref _bsc.Wcr1;
            case 0x04: return ref _bsc.Wcr2;
            case 0x06: return ref _bsc.Wcr3;
            case 0x08: return ref _bsc.Dcr;
            case 0x0A: return ref _bsc.Pcr;
            case 0x0C: return ref _bsc.Rcr;
            case 0x0E: return ref _bsc.Rtcsr;
            case 0x10: return ref _bsc.Rtcnt;
            case 0x12: return ref _bsc.Rtcor;
        }
        throw new NotSupportedException($"SH-1 bus register at offset 0x{offset:X}.");
    }

    private ref ushort PortRegister(uint offset)
    {
        switch (offset)
        {
            case 0x0: return ref _padr;
            case 0x2: return ref _pbdr;
            case 0x4: return ref _pfc.Paior;
            case 0x6: return ref _pfc.Pbior;
            case 0x8: return ref _pfc.Pacr1;
            case 0xA: return ref _pfc.Pacr2;
            case 0xC: return ref _pfc.Pbcr1;
            case 0xE: return ref _pfc.Pbcr2;
            case 0x10: return ref _pcdr;
        }
        throw new NotSupportedException($"SH-1 port register at offset 0x{offset:X}.");
    }

    private uint ReadPortLong(uint offset)
    {
        switch (offset)
        {
            case 0 or 2 or 4 or 6 or 8 or 0xA: return ((uint)PortRegister(offset) << 16) | PortRegister(offset + 2);
            case 0xC or 0xE: return 0;
            case 0x10: return (uint)_pcdr << 16;
        }
        Debug.Fail($"SH-1 port long read at offset 0x{offset:X}.");
        return 0;
    }

    private void WritePortLong(uint offset, uint data)
    {
        ushort high = (ushort)(data >> 16);
        ushort low = unchecked((ushort)data);
        switch (offset)
        {
            case 0:
                _padr = (ushort)((_padr & ~_pfc.Paior) | (high & _pfc.Paior));
                _pbdr = (ushort)((_pbdr & ~_pfc.Pbior) | (low & _pfc.Pbior));
                return;
            case 2 or 4 or 6 or 8 or 0xA or 0xC or 0xE:
                PortRegister(offset) = high;
                PortRegister(offset + 2) = low;
                return;
            case 0x10:
                _pcdr = high;
                return;
        }
        Debug.Fail($"SH-1 port long write at offset 0x{offset:X}.");
    }

    private ushort ReadDmacWord(uint address)
    {
        if (address == 0x05FFFF48)
            return _dmaor;

        ref DmaChannel channel = ref _dmac[(address - 0x05FFFF40) >> 4];
        switch ((address & 0xF))
        {
            case 0: return (ushort)(channel.Sar >> 16);
            case 2: return (ushort)channel.Sar;
            case 4: return (ushort)(channel.Dar >> 16);
            case 6: return (ushort)channel.Dar;
            case 8 or 0xC: return 0;
            case 0xA: return channel.Tcr;
            case 0xE: return channel.Chcr;
        }
        Debug.Fail($"SH-1 DMA word read at 0x{address:X8}.");
        return 0;
    }

    private void WriteDmacWord(uint address, ushort data)
    {
        if (address == 0x05FFFF48)
        {
            _dmaor = (ushort)(data & 0xFFF9);
            return;
        }

        ref DmaChannel channel = ref _dmac[(address - 0x05FFFF40) >> 4];
        switch (address & 0xF)
        {
            case 0: channel.Sar = (channel.Sar & 0xFFFF) | ((uint)data << 16); return;
            case 2: channel.Sar = (channel.Sar & 0xFFFF0000) | data; return;
            case 4: channel.Dar = (channel.Dar & 0xFFFF) | ((uint)data << 16); return;
            case 6: channel.Dar = (channel.Dar & 0xFFFF0000) | data; return;
            case 8 or 0xC: return;
            case 0xA: channel.Tcr = data; return;
            case 0xE: channel.Chcr = (ushort)(data & 0xFFFD); return;
        }
        Debug.Fail($"SH-1 DMA word write at 0x{address:X8}.");
    }

    private uint ReadDmacLong(uint address)
    {
        if (address == 0x05FFFF48)
            return (uint)_dmaor << 16;

        ref DmaChannel channel = ref _dmac[(address - 0x05FFFF40) >> 4];
        switch ((address & 0xF))
        {
            case 0: return channel.Sar;
            case 4: return channel.Dar;
            case 8 or 0xC: return 0;
            case 0xA: return channel.Tcr;
            case 0xE: return (uint)channel.Chcr << 16;
        }
        Debug.Fail($"SH-1 DMA long read at 0x{address:X8}.");
        return 0;
    }

    private void WriteDmacLong(uint address, uint data)
    {
        if (address == 0x05FFFF48)
        {
            _dmaor = (ushort)(data & 0xFFF9);
            return;
        }

        ref DmaChannel channel = ref _dmac[(address - 0x05FFFF40) >> 4];
        switch (address & 0xF)
        {
            case 0: channel.Sar = data; return;
            case 4: channel.Dar = data; return;
            case 8 or 0xC: return;
            case 0xA: channel.Tcr = (ushort)data; return;
            case 0xE: channel.Chcr = (ushort)((data >> 16) & 0xFFFD); return;
        }
        Debug.Fail($"SH-1 DMA long write at 0x{address:X8}.");
    }

    private void InitializeSci()
    {
        Array.Clear(_sci);
        for (int channel = 0; channel < 2; channel++)
        {
            _sci[channel].Smr = 0;
            _sci[channel].Brr = 0xFF;
            _sci[channel].Scr = 0;
            _sci[channel].Tdr = 0xFF;
            _sci[channel].Ssr = 0x84;
            _sci[channel].Rdr = 0;
        }
    }

    private static byte ReadSciByte(in SerialChannel channel, uint offset)
    {
        switch (offset)
        {
            case 0: return channel.Smr;
            case 1: return channel.Brr;
            case 2: return channel.Scr;
            case 3: return channel.Tdr;
            case 4: return channel.Ssr;
            case 5: return channel.Rdr;
        }
        Debug.Fail("Unknown SH-1 SCI byte register.");
        return 0;
    }

    private void WriteSciByte(uint offset, byte data)
    {
        switch (offset)
        {
            case 0: _sci[0].Smr = data; return;
            case 1: _sci[0].Brr = data; return;
            case 2:
                _sci[0].Scr = data;
                if ((data & SciTe) == 0)
                    _sci[0].Ssr |= SciTend;
                return;
            case 3:
                _sci[0].Tdr = data;
                if (_sci[0].TsrCounter == 0)
                {
                    _sci[0].Tsr = data;
                    _sci[0].TsrCounter = 8;
                    _sci[0].Ssr |= SciTdre;
                }
                else
                {
                    _sci[0].TdrWritten = true;
                    _sci[0].Ssr &= unchecked((byte)~SciTdre);
                }
                return;
            case 4:
                if (data == 0)
                {
                    bool clearTe = (_sci[0].Ssr & SciTdre) != 0;
                    _sci[0].Ssr &= 6;
                    if (clearTe)
                        _sci[0].Ssr &= unchecked((byte)~SciTend);
                }
                else
                    Debug.Fail("SH-1 SCI0 status accepts only a zero byte.");
                return;
            case 5:
            case 6:
            case 7: return;
            case 8: _sci[1].Smr = data; return;
            case 9: _sci[1].Brr = data; return;
            case 0xA: _sci[1].Scr = data; return;
            case 0xB:
                _sci[1].Tdr = data;
                _sci[1].TdrWritten = true;
                return;
            case 0xC:
                if (data == 0)
                    _sci[1].Ssr = data;
                return;
            case 0xD: return;
        }
        Debug.Fail("Unknown SH-1 SCI byte register.");
    }

    private static ushort ReadSciWord(in SerialChannel channel, uint offset)
    {
        switch (offset)
        {
            case 0: return (ushort)((channel.Smr << 8) | channel.Brr);
            case 2: return (ushort)((channel.Scr << 8) | channel.Tdr);
            case 4: return (ushort)((channel.Ssr << 8) | channel.Rdr);
        }
        Debug.Fail("Unknown SH-1 SCI word register.");
        return 0;
    }

    private static void WriteSciWord(ref SerialChannel channel, uint offset, ushort data)
    {
        switch (offset)
        {
            case 0:
                channel.Smr = (byte)(data >> 8);
                channel.Brr = (byte)(data & 0xFF);
                return;
            case 2:
                channel.Scr = (byte)(data >> 8);
                channel.Tdr = (byte)(data & 0xFF);
                return;
            case 4:
                channel.Ssr = (byte)(data >> 8);
                return;
        }
        Debug.Fail("Unknown SH-1 SCI word register.");
    }

    private void ReceiveSerialBit(int bit, int channel)
    {
        ref SerialChannel serial = ref _sci[channel];
        serial.Rsr = unchecked((byte)((serial.Rsr << 1) | bit));
        serial.RsrCounter++;
        if (serial.RsrCounter == 8)
        {
            serial.RsrCounter = 0;
            serial.Rdr = serial.Rsr;
            serial.Rsr = 0;
            serial.Ssr |= SciRdrf;
            if ((_sci[0].Scr & SciRie) != 0)
                sendInterrupt(101, Iprd & 0xF);
        }
    }

    private int TransmitSerialBit(int channel)
    {
        ref SerialChannel serial = ref _sci[channel];
        int bit = serial.Tsr & 1;
        if (serial.TsrCounter != 0)
        {
            serial.Tsr >>= 1;
            serial.TsrCounter--;
        }
        if (serial.TsrCounter == 0)
        {
            if (serial.TdrWritten)
            {
                serial.Tsr = serial.Tdr;
                serial.TsrCounter = 8;
                serial.TdrWritten = false;
                serial.Ssr |= SciTdre;
            }
            else
                serial.Ssr |= SciTend;
            Debug.Assert((_sci[0].Scr & SciTie) == 0);
        }
        return bit;
    }

    private void TickSerial(int channel, SaturnCdDrive drive)
    {
        ref SerialChannel serial = ref _sci[channel];
        int cyclesPerBit = (serial.Brr + 1) * 4;
        if ((_sci[0].Ssr & SciTend) != 0)
        {
            serial.SerialClockCounter = 0;
            return;
        }

        Debug.Assert((serial.Smr & 3) < 2);
        serial.SerialClockCounter++;
        if (serial.SerialClockCounter > cyclesPerBit)
        {
            if ((serial.Scr & SciTe) != 0 && (serial.Scr & SciRe) != 0)
            {
                ReceiveSerialBit(drive.GetSerialBit(), 0);
                drive.SetSerialBit(TransmitSerialBit(0));
            }
            serial.SerialClockCounter = 0;
        }
    }

    internal void RunOnchipCycles(int cycles, SaturnCdDrive drive)
    {
        TickTimerFast(3, cycles);
        TickTimerFast(4, cycles);
        for (int cycle = 0; cycle < cycles; cycle++)
            TickSerial(0, drive);
    }

    internal void InitializeIntc()
    {
        _intc.Ipra = 0;
        _intc.Iprb = 0;
        _intc.Iprc = 0;
        _intc.Iprd = 0;
        _intc.Ipre = 0;
    }

    internal void InitializeItu()
    {
        _itu.Tstr = 0xE0;
        _itu.Tsnc = 0xE0;
        _itu.Tmdr = 0x80;
        _itu.Tfcr = 0xC0;
        _itu.Tocr = 0xFF;

        for (int index = 0; index < 5; index++)
        {
            ref TimerChannel timer = ref _itu.Channels[index];
            timer.Tcr = 0x80;
            timer.Tior = 0x88;
            timer.Tier = 0xF8;
            timer.Tsr = 0xF8;
            timer.Tcnt = 0;
            timer.Gra = 0xFF;
            timer.Grb = 0xFF;
            timer.Bra = 0xFF;
            timer.Brb = 0xFF;
        }
    }

    private static byte ReadTimerByte(in TimerChannel timer, uint offset)
    {
        switch (offset)
        {
            case 0: return timer.Tcr;
            case 1: return timer.Tior;
            case 2: return timer.Tier;
            case 3: return timer.Tsr;
            case 4: return (byte)(timer.Tcnt >> 8);
            case 5: return (byte)(timer.Tcnt & 0xFF);
            case 6: return (byte)(timer.Gra >> 8);
            case 7: return (byte)(timer.Gra & 0xFF);
            case 8: return (byte)(timer.Grb >> 8);
            case 9: return (byte)(timer.Grb & 0xFF);
            case 10: return (byte)(timer.Bra >> 8);
            case 11: return (byte)(timer.Bra & 0xFF);
            case 12: return (byte)(timer.Brb >> 8);
            case 13: return (byte)(timer.Brb & 0xFF);
        }
        Debug.Fail("Unknown SH-1 timer register.");
        return 0;
    }

    private static void WriteTimerByte(ref TimerChannel timer, uint offset, byte data)
    {
        switch (offset)
        {
            case 0: timer.Tcr = data; return;
            case 1: timer.Tior = data; return;
            case 2: timer.Tier = data; return;
            case 3:
                if ((data & 1) == 0) timer.Tsr &= 0xFE;
                if ((data & 2) == 0) timer.Tsr &= 0xFD;
                if ((data & 4) == 0) timer.Tsr &= 0xFB;
                return;
            case 4: timer.Tcnt = (ushort)((timer.Tcnt & 0xFF) | (data << 8)); return;
            case 5: timer.Tcnt = (ushort)((timer.Tcnt & 0xFF00) | data); return;
            case 6: timer.Gra = (ushort)((timer.Gra & 0xFF) | (data << 8)); return;
            case 7: timer.Gra = (ushort)((timer.Gra & 0xFF00) | data); return;
            case 8: timer.Grb = (ushort)((timer.Grb & 0xFF) | (data << 8)); return;
            case 9: timer.Grb = (ushort)((timer.Grb & 0xFF00) | data); return;
            case 10: timer.Bra = (ushort)((timer.Bra & 0xFF) | (data << 8)); return;
            case 11: timer.Bra = (ushort)((timer.Bra & 0xFF00) | data); return;
            case 12: timer.Brb = (ushort)((timer.Brb & 0xFF) | (data << 8)); return;
            case 13: timer.Brb = (ushort)((timer.Brb & 0xFF00) | data); return;
        }
        Debug.Fail("Unknown SH-1 timer register.");
    }

    internal byte ReadItuByte(uint address)
    {
        switch (address)
        {
            case 0x05FFFF00: return _itu.Tstr;
            case 0x05FFFF01: return _itu.Tsnc;
            case 0x05FFFF02: return _itu.Tmdr;
            case 0x05FFFF03: return _itu.Tfcr;
            case >= 0x05FFFF04 and <= 0x05FFFF0D: return ReadTimerByte(_itu.Channels[0], address - 0x05FFFF04);
            case >= 0x05FFFF0E and <= 0x05FFFF17: return ReadTimerByte(_itu.Channels[1], address - 0x05FFFF0E);
            case >= 0x05FFFF18 and <= 0x05FFFF21: return ReadTimerByte(_itu.Channels[2], address - 0x05FFFF18);
            case >= 0x05FFFF22 and <= 0x05FFFF2F: return ReadTimerByte(_itu.Channels[3], address - 0x05FFFF22);
            case 0x05FFFF30: return 0;
            case 0x05FFFF31: return _itu.Tocr;
            case >= 0x05FFFF32 and <= 0x05FFFF3F: return ReadTimerByte(_itu.Channels[4], address - 0x05FFFF32);
        }
        Debug.Fail("Address outside SH-1 ITU registers.");
        return 0;
    }

    internal void WriteItuByte(uint address, byte data)
    {
        switch (address)
        {
            case 0x05FFFF00: _itu.Tstr = data; return;
            case 0x05FFFF01: _itu.Tsnc = data; return;
            case 0x05FFFF02: _itu.Tmdr = data; return;
            case 0x05FFFF03: _itu.Tfcr = data; return;
            case >= 0x05FFFF04 and <= 0x05FFFF0D: WriteTimerByte(ref _itu.Channels[0], address - 0x05FFFF04, data); return;
            case >= 0x05FFFF0E and <= 0x05FFFF17: WriteTimerByte(ref _itu.Channels[1], address - 0x05FFFF0E, data); return;
            case >= 0x05FFFF18 and <= 0x05FFFF21: WriteTimerByte(ref _itu.Channels[2], address - 0x05FFFF18, data); return;
            case >= 0x05FFFF22 and <= 0x05FFFF2F: WriteTimerByte(ref _itu.Channels[3], address - 0x05FFFF22, data); return;
            case 0x05FFFF30: return;
            case 0x05FFFF31: _itu.Tocr = data; return;
            case >= 0x05FFFF32 and <= 0x05FFFF3F: WriteTimerByte(ref _itu.Channels[4], address - 0x05FFFF32, data); return;
        }
        Debug.Fail("Address outside SH-1 ITU registers.");
    }

    internal ushort ReadItuWord(uint address)
    {
        switch (address)
        {
            case >= 0x05FFFF00 and <= 0x05FFFF07:
            case >= 0x05FFFF0E and <= 0x05FFFF11:
            case >= 0x05FFFF18 and <= 0x05FFFF1B:
            case >= 0x05FFFF22 and <= 0x05FFFF25:
            case 0x05FFFF30:
            case >= 0x05FFFF32 and <= 0x05FFFF35:
                return 0;
            case 0x05FFFF08: return _itu.Channels[0].Tcnt;
            case 0x05FFFF0A: return _itu.Channels[0].Gra;
            case 0x05FFFF0C: return _itu.Channels[0].Grb;
            case 0x05FFFF12: return _itu.Channels[1].Tcnt;
            case 0x05FFFF14: return _itu.Channels[1].Gra;
            case 0x05FFFF16: return _itu.Channels[1].Grb;
            case 0x05FFFF1C: return _itu.Channels[2].Tcnt;
            case 0x05FFFF1E: return _itu.Channels[2].Gra;
            case 0x05FFFF20: return _itu.Channels[2].Grb;
            case 0x05FFFF26: return _itu.Channels[3].Tcnt;
            case 0x05FFFF28: return _itu.Channels[3].Gra;
            case 0x05FFFF2A: return _itu.Channels[3].Grb;
            case 0x05FFFF2C: return _itu.Channels[3].Bra;
            case 0x05FFFF2E: return _itu.Channels[3].Brb;
            case 0x05FFFF36: return _itu.Channels[4].Tcnt;
            case 0x05FFFF38: return _itu.Channels[4].Gra;
            case 0x05FFFF3A: return _itu.Channels[4].Grb;
            case 0x05FFFF3C: return _itu.Channels[4].Bra;
            case 0x05FFFF3E: return _itu.Channels[4].Brb;
        }
        Debug.Fail("Unknown SH-1 timer word register.");
        return 0;
    }

    internal void WriteItuWord(uint address, ushort data)
    {
        switch (address)
        {
            case >= 0x05FFFF00 and <= 0x05FFFF07:
            case >= 0x05FFFF0E and <= 0x05FFFF11:
            case >= 0x05FFFF18 and <= 0x05FFFF1B:
            case >= 0x05FFFF22 and <= 0x05FFFF25:
            case 0x05FFFF30:
            case >= 0x05FFFF32 and <= 0x05FFFF35:
                return;
            case 0x05FFFF08: _itu.Channels[0].Tcnt = data; return;
            case 0x05FFFF0A: _itu.Channels[0].Gra = data; return;
            case 0x05FFFF0C: _itu.Channels[0].Grb = data; return;
            case 0x05FFFF12: _itu.Channels[1].Tcnt = data; return;
            case 0x05FFFF14: _itu.Channels[1].Gra = data; return;
            case 0x05FFFF16: _itu.Channels[1].Grb = data; return;
            case 0x05FFFF1C: _itu.Channels[2].Tcnt = data; return;
            case 0x05FFFF1E: _itu.Channels[2].Gra = data; return;
            case 0x05FFFF20: _itu.Channels[2].Grb = data; return;
            case 0x05FFFF26: _itu.Channels[3].Tcnt = data; return;
            case 0x05FFFF28: _itu.Channels[3].Gra = data; return;
            case 0x05FFFF2A: _itu.Channels[3].Grb = data; return;
            case 0x05FFFF2C: _itu.Channels[3].Bra = data; return;
            case 0x05FFFF2E: _itu.Channels[3].Brb = data; return;
            case 0x05FFFF36: _itu.Channels[4].Tcnt = data; return;
            case 0x05FFFF38: _itu.Channels[4].Gra = data; return;
            case 0x05FFFF3A: _itu.Channels[4].Grb = data; return;
            case 0x05FFFF3C: _itu.Channels[4].Bra = data; return;
            case 0x05FFFF3E: _itu.Channels[4].Brb = data; return;
        }
        Debug.Fail("Unknown SH-1 timer word register.");
    }

    internal uint ReadItuLong(uint address)
    {
        switch (address)
        {
            case >= 0x05FFFF00 and <= 0x05FFFF07:
            case >= 0x05FFFF0E and <= 0x05FFFF11:
            case >= 0x05FFFF18 and <= 0x05FFFF1B:
            case >= 0x05FFFF22 and <= 0x05FFFF25:
            case 0x05FFFF30:
            case >= 0x05FFFF32 and <= 0x05FFFF35:
                return 0;
            case 0x05FFFF08: return ((uint)_itu.Channels[0].Tcnt << 16) | _itu.Channels[0].Gra;
            case 0x05FFFF0A: return ((uint)_itu.Channels[0].Gra << 16) | _itu.Channels[0].Grb;
            case 0x05FFFF0C: return 0;
            case 0x05FFFF12: return 0;
            case 0x05FFFF14: return ((uint)_itu.Channels[1].Gra << 16) | _itu.Channels[1].Grb;
            case 0x05FFFF16: return ((uint)_itu.Channels[1].Grb << 16);
            case 0x05FFFF1C: return ((uint)_itu.Channels[2].Tcnt << 16) | _itu.Channels[2].Gra;
            case 0x05FFFF1E: return ((uint)_itu.Channels[2].Gra << 16) | _itu.Channels[2].Grb;
            case 0x05FFFF20: return 0;
            case 0x05FFFF26: return 0;
            case 0x05FFFF28: return ((uint)_itu.Channels[3].Gra << 16) | _itu.Channels[3].Grb;
            case 0x05FFFF2A: return ((uint)_itu.Channels[3].Grb << 16) | _itu.Channels[3].Bra;
            case 0x05FFFF2C: return ((uint)_itu.Channels[3].Bra << 16) | _itu.Channels[3].Brb;
            case 0x05FFFF2E: return ((uint)_itu.Channels[3].Brb << 16);
            case 0x05FFFF36: return 0;
            case 0x05FFFF38: return ((uint)_itu.Channels[4].Gra << 16) | _itu.Channels[4].Grb;
            case 0x05FFFF3A: return ((uint)_itu.Channels[4].Grb << 16) | _itu.Channels[4].Bra;
            case 0x05FFFF3C: return ((uint)_itu.Channels[4].Bra << 16) | _itu.Channels[4].Brb;
            case 0x05FFFF3E: return ((uint)_itu.Channels[4].Brb << 16);
        }
        Debug.Fail("Unknown SH-1 timer long register.");
        return 0;
    }

    internal void WriteItuLong(uint address, uint data)
    {
        ushort high = (ushort)(data >> 16);
        switch (address)
        {
            case >= 0x05FFFF00 and <= 0x05FFFF07:
            case >= 0x05FFFF0E and <= 0x05FFFF11:
            case >= 0x05FFFF18 and <= 0x05FFFF1B:
            case >= 0x05FFFF22 and <= 0x05FFFF25:
            case 0x05FFFF30:
            case >= 0x05FFFF32 and <= 0x05FFFF35:
                return;
            case 0x05FFFF08:
                _itu.Channels[0].Tcnt = high;
                _itu.Channels[0].Gra = high;
                return;
            case 0x05FFFF0A:
                _itu.Channels[0].Gra = high;
                _itu.Channels[0].Grb = high;
                return;
            case 0x05FFFF0C:
                return;
            case 0x05FFFF12:
                return;
            case 0x05FFFF14:
                _itu.Channels[1].Gra = high;
                _itu.Channels[1].Grb = high;
                return;
            case 0x05FFFF16:
                _itu.Channels[1].Grb = high;
                return;
            case 0x05FFFF1C:
                _itu.Channels[2].Tcnt = high;
                _itu.Channels[2].Gra = high;
                return;
            case 0x05FFFF1E:
                _itu.Channels[2].Gra = high;
                _itu.Channels[2].Grb = high;
                return;
            case 0x05FFFF20:
                return;
            case 0x05FFFF26:
                return;
            case 0x05FFFF28:
                _itu.Channels[3].Gra = high;
                _itu.Channels[3].Grb = high;
                return;
            case 0x05FFFF2A:
                _itu.Channels[3].Grb = high;
                _itu.Channels[3].Bra = high;
                return;
            case 0x05FFFF2C:
                _itu.Channels[3].Bra = high;
                _itu.Channels[3].Brb = high;
                return;
            case 0x05FFFF2E:
                _itu.Channels[3].Brb = high;
                return;
            case 0x05FFFF36:
                return;
            case 0x05FFFF38:
                _itu.Channels[4].Gra = high;
                _itu.Channels[4].Grb = high;
                return;
            case 0x05FFFF3A:
                _itu.Channels[4].Grb = high;
                _itu.Channels[4].Bra = high;
                return;
            case 0x05FFFF3C:
                _itu.Channels[4].Bra = high;
                _itu.Channels[4].Brb = high;
                return;
            case 0x05FFFF3E:
                _itu.Channels[4].Brb = high;
                return;
        }
        Debug.Fail("Unknown SH-1 timer long register.");
    }

    internal byte ReadIntcByte(uint address)
    {
        switch (address)
        {
            case 0x05FFFF84: return (byte)(_intc.Ipra >> 8);
            case 0x05FFFF85: return (byte)(_intc.Ipra & 0xFF);
            case 0x05FFFF86: return (byte)(_intc.Iprb >> 8);
            case 0x05FFFF87: return (byte)(_intc.Iprb & 0xFF);
            case 0x05FFFF88: return (byte)(_intc.Iprc >> 8);
            case 0x05FFFF89: return (byte)(_intc.Iprc & 0xFF);
            case 0x05FFFF8A: return (byte)(_intc.Iprd >> 8);
            case 0x05FFFF8B: return (byte)(_intc.Iprd & 0xFF);
            case 0x05FFFF8C: return (byte)(_intc.Ipre >> 8);
            case 0x05FFFF8D: return (byte)(_intc.Ipre & 0xFF);
            case 0x05FFFF8E: return (byte)(_intc.Icr >> 8);
            case 0x05FFFF8F: return (byte)(_intc.Icr & 0xFF);
        }
        Debug.Fail("Unknown SH-1 interrupt byte register.");
        return 0;
    }

    internal void WriteIntcByte(uint address, byte data)
    {
        switch (address)
        {
            case >= 0x05FFFF80 and <= 0x05FFFF83: return;
            case 0x05FFFF84: _intc.Ipra = (ushort)((_intc.Ipra & 0xFF) | (data << 8)); return;
            case 0x05FFFF85: _intc.Ipra = (ushort)((_intc.Ipra & 0xFF00) | data); return;
            case 0x05FFFF86: _intc.Iprb = (ushort)((_intc.Iprb & 0xFF) | (data << 8)); return;
            case 0x05FFFF87: _intc.Iprb = (ushort)((_intc.Iprb & 0xFF00) | data); return;
            case 0x05FFFF88: _intc.Iprc = (ushort)((_intc.Iprc & 0xFF) | (data << 8)); return;
            case 0x05FFFF89: _intc.Iprc = (ushort)((_intc.Iprc & 0xFF00) | data); return;
            case 0x05FFFF8A: _intc.Iprd = (ushort)((_intc.Iprd & 0xFF) | (data << 8)); return;
            case 0x05FFFF8B: _intc.Iprd = (ushort)((_intc.Iprd & 0xFF00) | data); return;
            case 0x05FFFF8C: _intc.Ipre = (ushort)((_intc.Ipre & 0xFF) | (data << 8)); return;
            case 0x05FFFF8D: _intc.Ipre = (ushort)((_intc.Ipre & 0xFF00) | data); return;
            case 0x05FFFF8E: _intc.Icr = (ushort)((_intc.Icr & 0xFF) | (data << 8)); return;
            case 0x05FFFF8F: _intc.Icr = (ushort)((_intc.Icr & 0xFF00) | data); return;
        }
        Debug.Fail("Unknown SH-1 interrupt byte register.");
    }

    internal ushort ReadIntcWord(uint address)
    {
        switch (address)
        {
            case 0x05FFFF80:
            case 0x05FFFF82: return 0;
            case 0x05FFFF84: return _intc.Ipra;
            case 0x05FFFF86: return _intc.Iprb;
            case 0x05FFFF88: return _intc.Iprc;
            case 0x05FFFF8A: return _intc.Iprd;
            case 0x05FFFF8C: return _intc.Ipre;
            case 0x05FFFF8E: return _intc.Icr;
        }
        Debug.Fail("Unknown SH-1 interrupt word register.");
        return 0;
    }

    internal void WriteIntcWord(uint address, ushort data)
    {
        switch (address)
        {
            case 0x05FFFF80:
            case 0x05FFFF82: return;
            case 0x05FFFF84: _intc.Ipra = data; return;
            case 0x05FFFF86: _intc.Iprb = data; return;
            case 0x05FFFF88: _intc.Iprc = data; return;
            case 0x05FFFF8A: _intc.Iprd = data; return;
            case 0x05FFFF8C: _intc.Ipre = data; return;
            case 0x05FFFF8E: _intc.Icr = data; return;
        }
        Debug.Fail("Unknown SH-1 interrupt word register.");
    }

    internal uint ReadIntcLong(uint address)
    {
        switch (address)
        {
            case 0x05FFFF80:
            case 0x05FFFF82: return 0;
            case 0x05FFFF84: return ((uint)_intc.Ipra << 16) | _intc.Iprb;
            case 0x05FFFF86: return ((uint)_intc.Iprb << 16) | _intc.Iprc;
            case 0x05FFFF88: return ((uint)_intc.Iprc << 16) | _intc.Iprd;
            case 0x05FFFF8A: return ((uint)_intc.Iprd << 16) | _intc.Ipre;
            case 0x05FFFF8C: return ((uint)_intc.Ipre << 16) | _intc.Icr;
            case 0x05FFFF8E: return ((uint)_intc.Icr << 16);
        }
        Debug.Fail("Unknown SH-1 interrupt long register.");
        return 0;
    }

    internal void WriteIntcLong(uint address, uint data)
    {
        switch (address)
        {
            case 0x05FFFF80:
            case 0x05FFFF82: return;
            case 0x05FFFF84:
                _intc.Ipra = (ushort)(data >> 16);
                _intc.Iprb = (ushort)(data & 0xFFFF);
                return;
            case 0x05FFFF86:
                _intc.Iprb = (ushort)(data >> 16);
                _intc.Iprc = (ushort)(data & 0xFFFF);
                return;
            case 0x05FFFF88:
                _intc.Iprc = (ushort)(data >> 16);
                _intc.Iprd = (ushort)(data & 0xFFFF);
                return;
            case 0x05FFFF8A:
                _intc.Iprd = (ushort)(data >> 16);
                _intc.Ipre = (ushort)(data & 0xFFFF);
                return;
            case 0x05FFFF8C:
                _intc.Ipre = (ushort)(data >> 16);
                _intc.Icr = (ushort)(data & 0xFFFF);
                return;
            case 0x05FFFF8E:
                _intc.Icr = (ushort)(data >> 16);
                return;
        }
        Debug.Fail("Unknown SH-1 interrupt long register.");
    }

    private static ushort UpdateTcntFraction(ref TimerChannel timer, int cycles, int division)
    {
        int remainder = 0;
        timer.TcntFraction = unchecked((byte)(timer.TcntFraction + cycles % division));
        if (timer.TcntFraction >= division)
        {
            remainder = 1;
            timer.TcntFraction -= (byte)division;
        }
        return unchecked((ushort)(timer.Tcnt + cycles / division + remainder));
    }

    private static ushort UpdateTcntFast(ref TimerChannel timer, int cycles)
    {
        switch (timer.Tcr & 7)
        {
            case 0: return unchecked((ushort)(timer.Tcnt + cycles));
            case 1: return UpdateTcntFraction(ref timer, cycles, 2);
            case 2: return UpdateTcntFraction(ref timer, cycles, 4);
            case 3: return UpdateTcntFraction(ref timer, cycles, 8);
        }
        Debug.Fail("Unsupported SH-1 timer clock source.");
        return 0;
    }

    private static bool CheckGrRange(ushort gr, ushort oldTcnt, ushort newTcnt)
    {
        if (newTcnt < oldTcnt)
            return gr >= oldTcnt || gr <= newTcnt;
        return gr >= oldTcnt && gr <= newTcnt;
    }

    internal void TickTimerFast(int channel, int cycles)
    {
        ref TimerChannel timer = ref _itu.Channels[channel];
        ushort oldTcnt = timer.Tcnt;
        ushort newTcnt = 0;
        bool counting = (_itu.Tstr & (1 << channel)) != 0;
        if (counting)
            newTcnt = UpdateTcntFast(ref timer, cycles);

        bool graMatch = CheckGrRange(timer.Gra, oldTcnt, newTcnt);
        bool grbMatch = CheckGrRange(timer.Grb, oldTcnt, newTcnt);
        if (counting && (timer.Tier & 4) != 0 && newTcnt < oldTcnt && channel == 4)
            sendInterrupt(98, (Iprd >> 4) & 0xF);

        if (graMatch)
        {
            switch (timer.Tior & 7)
            {
                case 0:
                    timer.Tsr |= 1;
                    if (((timer.Tcr >> 5) & 3) == 1)
                        newTcnt = 0;
                    if ((timer.Tier & 1) != 0)
                        sendInterrupt(96, (Iprd >> 4) & 0xF);
                    break;
                case 1: timer.Tsr &= 0xFD; break;
                case 2: timer.Tsr |= 2; break;
            }
        }
        if (grbMatch)
        {
            switch ((timer.Tior >> 4) & 7)
            {
                case 0:
                    timer.Tsr |= 2;
                    if (((timer.Tcr >> 5) & 3) == 2)
                        newTcnt = 0;
                    if ((timer.Tier & 2) != 0)
                        sendInterrupt(97, (Iprd >> 4) & 0xF);
                    break;
                case 1: timer.Tsr &= 0xFD; break;
                case 2: timer.Tsr |= 2; break;
            }
        }
        timer.Tcnt = newTcnt;
    }

    internal void SetOutputEnableFallingEdge()
    {
        ref TimerChannel timer = ref _itu.Channels[3];
        if ((timer.Tsr & 2) != 0)
            return;
        if (((timer.Tior >> 4) & 7) != 5)
            return;
        if ((timer.Tier & 2) == 0)
            return;

        timer.Brb = timer.Grb;
        timer.Grb = timer.Tcnt;
        if (((timer.Tcr >> 5) & 3) == 2)
            timer.Tcnt = 0;
        timer.Tsr |= 2;
        sendInterrupt(93, (Iprd >> 8) & 0xF);
    }

    internal void SetStart(bool state)
    {
        if (state)
            _pbdr &= 0xFFFB;
        else
            _pbdr |= 0x0004;
    }

    private void TickDma(int channelIndex)
    {
        ref DmaChannel channel = ref _dmac[channelIndex];
        int mode = (channel.Chcr >> 8) & 0xF;
        if ((_dmaor & 7) != 1 || (channel.Chcr & 3) != 1)
            return;
        if (mode is not (2 or 3))
            return;
        if (!ygr.DreqAsserted())
            return;

        int destinationMode = channel.Chcr >> 14;
        int sourceMode = (channel.Chcr >> 12) & 3;
        bool wordSize = (channel.Chcr & 8) != 0;
        int sourceIncrement = 0;
        int destinationIncrement = 0;

        if (sourceMode == 1)
            sourceIncrement = 2;
        else if (sourceMode == 2)
            sourceIncrement = -2;
        if (destinationMode == 1)
            destinationIncrement = 2;
        else if (destinationMode == 2)
            destinationIncrement = -2;

        if (wordSize)
        {
            ushort value = Read16(channel.Sar);
            Write16(channel.Dar, value);
        }
        else
        {
            byte value = Read8(channel.Sar);
            Write8(channel.Dar, value);
        }

        unchecked
        {
            channel.Sar += (uint)sourceIncrement;
            channel.Dar += (uint)destinationIncrement;
            channel.Tcr--;
        }
        if (channel.Tcr == 0)
        {
            channel.IsActive = false;
            channel.Chcr |= 2;
            if ((channel.Chcr & 4) != 0)
                sendInterrupt(74, (Iprc >> 12) & 0xF);
        }
    }

    internal void ExecuteDma(int cycles)
    {
        for (int i = 0; i < cycles; i++)
            TickDma(1);
    }

    private void InitializeDma(int channelIndex)
    {
        ref DmaChannel channel = ref _dmac[channelIndex];
        if ((_dmaor & 1) != 0 && (channel.Chcr & 1) != 0)
        {
            channel.IsActive = true;
            channel.Chcr &= 0xFFFD;
        }
    }

    internal void AssertDreq(int channelIndex)
    {
        if (!_dmac[channelIndex].IsActive)
            InitializeDma(channelIndex);
        TickDma(channelIndex);
    }

    internal void AssertTioca(int channel)
    {
        ref TimerChannel timer = ref _itu.Channels[channel];
        if ((timer.Tior & 7) == 5)
        {
            timer.Gra = timer.Tcnt;
            timer.Tsr |= 1;
            if ((timer.Tier & 1) != 0)
            {
                switch (channel)
                {
                    case 0: sendInterrupt(80, (Iprc >> 4) & 0xF); break;
                    case 1: sendInterrupt(84, Iprc & 0xF); break;
                    case 2: sendInterrupt(88, (Iprd >> 12) & 0xF); break;
                    case 3: sendInterrupt(92, (Iprd >> 8) & 0xF); break;
                    case 4: sendInterrupt(96, (Iprd >> 4) & 0xF); break;
                }
            }
        }
    }

    internal void AssertTiocb(int channel)
    {
        ref TimerChannel timer = ref _itu.Channels[channel];
        if (((timer.Tior >> 4) & 7) == 5)
        {
            timer.Grb = timer.Tcnt;
            timer.Tsr |= 2;
            if ((timer.Tier & 2) != 0)
            {
                switch (channel)
                {
                    case 0: sendInterrupt(81, (Iprc >> 4) & 0xF); break;
                    case 1: sendInterrupt(85, Iprc & 0xF); break;
                    case 2: sendInterrupt(89, (Iprd >> 12) & 0xF); break;
                    case 3: sendInterrupt(93, (Iprd >> 8) & 0xF); break;
                    case 4: sendInterrupt(97, (Iprd >> 4) & 0xF); break;
                }
            }
        }
    }
}

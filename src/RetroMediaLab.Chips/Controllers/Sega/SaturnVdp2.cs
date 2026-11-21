// Copyright 2003-2005 Guillaume Duhamel
// Copyright 2004-2007 Theo Berkau
// Translated from Yabause vdp2.c.
// SPDX-License-Identifier: GPL-2.0-or-later

using static RetroMediaLab.Chips.Controllers.Sega.SaturnMemoryAccess;

namespace RetroMediaLab.Chips.Controllers.Sega;

public sealed class SaturnVdp2
{
    public sealed record Connections(
        Func<int> LineCount,
        SaturnVideoSoftware Video,
        SaturnVdp1 Vdp1,
        Action SendVBlankIn,
        Action SendHBlankIn,
        Action SendVBlankOut,
        Func<bool> SlaveRunning,
        Action<int, int> SendSlaveInterrupt);

    private readonly Connections _connections;
    private readonly byte[] _ram = new byte[0x80000];
    private readonly byte[] _colorRam = new byte[0x1000];
    private SaturnVdp2Registers _registers;
    private readonly SaturnVdp2Registers[] _lines = new SaturnVdp2Registers[270];
    private readonly SaturnVdp2CellScrollData[] _cellScrollData = new SaturnVdp2CellScrollData[270];
    private bool _oddFrame;

    public SaturnVdp2(Connections connections)
    {
        _connections = connections;
        Reset();
    }

    public ref SaturnVdp2Registers Registers => ref _registers;
    internal ReadOnlySpan<byte> Ram => _ram;
    internal ReadOnlySpan<byte> ColorRam => _colorRam;
    internal Span<byte> DmaRam => _ram;
    internal Span<byte> DmaColorRam => _colorRam;
    internal bool OddFrame => _oddFrame;
    public ReadOnlySpan<SaturnVdp2Registers> Lines => _lines;
    public ReadOnlySpan<SaturnVdp2CellScrollData> CellScrollData => _cellScrollData;
    public int ColorMode { get; private set; }
    public int VBlankLineCount { get; private set; }
    public int DisplayToggle { get; private set; }

    internal static ReadOnlySpan<SaturnVdp2Registers> RestoreRegisters(int line, ReadOnlySpan<SaturnVdp2Registers> lines)
    {
        return (uint)line < (uint)lines.Length ? lines.Slice(line, 1) : default;
    }

    public byte RamReadByte(uint address) => T1ReadByte(_ram, address & 0x7FFFF);
    public ushort RamReadWord(uint address) => T1ReadWord(_ram, address & 0x7FFFF);
    public uint RamReadLong(uint address) => T1ReadLong(_ram, address & 0x7FFFF);

    public void RamWriteByte(uint address, byte value) => T1WriteByte(_ram, address & 0x7FFFF, value);
    public void RamWriteWord(uint address, ushort value) => T1WriteWord(_ram, address & 0x7FFFF, value);
    public void RamWriteLong(uint address, uint value) => T1WriteLong(_ram, address & 0x7FFFF, value);

    public byte ColorRamReadByte(uint address) => T2ReadByte(_colorRam, address & 0xFFF);
    public ushort ColorRamReadWord(uint address) => T2ReadWord(_colorRam, address & 0xFFF);
    public uint ColorRamReadLong(uint address) => T2ReadLong(_colorRam, address & 0xFFF);

    public void ColorRamWriteByte(uint address, byte value) => T2WriteByte(_colorRam, address & 0xFFF, value);
    public void ColorRamWriteWord(uint address, ushort value) => T2WriteWord(_colorRam, address & 0xFFF, value);
    public void ColorRamWriteLong(uint address, uint value) => T2WriteLong(_colorRam, address & 0xFFF, value);

    public void Reset()
    {
        _registers.TVMD = 0;
        _registers.EXTEN = 0;
        _registers.TVSTAT &= 1;
        _registers.VRSIZE = 0;
        _registers.RAMCTL = 0;
        _registers.BGON = 0;
        _registers.CHCTLA = 0;
        _registers.CHCTLB = 0;
        _registers.BMPNA = 0;
        _registers.MPOFN = 0;
        _registers.MPABN2 = 0;
        _registers.MPCDN2 = 0;
        _registers.SCXIN0 = 0;
        _registers.SCXDN0 = 0;
        _registers.SCYIN0 = 0;
        _registers.SCYDN0 = 0;
        _registers.ZMXN0.All = 0;
        _registers.ZMYN0.All = 0;
        _registers.SCXIN1 = 0;
        _registers.SCXDN1 = 0;
        _registers.SCYIN1 = 0;
        _registers.SCYDN1 = 0;
        _registers.ZMXN1.All = 0;
        _registers.ZMYN1.All = 0;
        _registers.SCXN2 = 0;
        _registers.SCYN2 = 0;
        _registers.SCXN3 = 0;
        _registers.SCYN3 = 0;
        _registers.ZMCTL = 0;
        _registers.SCRCTL = 0;
        _registers.VCSTA.All = 0;
        _registers.BKTAU = 0;
        _registers.BKTAL = 0;
        _registers.RPMD = 0;
        _registers.RPRCTL = 0;
        _registers.KTCTL = 0;
        _registers.KTAOF = 0;
        _registers.OVPNRA = 0;
        _registers.OVPNRB = 0;
        _registers.WPSX0 = 0;
        _registers.WPSY0 = 0;
        _registers.WPEX0 = 0;
        _registers.WPEY0 = 0;
        _registers.WPSX1 = 0;
        _registers.WPSY1 = 0;
        _registers.WPEX1 = 0;
        _registers.WPEY1 = 0;
        _registers.WCTLA = 0;
        _registers.WCTLB = 0;
        _registers.WCTLC = 0;
        _registers.WCTLD = 0;
        _registers.SPCTL = 0;
        _registers.SDCTL = 0;
        _registers.CRAOFA = 0;
        _registers.CRAOFB = 0;
        _registers.LNCLEN = 0;
        _registers.SFPRMD = 0;
        _registers.CCCTL = 0;
        _registers.SFCCMD = 0;
        _registers.PRISA = 0;
        _registers.PRISB = 0;
        _registers.PRISC = 0;
        _registers.PRISD = 0;
        _registers.PRINA = 0;
        _registers.PRINB = 0;
        _registers.PRIR = 0;
        _registers.CCRNA = 0;
        _registers.CCRNB = 0;
        _registers.CLOFEN = 0;
        _registers.CLOFSL = 0;
        _registers.COAR = 0;
        _registers.COAG = 0;
        _registers.COAB = 0;
        _registers.COBR = 0;
        _registers.COBG = 0;
        _registers.COBB = 0;

        VBlankLineCount = 225;
        ColorMode = 0;
        DisplayToggle = 0xFF;

    }

    public void VBlankIn()
    {
        _connections.Video.EndVdp2Draw(this, _connections.Vdp1);
        _connections.Vdp1.Registers.COPR = 0;
        if (_connections.Vdp1.ManualFrameBufferChange)
            _connections.Vdp1.Registers.EDSR >>= 1;

        _registers.TVSTAT |= 0x0008;
        _connections.SendVBlankIn();

        if (_connections.SlaveRunning())
            _connections.SendSlaveInterrupt(0x43, 6);
    }

    public void HBlankIn()
    {
        _registers.TVSTAT |= 0x0004;
        _connections.SendHBlankIn();

        if (_connections.SlaveRunning())
            _connections.SendSlaveInterrupt(0x41, 2);
    }

    public void HBlankOut()
    {
        _registers.TVSTAT &= 0xFFFB;
        int line = _connections.LineCount();
        if (line < 270)
        {
            uint start = (_registers.VCSTA.All & 0x7FFFE) << 1;
            _lines[line] = _registers;
            Span<uint> cells = _cellScrollData[line];
            for (int index = 0; index < cells.Length; index++)
                cells[index] = RamReadLong(start + (uint)index * 4);
        }
    }

    public void VBlankOut(SaturnSmpc smpc)
    {
        _oddFrame = !_oddFrame;
        bool displayOn = (_registers.TVMD & 0x8000) != 0;
        _registers.TVSTAT = (ushort)((_registers.TVSTAT & ~0x000A) | (_oddFrame ? 2 : 0));

        _connections.Video.StartVdp2Draw(this);
        if (displayOn)
            _connections.Video.DrawVdp2Screens(this, _connections.Vdp1);
        else
            _connections.Video.DisplayVdp2Off();

        if (_connections.Vdp1.Registers.PTMR == 2)
            _connections.Vdp1.Draw();

        if ((_connections.Vdp1.Registers.FBCR & 2) != 0 && (_connections.Vdp1.Registers.TVMR & 8) != 0)
            _connections.Vdp1.ManualFrameBufferErase = true;

        _connections.SendVBlankOut();

        if ((_registers.EXTEN & 0x200) != 0 && (smpc.ExternalLatchEnable & 1) != 0)
        {
            ReadOnlySpan<byte> port = smpc.Port1Data;
            SendExternalLatch((port[3] << 8) | port[4], (port[5] << 8) | port[6]);
        }
    }

    private void SendExternalLatch(int horizontal, int vertical)
    {
        _registers.HCNT = unchecked((ushort)(horizontal << 1));
        _registers.VCNT = unchecked((ushort)vertical);
        _registers.TVSTAT |= 0x200;
    }

    public byte ReadByte(uint address) => 0;

    public ushort ReadWord(uint address)
    {
        address &= 0x1FF;
        switch (address)
        {
            case 0x000:
                return _registers.TVMD;
            case 0x002:
                if ((_registers.EXTEN & 0x200) == 0)
                {
                    _registers.VCNT = unchecked((ushort)_connections.LineCount());
                    _registers.TVSTAT |= 0x200;
                }
                return _registers.EXTEN;
            case 0x004:
            {
                ushort value = _registers.TVSTAT;
                _registers.TVSTAT &= 0xFCFF;
                return (_registers.TVMD & 0x8000) != 0 ? value : (ushort)(value | 8);
            }
            case 0x006:
                return _registers.VRSIZE;
            case 0x008:
                return _registers.HCNT;
            case 0x00A:
                return _registers.VCNT;
            default:
                return 0;
        }
    }

    public uint ReadLong(uint address) => 0;

    public void WriteByte(uint address, byte value) { }

    public void WriteWord(uint address, ushort value)
    {
        address &= 0x1FF;
        switch (address)
        {
            case 0x000:
                _registers.TVMD = value;
                VBlankLineCount = 225 + (value & 0x30);
                return;
            case 0x002:
                _registers.EXTEN = value;
                return;
            case 0x004:
                return;
            case 0x006:
                _registers.VRSIZE = value;
                return;
            case 0x008:
                return;
            case 0x00A:
                return;
            case 0x00C:
                return;
            case 0x00E:
                _registers.RAMCTL = value;
                ColorMode = (value >> 12) & 3;
                return;
            case 0x010:
                _registers.CYCA0L = value;
                return;
            case 0x012:
                _registers.CYCA0U = value;
                return;
            case 0x014:
                _registers.CYCA1L = value;
                return;
            case 0x016:
                _registers.CYCA1U = value;
                return;
            case 0x018:
                _registers.CYCB0L = value;
                return;
            case 0x01A:
                _registers.CYCB0U = value;
                return;
            case 0x01C:
                _registers.CYCB1L = value;
                return;
            case 0x01E:
                _registers.CYCB1U = value;
                return;
            case 0x020:
                _registers.BGON = value;
                return;
            case 0x022:
                _registers.MZCTL = value;
                return;
            case 0x024:
                _registers.SFSEL = value;
                return;
            case 0x026:
                _registers.SFCODE = value;
                return;
            case 0x028:
                _registers.CHCTLA = value;
                return;
            case 0x02A:
                _registers.CHCTLB = value;
                return;
            case 0x02C:
                _registers.BMPNA = value;
                return;
            case 0x02E:
                _registers.BMPNB = value;
                return;
            case 0x030:
                _registers.PNCN0 = value;
                return;
            case 0x032:
                _registers.PNCN1 = value;
                return;
            case 0x034:
                _registers.PNCN2 = value;
                return;
            case 0x036:
                _registers.PNCN3 = value;
                return;
            case 0x038:
                _registers.PNCR = value;
                return;
            case 0x03A:
                _registers.PLSZ = value;
                return;
            case 0x03C:
                _registers.MPOFN = value;
                return;
            case 0x03E:
                _registers.MPOFR = value;
                return;
            case 0x040:
                _registers.MPABN0 = value;
                return;
            case 0x042:
                _registers.MPCDN0 = value;
                return;
            case 0x044:
                _registers.MPABN1 = value;
                return;
            case 0x046:
                _registers.MPCDN1 = value;
                return;
            case 0x048:
                _registers.MPABN2 = value;
                return;
            case 0x04A:
                _registers.MPCDN2 = value;
                return;
            case 0x04C:
                _registers.MPABN3 = value;
                return;
            case 0x04E:
                _registers.MPCDN3 = value;
                return;
            case 0x050:
                _registers.MPABRA = value;
                return;
            case 0x052:
                _registers.MPCDRA = value;
                return;
            case 0x054:
                _registers.MPEFRA = value;
                return;
            case 0x056:
                _registers.MPGHRA = value;
                return;
            case 0x058:
                _registers.MPIJRA = value;
                return;
            case 0x05A:
                _registers.MPKLRA = value;
                return;
            case 0x05C:
                _registers.MPMNRA = value;
                return;
            case 0x05E:
                _registers.MPOPRA = value;
                return;
            case 0x060:
                _registers.MPABRB = value;
                return;
            case 0x062:
                _registers.MPCDRB = value;
                return;
            case 0x064:
                _registers.MPEFRB = value;
                return;
            case 0x066:
                _registers.MPGHRB = value;
                return;
            case 0x068:
                _registers.MPIJRB = value;
                return;
            case 0x06A:
                _registers.MPKLRB = value;
                return;
            case 0x06C:
                _registers.MPMNRB = value;
                return;
            case 0x06E:
                _registers.MPOPRB = value;
                return;
            case 0x070:
                _registers.SCXIN0 = value;
                return;
            case 0x072:
                _registers.SCXDN0 = value;
                return;
            case 0x074:
                _registers.SCYIN0 = value;
                return;
            case 0x076:
                _registers.SCYDN0 = value;
                return;
            case 0x078:
                _registers.ZMXN0.High = value;
                return;
            case 0x07A:
                _registers.ZMXN0.Low = value;
                return;
            case 0x07C:
                _registers.ZMYN0.High = value;
                return;
            case 0x07E:
                _registers.ZMYN0.Low = value;
                return;
            case 0x080:
                _registers.SCXIN1 = value;
                return;
            case 0x082:
                _registers.SCXDN1 = value;
                return;
            case 0x084:
                _registers.SCYIN1 = value;
                return;
            case 0x086:
                _registers.SCYDN1 = value;
                return;
            case 0x088:
                _registers.ZMXN1.High = value;
                return;
            case 0x08A:
                _registers.ZMXN1.Low = value;
                return;
            case 0x08C:
                _registers.ZMYN1.High = value;
                return;
            case 0x08E:
                _registers.ZMYN1.Low = value;
                return;
            case 0x090:
                _registers.SCXN2 = value;
                return;
            case 0x092:
                _registers.SCYN2 = value;
                return;
            case 0x094:
                _registers.SCXN3 = value;
                return;
            case 0x096:
                _registers.SCYN3 = value;
                return;
            case 0x098:
                _registers.ZMCTL = value;
                return;
            case 0x09A:
                _registers.SCRCTL = value;
                return;
            case 0x09C:
                _registers.VCSTA.High = value;
                return;
            case 0x09E:
                _registers.VCSTA.Low = value;
                return;
            case 0x0A0:
                _registers.LSTA0.High = value;
                return;
            case 0x0A2:
                _registers.LSTA0.Low = value;
                return;
            case 0x0A4:
                _registers.LSTA1.High = value;
                return;
            case 0x0A6:
                _registers.LSTA1.Low = value;
                return;
            case 0x0A8:
                _registers.LCTA.High = value;
                return;
            case 0x0AA:
                _registers.LCTA.Low = value;
                return;
            case 0x0AC:
                _registers.BKTAU = value;
                return;
            case 0x0AE:
                _registers.BKTAL = value;
                return;
            case 0x0B0:
                _registers.RPMD = value;
                return;
            case 0x0B2:
                _registers.RPRCTL = value;
                return;
            case 0x0B4:
                _registers.KTCTL = value;
                return;
            case 0x0B6:
                _registers.KTAOF = value;
                return;
            case 0x0B8:
                _registers.OVPNRA = value;
                return;
            case 0x0BA:
                _registers.OVPNRB = value;
                return;
            case 0x0BC:
                _registers.RPTA.High = value;
                return;
            case 0x0BE:
                _registers.RPTA.Low = value;
                return;
            case 0x0C0:
                _registers.WPSX0 = value;
                return;
            case 0x0C2:
                _registers.WPSY0 = value;
                return;
            case 0x0C4:
                _registers.WPEX0 = value;
                return;
            case 0x0C6:
                _registers.WPEY0 = value;
                return;
            case 0x0C8:
                _registers.WPSX1 = value;
                return;
            case 0x0CA:
                _registers.WPSY1 = value;
                return;
            case 0x0CC:
                _registers.WPEX1 = value;
                return;
            case 0x0CE:
                _registers.WPEY1 = value;
                return;
            case 0x0D0:
                _registers.WCTLA = value;
                return;
            case 0x0D2:
                _registers.WCTLB = value;
                return;
            case 0x0D4:
                _registers.WCTLC = value;
                return;
            case 0x0D6:
                _registers.WCTLD = value;
                return;
            case 0x0D8:
                _registers.LWTA0.High = value;
                return;
            case 0x0DA:
                _registers.LWTA0.Low = value;
                return;
            case 0x0DC:
                _registers.LWTA1.High = value;
                return;
            case 0x0DE:
                _registers.LWTA1.Low = value;
                return;
            case 0x0E0:
                _registers.SPCTL = value;
                return;
            case 0x0E2:
                _registers.SDCTL = value;
                return;
            case 0x0E4:
                _registers.CRAOFA = value;
                return;
            case 0x0E6:
                _registers.CRAOFB = value;
                return;
            case 0x0E8:
                _registers.LNCLEN = value;
                return;
            case 0x0EA:
                _registers.SFPRMD = value;
                return;
            case 0x0EC:
                _registers.CCCTL = value;
                return;
            case 0x0EE:
                _registers.SFCCMD = value;
                return;
            case 0x0F0:
                _registers.PRISA = value;
                return;
            case 0x0F2:
                _registers.PRISB = value;
                return;
            case 0x0F4:
                _registers.PRISC = value;
                return;
            case 0x0F6:
                _registers.PRISD = value;
                return;
            case 0x0F8:
                _registers.PRINA = value;
                return;
            case 0x0FA:
                _registers.PRINB = value;
                return;
            case 0x0FC:
                _registers.PRIR = value;
                return;
            case 0x0FE:
                return;
            case 0x100:
                _registers.CCRSA = value;
                return;
            case 0x102:
                _registers.CCRSB = value;
                return;
            case 0x104:
                _registers.CCRSC = value;
                return;
            case 0x106:
                _registers.CCRSD = value;
                return;
            case 0x108:
                _registers.CCRNA = value;
                return;
            case 0x10A:
                _registers.CCRNB = value;
                return;
            case 0x10C:
                _registers.CCRR = value;
                return;
            case 0x10E:
                _registers.CCRLB = value;
                return;
            case 0x110:
                _registers.CLOFEN = value;
                return;
            case 0x112:
                _registers.CLOFSL = value;
                return;
            case 0x114:
                _registers.COAR = value;
                return;
            case 0x116:
                _registers.COAG = value;
                return;
            case 0x118:
                _registers.COAB = value;
                return;
            case 0x11A:
                _registers.COBR = value;
                return;
            case 0x11C:
                _registers.COBG = value;
                return;
            case 0x11E:
                _registers.COBB = value;
                return;
        }
    }

    public void WriteLong(uint address, uint value)
    {
        WriteWord(address, (ushort)(value >> 16));
        WriteWord(address + 2, unchecked((ushort)value));
    }
}

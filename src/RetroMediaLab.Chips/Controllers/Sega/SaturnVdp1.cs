// Copyright 2003-2005 Guillaume Duhamel
// Copyright 2004 Lawrence Sebald
// Copyright 2004-2006 Theo Berkau
// Translated from Yabause vdp1.c.
// SPDX-License-Identifier: GPL-2.0-or-later

using static RetroMediaLab.Chips.Controllers.Sega.SaturnMemoryAccess;

namespace RetroMediaLab.Chips.Controllers.Sega;

public sealed class SaturnVdp1(ISaturnVdp1Video video, Action sendDrawEnd)
{
    private readonly byte[] _ram = new byte[0x80000];
    private readonly byte[] _frameBuffer = new byte[0x80000];
    // Mednafen vdp1.cpp masks the low bits of CMDLINK for ASSIGN and CALL.
    private const uint CommandLinkMask = 0xFFFC;
    private SaturnVdp1Registers _registers;
    private bool _displayToggle = true;

    public ref SaturnVdp1Registers Registers => ref _registers;
    public byte[] Ram => _ram;
    public bool DisplayEnabled => _displayToggle;
    internal bool ManualFrameBufferChange { get; set; }
    internal bool ManualFrameBufferErase { get; set; }

    public byte RamReadByte(uint address) => T1ReadByte(_ram, address & 0x7FFFF);
    public ushort RamReadWord(uint address) => T1ReadWord(_ram, address & 0x7FFFF);
    public uint RamReadLong(uint address) => T1ReadLong(_ram, address & 0x7FFFF);

    public void RamWriteByte(uint address, byte value) => T1WriteByte(_ram, address & 0x7FFFF, value);
    public void RamWriteWord(uint address, ushort value) => T1WriteWord(_ram, address & 0x7FFFF, value);
    public void RamWriteLong(uint address, uint value) => T1WriteLong(_ram, address & 0x7FFFF, value);

    public byte FrameBufferReadByte(uint address)
    {
        address &= 0x3FFFF;
        if (video.ReadVdp1FrameBuffer is { } read)
            return unchecked((byte)read(0, address));

        return T1ReadByte(_frameBuffer, address);
    }

    public ushort FrameBufferReadWord(uint address)
    {
        address &= 0x3FFFF;
        if (video.ReadVdp1FrameBuffer is { } read)
            return unchecked((ushort)read(1, address));

        return T1ReadWord(_frameBuffer, address);
    }

    public uint FrameBufferReadLong(uint address)
    {
        address &= 0x3FFFF;
        if (video.ReadVdp1FrameBuffer is { } read)
            return read(2, address);

        return T1ReadLong(_frameBuffer, address);
    }

    public void FrameBufferWriteByte(uint address, byte value)
    {
        address &= 0x3FFFF;
        if (video.WriteVdp1FrameBuffer is { } write)
        {
            write(0, address, value);
            return;
        }

        T1WriteByte(_frameBuffer, address, value);
    }

    public void FrameBufferWriteWord(uint address, ushort value)
    {
        address &= 0x3FFFF;
        if (video.WriteVdp1FrameBuffer is { } write)
        {
            write(1, address, value);
            return;
        }

        T1WriteWord(_frameBuffer, address, value);
    }

    public void FrameBufferWriteLong(uint address, uint value)
    {
        address &= 0x3FFFF;
        if (video.WriteVdp1FrameBuffer is { } write)
        {
            write(2, address, value);
            return;
        }

        T1WriteLong(_frameBuffer, address, value);
    }

    public void Reset()
    {
        _registers.PTMR = 0;
        _registers.MODR = 0x1000;
        video.ResetVdp1(ref _registers);
    }

    public byte ReadByte(uint address) => 0;

    public ushort ReadWord(uint address)
    {
        address &= 0xFF;
        switch (address)
        {
            case 0x10:
                return _registers.EDSR;
            case 0x12:
                return _registers.LOPR;
            case 0x14:
                return _registers.COPR;
            case 0x16:
                return (ushort)(0x1000 | ((_registers.PTMR & 2) << 7) |
                    ((_registers.FBCR & 0x1E) << 3) | (_registers.TVMR & 0xF));
            default:
                return 0;
        }
    }

    public uint ReadLong(uint address) => 0;

    public void WriteByte(uint address, byte value) { }

    public void WriteWord(uint address, ushort value)
    {
        address &= 0xFF;
        switch (address)
        {
            case 0x00:
                _registers.TVMR = value;
                break;
            case 0x02:
                _registers.FBCR = value;
                if ((_registers.FBCR & 3) == 3)
                    ManualFrameBufferChange = true;
                else if ((_registers.FBCR & 3) == 2)
                    ManualFrameBufferErase = true;
                break;
            case 0x04:
                _registers.COPR = 0;
                _registers.PTMR = value;
                if (value == 1)
                    Draw();
                break;
            case 0x06:
                _registers.EWDR = value;
                break;
            case 0x08:
                _registers.EWLR = value;
                break;
            case 0x0A:
                _registers.EWRR = value;
                break;
            case 0x0C:
                _registers.ENDR = value;
                break;
        }
    }

    public void WriteLong(uint address, uint value) { }

    public void DrawCommands(byte[] ram, ref SaturnVdp1Registers registers, byte[] backFrameBuffer)
    {
        ushort command = T1ReadWord(ram, registers.Address);
        uint commandCounter = 0;
        uint returnAddress = uint.MaxValue;
        while ((command & 0x8000) == 0 && commandCounter < 2000)
        {
            if ((command & 0x4000) == 0)
            {
                switch (command & 0x000F)
                {
                    case 0:
                        video.NormalSpriteDraw(ram, ref registers, backFrameBuffer);
                        break;
                    case 1:
                        video.ScaledSpriteDraw(ram, ref registers, backFrameBuffer);
                        break;
                    case 2:
                    case 3:
                        video.DistortedSpriteDraw(ram, ref registers, backFrameBuffer);
                        break;
                    case 4:
                        video.PolygonDraw(ram, ref registers, backFrameBuffer);
                        break;
                    case 5:
                    case 7:
                        video.PolylineDraw(ram, ref registers, backFrameBuffer);
                        break;
                    case 6:
                        video.LineDraw(ram, ref registers, backFrameBuffer);
                        break;
                    case 8:
                    case 11:
                        video.UserClipping(ram, ref registers);
                        break;
                    case 9:
                        video.SystemClipping(ram, ref registers);
                        break;
                    case 10:
                        video.LocalCoordinate(ram, ref registers);
                        break;
                    default:
                        registers.EDSR |= 2;
                        video.EndVdp1Draw();
                        registers.LOPR = registers.COPR = (ushort)(registers.Address >> 3);
                        return;
                }
            }

            switch ((command & 0x3000) >> 12)
            {
                case 0:
                    registers.Address = (registers.Address + 0x20) & 0x7FFFF;
                    break;
                case 1:
                    registers.Address = (T1ReadWord(ram, registers.Address + 2) & CommandLinkMask) << 3;
                    break;
                case 2:
                    if (returnAddress == uint.MaxValue)
                        returnAddress = (registers.Address + 0x20) & 0x7FFFF;

                    registers.Address = (T1ReadWord(ram, registers.Address + 2) & CommandLinkMask) << 3;
                    break;
                case 3:
                    if (returnAddress != uint.MaxValue)
                    {
                        registers.Address = returnAddress;
                        returnAddress = uint.MaxValue;
                    }
                    else
                        registers.Address = (registers.Address + 0x20) & 0x7FFFF;
                    break;
            }

            command = T1ReadWord(ram, registers.Address);
            commandCounter++;
        }
    }

    public void FakeDrawCommands(byte[] ram, ref SaturnVdp1Registers registers)
    {
        ushort command = T1ReadWord(ram, registers.Address);
        uint commandCounter = 0;
        uint returnAddress = uint.MaxValue;
        while ((command & 0x8000) == 0 && commandCounter < 2000)
        {
            if ((command & 0x4000) == 0)
            {
                switch (command & 0x000F)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                        break;
                    case 8:
                    case 11:
                        video.UserClipping(ram, ref registers);
                        break;
                    case 9:
                        video.SystemClipping(ram, ref registers);
                        break;
                    case 10:
                        video.LocalCoordinate(ram, ref registers);
                        break;
                    default:
                        registers.EDSR |= 2;
                        video.EndVdp1Draw();
                        registers.LOPR = registers.COPR = (ushort)(registers.Address >> 3);
                        return;
                }
            }

            switch ((command & 0x3000) >> 12)
            {
                case 0:
                    registers.Address = (registers.Address + 0x20) & 0x7FFFF;
                    break;
                case 1:
                    registers.Address = (T1ReadWord(ram, registers.Address + 2) & CommandLinkMask) << 3;
                    break;
                case 2:
                    if (returnAddress == uint.MaxValue)
                        returnAddress = (registers.Address + 0x20) & 0x7FFFF;

                    registers.Address = (T1ReadWord(ram, registers.Address + 2) & CommandLinkMask) << 3;
                    break;
                case 3:
                    if (returnAddress != uint.MaxValue)
                    {
                        registers.Address = returnAddress;
                        returnAddress = uint.MaxValue;
                    }
                    else
                        registers.Address = (registers.Address + 0x20) & 0x7FFFF;
                    break;
            }

            command = T1ReadWord(ram, registers.Address);
            commandCounter++;
        }
    }

    internal void Draw()
    {
        if (!_displayToggle)
        {
            NoDraw();
            return;
        }

        _registers.Address = 0;
        _registers.EDSR >>= 1;
        _registers.COPR = 0;
        video.StartVdp1Draw(this);
        _registers.EDSR |= 2;
        _registers.COPR = unchecked((ushort)(_registers.Address >> 3));
        sendDrawEnd();
        video.EndVdp1Draw();
    }

    private void NoDraw()
    {
        _registers.EDSR >>= 1;
        _registers.COPR = 0;
        FakeDrawCommands(_ram, ref _registers);
        _registers.EDSR |= 2;
        sendDrawEnd();
        ManualFrameBufferChange = false;
    }

    public static SaturnVdp1Command ReadCommand(uint address, byte[] ram) => new()
    {
        CMDCTRL = T1ReadWord(ram, address),
        CMDLINK = T1ReadWord(ram, address + 0x2),
        CMDPMOD = T1ReadWord(ram, address + 0x4),
        CMDCOLR = T1ReadWord(ram, address + 0x6),
        CMDSRCA = T1ReadWord(ram, address + 0x8),
        CMDSIZE = T1ReadWord(ram, address + 0xA),
        CMDXA = unchecked((short)T1ReadWord(ram, address + 0xC)),
        CMDYA = unchecked((short)T1ReadWord(ram, address + 0xE)),
        CMDXB = unchecked((short)T1ReadWord(ram, address + 0x10)),
        CMDYB = unchecked((short)T1ReadWord(ram, address + 0x12)),
        CMDXC = unchecked((short)T1ReadWord(ram, address + 0x14)),
        CMDYC = unchecked((short)T1ReadWord(ram, address + 0x16)),
        CMDXD = unchecked((short)T1ReadWord(ram, address + 0x18)),
        CMDYD = unchecked((short)T1ReadWord(ram, address + 0x1A)),
        CMDGRDA = T1ReadWord(ram, address + 0x1C)
    };

    public void ToggleDisplay() => _displayToggle = !_displayToggle;
}

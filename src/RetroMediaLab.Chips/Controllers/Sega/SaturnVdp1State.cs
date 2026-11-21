// Copyright 2003-2005 Guillaume Duhamel
// Copyright 2004 Lawrence Sebald
// Copyright 2004-2006 Theo Berkau
// Translated from Yabause vdp1.h.
// SPDX-License-Identifier: GPL-2.0-or-later

namespace RetroMediaLab.Chips.Controllers.Sega;

public struct SaturnVdp1Registers
{
    public ushort TVMR;
    public ushort FBCR;
    public ushort PTMR;
    public ushort EWDR;
    public ushort EWLR;
    public ushort EWRR;
    public ushort ENDR;
    public ushort EDSR;
    public ushort LOPR;
    public ushort COPR;
    public ushort MODR;
    public uint Address;
    public short LocalX;
    public short LocalY;
    public ushort SystemClipX1;
    public ushort SystemClipY1;
    public ushort SystemClipX2;
    public ushort SystemClipY2;
    public ushort UserClipX1;
    public ushort UserClipY1;
    public ushort UserClipX2;
    public ushort UserClipY2;
}

public struct SaturnVdp1Command
{
    public ushort CMDCTRL;
    public ushort CMDLINK;
    public ushort CMDPMOD;
    public ushort CMDCOLR;
    public ushort CMDSRCA;
    public ushort CMDSIZE;
    public short CMDXA;
    public short CMDYA;
    public short CMDXB;
    public short CMDYB;
    public short CMDXC;
    public short CMDYC;
    public short CMDXD;
    public short CMDYD;
    public ushort CMDGRDA;
}

public interface ISaturnVdp1Video
{
    void ResetVdp1(ref SaturnVdp1Registers registers);
    void StartVdp1Draw(SaturnVdp1 vdp1);
    void EndVdp1Draw();
    void NormalSpriteDraw(byte[] ram, ref SaturnVdp1Registers registers, byte[] backFrameBuffer);
    void ScaledSpriteDraw(byte[] ram, ref SaturnVdp1Registers registers, byte[] backFrameBuffer);
    void DistortedSpriteDraw(byte[] ram, ref SaturnVdp1Registers registers, byte[] backFrameBuffer);
    void PolygonDraw(byte[] ram, ref SaturnVdp1Registers registers, byte[] backFrameBuffer);
    void PolylineDraw(byte[] ram, ref SaturnVdp1Registers registers, byte[] backFrameBuffer);
    void LineDraw(byte[] ram, ref SaturnVdp1Registers registers, byte[] backFrameBuffer);
    void UserClipping(byte[] ram, ref SaturnVdp1Registers registers);
    void SystemClipping(byte[] ram, ref SaturnVdp1Registers registers);
    void LocalCoordinate(byte[] ram, ref SaturnVdp1Registers registers);
    Func<int, uint, uint>? ReadVdp1FrameBuffer { get; }
    Action<int, uint, uint>? WriteVdp1FrameBuffer { get; }
}

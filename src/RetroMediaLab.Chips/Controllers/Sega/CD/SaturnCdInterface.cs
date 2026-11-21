// Copyright 2004-2005 Theo Berkau
// Copyright 2005 Joost Peters
// Copyright 2006 Guillaume Duhamel
// Translated from Yabause cdbase.h.
// SPDX-License-Identifier: GPL-2.0-or-later

namespace RetroMediaLab.Chips.Controllers.Sega.CD;

public sealed record SaturnCdInterface(
    int Id,
    string Name,
    Func<string?, int> Initialize,
    Action Deinitialize,
    Func<int> GetStatus,
    SaturnCdInterface.ReadTocHandler ReadToc,
    SaturnCdInterface.ReadToc10Handler ReadToc10,
    SaturnCdInterface.ReadSectorHandler ReadSectorFad,
    Action<uint> ReadAheadFad)
{
    public ReadSectorHandler? ReadSubchannelFad { get; init; }

    public const int Default = -1;
    public const int Dummy = 0;
    public const int Iso = 1;
    public const int Architecture = 2;

    public delegate int ReadTocHandler(Span<uint> toc);
    public delegate int ReadToc10Handler(Span<Toc10Entry> toc);
    public delegate int ReadSectorHandler(uint fad, Span<byte> buffer);

    public struct Toc10Entry
    {
        public byte ControlAddress;
        public byte TrackNumber;
        public byte Point;
        public byte Minute;
        public byte Second;
        public byte Frame;
        public byte Zero;
        public byte PointMinute;
        public byte PointSecond;
        public byte PointFrame;
    }
}

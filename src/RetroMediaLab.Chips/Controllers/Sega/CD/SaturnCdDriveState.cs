// Copyright 2014-2016 James Laird-Wah
// Copyright 2004-2006, 2013 Theo Berkau
// Translated from Yabause cd_drive.h.
// SPDX-License-Identifier: GPL-2.0-or-later

namespace RetroMediaLab.Chips.Controllers.Sega.CD;

internal sealed class SaturnCdDriveState
{
    internal int CyclesRemainder;
    internal int BitCounter;
    internal int ByteCounter;
    internal Status State;
    internal readonly byte[] StateData = new byte[13];
    internal readonly byte[] ReceivedData = new byte[13];
    internal byte PostSeekState;
    internal readonly SaturnCdInterface.Toc10Entry[] Toc = new SaturnCdInterface.Toc10Entry[103 * 3];
    internal readonly SaturnCdInterface.Toc10Entry[] Tracks = new SaturnCdInterface.Toc10Entry[100];
    internal int TocEntry;
    internal int NumTocEntries, NumTracks;
    internal uint DiscFad, TargetFad;
    internal int SeekTime;
    internal int Speed;

    internal struct Status
    {
        internal byte CurrentOperation, QSubcode, TrackNumber, IndexField;
        internal byte Minutes, Seconds, Frame;
        internal byte AbsoluteMinutes, AbsoluteSeconds, AbsoluteFrame;
    }
}

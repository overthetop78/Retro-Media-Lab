// Copyright 2003-2005 Guillaume Duhamel
// Copyright 2004-2006 Theo Berkau
// Translated from Yabause smpc.h.
// SPDX-License-Identifier: GPL-2.0-or-later

namespace RetroMediaLab.Chips.Controllers.Sega;

public sealed class SaturnSmpcState
{
    public enum Region : byte
    {
        AutoDetect = 0,
        Japan = 1,
        AsiaNtsc = 2,
        NorthAmerica = 4,
        CentralSouthAmericaNtsc = 5,
        Korea = 6,
        AsiaPal = 10,
        Europe = 12,
        CentralSouthAmericaPal = 13
    }

    public sealed class PortData
    {
        public int Offset;
        public int Size;
        public readonly byte[] Data = new byte[256];
    }

    internal readonly byte[] Registers = new byte[64];
    internal readonly byte[] Smem = new byte[4];
    internal byte DotSelect;
    internal byte MasterNmi;
    internal byte SoundReset;
    internal byte SystemReset;
    internal byte ResetButton;
    internal byte Ste;
    internal byte ResetDisabled;
    internal byte Intback;
    internal byte IntbackInput0;
    internal byte FirstPeripheral;
    internal Region RegionId;
    internal Region RegionSetting;
    internal int Timing;
    internal readonly PortData Port1 = new();
    internal readonly PortData Port2 = new();
    internal bool ClockSync;
    internal uint BaseTime;
    internal readonly byte[] Rtc = new byte[7];
    internal bool RtcValid;
    internal long RtcMicroseconds;
    internal bool RtcSetAwaitingCommit;

    internal byte Command { get => Registers[15]; set => Registers[15] = value; }
    internal Span<byte> Output => Registers.AsSpan(16, 32);
    internal byte Status { get => Registers[48]; set => Registers[48] = value; }
    internal byte StatusFlag { get => Registers[49]; set => Registers[49] = value; }
    internal Span<byte> PortDataRegisters => Registers.AsSpan(58, 2);
    internal Span<byte> DataDirection => Registers.AsSpan(60, 2);
    internal byte IoSelect { get => Registers[62]; set => Registers[62] = value; }
    internal byte ExternalLatchEnable { get => Registers[63]; set => Registers[63] = value; }
}

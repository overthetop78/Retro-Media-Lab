// Copyright 2003-2005 Guillaume Duhamel
// Copyright 2004-2006, 2013 Theo Berkau
// Copyright 2016 James Laird-Wah
// Translated from Yabause sh7034.h.
// SPDX-License-Identifier: GPL-2.0-or-later

namespace RetroMediaLab.Chips.CPU.Hitachi.SH1;

internal sealed class SaturnSh7034State
{
    internal byte Tstr, Tsnc, Tmdr, Tfcr, Tocr;
    internal readonly TimerChannel[] Channels = new TimerChannel[5];
    internal readonly byte[] Ram = new byte[0x2000];

    internal struct TimerChannel
    {
        internal byte Tcr, Tior, Tier, Tsr;
        internal ushort Tcnt, Gra, Grb, Bra, Brb;
        internal byte TcntFraction;
    }

    internal struct InterruptController
    {
        internal ushort Ipra, Iprb, Iprc, Iprd, Ipre, Icr;
    }

    internal struct DmaChannel
    {
        internal uint Sar, Dar;
        internal ushort Tcr, Chcr;
        internal bool IsActive;
    }

    internal struct BusStateController
    {
        internal ushort Bcr, Wcr1, Wcr2, Wcr3, Dcr, Pcr, Rcr, Rtcsr, Rtcnt, Rtcor;
    }

    internal struct UserBreakController
    {
        internal uint Bar, Bamr;
        internal ushort Bbr;
    }

    internal struct AnalogToDigitalConverter
    {
        internal ushort Addra, Addrb, Addrc, Addrd;
        internal byte Adcsr, Adcr;
    }

    internal struct PortFunctionController
    {
        internal ushort Paior, Pbior, Pacr1, Pacr2, Pbcr1, Pbcr2;
    }

    internal struct TimingPatternController
    {
        internal byte Tpmr, Tpcr, Nderb, Ndera, Ndrb, Ndra;
    }

    internal struct SerialChannel
    {
        internal byte Smr, Brr, Scr, Tdr, Ssr, Rdr;
        internal byte Rsr, RsrCounter;
        internal byte Tsr, TsrCounter;
        internal bool TdrWritten;
        internal int SerialClockCounter;
    }
}

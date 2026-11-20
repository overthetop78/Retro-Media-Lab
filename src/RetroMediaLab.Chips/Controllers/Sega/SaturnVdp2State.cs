// Copyright 2003-2005 Guillaume Duhamel
// Copyright 2004-2007 Theo Berkau
// Translated from Yabause vdp2.h.
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Runtime.CompilerServices;

namespace RetroMediaLab.Chips.Controllers.Sega;

public struct SaturnVdp2Registers
{
    public struct WordPair
    {
        public ushort High;
        public ushort Low;

        public uint All
        {
            readonly get => ((uint)High << 16) | Low;
            set
            {
                High = (ushort)(value >> 16);
                Low = unchecked((ushort)value);
            }
        }
    }

    public ushort TVMD;
    public ushort EXTEN;
    public ushort TVSTAT;
    public ushort VRSIZE;
    public ushort HCNT;
    public ushort VCNT;
    public ushort RAMCTL;
    public ushort CYCA0L;
    public ushort CYCA0U;
    public ushort CYCA1L;
    public ushort CYCA1U;
    public ushort CYCB0L;
    public ushort CYCB0U;
    public ushort CYCB1L;
    public ushort CYCB1U;
    public ushort BGON;
    public ushort MZCTL;
    public ushort SFSEL;
    public ushort SFCODE;
    public ushort CHCTLA;
    public ushort CHCTLB;
    public ushort BMPNA;
    public ushort BMPNB;
    public ushort PNCN0;
    public ushort PNCN1;
    public ushort PNCN2;
    public ushort PNCN3;
    public ushort PNCR;
    public ushort PLSZ;
    public ushort MPOFN;
    public ushort MPOFR;
    public ushort MPABN0;
    public ushort MPCDN0;
    public ushort MPABN1;
    public ushort MPCDN1;
    public ushort MPABN2;
    public ushort MPCDN2;
    public ushort MPABN3;
    public ushort MPCDN3;
    public ushort MPABRA;
    public ushort MPCDRA;
    public ushort MPEFRA;
    public ushort MPGHRA;
    public ushort MPIJRA;
    public ushort MPKLRA;
    public ushort MPMNRA;
    public ushort MPOPRA;
    public ushort MPABRB;
    public ushort MPCDRB;
    public ushort MPEFRB;
    public ushort MPGHRB;
    public ushort MPIJRB;
    public ushort MPKLRB;
    public ushort MPMNRB;
    public ushort MPOPRB;
    public ushort SCXIN0;
    public ushort SCXDN0;
    public ushort SCYIN0;
    public ushort SCYDN0;
    public WordPair ZMXN0;
    public WordPair ZMYN0;
    public ushort SCXIN1;
    public ushort SCXDN1;
    public ushort SCYIN1;
    public ushort SCYDN1;
    public WordPair ZMXN1;
    public WordPair ZMYN1;
    public ushort SCXN2;
    public ushort SCYN2;
    public ushort SCXN3;
    public ushort SCYN3;
    public ushort ZMCTL;
    public ushort SCRCTL;
    public WordPair VCSTA;
    public WordPair LSTA0;
    public WordPair LSTA1;
    public WordPair LCTA;
    public ushort BKTAU;
    public ushort BKTAL;
    public ushort RPMD;
    public ushort RPRCTL;
    public ushort KTCTL;
    public ushort KTAOF;
    public ushort OVPNRA;
    public ushort OVPNRB;
    public WordPair RPTA;
    public ushort WPSX0;
    public ushort WPSY0;
    public ushort WPEX0;
    public ushort WPEY0;
    public ushort WPSX1;
    public ushort WPSY1;
    public ushort WPEX1;
    public ushort WPEY1;
    public ushort WCTLA;
    public ushort WCTLB;
    public ushort WCTLC;
    public ushort WCTLD;
    public WordPair LWTA0;
    public WordPair LWTA1;
    public ushort SPCTL;
    public ushort SDCTL;
    public ushort CRAOFA;
    public ushort CRAOFB;
    public ushort LNCLEN;
    public ushort SFPRMD;
    public ushort CCCTL;
    public ushort SFCCMD;
    public ushort PRISA;
    public ushort PRISB;
    public ushort PRISC;
    public ushort PRISD;
    public ushort PRINA;
    public ushort PRINB;
    public ushort PRIR;
    public ushort CCRSA;
    public ushort CCRSB;
    public ushort CCRSC;
    public ushort CCRSD;
    public ushort CCRNA;
    public ushort CCRNB;
    public ushort CCRR;
    public ushort CCRLB;
    public ushort CLOFEN;
    public ushort CLOFSL;
    public ushort COAR;
    public ushort COAG;
    public ushort COAB;
    public ushort COBR;
    public ushort COBG;
    public ushort COBB;
}

[InlineArray(88)]
public struct SaturnVdp2CellScrollData
{
    private uint _first;
}

// Copyright 2003-2005 Guillaume Duhamel
// Copyright 2005-2006 Theo Berkau
// Translated from Yabause scu.h.
// SPDX-License-Identifier: GPL-2.0-or-later

namespace RetroMediaLab.Chips.Controllers.Sega;

public sealed partial class SaturnScuInterpreter
{
    private const int MaxBreakpoints = 10;

    private struct CodeBreakpoint
    {
        public uint Address;
    }

    private readonly record struct Interrupt(byte Vector, byte Level, ushort Mask, uint StatusBit);

    private sealed class ScuRegisters
    {
        public uint D0R, D0W, D0C, D0AD, D0EN, D0MD;
        public uint D1R, D1W, D1C, D1AD, D1EN, D1MD;
        public uint D2R, D2W, D2C, D2AD, D2EN, D2MD;
        public uint DSTP, DSTA;

        public uint PPAF = 0;
        public uint PPD = 0;
        public uint PDA;
        public uint PDD = 0;
        public uint T0C, T1S, T1MD;
        public uint IMS, IST;
        public uint AIACK, ASR0, ASR1, AREF;
        public uint RSEL, VER;

        public uint Timer0;
        public uint Timer1;
        public readonly Interrupt[] Interrupts = new Interrupt[30];
        public uint NumberOfInterrupts;
    }

    private sealed class BreakpointState
    {
        public readonly CodeBreakpoint[] CodeBreakpoints = new CodeBreakpoint[MaxBreakpoints];
        public int NumberOfCodeBreakpoints;
        public Action<uint>? BreakpointCallback;
        public byte InBreakpoint;
    }

    private struct ProgramControl
    {
        public uint All;

        public uint P { readonly get => All & 0xFF; set => All = (All & ~0xFFu) | (value & 0xFF); }
        public uint LE { readonly get => GetBit(15); set => SetBit(15, value); }
        public uint EX { readonly get => GetBit(16); set => SetBit(16, value); }
        public uint ES { readonly get => GetBit(17); set => SetBit(17, value); }
        public uint E { readonly get => GetBit(18); set => SetBit(18, value); }
        public uint V { readonly get => GetBit(19); set => SetBit(19, value); }
        public uint C { readonly get => GetBit(20); set => SetBit(20, value); }
        public uint Z { readonly get => GetBit(21); set => SetBit(21, value); }
        public uint S { readonly get => GetBit(22); set => SetBit(22, value); }
        public uint T0 { readonly get => GetBit(23); set => SetBit(23, value); }
        public uint EP { readonly get => GetBit(25); set => SetBit(25, value); }
        public uint PR { readonly get => GetBit(26); set => SetBit(26, value); }

        private readonly uint GetBit(int bit) => (All >> bit) & 1;
        private void SetBit(int bit, uint value) => All = (All & ~(1u << bit)) | ((value & 1) << bit);
    }

    private struct DspValue
    {
        public long All;

        public int L
        {
            readonly get => unchecked((int)All);
            set => All = (All & ~0xFFFFFFFFL) | unchecked((uint)value);
        }

        public short H
        {
            readonly get => unchecked((short)(All >> 32));
            set => All = (All & ~0xFFFF00000000L) | ((long)unchecked((ushort)value) << 32);
        }
    }

    private sealed class DspRegisters
    {
        public readonly uint[] ProgramRam = new uint[256];
        public readonly uint[,] MD = new uint[4, 64];
        public ProgramControl ProgControlPort;
        public byte PC;
        public byte TOP;
        public ushort LOP;
        public uint JumpAddress = uint.MaxValue;
        public bool JumpDelayed;
        public long Multiply;
        public byte DataRamPage;
        public byte DataRamReadAddress;
        public readonly byte[] CT = new byte[4];
        public int RX;
        public int RY;
        public uint RA0;
        public uint WA0;
        public DspValue AC, P, ALU;
    }

    private struct DmaInfo
    {
        public int Mode;
        public uint ReadAddress;
        public uint WriteAddress;
        public uint TransferNumber;
        public uint AddValue;
        public uint ModeAddressUpdate;
    }

    private sealed record DspInterface(
        Action<uint> SetProgram,
        Action<uint> SetDataAddress,
        Action<uint> SetDataRamData,
        Action<uint> SetProgramControl,
        Func<uint> GetProgramControl,
        Func<uint> GetDataRam);
}

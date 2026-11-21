// Translated from the CPU state declarations in Yabause sh2core.h.
// SPDX-License-Identifier: GPL-2.0-or-later

namespace RetroMediaLab.Chips.CPU.Hitachi.SH2;

public sealed class SaturnSh2State
{
    internal const int MaxInterrupts = 50;

    public readonly uint[] R = new uint[16];
    public StatusRegister SR;
    public uint GBR;
    public uint VBR;
    public uint MACH;
    public uint MACL;
    public uint PR;
    public uint PC;

    internal readonly Interrupt[] Interrupts = new Interrupt[MaxInterrupts];
    internal uint NumberOfInterrupts;
    internal uint Delay;
    internal uint Cycles;
    internal bool IsIdle;
    internal ushort Instruction;

    internal readonly record struct Interrupt(byte Vector, byte Level);

    public struct StatusRegister
    {
        public uint All;

        public uint T { readonly get => All & 1; set => All = (All & ~1u) | (value & 1); }
        public uint S { readonly get => (All >> 1) & 1; set => All = (All & ~2u) | ((value & 1) << 1); }
        public uint I { readonly get => (All >> 4) & 15; set => All = (All & ~0xF0u) | ((value & 15) << 4); }
        public uint Q { readonly get => (All >> 8) & 1; set => All = (All & ~0x100u) | ((value & 1) << 8); }
        public uint M { readonly get => (All >> 9) & 1; set => All = (All & ~0x200u) | ((value & 1) << 9); }
    }

    internal struct OnchipRegisters
    {
        internal byte SMR;
        internal byte BRR;
        internal byte SCR;
        internal byte TDR;
        internal byte SSR;
        internal byte RDR;
        internal byte TIER;
        internal byte FTCSR;
        internal ushort FRC;
        internal ushort OCRA;
        internal ushort OCRB;
        internal byte TCR;
        internal byte TOCR;
        internal ushort FICR;
        internal ushort IPRB;
        internal ushort VCRA;
        internal ushort VCRB;
        internal ushort VCRC;
        internal ushort VCRD;
        internal byte DRCR0;
        internal byte DRCR1;
        internal byte WTCSR;
        internal byte WTCNT;
        internal byte RSTCSR;
        internal byte SBYCR;
        internal byte CCR;
        internal ushort ICR;
        internal ushort IPRA;
        internal ushort VCRWDT;
        internal uint DVSR;
        internal uint DVCR;
        internal uint VCRDIV;
        internal uint DVDNTH;
        internal uint DVDNTL;
        internal uint DVDNTUH;
        internal uint DVDNTUL;
        internal uint BARA;
        internal uint BAMRA;
        internal uint BBRA;
        internal uint BARB;
        internal uint BAMRB;
        internal uint BBRB;
        internal uint BDRB;
        internal uint BDMRB;
        internal uint BRCR;
        internal uint SAR0;
        internal uint DAR0;
        internal uint TCR0;
        internal uint CHCR0;
        internal uint SAR1;
        internal uint DAR1;
        internal uint TCR1;
        internal uint CHCR1;
        internal uint VCRDMA0;
        internal uint VCRDMA1;
        internal uint DMAOR;
        internal ushort BCR1;
        internal ushort BCR2;
        internal ushort WCR;
        internal ushort MCR;
        internal ushort RTCSR;
        internal ushort RTCNT;
        internal ushort RTCOR;
        internal bool Dma0Active;
        internal bool Dma1Active;
        internal bool DmaRobin;
    }
}

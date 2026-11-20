// Copyright 2015 Theo Berkau
// Translated from Yabause scspdsp.h.
// SPDX-License-Identifier: GPL-2.0-or-later

namespace RetroMediaLab.Chips.Audio.Yamaha.SCSP;

internal readonly record struct SaturnScspDspInstruction(ulong All)
{
    public uint Nxadr => (uint)((All >> 0) & 0x1);
    public uint Adreb => (uint)((All >> 1) & 0x1);
    public uint Masa => (uint)((All >> 2) & 0x1F);
    public uint Nofl => (uint)((All >> 8) & 0x1);
    public uint Coef => (uint)((All >> 9) & 0x3F);
    public uint Bsel => (uint)((All >> 16) & 0x1);
    public uint Zero => (uint)((All >> 17) & 0x1);
    public uint Negb => (uint)((All >> 18) & 0x1);
    public uint Yrl => (uint)((All >> 19) & 0x1);
    public uint Shift => (uint)((All >> 20) & 0x3);
    public uint Frcl => (uint)((All >> 22) & 0x1);
    public uint Adrl => (uint)((All >> 23) & 0x1);
    public uint Ewa => (uint)((All >> 24) & 0xF);
    public uint Ewt => (uint)((All >> 28) & 0x1);
    public uint Mrd => (uint)((All >> 29) & 0x1);
    public uint Mwt => (uint)((All >> 30) & 0x1);
    public uint Table => (uint)((All >> 31) & 0x1);
    public uint Iwa => (uint)((All >> 32) & 0x1F);
    public uint Iwt => (uint)((All >> 37) & 0x1);
    public uint Ira => (uint)((All >> 38) & 0x3F);
    public uint Unknown2 => (uint)((All >> 44) & 0x1);
    public uint Ysel => (uint)((All >> 45) & 0x3);
    public uint Xsel => (uint)((All >> 47) & 0x1);
    public uint Twa => (uint)((All >> 48) & 0x7F);
    public uint Twt => (uint)((All >> 55) & 0x1);
    public uint Tra => (uint)((All >> 56) & 0x7F);
    public uint Unknown => (uint)((All >> 63) & 0x1);
}

internal sealed record SaturnScspDspInterface(
    Action<uint, uint> SetRblRbp,
    Action<uint, uint> SetExternalInputs,
    Action<uint, uint> SetMix,
    Func<int, uint> GetEffectOut,
    Action<ulong, uint> SetMpro,
    Func<uint, ulong> GetMpro,
    Action<uint, uint> SetCoefficient,
    Action<uint, uint> SetMemoryAddress,
    Func<uint, uint> GetCoefficient,
    Func<uint, uint> GetExternalInput,
    Func<uint, uint> GetMemoryAddress,
    Func<uint, uint> GetMems,
    Action Execute);

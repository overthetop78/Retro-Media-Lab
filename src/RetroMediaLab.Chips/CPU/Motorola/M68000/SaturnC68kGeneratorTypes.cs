// Copyright 2003-2004 Stephane Dallongeville
// Copyright 2004 Theo Berkau
// Translated from Yabause c68k/gen68k.h.
// SPDX-License-Identifier: GPL-2.0-or-later

#if C68K_GEN
namespace RetroMediaLab.Chips.CPU.Motorola.M68000;

internal sealed partial class SaturnC68kGenerator
{
    private readonly record struct EffectiveAddressInfo(uint Name, uint Mask, uint Match);

    private readonly record struct OpcodeInfo(
        string Name,
        ushort Base,
        ushort Mask,
        sbyte SizeType,
        sbyte SizeShift,
        sbyte EffectiveAddressModeShift,
        sbyte RegisterShift,
        sbyte SecondEffectiveAddressModeShift,
        sbyte SecondRegisterShift,
        string SupportedEffectiveAddresses,
        string SecondSupportedEffectiveAddresses,
        Action Generate);
}
#endif

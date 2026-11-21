// Copyright 2012 Guillaume Duhamel
// Translated from Yabause titan/titan.h.
// SPDX-License-Identifier: GPL-2.0-or-later

namespace RetroMediaLab.Chips.Controllers.Sega;

internal enum SaturnTitanLayer
{
    Back = -1,
    Nbg3 = 0,
    Nbg2 = 1,
    Nbg1 = 2,
    Nbg0 = 3,
    Rbg0 = 4,
    Sprite = 5
}

internal enum SaturnTitanBlend
{
    Top = 0,
    Bottom = 1,
    Add = 2
}

internal enum SaturnTitanShadow : byte
{
    None = 0,
    Normal = 1,
    Msb = 2
}

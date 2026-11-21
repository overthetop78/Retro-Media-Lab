// Copyright (c) 2018 Renaud Toumazet
// Translated from Saturnin interrupt_controller.h.
// SPDX-License-Identifier: Apache-2.0

namespace RetroMediaLab.Chips.Controllers.Sega;

internal readonly record struct SaturnInterrupt(byte Vector, byte Level, byte MaskBit, byte StatusBit, string Name = "")
{
    public static implicit operator int(SaturnInterrupt interrupt) => interrupt.Vector;
}

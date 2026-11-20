// Copyright (C) 2015-2017 Mednafen Team
// Light pulse and bus logic translated from Mednafen ss/input/gun.cpp.
// SPDX-License-Identifier: GPL-2.0-or-later

namespace RetroMediaLab.Chips.Controllers.Sega;

internal sealed class SaturnLightGun
{
    internal uint CyclesUntilEvent { get; private set; } = uint.MaxValue;
    private bool _lightActive;

    internal void Reset()
    {
        _lightActive = false;
        CyclesUntilEvent = uint.MaxValue;
    }

    internal void LineHook(int nominalX, int nominalY, int line, int divider)
    {
        if (Math.Abs(nominalY - line) > 1 || nominalX < 0 || nominalX >= 21472)
            return;

        int delay = (nominalX + 6832 - 80 * divider) * 4 / divider;
        if (delay >= 1)
        {
            _lightActive = false;
            CyclesUntilEvent = (uint)delay;
        }
    }

    internal void Advance(uint cycles)
    {
        if (CyclesUntilEvent == uint.MaxValue)
            return;

        CyclesUntilEvent -= cycles;
        if (CyclesUntilEvent == 0)
        {
            _lightActive = !_lightActive;
            CyclesUntilEvent = _lightActive ? 16u : uint.MaxValue;
        }
    }

    internal byte UpdateBus(byte buttons, byte output, byte asserted)
    {
        int state = (buttons & 0x30) | 0x0C | (_lightActive ? 0 : 0x40);
        return (byte)(((output & asserted) | (state & ~asserted)) & 0x7C);
    }
}

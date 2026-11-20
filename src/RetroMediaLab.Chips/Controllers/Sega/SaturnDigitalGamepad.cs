// Copyright (C) 2015-2017 Mednafen Team
// Bus logic translated from Mednafen ss/input/gamepad.cpp.
// SPDX-License-Identifier: GPL-2.0-or-later

namespace RetroMediaLab.Chips.Controllers.Sega;

internal static class SaturnDigitalGamepad
{
    internal static byte UpdateBus(byte firstButtonByte, byte secondButtonByte, byte output, byte asserted)
    {
        // INTBACK orders the nibbles as TH/TR 01, 10, 00, and 11.
        int buttons = (secondButtonByte >> 4) |
            (firstButtonByte & 0xF0) |
            ((firstButtonByte & 0x0F) << 8) |
            ((0x04 | (secondButtonByte & 0x08)) << 12);

        int selected = (buttons >> ((output >> 5) << 2)) & 0x0F;
        return (byte)(0x10 | (output & (asserted | 0xE0)) | (selected & ~asserted));
    }
}

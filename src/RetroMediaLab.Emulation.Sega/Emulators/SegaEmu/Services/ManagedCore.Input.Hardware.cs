using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Common;
using RetroMediaLab.Chips.Common.Memory;
using RetroMediaLab.Chips.Video.Sega.VDP;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Services;

internal sealed partial class ManagedCore
{
    private void osd_input_update_internal_bitmasks()
    {
        unchecked
        {
            int i = default;
            int player = 0;
            uint temp = default;
            short ret = ReadHostInput(unchecked((uint)(player)), unchecked((uint)(1)), unchecked((uint)(0)), unchecked((uint)(256)));
            for (i = 0; (i < 8); i++) {
            temp = unchecked((uint)(0));
            switch (input_dev[(int)(i)]) {
            case 1:
            if (((unchecked((int)(ret)) & (1 << 10)) != 0)) temp |= unchecked((uint)(1024));
            if (((unchecked((int)(ret)) & (1 << 9)) != 0)) temp |= unchecked((uint)(512));
            if (((unchecked((int)(ret)) & (1 << 11)) != 0)) temp |= unchecked((uint)(256));
            if (((unchecked((int)(ret)) & (1 << 2)) != 0)) temp |= unchecked((uint)(2048));
            goto case 0;
            case 0:
            if (((unchecked((int)(ret)) & (1 << 1)) != 0)) temp |= unchecked((uint)(64));
            goto case 2;
            case 2:
            if (((unchecked((int)(ret)) & (1 << 0)) != 0)) temp |= unchecked((uint)(16));
            if (((unchecked((int)(ret)) & (1 << 8)) != 0)) temp |= unchecked((uint)(32));
            if (((unchecked((int)(ret)) & (1 << 3)) != 0)) temp |= unchecked((uint)(128));
            if (((unchecked((int)(ret)) & (1 << 4)) != 0)) temp |= unchecked((uint)(1));
            if (((unchecked((int)(ret)) & (1 << 5)) != 0)) temp |= unchecked((uint)(2));
            if (((unchecked((int)(ret)) & (1 << 6)) != 0)) temp |= unchecked((uint)(4));
            if (((unchecked((int)(ret)) & (1 << 7)) != 0)) temp |= unchecked((uint)(8));
            player++;
            ret = ReadHostInput(unchecked((uint)(player)), unchecked((uint)(1)), unchecked((uint)(0)), unchecked((uint)(256)));
            break;
            case 3:
            new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(0)] = ReadHostInput(unchecked((uint)(player)), unchecked((uint)(2)), unchecked((uint)(0)), unchecked((uint)(0)));
            if ((invert_mouse != 0)) new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(1)] = ReadHostInput(unchecked((uint)(player)), unchecked((uint)(2)), unchecked((uint)(0)), unchecked((uint)(1))); else new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(1)] = unchecked((short)((-unchecked((int)(ReadHostInput(unchecked((uint)(player)), unchecked((uint)(2)), unchecked((uint)(0)), unchecked((uint)(1))))))));
            if ((unchecked((int)(new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(0)])) < (-255))) new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(0)] = unchecked((short)((-255))); else if ((unchecked((int)(new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(0)])) > 255)) new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(0)] = unchecked((short)(255));
            if ((unchecked((int)(new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(1)])) < (-255))) new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(1)] = unchecked((short)((-255))); else if ((unchecked((int)(new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(1)])) > 255)) new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(1)] = unchecked((short)(255));
            if ((ReadHostInput(unchecked((uint)(player)), unchecked((uint)(2)), unchecked((uint)(0)), unchecked((uint)(2))) != 0)) temp |= unchecked((uint)(16));
            if ((ReadHostInput(unchecked((uint)(player)), unchecked((uint)(2)), unchecked((uint)(0)), unchecked((uint)(3))) != 0)) temp |= unchecked((uint)(32));
            if ((ReadHostInput(unchecked((uint)(player)), unchecked((uint)(2)), unchecked((uint)(0)), unchecked((uint)(5))) != 0)) temp |= unchecked((uint)(64));
            if ((ReadHostInput(unchecked((uint)(player)), unchecked((uint)(2)), unchecked((uint)(0)), unchecked((uint)(6))) != 0)) temp |= unchecked((uint)(128));
            player++;
            ret = ReadHostInput(unchecked((uint)(player)), unchecked((uint)(1)), unchecked((uint)(0)), unchecked((uint)(256)));
            break;
            case 4:
            if ((retro_gun_mode == 0)) {
            int touch_count = default;
            new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(0)] = unchecked((short)((((unchecked((int)(ReadHostInput(unchecked((uint)(player)), unchecked((uint)(6)), unchecked((uint)(0)), unchecked((uint)(0))))) + 32767) * viewport_w) / 65534)));
            new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(1)] = unchecked((short)((((unchecked((int)(ReadHostInput(unchecked((uint)(player)), unchecked((uint)(6)), unchecked((uint)(0)), unchecked((uint)(1))))) + 32767) * viewport_h) / 65534)));
            if ((ReadHostInput(unchecked((uint)(player)), unchecked((uint)(6)), unchecked((uint)(0)), unchecked((uint)(2))) != 0)) temp |= unchecked((uint)(64));
            touch_count = unchecked((int)(ReadHostInput(unchecked((uint)(player)), unchecked((uint)(6)), unchecked((uint)(0)), unchecked((uint)(3)))));
            if ((touch_count == 2)) temp |= unchecked((uint)(16)); else if ((touch_count == 3)) temp |= unchecked((uint)(128)); else if ((touch_count == 4)) temp |= unchecked((uint)(32));
            } else {
            if ((ReadHostInput(unchecked((uint)(player)), unchecked((uint)(4)), unchecked((uint)(0)), unchecked((uint)(15))) != 0)) {
            new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(0)] = unchecked((short)((-1000)));
            new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(1)] = unchecked((short)((-1000)));
            } else {
            new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(0)] = unchecked((short)((((unchecked((int)(ReadHostInput(unchecked((uint)(player)), unchecked((uint)(4)), unchecked((uint)(0)), unchecked((uint)(13))))) + 32767) * viewport_w) / 65534)));
            new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(1)] = unchecked((short)((((unchecked((int)(ReadHostInput(unchecked((uint)(player)), unchecked((uint)(4)), unchecked((uint)(0)), unchecked((uint)(14))))) + 32767) * viewport_h) / 65534)));
            }
            if ((ReadHostInput(unchecked((uint)(player)), unchecked((uint)(4)), unchecked((uint)(0)), unchecked((uint)(2))) != 0)) temp |= unchecked((uint)(64));
            if ((ReadHostInput(unchecked((uint)(player)), unchecked((uint)(4)), unchecked((uint)(0)), unchecked((uint)(3))) != 0)) temp |= unchecked((uint)(16));
            if ((ReadHostInput(unchecked((uint)(player)), unchecked((uint)(4)), unchecked((uint)(0)), unchecked((uint)(4))) != 0)) temp |= unchecked((uint)(32));
            if ((ReadHostInput(unchecked((uint)(player)), unchecked((uint)(4)), unchecked((uint)(0)), unchecked((uint)(6))) != 0)) temp |= unchecked((uint)(128));
            }
            player++;
            ret = ReadHostInput(unchecked((uint)(player)), unchecked((uint)(1)), unchecked((uint)(0)), unchecked((uint)(256)));
            break;
            case 5:
            new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(0)] = unchecked((short)(((unchecked((int)(ReadHostInput(unchecked((uint)(player)), unchecked((uint)(5)), unchecked((uint)(0)), unchecked((uint)(0))))) + 32768) >> 8)));
            if (((unchecked((int)(ret)) & (1 << 0)) != 0)) temp |= unchecked((uint)(16));
            if (((unchecked((int)(ret)) & (1 << 3)) != 0)) temp |= unchecked((uint)(128));
            player++;
            ret = ReadHostInput(unchecked((uint)(player)), unchecked((uint)(1)), unchecked((uint)(0)), unchecked((uint)(256)));
            break;
            case 6:
            new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(0)] = unchecked((short)(((unchecked((int)(ReadHostInput(unchecked((uint)(player)), unchecked((uint)(5)), unchecked((uint)(0)), unchecked((uint)(0))))) + 32768) >> 8)));
            new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(1)] = unchecked((short)(((unchecked((int)(ReadHostInput(unchecked((uint)(player)), unchecked((uint)(5)), unchecked((uint)(0)), unchecked((uint)(1))))) + 32768) >> 8)));
            if (((unchecked((int)(ret)) & (1 << 0)) != 0)) temp |= unchecked((uint)(16));
            if (((unchecked((int)(ret)) & (1 << 8)) != 0)) temp |= unchecked((uint)(32));
            if (((unchecked((int)(ret)) & (1 << 3)) != 0)) temp |= unchecked((uint)(128));
            player++;
            ret = ReadHostInput(unchecked((uint)(player)), unchecked((uint)(1)), unchecked((uint)(0)), unchecked((uint)(256)));
            break;
            case 8:
            new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(0)] = unchecked((short)((60 + (((unchecked((int)(ReadHostInput(unchecked((uint)(player)), unchecked((uint)(6)), unchecked((uint)(0)), unchecked((uint)(0))))) + 32767) * (380 - 60)) / 65534))));
            new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(1)] = unchecked((short)((508 + (((unchecked((int)(ReadHostInput(unchecked((uint)(player)), unchecked((uint)(6)), unchecked((uint)(0)), unchecked((uint)(1))))) + 32767) * (759 - 508)) / 65534))));
            if ((ReadHostInput(unchecked((uint)(player)), unchecked((uint)(2)), unchecked((uint)(0)), unchecked((uint)(2))) != 0)) temp |= unchecked((uint)(128));
            if ((ReadHostInput(unchecked((uint)(player)), unchecked((uint)(2)), unchecked((uint)(0)), unchecked((uint)(3))) != 0)) temp |= unchecked((uint)(16));
            if ((ReadHostInput(unchecked((uint)(player)), unchecked((uint)(2)), unchecked((uint)(0)), unchecked((uint)(4))) != 0)) pico_current = unchecked((byte)(((unchecked((int)(pico_current)) - 1) & 7)));
            if ((ReadHostInput(unchecked((uint)(player)), unchecked((uint)(2)), unchecked((uint)(0)), unchecked((uint)(5))) != 0)) pico_current = unchecked((byte)(((unchecked((int)(pico_current)) + 1) & 7)));
            if (((unchecked((int)(ret)) & (1 << 4)) != 0)) temp |= unchecked((uint)(1));
            if (((unchecked((int)(ret)) & (1 << 5)) != 0)) temp |= unchecked((uint)(2));
            if (((unchecked((int)(ret)) & (1 << 6)) != 0)) temp |= unchecked((uint)(4));
            if (((unchecked((int)(ret)) & (1 << 7)) != 0)) temp |= unchecked((uint)(8));
            player++;
            ret = ReadHostInput(unchecked((uint)(player)), unchecked((uint)(1)), unchecked((uint)(0)), unchecked((uint)(256)));
            break;
            case 9:
            new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(0)] = unchecked((short)((((unchecked((int)(ReadHostInput(unchecked((uint)(player)), unchecked((uint)(6)), unchecked((uint)(0)), unchecked((uint)(0))))) + 32767) * 250) / 65534)));
            new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(1)] = unchecked((short)((((unchecked((int)(ReadHostInput(unchecked((uint)(player)), unchecked((uint)(6)), unchecked((uint)(0)), unchecked((uint)(1))))) + 32767) * 250) / 65534)));
            if ((ReadHostInput(unchecked((uint)(player)), unchecked((uint)(2)), unchecked((uint)(0)), unchecked((uint)(2))) != 0)) temp |= unchecked((uint)(16));
            if ((ReadHostInput(unchecked((uint)(player)), unchecked((uint)(2)), unchecked((uint)(0)), unchecked((uint)(6))) != 0)) temp |= unchecked((uint)(128));
            player++;
            ret = ReadHostInput(unchecked((uint)(player)), unchecked((uint)(1)), unchecked((uint)(0)), unchecked((uint)(256)));
            break;
            case 7:
            new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(0)] = unchecked((short)((((unchecked((int)(ReadHostInput(unchecked((uint)(player)), unchecked((uint)(6)), unchecked((uint)(0)), unchecked((uint)(0))))) + 32767) * 255) / 65534)));
            new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(1)] = unchecked((short)((((unchecked((int)(ReadHostInput(unchecked((uint)(player)), unchecked((uint)(6)), unchecked((uint)(0)), unchecked((uint)(1))))) + 32767) * 255) / 65534)));
            if ((ReadHostInput(unchecked((uint)(player)), unchecked((uint)(2)), unchecked((uint)(0)), unchecked((uint)(2))) != 0)) temp |= unchecked((uint)(4));
            if ((ReadHostInput(unchecked((uint)(player)), unchecked((uint)(2)), unchecked((uint)(0)), unchecked((uint)(6))) != 0)) temp |= unchecked((uint)(2));
            if ((ReadHostInput(unchecked((uint)(player)), unchecked((uint)(2)), unchecked((uint)(0)), unchecked((uint)(3))) != 0)) temp |= unchecked((uint)(1));
            player++;
            ret = ReadHostInput(unchecked((uint)(player)), unchecked((uint)(1)), unchecked((uint)(0)), unchecked((uint)(256)));
            break;
            case 10:
            {
            int rx = unchecked((int)((new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(0)] = ReadHostInput(unchecked((uint)(player)), unchecked((uint)(5)), unchecked((uint)(1)), unchecked((uint)(0))))));
            int ry = unchecked((int)((new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(1)] = ReadHostInput(unchecked((uint)(player)), unchecked((uint)(5)), unchecked((uint)(1)), unchecked((uint)(1))))));
            if ((abs(rx) > abs(ry))) new ArrayPointer<short>(input_analog, (int)((i + 1)) * 2)[(int)(0)] = unchecked((short)(((rx + 32768) >> 8))); else new ArrayPointer<short>(input_analog, (int)((i + 1)) * 2)[(int)(0)] = unchecked((short)(((32767 - ry) >> 8)));
            new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(0)] = unchecked((short)(((unchecked((int)(ReadHostInput(unchecked((uint)(player)), unchecked((uint)(5)), unchecked((uint)(0)), unchecked((uint)(0))))) + 32768) >> 8)));
            new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(1)] = unchecked((short)(((unchecked((int)(ReadHostInput(unchecked((uint)(player)), unchecked((uint)(5)), unchecked((uint)(0)), unchecked((uint)(1))))) + 32768) >> 8)));
            if (((unchecked((int)(ret)) & (1 << 11)) != 0)) temp |= unchecked((uint)(512));
            if (((unchecked((int)(ret)) & (1 << 13)) != 0)) temp |= unchecked((uint)(256));
            if (((unchecked((int)(ret)) & (1 << 10)) != 0)) temp |= unchecked((uint)(32));
            if (((unchecked((int)(ret)) & (1 << 12)) != 0)) temp |= unchecked((uint)(16));
            if (((unchecked((int)(ret)) & (1 << 1)) != 0)) temp |= unchecked((uint)(8192));
            if (((unchecked((int)(ret)) & (1 << 0)) != 0)) temp |= unchecked((uint)(4096));
            if (((unchecked((int)(ret)) & (1 << 2)) != 0)) temp |= unchecked((uint)(1024));
            if (((unchecked((int)(ret)) & (1 << 3)) != 0)) temp |= unchecked((uint)(2048));
            player++;
            ret = ReadHostInput(unchecked((uint)(player)), unchecked((uint)(1)), unchecked((uint)(0)), unchecked((uint)(256)));
            break;
            }
            case 12:
            {
            int pressed = default;
            if ((retro_gun_mode == 0)) {
            pressed = unchecked((int)(ReadHostInput(unchecked((uint)(player)), unchecked((uint)(6)), unchecked((uint)(0)), unchecked((uint)(2)))));
            new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(0)] = unchecked((short)((((unchecked((int)(ReadHostInput(unchecked((uint)(player)), unchecked((uint)(6)), unchecked((uint)(0)), unchecked((uint)(0))))) + 32767) * viewport_w) / 65534)));
            new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(1)] = unchecked((short)((((unchecked((int)(ReadHostInput(unchecked((uint)(player)), unchecked((uint)(6)), unchecked((uint)(0)), unchecked((uint)(1))))) + 32767) * viewport_h) / 65534)));
            } else {
            pressed = unchecked((int)(ReadHostInput(unchecked((uint)(player)), unchecked((uint)(4)), unchecked((uint)(0)), unchecked((uint)(2)))));
            if ((ReadHostInput(unchecked((uint)(player)), unchecked((uint)(4)), unchecked((uint)(0)), unchecked((uint)(15))) != 0)) {
            new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(0)] = (new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(1)] = unchecked((short)((-1))));
            } else {
            new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(0)] = unchecked((short)((((unchecked((int)(ReadHostInput(unchecked((uint)(player)), unchecked((uint)(4)), unchecked((uint)(0)), unchecked((uint)(13))))) + 32767) * viewport_w) / 65534)));
            new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(1)] = unchecked((short)((((unchecked((int)(ReadHostInput(unchecked((uint)(player)), unchecked((uint)(4)), unchecked((uint)(0)), unchecked((uint)(14))))) + 32767) * viewport_h) / 65534)));
            }
            }
            temp = unchecked((uint)(0));
            if ((pressed != 0)) {
            int x = unchecked((int)(new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(0)]));
            int y = (unchecked((int)(new ArrayPointer<short>(input_analog, (int)(i) * 2)[(int)(1)])) - (viewport_h / 2));
            if ((y < (viewport_h / 6))) {
            if ((x < (viewport_w / 3))) temp = unchecked((uint)(4)); else if ((x < ((2 * viewport_w) / 3))) temp = unchecked((uint)(2)); else temp = unchecked((uint)(1));
            } else if ((y < ((2 * viewport_h) / 6))) {
            if ((x < (viewport_w / 3))) temp = unchecked((uint)(32)); else if ((x < ((2 * viewport_w) / 3))) temp = unchecked((uint)(16)); else temp = unchecked((uint)(8));
            } else if ((y < (viewport_h / 2))) {
            if ((x < (viewport_w / 3))) temp = unchecked((uint)(256)); else if ((x < ((2 * viewport_w) / 3))) temp = unchecked((uint)(128)); else temp = unchecked((uint)(64));
            }
            }
            player++;
            ret = ReadHostInput(unchecked((uint)(player)), unchecked((uint)(1)), unchecked((uint)(0)), unchecked((uint)(256)));
            break;
            }
            default:
            break;
            }
            input_pad[(int)(i)] = unchecked((ushort)(temp));
            }
        }
    }
}

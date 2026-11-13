using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Input;

internal sealed partial class InputHardware
{
    internal void input_init()
    {
        unchecked
        {
            int i = default;
            int padtype = default;
            int player = 0;
            for (i = 0; (i < 8); i++) {
            input.dev[(int)(i)] = unchecked((byte)(255));
            input.pad[(int)(i)] = unchecked((ushort)(0));
            }
            if ((unchecked((int)(system_hw)) == 130)) {
            input.dev[(int)(0)] = unchecked((byte)(8));
            return;
            }
            if (((cart_special & unchecked((uint)(2))) != 0)) {
            input.dev[(int)(0)] = unchecked((byte)(9));
            return;
            }
            if (((unchecked((int)(romtype)) & 128) != 0)) {
            padtype = (((unchecked((int)(rom_peripherals)) & 2) != 0) ? 1 : 0);
            } else {
            padtype = 2;
            }
            switch (input.system[(int)(0)]) {
            case 1:
            {
            if ((unchecked((int)(configuredPadTypes[(int)(player)])) != ((2 | 0) | 1))) {
            input.dev[(int)(0)] = configuredPadTypes[(int)(player)];
            } else {
            input.dev[(int)(0)] = unchecked((byte)(padtype));
            }
            player++;
            break;
            }
            case 2:
            {
            input.dev[(int)(0)] = unchecked((byte)(3));
            player++;
            break;
            }
            case 6:
            {
            input.dev[(int)(0)] = unchecked((byte)(11));
            player++;
            break;
            }
            case 5:
            {
            input.dev[(int)(0)] = unchecked((byte)(10));
            player++;
            break;
            }
            case 14:
            {
            for (i = 0; (i < 4); i++) {
            if ((player < 8)) {
            if ((unchecked((int)(configuredPadTypes[(int)(player)])) != ((2 | 0) | 1))) {
            input.dev[(int)(i)] = unchecked((byte)((unchecked((int)(configuredPadTypes[(int)(player)])) & 1)));
            } else {
            input.dev[(int)(i)] = unchecked((byte)((padtype & 1)));
            }
            player++;
            }
            }
            break;
            }
            case 13:
            {
            for (i = 0; (i < 4); i++) {
            if ((player < 8)) {
            if ((unchecked((int)(configuredPadTypes[(int)(player)])) != ((2 | 0) | 1))) {
            input.dev[(int)(i)] = unchecked((byte)((unchecked((int)(configuredPadTypes[(int)(player)])) & 1)));
            } else {
            input.dev[(int)(i)] = unchecked((byte)((padtype & 1)));
            }
            player++;
            }
            }
            teamplayer_init(0);
            break;
            }
            case 12:
            {
            for (i = 0; (i < 4); i++) {
            if ((player < 8)) {
            input.dev[(int)(i)] = unchecked((byte)(2));
            player++;
            }
            }
            break;
            }
            case 7:
            {
            input.dev[(int)(0)] = unchecked((byte)(4));
            player++;
            break;
            }
            case 8:
            {
            input.dev[(int)(0)] = unchecked((byte)(5));
            player++;
            break;
            }
            case 9:
            {
            input.dev[(int)(0)] = unchecked((byte)(6));
            player++;
            break;
            }
            case 10:
            {
            input.dev[(int)(0)] = unchecked((byte)(7));
            player++;
            break;
            }
            case 11:
            {
            input.dev[(int)(0)] = unchecked((byte)(12));
            player++;
            break;
            }
            }
            if ((player == 8)) {
            return;
            }
            switch (input.system[(int)(1)]) {
            case 1:
            {
            if ((unchecked((int)(configuredPadTypes[(int)(player)])) != ((2 | 0) | 1))) {
            input.dev[(int)(4)] = configuredPadTypes[(int)(player)];
            } else {
            input.dev[(int)(4)] = unchecked((byte)(padtype));
            }
            player++;
            break;
            }
            case 2:
            {
            input.dev[(int)(4)] = unchecked((byte)(3));
            player++;
            break;
            }
            case 6:
            {
            input.dev[(int)(4)] = unchecked((byte)(11));
            player++;
            break;
            }
            case 5:
            {
            input.dev[(int)(4)] = unchecked((byte)(10));
            player++;
            break;
            }
            case 3:
            {
            input.dev[(int)(4)] = unchecked((byte)(4));
            player++;
            break;
            }
            case 4:
            {
            for (i = 4; (i < 6); i++) {
            if ((player < 8)) {
            input.dev[(int)(i)] = unchecked((byte)(4));
            player++;
            }
            }
            break;
            }
            case 13:
            {
            for (i = 4; (i < 8); i++) {
            if ((player < 8)) {
            if ((unchecked((int)(configuredPadTypes[(int)(player)])) != ((2 | 0) | 1))) {
            input.dev[(int)(i)] = unchecked((byte)((unchecked((int)(configuredPadTypes[(int)(player)])) & 1)));
            } else {
            input.dev[(int)(i)] = unchecked((byte)((padtype & 1)));
            }
            player++;
            }
            }
            teamplayer_init(1);
            break;
            }
            case 12:
            {
            for (i = 4; (i < 8); i++) {
            if ((player < 8)) {
            input.dev[(int)(i)] = unchecked((byte)(2));
            player++;
            }
            }
            break;
            }
            case 7:
            {
            input.dev[(int)(4)] = unchecked((byte)(4));
            player++;
            break;
            }
            case 8:
            {
            input.dev[(int)(4)] = unchecked((byte)(5));
            player++;
            break;
            }
            case 9:
            {
            input.dev[(int)(4)] = unchecked((byte)(6));
            player++;
            break;
            }
            case 10:
            {
            input.dev[(int)(4)] = unchecked((byte)(7));
            player++;
            break;
            }
            case 11:
            {
            input.dev[(int)(4)] = unchecked((byte)(12));
            player++;
            break;
            }
            }
            if (((cart_special & unchecked((uint)(4))) != 0)) {
            for (i = 5; (i < 7); i++) {
            if ((player < 8)) {
            if ((unchecked((int)(configuredPadTypes[(int)(player)])) != ((2 | 0) | 1))) {
            input.dev[(int)(i)] = unchecked((byte)((unchecked((int)(configuredPadTypes[(int)(player)])) & 1)));
            } else {
            input.dev[(int)(i)] = unchecked((byte)((padtype & 1)));
            }
            player++;
            }
            }
            }
        }
    }
    internal void input_reset()
    {
        unchecked
        {
            int i = default;
            for (i = 0; (i < 8); i++) {
            switch (input.dev[(int)(i)]) {
            case 2:
            case 0:
            case 1:
            {
            gamepad_reset(i);
            break;
            }
            case 4:
            {
            lightgun_reset(i);
            break;
            }
            case 3:
            {
            mouse_reset(i);
            break;
            }
            case 11:
            {
            activator_reset((i >> 2));
            break;
            }
            case 10:
            {
            xe_1ap_reset(i);
            break;
            }
            case 5:
            {
            paddle_reset(i);
            break;
            }
            case 6:
            {
            sportspad_reset(i);
            break;
            }
            case 9:
            {
            terebi_oekaki_reset();
            break;
            }
            case 7:
            {
            graphic_board_reset(i);
            break;
            }
            default:
            {
            break;
            }
            }
            }
            for (i = 0; (i < 2); i++) {
            if ((unchecked((int)(input.system[(int)(i)])) == 13)) {
            teamplayer_reset(i);
            }
            }
        }
    }
    internal void input_refresh()
    {
        unchecked
        {
            int i = default;
            for (i = 0; (i < 8); i++) {
            switch (input.dev[(int)(i)]) {
            case 1:
            {
            gamepad_refresh(i);
            break;
            }
            case 4:
            {
            lightgun_refresh(i);
            break;
            }
            }
            }
        }
    }
    internal void input_end_frame(uint cycles)
    {
        unchecked
        {
            int i = default;
            for (i = 0; (i < 8); i++) {
            switch (input.dev[(int)(i)]) {
            case 0:
            case 1:
            {
            gamepad_end_frame(i, cycles);
            break;
            }
            }
            }
        }
    }
}

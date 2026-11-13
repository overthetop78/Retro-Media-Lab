using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Input;

internal sealed partial class InputHardware
{
    internal void dummy_write(byte data, byte mask)
    {
        unchecked
        {

        }
    }
    internal byte dummy_read()
    {
        unchecked
        {
            return unchecked((byte)(127));
        }
    }
    internal void io_init()
    {
        unchecked
        {
            input_init();
            switch (input.system[(int)(0)]) {
            case 1:
            {
            port[(int)(0)].data_w = ((unchecked((int)(input.dev[(int)(0)])) == 2) ? dummy_write : gamepad_1_write);
            port[(int)(0)].data_r = gamepad_1_read;
            break;
            }
            case 2:
            {
            port[(int)(0)].data_w = mouse_write;
            port[(int)(0)].data_r = mouse_read;
            break;
            }
            case 6:
            {
            port[(int)(0)].data_w = activator_1_write;
            port[(int)(0)].data_r = activator_1_read;
            break;
            }
            case 5:
            {
            port[(int)(0)].data_w = xe_1ap_1_write;
            port[(int)(0)].data_r = xe_1ap_1_read;
            break;
            }
            case 14:
            {
            port[(int)(0)].data_w = wayplay_1_write;
            port[(int)(0)].data_r = wayplay_1_read;
            break;
            }
            case 13:
            {
            port[(int)(0)].data_w = teamplayer_1_write;
            port[(int)(0)].data_r = teamplayer_1_read;
            break;
            }
            case 12:
            {
            port[(int)(0)].data_w = mastertap_1_write;
            port[(int)(0)].data_r = mastertap_1_read;
            break;
            }
            case 7:
            {
            port[(int)(0)].data_w = dummy_write;
            port[(int)(0)].data_r = phaser_1_read;
            break;
            }
            case 8:
            {
            port[(int)(0)].data_w = paddle_1_write;
            port[(int)(0)].data_r = paddle_1_read;
            break;
            }
            case 9:
            {
            port[(int)(0)].data_w = sportspad_1_write;
            port[(int)(0)].data_r = sportspad_1_read;
            break;
            }
            case 10:
            {
            port[(int)(0)].data_w = graphic_board_write;
            port[(int)(0)].data_r = graphic_board_read;
            break;
            }
            case 11:
            {
            port[(int)(0)].data_w = dummy_write;
            port[(int)(0)].data_r = smash_1_read;
            break;
            }
            default:
            {
            port[(int)(0)].data_w = dummy_write;
            port[(int)(0)].data_r = dummy_read;
            break;
            }
            }
            switch (input.system[(int)(1)]) {
            case 1:
            {
            port[(int)(1)].data_w = ((unchecked((int)(input.dev[(int)(4)])) == 2) ? dummy_write : gamepad_2_write);
            port[(int)(1)].data_r = gamepad_2_read;
            break;
            }
            case 2:
            {
            port[(int)(1)].data_w = mouse_write;
            port[(int)(1)].data_r = mouse_read;
            break;
            }
            case 5:
            {
            port[(int)(1)].data_w = xe_1ap_2_write;
            port[(int)(1)].data_r = xe_1ap_2_read;
            break;
            }
            case 6:
            {
            port[(int)(1)].data_w = activator_2_write;
            port[(int)(1)].data_r = activator_2_read;
            break;
            }
            case 3:
            {
            port[(int)(1)].data_w = dummy_write;
            port[(int)(1)].data_r = menacer_read;
            break;
            }
            case 4:
            {
            port[(int)(1)].data_w = justifier_write;
            port[(int)(1)].data_r = justifier_read;
            break;
            }
            case 14:
            {
            port[(int)(1)].data_w = wayplay_2_write;
            port[(int)(1)].data_r = wayplay_2_read;
            break;
            }
            case 13:
            {
            port[(int)(1)].data_w = teamplayer_2_write;
            port[(int)(1)].data_r = teamplayer_2_read;
            break;
            }
            case 12:
            {
            port[(int)(1)].data_w = mastertap_2_write;
            port[(int)(1)].data_r = mastertap_2_read;
            break;
            }
            case 7:
            {
            port[(int)(1)].data_w = dummy_write;
            port[(int)(1)].data_r = phaser_2_read;
            break;
            }
            case 8:
            {
            port[(int)(1)].data_w = paddle_2_write;
            port[(int)(1)].data_r = paddle_2_read;
            break;
            }
            case 9:
            {
            port[(int)(1)].data_w = sportspad_2_write;
            port[(int)(1)].data_r = sportspad_2_read;
            break;
            }
            case 10:
            {
            port[(int)(1)].data_w = graphic_board_write;
            port[(int)(1)].data_r = graphic_board_read;
            break;
            }
            case 11:
            {
            port[(int)(1)].data_w = dummy_write;
            port[(int)(1)].data_r = smash_2_read;
            break;
            }
            default:
            {
            port[(int)(1)].data_w = dummy_write;
            port[(int)(1)].data_r = dummy_read;
            break;
            }
            }
            port[(int)(2)].data_w = dummy_write;
            port[(int)(2)].data_r = dummy_read;
        }
    }
    internal void io_reset()
    {
        unchecked
        {
            if (((unchecked((int)(system_hw)) & 129) == 128)) {
            io_reg[(int)(0)] = unchecked((byte)((unchecked((int)(region_code)) | (unchecked((int)(bios_configuration)) & 1))));
            io_reg[(int)(1)] = unchecked((byte)(0));
            io_reg[(int)(2)] = unchecked((byte)(0));
            io_reg[(int)(3)] = unchecked((byte)(0));
            io_reg[(int)(4)] = unchecked((byte)(0));
            io_reg[(int)(5)] = unchecked((byte)(0));
            io_reg[(int)(6)] = unchecked((byte)(0));
            io_reg[(int)(7)] = unchecked((byte)(255));
            io_reg[(int)(8)] = unchecked((byte)(0));
            io_reg[(int)(9)] = unchecked((byte)(0));
            io_reg[(int)(10)] = unchecked((byte)(255));
            io_reg[(int)(11)] = unchecked((byte)(0));
            io_reg[(int)(12)] = unchecked((byte)(0));
            io_reg[(int)(13)] = unchecked((byte)(251));
            io_reg[(int)(14)] = unchecked((byte)(0));
            io_reg[(int)(15)] = unchecked((byte)(0));
            if ((unchecked((int)(system_hw)) != 132)) {
            io_reg[(int)(0)] |= unchecked((byte)(32));
            }
            } else {
            io_reg[(int)(0)] = unchecked((byte)((128 | (unchecked((int)(region_code)) >> 1))));
            io_reg[(int)(1)] = unchecked((byte)(0));
            io_reg[(int)(2)] = unchecked((byte)(255));
            io_reg[(int)(3)] = unchecked((byte)(0));
            io_reg[(int)(4)] = unchecked((byte)(255));
            io_reg[(int)(5)] = unchecked((byte)(0));
            io_reg[(int)(6)] = unchecked((byte)(255));
            io_reg[(int)(13)] = unchecked((byte)(16));
            if ((unchecked((int)(system_hw)) != 129)) {
            io_reg[(int)(13)] |= unchecked((byte)(32));
            }
            if ((((unchecked((int)(system_hw)) & 32) != 0) || ((unchecked((int)(system_hw)) & 64) != 0))) {
            io_reg[(int)(14)] = unchecked((byte)((IsBiosMapped() ? 224 : 168)));
            } else {
            io_reg[(int)(14)] = unchecked((byte)(0));
            }
            if ((unchecked((int)(system_hw)) >= 32)) {
            io_reg[(int)(15)] = unchecked((byte)(255));
            } else {
            io_reg[(int)(15)] = unchecked((byte)(245));
            }
            }
            input_reset();
        }
    }
    internal void io_68k_write(uint offset, uint data)
    {
        unchecked
        {
            switch (offset) {
            case 1:
            case 2:
            case 3:
            {
            io_reg[(int)(offset)] = unchecked((byte)(data));
            port[(int)((offset - unchecked((uint)(1))))].data_w(unchecked((byte)(data)), io_reg[(int)((offset + unchecked((uint)(3))))]);
            return;
            }
            case 4:
            case 5:
            case 6:
            {
            if ((data != unchecked((uint)(io_reg[(int)(offset)])))) {
            io_reg[(int)(offset)] = unchecked((byte)(data));
            port[(int)((offset - unchecked((uint)(4))))].data_w(io_reg[(int)((offset - unchecked((uint)(3))))], unchecked((byte)(data)));
            }
            return;
            }
            case 7:
            case 10:
            case 13:
            {
            io_reg[(int)(offset)] = unchecked((byte)(data));
            return;
            }
            case 9:
            case 12:
            case 15:
            {
            io_reg[(int)(offset)] = unchecked((byte)((data & unchecked((uint)(248)))));
            return;
            }
            default:
            {
            return;
            }
            }
        }
    }
    internal uint io_68k_read(uint offset)
    {
        unchecked
        {
            switch (offset) {
            case 1:
            case 2:
            case 3:
            {
            uint mask = unchecked((uint)((128 | unchecked((int)(io_reg[(int)((offset + unchecked((uint)(3))))])))));
            uint data = unchecked((uint)(port[(int)((offset - unchecked((uint)(1))))].data_r()));
            return ((unchecked((uint)(io_reg[(int)(offset)])) & mask) | (data & (~mask)));
            }
            default:
            {
            return unchecked((uint)(io_reg[(int)(offset)]));
            }
            }
        }
    }
    internal void io_z80_write(uint offset, uint data, uint cycles)
    {
        unchecked
        {
            if ((offset != 0)) {
            port[(int)(0)].data_w(unchecked((byte)(((data << 1) & unchecked((uint)(96))))), unchecked((byte)((((~data) << 5) & unchecked((uint)(96))))));
            port[(int)(1)].data_w(unchecked((byte)(((data >> 1) & unchecked((uint)(96))))), unchecked((byte)((((~data) << 3) & unchecked((uint)(96))))));
            if ((((!((unchecked((int)(io_reg[(int)(15)])) & 128) != 0)) && ((data & unchecked((uint)(128))) != 0)) || ((!((unchecked((int)(io_reg[(int)(15)])) & 32) != 0)) && ((data & unchecked((uint)(32))) != 0)))) {
            hvc_latch = unchecked((uint)((unchecked((int)(hctab[(int)((cycles % unchecked((uint)(3420))))])) | 65536)));
            }
            if ((unchecked((int)(region_code)) == 0)) {
            data &= unchecked((uint)(15));
            }
            io_reg[(int)(15)] = unchecked((byte)(data));
            } else {
            io_reg[(int)(14)] = unchecked((byte)(data));
            sms_cart_switch(unchecked((byte)((~data))));
            }
        }
    }
    internal uint io_z80_read(uint offset)
    {
        unchecked
        {
            uint data = unchecked((uint)((unchecked((int)(port[(int)(0)].data_r())) | (unchecked((int)(port[(int)(1)].data_r())) << 8))));
            uint ctrl = unchecked((uint)(io_reg[(int)(15)]));
            if ((offset != 0)) {
            data = (((((data >> 10) & unchecked((uint)(15))) | (data & unchecked((uint)(64)))) | ((data >> 7) & unchecked((uint)(128)))) | unchecked((uint)(io_reg[(int)(13)])));
            io_reg[(int)(13)] |= unchecked((byte)(16));
            if ((!((ctrl & unchecked((uint)(8))) != 0))) {
            data &= unchecked((uint)((~128)));
            data |= (ctrl & unchecked((uint)(128)));
            }
            if ((!((ctrl & unchecked((uint)(2))) != 0))) {
            data &= unchecked((uint)((~64)));
            data |= ((ctrl & unchecked((uint)(32))) << 1);
            }
            if ((!((ctrl & unchecked((uint)(4))) != 0))) {
            data &= unchecked((uint)((~8)));
            data |= ((ctrl & unchecked((uint)(64))) >> 3);
            }
            } else {
            data = ((data & unchecked((uint)(63))) | ((data >> 2) & unchecked((uint)(192))));
            if ((!((ctrl & unchecked((uint)(1))) != 0))) {
            data &= unchecked((uint)((~32)));
            data |= ((ctrl & unchecked((uint)(16))) << 1);
            }
            }
            return data;
        }
    }
    internal void io_gg_write(uint offset, uint data)
    {
        unchecked
        {
            switch (offset) {
            case 1:
            io_reg[(int)(1)] = unchecked((byte)(data));
            return;
            case 2:
            io_reg[(int)(2)] = unchecked((byte)(data));
            return;
            case 3:
            io_reg[(int)(3)] = unchecked((byte)(data));
            return;
            case 5:
            io_reg[(int)(5)] = unchecked((byte)((data & unchecked((uint)(248)))));
            return;
            case 6:
            io_reg[(int)(6)] = unchecked((byte)(data));
            psg_config(z80Cycles, psg_preamp, data);
            return;
            default:
            return;
            }
        }
    }
    internal uint io_gg_read(uint offset)
    {
        unchecked
        {
            switch (offset) {
            case 0:
            return unchecked((uint)((unchecked((int)(io_reg[(int)(0)])) & (~(unchecked((int)(input.pad[(int)(0)])) & 128)))));
            case 1:
            return unchecked((uint)(((unchecked((int)(io_reg[(int)(1)])) & (~(unchecked((int)(io_reg[(int)(2)])) & 127))) | (unchecked((int)(io_reg[(int)(2)])) & 127))));
            case 2:
            return unchecked((uint)(io_reg[(int)(2)]));
            case 3:
            return unchecked((uint)(io_reg[(int)(3)]));
            case 4:
            return unchecked((uint)(io_reg[(int)(4)]));
            case 5:
            return unchecked((uint)(io_reg[(int)(5)]));
            default:
            return unchecked((uint)(255));
            }
        }
    }
}

using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cartridge;

internal sealed partial class FlashCfi
{
    private void flash_cfi_write(uint address, uint data)
    {
        unchecked
        {
            uint index = ((address >> 15) & unchecked((uint)(255)));
            if ((flash.mode == 3)) {
            if ((index > unchecked((uint)(0)))) {
            if ((!memory_map[(int)(index)].@base.Same(sram.sram))) {
            int i = 127;
            do {
            if (memory_map[(int)(i)].@base.Same(sram.sram)) {
            memory_map[(int)(i)].@base = (new CPointer<byte>(cart.rom) + (i << 16));
            CopyMemory(memory_map[(int)(i)].@base, new CPointer<byte>(sram.sram), 65536);
            break;
            }
            } while (((--i) > 0));
            CopyMemory(new CPointer<byte>(sram.sram), memory_map[(int)(index)].@base, 65536);
            memory_map[(int)(index)].@base = new CPointer<byte>(sram.sram);
            }
            sram.sram[(int)(((address << 1) & unchecked((uint)(65534))))] = unchecked((byte)((((data & unchecked((uint)(((unchecked((int)(sram.sram[(int)(((address << 1) & unchecked((uint)(65534))))])) << 8) | unchecked((int)(sram.sram[(int)((((address << 1) & unchecked((uint)(65534))) + unchecked((uint)(1))))])))))) >> 8) & unchecked((uint)(255)))));
            sram.sram[(int)((((address << 1) & unchecked((uint)(65534))) + unchecked((uint)(1))))] = unchecked((byte)(((data & unchecked((uint)(((unchecked((int)(sram.sram[(int)(((address << 1) & unchecked((uint)(65534))))])) << 8) | unchecked((int)(sram.sram[(int)((((address << 1) & unchecked((uint)(65534))) + unchecked((uint)(1))))])))))) & unchecked((uint)(255)))));
            }
            flash.mode = 0;
            } else {
            address &= unchecked((uint)(4095));
            data &= unchecked((uint)(255));
            if (((data == unchecked((uint)(240))) && (flash.mode < 7))) {
            flash.mode = 0;
            } else {
            switch (flash.mode) {
            case 1:
            {
            if ((address == unchecked((uint)(1365)))) {
            if ((data == unchecked((uint)(128)))) {
            flash.mode = 4;
            } else if ((data == unchecked((uint)(136)))) {
            flash.mode = 7;
            } else if ((data == unchecked((uint)(144)))) {
            flash.mode = 2;
            } else if ((data == unchecked((uint)(160)))) {
            flash.mode = 3;
            }
            }
            break;
            }
            case 7:
            {
            if (((((flash.addr == unchecked((uint)(1365))) && (flash.data == unchecked((uint)(170)))) && (address == unchecked((uint)(682)))) && (data == unchecked((uint)(85))))) {
            flash.mode = 8;
            }
            break;
            }
            case 8:
            {
            if ((((flash.addr == unchecked((uint)(1365))) && (flash.data == unchecked((uint)(144)))) && (data == unchecked((uint)(0))))) {
            flash.mode = 0;
            }
            break;
            }
            case 4:
            {
            if (((((flash.addr == unchecked((uint)(1365))) && (flash.data == unchecked((uint)(170)))) && (address == unchecked((uint)(682)))) && (data == unchecked((uint)(85))))) {
            flash.mode = 5;
            }
            break;
            }
            case 5:
            {
            if (((address == unchecked((uint)(1365))) && (data == unchecked((uint)(16))))) {
            int i = default;
            for (i = 1; (i < 128); i++) {
            if ((memory_map[(int)(i)].write16 != m68k_unused_16_w)) {
            FillMemory(memory_map[(int)(i)].@base, 255, 65536);
            break;
            }
            }
            flash.mode = 0;
            } else if ((data == unchecked((uint)(48)))) {
            if ((index > unchecked((uint)(0)))) {
            FillMemory(memory_map[(int)(index)].@base, 255, 65536);
            }
            flash.mode = 0;
            }
            break;
            }
            default:
            {
            if (((((flash.addr == unchecked((uint)(1365))) && (flash.data == unchecked((uint)(170)))) && (address == unchecked((uint)(682)))) && (data == unchecked((uint)(85))))) {
            flash.mode = 1;
            } else if (((address == unchecked((uint)(85))) && (data == unchecked((uint)(152))))) {
            flash.mode = 6;
            }
            break;
            }
            }
            }
            }
            flash.addr = address;
            flash.data = data;
        }
    }
    private uint flash_cfi_read(uint address)
    {
        unchecked
        {
            switch (flash.mode) {
            case 7:
            case 8:
            {
            return unchecked((uint)(flash.otp_area[(int)((address & unchecked((uint)((128 - 1)))))]));
            }
            case 2:
            {
            return unchecked((uint)(flash.id[(int)((address & unchecked((uint)(flash.readmask))))]));
            }
            case 6:
            {
            int index = unchecked((int)((address - unchecked((uint)(16)))));
            if (((index >= 0) && (index < 65))) {
            return unchecked((uint)(flash.cfi_data[(int)(index)]));
            }
            return unchecked((uint)(0));
            }
            default:
            {
            if (memory_map[(int)(((address >> 15) & unchecked((uint)(255))))].@base.Same(sram.sram)) {
            return unchecked((uint)(((unchecked((int)(sram.sram[(int)(((address << 1) & unchecked((uint)(65534))))])) << 8) | unchecked((int)(sram.sram[(int)((((address << 1) & unchecked((uint)(65534))) + unchecked((uint)(1))))])))));
            } else {
            return unchecked((uint)((memory_map[(int)(((address >> 15) & unchecked((uint)(255))))].@base + unchecked((int)(((address << 1) & unchecked((uint)(65534)))))).Cast<ushort>()[0]));
            }
            }
            }
        }
    }
}

using RetroMediaLab.Chips.Common.Memory;

using System.Buffers.Binary;


namespace RetroMediaLab.Chips.Controllers.Sega.CD;

public sealed partial class Gfx
{
    private void word_ram_0_dma_w(uint length)
    {
        unchecked
        {
            ushort data = default;
            uint words = (length >> 1);
            ushort src_index = unchecked((ushort)((unchecked((int)(cdc_dac.Word)) & 16382)));
            uint dst_index = unchecked((uint)(((unchecked((int)(scd_regs[(int)((10 >> 1))].Word)) << 3) & 131070)));
            scd_regs[(int)((10 >> 1))].Word += unchecked((ushort)((words >> 2)));
            cdc_dac.Word += unchecked((ushort)((words << 1)));
            while (((words--) != 0)) {
            data = unchecked((ushort)(((unchecked((int)(cdc_ram[(int)(src_index)])) << 8) | unchecked((int)(cdc_ram[(int)((unchecked((int)(src_index)) + 1))])))));
            (word_ram[(int)(0)] + unchecked((int)(dst_index))).Cast<ushort>()[0] = data;
            src_index = unchecked((ushort)(((unchecked((int)(src_index)) + 2) & 16382)));
            dst_index = ((dst_index + unchecked((uint)(2))) & unchecked((uint)(131070)));
            }
        }
    }
    private void word_ram_1_dma_w(uint length)
    {
        unchecked
        {
            ushort data = default;
            uint words = (length >> 1);
            ushort src_index = unchecked((ushort)((unchecked((int)(cdc_dac.Word)) & 16382)));
            uint dst_index = unchecked((uint)(((unchecked((int)(scd_regs[(int)((10 >> 1))].Word)) << 3) & 131070)));
            scd_regs[(int)((10 >> 1))].Word += unchecked((ushort)((words >> 2)));
            cdc_dac.Word += unchecked((ushort)((words << 1)));
            while (((words--) != 0)) {
            data = unchecked((ushort)(((unchecked((int)(cdc_ram[(int)(src_index)])) << 8) | unchecked((int)(cdc_ram[(int)((unchecked((int)(src_index)) + 1))])))));
            (word_ram[(int)(1)] + unchecked((int)(dst_index))).Cast<ushort>()[0] = data;
            src_index = unchecked((ushort)(((unchecked((int)(src_index)) + 2) & 16382)));
            dst_index = ((dst_index + unchecked((uint)(2))) & unchecked((uint)(131070)));
            }
        }
    }
    private void word_ram_2M_dma_w(uint length)
    {
        unchecked
        {
            ushort data = default;
            uint words = (length >> 1);
            ushort src_index = unchecked((ushort)((unchecked((int)(cdc_dac.Word)) & 16382)));
            uint dst_index = unchecked((uint)(((unchecked((int)(scd_regs[(int)((10 >> 1))].Word)) << 3) & 262142)));
            scd_regs[(int)((10 >> 1))].Word += unchecked((ushort)((words >> 2)));
            cdc_dac.Word += unchecked((ushort)((words << 1)));
            while (((words--) != 0)) {
            data = unchecked((ushort)(((unchecked((int)(cdc_ram[(int)(src_index)])) << 8) | unchecked((int)(cdc_ram[(int)((unchecked((int)(src_index)) + 1))])))));
            (new ArrayPointer<byte>(word_ram_2M) + unchecked((int)(dst_index))).Cast<ushort>()[0] = data;
            src_index = unchecked((ushort)(((unchecked((int)(src_index)) + 2) & 16382)));
            dst_index = ((dst_index + unchecked((uint)(2))) & unchecked((uint)(262142)));
            }
        }
    }
    private uint dot_ram_0_read16(uint address)
    {
        unchecked
        {
            byte data = word_ram[(int)(0)][(int)((((address >> 1) & unchecked((uint)(131071))) ^ unchecked((uint)(1))))];
            return unchecked((uint)(((unchecked((int)(data)) & 15) | ((unchecked((int)(data)) << 4) & 3840))));
        }
    }
    private uint dot_ram_1_read16(uint address)
    {
        unchecked
        {
            byte data = word_ram[(int)(1)][(int)((((address >> 1) & unchecked((uint)(131071))) ^ unchecked((uint)(1))))];
            return unchecked((uint)(((unchecked((int)(data)) & 15) | ((unchecked((int)(data)) << 4) & 3840))));
        }
    }
    private void dot_ram_0_write16(uint address, uint data)
    {
        unchecked
        {
            byte prev = default;
            address = ((address >> 1) & unchecked((uint)(131071)));
            prev = word_ram[(int)(0)][(int)((address ^ unchecked((uint)(1))))];
            data = ((data & unchecked((uint)(15))) | ((data >> 4) & unchecked((uint)(240))));
            data = unchecked((uint)(gfx.lut_prio[(int)((unchecked((uint)(((((unchecked((int)(scd_regs[(int)((2 >> 1))].Word)) >> 3) & 3) * 65536) + (unchecked((int)(prev)) * 256)))) + data))]));
            word_ram[(int)(0)][(int)((address ^ unchecked((uint)(1))))] = unchecked((byte)((data & unchecked((uint)(255)))));
        }
    }
    private void dot_ram_1_write16(uint address, uint data)
    {
        unchecked
        {
            byte prev = default;
            address = ((address >> 1) & unchecked((uint)(131071)));
            prev = word_ram[(int)(1)][(int)((address ^ unchecked((uint)(1))))];
            data = ((data & unchecked((uint)(15))) | ((data >> 4) & unchecked((uint)(240))));
            data = unchecked((uint)(gfx.lut_prio[(int)((unchecked((uint)(((((unchecked((int)(scd_regs[(int)((2 >> 1))].Word)) >> 3) & 3) * 65536) + (unchecked((int)(prev)) * 256)))) + data))]));
            word_ram[(int)(1)][(int)((address ^ unchecked((uint)(1))))] = unchecked((byte)((data & unchecked((uint)(255)))));
        }
    }
    private uint dot_ram_0_read8(uint address)
    {
        unchecked
        {
            byte data = word_ram[(int)(0)][(int)((((address >> 1) & unchecked((uint)(131071))) ^ unchecked((uint)(1))))];
            if (((address & unchecked((uint)(1))) != 0)) {
            return unchecked((uint)((unchecked((int)(data)) & 15)));
            }
            return unchecked((uint)((unchecked((int)(data)) >> 4)));
        }
    }
    private uint dot_ram_1_read8(uint address)
    {
        unchecked
        {
            byte data = word_ram[(int)(1)][(int)((((address >> 1) & unchecked((uint)(131071))) ^ unchecked((uint)(1))))];
            if (((address & unchecked((uint)(1))) != 0)) {
            return unchecked((uint)((unchecked((int)(data)) & 15)));
            }
            return unchecked((uint)((unchecked((int)(data)) >> 4)));
        }
    }
    private void dot_ram_0_write8(uint address, uint data)
    {
        unchecked
        {
            byte prev = word_ram[(int)(0)][(int)((((address >> 1) & unchecked((uint)(131071))) ^ unchecked((uint)(1))))];
            if (((address & unchecked((uint)(1))) != 0)) {
            data = (unchecked((uint)((unchecked((int)(prev)) & 240))) | (data & unchecked((uint)(15))));
            } else {
            data = (unchecked((uint)((unchecked((int)(prev)) & 15))) | (data << 4));
            }
            data = unchecked((uint)(gfx.lut_prio[(int)((unchecked((uint)(((((unchecked((int)(scd_regs[(int)((2 >> 1))].Word)) >> 3) & 3) * 65536) + (unchecked((int)(prev)) * 256)))) + data))]));
            word_ram[(int)(0)][(int)((((address >> 1) & unchecked((uint)(131071))) ^ unchecked((uint)(1))))] = unchecked((byte)((data & unchecked((uint)(255)))));
        }
    }
    private void dot_ram_1_write8(uint address, uint data)
    {
        unchecked
        {
            byte prev = word_ram[(int)(1)][(int)((((address >> 1) & unchecked((uint)(131071))) ^ unchecked((uint)(1))))];
            if (((address & unchecked((uint)(1))) != 0)) {
            data = (unchecked((uint)((unchecked((int)(prev)) & 240))) | (data & unchecked((uint)(15))));
            } else {
            data = (unchecked((uint)((unchecked((int)(prev)) & 15))) | (data << 4));
            }
            data = unchecked((uint)(gfx.lut_prio[(int)((unchecked((uint)(((((unchecked((int)(scd_regs[(int)((2 >> 1))].Word)) >> 3) & 3) * 65536) + (unchecked((int)(prev)) * 256)))) + data))]));
            word_ram[(int)(1)][(int)((((address >> 1) & unchecked((uint)(131071))) ^ unchecked((uint)(1))))] = unchecked((byte)((data & unchecked((uint)(255)))));
        }
    }
    private uint cell_ram_0_read16(uint address)
    {
        unchecked
        {
            address = (unchecked((uint)(gfx.lut_offset[(int)(((address >> 2) & unchecked((uint)(32767))))])) | (address & unchecked((uint)(65538))));
            return unchecked((uint)((word_ram[(int)(0)] + unchecked((int)(address))).Cast<ushort>()[0]));
        }
    }
    private uint cell_ram_1_read16(uint address)
    {
        unchecked
        {
            address = (unchecked((uint)(gfx.lut_offset[(int)(((address >> 2) & unchecked((uint)(32767))))])) | (address & unchecked((uint)(65538))));
            return unchecked((uint)((word_ram[(int)(1)] + unchecked((int)(address))).Cast<ushort>()[0]));
        }
    }
    private void cell_ram_0_write16(uint address, uint data)
    {
        unchecked
        {
            address = (unchecked((uint)(gfx.lut_offset[(int)(((address >> 2) & unchecked((uint)(32767))))])) | (address & unchecked((uint)(65538))));
            (word_ram[(int)(0)] + unchecked((int)(address))).Cast<ushort>()[0] = unchecked((ushort)(data));
        }
    }
    private void cell_ram_1_write16(uint address, uint data)
    {
        unchecked
        {
            address = (unchecked((uint)(gfx.lut_offset[(int)(((address >> 2) & unchecked((uint)(32767))))])) | (address & unchecked((uint)(65538))));
            (word_ram[(int)(1)] + unchecked((int)(address))).Cast<ushort>()[0] = unchecked((ushort)(data));
        }
    }
    private uint cell_ram_0_read8(uint address)
    {
        unchecked
        {
            address = (unchecked((uint)(gfx.lut_offset[(int)(((address >> 2) & unchecked((uint)(32767))))])) | (address & unchecked((uint)(65539))));
            return unchecked((uint)(word_ram[(int)(0)][(int)((address ^ unchecked((uint)(1))))]));
        }
    }
    private uint cell_ram_1_read8(uint address)
    {
        unchecked
        {
            address = (unchecked((uint)(gfx.lut_offset[(int)(((address >> 2) & unchecked((uint)(32767))))])) | (address & unchecked((uint)(65539))));
            return unchecked((uint)(word_ram[(int)(1)][(int)((address ^ unchecked((uint)(1))))]));
        }
    }
    private void cell_ram_0_write8(uint address, uint data)
    {
        unchecked
        {
            address = (unchecked((uint)(gfx.lut_offset[(int)(((address >> 2) & unchecked((uint)(32767))))])) | (address & unchecked((uint)(65539))));
            word_ram[(int)(0)][(int)((address ^ unchecked((uint)(1))))] = unchecked((byte)((data & unchecked((uint)(255)))));
        }
    }
    private void cell_ram_1_write8(uint address, uint data)
    {
        unchecked
        {
            address = (unchecked((uint)(gfx.lut_offset[(int)(((address >> 2) & unchecked((uint)(32767))))])) | (address & unchecked((uint)(65539))));
            word_ram[(int)(1)][(int)((address ^ unchecked((uint)(1))))] = unchecked((byte)((data & unchecked((uint)(255)))));
        }
    }
    private void gfx_init()
    {
        unchecked
        {
            int i = default;
            int j = default;
            ushort offset = default;
            byte mask = default;
            byte row = default;
            byte col = default;
            byte temp = default;
            ClearState();
            for (i = 0; (i < 16384); i++) {
            offset = unchecked((ushort)(((i & 7) << 8)));
            offset = unchecked((ushort)((unchecked((int)(offset)) | (((i >> 8) & 63) << 2))));
            offset = unchecked((ushort)((unchecked((int)(offset)) | (((i >> 3) & 31) << 11))));
            gfx.lut_offset[(int)(i)] = offset;
            }
            for (i = 16384; (i < 24576); i++) {
            offset = unchecked((ushort)(((i & 7) << 8)));
            offset = unchecked((ushort)((unchecked((int)(offset)) | (((i >> 7) & 63) << 2))));
            offset = unchecked((ushort)((unchecked((int)(offset)) | (((i >> 3) & 15) << 11))));
            gfx.lut_offset[(int)(i)] = offset;
            }
            for (i = 24576; (i < 28672); i++) {
            offset = unchecked((ushort)(((i & 7) << 8)));
            offset = unchecked((ushort)((unchecked((int)(offset)) | (((i >> 6) & 63) << 2))));
            offset = unchecked((ushort)((unchecked((int)(offset)) | (((i >> 3) & 7) << 11))));
            gfx.lut_offset[(int)(i)] = unchecked((ushort)((unchecked((int)(offset)) | 32768)));
            }
            for (i = 28672; (i < 30720); i++) {
            offset = unchecked((ushort)(((i & 7) << 8)));
            offset = unchecked((ushort)((unchecked((int)(offset)) | (((i >> 5) & 63) << 2))));
            offset = unchecked((ushort)((unchecked((int)(offset)) | (((i >> 3) & 3) << 11))));
            gfx.lut_offset[(int)(i)] = unchecked((ushort)((unchecked((int)(offset)) | 49152)));
            }
            for (i = 30720; (i < 32768); i++) {
            offset = unchecked((ushort)(((i & 7) << 8)));
            offset = unchecked((ushort)((unchecked((int)(offset)) | (((i >> 5) & 63) << 2))));
            offset = unchecked((ushort)((unchecked((int)(offset)) | (((i >> 3) & 3) << 11))));
            gfx.lut_offset[(int)(i)] = unchecked((ushort)((unchecked((int)(offset)) | 57344)));
            }
            for (i = 0; (i < 256); i++) {
            for (j = 0; (j < 256); j++) {
            gfx.lut_prio[(int)((((0 * 65536) + (i * 256)) + j))] = unchecked((byte)(j));
            gfx.lut_prio[(int)((((1 * 65536) + (i * 256)) + j))] = unchecked((byte)(((((i & 15) != 0) ? (i & 15) : (j & 15)) | (((i & 240) != 0) ? (i & 240) : (j & 240)))));
            gfx.lut_prio[(int)((((2 * 65536) + (i * 256)) + j))] = unchecked((byte)(((((j & 15) != 0) ? (j & 15) : (i & 15)) | (((j & 240) != 0) ? (j & 240) : (i & 240)))));
            gfx.lut_prio[(int)((((3 * 65536) + (i * 256)) + j))] = unchecked((byte)(i));
            }
            }
            for (i = 0; (i < 256); i++) {
            mask = unchecked((byte)((((i & 8) != 0) ? 3 : 1)));
            row = unchecked((byte)(((i >> 6) & unchecked((int)(mask)))));
            col = unchecked((byte)(((i >> 4) & unchecked((int)(mask)))));
            if (((i & 4) != 0)) {
            col = unchecked((byte)((unchecked((int)(col)) ^ unchecked((int)(mask)))));
            }
            if (((i & 2) != 0)) {
            col = unchecked((byte)((unchecked((int)(col)) ^ unchecked((int)(mask)))));
            row = unchecked((byte)((unchecked((int)(row)) ^ unchecked((int)(mask)))));
            }
            if (((i & 1) != 0)) {
            temp = col;
            col = unchecked((byte)((unchecked((int)(row)) ^ unchecked((int)(mask)))));
            row = temp;
            }
            gfx.lut_cell[(int)(i)] = unchecked((byte)((unchecked((int)(row)) + (unchecked((int)(col)) * (unchecked((int)(mask)) + 1)))));
            }
            for (i = 0; (i < 512); i++) {
            row = unchecked((byte)(((i >> 6) & 7)));
            col = unchecked((byte)(((i >> 3) & 7)));
            if (((i & 4) != 0)) {
            col = unchecked((byte)((unchecked((int)(col)) ^ 7)));
            }
            if (((i & 2) != 0)) {
            col = unchecked((byte)((unchecked((int)(col)) ^ 7)));
            row = unchecked((byte)((unchecked((int)(row)) ^ 7)));
            }
            if (((i & 1) != 0)) {
            temp = col;
            col = unchecked((byte)((unchecked((int)(row)) ^ 7)));
            row = temp;
            }
            gfx.lut_pixel[(int)(i)] = unchecked((byte)((unchecked((int)(col)) + (unchecked((int)(row)) * 8))));
            }
        }
    }
    private void gfx_reset()
    {
        unchecked
        {
            gfx.cycles = unchecked((uint)(0));
        }
    }
    private void gfx_render(uint bufferIndex, uint width)
    {
        unchecked
        {
            byte pixel_in = default;
            byte pixel_out = default;
            ushort stamp_data = default;
            uint stamp_index = default;
            uint stamp_mask = unchecked((uint)((((unchecked((int)(scd_regs[(int)((88 >> 1))].Low)) & 2) != 0) ? 2044 : 2047)));
            uint xpos = unchecked((uint)((unchecked((int)((gfx.tracePtr++)[0])) << 8)));
            uint ypos = unchecked((uint)((unchecked((int)((gfx.tracePtr++)[0])) << 8)));
            uint xoffset = unchecked((uint)(unchecked((short)((gfx.tracePtr++)[0]))));
            uint yoffset = unchecked((uint)(unchecked((short)((gfx.tracePtr++)[0]))));
            if (gfx.tracePtr.Same((new ArrayPointer<byte>(word_ram_2M) + 262144).Cast<ushort>())) {
            gfx.tracePtr = new ArrayPointer<byte>(word_ram_2M).Cast<ushort>();
            }
            while (((width--) != 0)) {
            if (((unchecked((int)(scd_regs[(int)((88 >> 1))].Low)) & 1) != 0)) {
            xpos &= gfx.dotMask;
            ypos &= gfx.dotMask;
            } else {
            xpos &= unchecked((uint)(16777215));
            ypos &= unchecked((uint)(16777215));
            }
            if ((((xpos | ypos) & (~gfx.dotMask)) != 0)) {
            pixel_out = unchecked((byte)(0));
            } else {
            stamp_data = gfx.mapPtr[(int)(((xpos >> unchecked((int)(gfx.stampShift))) | ((ypos >> unchecked((int)(gfx.stampShift))) << unchecked((int)(gfx.mapShift)))))];
            stamp_index = ((unchecked((uint)(stamp_data)) & stamp_mask) << 8);
            if ((stamp_index != 0)) {
            stamp_data = unchecked((ushort)(((unchecked((int)(stamp_data)) >> 13) & 7)));
            stamp_index |= unchecked((uint)((unchecked((int)(gfx.lut_cell[(int)(((unchecked((uint)((unchecked((int)(stamp_data)) | ((unchecked((int)(scd_regs[(int)((88 >> 1))].Low)) & 2) << 2)))) | ((ypos >> 8) & unchecked((uint)(192)))) | ((xpos >> 10) & unchecked((uint)(48)))))])) << 6)));
            stamp_index |= unchecked((uint)(gfx.lut_pixel[(int)(((unchecked((uint)(stamp_data)) | ((xpos >> 8) & unchecked((uint)(56)))) | ((ypos >> 5) & unchecked((uint)(448)))))]));
            pixel_out = word_ram_2M[(int)(((stamp_index >> 1) ^ unchecked((uint)(1))))];
            if (((stamp_index & unchecked((uint)(1))) != 0)) {
            pixel_out &= unchecked((byte)(15));
            } else {
            pixel_out >>= 4;
            }
            } else {
            pixel_out = unchecked((byte)(0));
            }
            }
            pixel_in = word_ram_2M[(int)((((bufferIndex >> 1) & unchecked((uint)(262143))) ^ unchecked((uint)(1))))];
            if (((bufferIndex & unchecked((uint)(1))) != 0)) {
            pixel_out |= unchecked((byte)((unchecked((int)(pixel_in)) & 240)));
            } else {
            pixel_out = unchecked((byte)(((unchecked((int)(pixel_out)) << 4) | (unchecked((int)(pixel_in)) & 15))));
            }
            pixel_out = gfx.lut_prio[(int)((((((unchecked((int)(scd_regs[(int)((2 >> 1))].Word)) >> 3) & 3) * 65536) + (unchecked((int)(pixel_in)) * 256)) + unchecked((int)(pixel_out))))];
            word_ram_2M[(int)((((bufferIndex >> 1) & unchecked((uint)(262143))) ^ unchecked((uint)(1))))] = unchecked((byte)((unchecked((int)(pixel_out)) & 255)));
            if (((bufferIndex & unchecked((uint)(7))) != unchecked((uint)(7)))) {
            bufferIndex++;
            } else {
            bufferIndex += unchecked((uint)(gfx.bufferOffset));
            }
            xpos += xoffset;
            ypos += yoffset;
            }
        }
    }
    private void gfx_start(uint @base, int cycles)
    {
        unchecked
        {
            uint mask = default;
            gfx.tracePtr = (new ArrayPointer<byte>(word_ram_2M) + unchecked((int)(((@base << 2) & unchecked((uint)(262136)))))).Cast<ushort>();
            switch (((unchecked((int)(scd_regs[(int)((88 >> 1))].Low)) >> 1) & 3)) {
            case 0:
            gfx.dotMask = unchecked((uint)(524287));
            gfx.stampShift = unchecked((byte)((11 + 4)));
            gfx.mapShift = unchecked((byte)(4));
            mask = unchecked((uint)(261632));
            break;
            case 1:
            gfx.dotMask = unchecked((uint)(524287));
            gfx.stampShift = unchecked((byte)((11 + 5)));
            gfx.mapShift = unchecked((byte)(3));
            mask = unchecked((uint)(262016));
            break;
            case 2:
            gfx.dotMask = unchecked((uint)(8388607));
            gfx.stampShift = unchecked((byte)((11 + 4)));
            gfx.mapShift = unchecked((byte)(8));
            mask = unchecked((uint)(131072));
            break;
            case 3:
            gfx.dotMask = unchecked((uint)(8388607));
            gfx.stampShift = unchecked((byte)((11 + 5)));
            gfx.mapShift = unchecked((byte)(7));
            mask = unchecked((uint)(229376));
            break;
            }
            gfx.mapPtr = (new ArrayPointer<byte>(word_ram_2M) + unchecked((int)((unchecked((uint)((unchecked((int)(scd_regs[(int)((90 >> 1))].Word)) << 2))) & mask)))).Cast<ushort>();
            gfx.bufferOffset = unchecked((ushort)(((((unchecked((int)(scd_regs[(int)((92 >> 1))].Low)) & 31) + 1) << 6) - 7)));
            gfx.bufferStart = unchecked((uint)(((unchecked((int)(scd_regs[(int)((94 >> 1))].Word)) << 3) & 524224)));
            gfx.bufferStart += unchecked((uint)((unchecked((int)(scd_regs[(int)((96 >> 1))].Low)) & 63)));
            gfx.cycles = unchecked((uint)(cycles));
            gfx.cyclesPerLine = unchecked((uint)(((4 * 3) * ((4 + (2 * (unchecked((int)(scd_regs[(int)((98 >> 1))].Word)) & 511))) + ((((unchecked((int)(scd_regs[(int)((98 >> 1))].Word)) & 511) + (unchecked((int)(scd_regs[(int)((96 >> 1))].Low)) & 3)) + 3) >> 2)))));
            scd_regs[(int)((88 >> 1))].High = unchecked((byte)(128));
        }
    }
    private void gfx_update(int cycles)
    {
        unchecked
        {
            if (((unchecked((int)(scd_regs[(int)((2 >> 1))].Low)) & 5) != 1)) {
            cycles -= unchecked((int)(gfx.cycles));
            if ((cycles > 0)) {
            uint lines = (((unchecked((uint)(cycles)) + gfx.cyclesPerLine) - unchecked((uint)(1))) / gfx.cyclesPerLine);
            if ((lines < unchecked((uint)(scd_regs[(int)((100 >> 1))].Low)))) {
            scd_regs[(int)((100 >> 1))].Low -= unchecked((byte)(lines));
            gfx.cycles += (lines * gfx.cyclesPerLine);
            } else {
            lines = unchecked((uint)(scd_regs[(int)((100 >> 1))].Low));
            scd_regs[(int)((100 >> 1))].Low = unchecked((byte)(0));
            gfx.cycles += (lines * gfx.cyclesPerLine);
            scd_regs[(int)((88 >> 1))].High = unchecked((byte)(0));
            if (((sub_stopped & unchecked((uint)((1 << 8)))) != 0)) {
            if ((unchecked((uint)(sub_cycles)) < gfx.cycles)) {
            sub_cycles = unchecked((int)(gfx.cycles));
            }
            sub_stopped = unchecked((uint)(0));
            }
            if (((unchecked((int)(scd_regs[(int)((50 >> 1))].Low)) & 2) != 0)) {
            pending |= unchecked((byte)((1 << 1)));
            s68k_update_irq(unchecked((uint)(((unchecked((int)(pending)) & unchecked((int)(scd_regs[(int)((50 >> 1))].Low))) >> 1))));
            }
            }
            while (((lines--) != 0)) {
            gfx_render(gfx.bufferStart, unchecked((uint)((unchecked((int)(scd_regs[(int)((98 >> 1))].Word)) & 511))));
            gfx.bufferStart += unchecked((uint)(8));
            }
            }
            } else {
            gfx.cycles = unchecked((uint)(cycles));
            }
        }
    }
}

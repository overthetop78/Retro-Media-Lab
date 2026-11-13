using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed partial class Cdc
{
    private void cdc_init()
    {
        unchecked
        {
            ClearState();
            if (((unchecked((int)(hardware_type)) == 2) || (unchecked((int)(hardware_type)) == 3))) {
            cdc.ar_mask = unchecked((byte)(31));
            } else {
            cdc.ar_mask = unchecked((byte)(15));
            }
        }
    }
    private void cdc_reset()
    {
        unchecked
        {
            scd_regs[(int)((4 >> 1))].Low = unchecked((byte)(0));
            cdc.ifstat = unchecked((byte)(255));
            cdc.ifctrl = unchecked((byte)(0));
            cdc.ctrl[(int)(0)] = unchecked((byte)(0));
            cdc.ctrl[(int)(1)] = unchecked((byte)(0));
            cdc.stat[(int)(0)] = unchecked((byte)(0));
            cdc.stat[(int)(1)] = unchecked((byte)(0));
            cdc.stat[(int)(2)] = unchecked((byte)(0));
            cdc.stat[(int)(3)] = unchecked((byte)(128));
            new CPointer<byte>(cdc.head, (int)(0) * 4)[(int)(0)] = unchecked((byte)(0));
            new CPointer<byte>(cdc.head, (int)(0) * 4)[(int)(1)] = unchecked((byte)(0));
            new CPointer<byte>(cdc.head, (int)(0) * 4)[(int)(2)] = unchecked((byte)(0));
            new CPointer<byte>(cdc.head, (int)(0) * 4)[(int)(3)] = unchecked((byte)(1));
            new CPointer<byte>(cdc.head, (int)(1) * 4)[(int)(0)] = unchecked((byte)(0));
            new CPointer<byte>(cdc.head, (int)(1) * 4)[(int)(1)] = unchecked((byte)(0));
            new CPointer<byte>(cdc.head, (int)(1) * 4)[(int)(2)] = unchecked((byte)(0));
            new CPointer<byte>(cdc.head, (int)(1) * 4)[(int)(3)] = unchecked((byte)(0));
            cdc.cycles[(int)(0)] = (cdc.cycles[(int)(1)] = 0);
            cdc.dma_w = (cdc.halted_dma_w = null);
            cdc.irq = unchecked((byte)(0));
            if (((unchecked((int)(pending)) & (1 << 5)) != 0)) {
            pending &= unchecked((byte)((~(1 << 5))));
            s68k_update_irq(unchecked((uint)(((unchecked((int)(pending)) & unchecked((int)(scd_regs[(int)((50 >> 1))].Low))) >> 1))));
            }
        }
    }
    private void cdc_dma_init()
    {
        unchecked
        {
            if (((unchecked((int)(cdc.ifstat)) & 2) != 0)) return;
            cdc.dma_w = (cdc.halted_dma_w = null);
            switch ((unchecked((int)(scd_regs[(int)((4 >> 1))].High)) & 7)) {
            case 2:
            case 3:
            {
            scd_regs[(int)((8 >> 1))].Word = unchecked((ushort)(((unchecked((int)(cdc.ram[(int)((unchecked((int)(cdc.dac.Word)) & 16382))])) << 8) | unchecked((int)(cdc.ram[(int)(((unchecked((int)(cdc.dac.Word)) & 16382) + 1))])))));
            scd_regs[(int)((4 >> 1))].High |= unchecked((byte)(64));
            cdc.dac.Word += unchecked((ushort)(2));
            cdc.dbc.Word -= unchecked((ushort)(2));
            if ((unchecked((int)(unchecked((short)(cdc.dbc.Word)))) < 0)) {
            cdc.dbc.Word = unchecked((ushort)(65535));
            cdc.ifstat |= unchecked((byte)((8 | 2)));
            cdc.ifstat &= unchecked((byte)((~64)));
            if (((unchecked((int)(cdc.ifctrl)) & 64) != 0)) {
            if ((((unchecked((int)(cdc.irq)) & 32) != 0) && (cdc.cycles[(int)(0)] > cdc.cycles[(int)(1)]))) {
            cdc.ifstat |= unchecked((byte)(32));
            cdc.irq &= unchecked((byte)((~32)));
            }
            if (((!(cdc.irq != 0)) && ((unchecked((int)(scd_regs[(int)((50 >> 1))].Low)) & 32) != 0))) {
            pending |= unchecked((byte)((1 << 5)));
            s68k_update_irq(unchecked((uint)(((unchecked((int)(pending)) & unchecked((int)(scd_regs[(int)((50 >> 1))].Low))) >> 1))));
            }
            cdc.irq |= unchecked((byte)(64));
            }
            scd_regs[(int)((4 >> 1))].High |= unchecked((byte)(128));
            }
            break;
            }
            case 4:
            {
            cdc.dma_w = pcm_ram_dma_w;
            break;
            }
            case 5:
            {
            if (((unchecked((int)(scd_regs[(int)(0)].Low)) & 2) != 0)) {
            cdc.halted_dma_w = prg_ram_dma_w;
            } else {
            cdc.dma_w = prg_ram_dma_w;
            }
            break;
            }
            case 7:
            {
            if (((unchecked((int)(scd_regs[(int)((2 >> 1))].Low)) & 4) != 0)) {
            if (((unchecked((int)(scd_regs[(int)((2 >> 1))].Low)) & 1) != 0)) {
            cdc.dma_w = word_ram_0_dma_w;
            } else {
            cdc.dma_w = word_ram_1_dma_w;
            }
            } else {
            if (((unchecked((int)(scd_regs[(int)((2 >> 1))].Low)) & 1) != 0)) {
            cdc.halted_dma_w = word_ram_2M_dma_w;
            } else {
            cdc.dma_w = word_ram_2M_dma_w;
            }
            }
            break;
            }
            default:
            {
            break;
            }
            }
        }
    }
    private void cdc_dma_update(uint cycles)
    {
        unchecked
        {
            int dma_bytes = unchecked((int)(((((cycles - unchecked((uint)(cdc.cycles[(int)(0)]))) + unchecked((uint)(16))) - unchecked((uint)(1))) / unchecked((uint)(16)))));
            dma_bytes = ((dma_bytes / 8) * 8);
            if ((unchecked((int)(cdc.dbc.Word)) < dma_bytes)) {
            cdc.dma_w(unchecked((uint)((unchecked((int)(cdc.dbc.Word)) + 1))));
            cdc.cycles[(int)(0)] += ((unchecked((int)(cdc.dbc.Word)) + 1) * 16);
            cdc.dbc.Word = unchecked((ushort)(65535));
            cdc.ifstat |= unchecked((byte)((8 | 2)));
            cdc.ifstat &= unchecked((byte)((~64)));
            if (((unchecked((int)(cdc.ifctrl)) & 64) != 0)) {
            if ((((unchecked((int)(cdc.irq)) & 32) != 0) && (cdc.cycles[(int)(0)] > cdc.cycles[(int)(1)]))) {
            cdc.ifstat |= unchecked((byte)(32));
            cdc.irq &= unchecked((byte)((~32)));
            }
            if (((!(cdc.irq != 0)) && ((unchecked((int)(scd_regs[(int)((50 >> 1))].Low)) & 32) != 0))) {
            pending |= unchecked((byte)((1 << 5)));
            s68k_update_irq(unchecked((uint)(((unchecked((int)(pending)) & unchecked((int)(scd_regs[(int)((50 >> 1))].Low))) >> 1))));
            }
            cdc.irq |= unchecked((byte)(64));
            }
            scd_regs[(int)((4 >> 1))].High = unchecked((byte)(((unchecked((int)(scd_regs[(int)((4 >> 1))].High)) & 7) | 128)));
            if (((sub_stopped & unchecked((uint)((1 << 4)))) != 0)) {
            if ((sub_cycles < cdc.cycles[(int)(0)])) {
            sub_cycles = cdc.cycles[(int)(0)];
            }
            sub_stopped = unchecked((uint)(0));
            }
            cdc.dma_w = (cdc.halted_dma_w = null);
            } else if ((dma_bytes > 0)) {
            cdc.dma_w(unchecked((uint)(dma_bytes)));
            cdc.dbc.Word -= unchecked((ushort)(dma_bytes));
            cdc.cycles[(int)(0)] += (dma_bytes * 16);
            }
        }
    }
    private void cdc_decoder_update(uint header)
    {
        unchecked
        {
            if (((unchecked((int)(cdc.ctrl[(int)(0)])) & 128) != 0)) {
            new CPointer<byte>(cdc.head, (int)(0) * 4).Cast<uint>()[0] = header;
            cdc.stat[(int)(3)] = unchecked((byte)(0));
            cdc.ifstat &= unchecked((byte)((~32)));
            cdc.cycles[(int)(1)] = (sub_cycles + 267000);
            if (((unchecked((int)(cdc.ifctrl)) & 32) != 0)) {
            if (((!((unchecked((int)(cdc.irq)) & 64) != 0)) && ((unchecked((int)(scd_regs[(int)((50 >> 1))].Low)) & 32) != 0))) {
            pending |= unchecked((byte)((1 << 5)));
            s68k_update_irq(unchecked((uint)(((unchecked((int)(pending)) & unchecked((int)(scd_regs[(int)((50 >> 1))].Low))) >> 1))));
            }
            cdc.irq |= unchecked((byte)(32));
            }
            if (((unchecked((int)(cdc.ctrl[(int)(0)])) & 4) != 0)) {
            int offset = default;
            cdc.pt.Word += unchecked((ushort)(2352));
            cdc.wa.Word += unchecked((ushort)(2352));
            offset = (unchecked((int)(cdc.pt.Word)) & 16383);
            (new CPointer<byte>(cdc.ram) + offset).Cast<uint>()[0] = header;
            offset += 4;
            if ((unchecked((int)(new CPointer<byte>(cdc.head, (int)(0) * 4)[(int)(3)])) == 1)) {
            cdd_read_data((new CPointer<byte>(cdc.ram) + offset), default(CPointer<byte>));
            offset += 2048;
            } else {
            if (((unchecked((int)(cdc.ctrl[(int)(1)])) & 8) != 0)) {
            cdd_read_data(((new CPointer<byte>(cdc.ram) + offset) + 8), new CPointer<byte>(cdc.head, (int)(1) * 4));
            (new CPointer<byte>(cdc.ram) + offset).Cast<uint>()[0] = new CPointer<byte>(cdc.head, (int)(1) * 4).Cast<uint>()[0];
            ((new CPointer<byte>(cdc.ram) + offset) + 4).Cast<uint>()[0] = new CPointer<byte>(cdc.head, (int)(1) * 4).Cast<uint>()[0];
            offset += 2336;
            } else {
            cdd_read_data((new CPointer<byte>(cdc.ram) + offset), new CPointer<byte>(cdc.head, (int)(1) * 4));
            offset += 2328;
            }
            if (((unchecked((int)(cdc.ctrl[(int)(0)])) & 16) != 0)) {
            cdc.stat[(int)(2)] = unchecked((byte)(((unchecked((int)(cdc.ctrl[(int)(1)])) & 8) | ((unchecked((int)(new CPointer<byte>(cdc.head, (int)(1) * 4)[(int)(2)])) & 32) >> 3))));
            }
            }
            if ((offset > 16384)) {
            CopyMemory(new CPointer<byte>(cdc.ram), (new CPointer<byte>(cdc.ram) + 16384), (offset - 16384));
            }
            }
            }
        }
    }
    private void cdc_reg_w(byte data)
    {
        unchecked
        {
            switch (scd_regs[(int)((4 >> 1))].Low) {
            case 1:
            {
            byte prev_irq = cdc.irq;
            if ((sub_cycles > cdc.cycles[(int)(1)])) {
            cdc.ifstat |= unchecked((byte)(32));
            prev_irq &= unchecked((byte)((~32)));
            }
            cdc.irq = unchecked((byte)((((~unchecked((int)(cdc.ifstat))) & unchecked((int)(data))) & (64 | 32))));
            if ((((cdc.irq != 0) && (!(prev_irq != 0))) && ((unchecked((int)(scd_regs[(int)((50 >> 1))].Low)) & 32) != 0))) {
            pending |= unchecked((byte)((1 << 5)));
            s68k_update_irq(unchecked((uint)(((unchecked((int)(pending)) & unchecked((int)(scd_regs[(int)((50 >> 1))].Low))) >> 1))));
            }
            if ((!((unchecked((int)(data)) & 2) != 0))) {
            cdc.ifstat |= unchecked((byte)((8 | 2)));
            cdc.dma_w = (cdc.halted_dma_w = null);
            }
            cdc.ifctrl = data;
            break;
            }
            case 2:
            cdc.dbc.Low = data;
            break;
            case 3:
            cdc.dbc.High = unchecked((byte)((unchecked((int)(data)) & 15)));
            break;
            case 4:
            cdc.dac.Low = data;
            break;
            case 5:
            cdc.dac.High = data;
            break;
            case 6:
            {
            if (((unchecked((int)(cdc.ifctrl)) & 2) != 0)) {
            cdc.ifstat &= unchecked((byte)((~(8 | 2))));
            scd_regs[(int)((4 >> 1))].High &= unchecked((byte)(7));
            cdc_dma_init();
            cdc.cycles[(int)(0)] = sub_cycles;
            }
            break;
            }
            case 7:
            {
            cdc.ifstat |= unchecked((byte)(64));
            cdc.irq &= unchecked((byte)((~64)));
            break;
            }
            case 8:
            cdc.wa.Low = data;
            break;
            case 9:
            cdc.wa.High = data;
            break;
            case 10:
            {
            cdc.stat[(int)(0)] = unchecked((byte)((unchecked((int)(data)) & 128)));
            if ((!((unchecked((int)(data)) & 128) != 0))) {
            cdc.ifstat |= unchecked((byte)(32));
            cdc.irq &= unchecked((byte)((~32)));
            }
            if (((unchecked((int)(data)) & 16) != 0)) {
            cdc.stat[(int)(2)] = unchecked((byte)(((unchecked((int)(cdc.ctrl[(int)(1)])) & 8) | ((unchecked((int)(new CPointer<byte>(cdc.head, (int)(1) * 4)[(int)(2)])) & 32) >> 3))));
            } else {
            cdc.stat[(int)(2)] = unchecked((byte)((unchecked((int)(cdc.ctrl[(int)(1)])) & (8 | 4))));
            }
            cdc.ctrl[(int)(0)] = data;
            break;
            }
            case 11:
            {
            if (((unchecked((int)(cdc.ctrl[(int)(0)])) & 16) != 0)) {
            cdc.stat[(int)(2)] = unchecked((byte)(((unchecked((int)(data)) & 8) | ((unchecked((int)(new CPointer<byte>(cdc.head, (int)(1) * 4)[(int)(2)])) & 32) >> 3))));
            } else {
            cdc.stat[(int)(2)] = unchecked((byte)((unchecked((int)(data)) & (8 | 4))));
            }
            cdc.ctrl[(int)(1)] = data;
            break;
            }
            case 12:
            cdc.pt.Low = data;
            break;
            case 13:
            cdc.pt.High = data;
            break;
            case 15:
            cdc_reset();
            break;
            default:
            break;
            }
            if ((scd_regs[(int)((4 >> 1))].Low != 0)) {
            scd_regs[(int)((4 >> 1))].Low = unchecked((byte)(((unchecked((int)(scd_regs[(int)((4 >> 1))].Low)) + 1) & unchecked((int)(cdc.ar_mask)))));
            }
        }
    }
    private byte cdc_reg_r()
    {
        unchecked
        {
            byte data = default;
            switch (scd_regs[(int)((4 >> 1))].Low) {
            case 1:
            {
            if ((sub_cycles > cdc.cycles[(int)(1)])) {
            cdc.ifstat |= unchecked((byte)(32));
            cdc.irq &= unchecked((byte)((~32)));
            }
            data = cdc.ifstat;
            break;
            }
            case 2:
            {
            data = cdc.dbc.Low;
            break;
            }
            case 3:
            {
            data = cdc.dbc.High;
            break;
            }
            case 4:
            {
            data = new CPointer<byte>(cdc.head, (int)((unchecked((int)(cdc.ctrl[(int)(1)])) & 1)) * 4)[(int)(0)];
            break;
            }
            case 5:
            {
            data = new CPointer<byte>(cdc.head, (int)((unchecked((int)(cdc.ctrl[(int)(1)])) & 1)) * 4)[(int)(1)];
            break;
            }
            case 6:
            {
            data = new CPointer<byte>(cdc.head, (int)((unchecked((int)(cdc.ctrl[(int)(1)])) & 1)) * 4)[(int)(2)];
            break;
            }
            case 7:
            {
            data = new CPointer<byte>(cdc.head, (int)((unchecked((int)(cdc.ctrl[(int)(1)])) & 1)) * 4)[(int)(3)];
            break;
            }
            case 8:
            {
            data = cdc.pt.Low;
            break;
            }
            case 9:
            {
            data = cdc.pt.High;
            break;
            }
            case 10:
            {
            data = cdc.wa.Low;
            break;
            }
            case 11:
            {
            data = cdc.wa.High;
            break;
            }
            case 12:
            {
            data = cdc.stat[(int)(0)];
            break;
            }
            case 13:
            {
            data = unchecked((byte)(0));
            break;
            }
            case 14:
            {
            data = cdc.stat[(int)(2)];
            break;
            }
            case 15:
            {
            data = cdc.stat[(int)(3)];
            cdc.stat[(int)(3)] = unchecked((byte)(128));
            cdc.ifstat |= unchecked((byte)(32));
            cdc.irq &= unchecked((byte)((~32)));
            break;
            }
            default:
            {
            data = unchecked((byte)(255));
            break;
            }
            }
            if ((scd_regs[(int)((4 >> 1))].Low != 0)) {
            scd_regs[(int)((4 >> 1))].Low = unchecked((byte)(((unchecked((int)(scd_regs[(int)((4 >> 1))].Low)) + 1) & unchecked((int)(cdc.ar_mask)))));
            }
            return data;
        }
    }
    private ushort cdc_host_r(byte cpu_access)
    {
        unchecked
        {
            ushort data = scd_regs[(int)((8 >> 1))].Word;
            if (((unchecked((int)(scd_regs[(int)((4 >> 1))].High)) & 71) == unchecked((int)(cpu_access)))) {
            if (((unchecked((int)(scd_regs[(int)((4 >> 1))].High)) & 128) != 0)) {
            scd_regs[(int)((4 >> 1))].High &= unchecked((byte)((~64)));
            } else {
            scd_regs[(int)((8 >> 1))].Word = unchecked((ushort)(((unchecked((int)(cdc.ram[(int)((unchecked((int)(cdc.dac.Word)) & 16382))])) << 8) | unchecked((int)(cdc.ram[(int)(((unchecked((int)(cdc.dac.Word)) & 16382) + 1))])))));
            cdc.dac.Word += unchecked((ushort)(2));
            cdc.dbc.Word -= unchecked((ushort)(2));
            if ((unchecked((int)(unchecked((short)(cdc.dbc.Word)))) < 0)) {
            cdc.dbc.Word = unchecked((ushort)(65535));
            cdc.ifstat |= unchecked((byte)((8 | 2)));
            cdc.ifstat &= unchecked((byte)((~64)));
            if (((unchecked((int)(cdc.ifctrl)) & 64) != 0)) {
            if ((((unchecked((int)(cdc.irq)) & 32) != 0) && (cdc.cycles[(int)(0)] > cdc.cycles[(int)(1)]))) {
            cdc.ifstat |= unchecked((byte)(32));
            cdc.irq &= unchecked((byte)((~32)));
            }
            if (((!(cdc.irq != 0)) && ((unchecked((int)(scd_regs[(int)((50 >> 1))].Low)) & 32) != 0))) {
            pending |= unchecked((byte)((1 << 5)));
            s68k_update_irq(unchecked((uint)(((unchecked((int)(pending)) & unchecked((int)(scd_regs[(int)((50 >> 1))].Low))) >> 1))));
            }
            cdc.irq |= unchecked((byte)(64));
            }
            scd_regs[(int)((4 >> 1))].High |= unchecked((byte)(128));
            }
            }
            }
            return data;
        }
    }
}

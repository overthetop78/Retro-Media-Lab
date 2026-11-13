using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed partial class Cdd
{
    private void cdd_reset()
    {
        unchecked
        {
            cdd.latency = unchecked((uint)(0));
            cdd.index = 0;
            cdd.lba = 0;
            cdd.status = unchecked((byte)(((cdd.loaded != 0) ? 9 : 11)));
            cdd.pending = unchecked((byte)(0));
            cdd.fader[(int)(0)] = (cdd.fader[(int)(1)] = unchecked((ushort)(1024)));
            cdd.audio[(int)(0)] = (cdd.audio[(int)(1)] = unchecked((short)(0)));
        }
    }
    private void cdd_update()
    {
        unchecked
        {
            if ((cdd.latency > unchecked((uint)(0)))) {
            cdd.latency--;
            cdc_decoder_update(unchecked((uint)(0)));
            } else {
            if ((unchecked((int)(cdd.status)) == 1)) {
            if ((cdd.index >= cdd.toc.last)) {
            cdd.status = unchecked((byte)(12));
            return;
            }
            if (has_subcode) {
            cdd_read_subcode();
            }
            if ((cdd.toc.tracks[(int)(cdd.index)].type != 0)) {
            Span<byte> header = stackalloc byte[(int)(4)];
            uint msf = unchecked((uint)((cdd.lba + 150)));
            header[(int)(0)] = lut_BCD_8[(int)(((msf / unchecked((uint)(75))) / unchecked((uint)(60))))];
            header[(int)(1)] = lut_BCD_8[(int)(((msf / unchecked((uint)(75))) % unchecked((uint)(60))))];
            header[(int)(2)] = lut_BCD_8[(int)((msf % unchecked((uint)(75))))];
            header[(int)(3)] = unchecked((byte)(cdd.toc.tracks[(int)(cdd.index)].type));
            cdc_decoder_update(new CPointer<byte>(header).Cast<uint>()[0]);
            } else {
            if ((cdd.lba >= cdd.toc.tracks[(int)(cdd.index)].start)) {
            scd_regs[(int)((54 >> 1))].High = unchecked((byte)(0));
            }
            cdc_decoder_update(unchecked((uint)(0)));
            }
            cdd.lba++;
            if ((cdd.lba >= cdd.toc.tracks[(int)(cdd.index)].end)) {
            cdd_seek_audio((cdd.index + 1), cdd.toc.tracks[(int)((cdd.index + 1))].start);
            cdd.index++;
            scd_regs[(int)((54 >> 1))].High = unchecked((byte)(1));
            }
            } else {
            cdc_decoder_update(unchecked((uint)(0)));
            if ((unchecked((int)(cdd.status)) == 3)) {
            int index = cdd.index;
            cdd.lba += cdd.scanOffset;
            if ((cdd.lba >= cdd.toc.tracks[(int)(index)].end)) {
            index++;
            if ((index < cdd.toc.last)) {
            cdd.lba = cdd.toc.tracks[(int)(index)].start;
            } else {
            cdd.lba = cdd.toc.end;
            cdd.index = cdd.toc.last;
            cdd.status = unchecked((byte)(12));
            scd_regs[(int)((54 >> 1))].High = unchecked((byte)(1));
            return;
            }
            } else if ((cdd.lba < cdd.toc.tracks[(int)(index)].start)) {
            if ((index > 0)) {
            index--;
            cdd.lba = cdd.toc.tracks[(int)(index)].end;
            } else {
            cdd.lba = 0;
            }
            }
            if (has_subcode) {
            SeekSubcode((cdd.lba * 96));
            }
            if ((cdd.toc.tracks[(int)(index)].type == 0)) {
            cdd_seek_audio(index, cdd.lba);
            scd_regs[(int)((54 >> 1))].High = unchecked((byte)(0));
            } else {
            scd_regs[(int)((54 >> 1))].High = unchecked((byte)(1));
            }
            cdd.index = index;
            }
            }
            }
            if ((cdd.pending != 0)) {
            int index = 0;
            int lba = (((((((unchecked((int)(scd_regs[(int)((68 >> 1))].High)) * 10) + unchecked((int)(scd_regs[(int)((68 >> 1))].Low))) * 60) + ((unchecked((int)(scd_regs[(int)((70 >> 1))].High)) * 10) + unchecked((int)(scd_regs[(int)((70 >> 1))].Low)))) * 75) + ((unchecked((int)(scd_regs[(int)((72 >> 1))].High)) * 10) + unchecked((int)(scd_regs[(int)((72 >> 1))].Low)))) - 150);
            if ((lba > cdd.toc.end)) {
            lba = cdd.toc.end;
            }
            if ((!(cdd.latency != 0))) {
            cdd.latency = unchecked((uint)((2 + (10 * drive_latency))));
            }
            if ((lba > cdd.lba)) {
            cdd.latency += unchecked((uint)(((((lba - cdd.lba) * 120) * drive_latency) / 270000)));
            } else {
            cdd.latency += unchecked((uint)(((((cdd.lba - lba) * 120) * drive_latency) / 270000)));
            }
            cdd.lba = lba;
            while (((cdd.toc.tracks[(int)(index)].end <= lba) && (index < cdd.toc.last))) index++;
            if ((cdd.toc.tracks[(int)(index)].type == 0)) {
            if ((lba < cdd.toc.tracks[(int)(index)].start)) {
            lba = cdd.toc.tracks[(int)(index)].start;
            }
            cdd_seek_audio(index, lba);
            }
            cdd.index = index;
            if (has_subcode) {
            SeekSubcode((lba * 96));
            }
            scd_regs[(int)((54 >> 1))].High = unchecked((byte)(1));
            cdd.status = cdd.pending;
            cdd.pending = unchecked((byte)(0));
            }
        }
    }
    private void cdd_process()
    {
        unchecked
        {
            switch ((unchecked((int)(scd_regs[(int)((66 >> 1))].High)) & 15)) {
            case 0:
            {
            if ((cdd.latency <= unchecked((uint)(3)))) {
            scd_regs[(int)((56 >> 1))].High = cdd.status;
            if (((unchecked((int)(cdd.status)) == 0) || (unchecked((int)(cdd.status)) > 4))) break;
            if ((unchecked((int)(scd_regs[(int)((56 >> 1))].Low)) == 15)) {
            int lba = (cdd.lba + 150);
            scd_regs[(int)((56 >> 1))].Low = unchecked((byte)(0));
            scd_regs[(int)((58 >> 1))].Word = lut_BCD_16[(int)(((lba / 75) / 60))];
            scd_regs[(int)((60 >> 1))].Word = lut_BCD_16[(int)(((lba / 75) % 60))];
            scd_regs[(int)((62 >> 1))].Word = lut_BCD_16[(int)((lba % 75))];
            scd_regs[(int)((64 >> 1))].High = unchecked((byte)(((cdd.toc.tracks[(int)(cdd.index)].type != 0) ? 4 : 0)));
            } else if ((unchecked((int)(scd_regs[(int)((56 >> 1))].Low)) == 0)) {
            int lba = (cdd.lba + 150);
            scd_regs[(int)((58 >> 1))].Word = lut_BCD_16[(int)(((lba / 75) / 60))];
            scd_regs[(int)((60 >> 1))].Word = lut_BCD_16[(int)(((lba / 75) % 60))];
            scd_regs[(int)((62 >> 1))].Word = lut_BCD_16[(int)((lba % 75))];
            scd_regs[(int)((64 >> 1))].High = unchecked((byte)(((cdd.toc.tracks[(int)(cdd.index)].type != 0) ? 4 : 0)));
            } else if ((unchecked((int)(scd_regs[(int)((56 >> 1))].Low)) == 1)) {
            int lba = abs((cdd.lba - cdd.toc.tracks[(int)(cdd.index)].start));
            scd_regs[(int)((58 >> 1))].Word = lut_BCD_16[(int)(((lba / 75) / 60))];
            scd_regs[(int)((60 >> 1))].Word = lut_BCD_16[(int)(((lba / 75) % 60))];
            scd_regs[(int)((62 >> 1))].Word = lut_BCD_16[(int)((lba % 75))];
            scd_regs[(int)((64 >> 1))].High = unchecked((byte)(((cdd.toc.tracks[(int)(cdd.index)].type != 0) ? 4 : 0)));
            } else if ((unchecked((int)(scd_regs[(int)((56 >> 1))].Low)) == 2)) {
            scd_regs[(int)((58 >> 1))].Word = unchecked((ushort)(((cdd.index < cdd.toc.last) ? unchecked((int)(lut_BCD_16[(int)((cdd.index + 1))])) : 2570)));
            }
            }
            break;
            }
            case 1:
            {
            cdd.status = unchecked((byte)(((cdd.loaded != 0) ? 9 : 11)));
            scd_regs[(int)((54 >> 1))].High = unchecked((byte)(1));
            scd_regs[(int)((56 >> 1))].Word = unchecked((ushort)(((0 << 8) | 15)));
            scd_regs[(int)((58 >> 1))].Word = unchecked((ushort)(0));
            scd_regs[(int)((60 >> 1))].Word = unchecked((ushort)(0));
            scd_regs[(int)((62 >> 1))].Word = unchecked((ushort)(0));
            scd_regs[(int)((64 >> 1))].Word = unchecked((ushort)(((~(0 + 15)) & 15)));
            cdd.index = (cdd.lba = 0);
            return;
            }
            case 2:
            {
            switch (scd_regs[(int)((68 >> 1))].Low) {
            case 0:
            {
            int lba = (cdd.lba + 150);
            scd_regs[(int)((56 >> 1))].Word = unchecked((ushort)((unchecked((int)(cdd.status)) << 8)));
            scd_regs[(int)((58 >> 1))].Word = lut_BCD_16[(int)(((lba / 75) / 60))];
            scd_regs[(int)((60 >> 1))].Word = lut_BCD_16[(int)(((lba / 75) % 60))];
            scd_regs[(int)((62 >> 1))].Word = lut_BCD_16[(int)((lba % 75))];
            scd_regs[(int)((64 >> 1))].High = unchecked((byte)(((cdd.toc.tracks[(int)(cdd.index)].type != 0) ? 4 : 0)));
            break;
            }
            case 1:
            {
            int lba = abs((cdd.lba - cdd.toc.tracks[(int)(cdd.index)].start));
            scd_regs[(int)((56 >> 1))].Word = unchecked((ushort)(((unchecked((int)(cdd.status)) << 8) | 1)));
            scd_regs[(int)((58 >> 1))].Word = lut_BCD_16[(int)(((lba / 75) / 60))];
            scd_regs[(int)((60 >> 1))].Word = lut_BCD_16[(int)(((lba / 75) % 60))];
            scd_regs[(int)((62 >> 1))].Word = lut_BCD_16[(int)((lba % 75))];
            scd_regs[(int)((64 >> 1))].High = unchecked((byte)(((cdd.toc.tracks[(int)(cdd.index)].type != 0) ? 4 : 0)));
            break;
            }
            case 2:
            {
            scd_regs[(int)((56 >> 1))].Word = unchecked((ushort)(((unchecked((int)(cdd.status)) << 8) | 2)));
            scd_regs[(int)((58 >> 1))].Word = unchecked((ushort)(((cdd.index < cdd.toc.last) ? unchecked((int)(lut_BCD_16[(int)((cdd.index + 1))])) : 2570)));
            scd_regs[(int)((60 >> 1))].Word = unchecked((ushort)(0));
            scd_regs[(int)((62 >> 1))].Word = unchecked((ushort)(0));
            scd_regs[(int)((64 >> 1))].High = unchecked((byte)(0));
            break;
            }
            case 3:
            {
            int lba = (cdd.toc.end + 150);
            scd_regs[(int)((56 >> 1))].Word = unchecked((ushort)(((unchecked((int)(cdd.status)) << 8) | 3)));
            scd_regs[(int)((58 >> 1))].Word = lut_BCD_16[(int)(((lba / 75) / 60))];
            scd_regs[(int)((60 >> 1))].Word = lut_BCD_16[(int)(((lba / 75) % 60))];
            scd_regs[(int)((62 >> 1))].Word = lut_BCD_16[(int)((lba % 75))];
            scd_regs[(int)((64 >> 1))].High = unchecked((byte)(0));
            break;
            }
            case 4:
            {
            scd_regs[(int)((56 >> 1))].Word = unchecked((ushort)(((unchecked((int)(cdd.status)) << 8) | 4)));
            scd_regs[(int)((58 >> 1))].Word = unchecked((ushort)(1));
            scd_regs[(int)((60 >> 1))].Word = lut_BCD_16[(int)(cdd.toc.last)];
            scd_regs[(int)((62 >> 1))].Word = unchecked((ushort)(0));
            scd_regs[(int)((64 >> 1))].High = unchecked((byte)(0));
            break;
            }
            case 5:
            {
            int track = ((unchecked((int)(scd_regs[(int)((70 >> 1))].High)) * 10) + unchecked((int)(scd_regs[(int)((70 >> 1))].Low)));
            if (((track > 0) && (track <= cdd.toc.last))) {
            int lba = (cdd.toc.tracks[(int)((track - 1))].start + 150);
            scd_regs[(int)((56 >> 1))].Word = unchecked((ushort)(((unchecked((int)(cdd.status)) << 8) | 5)));
            scd_regs[(int)((58 >> 1))].Word = lut_BCD_16[(int)(((lba / 75) / 60))];
            scd_regs[(int)((60 >> 1))].Word = lut_BCD_16[(int)(((lba / 75) % 60))];
            scd_regs[(int)((62 >> 1))].Word = lut_BCD_16[(int)((lba % 75))];
            scd_regs[(int)((62 >> 1))].High |= unchecked((byte)(((cdd.toc.tracks[(int)((track - 1))].type != 0) ? 8 : 0)));
            scd_regs[(int)((64 >> 1))].High = unchecked((byte)((track % 10)));
            } else {
            scd_regs[(int)((56 >> 1))].Word = unchecked((ushort)(((unchecked((int)(cdd.status)) << 8) | 15)));
            scd_regs[(int)((58 >> 1))].Word = unchecked((ushort)(0));
            scd_regs[(int)((60 >> 1))].Word = unchecked((ushort)(0));
            scd_regs[(int)((62 >> 1))].Word = unchecked((ushort)(0));
            scd_regs[(int)((64 >> 1))].High = unchecked((byte)(0));
            }
            break;
            }
            case 6:
            {
            scd_regs[(int)((56 >> 1))].Word = unchecked((ushort)(((unchecked((int)(cdd.status)) << 8) | 6)));
            scd_regs[(int)((58 >> 1))].Word = unchecked((ushort)(0));
            scd_regs[(int)((60 >> 1))].Word = unchecked((ushort)(0));
            scd_regs[(int)((62 >> 1))].Word = unchecked((ushort)(0));
            scd_regs[(int)((64 >> 1))].High = unchecked((byte)(0));
            break;
            }
            default:
            {
            return;
            }
            }
            break;
            }
            case 3:
            {
            scd_regs[(int)((56 >> 1))].Word = unchecked((ushort)(((2 << 8) | 15)));
            scd_regs[(int)((58 >> 1))].Word = unchecked((ushort)(0));
            scd_regs[(int)((60 >> 1))].Word = unchecked((ushort)(0));
            scd_regs[(int)((62 >> 1))].Word = unchecked((ushort)(0));
            scd_regs[(int)((64 >> 1))].Word = unchecked((ushort)(((~(2 + 15)) & 15)));
            cdd.pending = unchecked((byte)(1));
            return;
            }
            case 4:
            {
            scd_regs[(int)((56 >> 1))].Word = unchecked((ushort)(((2 << 8) | 15)));
            scd_regs[(int)((58 >> 1))].Word = unchecked((ushort)(0));
            scd_regs[(int)((60 >> 1))].Word = unchecked((ushort)(0));
            scd_regs[(int)((62 >> 1))].Word = unchecked((ushort)(0));
            scd_regs[(int)((64 >> 1))].Word = unchecked((ushort)(((~(2 + 15)) & 15)));
            cdd.pending = unchecked((byte)(4));
            return;
            }
            case 6:
            {
            scd_regs[(int)((54 >> 1))].High = unchecked((byte)(1));
            cdd.status = (scd_regs[(int)((56 >> 1))].High = unchecked((byte)(4)));
            break;
            }
            case 7:
            {
            cdd.status = (scd_regs[(int)((56 >> 1))].High = unchecked((byte)(1)));
            break;
            }
            case 8:
            {
            cdd.scanOffset = 30;
            cdd.status = (scd_regs[(int)((56 >> 1))].High = unchecked((byte)(3)));
            break;
            }
            case 9:
            {
            cdd.scanOffset = (-30);
            cdd.status = (scd_regs[(int)((56 >> 1))].High = unchecked((byte)(3)));
            break;
            }
            case 10:
            {
            scd_regs[(int)((54 >> 1))].High = unchecked((byte)(1));
            cdd.status = (scd_regs[(int)((56 >> 1))].High = unchecked((byte)(4)));
            break;
            }
            case 12:
            {
            scd_regs[(int)((54 >> 1))].High = unchecked((byte)(1));
            cdd.status = unchecked((byte)(((cdd.loaded != 0) ? 9 : 11)));
            scd_regs[(int)((56 >> 1))].Word = unchecked((ushort)(((0 << 8) | 15)));
            scd_regs[(int)((58 >> 1))].Word = unchecked((ushort)(0));
            scd_regs[(int)((60 >> 1))].Word = unchecked((ushort)(0));
            scd_regs[(int)((62 >> 1))].Word = unchecked((ushort)(0));
            scd_regs[(int)((64 >> 1))].Word = unchecked((ushort)(((~(0 + 15)) & 15)));
            cdd.index = (cdd.lba = 0);
            return;
            }
            case 13:
            {
            scd_regs[(int)((54 >> 1))].High = unchecked((byte)(1));
            cdd.status = unchecked((byte)(5));
            scd_regs[(int)((56 >> 1))].Word = unchecked((ushort)(((5 << 8) | 15)));
            scd_regs[(int)((58 >> 1))].Word = unchecked((ushort)(0));
            scd_regs[(int)((60 >> 1))].Word = unchecked((ushort)(0));
            scd_regs[(int)((62 >> 1))].Word = unchecked((ushort)(0));
            scd_regs[(int)((64 >> 1))].Word = unchecked((ushort)(((~(5 + 15)) & 15)));
            cdd.index = (cdd.lba = 0);
            return;
            }
            default:
            scd_regs[(int)((56 >> 1))].High = cdd.status;
            break;
            }
            scd_regs[(int)((64 >> 1))].Low = unchecked((byte)(((~((((((((unchecked((int)(scd_regs[(int)((56 >> 1))].High)) + unchecked((int)(scd_regs[(int)((56 >> 1))].Low))) + unchecked((int)(scd_regs[(int)((58 >> 1))].High))) + unchecked((int)(scd_regs[(int)((58 >> 1))].Low))) + unchecked((int)(scd_regs[(int)((60 >> 1))].High))) + unchecked((int)(scd_regs[(int)((60 >> 1))].Low))) + unchecked((int)(scd_regs[(int)((62 >> 1))].High))) + unchecked((int)(scd_regs[(int)((62 >> 1))].Low))) + unchecked((int)(scd_regs[(int)((64 >> 1))].High)))) & 15)));
        }
    }
}

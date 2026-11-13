using System.Buffers.Binary;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed partial class Pcm
{
    private void pcm_run(uint length)
    {
        unchecked
        {
            int prev_l = unchecked((int)(pcm.sampleOutput[(int)(0)]));
            int prev_r = unchecked((int)(pcm.sampleOutput[(int)(1)]));
            if ((pcm.enabled != 0)) {
            int i = default;
            int j = default;
            int l = default;
            int r = default;
            for (i = 0; (unchecked((uint)(i)) < length); i++) {
            l = (r = 0);
            for (j = 0; (j < 8); j++) {
            if (((unchecked((int)(pcm.status)) & (1 << j)) != 0)) {
            short data = unchecked((short)(pcm.ram[(int)(((pcm.chan[(int)(j)].addr >> 11) & unchecked((uint)(65535))))]));
            if ((unchecked((int)(data)) == 255)) {
            pcm.chan[(int)(j)].addr = unchecked((uint)((unchecked((int)(pcm.chan[(int)(j)].ls.Word)) << 11)));
            data = unchecked((short)(pcm.ram[(int)(pcm.chan[(int)(j)].ls.Word)]));
            } else {
            pcm.chan[(int)(j)].addr += unchecked((uint)(pcm.chan[(int)(j)].fd.Word));
            }
            if ((unchecked((int)(data)) != 255)) {
            if (((unchecked((int)(data)) & 128) != 0)) {
            data = unchecked((short)((unchecked((int)(data)) & 127)));
            } else {
            data = unchecked((short)((-(unchecked((int)(data)) & 127))));
            }
            l += (((unchecked((int)(data)) * unchecked((int)(pcm.chan[(int)(j)].env))) * (unchecked((int)(pcm.chan[(int)(j)].pan)) & 15)) >> 5);
            r += (((unchecked((int)(data)) * unchecked((int)(pcm.chan[(int)(j)].env))) * (unchecked((int)(pcm.chan[(int)(j)].pan)) >> 4)) >> 5);
            }
            }
            }
            if ((l < (-32768))) l = (-32768); else if ((l > 32767)) l = 32767;
            if ((r < (-32768))) r = (-32768); else if ((r > 32767)) r = 32767;
            l = ((l * pcm_volume) / 100);
            r = ((r * pcm_volume) / 100);
            AddDeltaFast(unchecked((uint)(i)), (l - prev_l), (r - prev_r));
            prev_l = l;
            prev_r = r;
            }
            pcm.sampleOutput[(int)(0)] = unchecked((short)(prev_l));
            pcm.sampleOutput[(int)(1)] = unchecked((short)(prev_r));
            } else {
            if (((prev_l | prev_r) != 0)) {
            AddDeltaFast(unchecked((uint)(0)), (-prev_l), (-prev_r));
            pcm.sampleOutput[(int)(0)] = unchecked((short)(0));
            pcm.sampleOutput[(int)(1)] = unchecked((short)(0));
            }
            }
            EndFrame(length);
            pcm.cycles += (length * unchecked((uint)((384 * 4))));
        }
    }
    private void pcm_update(uint samples)
    {
        unchecked
        {
            uint clocks = ClocksNeeded(samples);
            if ((clocks > unchecked((uint)(0)))) {
            pcm_run(clocks);
            }
            pcm.cycles = unchecked((uint)(0));
        }
    }
    private void pcm_write(uint address, byte data, uint cycles)
    {
        unchecked
        {
            int clocks = unchecked((int)((cycles - pcm.cycles)));
            if ((clocks > 0)) {
            clocks = (((clocks + (384 * 4)) - 1) / (384 * 4));
            pcm_run(unchecked((uint)(clocks)));
            }
            if ((address >= unchecked((uint)(4096)))) {
            pcm.bank[(int)((address & unchecked((uint)(4095))))] = data;
            return;
            }
            switch (address) {
            case 0:
            {
            pcm.chan[(int)(pcm.index)].env = data;
            return;
            }
            case 1:
            {
            pcm.chan[(int)(pcm.index)].pan = data;
            return;
            }
            case 2:
            {
            pcm.chan[(int)(pcm.index)].fd.Low = data;
            return;
            }
            case 3:
            {
            pcm.chan[(int)(pcm.index)].fd.High = data;
            return;
            }
            case 4:
            {
            pcm.chan[(int)(pcm.index)].ls.Low = data;
            return;
            }
            case 5:
            {
            pcm.chan[(int)(pcm.index)].ls.High = data;
            return;
            }
            case 6:
            {
            pcm.chan[(int)(pcm.index)].st = unchecked((uint)((unchecked((int)(data)) << (8 + 11))));
            if ((!((unchecked((int)(pcm.status)) & (1 << unchecked((int)(pcm.index)))) != 0))) {
            pcm.chan[(int)(pcm.index)].addr = pcm.chan[(int)(pcm.index)].st;
            }
            return;
            }
            case 7:
            {
            if (((unchecked((int)(data)) & 64) != 0)) {
            pcm.index = unchecked((byte)((unchecked((int)(data)) & 7)));
            } else {
            pcm.bank = new CPointer<byte>(pcm.ram, (int)(((unchecked((int)(data)) & 15) << 12)));
            }
            pcm.enabled = unchecked((byte)((unchecked((int)(data)) & 128)));
            return;
            }
            case 8:
            {
            pcm.status = unchecked((byte)((~unchecked((int)(data)))));
            if (((unchecked((int)(data)) & 1) != 0)) pcm.chan[(int)(0)].addr = pcm.chan[(int)(0)].st;
            if (((unchecked((int)(data)) & 2) != 0)) pcm.chan[(int)(1)].addr = pcm.chan[(int)(1)].st;
            if (((unchecked((int)(data)) & 4) != 0)) pcm.chan[(int)(2)].addr = pcm.chan[(int)(2)].st;
            if (((unchecked((int)(data)) & 8) != 0)) pcm.chan[(int)(3)].addr = pcm.chan[(int)(3)].st;
            if (((unchecked((int)(data)) & 16) != 0)) pcm.chan[(int)(4)].addr = pcm.chan[(int)(4)].st;
            if (((unchecked((int)(data)) & 32) != 0)) pcm.chan[(int)(5)].addr = pcm.chan[(int)(5)].st;
            if (((unchecked((int)(data)) & 64) != 0)) pcm.chan[(int)(6)].addr = pcm.chan[(int)(6)].st;
            if (((unchecked((int)(data)) & 128) != 0)) pcm.chan[(int)(7)].addr = pcm.chan[(int)(7)].st;
            return;
            }
            default:
            {
            return;
            }
            }
        }
    }
    private byte pcm_read(uint address, uint cycles)
    {
        unchecked
        {
            int clocks = unchecked((int)((cycles - pcm.cycles)));
            if ((clocks > 0)) {
            clocks = (((clocks + (384 * 4)) - 1) / (384 * 4));
            pcm_run(unchecked((uint)(clocks)));
            }
            if ((address >= unchecked((uint)(4096)))) {
            return pcm.bank[(int)((address & unchecked((uint)(4095))))];
            }
            if (((address >= unchecked((uint)(16))) && (address < unchecked((uint)(32))))) {
            int index = unchecked((int)(((address >> 1) & unchecked((uint)(7)))));
            if (((address & unchecked((uint)(1))) != 0)) {
            return unchecked((byte)(((pcm.chan[(int)(index)].addr >> (11 + 8)) & unchecked((uint)(255)))));
            } else {
            return unchecked((byte)(((pcm.chan[(int)(index)].addr >> 11) & unchecked((uint)(255)))));
            }
            }
            return unchecked((byte)(255));
        }
    }
}

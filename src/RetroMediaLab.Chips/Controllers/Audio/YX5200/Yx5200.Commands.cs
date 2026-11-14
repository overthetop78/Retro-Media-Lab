using System.Buffers.Binary;
using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Controllers.Audio.YX5200;

public sealed partial class Yx5200
{
    private void yx5200_reset()
    {
        unchecked
        {
            yx5200_unload_track();
            ClearState();
            yx5200.playbackVolume = unchecked((byte)(30));
            yx5200.audioEnabled = unchecked((byte)(1));
        }
    }
    private void yx5200_write(uint rx_data)
    {
        unchecked
        {
            if ((unchecked((int)(yx5200.rxCycle)) == 0)) {
            if ((!(rx_data != 0))) {
            yx5200.rxBuffer[(int)(yx5200.rxCounter)] = unchecked((byte)(0));
            yx5200.rxCycle = unchecked((byte)(1));
            }
            } else if ((unchecked((int)(yx5200.rxCycle)) < 9)) {
            yx5200.rxBuffer[(int)(yx5200.rxCounter)] |= unchecked((byte)((rx_data << (unchecked((int)(yx5200.rxCycle)) - 1))));
            yx5200.rxCycle++;
            } else {
            if ((rx_data != 0)) {
            yx5200.rxCounter++;
            if ((unchecked((int)(yx5200.rxCounter)) == 10)) {
            yx5200_process_cmd();
            yx5200.rxCounter = unchecked((byte)(0));
            }
            }
            yx5200.rxCycle = unchecked((byte)(0));
            }
        }
    }
    private void yx5200_process_cmd()
    {
        unchecked
        {
            switch (yx5200.rxBuffer[(int)(3)]) {
            case 1:
            {
            if (((unchecked((int)(yx5200.trackIndex)) > 0) && (unchecked((int)(yx5200.trackIndex)) <= 2999))) {
            yx5200_load_track(unchecked((ushort)((unchecked((int)(yx5200.trackIndex)) + 1))), yx5200.playbackLoop);
            }
            break;
            }
            case 2:
            {
            if ((unchecked((int)(yx5200.trackIndex)) > 1)) {
            yx5200_load_track(unchecked((ushort)((unchecked((int)(yx5200.trackIndex)) - 1))), yx5200.playbackLoop);
            }
            break;
            }
            case 3:
            {
            uint index = unchecked((uint)(((unchecked((int)(yx5200.rxBuffer[(int)(5)])) << 8) | unchecked((int)(yx5200.rxBuffer[(int)(6)])))));
            if (((index > unchecked((uint)(0))) && (index <= unchecked((uint)(2999))))) {
            yx5200_load_track(unchecked((ushort)(index)), unchecked((byte)(1)));
            }
            break;
            }
            case 4:
            {
            if ((unchecked((int)(yx5200.playbackVolume)) < 30)) {
            yx5200.playbackVolume++;
            }
            break;
            }
            case 5:
            {
            if ((unchecked((int)(yx5200.playbackVolume)) > 0)) {
            yx5200.playbackVolume--;
            }
            break;
            }
            case 6:
            {
            uint volume = unchecked((uint)(((unchecked((int)(yx5200.rxBuffer[(int)(5)])) << 8) | unchecked((int)(yx5200.rxBuffer[(int)(6)])))));
            if ((volume <= unchecked((uint)(30)))) {
            yx5200.playbackVolume = unchecked((byte)(volume));
            }
            break;
            }
            case 8:
            {
            uint index = unchecked((uint)(((unchecked((int)(yx5200.rxBuffer[(int)(5)])) << 8) | unchecked((int)(yx5200.rxBuffer[(int)(6)])))));
            if (((index > unchecked((uint)(0))) && (index <= unchecked((uint)(2999))))) {
            yx5200_load_track(unchecked((ushort)(index)), unchecked((byte)(0)));
            }
            break;
            }
            case 12:
            {
            yx5200_reset();
            break;
            }
            case 13:
            {
            if ((yx5200.trackIndex != 0)) {
            yx5200.playbackEnabled = unchecked((byte)(1));
            }
            break;
            }
            case 14:
            {
            yx5200.playbackEnabled = unchecked((byte)(0));
            break;
            }
            case 22:
            {
            yx5200_unload_track();
            break;
            }
            case 25:
            {
            if ((yx5200.playbackEnabled != 0)) {
            yx5200.playbackLoop = unchecked((byte)(((unchecked((int)(yx5200.rxBuffer[(int)(6)])) & 1) ^ 1)));
            }
            break;
            }
            case 26:
            {
            yx5200.audioEnabled = unchecked((byte)(((unchecked((int)(yx5200.rxBuffer[(int)(6)])) & 1) ^ 1)));
            break;
            }
            default:
            {
            break;
            }
            }
        }
    }
}

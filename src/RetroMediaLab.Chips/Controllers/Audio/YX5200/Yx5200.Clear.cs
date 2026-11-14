using System.Buffers.Binary;
using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Controllers.Audio.YX5200;

public sealed partial class Yx5200
{
    private void ClearState()
    {
        yx5200.rxCycle = 0;
        yx5200.rxCounter = 0;
        yx5200.rxBuffer.AsSpan().Clear();
        yx5200.playbackEnabled = 0;
        yx5200.playbackLoop = 0;
        yx5200.playbackVolume = 0;
        yx5200.audioEnabled = 0;
        yx5200.trackIndex = 0;
        yx5200.audio.AsSpan().Clear();
    }
}

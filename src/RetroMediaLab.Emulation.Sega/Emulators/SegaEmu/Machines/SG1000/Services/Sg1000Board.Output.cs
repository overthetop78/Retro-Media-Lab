using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using C = RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Constants.Sg1000Constants;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Services;

internal sealed partial class Sg1000Board
{
    private readonly byte[] _pixels = new byte[C.Width * C.Height];
    private readonly bool[] _pixelKnown = new bool[C.Width * C.Height];
    private readonly List<short> _samples = [];
    private long _audioTime, _sampleIndex = 1, _sampleStart;
    private double _sampleArea, _previousSample, _dcOutput;
    private int _audioLevel;
    private bool _completedRaster;
    public long FramesCompleted { get; private set; }
    public ReadOnlyMemory<byte> ColourIndices => _pixels;
    public ReadOnlyMemory<bool> PixelKnowledge => _pixelKnown;

    private void OnVideo(Tms9918VideoSignal signal)
    {
        if (signal.Dot == 341 && signal.Line == 261) _completedRaster = true;
        if (signal.Dot is < 0 or >= C.Width || signal.Line is < 0 or >= C.Height) return;
        int offset = signal.Line * C.Width + signal.Dot;
        _pixels[offset] = signal.Colour;
        _pixelKnown[offset] = signal.ColourKnown;
    }

    private void AdvanceAudio(long time)
    {
        while (_audioTime < time)
        {
            long boundary = (long)((Int128)_sampleIndex * C.NanosecondsPerSecond / C.SampleRate);
            long end = Math.Min(time, boundary);
            _sampleArea += _audioLevel * (double)(end - _audioTime);
            _audioTime = end;
            if (end != boundary) break;
            double sample = _sampleArea / (boundary - _sampleStart);
            // Host PCM presentation: remove DC from the unipolar chip output, with headroom.
            // This is not a claimed simulation of the console's RF analog filter.
            _dcOutput = sample - _previousSample + 0.9973854300793629 * _dcOutput;
            _previousSample = sample;
            _samples.Add((short)Math.Clamp(Math.Round(_dcOutput * 0.8), short.MinValue, short.MaxValue));
            _sampleArea = 0; _sampleStart = boundary; _sampleIndex++;
        }
    }
    public short[] DrainAudio()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var result = _samples.ToArray();
        _samples.Clear();
        return result;
    }
}

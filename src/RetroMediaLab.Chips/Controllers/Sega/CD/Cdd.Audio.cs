namespace RetroMediaLab.Chips.Controllers.Sega.CD;

public sealed partial class Cdd
{
    public void ReadAudio(uint samples)
    {
        _cancellation.ThrowIfCancellationRequested();
        int left = cdd.audio[0], right = cdd.audio[1];
        if (scd_regs[0x36 >> 1].High == 0 && _media is not null && cdd.index < cdd.toc.last)
        {
            int volume = cdd.fader[0], target = cdd.fader[1];
            uint done = 0;
            while (done < samples)
            {
                _cancellation.ThrowIfCancellationRequested();
                int count = (int)Math.Min(samples - done, (uint)(_audioScratch.Length / 2));
                Span<short> audio = _audioScratch.AsSpan(0, count * 2); audio.Clear();
                int read = _media.ReadAudio(cdd.index, _audioSamplePosition, audio);
                if (read < 0 || read > count) throw new InvalidOperationException("Invalid CD audio sample count.");
                for (int i = 0; i < count; i++)
                {
                    int multiplier = (volume & 0x7fc) != 0 ? volume & 0x7fc : volume & 3;
                    int nextLeft = audio[i * 2] * multiplier / 1024 * _audioVolume / 100;
                    int nextRight = audio[i * 2 + 1] * multiplier / 1024 * _audioVolume / 100;
                    _output.AddDeltaFast(done + (uint)i, nextLeft - left, nextRight - right);
                    left = nextLeft; right = nextRight;
                    if (volume < target) volume++; else if (volume > target) volume--;
                }
                _audioSamplePosition += count; done += (uint)count;
            }
            cdd.fader[0] = (ushort)volume; cdd.audio[0] = (short)left; cdd.audio[1] = (short)right;
        }
        else if ((left | right) != 0)
        {
            _output.AddDeltaFast(0, -left, -right); cdd.audio[0] = cdd.audio[1] = 0;
        }
        _output.EndFrame(samples);
    }
}

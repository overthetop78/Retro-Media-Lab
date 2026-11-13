using System.Buffers.Binary;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed partial class Cdd
{
    public void ReadAudio(uint samples)
    {
        bool completed = false;
        try
        {
            unchecked
            {
                int previousLeft = cdd.audio[0], previousRight = cdd.audio[1];
                track_t track = cdd.toc.tracks[cdd.index];
                if (scd_regs[0x36 >> 1].High == 0 && (_chd is not null || track.File is not null))
                {
                    int volume = cdd.fader[0], target = cdd.fader[1];
                    if (_chd is not null)
                    {
                        for (uint i = 0; i < samples; i++)
                        {
                            _cancellation.ThrowIfCancellationRequested();
                            Span<byte> bytes = ReadHunk((int)_chdAudioPosition);
                            int multiplier = (volume & 0x7fc) != 0 ? volume & 0x7fc : volume & 3;
                            int left = BinaryPrimitives.ReadInt16BigEndian(bytes) * multiplier / 1024;
                            int right = BinaryPrimitives.ReadInt16BigEndian(bytes[2..]) * multiplier / 1024;
                            left = left * _audioVolume / 100; right = right * _audioVolume / 100;
                            _output.AddDeltaFast(i, left - previousLeft, right - previousRight);
                            previousLeft = left; previousRight = right;
                            _chdAudioPosition += 4;
                            if (_chdAudioPosition % 2448 >= 2352) _chdAudioPosition += 96;
                            if (volume < target) volume++;
                            else if (volume > target) volume--;
                            else if (volume == 0) break;
                        }
                    }
                    else
                    {
                        Span<byte> audio = _bus.CdcRam.Span[..checked((int)samples * 4)];
                        if (track.Vorbis is not null)
                        {
                            int done = 0;
                            while (done < audio.Length)
                            {
                                _cancellation.ThrowIfCancellationRequested();
                                int length = track.Vorbis.ReadPcm(audio[done..]);
                                if (length <= 0) break;
                                done += length;
                            }
                        }
                        else ReadStream(track.File!, audio);
                        for (uint i = 0; i < samples; i++)
                        {
                            int multiplier = (volume & 0x7fc) != 0 ? volume & 0x7fc : volume & 3;
                            int offset = (int)i * 4;
                            int left = BinaryPrimitives.ReadInt16LittleEndian(audio[offset..]) * multiplier / 1024;
                            int right = BinaryPrimitives.ReadInt16LittleEndian(audio[(offset + 2)..]) * multiplier / 1024;
                            left = left * _audioVolume / 100; right = right * _audioVolume / 100;
                            _output.AddDeltaFast(i, left - previousLeft, right - previousRight);
                            previousLeft = left; previousRight = right;
                            if (volume < target) volume++;
                            else if (volume > target) volume--;
                            else if (volume == 0) break;
                        }
                    }
                    cdd.fader[0] = (ushort)volume;
                    cdd.audio[0] = (short)previousLeft; cdd.audio[1] = (short)previousRight;
                }
                else if ((previousLeft | previousRight) != 0)
                {
                    _output.AddDeltaFast(0, -previousLeft, -previousRight);
                    cdd.audio[0] = cdd.audio[1] = 0;
                }
                _output.EndFrame(samples);
            }
            completed = true;
        }
        finally
        {
            if (!completed) Unload();
        }
    }
}

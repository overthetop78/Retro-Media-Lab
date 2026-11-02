using RetroMediaLab.Chips.Video.Acorn.Archimedes.VIDC.Enums;
using C = RetroMediaLab.Chips.Video.Acorn.Archimedes.VIDC.Constants.ArchimedesVidcConstants;

namespace RetroMediaLab.Chips.Video.Acorn.Archimedes.VIDC.Services;

public sealed partial class ArchimedesVidc
{
    private static readonly short[] SoundDac = CreateSoundDac();
    private readonly byte[] _stereo = new byte[C.StereoChannels];
    private uint _soundWord;
    private int _soundWordBytes;
    private byte _soundHeld;
    private int _soundChannel;
    private int _soundRemaining;
    private int _soundLeft;
    private int _soundRight;
    private long _audioLeftArea;
    private long _audioRightArea;
    private long _audioClocks;

    private int SoundPeriod => ((int)(Register(VidcRegister.SoundFrequency) & C.SoundFrequencyMask) + C.SoundPeriodOffset) * C.SoundClockDivider;

    private void ResetSound()
    {
        Array.Clear(_stereo);
        _soundWord = 0;
        _soundWordBytes = _soundHeld = 0;
        _soundChannel = _soundLeft = _soundRight = 0;
        _soundRemaining = SoundPeriod;
        _audioLeftArea = _audioRightArea = _audioClocks = 0;
    }

    private void AdvanceSound(int clocks)
    {
        while (clocks > 0)
        {
            var elapsed = Math.Min(clocks, _soundRemaining);
            _audioLeftArea += (long)_soundLeft * elapsed;
            _audioRightArea += (long)_soundRight * elapsed;
            _audioClocks += elapsed;
            _soundRemaining -= elapsed;
            clocks -= elapsed;
            if (_soundRemaining != 0) continue;
            SampleSound();
            _soundRemaining = SoundPeriod;
        }
    }

    private void SampleSound()
    {
        if (_soundWordBytes == 0 && _soundFifo.TryTake(out _soundWord)) _soundWordBytes = sizeof(uint);
        if (_soundWordBytes > 0)
        {
            _soundHeld = (byte)_soundWord;
            _soundWord >>= C.ByteBits;
            _soundWordBytes--;
        }
        if (_soundFifo.Count == 0) SoundDmaRequest = true;
        var sample = SoundDac[_soundHeld];
        var position = _stereo[_soundChannel];
        _soundLeft = sample * C.LeftStereoWeights[position] * C.SoundDeglitchNumerator / (C.StereoDivisor * C.SoundDeglitchDivisor);
        _soundRight = sample * C.RightStereoWeights[position] * C.SoundDeglitchNumerator / (C.StereoDivisor * C.SoundDeglitchDivisor);
        _soundChannel = (_soundChannel + 1) & (C.StereoChannels - 1);
    }

    /// <summary>Intègre les créneaux multiplexés, plutôt que d'échantillonner une seule voie.</summary>
    public (short Left, short Right) TakeAudioSample()
    {
        if (_audioClocks == 0) return ((short)_soundLeft, (short)_soundRight);
        var sample = ((short)(_audioLeftArea / _audioClocks), (short)(_audioRightArea / _audioClocks));
        _audioLeftArea = _audioRightArea = _audioClocks = 0;
        return sample;
    }

    private static short[] CreateSoundDac()
    {
        var table = new short[byte.MaxValue + 1];
        for (var code = 0; code < table.Length; code++)
        {
            var point = (code >> C.SoundPointShift) & C.SoundPointMask;
            var chord = code >> C.SoundChordShift;
            var magnitude = (1 << (chord + C.SoundChordBase)) - (1 << C.SoundChordBase) + (point << chord);
            table[code] = (short)(magnitude * C.SoundPcmGain * ((code & C.SoundSignMask) != 0 ? -1 : 1));
        }
        return table;
    }
}

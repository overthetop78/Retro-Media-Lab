namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Sound;

internal sealed partial class SoundUnit
{
    private readonly BlipBuffer[] _mixedOutputs = new BlipBuffer[3];
    private BlipBuffer? _pcmOutput, _cddaOutput, _cartridgeOutput;
    private Action<int>? _updatePcm, _updateCdda, _updateCartridge;
    private int _mixedOutputCount;
    private Equalizer _leftEqualizer, _rightEqualizer;
    private short _leftLowPass, _rightLowPass;
    public int SampleRate { get; private set; }
    public double FrameRate { get; private set; }

    // The CD and cartridge units retain ownership of their buffers and callbacks.
    public void AttachCdAudio(BlipBuffer? pcm, BlipBuffer? cdda, Action<int>? updatePcm, Action<int>? updateCdda)
    {
        if ((pcm is null) != (cdda is null) || (pcm is not null && (updatePcm is null || updateCdda is null)))
            throw new ArgumentException("Both CD audio streams and their update callbacks are required.");
        _pcmOutput = pcm; _cddaOutput = cdda;
        _updatePcm = updatePcm; _updateCdda = updateCdda;
        RebuildMixInputs();
    }

    public void AttachCartridgeAudio(BlipBuffer? output, Action<int>? update)
    {
        if (output is not null && update is null) throw new ArgumentNullException(nameof(update));
        _cartridgeOutput = output; _updateCartridge = update;
        RebuildMixInputs();
    }

    private void RebuildMixInputs()
    {
        Array.Clear(_mixedOutputs);
        _mixedOutputCount = 0;
        if (_pcmOutput is not null)
        {
            _mixedOutputs[_mixedOutputCount++] = _pcmOutput;
            _mixedOutputs[_mixedOutputCount++] = _cddaOutput!;
        }
        if (_cartridgeOutput is not null) _mixedOutputs[_mixedOutputCount++] = _cartridgeOutput;
    }

    public void SetOutputRate(int sampleRate, double frameRate, uint systemClock, bool pal)
    {
        SampleRate = sampleRate;
        FrameRate = frameRate;
        double masterClock = frameRate != 0 ? 3420 * (pal ? 313 : 262) * frameRate : systemClock;
        _output.SetRates(masterClock, sampleRate);
    }

    public void ResetOutput()
    {
        _output.Clear();
        for (int i = 0; i < _mixedOutputCount; i++) _mixedOutputs[i].Clear();
        _leftLowPass = _rightLowPass = 0;
        ConfigureEqualizer();
    }

    public void ConfigureEqualizer()
    {
        _leftEqualizer.Initialize(_configuration.LowFrequency, _configuration.HighFrequency, SampleRate);
        _rightEqualizer.Initialize(_configuration.LowFrequency, _configuration.HighFrequency, SampleRate);
        _leftEqualizer.LowGain = _rightEqualizer.LowGain = _configuration.LowGain / 100.0;
        _leftEqualizer.MidGain = _rightEqualizer.MidGain = _configuration.MidGain / 100.0;
        _leftEqualizer.HighGain = _rightEqualizer.HighGain = _configuration.HighGain / 100.0;
    }

    public int UpdateAudio(uint cycles, Span<short> stereo)
    {
        int size = EndFrame(cycles);
        _updatePcm?.Invoke(size);
        _updateCdda?.Invoke(size);
        _updateCartridge?.Invoke(size);
        _output.MixSamples(_mixedOutputs.AsSpan(0, _mixedOutputCount), stereo, size);
        if ((_configuration.Filter & 1) != 0)
        {
            uint factorA = _configuration.LowPassRange;
            uint factorB = unchecked(0x10000u - factorA);
            int left = _leftLowPass, right = _rightLowPass;
            for (int i = 0; i < size * 2; i += 2)
            {
                left = unchecked((int)((uint)left * factorA + (uint)stereo[i] * factorB)) >> 16;
                right = unchecked((int)((uint)right * factorA + (uint)stereo[i + 1] * factorB)) >> 16;
                stereo[i] = unchecked((short)left);
                stereo[i + 1] = unchecked((short)right);
            }
            _leftLowPass = unchecked((short)left);
            _rightLowPass = unchecked((short)right);
        }
        else if ((_configuration.Filter & 2) != 0)
        {
            for (int i = 0; i < size * 2; i += 2)
            {
                int left = (int)_leftEqualizer.Process(stereo[i]);
                int right = (int)_rightEqualizer.Process(stereo[i + 1]);
                stereo[i] = (short)Math.Clamp(left, short.MinValue, short.MaxValue);
                stereo[i + 1] = (short)Math.Clamp(right, short.MinValue, short.MaxValue);
            }
        }
        if (_configuration.Mono)
            for (int i = 0; i < size * 2; i += 2)
                stereo[i] = stereo[i + 1] = (short)((stereo[i] + stereo[i + 1]) / 2);
        return size;
    }
}

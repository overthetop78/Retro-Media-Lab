using RetroMediaLab.MediaEngine.Contracts;

namespace RetroMediaLab.MediaEngine.Functions;

/// <summary>Provides phase-independent two-frequency FSK classification and configurable UART frame decoding.</summary>
public static class FskSerialFunctions
{
    private const int MinimumCarrierHalfWavesForClockRecovery = 64;
    private const double MinimumHalfWaveRatio = 0.5;
    private const double MaximumHalfWaveRatio = 1.5;
    public static void AppendPcmCarrier(List<short> samples, ref short level, int sampleRate, int baudRate, int zeroFrequency, int oneFrequency, double durationSeconds)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (!double.IsFinite(durationSeconds) || durationSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(durationSeconds));
        var bitCount = checked((int)Math.Round(durationSeconds * baudRate));
        for (var bit = 0; bit < bitCount; bit++)
            AppendPcmBit(samples, ref level, sampleRate, baudRate, zeroFrequency, oneFrequency, true);
    }

    public static void AppendPcmFrame(List<short> samples, ref short level, int sampleRate, int baudRate, int zeroFrequency, int oneFrequency, int dataBits, int stopBits, byte value)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (dataBits is < 1 or > 8) throw new ArgumentOutOfRangeException(nameof(dataBits));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stopBits);
        AppendPcmBit(samples, ref level, sampleRate, baudRate, zeroFrequency, oneFrequency, false);
        for (var bit = 0; bit < dataBits; bit++)
            AppendPcmBit(samples, ref level, sampleRate, baudRate, zeroFrequency, oneFrequency, (value & 1 << bit) != 0);
        for (var stop = 0; stop < stopBits; stop++)
            AppendPcmBit(samples, ref level, sampleRate, baudRate, zeroFrequency, oneFrequency, true);
    }

    public static FskSerialDecodeResult Decode(IReadOnlyList<int> samples, int sampleRate, int baudRate, int zeroFrequency, int oneFrequency, int dataBits, int stopBits, int minimumSamplesPerBit)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baudRate);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(zeroFrequency);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(oneFrequency);
        if (dataBits is < 1 or > 8) throw new ArgumentOutOfRangeException(nameof(dataBits));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stopBits);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minimumSamplesPerBit);

        var samplesPerBit = sampleRate / (double)baudRate;
        if (samplesPerBit < minimumSamplesPerBit) return new FskSerialDecodeResult([], 0, 0);
        // Decode the tone intervals once, then synchronize each UART frame separately.
        // A single grid for the whole tape accumulates oscillator/tape-speed drift.
        var tones = ToneLevels(samples, sampleRate, zeroFrequency, oneFrequency, out var tapeTimeScale);
        samplesPerBit *= tapeTimeScale;
        var bytes = new List<byte>();
        var positions = new List<int>();
        var tested = 0;
        for (var start = 1; start < tones.Length; start++)
        {
            var lastStop = (int)Math.Round(start + (dataBits + stopBits + 0.5) * samplesPerBit);
            if (lastStop >= tones.Length) break;
            if (tones[start] || !tones[start - 1]) continue;
            if (tones[(int)Math.Round(start + samplesPerBit / 2)]) continue;
            tested++;
            var validStop = true;
            for (var stop = 0; stop < stopBits; stop++)
                validStop &= tones[(int)Math.Round(start + (dataBits + 1.5 + stop) * samplesPerBit)];
            if (!validStop) continue;
            byte value = 0;
            for (var bit = 0; bit < dataBits; bit++)
                if (tones[(int)Math.Round(start + (bit + 1.5) * samplesPerBit)]) value |= (byte)(1 << bit);
            bytes.Add(value);
            positions.Add(start);
            // Resume inside the last stop bit, so an early next start cannot be skipped.
            start = lastStop;
        }
        return new FskSerialDecodeResult(bytes, bytes.Count, tested, positions);
    }

    private static bool[] ToneLevels(IReadOnlyList<int> samples, int sampleRate, int zeroFrequency, int oneFrequency, out double tapeTimeScale)
    {
        var result = new bool[samples.Count];
        Array.Fill(result, true);
        var zeroHalfWave = sampleRate / (2.0 * zeroFrequency);
        var oneHalfWave = sampleRate / (2.0 * oneFrequency);
        var minimum = Math.Min(zeroHalfWave, oneHalfWave) * MinimumHalfWaveRatio;
        var maximum = Math.Max(zeroHalfWave, oneHalfWave) * MaximumHalfWaveRatio;
        var previous = 0;
        var edge = -1;
        var carrierWaves = 0;
        long carrierSamples = 0;
        var longestCarrier = 0;
        tapeTimeScale = 1;
        for (var index = 0; index < samples.Count; index++)
        {
            var current = Math.Sign(samples[index]);
            if (current == 0 || current == previous) continue;
            if (edge >= 0)
            {
                var duration = index - edge;
                if (duration >= minimum && duration <= maximum)
                {
                    var mark = Math.Abs(duration - oneHalfWave) < Math.Abs(duration - zeroHalfWave);
                    Array.Fill(result, mark, edge, duration);
                    if (mark)
                    {
                        ++carrierWaves;
                        carrierSamples += duration;
                        if (carrierWaves >= MinimumCarrierHalfWavesForClockRecovery && carrierWaves > longestCarrier)
                        {
                            longestCarrier = carrierWaves;
                            // Tape speed changes both tones and baud rate by the same factor.
                            tapeTimeScale = carrierSamples / (carrierWaves * oneHalfWave);
                        }
                    }
                    else { carrierWaves = 0; carrierSamples = 0; }
                }
                else { carrierWaves = 0; carrierSamples = 0; }
            }
            edge = index;
            previous = current;
        }
        return result;
    }

    private static void AppendPcmBit(List<short> samples, ref short level, int sampleRate, int baudRate, int zeroFrequency, int oneFrequency, bool value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baudRate);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(zeroFrequency);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(oneFrequency);
        var frequency = value ? oneFrequency : zeroFrequency;
        var halfWaveCount = checked((int)Math.Round(frequency * 2d / baudRate));
        var start = samples.Count;
        var end = checked((int)Math.Round((Math.Round(start * (double)baudRate / sampleRate) + 1) * sampleRate / baudRate));
        for (var halfWave = 0; halfWave < halfWaveCount; halfWave++)
        {
            var next = start + (int)Math.Round((halfWave + 1) * (end - start) / (double)halfWaveCount);
            while (samples.Count < next) samples.Add(level);
            level = (short)-level;
        }
    }

    public static FskSerialDecodeResult DecodeFrames(IReadOnlyList<bool> bits, int dataBits, int stopBits)
    {
        ArgumentNullException.ThrowIfNull(bits);
        if (dataBits is < 1 or > 8) throw new ArgumentOutOfRangeException(nameof(dataBits));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(stopBits);
        var bytes = new List<byte>();
        var positions = new List<int>();
        var tested = 0;
        var valid = 0;
        var frameBits = 1 + dataBits + stopBits;
        for (var index = 0; index + frameBits <= bits.Count;)
        {
            if (bits[index])
            {
                index++;
                continue;
            }
            tested++;
            var validStop = true;
            for (var stop = 0; stop < stopBits; stop++)
                validStop &= bits[index + 1 + dataBits + stop];
            if (!validStop)
            {
                index++;
                continue;
            }
            byte value = 0;
            for (var bit = 0; bit < dataBits; bit++)
                if (bits[index + bit + 1]) value |= (byte)(1 << bit);
            bytes.Add(value);
            positions.Add(index);
            valid++;
            index += frameBits;
        }
        return new FskSerialDecodeResult(bytes, valid, tested, positions);
    }
}

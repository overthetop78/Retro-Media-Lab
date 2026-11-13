namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Sound;

internal struct Equalizer
{
    private double _lowFrequency, _highFrequency;
    private double _low0, _low1, _low2, _low3;
    private double _high0, _high1, _high2, _high3;
    private double _sample1, _sample2, _sample3;
    public double LowGain;
    public double MidGain;
    public double HighGain;

    public void Initialize(int lowFrequency, int highFrequency, int mixFrequency)
    {
        this = default;
        LowGain = MidGain = HighGain = 1.0;
        _lowFrequency = 2 * Math.Sin(Math.PI * ((double)lowFrequency / mixFrequency));
        _highFrequency = 2 * Math.Sin(Math.PI * ((double)highFrequency / mixFrequency));
    }

    public double Process(int sample)
    {
        const double smallAmount = 1.0 / 4294967295.0;
        _low0 += _lowFrequency * (sample - _low0) + smallAmount;
        _low1 += _lowFrequency * (_low0 - _low1);
        _low2 += _lowFrequency * (_low1 - _low2);
        _low3 += _lowFrequency * (_low2 - _low3);
        double low = _low3;
        _high0 += _highFrequency * (sample - _high0) + smallAmount;
        _high1 += _highFrequency * (_high0 - _high1);
        _high2 += _highFrequency * (_high1 - _high2);
        _high3 += _highFrequency * (_high2 - _high3);
        double high = _sample3 - _high3;
        double mid = sample - (high + low);
        low *= LowGain;
        mid *= MidGain;
        high *= HighGain;
        _sample3 = _sample2;
        _sample2 = _sample1;
        _sample1 = sample;
        return (int)(low + mid + high);
    }
}

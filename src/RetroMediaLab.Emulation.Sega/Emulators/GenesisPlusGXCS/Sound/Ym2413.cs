using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Sound;

internal sealed partial class Ym2413
{
    private const double M_PI = Math.PI;
    private readonly YM2413 ym2413 = new();
    private readonly int[] tl_tab = new int[11 * 2 * 256];
    private readonly uint[] sin_tab = new uint[1024 * 2];
    private readonly int[] output = new int[2];
    private uint LFO_AM;
    private int LFO_PM;
    private static double pow(double x, double y) => Math.Pow(x, y);
    private static double floor(double x) => Math.Floor(x);
    private static double sin(double x) => Math.Sin(x);
    private static double log(double x) => Math.Log(x);

    public Ym2413()
    {
        init_tables();
        OPLL_initalize();
    }
    public void Reset() => YM2413ResetChip();
    public void Write(uint address, uint value) => YM2413Write(address, value);
    public uint Read() => YM2413Read();
    public void Update(Span<int> stereo, int samples)
    {
        if (samples < 0 || samples > stereo.Length / 2) throw new ArgumentOutOfRangeException(nameof(samples));
        YM2413Update(new CPointer<int>(stereo), samples);
    }
    public int SaveContext(Span<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("YM2413 context buffer is too short.", nameof(state));
        state[..ContextSize].Clear();
        WriteYM2413(ym2413, state);
        return ContextSize;
    }
    public int LoadContext(ReadOnlySpan<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("YM2413 context buffer is too short.", nameof(state));
        ReadYM2413(ym2413, state);
        return ContextSize;
    }
}

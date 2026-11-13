using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Sound;

internal sealed partial class Ym2612
{
    public const int Discrete = 0, Integrated = 1, Enhanced = 2;
    private const int YM2612_DISCRETE = Discrete, YM2612_INTEGRATED = Integrated, YM2612_ENHANCED = Enhanced;
    private const double M_PI = Math.PI;
    private readonly int[] _signals = new int[10];
    private readonly YM2612 ym2612;
    private readonly int[] tl_tab = new int[13 * 2 * 256];
    private readonly uint[] sin_tab = new uint[1024];
    private readonly int[] lfo_pm_table = new int[128 * 8 * 32];
    private readonly uint[] op_mask = new uint[8 * 4];
    private static readonly byte[] lfo_pm_output = lfo_pm_output_data.ToArray();
    private int chip_type;
    private ref int m2 => ref _signals[0];
    private ref int c1 => ref _signals[1];
    private ref int c2 => ref _signals[2];
    private ref int mem => ref _signals[3];
    private Span<int> out_fm => _signals.AsSpan(4, 6);
    private static double pow(double x, double y) => Math.Pow(x, y);
    private static double floor(double x) => Math.Floor(x);
    private static double sin(double x) => Math.Sin(x);
    private static double log(double x) => Math.Log(x);

    public Ym2612()
    {
        ym2612 = new YM2612(_signals);
        init_tables();
    }

    public void Configure(int type) => YM2612Config(type);
    public void Reset() => YM2612ResetChip();
    public void Write(uint address, uint value) => YM2612Write(address, value);
    public uint Read() => YM2612Read();
    public void Update(Span<int> stereo, int samples)
    {
        if (samples < 0 || samples > stereo.Length / 2) throw new ArgumentOutOfRangeException(nameof(samples));
        YM2612Update(new CPointer<int>(stereo), samples);
    }

    private void setup_connection(CPointer<FM_CH> channel, int index)
    {
        FM_CH ch = channel[0];
        int carrier = 4 + index;
        ch.connect4Index = carrier;
        (ch.connect1Index, ch.connect3Index, ch.connect2Index, ch.mem_connectIndex) = ch.ALGO switch
        {
            0 => (1, 2, 3, 0),
            1 => (3, 2, 3, 0),
            2 => (2, 2, 3, 0),
            3 => (1, 2, 3, 2),
            4 => (1, 2, carrier, 3),
            5 => (-1, carrier, carrier, 0),
            6 => (1, carrier, carrier, 3),
            _ => (carrier, carrier, carrier, 3)
        };
    }

    public int SaveContext(Span<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("YM2612 context buffer is too short.", nameof(state));
        state[..ContextSize].Clear();
        WriteYM2612(ym2612, state);
        int offset = NativeContextSize;
        for (int c = 0; c < 6; c++)
            for (int s = 0; s < 4; s++, offset += 2)
                state[offset] = (byte)ym2612.CH[c].SLOT[s].DetuneIndex;
        return ContextSize;
    }

    public int LoadContext(ReadOnlySpan<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("YM2612 context buffer is too short.", nameof(state));
        ReadYM2612(ym2612, state);
        ym2612.OPN.LFO_PM &= 0x1f;
        ym2612.OPN.ST.fn_h &= 0x3f;
        ym2612.OPN.SL3.fn_h &= 0x3f;
        for (int s = 0; s < 3; s++) ym2612.OPN.SL3.kcode[s] &= 0x1f;
        int offset = NativeContextSize;
        for (int c = 0; c < 6; c++)
        {
            FM_CH ch = ym2612.CH[c];
            ch.ALGO &= 7;
            ch.pms &= 0xe0;
            ch.kcode &= 0x1f;
            for (int s = 0; s < 4; s++, offset += 2)
            {
                FM_SLOT slot = ch.SLOT[s];
                slot.DetuneIndex = state[offset] & 7;
                slot.KSR &= 3;
                slot.d1r = Math.Min(slot.d1r, 94u);
                slot.d2r = Math.Min(slot.d2r, 94u);
                slot.rr = Math.Min(slot.rr, 94u);
                slot.ksr &= 0x1f;
                slot.eg_sel_ar = Math.Min(slot.eg_sel_ar, (byte)144);
                slot.eg_sel_d1r = Math.Min(slot.eg_sel_d1r, (byte)144);
                slot.eg_sel_d2r = Math.Min(slot.eg_sel_d2r, (byte)144);
                slot.eg_sel_rr = Math.Min(slot.eg_sel_rr, (byte)144);
            }
            setup_connection(new CPointer<FM_CH>(ym2612.CH, c), c);
        }
        return ContextSize;
    }
}

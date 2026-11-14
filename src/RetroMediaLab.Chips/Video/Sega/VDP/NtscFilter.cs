using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Video.Sega.VDP;

public readonly record struct NtscSetup(double Hue = 0, double Saturation = 0, double Contrast = 0,
    double Brightness = 0, double Sharpness = 0, double Gamma = 0, double Resolution = 0,
    double Artifacts = 0, double Fringing = 0, double Bleed = 0, float[]? DecoderMatrix = null, byte[]? PaletteOut = null);

public sealed partial class NtscFilter
{
    private const int KernelHalf = 16, KernelSize = 33;
    private const float Pi = 3.14159265358979323846f;
    private const uint RgbBuilder = (1u << 21) | (1u << 11) | (1u << 1);
    private const uint RgbBias = 256 * 2 * RgbBuilder;
    private const float RgbOffset = 256 * 2 + 0.5f;
    private readonly bool _megaDrive;
    private readonly bool _rgb555;
    private readonly int _alignmentCount, _entrySize, _rescaleIn, _rescaleOut, _gammaSize;
    private readonly uint[] _table;
    private readonly float[] _toRgb = new float[6];
    private readonly float[] _toFloat;
    private readonly float[] _kernel;
    private float _contrast, _brightness, _artifacts, _fringing;

    public static NtscSetup Composite(bool megaDrive) => new(Resolution: megaDrive ? 0 : .25);
    public static NtscSetup SVideo(bool megaDrive) => new(Resolution: megaDrive ? .2 : .25, Artifacts: -1, Fringing: -1);
    public static NtscSetup Rgb(bool megaDrive) => new(Sharpness: .2, Resolution: .7, Artifacts: -1, Fringing: -1, Bleed: -1);
    public static NtscSetup Monochrome(bool megaDrive) => new(Saturation: -1, Sharpness: .2, Resolution: megaDrive ? 0 : .2, Artifacts: -.2, Fringing: -.2, Bleed: -1);

    public NtscFilter(bool megaDrive, bool rgb555, NtscSetup? setup = null)
    {
        _megaDrive = megaDrive;
        _rgb555 = rgb555;
        _alignmentCount = megaDrive ? 2 : 3;
        _entrySize = megaDrive ? 32 : 42;
        _rescaleIn = megaDrive ? 1 : 8;
        _rescaleOut = megaDrive ? 1 : 7;
        _gammaSize = megaDrive ? 8 : 16;
        _toFloat = new float[_gammaSize];
        _kernel = new float[_rescaleOut * KernelSize * 2];
        _table = new uint[(megaDrive ? 512 : 4096) * _entrySize];
        Initialize(setup ?? Composite(megaDrive));
    }

    public void Initialize(NtscSetup setup)
    {
        _brightness = (float)setup.Brightness * (0.5f * 256) + RgbOffset;
        _contrast = (float)setup.Contrast * (0.5f * 256) + 256;
        const float artifactsMid = .4f;
        float artifactsMax = _megaDrive ? 1.0f : 1.2f;
        _artifacts = (float)setup.Artifacts;
        if (_artifacts > 0) _artifacts *= artifactsMax - artifactsMid;
        _artifacts = _artifacts * artifactsMid + artifactsMid;
        float fringingMid = _megaDrive ? .3f : .8f;
        _fringing = (float)setup.Fringing;
        if (_fringing > 0) _fringing *= fringingMid * 2 - fringingMid;
        _fringing = _fringing * fringingMid + fringingMid;
        InitializeFilters(setup);
        float toFloat = 1.0f / (_gammaSize - 1);
        float gamma = 1.1333f - (float)setup.Gamma * .5f;
        for (int i = 0; i < _gammaSize; i++) _toFloat[i] = (float)Math.Pow(i * toFloat, gamma) * _contrast + _brightness;
        float hue = (float)setup.Hue * Pi + Pi / 180 * 15;
        float saturation = (float)setup.Saturation + 1;
        ReadOnlySpan<float> decoder = setup.DecoderMatrix;
        if (decoder.IsEmpty)
        {
            decoder = DefaultDecoder;
            hue += Pi / 180 * -15;
        }
        float sine = (float)Math.Sin(hue) * saturation;
        float cosine = (float)Math.Cos(hue) * saturation;
        for (int n = 0; n < 3; n++)
        {
            float i = decoder[n * 2], q = decoder[n * 2 + 1];
            _toRgb[n * 2] = i * cosine - q * sine;
            _toRgb[n * 2 + 1] = i * sine + q * cosine;
        }
        for (int entry = 0; entry < _table.Length / _entrySize; entry++)
        {
            float blue = _toFloat[_megaDrive ? entry >> 6 & 7 : entry >> 8 & 15];
            float green = _toFloat[_megaDrive ? entry >> 3 & 7 : entry >> 4 & 15];
            float red = _toFloat[entry & (_gammaSize - 1)];
            float y = red * .299f + green * .587f + blue * .114f;
            float i = red * .596f - green * .275f - blue * .321f;
            float q = red * .212f - green * .523f + blue * .311f;
            uint rgb = PackRgb((int)(y + _toRgb[0] * i + _toRgb[1] * q),
                (int)(y + _toRgb[2] * i + _toRgb[3] * q), (int)(y + _toRgb[4] * i + _toRgb[5] * q));
            if (setup.PaletteOut is { } palette)
            {
                uint clamped = Clamp(rgb);
                palette[entry * 3] = unchecked((byte)(clamped >> 21));
                palette[entry * 3 + 1] = unchecked((byte)(clamped >> 11));
                palette[entry * 3 + 2] = unchecked((byte)(clamped >> 1));
            }
            Span<uint> table = _table.AsSpan(entry * _entrySize, _entrySize);
            GenerateKernel(y, i, q, table);
            CorrectErrors(rgb, table);
        }
    }

    private void InitializeFilters(NtscSetup setup)
    {
        Span<float> scratch = stackalloc float[KernelSize * 2];
        Span<float> kernels = _rescaleOut > 1 ? scratch : _kernel;
        float rolloff = 1 + (float)setup.Sharpness * (float).032;
        const float maxH = 32;
        float power = (float)Math.Pow(rolloff, maxH);
        float toAngle = (float)setup.Resolution + 1;
        toAngle = Pi / maxH * (float)(_megaDrive ? .1974 : .20) * (toAngle * toAngle + 1);
        kernels[KernelSize * 3 / 2] = maxH;
        for (int i = 0; i < KernelHalf * 2 + 1; i++)
        {
            int x = i - KernelHalf;
            float angle = x * toAngle;
            if (x != 0 || power > (float)1.056 || power < (float).981)
            {
                float rolloffCos = rolloff * (float)Math.Cos(angle);
                float numerator = 1 - rolloffCos - power * (float)Math.Cos(maxH * angle)
                    + power * rolloff * (float)Math.Cos((maxH - 1) * angle);
                float denominator = 1 - rolloffCos - rolloffCos + rolloff * rolloff;
                kernels[KernelSize * 3 / 2 - KernelHalf + i] = numerator / denominator - (float).5;
            }
        }
        float sum = 0;
        for (int i = 0; i < KernelHalf * 2 + 1; i++)
        {
            float x = Pi * 2 / (KernelHalf * 2) * i;
            float window = .42f - .5f * (float)Math.Cos(x) + .08f * (float)Math.Cos(x * 2);
            int index = KernelSize * 3 / 2 - KernelHalf + i;
            sum += kernels[index] *= window;
        }
        sum = 1.0f / sum;
        for (int i = 0; i < KernelHalf * 2 + 1; i++) kernels[KernelSize * 3 / 2 - KernelHalf + i] *= sum;
        const float cutoffFactor = -.03125f;
        float cutoff = (float)setup.Bleed;
        if (cutoff < 0)
        {
            cutoff *= cutoff; cutoff *= cutoff; cutoff *= cutoff;
            cutoff *= -30.0f / .65f;
        }
        cutoff = cutoffFactor - .65f * cutoffFactor * cutoff;
        for (int i = -KernelHalf; i <= KernelHalf; i++) kernels[KernelSize / 2 + i] = (float)Math.Exp(i * i * cutoff);
        for (int i = 0; i < 2; i++)
        {
            sum = 0;
            for (int x = i; x < KernelSize; x += 2) sum += kernels[x];
            sum = 1.0f / sum;
            for (int x = i; x < KernelSize; x += 2) kernels[x] *= sum;
        }
        if (_rescaleOut > 1)
        {
            float weight = 1.0f;
            int output = 0;
            for (int n = _rescaleOut; n != 0; n--)
            {
                float remaining = 0;
                weight -= 1.0f / _rescaleIn;
                for (int i = 0; i < KernelSize * 2; i++)
                {
                    float current = kernels[i];
                    float scaled = current * weight;
                    _kernel[output++] = scaled + remaining;
                    remaining = current - scaled;
                }
            }
        }
    }

    private static ReadOnlySpan<float> DefaultDecoder => [.956f, .621f, -.272f, -.647f, -1.105f, 1.702f];
    private static uint PackRgb(int red, int green, int blue) => unchecked((uint)(red << 21 | green << 11 | blue << 1));
    private static uint Clamp(uint color)
    {
        unchecked
        {
            uint sub = color >> 9 & (RgbBuilder * 3 / 2);
            uint clamp = RgbBuilder * 0x101 - sub;
            color |= clamp;
            clamp -= sub;
            return color & clamp;
        }
    }
}

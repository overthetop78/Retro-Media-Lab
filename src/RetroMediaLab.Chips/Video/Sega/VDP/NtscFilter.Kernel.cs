using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Video.Sega.VDP;

public sealed partial class NtscFilter
{
    private void GenerateKernel(float y, float i, float q, Span<uint> output)
    {
        y -= RgbOffset;
        int outputIndex = 0;
        int count = _entrySize / _alignmentCount;
        for (int alignment = 0; alignment < _alignmentCount; alignment++)
        {
            int ntsc = -4 + alignment * 2;
            int scaled = -9 + alignment * 2;
            int kernelIndex;
            if (_rescaleIn > 1)
            {
                int adjusted = ntsc - scaled / _rescaleOut * _rescaleIn;
                int phase = (scaled + _rescaleOut * 10) % _rescaleOut;
                kernelIndex = KernelSize / 2 + adjusted + (phase != 0 ? 1 : 0)
                    + (_rescaleOut - phase) % _rescaleOut + KernelSize * 2 * phase;
            }
            else kernelIndex = KernelSize / 2 + ntsc - scaled;
            float negate = 1.0f - ((ntsc + 100) & 2);
            ReadOnlySpan<float> pixel = _megaDrive ? MegaDrivePixel : alignment switch { 0 => SmsPixel0, 1 => SmsPixel1, _ => SmsPixel2 };
            float yy = y * _fringing * negate;
            float ic0 = (i + yy) * pixel[0], qc1 = (q + yy) * pixel[1];
            float ic2 = (i - yy) * pixel[2], qc3 = (q - yy) * pixel[3];
            float factor = _artifacts * negate;
            float ii = i * factor;
            float yc0 = (y + ii) * pixel[0], yc2 = (y - ii) * pixel[2];
            float qq = q * factor;
            float yc1 = (y + qq) * pixel[1], yc3 = (y - qq) * pixel[3];
            for (int n = count; n != 0; n--)
            {
                int k = kernelIndex;
                float outI = _kernel[k] * ic0 + _kernel[k + 2] * ic2;
                float outQ = _kernel[k + 1] * qc1 + _kernel[k + 3] * qc3;
                float outY = _kernel[k + KernelSize] * yc0 + _kernel[k + KernelSize + 1] * yc1
                    + _kernel[k + KernelSize + 2] * yc2 + _kernel[k + KernelSize + 3] * yc3 + RgbOffset;
                if (_rescaleOut <= 1) kernelIndex--;
                else if (kernelIndex < KernelSize * 2 * (_rescaleOut - 1)) kernelIndex += KernelSize * 2 - 1;
                else kernelIndex -= KernelSize * 2 * (_rescaleOut - 1) + 2;
                int red = (int)(outY + _toRgb[0] * outI + _toRgb[1] * outQ);
                int green = (int)(outY + _toRgb[2] * outI + _toRgb[3] * outQ);
                int blue = (int)(outY + _toRgb[4] * outI + _toRgb[5] * outQ);
                output[outputIndex++] = unchecked(PackRgb(red, green, blue) - RgbBias);
            }
        }
    }

    private void CorrectErrors(uint color, Span<uint> output)
    {
        unchecked
        {
            if (_megaDrive)
            {
                for (int i = 0; i < 4; i++)
                {
                    uint error = color - output[i] - output[i + 18] - output[i + 4] - output[i + 22]
                        - output[i + 8] - output[(i + 10) % 16 + 16] - output[(i + 12) % 16] - output[(i + 14) % 16 + 16];
                    output[i + 22] += error;
                }
            }
            else
            {
                for (int i = 0; i < 7; i++)
                {
                    uint error = color - output[i] - output[(i + 12) % 14 + 14] - output[(i + 10) % 14 + 28]
                        - output[i + 7] - output[i + 19] - output[i + 31];
                    output[i + 31] += error;
                }
            }
        }
    }

    private static ReadOnlySpan<float> MegaDrivePixel => [.1f, .9f, .9f, .1f];
    private static ReadOnlySpan<float> SmsPixel0 => [1, 1, .6667f, 0];
    private static ReadOnlySpan<float> SmsPixel1 => [.3333f, 1, 1, .3333f];
    private static ReadOnlySpan<float> SmsPixel2 => [0, .6667f, 1, 1];
}

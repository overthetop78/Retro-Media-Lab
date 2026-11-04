namespace RetroMediaLab.Chips.IO.Acorn.SerialUla.BbcSerialUla.Constants;

public static class BbcSerialUlaConstants
{
    public const int MasterClock = 16_000_000;
    public const int ExternalClockDivider = 13;
    public const int CassetteClockDivider = 64;
    public const int LowToneDivider = 1024;
    public const int HighToneDivider = LowToneDivider / 2;
    public const int RateMask = 7;
    public const int ReceiveRateShift = 3;
    public const int DividerCounterMask = 255;
    public const byte Rs423Mask = 0x40;
    public const byte MotorMask = 0x80;
    public const byte ReadControlValue = 0xfe;
    public const byte ReadBusValue = 0;
    public const int MinimumHalfWaveRate = 1800;
    public const int MaximumHalfWaveRate = 8000;
    public const int HalfWaveThresholdNumerator = 3;
    public const int HalfWaveThresholdDenominator = 10000;
    public const int SilenceRate = 600;
    public const int CarrierHalfWaves = 836;
    public const int CarrierPulseRate = 4800;
    public const int SynthesisSteps = 16;
    public const short OutputAmplitude = 16000;
    internal static ReadOnlySpan<int> Dividers => [1, 16, 4, 128, 2, 64, 8, 256];
    // Nominal sine levels, not a measured DAC table for any particular ULA revision.
    internal static readonly short[] Sine = [0, 6123, 11314, 14782, 16000, 14782, 11314, 6123, 0, -6123, -11314, -14782, -16000, -14782, -11314, -6123];
}

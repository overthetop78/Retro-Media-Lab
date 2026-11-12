namespace RetroMediaLab.Chips.Audio.TexasInstruments.SN764xx.SN76489.Constants;

public static class Sn76489Constants
{
    public const int StateFormatVersion = 2;
    public const int TypicalReadyAssertNanoseconds = 90;
    public const int MaximumReadyAssertNanoseconds = 150;
    public const int ToneChannels = 3;
    public const int ChannelCount = 4;
    public const int NoiseChannel = 3;
    public const int LatchFlag = 0x80;
    public const int RegisterShift = 4;
    public const int RegisterMask = 7;
    public const int AttenuationRegisterFlag = 1;
    public const int ChannelShift = 1;
    public const int LowDataMask = 0x0f;
    public const int HighDataMask = 0x3f;
    public const int HighPeriodMask = 0x3f0;
    public const int PeriodDataShift = 4;
    public const int NoiseControlMask = 7;
    public const int NoiseRateMask = 3;
    public const int NoiseToneRate = 3;
    public const int WhiteNoiseFlag = 4;
    public const int NoiseFeedbackTap = 4;
    public const int WhiteNoiseFeedbackTap = 8;
    public const int NoiseOutputBit = 1;
    public const int FixedNoisePeriodShift = 5;
    public const int ToneNoisePeriodMultiplier = 2;
    public const int SilentAttenuation = 15;
    public const int AttenuationSteps = 16;
    public const double DecibelAmplitudeDivisor = 10;

    /// <summary>Un compteur de ton avance toutes les seize périodes de l'horloge d'entrée.</summary>
    public const int ClockDivider = 16;
    /// <summary>Durée nominale du transfert en périodes d'entrée, indépendamment de l'audio.</summary>
    public const int TransferClocks = 32;
    /// <summary>Sur la variante TI, un registre de période nul représente 1024, et non un ton arrêté.</summary>
    public const int ZeroPeriod = 1024;
    /// <summary>État initial du registre de bruit de la variante A, avec les étages de sortie (référence MAME).</summary>
    public const int NoiseSeed = 0x10000;
    /// <summary>Chaque voie reçoit au plus le quart de l'amplitude PCM, pour éviter l'écrêtage du mélange.</summary>
    public static readonly IReadOnlyList<int> Amplitudes = Array.AsReadOnly(
        Enumerable.Range(0, AttenuationSteps)
            .Select(value => value == SilentAttenuation ? 0
                : (int)Math.Round((double)short.MaxValue / ChannelCount
                    * Math.Pow(10, -value / DecibelAmplitudeDivisor)))
            .ToArray());

}

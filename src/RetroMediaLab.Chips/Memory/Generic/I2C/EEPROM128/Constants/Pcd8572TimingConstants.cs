namespace RetroMediaLab.Chips.Memory.Generic.I2C.EEPROM128.Constants;

/// <summary>Convention nominale DS20016E, table AC ; ce n'est pas une mesure du RC du Compact.</summary>
public static class Pcd8572TimingConstants
{
    public const int NominalWriteMicroseconds = 30_000;
    public const int DefaultReferenceFrequency = 1_000_000;
    internal const int MicrosecondsPerSecond = 1_000_000;
    internal const int WriteBufferBytes = 2;
}

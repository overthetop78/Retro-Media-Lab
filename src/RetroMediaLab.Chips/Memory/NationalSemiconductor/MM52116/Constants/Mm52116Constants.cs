namespace RetroMediaLab.Chips.Memory.NationalSemiconductor.MM52116.Constants;

public static class Mm52116Constants
{
    public const int Words = 2048;
    public const int AddressMask = Words - 1;
    public const byte SelectMask = 7;
    public const long MaximumAddressAccessNanoseconds = 450;
    public const long MaximumSelectAccessNanoseconds = 120;
    public const long MaximumOutputDisableNanoseconds = 100;
}

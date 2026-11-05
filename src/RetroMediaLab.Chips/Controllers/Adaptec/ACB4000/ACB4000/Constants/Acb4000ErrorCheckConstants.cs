namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;

/// <summary>ECC configuré dans l'AIC-010 par le firmware 400108-00, table 1FC7.</summary>
public static class Acb4000ErrorCheckConstants
{
    public const uint Polynomial = 0x41044185;
    public const uint InitialValue = 0;
    public const uint HighBit = 0x80000000;
    public const int BitsPerByte = 8;
    public const int CheckByteCount = sizeof(uint);
    public const int CheckBitCount = CheckByteCount * BitsPerByte;
    public const int MaximumBurstBits = 8;
    public const uint BurstMask = (1U << MaximumBurstBits) - 1;
    public const int SyncAndMarkByteCount = 2;
    public const int MaximumRecordLength = Acb4000ParameterConstants.MaximumBlockSize + SyncAndMarkByteCount + CheckByteCount;
}

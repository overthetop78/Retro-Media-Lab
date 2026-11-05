namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;

/// <summary>Compteur de phase AIC-250, exprimé en temps de bits NRZ.</summary>
internal static class Acb4000ReadSyncConstants
{
    internal const int AcquisitionBits = 16;
    internal const int AddressSearchBits = 64;
    internal const int SearchLimitBits = 128;
    internal const int OscillatorBits = 32;
    internal const int AcquisitionCells = AcquisitionBits * Acb4000MfmConstants.CellsPerBit;
    internal const int ReadDelayBits = 4;
    internal const int ReadDelayCells = ReadDelayBits * Acb4000MfmConstants.CellsPerBit;
    internal const uint ReadDelayOutputMask = 1u << (ReadDelayCells - 1);
}

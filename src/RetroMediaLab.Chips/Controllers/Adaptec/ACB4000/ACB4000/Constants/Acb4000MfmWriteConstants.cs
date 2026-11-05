namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;

/// <summary>Sortie MFM numérique de l'ENDEC AIC-250, fiche constructeur page 5.</summary>
public static class Acb4000MfmWriteConstants
{
    public const int DelayBits = 3;
    public const int CellsPerBit = 2;
    public const int DelayCells = DelayBits * CellsPerBit;
    public const int ClockCellShift = 1;
    public const byte PairMask = 3;
    public const uint DelayMask = (1u << DelayCells) - 1;
}

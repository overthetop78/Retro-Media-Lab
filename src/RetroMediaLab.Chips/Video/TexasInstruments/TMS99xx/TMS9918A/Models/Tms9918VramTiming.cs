namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Models;

/// <summary>Profil de transport provisoire explicité dans la fiche TMS9918A.
/// Ce n'est pas un chronogramme mesuré de la révision A. La clock native reste
/// inchangée ; la réserve de 2,5 ns environ sur CAS haut n'est pas cachée.</summary>
public static class Tms9918VramTiming
{
    public const bool ElectricallyCharacterized = false;
    public const bool AddressPermutationCharacterized = false;
    public const int RasAssert = 45, CasAssert = 85, Column = 85;
    public const int WriteData = 189, WriteAssert = 195;
    public const int RasRelease = 270, CasRelease = 335, WriteRelease = 385;
    public const int CpuPreparationHalfEdges = 32;
    public const int RamLatchHalfEdges = 12;
}

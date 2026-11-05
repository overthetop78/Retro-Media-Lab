using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000FieldConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

/// <summary>Adresse logique et drapeaux d'un champ ID physique ; aucune adresse CHS n'est inventée.</summary>
public readonly record struct Acb4000IdField(uint LogicalAddress, byte Flags, bool IsDefective)
{
    public bool IsLastSector => (Flags & C.LastSectorMask) != 0;
    public int DefectCount => Flags & C.DefectCountMask;
}

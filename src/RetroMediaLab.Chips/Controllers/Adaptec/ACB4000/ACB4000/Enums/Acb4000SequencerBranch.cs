namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;

/// <summary>Les conditions 1–3 arrêtent, 4–7 vont au registre de branchement ; l'ECC en lecture change leur prédicat.</summary>
public enum Acb4000SequencerBranch : byte
{
    Continue = 0,
    StopOnInputOrEccError = 1,
    StopOnIndexOrMismatch = 2,
    StopOnMismatchOrReadError = 3,
    BranchOnCarryOrGoodRead = 4,
    BranchOnInputOrEccError = 5,
    BranchOnIndexOrMismatch = 6,
    BranchOnMismatchOrReadError = 7
}

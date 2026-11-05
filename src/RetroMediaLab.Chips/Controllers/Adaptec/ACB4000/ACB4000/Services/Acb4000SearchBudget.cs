using F = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000FormatConstants;
using S = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000SearchConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

/// <summary>Budget des identités de piste ; distinct du délai d'attente sans événement du séquenceur.</summary>
public sealed class Acb4000SearchBudget
{
    public int Remaining { get; private set; }

    public void Begin(int sectorsPerTrack)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sectorsPerTrack);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(sectorsPerTrack, F.SmallBlockInterleavedSectors);
        Remaining = sectorsPerTrack * S.TrackAttempts;
    }

    /// <summary>À épuisement, le statut de la dernière identité détermine le code, comme B en 16A1.</summary>
    public byte? ObserveId(bool checkValid)
    {
        if (Remaining > 0) Remaining--;
        return Remaining == 0 ? checkValid ? S.SectorNotFound : S.IdDataCheck : null;
    }

    public void Reset() => Remaining = 0;
}

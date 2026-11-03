namespace RetroMediaLab.Chips.Controllers.WesternDigital.WD177x.WD1770.Enums;

/// <summary>Reconnaissance interne du détecteur de marques ; ces valeurs ne sont pas des registres CPU.</summary>
internal enum Wd1770ReadWordKind
{
    Data,
    SyncA1,
    SyncC2,
    AddressMark,
    IndexMark
}

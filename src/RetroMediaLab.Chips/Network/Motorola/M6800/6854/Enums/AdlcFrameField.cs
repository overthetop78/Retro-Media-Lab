namespace RetroMediaLab.Chips.Network.Motorola.M6800.Chip6854.Enums;

/// <summary>Champs successifs d’une trame ADLC, hors flags et FCS.</summary>
internal enum AdlcFrameField
{
    Address,
    Control,
    ExtendedControl,
    LogicalControl,
    Information
}

namespace RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Enums;

/// <summary>Broches de contrôle actuellement pilotées par le VIA ; les autres sont des entrées.</summary>
[Flags]
public enum ViaControlOutputs
{
    None = 0,
    Ca2 = 1,
    Cb1 = 2,
    Cb2 = 4
}

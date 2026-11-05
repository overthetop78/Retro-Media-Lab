namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;

/// <summary>Assertions MSG/C-D/I-O des phases d'information ; les états libres ne conduisent aucun de ces fils.</summary>
public enum Acb4000BusPhase : byte
{
    DataOut = 0,
    DataIn = 1,
    Command = 2,
    Status = 3,
    MessageOut = 6,
    MessageIn = 7,
    BusFree = 8,
    Selected = 9
}

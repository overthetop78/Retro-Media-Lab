namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;

/// <summary>Comportements communs conservés sans caractérisation de cette variante.
/// Ces réserves ne déclenchent aucune différence matérielle inventée.</summary>
[Flags]
public enum Z80BehaviorReservations
{
    None = 0, SpecialReset = 1, HaltBusDrive = 2,
    IrReadInterruptParity = 4, OtherUndocumentedFlags = 8,
}

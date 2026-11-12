using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;

namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Models;

/// <summary>Configuration physique immuable de l'unique moteur ; aucune instruction,
/// horloge, mémoire ou continuation n'appartient au profil.</summary>
public sealed record Z80HardwareProfile(Z80SiliconVariant Silicon, Z80CarryFlagSource CarryFlagSource,
    byte UndocumentedOutCValue, bool IrReadInterruptClearsParity, Z80BehaviorReservations Reservations)
{
    /// <summary>Zilog NMOS d'origine ; le grade de vitesse est choisi séparément.</summary>
    public static Z80HardwareProfile ZilogNmos { get; } = new(Z80SiliconVariant.ZilogNmos,
        Z80CarryFlagSource.AccumulatorWithQHistory, 0, true, Z80BehaviorReservations.None);

    internal void Validate()
    {
        if (!Enum.IsDefined(Silicon) || !Enum.IsDefined(CarryFlagSource)
            || (Reservations & ~(Z80BehaviorReservations.SpecialReset | Z80BehaviorReservations.HaltBusDrive
                | Z80BehaviorReservations.IrReadInterruptParity | Z80BehaviorReservations.OtherUndocumentedFlags)) != 0)
            throw new ArgumentException("Invalid Z80 hardware profile.");
    }
}

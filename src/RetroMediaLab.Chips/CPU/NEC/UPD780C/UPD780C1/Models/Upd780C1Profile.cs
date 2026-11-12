using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Models;

namespace RetroMediaLab.Chips.CPU.NEC.UPD780C.UPD780C1.Models;

/// <summary>NEC µPD780C-1 : profil du CpuZ80 commun, jamais un second CPU.</summary>
public static class Upd780C1Profile
{
    // SCF/CCF : observation NMOS NEC de David Banks. Les points non caractérisés
    // restent inconnus pour NEC : modèle provisoire, non qualifié matériellement.
    // RESET normal : catalogue NEC 1982, p.393, retenu par l'utilisateur :
    // adresse/données flottantes et commandes inactives, déjà assurées par le
    // séquenceur commun. Cette qualification ne couvre ni HALT ni le reset court.
    public static Z80HardwareProfile Hardware { get; } = new(Z80SiliconVariant.NecNmos,
        Z80CarryFlagSource.Accumulator, 0, true,
        Z80BehaviorReservations.SpecialReset | Z80BehaviorReservations.HaltBusDrive
        | Z80BehaviorReservations.IrReadInterruptParity | Z80BehaviorReservations.OtherUndocumentedFlags);

    /// <summary>Maxima NEC µPD780-1, fiche pages 4-8/4-9 : +5 V ±5 %, 0–70 °C.
    /// Ces limites ne sont ni une mesure SG-1000 ni des valeurs typiques.</summary>
    public static Z80BusTiming Timing { get; } = new(
        110, 150, 90, 90, 80, 100, 100, 85, 85, 85, 95, 85,
        75, 85, 85, 65, 80, 80, 130, 120, 250, 110, 110, 300, 100, 100, 85, 85);
}

namespace RetroMediaLab.Chips.CPU.Motorola.M6800.Chip6809.Interfaces;

/// <summary>Référence de carte au pas EXTAL du MC6809, quatre fois la fréquence E nominale.</summary>
/// <remarks>Raccordement facultatif pour les cartes qui doivent avancer pendant un étirement MRDY
/// au quart de période. Dans ce cas le CPU appelle TickInputClock à la place de ICpuBus.Tick,
/// jamais les deux. La carte cadence les récepteurs d'E/Q par leurs notifications et ses autres
/// domaines par cette référence ; elle ne consulte pas leurs sorties en boucle.</remarks>
public interface ICpu6809ClockBus
{
    void TickInputClock();
}

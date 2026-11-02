using RetroMediaLab.Chips.IO.ChipsAndTechnologies.SuperIO.Chip82C711.Enums;
using C = RetroMediaLab.Chips.IO.ChipsAndTechnologies.SuperIO.Chip82C711.Constants.Upc82C711Constants;

namespace RetroMediaLab.Chips.IO.ChipsAndTechnologies.SuperIO.Chip82C711.Interfaces;
/// <summary>Contrat matériel du composant Upc82C711Configuration ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IUpc82C711Configuration
{
    bool IsUnlocked { get; }

    void PowerOn();
    byte Read(int address);
    bool Write(int address, byte value);
    bool TryDecode(int address, out UpcPeripheral peripheral, out int register);
}

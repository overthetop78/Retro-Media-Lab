namespace RetroMediaLab.Chips.Memory.Generic.I2C.EEPROM128.Interfaces;
/// <summary>Contrat matériel du composant CompactEeprom ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface ICompactEeprom
{
    byte ReadPort { get; }

    void WritePort(byte value);
    byte[] Export();
    void Import(ReadOnlySpan<byte> bytes);
}

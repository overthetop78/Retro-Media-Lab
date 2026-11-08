namespace RetroMediaLab.Chips.Memory.Generic.I2C.EEPROM128.Interfaces;
/// <summary>Contrat matériel du composant CompactEeprom ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface ICompactEeprom
{
    byte ReadPort { get; }
    long ReferenceFrequency { get; }
    long ClockCount { get; }
    long? NextClockEvent { get; }
    bool Writing { get; }
    event Action<long?>? NextClockEventChanged;
    void AdvanceTo(long clock);

    void WritePort(byte value);
    byte[] Export();
    void Import(ReadOnlySpan<byte> bytes);
}

using RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Contracts;

namespace RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Interfaces;

/// <summary>Contrat matériel du composant Adc7002 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IAdc7002
{
    /// <summary>Niveau physique de la sortie EOC, active basse.</summary>
    bool EndOfConversionHigh { get; }
    /// <summary>Bit de drapeau logiciel renvoyé par le registre de statut.</summary>
    bool Flag { get; }
    bool Converting { get; }
    int InputClockFrequency { get; }
    int ConversionClockFrequency { get; }
    long InputClockCount { get; }
    long? NextClockEvent { get; }
    Action<long?>? NextClockEventChanged { get; set; }
    void AdvanceTo(long inputClockCount);
    bool TestMode { get; }
    void Reset();
    Adc7002State CaptureState();
    void RestoreState(Adc7002State state);
    void SetChannel(int channel, ushort value);
    byte Read(int register);
    void Write(int register, byte value);
    /// <summary>
    /// Applique les niveaux physiques CS/RD/WR (actifs bas), l'adresse et les données d'entrée.
    /// Renvoie null si le bus de sortie n'est pas piloté. Les accès Read/Write restent des cycles complets.
    /// </summary>
    byte? SetBus(bool chipSelectHigh, bool readHigh, bool writeHigh, int register, byte input);
    void Tick();
    /// <summary>Avance l'horloge appelante, sans modifier sa fréquence.</summary>
    void Advance(int cycles);
    /// <summary>Un cycle complet de l'horloge de conversion externe, si sa fréquence automatique est nulle.</summary>
    void Clock();
}

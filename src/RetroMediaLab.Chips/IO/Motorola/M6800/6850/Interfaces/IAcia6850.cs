namespace RetroMediaLab.Chips.IO.Motorola.M6800.Chip6850.Interfaces;
/// <summary>Contrat matériel du composant Acia6850 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IAcia6850
{
    bool TransmitBit { get; }
    /// <summary>Niveau physique de RTS : false correspond a la demande active basse.</summary>
    bool RtsHigh { get; }

    int BaudRateOverride { get; set; }

    bool Receiving { get; }

    int BaudRate { get; }

    bool Irq { get; }

    void Reset();
    byte Read(bool data);
    void Write(bool data, byte value);
    void SetDcd(bool high);
    void SetCts(bool blocked);
    void BeginReceive();
    void CancelReceive();
    void ReceiveBit(bool value);
    /// <summary>Front montant de l'horloge RxC, RXD stable.</summary>
    void ClockReceive(bool value);
    /// <summary>Front descendant de l'horloge TxC.</summary>
    void ClockTransmit();
    void Tick();
}

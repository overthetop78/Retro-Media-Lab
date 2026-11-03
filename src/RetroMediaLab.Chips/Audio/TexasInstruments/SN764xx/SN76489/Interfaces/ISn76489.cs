namespace RetroMediaLab.Chips.Audio.TexasInstruments.SN764xx.SN76489.Interfaces;
/// <summary>Contrat matériel du composant Sn76489 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface ISn76489
{
    /// <summary>Somme mono instantanée, avant le couplage analogique de la carte.</summary>
    int Output { get; }
    /// <summary>True lorsque la sortie READY à collecteur ouvert est relâchée.</summary>
    bool Ready { get; }

    void Reset();
    /// <summary>Transfert déjà validé par le propriétaire du bus.</summary>
    void Write(byte value);
    /// <summary>Maintient les données et CE/WE jusqu'au prochain appel ; true signifie une broche active basse.</summary>
    void SetBus(byte value, bool chipSelected, bool writeEnabled);
    void Tick();
}

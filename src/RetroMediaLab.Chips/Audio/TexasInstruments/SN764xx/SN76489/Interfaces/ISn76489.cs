namespace RetroMediaLab.Chips.Audio.TexasInstruments.SN764xx.SN76489.Interfaces;
/// <summary>Contrat matériel du composant Sn76489 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface ISn76489
{
    int Output { get; }

    void Reset();
    void Write(byte value);
    void Tick();
}

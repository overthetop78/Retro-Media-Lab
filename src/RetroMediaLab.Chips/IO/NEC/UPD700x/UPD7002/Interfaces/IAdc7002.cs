using C = RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Constants.Adc7002Constants;

namespace RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Interfaces;
/// <summary>Contrat matériel du composant Adc7002 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IAdc7002
{
    void Reset();
    void SetChannel(int channel, ushort value);
    byte Read(int register);
    void Write(int register, byte value);
    void Tick();
}

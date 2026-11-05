namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Interfaces;

/// <summary>Registres matériels empruntés par la carte ACB-4000, avec leurs effets de lecture/écriture.</summary>
/// <remarks>La RAM des contextes appartient à la carte. Ce bus n'est ni initialisé ni libéré par elle.</remarks>
public interface IAcb4000RegisterBus
{
    byte Read(ushort address);
    void Write(ushort address, byte value);
}

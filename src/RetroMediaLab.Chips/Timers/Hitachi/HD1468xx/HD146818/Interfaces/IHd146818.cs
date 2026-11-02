using R = RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Constants.MasterClockConstants;

namespace RetroMediaLab.Chips.Timers.Hitachi.HD1468xx.HD146818.Interfaces;
/// <summary>Contrat matériel du composant Hd146818 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IHd146818
{
    byte ReadRegister(int address);
    void WriteRegister(int address, byte value);
    byte[] Export();
    void Import(ReadOnlySpan<byte> bytes);
}

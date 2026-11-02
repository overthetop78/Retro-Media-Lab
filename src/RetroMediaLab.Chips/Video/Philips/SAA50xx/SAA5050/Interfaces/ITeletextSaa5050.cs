namespace RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Interfaces;
/// <summary>Contrat matériel du composant TeletextSaa5050 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface ITeletextSaa5050
{
    void Reset();
    void BeginLine(int row);
    (int Mask, int Foreground, int Background) Character(int data, int raster, long frame);
}

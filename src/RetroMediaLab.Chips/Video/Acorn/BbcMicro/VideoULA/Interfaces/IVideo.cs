using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Services;
using RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Services;
using System.Runtime.InteropServices;
using VideoConstants = RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Constants.VideoConstants;

namespace RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Interfaces;
/// <summary>Contrat matériel du composant Video ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IVideo
{
    int Height { get; }

    int ScreenWrap { get; set; }

    bool Monochrome { get; set; }

    Func<int, byte>? ReadMemory { get; set; }

    bool VSync { get; }

    long Frame { get; }

    void Reset();
    void Select(byte value);
    void WriteRegister(byte value);
    byte ReadRegister();
    void WriteUla(ushort address, byte value);
    void Tick();
    byte[] Render();
}

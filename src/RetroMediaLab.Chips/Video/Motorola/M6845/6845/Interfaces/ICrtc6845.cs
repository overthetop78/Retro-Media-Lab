using RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Enums;

namespace RetroMediaLab.Chips.Video.Motorola.M6845.Chip6845.Interfaces;
/// <summary>Contrat matériel du composant Crtc6845 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface ICrtc6845
{
    Crtc6845Model Model { get; }

    bool TestMode { get; }

    void SetReset(bool asserted);

    int MemoryAddress { get; }

    void SetLightPen(bool level);

    bool DataBusDriven { get; }

    byte? DataBusValue { get; }

    void SetBus(bool selected, bool registerSelect, bool read, bool enable, byte data);

    int Column { get; }

    int Row { get; }

    int Raster { get; }

    int Scanline { get; }

    long Frame { get; }

    bool InterlaceVideo { get; }

    int RasterAddress { get; }

    int DisplayColumn { get; }

    bool DisplayEnabled { get; }

    int Address { get; }

    bool VSync { get; }

    bool HSync { get; }

    bool IsProgrammed { get; }

    void Select(byte value);
    byte Read();
    void Write(byte value);
    bool Cursor { get; }

    bool CursorForCell(int address);
    bool Tick();
    void Reset();
}

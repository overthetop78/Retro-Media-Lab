using RetroMediaLab.Chips.Video.Acorn.Archimedes.VIDC.Enums;
using C = RetroMediaLab.Chips.Video.Acorn.Archimedes.VIDC.Constants.ArchimedesVidcConstants;
using Board = RetroMediaLab.Chips.Custom.Acorn.Archimedes.Hardware.Constants.ArchimedesHardwareConstants;

namespace RetroMediaLab.Chips.Video.Acorn.Archimedes.VIDC.Interfaces;
/// <summary>Contrat matériel du composant ArchimedesVidc ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface IArchimedesVidc
{
    uint Control { get; }

    int BitsPerPixel { get; }

    bool VideoDmaRequest { get; }

    bool CursorDmaRequest { get; }

    bool SoundDmaRequest { get; }

    bool Flyback { get; }

    bool HighResolutionMonochrome { get; set; }

    Action<bool>? FlybackChanged { get; set; }

    Action? VideoFrameStarted { get; set; }

    Action? CursorFrameStarted { get; set; }

    long Frame { get; }

    int Raster { get; }

    int HorizontalPixel { get; }

    int? FrameMasterClocks { get; }

    double? FramesPerSecond { get; }

    void Reset();
    void Write(uint value);
    void SupplyVideoBurst(ReadOnlySpan<uint> words);
    void SupplyCursorBurst(ReadOnlySpan<uint> words);
    void SupplySoundBurst(ReadOnlySpan<uint> words);
    void AdvanceMasterClocks(int clocks);
    (short Left, short Right) TakeAudioSample();
    byte[] CompletedFrame { get; }

    int Width { get; }

    int Height { get; }
}

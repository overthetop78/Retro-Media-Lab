using RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Enums;

namespace RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Interfaces;

/// <summary>Entrées et sorties du VDG ; le bus et le récepteur vidéo appartiennent à la carte appelante.</summary>
public interface IMc6847
{
    byte Data { get; set; }
    Mc6847Mode Mode { get; set; }
    bool MemorySelect { get; set; }
    ushort? Address { get; }
    Action? DataRequested { get; set; }
    bool Clock { get; }
    bool HorizontalSync { get; }
    bool FieldSync { get; }
    /// <summary>Nouveau niveau de FS à son front matériel ; le destinataire initialise son entrée avec FieldSync.</summary>
    Action<bool>? FieldSyncChanged { get; set; }
    bool RowPreset { get; }
    bool Active { get; }
    int HalfClock { get; }
    int Scanline { get; }
    int CharacterRow { get; }
    long Field { get; }
    Mc6847Colour Colour { get; }
    int LuminanceMillivolts { get; }
    int ChromaAMillivolts { get; }
    int ChromaBMillivolts { get; }
    int ChromaBiasMillivolts { get; }
    void SetClock(bool level);
    void Reset();
}

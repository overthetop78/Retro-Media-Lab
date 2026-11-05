namespace RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Interfaces;

/// <summary>VIDPROC seul : les autres circuits et la mémoire appartiennent à la carte appelante.</summary>
public interface IBbcVideoUla
{
    byte Data { get; set; }
    bool DisplayEnabled { get; set; }
    bool Cursor { get; set; }
    int TeletextRgb { get; set; }
    bool Inverted { get; set; }
    byte Control { get; }
    /// <summary>Présente DATA, CURSOR et DE ensemble, sans produire de front d'horloge.</summary>
    void SetCharacterInputs(byte data, bool cursor, bool displayEnabled);
    bool TeletextSelected { get; }
    bool FastCrtc { get; }
    bool Clock8 { get; }
    bool Clock4 { get; }
    bool Clock2 { get; }
    bool Clock1 { get; }
    bool CrtcClock { get; }
    bool CursorActive { get; }
    int Rgb { get; }
    bool Red { get; }
    bool Green { get; }
    bool Blue { get; }
    void Write(ushort address, byte value);
    void SetMasterClock(bool level);
    /// <summary>Huit périodes maître complètes ; RGB externe fourni pour chaque période, trois bits par couleur.</summary>
    uint ClockMasterPeriods(uint inputRgb);
    void Reset();
}

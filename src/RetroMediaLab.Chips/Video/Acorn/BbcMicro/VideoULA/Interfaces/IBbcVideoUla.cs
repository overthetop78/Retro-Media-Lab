namespace RetroMediaLab.Chips.Video.Acorn.BbcMicro.VideoULA.Interfaces;

/// <summary>VIDPROC seul : la mémoire vidéo, le CRTC et le SAA5050 appartiennent à la carte appelante.</summary>
public interface IBbcVideoUla
{
    byte Data { get; set; }
    bool DisplayEnabled { get; set; }
    bool Cursor { get; set; }
    int TeletextRgb { get; set; }
    bool Inverted { get; set; }
    byte Control { get; }
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
    void Reset();
}

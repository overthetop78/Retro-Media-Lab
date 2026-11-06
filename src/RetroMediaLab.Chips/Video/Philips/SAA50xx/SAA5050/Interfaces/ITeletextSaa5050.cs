using RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Contracts;

namespace RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Interfaces;
/// <summary>Contrat matériel du composant TeletextSaa5050 ; aucune ressource hôte n'est possédée par ce contrat.</summary>
public interface ITeletextSaa5050
{
    TeletextSaa5050State CaptureState();
    void RestoreState(TeletextSaa5050State state);
    bool PictureOn { get; set; }
    bool DisplayEnabled { get; set; }
    bool SuperimposeInputHigh { get; set; }
    bool TextMode { get; }
    bool Reveal { get; }
    bool Superimpose { get; }
    bool SuperimposeDriveLow { get; }
    bool Boxed { get; }
    byte Data { get; set; }
    bool CharacterRoundingSelect { get; set; }
    bool BigCharacterSelectHigh { get; set; }
    bool RemoteData { get; set; }
    int CharacterRow { get; }
    int CharacterScanline { get; }
    int FlashField { get; }
    bool TransmittedLargeCharacterHigh { get; }
    int Rgb { get; }
    bool Red { get; }
    bool Green { get; }
    bool Blue { get; }
    bool Monochrome { get; }
    bool Blanking { get; }
    void SetCharacterClock(bool level);
    void SetDotClock(bool level);
    /// <summary>Transitions TR6 sans changement des autres broches ; RGB successifs, trois bits par transition.</summary>
    ulong ClockHalfDots(int count);
    void SetLoadOutputShiftEnable(bool level);
    void SetDataEntryWindow(bool level);
    void SetGeneralLineReset(bool level);
    void SetRemoteClock(bool level);
    void Reset();
    void BeginLine(int row);
    (int Mask, int Foreground, int Background) Character(int data, int raster, long frame);
}

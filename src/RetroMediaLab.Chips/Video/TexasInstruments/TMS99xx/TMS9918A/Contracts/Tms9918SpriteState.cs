namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

/// <summary>FIFO réelle de cinq numéros, canaux en préparation puis en sortie.
/// Les tableaux de capture sont copiés et ne doivent pas servir de mémoire VRAM.</summary>
public sealed record Tms9918SpriteState(int ScanLine, int SelectedCount,
    bool ScanTerminated, bool ScanKnown, int[] SelectedNumbers,
    int PreparingLine, bool PreparingKnown, Tms9918SpriteChannelState[] Preparing,
    int ActiveLine, bool ActiveKnown, Tms9918SpriteChannelState[] Active);

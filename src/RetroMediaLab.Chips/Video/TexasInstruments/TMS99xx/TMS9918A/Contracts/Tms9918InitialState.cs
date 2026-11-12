namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

/// <summary>Graines explicites de l'interface ; aucune valeur d'allumage inconnue
/// n'est présentée comme garantie du constructeur. L'état complet viendra avec le balayage.</summary>
public readonly record struct Tms9918InitialState(
    Tms9918RegisterState Registers, byte Status, Tms9918ControlLatchState Control,
    Tms9918CpuMemoryState Memory, Tms9918CpuPins CpuPins,
    Tms9918ClockState Clock = default, Tms9918RasterState Raster = default);

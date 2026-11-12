namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

/// <summary>Adresse formée dans le VDP ; Qualified ne transforme pas une
/// configuration hors spécification en une adresse matériellement prouvée.</summary>
internal readonly record struct Tms9918FetchAddress(ushort Address, bool Qualified);

namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

/// <summary>Determined output waveform of one complete VRAM read. All dates
/// are produced by the VDP; connected memories retain their own timing/state.</summary>
public readonly record struct Tms9918VramReadCycle(long RowTime, long RasFall, long CasFall,
    long RasRise, long CasRise, byte Row, byte Column, bool AddressKnown);

public delegate bool Tms9918VramReadCycleReceiver(in Tms9918VramReadCycle cycle);

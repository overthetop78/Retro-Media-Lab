namespace RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Contracts;

/// <summary>A received, finite read waveform. Dates and DIN levels come from
/// the actual wired producer, not from an oscillator or guessed memory access.
/// The consumer keeps all physical state; this is interruptible transport.</summary>
public readonly record struct Mb8118ReadCycle(long RowTime, long RasFall, long CasFall,
    long RasRise, long CasRise, byte Row, byte Column, bool RowKnown, bool ColumnKnown,
    bool? RowData, bool? ColumnData);

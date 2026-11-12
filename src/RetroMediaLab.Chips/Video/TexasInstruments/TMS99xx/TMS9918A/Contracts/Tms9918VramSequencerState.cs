namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

/// <summary>Un cycle de bus, son éventuelle fin R/W et le verrou de résultat
/// retardé. Les destinations sont les valeurs sérialisées de VramSlotKind.</summary>
public readonly record struct Tms9918VramSequencerState(long? Due, long? WriteEnd,
    long Started, long StartedHalfEdge, long CpuReadyHalfEdge, int Step, bool Active,
    bool Write, bool AddressKnown, byte Row, byte Column, byte WriteData, byte RefreshRow,
    int SlotKind, int SlotIndex, int RamSlotKind, int RamSlotIndex,
    bool RamLoadActive, bool RamDataKnown, bool RamCpuWrite, byte RamData,
    long RamLoadHalfEdge, bool CpuInService);

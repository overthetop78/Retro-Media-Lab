namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

/// <summary>État du VDP, sans sa VRAM extérieure et sans délégué de câblage.
/// Les composants de la carte et leurs niveaux doivent être restaurés ensemble.</summary>
public sealed record Tms9918AState(int Version, long TimeNanoseconds,
    Tms9918RegisterState Registers, byte Status, Tms9918ControlLatchState Control,
    Tms9918CpuMemoryState Memory, Tms9918CpuInterfaceState CpuInterface,
    Tms9918CpuWriteState CpuWriteBus, Tms9918StatusReadState StatusReadBus,
    Tms9918CpuOutputs CpuOutputs, Tms9918ClockState Clock, Tms9918RasterState Raster,
    Tms9918VideoSignal VideoSignal, Tms9918VramBusState VramBus,
    Tms9918VramSequencerState VramSequencer, Tms9918BackgroundState Background,
    Tms9918SpriteState Sprites, Tms9918ExternalVideoState ExternalVideo,
    long CpuAccessNanoseconds, long CpuReleaseNanoseconds, byte SpriteCounter);

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Contracts;

internal sealed record CassetteState
{
    public required EmulationCassetteSignal? Signal { get; init; }
    public required int Position { get; init; }
    public required int SamplePhase { get; init; }
    public required bool InputLevel { get; init; }
    public required bool InputPresented { get; init; }
    public required int PresentedPosition { get; init; }
    public required int SampleIncrement { get; init; }
    public required short[]? Recorded { get; init; }
    public required bool Writable { get; init; }
    public required EmulationCassetteSettings Settings { get; init; }
    public required int? SeekTarget { get; init; }
    public required EmulationCassetteCommand? Operation { get; init; }
    public required long LatencyTicks { get; init; }
    public required bool ClockRequired { get; init; }
    public required bool PublishedActive { get; init; }
    public required EmulationCassetteCommand? PublishedOperation { get; init; }
    public required EmulationCassetteState PublishedState { get; init; }
    public required bool HasChanges { get; init; }
    public required EmulationCassetteState State { get; init; }
}

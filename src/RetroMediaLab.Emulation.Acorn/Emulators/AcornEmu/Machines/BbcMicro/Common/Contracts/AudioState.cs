using RetroMediaLab.Chips.Audio.TexasInstruments.SN764xx.SN76489.Contracts;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Contracts;

/// <summary>Signal sonore de la carte et intégration PCM, sans périphérique audio hôte.</summary>
internal sealed record AudioState
{
    public required int FormatVersion { get; init; }
    public required Sn76489State Chip { get; init; }
    public required short[] Samples { get; init; }
    public required int Phase { get; init; }
    public required int Level { get; init; }
    public required int PendingTicks { get; init; }
    public required int ClockCountdown { get; init; }
    public required long Area { get; init; }
    public required double PreviousInput { get; init; }
    public required double PreviousOutput { get; init; }
}

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants;

/// <summary>Cadence de carte et paramètres du magnétophone ; les seuils électroniques sont dans SERPROC.</summary>
internal static class CassetteConstants
{
    internal const int CpuClock = (int)BbcMicroModelConstants.CpuFrequencyHz;
    internal const int ExternalAciaClock = 0;
    /// <summary>Présence enregistrée du magnétophone externe ; la prise de la carte reste présente indépendamment.</summary>
    internal const string ConnectedOption = "acornemu.cassette-connected";
    internal const int RecordingSampleRate = 48000;
    /// <summary>Formats de signal réellement pris en charge, partagés par la fabrique et le cœur.</summary>
    internal static readonly IReadOnlyList<string> Extensions = Array.AsReadOnly<string>([".uef", ".csw", ".wav", ".raw"]);
    internal static readonly IReadOnlySet<EmulationCassetteCommand> Commands = new HashSet<EmulationCassetteCommand> { EmulationCassetteCommand.Play, EmulationCassetteCommand.Stop, EmulationCassetteCommand.Pause, EmulationCassetteCommand.Rewind, EmulationCassetteCommand.FastForward };
    internal static readonly IReadOnlySet<EmulationCassetteCommand> RecordingCommands = new HashSet<EmulationCassetteCommand>(Commands) { EmulationCassetteCommand.Record };
}

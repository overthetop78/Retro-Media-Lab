namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Constants;

/// <summary>Cadence de carte et paramètres du magnétophone ; les seuils électroniques sont dans SERPROC.</summary>
internal static class TapeConstants
{
    internal const int CpuClock = (int)BbcMicroModelConstants.CpuFrequencyHz;
    internal const int ExternalAciaClock = 0;
    /// <summary>Présence enregistrée du magnétophone externe ; la prise de la carte reste présente indépendamment.</summary>
    internal const string ConnectedOption = "acornemu.tape-connected";
    internal const int RecordingSampleRate = 48000;
    internal const int NormalSpeedPercent = 100;
    /// <summary>Formats de signal réellement pris en charge, partagés par la fabrique et le cœur.</summary>
    internal static readonly IReadOnlyList<string> Extensions = Array.AsReadOnly<string>([".uef", ".csw", ".wav", ".raw"]);
    internal static readonly IReadOnlySet<EmulationTapeCommand> Commands = new HashSet<EmulationTapeCommand> { EmulationTapeCommand.Play, EmulationTapeCommand.Stop, EmulationTapeCommand.Pause, EmulationTapeCommand.Rewind, EmulationTapeCommand.FastForward };
    internal static readonly IReadOnlySet<EmulationTapeCommand> RecordingCommands = new HashSet<EmulationTapeCommand>(Commands) { EmulationTapeCommand.Record };
}

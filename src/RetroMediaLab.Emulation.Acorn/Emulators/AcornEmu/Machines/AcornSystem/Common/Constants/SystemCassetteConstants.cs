namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Constants;

internal static class SystemCassetteConstants
{
    internal const int MarkFrequency = 2400;
    internal const int SpaceFrequency = 1200;
    internal const int HalfWavesPerCycle = 2;
    internal const int HalfWaveThresholdNumerator = 3;
    internal const int HalfWaveThresholdDenominator = 10000;
    internal const int SilenceTimeoutFrequency = 600;
    internal const int Percent = 100;
    internal const int MillisecondsPerSecond = 1000;
    internal const int RecordingSampleRate = 48000;
    internal const short RecordingAmplitude = 16000;
    internal static IReadOnlySet<EmulationCassetteCommand> Commands { get; } = new HashSet<EmulationCassetteCommand>
    {
        EmulationCassetteCommand.Play,
        EmulationCassetteCommand.Stop,
        EmulationCassetteCommand.Pause,
        EmulationCassetteCommand.Rewind,
        EmulationCassetteCommand.FastForward
    };
    internal static IReadOnlySet<EmulationCassetteCommand> RecordingCommands { get; } = new HashSet<EmulationCassetteCommand>(Commands)
    {
        EmulationCassetteCommand.Record
    };
    internal static IReadOnlySet<EmulationCassetteCommand> EmptyCommands { get; } = new HashSet<EmulationCassetteCommand>();
}

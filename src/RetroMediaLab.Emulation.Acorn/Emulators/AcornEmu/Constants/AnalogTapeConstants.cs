namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants;

internal static class AnalogTapeConstants
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
    internal static IReadOnlySet<EmulationTapeCommand> Commands { get; } = new HashSet<EmulationTapeCommand>
    {
        EmulationTapeCommand.Play,
        EmulationTapeCommand.Stop,
        EmulationTapeCommand.Pause,
        EmulationTapeCommand.Rewind,
        EmulationTapeCommand.FastForward
    };
    internal static IReadOnlySet<EmulationTapeCommand> RecordingCommands { get; } = new HashSet<EmulationTapeCommand>(Commands)
    {
        EmulationTapeCommand.Record
    };
    internal static IReadOnlySet<EmulationTapeCommand> EmptyCommands { get; } = new HashSet<EmulationTapeCommand>();
}

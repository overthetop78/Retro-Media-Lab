using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Functions.Emulation.Machine;

internal static class TapeTransportPresentationFunctions
{
    internal static bool IsEnabled(bool powered, bool supported, EmulationTapeState state, EmulationTapeCommand? activeOperation, EmulationTapeCommand command) => powered && supported && (state != EmulationTapeState.Empty || command == EmulationTapeCommand.Record) && !IsRunning(state, activeOperation, command);

    internal static bool IsActive(EmulationTapeState state, EmulationTapeCommand? activeOperation, EmulationTapeCommand command) => command switch
    {
        EmulationTapeCommand.Play => activeOperation == command
            && state is EmulationTapeState.Playing or EmulationTapeState.Paused,
        EmulationTapeCommand.Record => activeOperation == command
            && state is EmulationTapeState.Recording or EmulationTapeState.Paused,
        EmulationTapeCommand.Pause => state == EmulationTapeState.Paused,
        EmulationTapeCommand.Rewind or EmulationTapeCommand.FastForward => activeOperation == command,
        _ => false
    };

    internal static bool IsBlinking(EmulationTapeState state, EmulationTapeCommand? activeOperation, EmulationTapeCommand command) =>
        state == EmulationTapeState.Paused
        && activeOperation is EmulationTapeCommand.Play or EmulationTapeCommand.Record
        && command == activeOperation;

    private static bool IsRunning(EmulationTapeState state, EmulationTapeCommand? activeOperation, EmulationTapeCommand command) => IsActive(state, activeOperation, command) && command is EmulationTapeCommand.Play or EmulationTapeCommand.Record;
}

namespace RetroMediaLab.Emulation.Interfaces;

public interface IEmulationTapeTransport
{
    EmulationTapeState State { get; }
    EmulationTapeCommand? ActiveOperation => State switch
    {
        EmulationTapeState.Playing => EmulationTapeCommand.Play,
        EmulationTapeState.Recording => EmulationTapeCommand.Record,
        _ => null
    };
    IReadOnlySet<EmulationTapeCommand> AvailableCommands { get; }
    ValueTask ExecuteAsync(EmulationTapeCommand command, CancellationToken cancellationToken = default);
}

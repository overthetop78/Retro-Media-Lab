namespace RetroMediaLab.Emulation.Acorn.Common.Services;

/// <summary>Projette la facette tape du cœur vers l'API de transport existante de l'application.</summary>
internal sealed partial class Machine : IEmulationTapeTransport
{
    /// <summary>Les autres cœurs restent dépourvus de transport lorsqu'ils ne fournissent pas cette facette.</summary>
    public IEmulationTapeTransport? TapeTransport => _core is IEmulatorTapeTransport ? this : null;
    EmulationTapeState IEmulationTapeTransport.State => (_core as IEmulatorTapeTransport)?.TapeState ?? EmulationTapeState.Empty;
    EmulationTapeCommand? IEmulationTapeTransport.ActiveOperation => (_core as IEmulatorTapeTransport)?.TapeOperation;
    IReadOnlySet<EmulationTapeCommand> IEmulationTapeTransport.AvailableCommands => (_core as IEmulatorTapeTransport)?.TapeCommands ?? new HashSet<EmulationTapeCommand>();
    /// <summary>Le lecteur est modifié sur le thread propriétaire du cœur, comme les montages et les resets.</summary>
    ValueTask IEmulationTapeTransport.ExecuteAsync(EmulationTapeCommand command, CancellationToken cancellationToken) => QueueCommand(() => ((IEmulatorTapeTransport)_core).ExecuteTape(command), cancellationToken);
}

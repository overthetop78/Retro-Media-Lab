namespace RetroMediaLab.Emulation.Acorn.Common.Interfaces;

/// <summary>Facette tape synchrone d'un cœur, empruntée par la session Common.</summary>
/// <remarks>Common sérialise Execute avec les autres commandes du cœur. Les états sont des instantanés ;
/// le cœur possède le lecteur et son signal, tandis que la session possède le thread d'exécution.</remarks>
internal interface IEmulatorTapeTransport
{
    event Action? ActivityChanged;
    EmulationTapeState TapeState { get; }
    bool TapeActive { get; }
    EmulationTapeCommand? TapeOperation { get; }
    IReadOnlySet<EmulationTapeCommand> TapeCommands { get; }
    void ExecuteTape(EmulationTapeCommand command);
}

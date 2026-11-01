using RetroMediaLab.Infrastructure.Commands;
namespace RetroMediaLab.Infrastructure.Commands.Execution;

public interface IGreaseweazleRunner
{
    bool IsRunning { get; }
    Task<GwExecutionResult> RunAsync(GwCommand command, IProgress<GwOutputLine>? output = null, CancellationToken cancellationToken = default);
}

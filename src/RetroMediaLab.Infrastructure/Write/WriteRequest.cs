using EnabledOption = RetroMediaLab.MediaEngine.Contracts.Options.EnabledOption;
namespace RetroMediaLab.Infrastructure.Write;

public sealed record WriteRequest(string GwExecutable, string SourcePath, string? FormatId, IReadOnlyList<EnabledOption> Options, bool DisableVerify = false, string? Device = null, string? Drive = null, string? ExpertArguments = null);

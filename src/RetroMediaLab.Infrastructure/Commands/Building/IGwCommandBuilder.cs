using RetroMediaLab.Infrastructure.Commands;
using EnabledOption = RetroMediaLab.MediaEngine.Contracts.Options.EnabledOption;
using ConversionOutput = RetroMediaLab.MediaEngine.Images.Conversion.ConversionOutput;
using RetroMediaLab.Infrastructure.Maintenance;
using RetroMediaLab.Infrastructure.Read;
using RetroMediaLab.Infrastructure.Write;
namespace RetroMediaLab.Infrastructure.Commands.Building;

/// <summary>
/// Central contract for every command assembled by the application.
/// Specialized builders retain the validation rules for their operation;
/// this service gives callers one substitutable dependency.
/// </summary>
public interface IGwCommandBuilder
{
    GwCommand BuildInfo(GwInfoRequest request);
    GwCommand BuildRead(ReadRequest request);
    GwCommand BuildWrite(WriteRequest request);
    GwCommand BuildConversion(string executable, string source, ConversionOutput output, IReadOnlyList<EnabledOption>? options = null, string? expertArguments = null);
    GwCommand BuildErase(EraseRequest request);
    GwCommand BuildClean(CleanRequest request);
    GwCommand BuildTool(ToolCommandRequest request);
}

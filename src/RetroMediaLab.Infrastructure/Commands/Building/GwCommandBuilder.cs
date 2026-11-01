using RetroMediaLab.Infrastructure.Commands;
using EnabledOption = RetroMediaLab.MediaEngine.Contracts.Options.EnabledOption;
using ConversionCommandBuilder = RetroMediaLab.Infrastructure.Conversion.ConversionCommandBuilder;
using ConversionOutput = RetroMediaLab.MediaEngine.Images.Conversion.ConversionOutput;
using RetroMediaLab.Infrastructure.Maintenance;
using RetroMediaLab.Infrastructure.Read;
using RetroMediaLab.Infrastructure.Write;
namespace RetroMediaLab.Infrastructure.Commands.Building;

public sealed class GwCommandBuilder : IGwCommandBuilder
{
    public GwCommand BuildInfo(GwInfoRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Executable);
        var arguments = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Device)) arguments.AddRange(["--device", request.Device]);
        if (request.Bootloader) arguments.Add("--bootloader");
        return new GwCommand(request.Executable, "info", arguments);
    }

    public GwCommand BuildRead(ReadRequest request) => ReadCommandBuilder.Build(request);
    public GwCommand BuildWrite(WriteRequest request) => WriteCommandBuilder.Build(request);
    public GwCommand BuildConversion(string executable, string source, ConversionOutput output, IReadOnlyList<EnabledOption>? options = null, string? expertArguments = null) => ConversionCommandBuilder.Build(executable, source, output, options, expertArguments);
    public GwCommand BuildErase(EraseRequest request) => MaintenanceCommandBuilder.Erase(request);
    public GwCommand BuildClean(CleanRequest request) => MaintenanceCommandBuilder.Clean(request);
    public GwCommand BuildTool(ToolCommandRequest request) => ToolCommandBuilder.Build(request);
}

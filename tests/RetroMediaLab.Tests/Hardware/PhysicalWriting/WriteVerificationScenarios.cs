using RetroMediaLab.App.Enums.Services.PhysicalDiskWriting;
using RetroMediaLab.App.Services.PhysicalDiskWriting;
using RetroMediaLab.Infrastructure.Hardware.Greaseweazle;
using RetroMediaLab.Infrastructure.Hardware.Media;
using RetroMediaLab.MediaEngine.Images.Reading;
using RetroMediaLab.MediaEngine.PhysicalMedia.Writing;

namespace RetroMediaLab.Tests.Hardware.PhysicalWriting;

internal static class WriteVerificationScenarios
{
    public static async Task Unsupported()
    {
        var device = new WritePlanningScenarios.Device();
        var plan = new FloppyMediaWritePlanningService(DiskImageExplorer.CreateDefault()).CreatePlan(WritePlanningScenarios.Image, 0);
        var writers = new MediaPhysicalWriterRegistry([new GreaseweazleMediaPhysicalWriter(() => device)]);
        var result = await new PhysicalDiskWriteService(writers).WriteAsync(plan, WritePlanningScenarios.Options with { Verify = true });
        Assert.False(result.IsSuccess);
        Assert.Equal(0, result.WrittenTracks);
        Assert.Equal(PhysicalDiskWriteFailureCategory.Validation, Assert.Single(result.Failures).Category);
        Assert.Empty(device.Calls);
    }
}

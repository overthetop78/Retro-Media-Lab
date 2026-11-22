namespace RetroMediaLab.App.Functions.Emulation.Machine;

internal static class EmulationVideoConfigurationFunctions
{
    internal static IEmulationConfiguration Apply(IEmulationModule module, IEmulationConfiguration configuration, EmulationVideoRenderer renderer) =>
        module is IEmulationVideoSettingsManager manager ? manager.ApplyVideoSettings(configuration, GraphicsApi(renderer)) : configuration;

    internal static EmulationGraphicsApi GraphicsApi(EmulationVideoRenderer renderer) => renderer switch
    {
        EmulationVideoRenderer.OpenGL => EmulationGraphicsApi.OpenGL,
        EmulationVideoRenderer.Vulkan => EmulationGraphicsApi.Vulkan,
        EmulationVideoRenderer.Direct3D11 => EmulationGraphicsApi.Direct3D11,
        _ => EmulationGraphicsApi.Automatic
    };

    internal static bool IsVideoHardwareField(EmulationSettingsField field) =>
        field.LabelResourceKey is "Emulation.Memory.Video" or "Emulation.Watara.Memory.Video" or "Emulation.Acorn.Atom.VideoRam"
            or "Emulation.Acorn.Video.Controller" or "Emulation.Acorn.Video.Pixels" or "Emulation.Acorn.Video.Teletext"
        || (field.Tab == EmulationMachineTab.Ram && field.Id.EndsWith("_display_memory", StringComparison.Ordinal))
        || (field.Tab == EmulationMachineTab.Video
            && (field.Id is "configuration.model.video" or "machine.chipset"
                || field.LabelResourceKey.EndsWith(".Chipset", StringComparison.Ordinal)
                || field.LabelResourceKey.EndsWith(".Video.Chip", StringComparison.Ordinal)
                || field.LabelResourceKey.EndsWith(".VideoChip", StringComparison.Ordinal)));
}

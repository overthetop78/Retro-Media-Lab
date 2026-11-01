using CommonCoreHostFunctions = RetroMediaLab.Emulation.Wenquxing.Common.Functions.CoreHostFunctions;
namespace RetroMediaLab.Emulation.Wenquxing.Common.Machines.Common.Functions;
internal static class EmulationMediaConversionFunctions
{
    internal static EmulationMedia ToCommon(MediaConfiguration media) => new(media.Path, media.Slot, media.Category, media.IsReadOnly, media.IsInserted);
    internal static MediaConfiguration ToModule(EmulationMedia media,
        IReadOnlyList<MediaConfiguration> mountedMedia) => throw CommonCoreHostFunctions.Messages.CreateError(EmulationMessageCategory.Media, EmulationMessageCode.MediaUnsupported, null);
}
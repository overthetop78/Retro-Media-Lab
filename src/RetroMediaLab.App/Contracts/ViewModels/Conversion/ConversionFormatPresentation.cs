using RetroMediaLab.MediaEngine.Images.Formats;
using RetroMediaLab.App.Enums.ViewModels.Conversion;

namespace RetroMediaLab.App.Contracts.ViewModels.Conversion;

public sealed record ConversionFormatPresentation(DiskFormat Format, bool IsCompatible, bool IsSelected, IReadOnlySet<string> ExplicitExtensions, ConversionFormatGroup Group, bool IsReconstructedFlux);

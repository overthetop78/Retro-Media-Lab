namespace GWGUI.App.Theme.OS.Editions.Windows95.Views;

internal enum ConversionOptionField
{
    TracksEnabled,
    TracksValue,
    OutputTracksEnabled,
    OutputTracksValue,
    AdjustSpeedEnabled,
    AdjustSpeedValue,
    PllEnabled,
    PllValue,
    HardSectors,
    Reverse,
    DiskDefsEnabled,
    DiskDefsValue,
    BrowseDiskDefs,
    ExpertArguments
}

internal static class ConversionOptionsFields
{
    internal const double CellMargin = 3;
    internal const double MinimumWidth = 420;
    internal const double MaximumHeight = 500;
    internal const string ExpertResource = "Common.ExpertArguments";

    internal static IReadOnlyList<(ConversionOptionField Toggle, ConversionOptionField? Value)> Rows { get; } =
    [
        (ConversionOptionField.TracksEnabled, ConversionOptionField.TracksValue),
        (ConversionOptionField.OutputTracksEnabled, ConversionOptionField.OutputTracksValue),
        (ConversionOptionField.AdjustSpeedEnabled, ConversionOptionField.AdjustSpeedValue),
        (ConversionOptionField.PllEnabled, ConversionOptionField.PllValue),
        (ConversionOptionField.HardSectors, null),
        (ConversionOptionField.Reverse, null)
    ];
}

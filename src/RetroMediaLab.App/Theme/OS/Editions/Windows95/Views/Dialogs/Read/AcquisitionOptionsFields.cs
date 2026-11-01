namespace RetroMediaLab.App.Theme.OS.Editions.Windows95.Views;

internal enum AcquisitionOptionField
{
    RevsEnabled,
    RevsValue,
    RetriesEnabled,
    RetriesValue,
    TracksEnabled,
    TracksValue,
    SeekRetriesEnabled,
    SeekRetriesValue,
    FakeIndexEnabled,
    FakeIndexValue,
    HardSectors,
    Reverse,
    AdjustSpeedEnabled,
    AdjustSpeedValue,
    PllEnabled,
    PllValue,
    DenselEnabled,
    DenselValue,
    Tg43,
    DiskDefsEnabled,
    DiskDefsValue,
    BrowseDiskDefs,
    AutoNumber,
    SequenceKind,
    SequenceWidth,
    SequenceValue,
    NamePreview,
    ExpertArguments
}

internal static class AcquisitionOptionsFields
{
    internal const string NextResource = "Read.Next";
    internal const string ExpertResource = "Common.ExpertArguments";
    internal const double CellMargin = 3;
    internal const double MinimumWidth = 540;
    internal const double MaximumHeight = 580;

    internal static IReadOnlyList<(AcquisitionOptionField Enabled, AcquisitionOptionField? Value)> Capture { get; } =
    [
        (AcquisitionOptionField.RetriesEnabled, AcquisitionOptionField.RetriesValue),
        (AcquisitionOptionField.TracksEnabled, AcquisitionOptionField.TracksValue),
        (AcquisitionOptionField.SeekRetriesEnabled, AcquisitionOptionField.SeekRetriesValue),
        (AcquisitionOptionField.FakeIndexEnabled, AcquisitionOptionField.FakeIndexValue),
        (AcquisitionOptionField.HardSectors, null),
        (AcquisitionOptionField.Reverse, null),
        (AcquisitionOptionField.AdjustSpeedEnabled, AcquisitionOptionField.AdjustSpeedValue),
        (AcquisitionOptionField.PllEnabled, AcquisitionOptionField.PllValue),
        (AcquisitionOptionField.DenselEnabled, AcquisitionOptionField.DenselValue),
        (AcquisitionOptionField.Tg43, null)
    ];
}

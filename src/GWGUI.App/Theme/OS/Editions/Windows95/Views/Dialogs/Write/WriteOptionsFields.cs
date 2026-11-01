namespace GWGUI.App.Theme.OS.Editions.Windows95.Views;

internal enum WriteOptionField
{
    NoVerify,
    EraseEmpty,
    RetriesEnabled,
    RetriesValue,
    TracksEnabled,
    TracksValue,
    PreErase,
    Reverse,
    FakeIndexEnabled,
    FakeIndexValue,
    HardSectors,
    PrecompEnabled,
    PrecompValue,
    DenselEnabled,
    DenselValue,
    Tg43,
    DiskDefsEnabled,
    DiskDefsValue,
    BrowseDiskDefs,
    ExpertArguments
}

internal static class WriteOptionsFields
{
    internal const double CellMargin = 3;
    internal const double MinimumWidth = 420;
    internal const double MaximumHeight = 540;
    internal const string ExpertResource = "Common.ExpertArguments";

    internal static IReadOnlyList<(WriteOptionField Toggle, WriteOptionField? Value)> Options { get; } =
    [
        (WriteOptionField.NoVerify, null),
        (WriteOptionField.EraseEmpty, null),
        (WriteOptionField.RetriesEnabled, WriteOptionField.RetriesValue),
        (WriteOptionField.TracksEnabled, WriteOptionField.TracksValue),
        (WriteOptionField.PreErase, null),
        (WriteOptionField.Reverse, null),
        (WriteOptionField.FakeIndexEnabled, WriteOptionField.FakeIndexValue),
        (WriteOptionField.HardSectors, null),
        (WriteOptionField.PrecompEnabled, WriteOptionField.PrecompValue),
        (WriteOptionField.DenselEnabled, WriteOptionField.DenselValue),
        (WriteOptionField.Tg43, null)
    ];
}

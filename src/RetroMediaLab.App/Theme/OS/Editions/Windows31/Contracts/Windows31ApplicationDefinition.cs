using System.Windows;
using System.Windows.Media;

namespace RetroMediaLab.App.Theme.OS.Editions.Windows31;

internal sealed record Windows31ApplicationDefinition(
    Windows31ApplicationId Id,
    Windows31ProgramGroup Group,
    string TitleResource,
    Func<Windows31Document> CreateDocument,
    Size InitialSize)
{
    internal ImageSource? Icon { get; init; }
}

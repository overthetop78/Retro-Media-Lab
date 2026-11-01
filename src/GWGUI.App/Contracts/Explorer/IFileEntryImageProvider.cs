using System.Windows.Media;
using GWGUI.App.Enums.Explorer;

namespace GWGUI.App.Contracts.Explorer;

/// <summary>Theme artwork for the existing file categories; does not classify entries.</summary>
public interface IFileEntryImageProvider
{
    public const string ResourceKey = nameof(IFileEntryImageProvider);
    bool SupportsTint { get; }
    ImageSource GetImage(ExplorerIconCategory category, int requestedSize, bool expanded);
}

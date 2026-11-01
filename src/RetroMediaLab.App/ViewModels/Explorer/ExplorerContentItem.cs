using FileSystemEntryKind = RetroMediaLab.MediaEngine.Enums.FileSystemEntryKind;
using RetroMediaLab.App.Enums.Explorer;
using RetroMediaLab.App.Functions.Explorer;
using RetroMediaLab.App.Functions.Storage;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.MediaEngine.Contracts.Explorer;


namespace RetroMediaLab.App.ViewModels.Explorer;

public sealed class ExplorerContentItem
{
    public ExplorerContentItem(FileSystemEntry entry)
    {
        Entry = entry;
        Definition = ExplorerFilePresentation.DefinitionFor(entry);
        IconCategory = Definition.IconCategory;
        TypeText = ExplorerFilePresentation.TypeTextFor(Definition);
        Tone = Definition.ExecutionKind != ExplorerExecutionKind.None ? ExplorerEntryTone.Executable : Definition.Category == ExplorerFileCategory.System || IsMarked(entry, "Hidden") || IsMarked(entry, "System") ? ExplorerEntryTone.Muted : ExplorerEntryTone.Default;
    }

    public FileSystemEntry Entry { get; }
    public RetroMediaLab.App.Contracts.Explorer.ExplorerFileTypeDefinition Definition { get; }
    public string Name => Entry.Name;
    public ExplorerIconCategory IconCategory { get; }
    public string TypeText { get; }
    public ExplorerEntryTone Tone { get; }
    public string SizeText => Entry.Kind == FileSystemEntryKind.Directory ? string.Empty : StorageSizeFormatter.FormatBytes(Entry.Size);
    public string ModifiedText => Entry.Modified?.LocalDateTime.ToString("g") ?? "\u2014";

    private static bool IsMarked(FileSystemEntry entry, string value) => entry.Attributes.Any(attribute => attribute.Equals(value, StringComparison.OrdinalIgnoreCase));
}

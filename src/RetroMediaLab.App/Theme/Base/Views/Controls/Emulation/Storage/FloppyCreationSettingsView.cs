using RetroMediaLab.App.Functions.Emulation.Storage;
using RetroMediaLab.App.Services.Dialogs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using RetroMediaLab.App.Constants.Emulation;
using RetroMediaLab.App.Contracts.Storage;
using RetroMediaLab.App.Functions.Views.Emulation.Storage;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.Emulation.Contracts;
using Microsoft.Win32;

namespace RetroMediaLab.App.Views.Controls.Emulation.Storage;

/// <summary>Sépare le contenu de la disquette, son conteneur et le fichier de destination.</summary>
internal sealed class FloppyCreationSettingsView : Border
{
    private readonly ComboBox _disk = new();
    private readonly ComboBox _image = new() { FlowDirection = FlowDirection.LeftToRight };
    private readonly TextBox _path = new() { IsReadOnly = true, MinHeight = 32, VerticalContentAlignment = VerticalAlignment.Center, FlowDirection = FlowDirection.LeftToRight };
    private readonly IReadOnlyDictionary<string, FloppyCreationFormatChoice[]> _groups;

    internal FloppyCreationFormatChoice? SelectedFormat => _image.SelectedItem as FloppyCreationFormatChoice;
    internal string DestinationPath => _path.Text;
    internal ComboBox PresentationDisk => _disk;
    internal ComboBox PresentationImage => _image;
    internal TextBox PresentationDestination => _path;
    internal Button PresentationBrowse { get; }
    internal bool IsComplete => SelectedFormat is not null && !string.IsNullOrWhiteSpace(DestinationPath);

    internal FloppyCreationSettingsView(FloppyDriveModelChoice model, FloppyDriveDialogOptions options, Guid clientGuid)
    {
        SetResourceReference(StyleProperty, "Card");
        Padding = new Thickness(16);
        var (groups, initial) = FloppyChoicePresentation.CreationFormats(model);
        _groups = groups;
        _disk.ItemsSource = _groups.Values.Select(group => new StorageDialogChoice(group[0].FormatId, FloppyChoicePresentation.DiskFormat(group[0]))).ToArray();
        StorageDialogUi.WrapChoices(_disk, nameof(StorageDialogChoice.Text));
        StorageDialogUi.WrapChoices(_image, nameof(FloppyCreationFormatChoice.DisplayName));
        _path.SetBinding(ToolTipProperty, new Binding(nameof(TextBox.Text)) { Source = _path });

        var fields = new StackPanel();
        AddField(MediaCreationConstants.FormatLabel, _disk);
        AddField(MediaCreationConstants.ImageFormatLabel, _image);
        var destination = new Grid();
        destination.ColumnDefinitions.Add(new ColumnDefinition());
        destination.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        destination.Children.Add(_path);
        var browse = new Button { Content = LocExtension.Get("Common.Browse"), Margin = new Thickness(10, 0, 0, 0), MinWidth = 100 };
        Grid.SetColumn(browse, 1); destination.Children.Add(browse);
        PresentationBrowse = browse;
        browse.Click += (_, _) => Browse();
        AddField(MediaCreationConstants.DestinationLabel, destination, last: true);
        Child = fields;

        _disk.SelectionChanged += (_, _) => RefreshImages();
        _image.SelectionChanged += (_, _) => RefreshExtension();
        _disk.SelectedItem = _disk.Items.Cast<StorageDialogChoice>().Single(choice => choice.Value == initial.FormatId);

        void AddField(string resource, FrameworkElement editor, bool last = false)
        {
            fields.Children.Add(new TextBlock { Text = LocExtension.Get(resource), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
            editor.Margin = new Thickness(0, 0, 0, last ? 0 : 16);
            fields.Children.Add(editor);
        }

        void Browse()
        {
            var selected = SelectedFormat;
            var (filter, fileName) = FloppyChoicePresentation.DestinationDialog(
                _image.Items.Cast<FloppyCreationFormatChoice>(), selected, DestinationPath);
            var dialog = new SaveFileDialog
            {
                Filter = filter,
                DefaultExt = selected?.Extension ?? string.Empty,
                AddExtension = selected is not null,
                InitialDirectory = FileSelectionFiles.ExistingDirectoryOrEmpty(options.ImageDirectory),
                ClientGuid = clientGuid,
                FileName = fileName
            };
            if (WpfFileDialogService.ShowFileDialog(dialog, Window.GetWindow(this)) == true) _path.Text = dialog.FileName;
        }
    }

    private void RefreshImages()
    {
        var previous = SelectedFormat;
        var formats = _disk.SelectedItem is StorageDialogChoice disk ? _groups[disk.Value] : [];
        var choices = formats.Select(FloppyChoicePresentation.ImageFormat).ToArray();
        _image.ItemsSource = choices;
        _image.SelectedItem = FloppyChoicePresentation.SelectedImageFormat(choices, previous);
        RefreshExtension();
    }

    private void RefreshExtension() =>
        _path.Text = FloppyChoicePresentation.DestinationForFormat(DestinationPath, SelectedFormat);
}

using RetroMediaLab.App.Functions.Emulation.Storage;
using RetroMediaLab.App.Services.Dialogs;
using RetroMediaLab.App.Constants.Emulation;
using RetroMediaLab.App.Constants.Controls.Visual;
using RetroMediaLab.App.Constants.Localization;
using RetroMediaLab.App.Contracts.Storage;
using RetroMediaLab.App.Functions.Storage;
using RetroMediaLab.App.Functions.Views.Emulation.Storage;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Theme.Common;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RetroMediaLab.Emulation;
using Microsoft.Win32;
using RetroMediaLab.Emulation.HardDisks;


namespace RetroMediaLab.App.Views.Dialogs.Emulation.Storage;

public sealed class HardDiskDriveConfigurationDialog : Window
{
    private readonly string _identifier;
    private readonly int? _fixedImageSizeBytes;
    private readonly EmulationStorageDialogPresentation? _presentation;
    private readonly string _imageDirectory;
    private readonly Guid _clientGuid;
    private readonly TabControl _supportMode = new();
    private readonly TextBox _existingPath = new();
    private readonly TextBox _newName = new() { Text = EmulationControlDefaults.HardDiskFileName };
    private readonly ComboBox _sizePreset = new();
    private readonly TextBox _customSize = new() { Text = EmulationControlDefaults.HardDiskSizeMiB.ToString() };
    private readonly ComboBox _sizeUnit = new();
    private readonly ComboBox _imageFormat = new() { DisplayMemberPath = nameof(HardDiskImageFormat.DisplayName) };
    private readonly IReadOnlyList<HardDiskImageFormat> _formats;
    private readonly Func<string, Task<bool>>? _deleteImage;
    private readonly StackPanel _customSizeRow = new();
    private readonly TextBlock _limits = new() { TextWrapping = TextWrapping.Wrap };
    private readonly ComboBox _preparation = new() { DisplayMemberPath = "Label", SelectedValuePath = "Value" };
    private readonly CheckBox _preallocate = new() { IsChecked = true };
    private readonly CheckBox _automaticGeometry = new() { IsChecked = true, IsEnabled = false };
    private readonly TextBox _cylinders = new();
    private readonly TextBox _heads = new() { Text = EmulationControlDefaults.HardDiskHeads.ToString() };
    private readonly TextBox _sectors = new() { Text = EmulationControlDefaults.HardDiskSectorsPerTrack.ToString() };
    private readonly TextBox _bytesPerSector = new() { Text = EmulationControlDefaults.HardDiskBytesPerSector.ToString() };
    private readonly TextBlock _capacity = new() { TextWrapping = TextWrapping.Wrap };
    private readonly Dictionary<string, FrameworkElement> _fields = new(StringComparer.Ordinal);
    private readonly IReadOnlyDictionary<string, string> _initialOptions;

    internal TabControl PresentationMode => _supportMode;
    internal TextBox PresentationExistingPath => _existingPath;
    internal Button PresentationBrowse { get; }
    internal Button? PresentationDelete { get; }
    internal Button PresentationRemove { get; }
    internal IReadOnlyList<(string Label, FrameworkElement Control)> PresentationCreateFields { get; }
    internal IReadOnlyList<(string Label, FrameworkElement Control)> PresentationGeometryFields { get; }
    internal Grid PresentationCustomSize { get; }
    internal FrameworkElement PresentationCustomSizeContainer => _customSizeRow;
    internal CheckBox PresentationPreallocate => _preallocate;
    internal CheckBox PresentationAutomaticGeometry => _automaticGeometry;
    internal TextBlock PresentationLimits => _limits;
    internal TextBlock PresentationCapacity => _capacity;
    internal TextBlock PresentationInterface { get; }
    internal TextBlock PresentationDirectory { get; }
    internal int? PresentationFixedSize => _fixedImageSizeBytes;
    internal string PresentationImageLabel { get; }
    internal string PresentationExistingHint { get; }
    internal string? PresentationModule { get; }
    internal IReadOnlyList<EmulationSettingsField> PresentationFields { get; }
    internal IReadOnlyDictionary<string, FrameworkElement> PresentationControls => _fields;
    private Task _deleteTask = Task.CompletedTask;
    private bool _deleteBusy;
    private bool _closeRequested;
    internal Task PresentationOperation => _deleteTask;

    internal void RequestPresentationClose()
    {
        if (_deleteBusy) _closeRequested = true;
        else Close();
    }

    public string? SupportPath { get; private set; }
    public string? InterfaceId { get; private set; }
    /// <summary>Réglages opaques du lecteur, conservés avec les champs fournis par le module.</summary>
    public IReadOnlyDictionary<string, string> Options
    {
        get
        {
            var values = new Dictionary<string, string>(_initialOptions, StringComparer.Ordinal);
            foreach (var entry in _fields) values[entry.Key] = StorageConfigurationFieldFunctions.Read(entry.Value);
            return values;
        }
    }

    public HardDiskDriveConfigurationDialog(string identifier, string machineName, string? currentPath,
        string imageDirectory, IReadOnlyList<HardDiskImageFormat> formats, Guid clientGuid,
        Func<string, Task<bool>>? deleteImage = null, int? fixedImageSizeBytes = null,
        string? defaultImageFileName = null, EmulationStorageDialogPresentation? presentation = null, IReadOnlyList<EmulationSettingsField>? configurationFields = null, IReadOnlyDictionary<string, string>? options = null, string? moduleId = null)
    {
        _initialOptions = options ?? new Dictionary<string, string>();
        PresentationModule = moduleId;
        PresentationFields = configurationFields ?? [];
        PresentationImageLabel = LocExtension.Get(presentation?.ImageResourceKey ?? "Emulation.Storage.Disk.Image");
        PresentationExistingHint = LocExtension.Get(presentation?.ExistingHintResourceKey ?? "Emulation.Storage.Disk.ExistingHint");
        Closing += (_, args) =>
        {
            if (!_deleteBusy) return;
            args.Cancel = true;
            _closeRequested = true;
        };
        _identifier = identifier;
        _fixedImageSizeBytes = fixedImageSizeBytes;
        _presentation = presentation;
        _imageDirectory = imageDirectory;
        _clientGuid = clientGuid;
        _formats = formats;
        if (formats.Count == 0) throw new ArgumentException(nameof(formats));
        _deleteImage = deleteImage;
        SupportPath = currentPath;
        Title = $"{LocExtension.Get(EmulationResourceKeys.StorageDeviceConfigure)} {identifier}";
        Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive);
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Width = Math.Min(fixedImageSizeBytes is null ? StorageConfigurationLayoutConstants.HardDiskWidth : StorageConfigurationLayoutConstants.ManagedWidth, SystemParameters.WorkArea.Width * StorageConfigurationLayoutConstants.WorkAreaWidthRatio);
        Height = fixedImageSizeBytes is null ? StorageConfigurationLayoutConstants.HardDiskHeight : double.NaN;
        MinWidth = fixedImageSizeBytes is null ? StorageConfigurationLayoutConstants.FloppyWidth : StorageConfigurationLayoutConstants.MinimumWidth;
        MinHeight = fixedImageSizeBytes is null ? StorageConfigurationLayoutConstants.MinimumWidth : 0;
        SizeToContent = fixedImageSizeBytes is null ? SizeToContent.Manual : SizeToContent.Height;
        MaxHeight = SystemParameters.WorkArea.Height;
        ResizeMode = ResizeMode.CanResize;

        var interfaceChoice = new TextBlock { Margin = new Thickness(0, 3, 0, 0) };
        PresentationInterface = interfaceChoice;
        _imageFormat.ItemsSource = formats;
        _imageFormat.SelectedItem = HardDiskConfigurationFunctions.FindExistingFormat(formats, currentPath) ?? formats[0];
        _newName.Text = defaultImageFileName ?? $"{identifier.TrimEnd(':')}{SelectedFormat.Extension}";
        if (_fixedImageSizeBytes is not null) _newName.Text = Path.GetFileNameWithoutExtension(_newName.Text);
        interfaceChoice.Text = _fixedImageSizeBytes is not null ? LocExtension.Get("Emulation.Storage.MemoryCard.Device") : SelectedFormat.InterfaceName;
        _imageFormat.SelectionChanged += (_, _) =>
        {
            interfaceChoice.Text = _fixedImageSizeBytes is not null ? LocExtension.Get("Emulation.Storage.MemoryCard.Device") : SelectedFormat.InterfaceName;
            if (_fixedImageSizeBytes is null) _newName.Text = Path.ChangeExtension(_newName.Text, SelectedFormat.Extension);
            SetSizeChoices();
        };
        _existingPath.Text = currentPath ?? string.Empty;
        var existing = new StackPanel { Margin = new Thickness(8) };
        var existingPathField = StorageDialogUi.PathField(PresentationImageLabel, _existingPath, BrowseExisting);
        PresentationBrowse = existingPathField.Children.OfType<Button>().Single();
        existing.Children.Add(existingPathField);
        existing.Children.Add(StorageDialogUi.Info(PresentationExistingHint));
        if (_deleteImage is not null)
        {
            var delete = new Button { Content = LocExtension.Get("Emulation.Hdd.Delete"), HorizontalAlignment = HorizontalAlignment.Left };
            PresentationDelete = delete;
            async Task DeleteExistingAsync()
            {
                _deleteBusy = true;
                delete.IsEnabled = false;
                try
                {
                    if (!_formats.Any(format => string.Equals(format.Extension, Path.GetExtension(_existingPath.Text), StringComparison.OrdinalIgnoreCase)))
                    { ShowError(LocExtension.Get("Emulation.Hdd.InvalidFormat")); return; }
                    if (await _deleteImage(_existingPath.Text))
                    {
                        _existingPath.Clear();
                        SupportPath = null;
                    }
                }
                catch (Exception error) { ShowError(error.Message); }
                finally
                {
                    _deleteBusy = false;
                    delete.IsEnabled = true;
                    if (_closeRequested) Close();
                }
            }
            delete.Click += async (_, _) =>
            {
                if (_deleteBusy) return;
                _deleteTask = DeleteExistingAsync();
                await _deleteTask;
            };
            existing.Children.Add(delete);
        }

        _sizeUnit.ItemsSource = new[] { LocExtension.Get("Emulation.Storage.Unit.MiB"), LocExtension.Get("Emulation.Storage.Unit.GiB") };
        _sizeUnit.SelectedIndex = 0;
        SetSizeChoices();
        _sizePreset.SelectionChanged += (_, _) => UpdateDiskGeometry();
        _customSize.TextChanged += (_, _) => UpdateDiskGeometry();
        _sizeUnit.SelectionChanged += (_, _) => UpdateDiskGeometry();
        _automaticGeometry.Checked += (_, _) => UpdateDiskGeometry();
        _automaticGeometry.Unchecked += (_, _) => UpdateDiskGeometry();
        foreach (var field in new[] { _cylinders, _heads, _sectors, _bytesPerSector }) field.TextChanged += (_, _) => UpdateCapacity();
        _cylinders.TextChanged += (_, _) => UpdateCapacity();
        _heads.TextChanged += (_, _) => UpdateCapacity();
        _sectors.TextChanged += (_, _) => UpdateCapacity();
        _bytesPerSector.TextChanged += (_, _) => UpdateCapacity();
        _preallocate.Content = LocExtension.Get("Emulation.Storage.File.Preallocate");
        _automaticGeometry.Content = LocExtension.Get("Emulation.Storage.Geometry.AutomaticProfile");

        var customSize = new Grid();
        customSize.ColumnDefinitions.Add(new ColumnDefinition());
        customSize.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        customSize.Children.Add(_customSize);
        Grid.SetColumn(_sizeUnit, 1);
        _sizeUnit.Margin = new Thickness(6, 0, 0, 0);
        customSize.Children.Add(_sizeUnit);
        PresentationCustomSize = customSize;
        PresentationCreateFields = _fixedImageSizeBytes is not null
            ? [(LocExtension.Get("Emulation.Storage.MemoryCard.Name"), _newName)]
            : [
            (LocExtension.Get(_presentation?.FileNameResourceKey ?? "Read.FileName"), _newName),
            (LocExtension.Get(MediaCreationConstants.ImageFormatLabel), _imageFormat),
            (LocExtension.Get("Emulation.Hdd.Preparation"), SingleChoice(_preparation, () => _preparation.Text)),
            (LocExtension.Get("Emulation.Storage.Geometry.SizeProfile"), SingleChoice(_sizePreset, () => (_sizePreset.SelectedItem as HardDiskCapacityChoice)?.Text ?? _sizePreset.Text))];
        var imageFields = StorageDialogUi.CompactFields(PresentationCreateFields.ToArray());
        var image = new StackPanel();
        image.Children.Add(imageFields);
        if (_fixedImageSizeBytes is null)
        {
            _customSizeRow.Children.Add(StorageDialogUi.CompactFields((LocExtension.Get("Emulation.Storage.Geometry.CustomSize"), customSize)));
            image.Children.Add(_customSizeRow);
        }
        var destination = new TextBlock
        {
            Text = _imageDirectory,
            TextTrimming = TextTrimming.CharacterEllipsis,
            ToolTip = _imageDirectory,
            Margin = new Thickness(0, 6, 0, 0)
        };
        PresentationDirectory = destination;
        var destinationAndAllocation = new StackPanel();
        if (_fixedImageSizeBytes is null) destinationAndAllocation.Children.Add(destination);
        _preallocate.Margin = new Thickness(0, 18, 0, 0);
        if (_fixedImageSizeBytes is null)
        {
            destinationAndAllocation.Children.Add(_preallocate);
            _preallocate.ToolTip = LocExtension.Get("Emulation.Storage.File.PreallocationHint");
            destinationAndAllocation.Children.Add(_limits);
            _preparation.ToolTip = LocExtension.Get("Emulation.Hdd.PreparationHint");
        }
        else destinationAndAllocation.Children.Add(StorageDialogUi.Info(StorageSizeFormatter.FormatCapacity(_fixedImageSizeBytes.Value)));
        var createTop = new StackPanel();
        createTop.Children.Add(image);
        createTop.Children.Add(destinationAndAllocation);
        var create = new StackPanel { Margin = new Thickness(4) };
        create.Children.Add(createTop);
        if (_fixedImageSizeBytes is not null)
        {
            create.Children.Add(new TextBlock { Text = LocExtension.Get(_presentation?.DestinationFolderResourceKey ?? "Emulation.Storage.File.DestinationFolder"), FontWeight = FontWeights.SemiBold });
            create.Children.Add(destination);
            create.Children.Add(new TextBlock { Text = $"{_fixedImageSizeBytes.Value / 1024} KiB", Margin = new Thickness(0, 8, 0, 0) });
        }

        _supportMode.Items.Add(new TabItem { Header = LocExtension.Get(_presentation?.UseExistingResourceKey ?? "Emulation.Storage.Disk.UseExisting"), Content = existing });
        _supportMode.Items.Add(new TabItem { Header = LocExtension.Get(_presentation?.CreateResourceKey ?? "Emulation.Storage.HardDisk.Create"), Content = create });
        _supportMode.SelectedIndex = string.IsNullOrWhiteSpace(currentPath) ? 1 : 0;

        PresentationGeometryFields = [
            (LocExtension.Get("Emulation.Storage.Geometry.Cylinders"), _cylinders),
            (LocExtension.Get("Emulation.Storage.Geometry.Heads"), _heads),
            (LocExtension.Get("Emulation.Storage.Geometry.Sectors"), _sectors),
            (LocExtension.Get("Emulation.Storage.Geometry.BytesPerSector"), _bytesPerSector)];
        var geometry = StorageDialogUi.CompactFields(PresentationGeometryFields.ToArray());
        var geometryPanel = new StackPanel { Margin = new Thickness(8) };
        geometryPanel.Children.Add(_automaticGeometry);
        geometryPanel.Children.Add(geometry);
        geometryPanel.Children.Add(_capacity);
        var advanced = new Expander
        {
            Header = LocExtension.Get("Emulation.Tab.Advanced"),
            Content = geometryPanel,
            Margin = new Thickness(0, 10, 0, 0)
        };
        var support = new StackPanel();
        support.Children.Add(_supportMode);
        if (_fixedImageSizeBytes is null) support.Children.Add(advanced);

        var footer = StorageDialogUi.Footer(this, LocExtension.Get(_presentation?.UseActionResourceKey ?? "Emulation.Storage.Disk.Use"), Accept);
        var remove = new Button { Content = LocExtension.Get("Emulation.Storage.Media.Remove"), HorizontalAlignment = HorizontalAlignment.Left, Visibility = string.IsNullOrWhiteSpace(currentPath) ? Visibility.Collapsed : Visibility.Visible };
        remove.Click += (_, _) => { SupportPath = null; ThemeDialogCompletion.Complete(this, true); };
        PresentationRemove = remove;
        footer.Children.Insert(0, remove);

        var body = new StackPanel();
        body.Children.Add(support);
        var behaviorGroups = StorageConfigurationFieldFunctions.CreateGroups(configurationFields ?? [], moduleId, _initialOptions, _fields);
        FrameworkElement arrangedBody = body;
        if (behaviorGroups.Children.Count > 0)
        {
            var columns = new Grid();
            columns.ColumnDefinitions.Add(new ColumnDefinition());
            columns.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(StorageConfigurationLayoutConstants.BehaviorColumnWidth) });
            body.Margin = new Thickness(0, 0, 10, 0);
            columns.Children.Add(body);
            Grid.SetColumn(behaviorGroups, 1);
            columns.Children.Add(behaviorGroups);
            arrangedBody = columns;
        }

        var root = new Grid { Margin = new Thickness(18) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = StorageDialogUi.DialogHeader(_fixedImageSizeBytes is null ? IconGlyphs.HardDisk : IconGlyphs.Memory, Title, $"{LocExtension.Get(_presentation?.DeviceResourceKey ?? "Emulation.Storage.HardDisk.Device")} · {machineName}", _fixedImageSizeBytes is null ? interfaceChoice : null);
        root.Children.Add(header);

        var scroll = new ScrollViewer
        {
            Content = arrangedBody,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Margin = new Thickness(0, 12, 0, 12)
        };
        Grid.SetRow(scroll, 1);
        root.Children.Add(scroll);

        Grid.SetRow(footer, 2);
        root.Children.Add(footer);
        Content = root;
        if (_fixedImageSizeBytes is null) UpdateDiskGeometry();
    }

    private void BrowseExisting()
    {
        var dialog = new OpenFileDialog
        {
            Filter = string.Join('|', _formats.Select(format => $"{format.DisplayName}|*{format.Extension}")),
            InitialDirectory = FileSelectionFiles.ExistingDirectoryOrEmpty(_imageDirectory),
            ClientGuid = _clientGuid
        };
        if (WpfFileDialogService.ShowFileDialog(dialog, this) == true)
        {
            _existingPath.Text = dialog.FileName;
            _imageFormat.SelectedItem = HardDiskConfigurationFunctions.FindExistingFormat(_formats, dialog.FileName, allowExtensionFallback: true);
        }
    }

    private void Accept()
    {
        try { AcceptImage(); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
        { ShowError(error.Message); }
    }

    private void AcceptImage()
    {
        if (_supportMode.SelectedIndex == 0)
        {
            var existing = HardDiskConfigurationFunctions.ValidateExistingImage(_formats, _existingPath.Text, _fixedImageSizeBytes);
            if (existing.Error == HardDiskConfigurationFunctions.ExistingImageError.Missing)
            {
                WpfMessageDialogService.ShowMessage(this, LocExtension.Get(_presentation?.ImageRequiredResourceKey ?? "Emulation.Storage.Disk.ImageRequired"), Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SupportPath = existing.Path;
            if (existing.Error == HardDiskConfigurationFunctions.ExistingImageError.InvalidSize)
            { ShowError(LocExtension.Get(_presentation?.InvalidSizeResourceKey ?? "Emulation.Storage.Disk.InvalidSize")); return; }
            if (existing.Format is not { } format)
            { ShowError(LocExtension.Get(_presentation?.InvalidFormatResourceKey ?? "Emulation.Hdd.InvalidFormat")); return; }
            InterfaceId = format.InterfaceName;
            ThemeDialogCompletion.Complete(this, true);
            return;
        }

        var formatToCreate = SelectedFormat;
        var size = _fixedImageSizeBytes is null && TryGetByteSize(out var byteSize) ? byteSize : (long?)null;
        var (error, path) = HardDiskConfigurationFunctions.CreateImage(
            _imageDirectory, _newName.Text, _identifier, formatToCreate, _fixedImageSizeBytes, size,
            _preallocate.IsChecked == true, _preparation.SelectedValue is HardDiskPreparation preparation ? preparation : HardDiskPreparation.Blank,
            (_cylinders.Text, _heads.Text, _sectors.Text, _bytesPerSector.Text));
        switch (error)
        {
            case HardDiskConfigurationFunctions.NewImageError.InvalidFormat:
                ShowError(LocExtension.Get(_presentation?.InvalidFormatResourceKey ?? "Emulation.Hdd.InvalidFormat"));
                return;
            case HardDiskConfigurationFunctions.NewImageError.AlreadyExists:
                ShowError(LocExtension.Get("Emulation.Hdd.Exists"));
                return;
            case HardDiskConfigurationFunctions.NewImageError.MissingSize:
                WpfMessageDialogService.ShowMessage(this, LocExtension.Get("Emulation.Storage.Disk.InvalidSize"), Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            case HardDiskConfigurationFunctions.NewImageError.UnsupportedCapacity:
                ShowError(_limits.Text);
                return;
            case HardDiskConfigurationFunctions.NewImageError.InvalidGeometry:
                ShowError(LocExtension.Get("Emulation.Storage.Disk.InvalidSize"));
                return;
        }
        SupportPath = path;
        InterfaceId = formatToCreate.InterfaceName;
        ThemeDialogCompletion.Complete(this, true);
    }

    /// <summary>Une valeur imposée reste visible sans présenter un choix qui n'existe pas.</summary>
    private static FrameworkElement SingleChoice(ComboBox choice, Func<string> label)
    {
        var text = new TextBlock { VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
        var panel = new Grid();
        panel.Children.Add(choice);
        panel.Children.Add(text);
        void Refresh()
        {
            var single = choice.Items.Count == 1;
            choice.Visibility = single ? Visibility.Collapsed : Visibility.Visible;
            text.Visibility = single ? Visibility.Visible : Visibility.Collapsed;
            text.Text = label();
            text.ToolTip = choice.ToolTip;
        }
        choice.SelectionChanged += (_, _) => Refresh();
        panel.Loaded += (_, _) => Refresh();
        Refresh();
        return panel;
    }

    private HardDiskImageFormat SelectedFormat => (HardDiskImageFormat)_imageFormat.SelectedItem;

    private void ShowError(string message) => WpfMessageDialogService.ShowMessage(this, message, Title, MessageBoxButton.OK, MessageBoxImage.Warning);

    private void SetSizeChoices()
    {
        var format = SelectedFormat;
        _preparation.ItemsSource = format.SupportedPreparations.Select(value => new { Value = value, Label = LocExtension.Get("Emulation.Hdd.Prepare." + value) }).ToArray();
        _automaticGeometry.IsEnabled = format.SupportsExplicitGeometry && format.FixedGeometry is null;
        if (!format.SupportsExplicitGeometry || format.FixedGeometry is not null) _automaticGeometry.IsChecked = true;
        _preparation.SelectedIndex = 0;
        _limits.Visibility = format.MinimumBytes == format.MaximumBytes ? Visibility.Collapsed : Visibility.Visible;
        _limits.Text = LocExtension.Get("Emulation.Hdd.CapacityLimits", StorageSizeFormatter.FormatCapacity(format.MinimumBytes), StorageSizeFormatter.FormatCapacity(format.MaximumBytes), string.Join(" / ", format.LogicalSectorSizes.Order()));
        _preallocate.IsEnabled = format.SupportsFixedAllocation;
        if (!format.SupportsFixedAllocation) _preallocate.IsChecked = false;
        var sizeChoices = HardDiskConfigurationFunctions.CapacityChoices(format);
        _sizePreset.ItemsSource = sizeChoices;
        _sizePreset.SelectedIndex = Math.Max(0, Array.FindIndex(sizeChoices, choice => choice.Bytes == format.DefaultBytes));
        UpdateDiskGeometry();
    }

    private void UpdateDiskGeometry()
    {
        var custom = SelectedFormat.AllowedCapacitiesBytes is null && (_sizePreset.SelectedItem as HardDiskCapacityChoice)?.Bytes is null;
        _customSizeRow.Visibility = SelectedFormat.AllowedCapacitiesBytes is null ? Visibility.Visible : Visibility.Collapsed;
        _customSize.IsEnabled = custom;
        _sizeUnit.IsEnabled = custom;
        var automatic = _automaticGeometry.IsChecked == true;
        _cylinders.IsEnabled = !automatic;
        _heads.IsEnabled = !automatic;
        _sectors.IsEnabled = !automatic;
        _bytesPerSector.IsEnabled = !automatic;
        if (SelectedFormat.FixedGeometry is { } fixedGeometry)
        {
            _cylinders.Text = fixedGeometry.Cylinders.ToString();
            _heads.Text = fixedGeometry.Heads.ToString();
            _sectors.Text = fixedGeometry.SectorsPerTrack.ToString();
            _bytesPerSector.Text = fixedGeometry.SectorSize.ToString();
        }
        else if (automatic && TryGetSelectedSize(out var byteSize))
        {
            var geometry = HardDiskConfigurationFunctions.AutomaticGeometry(SelectedFormat, byteSize);
            _heads.Text = geometry.Heads.ToString();
            _sectors.Text = geometry.SectorsPerTrack.ToString();
            _bytesPerSector.Text = geometry.SectorSize.ToString();
            _cylinders.Text = geometry.Cylinders.ToString();
        }
        UpdateCapacity();
    }

    private void UpdateCapacity()
    {
        _capacity.Text = TryGetByteSize(out var byteSize) ? LocExtension.Get("Emulation.Storage.Geometry.CalculatedCapacity", StorageSizeFormatter.FormatCapacity(byteSize)) : LocExtension.Get("Emulation.Storage.Disk.InvalidSize");
    }

    private bool TryGetByteSize(out long byteSize)
    {
        if (_automaticGeometry.IsChecked == true && !SelectedFormat.SupportsExplicitGeometry) return TryGetSelectedSize(out byteSize);
        return HardDiskConfigurationFunctions.TryGeometrySize(_cylinders.Text, _heads.Text, _sectors.Text, _bytesPerSector.Text, out byteSize);
    }

    private bool TryGetSelectedSize(out long byteSize)
    {
        if ((_sizePreset.SelectedItem as HardDiskCapacityChoice)?.Bytes is long preset)
        {
            byteSize = preset;
            return true;
        }
        return HardDiskConfigurationFunctions.TryCustomSize(_customSize.Text, _sizeUnit.SelectedIndex == 1, out byteSize);
    }

}


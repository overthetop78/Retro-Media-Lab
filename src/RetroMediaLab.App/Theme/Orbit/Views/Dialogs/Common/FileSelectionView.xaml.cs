using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RetroMediaLab.App.Enums.Explorer;
using RetroMediaLab.App.Enums.Services.Dialogs;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.App.Views.Dialogs.Common;

namespace RetroMediaLab.App.Theme.Orbit.Views;

internal partial class FileSelectionView : UserControl, IDisposable
{
    private readonly FileSelectionDialog source;
    private bool updating;
    private bool disposed;
    private OrbitResources? resourceScope;
    private const double FolderIndent = 12;
    internal FileSelectionView(FileSelectionDialog source, bool? dark = null)
    {
        this.source = source;
        try
        {
            resourceScope = new OrbitResources(this, dark);
            InitializeComponent();
            NameField.Text = source.FileName;
            Accept.SetBinding(ContentControl.ContentProperty, LocExtension.CreateBinding(source.Kind switch
            {
                FileSelectionKind.Open => "FileDialog.Open",
                FileSelectionKind.Save => "Common.Save",
                _ => "Common.OK"
            }));
            ReadOnly.Visibility = source.ShowReadOnly ? Visibility.Visible : Visibility.Collapsed;
            ReadOnly.IsChecked = source.ReadOnly;
            Types.ItemsSource = source.Filters;
            Types.SelectedIndex = source.FilterIndex;
            if (source.Kind == FileSelectionKind.Folder)
            {
                Types.Visibility = TypesLabel.Visibility = Visibility.Collapsed;
                NameLabel.SetBinding(ContentControl.ContentProperty, LocExtension.CreateBinding("Explorer.Directory"));
            }
            source.Changed += OnChanged;
            Loaded += OnLoaded;
            PreviewKeyDown += OnKeyDown;
            Rebuild();
        }
        catch { Dispose(); throw; }
    }
    private async void OnLoaded(object sender, RoutedEventArgs args)
    {
        Loaded -= OnLoaded;
        NameField.Focus(); NameField.SelectAll();
        await source.InitializeAsync();
    }
    private void OnChanged(object? sender, EventArgs args) => Rebuild();
    private void Rebuild()
    {
        if (disposed) return;
        updating = true;
        try
        {
            CurrentPath.Text = source.CurrentDirectory;
            CurrentPath.ToolTip = source.CurrentDirectory;
            if (source.Kind == FileSelectionKind.Folder && string.IsNullOrWhiteSpace(source.FileName)) NameField.Text = source.CurrentDirectory;
            Files.ItemsSource = source.Directories.Select(directory => new Entry(directory))
                .Concat(source.Kind == FileSelectionKind.Folder ? [] : source.Files.Select(file => new Entry(file))).ToArray();
            Files.IsEnabled = !source.Busy;
            var chain = new Stack<DirectoryInfo>();
            for (var directory = new DirectoryInfo(source.CurrentDirectory); directory is not null; directory = directory.Parent) chain.Push(directory);
            var folders = new List<FolderRow>();
            foreach (var drive in source.Drives) folders.Add(new(new DirectoryInfo(drive.Name), 0, false));
            var level = 0;
            foreach (var directory in chain) folders.Add(new(directory, level++, true));
            Folders.ItemsSource = folders;
            Folders.SelectedItem = folders.LastOrDefault();
            Folders.IsEnabled = !source.Busy;
            Error.Text = source.Error;
            Error.Visibility = string.IsNullOrEmpty(source.Error) ? Visibility.Collapsed : Visibility.Visible;
            Accept.IsEnabled = !source.Busy;
        }
        finally { updating = false; }
    }
    private void OnNameChanged(object sender, TextChangedEventArgs args)
    {
        if (!updating && !disposed) source.FileName = NameField.Text;
    }
    private void OnFileSelected(object sender, SelectionChangedEventArgs args)
    {
        if (updating || disposed || Files.SelectedItem is not Entry selected) return;
        if (selected.Info is FileInfo file) NameField.Text = file.Name;
        else if (source.Kind == FileSelectionKind.Folder) NameField.Text = selected.Info.FullName;
    }
    private async void OnFolderSelected(object sender, SelectionChangedEventArgs args)
    {
        if (!updating && !disposed && Folders.SelectedItem is FolderRow folder
            && !string.Equals(folder.Folder.FullName, source.CurrentDirectory, StringComparison.OrdinalIgnoreCase))
            await NavigateAsync(folder.Folder.FullName);
    }
    private async void OnFilterSelected(object sender, SelectionChangedEventArgs args)
    {
        if (!updating && !disposed && IsLoaded && Types.SelectedIndex >= 0) await source.SelectFilterAsync(Types.SelectedIndex);
    }
    private async Task NavigateAsync(string path)
    {
        if (source.Kind == FileSelectionKind.Folder) NameField.Text = string.Empty;
        await source.NavigateAsync(path);
    }
    private async void OnFileDoubleClick(object sender, MouseButtonEventArgs args)
    {
        if (disposed || args.ChangedButton != MouseButton.Left || Files.SelectedItem is not Entry selected
            || ItemsControl.ContainerFromElement(Files, args.OriginalSource as DependencyObject) is not ListViewItem) return;
        args.Handled = true;
        if (selected.Info is DirectoryInfo directory) await NavigateAsync(directory.FullName);
        else { NameField.Text = selected.Name; await AcceptAsync(); }
    }
    private async void OnAccept(object sender, RoutedEventArgs args) { if (!disposed) await AcceptAsync(); }
    private async Task AcceptAsync()
    {
        await source.AcceptAsync();
        if (!disposed && NameField.Text != source.FileName) NameField.Text = source.FileName;
    }
    private void OnCancel(object sender, RoutedEventArgs args) { if (!disposed) source.RequestPresentationClose(); }
    private void OnReadOnlyChanged(object sender, RoutedEventArgs args) { if (!disposed) source.ReadOnly = ReadOnly.IsChecked == true; }
    private async void OnKeyDown(object sender, KeyEventArgs args)
    {
        if (disposed) return;
        if (args.Key == Key.Escape) { args.Handled = true; source.RequestPresentationClose(); }
        else if (args.Key == Key.Enter && Files.IsKeyboardFocusWithin && Files.SelectedItem is Entry { Info: DirectoryInfo directory })
        { args.Handled = true; await NavigateAsync(directory.FullName); }
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try
        {
            source.Changed -= OnChanged;
            Loaded -= OnLoaded;
            PreviewKeyDown -= OnKeyDown;
            if (NameField is null) return;
            NameField.TextChanged -= OnNameChanged;
            Files.SelectionChanged -= OnFileSelected;
            Files.MouseDoubleClick -= OnFileDoubleClick;
            Folders.SelectionChanged -= OnFolderSelected;
            Types.SelectionChanged -= OnFilterSelected;
            Accept.Click -= OnAccept; Cancel.Click -= OnCancel;
            ReadOnly.Checked -= OnReadOnlyChanged; ReadOnly.Unchecked -= OnReadOnlyChanged;
            Files.ItemsSource = Folders.ItemsSource = Types.ItemsSource = null;
        }
        finally
        {
            try { resourceScope?.Dispose(); }
            finally { resourceScope = null; Content = null; Resources.Clear(); }
        }
    }
    private sealed record Entry(FileSystemInfo Info)
    {
        public string Name => Info.Name;
        public ExplorerIconCategory Category => Info is DirectoryInfo ? ExplorerIconCategory.Folder : ExplorerIconCategory.File;
        public string Modified => Info.LastWriteTime.ToString("g");
    }
    private sealed record FolderRow(DirectoryInfo Folder, int Level, bool Expanded)
    {
        public string Name => Folder.Name;
        public Thickness Indent => new(Level * FolderIndent, 0, 0, 0);
    }
}

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GWGUI.App.Enums.Services.Dialogs;
using GWGUI.App.Views.Dialogs.Common;
using GWGUI.App.Localization.Extensions;

namespace GWGUI.App.Theme.OS.Editions.Windows31.Views;

internal partial class FileSelectionView : UserControl, IDisposable
{
    private const double FolderIndent = 12;
    private readonly FileSelectionDialog source;
    private bool updating;
    private bool disposed;

    internal FileSelectionView(FileSelectionDialog source)
    {
        this.source = source;
        try
        {
            InitializeComponent();
            NameField.Text = source.FileName;
            ReadOnly.Visibility = source.ShowReadOnly ? Visibility.Visible : Visibility.Collapsed;
            ReadOnly.IsChecked = source.ReadOnly;
            Types.ItemsSource = source.Filters;
            Types.SelectedIndex = source.FilterIndex;
            if (source.Kind == FileSelectionKind.Folder)
            {
                Files.Visibility = Visibility.Collapsed;
                Types.Visibility = Visibility.Collapsed;
                TypesLabel.Visibility = Visibility.Collapsed;
                FolderLabel.Visibility = Visibility.Collapsed;
                CurrentPath.Visibility = Visibility.Collapsed;
                NameLabel.SetBinding(ContentControl.ContentProperty, LocExtension.CreateBinding("Explorer.Directory"));
                Grid.SetColumnSpan(NameField, 3);
                Grid.SetColumn(Folders, 0);
                Grid.SetColumnSpan(Folders, 3);
                Grid.SetColumn(DriveLabel, 0);
                Grid.SetColumn(Drives, 0);
                Grid.SetColumnSpan(Drives, 3);
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
        NameField.Focus();
        NameField.SelectAll();
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
            Files.ItemsSource = source.Files;
            Files.IsEnabled = !source.Busy;
            var chain = new Stack<DirectoryInfo>();
            for (var folder = new DirectoryInfo(source.CurrentDirectory); folder is not null; folder = folder.Parent) chain.Push(folder);
            var folders = new List<FolderRow>();
            var level = 0;
            foreach (var folder in chain) folders.Add(new(folder, level++, true));
            var current = folders.LastOrDefault();
            foreach (var folder in source.Directories) folders.Add(new(folder, level, false));
            Folders.ItemsSource = folders;
            Folders.SelectedItem = current;
            Folders.IsEnabled = !source.Busy;
            Drives.ItemsSource = source.Drives;
            Drives.SelectedItem = source.Drives.FirstOrDefault(drive => string.Equals(drive.Name,
                Path.GetPathRoot(source.CurrentDirectory), StringComparison.OrdinalIgnoreCase));
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
        if (!updating && Files.SelectedItem is FileInfo file) NameField.Text = file.Name;
    }

    private void OnFolderSelected(object sender, SelectionChangedEventArgs args)
    {
        if (!updating && source.Kind == FileSelectionKind.Folder && Folders.SelectedItem is FolderRow folder)
            NameField.Text = folder.Folder.FullName;
    }

    private async void OnFilterSelected(object sender, SelectionChangedEventArgs args)
    {
        if (!updating && IsLoaded && Types.SelectedIndex >= 0) await source.SelectFilterAsync(Types.SelectedIndex);
    }

    private async void OnDriveSelected(object sender, SelectionChangedEventArgs args)
    {
        if (!updating && Drives.SelectedItem is DriveInfo drive) await NavigateAsync(drive.Name);
    }

    private async Task NavigateAsync(string path)
    {
        if (source.Kind == FileSelectionKind.Folder) NameField.Text = string.Empty;
        await source.NavigateAsync(path);
    }

    private async void OnFileDoubleClick(object sender, MouseButtonEventArgs args)
    {
        if (Files.SelectedItem is FileInfo file && ItemUnderPointer(args, Files))
        { args.Handled = true; NameField.Text = file.Name; await AcceptAsync(); }
    }

    private async void OnFolderDoubleClick(object sender, MouseButtonEventArgs args)
    {
        if (Folders.SelectedItem is FolderRow folder && ItemUnderPointer(args, Folders))
        { args.Handled = true; await NavigateAsync(folder.Folder.FullName); }
    }

    private static bool ItemUnderPointer(MouseButtonEventArgs args, ListBox list)
        => args.OriginalSource is DependencyObject target && ItemsControl.ContainerFromElement(list, target) is ListBoxItem;

    private async void OnAccept(object sender, RoutedEventArgs args) => await AcceptAsync();

    private async Task AcceptAsync()
    {
        await source.AcceptAsync();
        if (!disposed && NameField.Text != source.FileName) NameField.Text = source.FileName;
    }

    private void OnCancel(object sender, RoutedEventArgs args) => source.RequestPresentationClose();
    private void OnReadOnlyChanged(object sender, RoutedEventArgs args) => source.ReadOnly = ReadOnly.IsChecked == true;

    private async void OnKeyDown(object sender, KeyEventArgs args)
    {
        if (args.Key == Key.Escape) { args.Handled = true; source.RequestPresentationClose(); }
        else if (args.Key == Key.Enter && Folders.IsKeyboardFocusWithin && Folders.SelectedItem is FolderRow folder)
        { args.Handled = true; await NavigateAsync(folder.Folder.FullName); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        source.Changed -= OnChanged;
        Loaded -= OnLoaded;
        PreviewKeyDown -= OnKeyDown;
        if (NameField is null) return;
        NameField.TextChanged -= OnNameChanged;
        Files.SelectionChanged -= OnFileSelected;
        Files.MouseDoubleClick -= OnFileDoubleClick;
        Folders.SelectionChanged -= OnFolderSelected;
        Folders.MouseDoubleClick -= OnFolderDoubleClick;
        Types.SelectionChanged -= OnFilterSelected;
        Drives.SelectionChanged -= OnDriveSelected;
        Accept.Click -= OnAccept;
        Cancel.Click -= OnCancel;
        ReadOnly.Checked -= OnReadOnlyChanged;
        ReadOnly.Unchecked -= OnReadOnlyChanged;
        Files.ItemsSource = null;
        Folders.ItemsSource = null;
        Drives.ItemsSource = null;
        Types.ItemsSource = null;
        Content = null;
    }

    private sealed record FolderRow(DirectoryInfo Folder, int Level, bool Expanded)
    {
        public string Name => Folder.Name;
        public Thickness Indent => new(Level * FolderIndent, 0, 0, 0);
        public ImageSource Image => Windows31FileImages.FolderBranch(false, Expanded);
    }
}

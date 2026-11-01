using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using GWGUI.App.Contracts.ViewModels.Conversion;
using GWGUI.App.Theme.Common;
using GWGUI.App.Views.Controls.Common;
using GWGUI.App.Views.Controls.Conversion;
using GWGUI.App.Views.Controls.Shell;

namespace GWGUI.App.Theme.OS.Editions.WindowsXP.Views;

internal partial class ConversionView : UserControl, IDisposable
{
    private static readonly DependencyProperty PresentationsProperty = DependencyProperty.Register(
        "Presentations", typeof(IEnumerable), typeof(ConversionView), new PropertyMetadata(null, PresentationsChanged));
    private readonly ThemeBindingScope bindings = new();
    private readonly List<ConversionFormatRow> rows = [];
    private readonly List<CheckBox> extensionChoices = [];
    private readonly ConversionTabSection source;
    private readonly Action showAdvanced;
    private readonly Action close;
    private bool rebuilding;
    private bool disposed;
    private WindowsXPResources? resourceScope;

    internal ConversionView(ConversionTabSection source, ApplicationStatusBar status, Action showAdvanced, Action close, WindowsXPColorScheme scheme = WindowsXPColorScheme.Blue)
    {
        this.source = source;
        this.showAdvanced = showAdvanced;
        this.close = close;
        try
        {
            InitializeComponent();
            resourceScope = new WindowsXPResources(this, scheme);
            PageIcon.Source = WindowsXPApplicationIcons.ForResource("Tab.Convert");
            bindings.TwoWay(source.SourceBlock, PathSection.TextProperty, SourcePath, TextBox.TextProperty);
            bindings.OneWay(source.SourceBlock.Input, TextBoxBase.IsReadOnlyProperty, SourcePath, TextBoxBase.IsReadOnlyProperty);
            bindings.TwoWay(source.OutputBlock.OutputNameTextBox, TextBox.TextProperty, OutputName, TextBox.TextProperty);
            bindings.TwoWay(source.OutputBlock.TagsCheckBox, ToggleButton.IsCheckedProperty, Tags, ToggleButton.IsCheckedProperty);
            bindings.OneWay(source.OutputBlock.SourceInformation, TextBlock.TextProperty, SourceInformation, TextBlock.TextProperty);
            bindings.Action(source.SourceBlock.BrowseButton, Browse);
            bindings.Action(source.SourceBlock.BrowseButton, OpenCommand);
            bindings.Action(source.SourceBlock.ActionButton, PreviewCommand, followCaption: true);
            bindings.Action(source.ExecuteActionButton, Execute, followCaption: true);
            bindings.Action(source.ExecuteActionButton, ExecuteCommand, followCaption: true);
            bindings.Action(source.MigrationActionButton, MigrationCommand);
            bindings.Action(source.ProfileBlock.SaveButton, SaveProfile);
            bindings.Action(source.ProfileBlock.ResetButton, ResetProfile);
            Choice(source.ProfileBlock.ProfileCombo, Profiles);
            Choice(source.FormatsBlock.MachineSelector, Machines);
            bindings.OneWay(source.FormatsBlock.SelectedItems, ItemsControl.ItemsSourceProperty, SelectedOutputs, ItemsControl.ItemsSourceProperty);
            bindings.OneWay(status.OperationText, TextBlock.TextProperty, Operation, TextBlock.TextProperty);
            bindings.OneWay(source.FormatsBlock.FormatItems, ItemsControl.ItemsSourceProperty, this, PresentationsProperty);
        }
        catch { Dispose(); throw; }
    }

    private void Choice(Selector original, ComboBox target)
    {
        bindings.OneWay(original, ItemsControl.DisplayMemberPathProperty, target, ItemsControl.DisplayMemberPathProperty);
        bindings.OneWay(original, ItemsControl.ItemsSourceProperty, target, ItemsControl.ItemsSourceProperty);
        bindings.TwoWay(original, Selector.SelectedItemProperty, target, Selector.SelectedItemProperty);
        bindings.OneWay(original, IsEnabledProperty, target, IsEnabledProperty);
    }

    private static void PresentationsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) =>
        ((ConversionView)sender).RebuildFormats(args.NewValue as IEnumerable);

    private void RebuildFormats(IEnumerable? presentations)
    {
        if (disposed) return;
        var selectedId = (Formats.SelectedItem as ConversionFormatRow)?.Presentation.Format.Id;
        rebuilding = true;
        try
        {
            ReleaseRows();
            if (presentations is not null)
                foreach (var presentation in presentations.Cast<ConversionFormatPresentation>())
                {
                    var row = new ConversionFormatRow(presentation, RequestSelection);
                    rows.Add(row);
                    Formats.Items.Add(row);
                }
            Formats.SelectedItem = rows.FirstOrDefault(item => item.Presentation.Format.Id == selectedId) ?? rows.FirstOrDefault();
        }
        finally { rebuilding = false; }
        ShowExtensions();
    }

    private void RequestSelection(ConversionFormatPresentation selection)
    {
        if (!disposed) source.FormatsBlock.RequestSelection(selection.Format.Id, selection.IsSelected, selection.ExplicitExtensions);
    }

    private void FormatSelected(object sender, SelectionChangedEventArgs args)
    {
        if (!rebuilding && !disposed) ShowExtensions();
    }

    private void ShowExtensions()
    {
        ReleaseExtensions();
        if (Formats.SelectedItem is not ConversionFormatRow row) { FormatInformation.Text = string.Empty; return; }
        FormatInformation.Text = row.Presentation.Format.DisplayName;
        foreach (var extension in row.Presentation.Format.Extensions)
        {
            var check = new CheckBox
            {
                Content = extension.Extension.TrimStart('.').ToUpperInvariant(),
                Tag = extension.Extension,
                ToolTip = extension.DisplayName,
                Margin = new Thickness(0, 0, 12, 2),
                IsEnabled = row.Presentation.IsCompatible,
                IsChecked = row.Presentation.ExplicitExtensions.Contains(extension.Extension)
            };
            check.Checked += ExtensionChanged;
            check.Unchecked += ExtensionChanged;
            extensionChoices.Add(check);
            Extensions.Children.Add(check);
        }
    }

    private void ExtensionChanged(object sender, RoutedEventArgs args)
    {
        if (disposed || Formats.SelectedItem is not ConversionFormatRow row) return;
        var extensions = extensionChoices.Where(item => item.IsChecked == true).Select(item => (string)item.Tag)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        RequestSelection(row.Presentation with { IsSelected = row.SelectedForConversion, ExplicitExtensions = extensions });
    }

    private void AdvancedClick(object sender, RoutedEventArgs args) { if (!disposed) showAdvanced(); }
    private void CloseClick(object sender, RoutedEventArgs args) { if (!disposed) close(); }

    private void ReleaseExtensions()
    {
        try
        {
            foreach (var check in extensionChoices)
            {
                check.Checked -= ExtensionChanged;
                check.Unchecked -= ExtensionChanged;
            }
        }
        finally { extensionChoices.Clear(); Extensions.Children.Clear(); }
    }

    private void ReleaseRows()
    {
        try { foreach (var row in rows) row.Dispose(); }
        finally { rows.Clear(); Formats.Items.Clear(); }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { bindings.Dispose(); }
        finally
        {
            if (AdvancedCommand is not null) AdvancedCommand.Click -= AdvancedClick;
            if (CloseCommand is not null) CloseCommand.Click -= CloseClick;
            if (Formats is not null) Formats.SelectionChanged -= FormatSelected;
            try { if (Extensions is not null) ReleaseExtensions(); }
            finally
            {
                try { if (Formats is not null) ReleaseRows(); }
                finally
                {
                    if (Profiles is not null) Profiles.ItemsSource = null;
                    if (Machines is not null) Machines.ItemsSource = null;
                    if (SelectedOutputs is not null) SelectedOutputs.ItemsSource = null;
                    ClearValue(PresentationsProperty);
                    try { resourceScope?.Dispose(); }
                    finally { resourceScope = null; Content = null; }
                }
            }
        }
    }
}

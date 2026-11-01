using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using GWGUI.App.Theme.Common;
using GWGUI.App.Views.Controls.Common;
using GWGUI.App.Views.Controls.Shell;
using GWGUI.App.Views.Controls.Write;

namespace GWGUI.App.Theme.OS.Editions.WindowsXP.Views;

internal partial class WriteView : UserControl, IDisposable
{
    private readonly ThemeBindingScope bindings = new();
    private readonly Action showAdvanced;
    private readonly Action close;
    private bool disposed;
    private WindowsXPResources? resourceScope;

    internal WriteView(WriteTabSection source, ApplicationStatusBar status, Action showAdvanced, Action close, WindowsXPColorScheme scheme = WindowsXPColorScheme.Blue)
    {
        this.showAdvanced = showAdvanced;
        this.close = close;
        try
        {
            InitializeComponent();
            resourceScope = new WindowsXPResources(this, scheme);
            PageIcon.Source = WindowsXPApplicationIcons.ForResource("Tab.Write");
            bindings.TwoWay(source.SourceBlock, PathSection.TextProperty, SourcePath, TextBox.TextProperty);
            bindings.OneWay(source.SourceBlock.Input, TextBoxBase.IsReadOnlyProperty, SourcePath, TextBoxBase.IsReadOnlyProperty);
            bindings.Action(source.SourceBlock.BrowseButton, Browse);
            bindings.Action(source.SourceBlock.BrowseButton, OpenCommand);
            bindings.Action(source.ExecuteActionButton, Execute, followCaption: true);
            bindings.Action(source.ExecuteActionButton, WriteCommand, followCaption: true);
            bindings.Action(source.ProfileBlock.SaveButton, SaveProfile);
            bindings.Action(source.ProfileBlock.ResetButton, ResetProfile);
            bindings.Action(source.FormatBlock.ModifyButton, ModifyCommand);
            bindings.Action(source.FormatBlock.VisualizeTracksButton, VisualizeCommand);
            Choice(source.ProfileBlock.ProfileCombo, Profiles);
            Choice(source.FormatBlock.FormatCombo, Formats);
            bindings.OneWay(source.FormatBlock.DetectionText, TextBlock.TextProperty, Detection, TextBlock.TextProperty);
            bindings.OneWay(source.AdvancedBlock.NoVerifyCheckBox, ToggleButton.IsCheckedProperty, Verification, TagProperty);
            bindings.OneWay(status.HardwareText, TextBlock.TextProperty, Hardware, TextBlock.TextProperty);
        }
        catch { Dispose(); throw; }
    }

    private void Choice(ComboBox original, ComboBox target)
    {
        bindings.OneWay(original, ItemsControl.DisplayMemberPathProperty, target, ItemsControl.DisplayMemberPathProperty);
        bindings.OneWay(original, ItemsControl.ItemsSourceProperty, target, ItemsControl.ItemsSourceProperty);
        bindings.TwoWay(original, Selector.SelectedItemProperty, target, Selector.SelectedItemProperty);
        bindings.OneWay(original, IsEnabledProperty, target, IsEnabledProperty);
        bindings.OneWay(original, VisibilityProperty, target, VisibilityProperty);
    }

    private void AdvancedClick(object sender, RoutedEventArgs args) { if (!disposed) showAdvanced(); }
    private void CloseClick(object sender, RoutedEventArgs args) { if (!disposed) close(); }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { bindings.Dispose(); }
        finally
        {
            if (AdvancedCommand is not null) AdvancedCommand.Click -= AdvancedClick;
            if (CloseCommand is not null) CloseCommand.Click -= CloseClick;
            if (Profiles is not null) Profiles.ItemsSource = null;
            if (Formats is not null) Formats.ItemsSource = null;
            try { resourceScope?.Dispose(); }
            finally { resourceScope = null; Content = null; }
        }
    }

}

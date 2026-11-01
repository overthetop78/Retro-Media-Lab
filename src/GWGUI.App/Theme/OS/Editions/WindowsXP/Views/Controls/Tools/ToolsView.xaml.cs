using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using GWGUI.App.Theme.Common;
using GWGUI.App.Views.Controls.Shell;
using GWGUI.App.Views.Controls.Tools;

namespace GWGUI.App.Theme.OS.Editions.WindowsXP.Views;

internal partial class ToolsView : UserControl, IDisposable
{
    private readonly ThemeBindingScope bindings = new();
    private readonly ToolsTabSection source;
    private readonly Action close;
    private ThemeBindingScope? activeAction;
    private bool disposed;
    private WindowsXPResources? resourceScope;

    internal ToolsView(ToolsTabSection source, ApplicationStatusBar status, Action close, WindowsXPColorScheme scheme = WindowsXPColorScheme.Blue)
    {
        this.source = source;
        this.close = close;
        try
        {
            InitializeComponent();
            resourceScope = new WindowsXPResources(this, scheme);
            bindings.TwoWay(source.ToolsList, Selector.SelectedIndexProperty, Modes, Selector.SelectedIndexProperty);
            bindings.OneWay(source.ToolsList, IsEnabledProperty, Modes, IsEnabledProperty);
            Toggle(source.EraseTracksEnabled, EraseTracksEnabled);
            Toggle(source.EraseRevsEnabled, EraseRevolutionsEnabled);
            Toggle(source.CleanCylindersEnabled, CleanCylindersEnabled);
            Toggle(source.CleanPassesEnabled, CleanPassesEnabled);
            Toggle(source.CleanLingerEnabled, CleanLingerEnabled);
            Value(source.EraseTracksValue, EraseTracks);
            Value(source.EraseRevsValue, EraseRevolutions);
            Value(source.EraseExpertArguments, EraseArguments);
            Value(source.CleanCylindersValue, CleanCylinders);
            Value(source.CleanPassesValue, CleanPasses);
            Value(source.CleanLingerValue, CleanLinger);
            Value(source.CleanExpertArguments, CleanArguments);
            bindings.Action(source.EraseExecuteButton, EraseExecute, followCaption: true);
            bindings.Action(source.CleanExecuteButton, CleanExecute, followCaption: true);
            bindings.OneWay(status.HardwareText, TextBlock.TextProperty, Hardware, TextBlock.TextProperty);
            BindActiveAction();
        }
        catch { Dispose(); throw; }
    }

    private void Toggle(CheckBox original, CheckBox target)
    {
        bindings.TwoWay(original, ToggleButton.IsCheckedProperty, target, ToggleButton.IsCheckedProperty);
        Availability(original, target);
    }

    private void Value(TextBox original, TextBox target)
    {
        bindings.TwoWay(original, TextBox.TextProperty, target, TextBox.TextProperty);
        bindings.OneWay(original, TextBoxBase.IsReadOnlyProperty, target, TextBoxBase.IsReadOnlyProperty);
        Availability(original, target);
    }

    private void Availability(UIElement original, UIElement target)
    {
        bindings.OneWay(original, IsEnabledProperty, target, IsEnabledProperty);
        bindings.OneWay(original, VisibilityProperty, target, VisibilityProperty);
    }

    private void BindActiveAction()
    {
        activeAction?.Dispose();
        activeAction = new ThemeBindingScope();
        activeAction.Action(Modes.SelectedIndex == EraseIndex ? source.EraseExecuteButton : source.CleanExecuteButton,
            ExecuteCommand, followCaption: true);
    }

    private void ModeChanged(object sender, SelectionChangedEventArgs args)
    {
        if (IsInitialized && !disposed && ReferenceEquals(args.OriginalSource, Modes)) BindActiveAction();
    }

    private void CloseClick(object sender, RoutedEventArgs args) { if (!disposed) close(); }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { activeAction?.Dispose(); }
        finally
        {
            try { bindings.Dispose(); }
            finally
            {
                if (Modes is not null) Modes.SelectionChanged -= ModeChanged;
                if (CloseCommand is not null) CloseCommand.Click -= CloseClick;
                try { resourceScope?.Dispose(); }
                finally { resourceScope = null; Content = null; }
            }
        }
    }

    private const int EraseIndex = 0;
}

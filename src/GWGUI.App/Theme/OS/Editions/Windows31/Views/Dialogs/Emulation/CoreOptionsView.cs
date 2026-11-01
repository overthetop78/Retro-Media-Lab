using System.Windows;
using System.Windows.Controls;
using GWGUI.App.Theme.Common;
using GWGUI.App.Views.Dialogs.Emulation;
using GWGUI.Emulation;

namespace GWGUI.App.Theme.OS.Editions.Windows31.Views;

internal sealed class CoreOptionsView : BusinessDialogView
{
    private readonly List<SettingsFieldView> fields = [];
    private const double LabelWidth = 180;

    internal CoreOptionsView(CoreOptionsDialog source) : base(source)
    {
        try
        {
            var commands = ThemeDialogCompletion.CommandButtons(source);
            var cancel = Command("Common.Cancel", Cancel);
            Bindings.OneWay(commands.Cancel, IsEnabledProperty, cancel, IsEnabledProperty);
            var save = Forward(commands.Accept);
            save.IsDefault = true;
            Footer(cancel, save);
            var subtitle = new TextBlock { Text = source.PresentationSubtitle, TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8) };
            SetDock(subtitle, Dock.Top);
            Children.Add(subtitle);
            var body = new StackPanel();
            foreach (var group in source.PresentationOptions.GroupBy(option => option.Category))
            {
                var form = new StackPanel();
                foreach (var option in group)
                {
                    var definition = new EmulationSettingsField(option.Key, EmulationMachineTab.General, group.Key,
                        string.Empty, option.Control is TextBox ? EmulationSettingsEditor.Text : EmulationSettingsEditor.Selection, null);
                    var field = new SettingsFieldView(definition, option.Control, () => Task.CompletedTask) { ToolTip = option.Control.ToolTip };
                    fields.Add(field);
                    var row = new Grid { Margin = new Thickness(0, 3, 0, 5) };
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(LabelWidth) });
                    row.ColumnDefinitions.Add(new ColumnDefinition());
                    row.Children.Add(new TextBlock { Text = option.Label, TextWrapping = TextWrapping.Wrap,
                        VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
                    Grid.SetColumn(field, 1);
                    row.Children.Add(field);
                    form.Children.Add(row);
                }
                body.Children.Add(new GroupBox { Header = group.Key, Content = form, Margin = new Thickness(0, 0, 0, 10) });
            }
            Children.Add(new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        }
        catch { Dispose(); throw; }
    }

    protected override void Detach()
    {
        Exception? failure = null;
        try
        {
            foreach (var field in fields)
                try { field.Dispose(); }
                catch (Exception error) { failure ??= error; }
        }
        finally { fields.Clear(); }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
}

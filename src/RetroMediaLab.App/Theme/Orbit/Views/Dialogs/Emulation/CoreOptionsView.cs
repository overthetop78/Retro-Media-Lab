using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Dialogs.Emulation;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Theme.Orbit.Views;

internal sealed class CoreOptionsView : BusinessDialogView
{
    private readonly List<SettingsFieldView> fields = [];
    private const double LabelWidth = 150;

    internal CoreOptionsView(CoreOptionsDialog source, bool? dark = null) : base(source, dark)
    {
        try
        {
            var commands = ThemeDialogCompletion.CommandButtons(source);
            var cancel = Command("Common.Cancel", Cancel);
            Bindings.OneWay(commands.Cancel, IsEnabledProperty, cancel, IsEnabledProperty);
            var save = Forward(commands.Accept);
            save.IsDefault = true;
            Footer(save, cancel);
            var subtitle = new TextBlock { Text = source.PresentationSubtitle, TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8) };
            SetDock(subtitle, Dock.Top);
            Children.Add(subtitle);
            var tabs = new TabControl();
            Children.Add(tabs);
            foreach (var group in source.PresentationOptions.GroupBy(option => option.Category))
            {
                var form = new Grid { Margin = new Thickness(8) };
                form.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(LabelWidth) });
                form.ColumnDefinitions.Add(new ColumnDefinition());
                foreach (var option in group)
                {
                    var definition = new EmulationSettingsField(option.Key, EmulationMachineTab.General, group.Key,
                        string.Empty, option.Control is TextBox ? EmulationSettingsEditor.Text : EmulationSettingsEditor.Selection, null);
                    var field = new SettingsFieldView(definition, option.Control, () => Task.CompletedTask)
                        { ToolTip = option.Control.ToolTip, Margin = new Thickness(0, 3, 0, 5) };
                    fields.Add(field);
                    OrbitOptionPages.Row(form, new TextBlock { Text = option.Label, TextWrapping = TextWrapping.Wrap,
                        VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 3, 8, 5) }, field);
                }
                tabs.Items.Add(new TabItem { Header = group.Key, Content = new ScrollViewer { Content = form,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled } });
            }
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

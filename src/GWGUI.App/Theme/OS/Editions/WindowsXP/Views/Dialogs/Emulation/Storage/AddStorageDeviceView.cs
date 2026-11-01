using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using GWGUI.App.Theme.Common;
using GWGUI.App.Views.Dialogs.Emulation.Storage;

namespace GWGUI.App.Theme.OS.Editions.WindowsXP.Views;

internal sealed class AddStorageDeviceView : BusinessDialogView
{
    private readonly ComboBox type = new();
    internal AddStorageDeviceView(AddStorageDeviceDialog source, WindowsXPColorScheme scheme = WindowsXPColorScheme.Blue) : base(source, scheme)
    {
        try
        {
            var commands = ThemeDialogCompletion.CommandButtons(source);
            var add = Forward(commands.Accept);
            add.IsDefault = true;
            Footer(add, Command("Common.Cancel", Cancel));
            var form = new StackPanel();
            var label = Label("Emulation.Device.Name.Type");
            label.Margin = new Thickness(0, 0, 0, 6);
            form.Children.Add(label);
            Bindings.OneWay(source.PresentationType, ItemsControl.DisplayMemberPathProperty, type, ItemsControl.DisplayMemberPathProperty);
            Bindings.OneWay(source.PresentationType, ItemsControl.ItemsSourceProperty, type, ItemsControl.ItemsSourceProperty);
            Bindings.TwoWay(source.PresentationType, Selector.SelectedItemProperty, type, Selector.SelectedItemProperty);
            Bindings.OneWay(source.PresentationType, IsEnabledProperty, type, IsEnabledProperty);
            form.Children.Add(type);
            Children.Add(form);
        }
        catch { Dispose(); throw; }
    }
    protected override void Detach() => type.ItemsSource = null;
}

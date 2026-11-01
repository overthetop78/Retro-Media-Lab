using System.Windows;
using System.Windows.Controls;
using RetroMediaLab.App.Views.Dialogs.Hardware;

namespace RetroMediaLab.App.Theme.Orbit.Views;

internal sealed class HardwareUnavailableView : BusinessDialogView
{
    private readonly ListBox controllers = new();
    internal HardwareUnavailableView(HardwareUnavailableWindow source, bool? dark = null) : base(source, dark)
    {
        try
        {
            Footer(Forward(source.ContinueButton), Forward(source.SettingsButton), Forward(source.RetryButton));
            var heading = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            heading.Children.Add(Label("Hardware.StartupMissing"));
            heading.Children.Add(Label("Hardware.StartupExplanation"));
            SetDock(heading, Dock.Top);
            Children.Add(heading);
            Bindings.OneWay(source.MissingControllers, ItemsControl.ItemsSourceProperty, controllers, ItemsControl.ItemsSourceProperty);
            Children.Add(controllers);
        }
        catch { Dispose(); throw; }
    }
    protected override void Detach() => controllers.ItemsSource = null;
}

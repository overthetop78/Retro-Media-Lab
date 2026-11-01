using System.Windows;
using System.Windows.Controls;
using GWGUI.App.Views.Dialogs.Profiles;

namespace GWGUI.App.Theme.Orbit.Views;

internal sealed class ProfileNameView : BusinessDialogView
{
    private readonly ProfileNameWindow source;
    private readonly TextBox name = new();
    private readonly TextBlock required;

    internal ProfileNameView(ProfileNameWindow source, bool? dark = null) : base(source, dark)
    {
        this.source = source;
        required = Label("Profile.NameRequired");
        try
        {
            Footer(Command("Common.Save", source.RequestSave, true), Command("Common.Cancel", Cancel));
            var form = new StackPanel();
            form.Children.Add(Label("Profile.Name"));
            name.Margin = new Thickness(0, 6, 0, 8);
            form.Children.Add(name);
            required.Visibility = Visibility.Collapsed;
            form.Children.Add(required);
            Children.Add(form);
            Bindings.TwoWay(source.NameText, TextBox.TextProperty, name, TextBox.TextProperty);
            name.TextChanged += NameChanged;
            source.NameRequired += NameRequired;
            Loaded += SelectName;
        }
        catch { Dispose(); throw; }
    }

    private void NameRequired(object? sender, EventArgs args)
    {
        required.Visibility = Visibility.Visible;
        name.Focus();
    }
    private void SelectName(object sender, RoutedEventArgs args) { name.SelectAll(); name.Focus(); }
    private void NameChanged(object sender, TextChangedEventArgs args)
    {
        if (!string.IsNullOrWhiteSpace(name.Text)) required.Visibility = Visibility.Collapsed;
    }
    protected override void Detach()
    {
        source.NameRequired -= NameRequired;
        name.TextChanged -= NameChanged;
        Loaded -= SelectName;
    }
}

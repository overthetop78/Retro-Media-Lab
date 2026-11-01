using GWGUI.App.Services.Dialogs;
using GWGUI.App.Theme.Common;
using GWGUI.App.Localization.Extensions;
using System.Windows;

namespace GWGUI.App.Views.Dialogs.Profiles;

public partial class ProfileNameWindow : Window
{
    private readonly Action _accept;
    private readonly Action _nameRequired;
    public string ProfileName => NameText.Text.Trim();
    public ProfileNameWindow(string? initialName = null) : this(initialName, null, null) { }
    internal ProfileNameWindow(string? initialName, Action? accept, Action? nameRequired)
    {
        InitializeComponent(); NameText.Text = initialName ?? "";
        _accept = accept ?? (() => ThemeDialogCompletion.Complete(this, true));
        _nameRequired = nameRequired ?? (() => WpfMessageDialogService.ShowMessage(this, LocExtension.Get("Profile.NameRequired"), LocExtension.Get("Profile.Title"), MessageBoxButton.OK, MessageBoxImage.Information));
        Loaded += (_, _) => { NameText.SelectAll(); NameText.Focus(); };
    }
    internal event EventHandler? NameRequired;
    internal void RequestSave() => Save_Click(this, new RoutedEventArgs());

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ProfileName))
        {
            if (NameRequired is { } required) required(this, EventArgs.Empty);
            else _nameRequired();
            return;
        }
        _accept();
    }
}

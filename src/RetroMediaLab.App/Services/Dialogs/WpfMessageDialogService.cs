using RetroMediaLab.App.Enums.Services.Dialogs;
using RetroMediaLab.App.Interfaces.Services.Dialogs;
using RetroMediaLab.App.Services.Windows;
using RetroMediaLab.App.Theme.Common;
using RetroMediaLab.App.Views.Dialogs.Common;
using System.Windows;

namespace RetroMediaLab.App.Services.Dialogs;

public sealed class WpfMessageDialogService(Window owner) : IMessageDialogService
{
    public UserDialogResult Show(string message, string title, UserDialogButtons buttons = UserDialogButtons.Ok, UserDialogIcon icon = UserDialogIcon.None)
    {
        var result = ShowMessage(owner, message, title, buttons switch
        {
            UserDialogButtons.OkCancel => MessageBoxButton.OKCancel,
            UserDialogButtons.YesNo => MessageBoxButton.YesNo,
            UserDialogButtons.YesNoCancel => MessageBoxButton.YesNoCancel,
            _ => MessageBoxButton.OK
        }, icon switch
        {
            UserDialogIcon.Information => MessageBoxImage.Information,
            UserDialogIcon.Warning => MessageBoxImage.Warning,
            UserDialogIcon.Error => MessageBoxImage.Error,
            UserDialogIcon.Question => MessageBoxImage.Question,
            _ => MessageBoxImage.None
        });
        return result switch
        {
            MessageBoxResult.OK => UserDialogResult.Ok,
            MessageBoxResult.Cancel => UserDialogResult.Cancel,
            MessageBoxResult.Yes => UserDialogResult.Yes,
            MessageBoxResult.No => UserDialogResult.No,
            _ => UserDialogResult.None
        };
    }

    internal static MessageBoxResult ShowMessage(string message, string title, MessageBoxButton buttons = MessageBoxButton.OK,
        MessageBoxImage icon = MessageBoxImage.None, MessageBoxResult defaultResult = MessageBoxResult.None)
        => ShowMessage(WpfDialogOwner.Resolve(), message, title, buttons, icon, defaultResult);

    internal static MessageBoxResult ShowMessage(Window? owner, string message, string title, MessageBoxButton buttons = MessageBoxButton.OK,
        MessageBoxImage icon = MessageBoxImage.None, MessageBoxResult defaultResult = MessageBoxResult.None)
        => ShowMessage(owner, message, title, buttons, icon, defaultResult,
            (nativeOwner, text, caption, choices, image, preferred) =>
                MessageBox.Show(nativeOwner, text, caption, choices, image, preferred));

    internal static MessageBoxResult ShowMessage(Window? owner, string message, string title, MessageBoxButton buttons,
        MessageBoxImage icon, MessageBoxResult defaultResult,
        Func<Window?, string, string, MessageBoxButton, MessageBoxImage, MessageBoxResult, MessageBoxResult> native)
    {
        owner ??= WpfDialogOwner.Resolve();
        var request = new MessageDialog(message, title, buttons, icon, defaultResult);
        try
        {
            using var ownership = owner is null ? null : ThemeDialogCompletion.AttachOwner(request, owner);
            if (owner is not null && WpfWindowNavigationService.TryShowThemeWindow(owner, request)) return request.Result;
        }
        finally { request.Close(); }
        return native(WpfDialogOwner.Resolve(owner) ?? owner, message, title, buttons, icon, defaultResult);
    }

}

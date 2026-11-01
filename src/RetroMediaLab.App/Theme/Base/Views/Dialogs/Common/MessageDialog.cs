using System.Windows;
using RetroMediaLab.App.Theme.Common;

namespace RetroMediaLab.App.Views.Dialogs.Common;

/// <summary>Retains a message request and its result while a theme owns its presentation.</summary>
internal sealed class MessageDialog : Window
{
    internal string Message { get; }
    internal MessageBoxImage MessageIcon { get; }
    internal IReadOnlyList<MessageBoxResult> Choices { get; }
    internal MessageBoxResult DefaultResult { get; }
    internal MessageBoxResult Result { get; private set; }
    internal MessageBoxResult CancelResult { get; }

    internal MessageDialog(string message, string title, MessageBoxButton buttons, MessageBoxImage icon, MessageBoxResult defaultResult)
    {
        Message = message;
        Title = title;
        ResizeMode = ResizeMode.NoResize;
        MessageIcon = icon;
        Choices = buttons switch
        {
            MessageBoxButton.OK => [MessageBoxResult.OK],
            MessageBoxButton.OKCancel => [MessageBoxResult.OK, MessageBoxResult.Cancel],
            MessageBoxButton.YesNo => [MessageBoxResult.Yes, MessageBoxResult.No],
            MessageBoxButton.YesNoCancel => [MessageBoxResult.Yes, MessageBoxResult.No, MessageBoxResult.Cancel],
            MessageBoxButton.AbortRetryIgnore => [MessageBoxResult.Abort, MessageBoxResult.Retry, MessageBoxResult.Ignore],
            MessageBoxButton.RetryCancel => [MessageBoxResult.Retry, MessageBoxResult.Cancel],
            MessageBoxButton.CancelTryContinue => [MessageBoxResult.Cancel, MessageBoxResult.TryAgain, MessageBoxResult.Continue],
            _ => throw new ArgumentOutOfRangeException(nameof(buttons))
        };
        DefaultResult = Choices.Contains(defaultResult) ? defaultResult : Choices[0];
        CancelResult = buttons == MessageBoxButton.OK ? MessageBoxResult.OK
            : Choices.Contains(MessageBoxResult.Cancel) ? MessageBoxResult.Cancel : MessageBoxResult.None;
        Closing += (_, _) => { if (Result == MessageBoxResult.None) Result = CancelResult; };
    }

    internal void Choose(MessageBoxResult result)
    {
        if (!Choices.Contains(result)) throw new ArgumentOutOfRangeException(nameof(result));
        Result = result;
        ThemeDialogCompletion.Complete(this, result is not MessageBoxResult.Cancel and not MessageBoxResult.No and not MessageBoxResult.Abort);
    }

    internal void Cancel()
    {
        if (CancelResult != MessageBoxResult.None) Choose(CancelResult);
    }
}

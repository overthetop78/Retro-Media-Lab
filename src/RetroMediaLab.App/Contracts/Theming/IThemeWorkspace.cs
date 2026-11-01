namespace RetroMediaLab.App.Contracts.Theming;

internal interface IThemeWorkspace : IDisposable
{
    bool TryShowWindow(System.Windows.Window window) => false;
}

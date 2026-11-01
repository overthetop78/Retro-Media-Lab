using RetroMediaLab.App.Contracts.Theming;
using RetroMediaLab.App.Dictionaries.Theming;
using RetroMediaLab.App.Services.Theming;
using RetroMediaLab.Infrastructure.Settings;

namespace RetroMediaLab.App.Views.Windows.Shell;

public partial class MainWindow
{
    private IThemeWorkspace? themeWorkspace;
    private AppThemeDesign workspaceDesign;
    private string? workspaceEdition;

    internal bool TryShowThemeWindow(System.Windows.Window window) => themeWorkspace?.TryShowWindow(window) == true;

    internal void ApplyThemeWorkspace()
    {
        var definition = ThemeCatalog.Get(ThemeManager.CurrentDesign);
        var edition = ThemeManager.CurrentEdition;
        var createWorkspace = edition?.CreateWorkspace ?? definition.CreateWorkspace;
        if (workspaceDesign == definition.Id && workspaceEdition == edition?.Id
            && (themeWorkspace is not null || createWorkspace is null)) return;
        ReleaseThemeWorkspace();
        workspaceDesign = definition.Id;
        workspaceEdition = edition?.Id;
        if (createWorkspace is { } create)
        {
            try { themeWorkspace = create(ShellRoot, MainTabs); }
            catch
            {
                ReleaseThemeWorkspace();
                throw;
            }
        }
    }

    private void ReleaseThemeWorkspace()
    {
        try { themeWorkspace?.Dispose(); }
        finally { themeWorkspace = null; }
    }
}

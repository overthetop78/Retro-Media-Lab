using GWGUI.App.Contracts.Theming;
using GWGUI.App.Dictionaries.Theming;
using GWGUI.App.Services.Theming;
using GWGUI.Infrastructure.Settings;

namespace GWGUI.App.Views.Windows.Shell;

public partial class MainWindow
{
    private IThemeWorkspace? themeWorkspace;
    private AppThemeDesign workspaceDesign;

    internal void ApplyThemeWorkspace()
    {
        var definition = ThemeCatalog.Get(ThemeManager.CurrentDesign);
        if (workspaceDesign == definition.Id && (themeWorkspace is not null || definition.CreateWorkspace is null)) return;
        ReleaseThemeWorkspace();
        workspaceDesign = definition.Id;
        if (definition.CreateWorkspace is { } create)
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

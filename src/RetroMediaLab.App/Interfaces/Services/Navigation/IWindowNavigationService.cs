using RetroMediaLab.Infrastructure.Settings;
using RetroMediaLab.App.Contracts.Services.Navigation;
using RetroMediaLab.App.Enums.Services.Navigation;

namespace RetroMediaLab.App.Interfaces.Services.Navigation;

public interface IWindowNavigationService
{
    bool ShowPreferences(AppSettings settings, PreferencesSection section = PreferencesSection.General);
    void ShowEmulationPreferences(AppSettings settings);
    void ShowEmulationModuleOptions(AppSettings settings, string moduleId);
    void ShowUpdates();
    void ShowLogHistory(string logsDirectory);
    void ShowAbout();
    void ShowGwTool(GwToolWindowRequest request);
}

using RetroMediaLab.App.Services.Documentation;
using RetroMediaLab.App.Views.Windows.Shell;
using RetroMediaLab.App.Views.Controls.Shell;
using RetroMediaLab.App.Interfaces.Services.Dialogs;
using RetroMediaLab.App.Interfaces.Services.Navigation;
using RetroMediaLab.App.Enums.Services.Dialogs;
using RetroMediaLab.App.Localization.Extensions;
using RetroMediaLab.Infrastructure.HostTools;
using RetroMediaLab.Infrastructure.Commands.Execution;
using RetroMediaLab.Infrastructure.Hardware;
using RetroMediaLab.Infrastructure.Settings;
using RetroMediaLab.Tests.Application.TestInfrastructure;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
namespace RetroMediaLab.Tests.Interface.Help;
internal static class HelpTargetScenarios
{
    public static void Command(string language, string expectedLanguage)
    {
        var old = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
            var targets = new List<string>(); var errors = new List<Exception>(); var messages = new List<string>();
            var failure = new InvalidOperationException("synthetic browser failure"); var fail = true;
            var shell = new MainWindow(ControlledDependencies.Simulate<IMessageDialogService>((method, args) => {
                Assert.Equal("Show", method.Name); Assert.Equal(UserDialogIcon.Error, args[3]);
                Assert.Equal(LocExtension.Get("App.Title"), args[1]); messages.Add((string)args[0]!); return UserDialogResult.Ok;
            }), ControlledDependencies.Reject<IFileDialogService>(), ControlledDependencies.Reject<IBusinessDialogService>(),
                ControlledDependencies.Reject<IWindowNavigationService>(), hostTools: ControlledDependencies.Reject<IGwInstallationManager>(),
                runner: ControlledDependencies.Reject<IGreaseweazleRunner>(), settingsStore: ControlledDependencies.Reject<ISettingsStore>(),
                hardwareRegistry: ControlledDependencies.Reject<IHardwareRegistry>(),
                openDocumentation: url => { targets.Add(url); if (fail) throw failure; },
                logError: (error, context) =>
                {
                    Assert.Equal(LocExtension.Get("Menu.Documentation"), context);
                    errors.Add(error);
                }, dataDirectory: "virtual-data");
            var menu = Assert.IsType<MainMenu>(shell.FindName("ApplicationMenu"));
            var command = Assert.Single(menu.HelpMenuItem.Items.Cast<MenuItem>(), item => Equals(item.Header, LocExtension.Get("Menu.Documentation")));
            command.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Assert.Same(failure, Assert.Single(errors)); Assert.False(string.IsNullOrWhiteSpace(Assert.Single(messages)));
            fail = false;
            var tabs = Assert.IsType<TabControl>(shell.FindName("MainTabs"));
            for (var index = 0; index < tabs.Items.Count; index++) {
                tabs.SelectedIndex = index;
                command.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            }
            Assert.Equal(8, targets.Count);
            Assert.All(targets, url => Assert.Equal("https://github.com/overthetop78/Retro-Media-Lab/wiki/" + expectedLanguage + "-Guide", url));
            Assert.Single(errors); Assert.Single(messages); Assert.False(shell.IsLoaded);
        }
        finally { CultureInfo.CurrentUICulture = old; }
    }
    public static void Target(string language,string expected)
    {
        Assert.Equal("https://github.com/overthetop78/Retro-Media-Lab/wiki/"+expected+"-Guide",UserGuideLocator.GetUrl(language));
    }
}

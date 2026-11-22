using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using RetroMediaLab.App.Contracts.Emulation.Settings;
using RetroMediaLab.App.Views.Dialogs.Emulation.Storage;
using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Tests.Application.TestInfrastructure;

namespace RetroMediaLab.Tests.Interface.Themes;

[Collection("WPF")]
public sealed class CartridgeSettingsViewTests(StaExecutionScenarios sta)
{
    [Fact]
    public Task CartridgeSelectorsFollowChangesInAllThemes() => sta.RunAsync(async () =>
    {
        var first = Field("first", new("none", "none"), new("32x", "32X"));
        var second = Field("second", new("none", "none"), new("genie", "Game Genie"));
        Func<CartridgeSlotConfigurationDialog, Panel>[] themes =
        [
            source => new RetroMediaLab.App.Theme.Orbit.Views.CartridgeSettingsView(source),
            source => new RetroMediaLab.App.Theme.OS.Editions.Windows31.Views.CartridgeSettingsView(source),
            source => new RetroMediaLab.App.Theme.OS.Editions.Windows95.Views.CartridgeSettingsView(source),
            source => new RetroMediaLab.App.Theme.OS.Editions.WindowsXP.Views.CartridgeSettingsView(source)
        ];

        foreach (var theme in themes)
        {
            var dialog = new CartridgeSlotConfigurationDialog("Cartridge", "Mega Drive", new(), string.Empty, null,
                configurationFields: [first], describeOptions: values => values.GetValueOrDefault("first") == "32x" ? [first, second] : [first]);
            try
            {
                var view = theme(dialog);
                try
                {
                    Select(Assert.Single(Selectors(view)), "32x");
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    var expanded = Selectors(view).ToArray();
                    Assert.Equal(2, expanded.Length);
                    Select(expanded[1], "genie");
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    Assert.Equal("genie", dialog.Settings.Options!["second"]);
                    Select(Selectors(view).First(), "none");
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    Assert.Single(Selectors(view));
                    Assert.False(dialog.Settings.Options!.ContainsKey("second"));
                }
                finally { ((IDisposable)view).Dispose(); }
            }
            finally { dialog.Close(); }
        }
    });

    private static EmulationSettingsField Field(string id, params EmulationSettingsChoice[] choices) =>
        new(id, EmulationMachineTab.Storage, string.Empty, id, EmulationSettingsEditor.Selection, choices[0].Id, choices);

    private static void Select(ComboBox selector, string choice) =>
        selector.SetCurrentValue(ComboBox.SelectedItemProperty,
            selector.Items.Cast<EmulationSettingsChoiceView>().Single(item => item.Choice.Id == choice));

    private static IEnumerable<ComboBox> Selectors(DependencyObject node)
    {
        if (node is ComboBox selection)
        {
            yield return selection;
            yield break;
        }
        foreach (var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>())
        foreach (var selector in Selectors(child))
            yield return selector;
    }
}

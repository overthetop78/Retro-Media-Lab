using RetroMediaLab.App.Functions.Emulation.Settings;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;

namespace RetroMediaLab.Tests.Application;

public sealed class EmulationSettingsRuleTests
{
    [Theory]
    [InlineData("mosaic", "axlon")]
    [InlineData("axlon", "mosaic")]
    public void TheNewSelectionTakesPrecedenceOverThePreviousExpansion(string selected, string previous)
    {
        var settings = new EmulationMachineSettings("atari", new(new Dictionary<EmulationMachineTab, bool>()),
            [new("ram", EmulationMachineTab.Ram, "RAM",
                [new(selected, EmulationMachineTab.Ram, "ram", selected, EmulationSettingsEditor.Selection, "disabled"),
                 new(previous, EmulationMachineTab.Ram, "ram", previous, EmulationSettingsEditor.Selection, "enabled")])],
            [new(EmulationSettingsRuleCategory.MutuallyExclusive, "mosaic", "axlon", "disabled")]);

        var changes = EmulationSettingsRuleFunctions.ChangesFor(settings, selected, "enabled");

        Assert.Equal("enabled", changes[selected]);
        Assert.Equal("disabled", changes[previous]);
        Assert.Single(EmulationSettingsRuleFunctions.ChangesFor(settings, selected, "disabled"));
    }

    [Fact]
    public void AxlonShadowIsHiddenWhenItsSourceIsDisabled()
    {
        var rule = new EmulationSettingsRule(EmulationSettingsRuleCategory.VisibleWhenSourceDiffers, "axlon", "shadow", "disabled");
        Assert.True(EmulationSettingsRuleFunctions.HidesTarget(rule, "disabled"));
        Assert.False(EmulationSettingsRuleFunctions.HidesTarget(rule, "128"));
    }
}

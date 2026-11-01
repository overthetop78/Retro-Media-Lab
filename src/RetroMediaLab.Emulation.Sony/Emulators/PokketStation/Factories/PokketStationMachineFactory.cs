using RetroMediaLab.Emulation.Sony.Emulators.Common.Interop.Contracts;
using RetroMediaLab.Emulation.Sony.Emulators.Common.Interop.Factories;
using RetroMediaLab.Emulation.Sony.Emulators.PokketStation.Constants;
using RetroMediaLab.Emulation.Sony.Emulators.PokketStation.Functions;
using RetroMediaLab.Emulation.Sony.Emulators.PokketStation.Dictionaries;
using RetroMediaLab.Emulation.Sony.Emulators.Common.Interop.Functions;
using PocketInputs = RetroMediaLab.Emulation.Sony.Common.Machines.PocketStation.Functions.ControllerFunctions;

namespace RetroMediaLab.Emulation.Sony.Emulators.PokketStation.Factories;

internal sealed class PokketStationMachineFactory : CoreMachineFactory
{
    /// <inheritdoc/>
    public override Emulator Emulator => Emulator.PokketStation;
    protected override CoreDefinition CoreDefinition => CoreConstants.Definition;

    public override int GetControllerPortCount(MachineConfiguration configuration) => LinkFunctions.IsLinked(configuration) ? LinkConstants.UnitCount : 1;

    public override IReadOnlyList<EmulationMediaDevice> DescribeMediaDevices(MachineConfiguration configuration) =>
    [new(new EmulationMediaSlot(EmulationMediaCategory.MemoryCard, 1), EmulationMediaType.MemoryCard,
        RetroMediaLab.Emulation.Sony.Common.Machines.PocketStation.Constants.MediaConstants.FlashImageExtensions,
        RequiresMachineRecreation: true, DisplayLabel: LinkConstants.SecondaryCardLabel)];

    public override IReadOnlyList<InputBindingDefinition>? GetControllerBindingDefinitions(MachineConfiguration configuration, PeripheralCategory type, int port)
    {
        if (type == PeripheralCategory.None) return [];
        return LinkFunctions.IsLinked(configuration) && port == 0
            ? [.. PocketInputs.Definitions,
                new(EmulationControllerCommandIds.L, LinkConstants.SelectUnitALabel, string.Empty),
                new(EmulationControllerCommandIds.R, LinkConstants.SelectUnitBLabel, string.Empty)]
            : PocketInputs.Definitions;
    }

    public override IReadOnlyList<EmulationSettingsBlock> DescribeSettings(MachineConfiguration configuration, EmulatorManagementContext? context = null)
    {
        if (!LinkFunctions.IsLinked(configuration)) return [];
        var identity = OptionCatalog.All.Single(option => option.Key == LinkConstants.IdentityKey);
        var field = new EmulationSettingsField(identity.Key, EmulationMachineTab.General, LinkConstants.Group,
            identity.Name, EmulationSettingsEditor.Selection, configuration.Options?.GetValueOrDefault(identity.Key) ?? identity.DefaultValue,
            Choices: identity.Values.Select(value => new EmulationSettingsChoice(value.Value, string.Empty, InvariantDisplayValue: value.Label)).ToArray(),
            ExplanationResourceKey: LinkConstants.Help,
            RequiresRestart: true);
        return [.. CoreSettingsFunctions.Blocks(OptionCatalog.All.Where(option => option.Key != identity.Key).ToArray(), configuration),
            new(LinkConstants.Group, EmulationMachineTab.General, LinkConstants.GroupLabel, [field])];
    }
}

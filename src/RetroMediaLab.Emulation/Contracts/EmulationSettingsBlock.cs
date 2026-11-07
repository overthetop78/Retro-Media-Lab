namespace RetroMediaLab.Emulation.Contracts;

public sealed record EmulationSettingsBlock(
    string Id,
    EmulationMachineTab Tab,
    string TitleResourceKey,
    IReadOnlyList<EmulationSettingsField> Fields,
    string? Icon = null,
    int Columns = 1,
    bool IsVisible = true,
    bool IsCollapsible = false,
    bool IsCompact = false,
    string? VisibleWhenFieldId = null,
    string? VisibleWhenValue = null)
{
    /// <summary>Présente les champs de ce lecteur comme une colonne du mélangeur Audio commun.</summary>
    /// <remarks>Le module ne déclare la colonne que lorsque le lecteur est raccordé.</remarks>
    public EmulationMediaSlot? AudioMixerSlot { get; init; }
}

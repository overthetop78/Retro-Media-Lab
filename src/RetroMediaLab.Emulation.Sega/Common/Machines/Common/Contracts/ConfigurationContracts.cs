namespace RetroMediaLab.Emulation.Sega.Common.Machines.Common.Contracts;

public sealed record MachineConfiguration(
    string Model,
    string EmulatorId,
    IReadOnlyDictionary<string, string>? Options = null,
    Guid Id = default,
    IReadOnlyList<PeripheralCategory>? Controllers = null,
    InputConfiguration? Input = null,
    int SchemaVersion = ConfigurationStoreConstants.CurrentSchemaVersion,
    IReadOnlyList<MediaConfiguration>? Media = null,
    AudioConfiguration? Audio = null,
    IReadOnlyDictionary<string, string>? FirmwarePaths = null)
    : RetroMediaLab.Emulation.Interfaces.IEmulationConfiguration
{
    public string ModuleId => MachineConfigurationConstants.ModuleId;
    string RetroMediaLab.Emulation.Interfaces.IEmulationConfiguration.MachineId => Model;
    internal const string Sg1000ModelOption = "segaemu_sg1000_model";
    internal const string Sg1000RamExtensionOption = "configuration.sg1000RamExtension";
    internal const string Sg1000SecondModel = "sg-1000 II";
    internal const string Sg1000LegacyExtendedModel = "sg-1000 II + ram ext.";
    internal bool IsSg1000II => Options?.GetValueOrDefault(Sg1000ModelOption) is Sg1000SecondModel or Sg1000LegacyExtendedModel;
    internal bool HasSg1000RamExtension => IsSg1000II
        && (Options?.GetValueOrDefault(Sg1000RamExtensionOption) is { } extension
            ? extension == SettingsDescriptionFunctionsConstants.Enabled
            : Options?.GetValueOrDefault(Sg1000ModelOption) == Sg1000LegacyExtendedModel);
    internal bool IsMegaDriveModelII => Model == ModelConstants.MegaDriveII || Options?.GetValueOrDefault(SettingsConstants.MegaDriveModel) == ModelConstants.MegaDriveModelII;
    internal IReadOnlyList<string> MegaDriveRevisions => IsMegaDriveModelII
        ? ModelConstants.MegaDriveModelIIRevisions : ModelConstants.MegaDriveModelIRevisions;
    internal string MegaDriveRevision
    {
        get
        {
            string? revision = Options?.GetValueOrDefault(SettingsConstants.MegaDriveRevision);
            return revision is not null && MegaDriveRevisions.Contains(revision, StringComparer.Ordinal)
                ? revision : IsMegaDriveModelII ? "VA0" : "VA4";
        }
    }
    internal bool HasIntegratedMegaDriveFm => IsMegaDriveModelII
        ? MegaDriveRevision is not ("VA2" or "VA2.3") : MegaDriveRevision == ModelConstants.MegaDriveVa7;
    internal bool HasMegaDriveTmss => IsMegaDriveModelII || MegaDriveRevision is
        ModelConstants.MegaDriveVa5Tmss or ModelConstants.MegaDriveVa6 or ModelConstants.MegaDriveVa65 or ModelConstants.MegaDriveVa68 or ModelConstants.MegaDriveVa7;
    internal bool HasMegaDriveExtensionPort => !IsMegaDriveModelII && MegaDriveRevision is not
        (ModelConstants.MegaDriveVa65 or ModelConstants.MegaDriveVa68 or ModelConstants.MegaDriveVa7);
    public string MegaCdModel => Model == ModelConstants.MegaCdSecond ? ModelConstants.MegaCdII : Options?.GetValueOrDefault(SettingsConstants.MegaCdModel, ModelConstants.MegaCdI) ?? ModelConstants.MegaCdI;
    public bool MegaCdEnabled => Options?.TryGetValue(SettingsConstants.MegaCdEnabled, out var megaCdEnabled) == true && (bool.TryParse(megaCdEnabled, out var enabled) && enabled || megaCdEnabled.Equals(SettingsDescriptionFunctionsConstants.Enabled, StringComparison.OrdinalIgnoreCase));
    public bool ThirtyTwoXEnabled => Options?.TryGetValue(SettingsConstants.MegaDriveThirtyTwoX, out var value) == true && (bool.TryParse(value, out var enabled) && enabled || value.Equals(SettingsDescriptionFunctionsConstants.Enabled, StringComparison.OrdinalIgnoreCase));
    public MachineConfiguration EnsureId() => Id == Guid.Empty ? this with { Id = Guid.NewGuid() } : this;
}

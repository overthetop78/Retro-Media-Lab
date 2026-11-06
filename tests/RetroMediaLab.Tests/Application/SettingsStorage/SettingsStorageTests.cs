namespace RetroMediaLab.Tests.Application.SettingsStorage;
public class SettingsStorageTests
{
    [Fact]
    public void EmulatorShortcutsMigrateOnceToTheRequestedControlShiftLayout()
    {
        var settings = new RetroMediaLab.Infrastructure.Settings.AppSettings { SchemaVersion = 8 };
        settings.EmulationShortcuts[RetroMediaLab.Infrastructure.Settings.Emulation.EmulationShortcutDefaults.Power] = "Ctrl+Alt+End";
        settings.EmulationShortcuts[RetroMediaLab.Infrastructure.Settings.Emulation.EmulationShortcutDefaults.InsertMedia] = "Ctrl+I";
        settings.EmulationShortcuts[RetroMediaLab.Infrastructure.Settings.Emulation.EmulationShortcutDefaults.FastForward] = "Ctrl+F11";
        RetroMediaLab.Infrastructure.Settings.SettingsMigrator.Migrate(settings);
        Assert.Equal("Ctrl+Shift+F1", settings.EmulationShortcuts[RetroMediaLab.Infrastructure.Settings.Emulation.EmulationShortcutDefaults.Power]);
        Assert.Equal("Ctrl+Shift+F5", settings.EmulationShortcuts[RetroMediaLab.Infrastructure.Settings.Emulation.EmulationShortcutDefaults.OpenSettings]);
        Assert.Equal("Ctrl+Shift+F12", settings.EmulationShortcuts[RetroMediaLab.Infrastructure.Settings.Emulation.EmulationShortcutDefaults.ReleaseMouse]);
        Assert.DoesNotContain(RetroMediaLab.Infrastructure.Settings.Emulation.EmulationShortcutDefaults.InsertMedia, settings.EmulationShortcuts.Keys);
        Assert.DoesNotContain(RetroMediaLab.Infrastructure.Settings.Emulation.EmulationShortcutDefaults.FastForward, settings.EmulationShortcuts.Keys);
        settings.EmulationShortcuts[RetroMediaLab.Infrastructure.Settings.Emulation.EmulationShortcutDefaults.Power] = "Ctrl+Shift+P";
        RetroMediaLab.Infrastructure.Settings.SettingsMigrator.Migrate(settings);
        Assert.Equal("Ctrl+Shift+P", settings.EmulationShortcuts[RetroMediaLab.Infrastructure.Settings.Emulation.EmulationShortcutDefaults.Power]);
    }
    [Theory] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    public void LegacyFormatTagsAndEngineStructuresAreMigrated(int version) => MigrationAndRecoveryScenarios.LegacyStructures(version);
    [Fact] public Task SaveProducesExpectedJsonAndKeepsBackup()=>SettingsRoundTripScenarios.Save();
    [Fact] public Task LoadUsesInMemoryJsonAndMissingDefaults()=>SettingsRoundTripScenarios.Load();
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void OldSchemasMigrate(int version)=>MigrationAndRecoveryScenarios.Migrate(version);
    [Fact] public Task InvalidJsonIsPreservedAndBackupRecovered()=>MigrationAndRecoveryScenarios.Recover();
    [Fact] public Task FailedReplacementPreservesCurrentSettings()=>MigrationAndRecoveryScenarios.FailedSave();
    [Fact] public Task FutureSchemaDoesNotOverwriteOriginal()=>MigrationAndRecoveryScenarios.FutureSchema();
    [Fact] public Task PathsStayWithinInjectedStore()=>StorageLocationScenarios.Paths();
}

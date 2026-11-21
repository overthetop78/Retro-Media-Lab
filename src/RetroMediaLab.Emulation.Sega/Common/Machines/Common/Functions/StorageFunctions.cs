namespace RetroMediaLab.Emulation.Sega.Common.Machines.Common.Functions;

internal static class StorageSettingsFunctions
{
    internal static bool IsMegaCdDriveField(string id) =>
        id is "segaemu_system_bram" or "segaemu_cart_bram" or "segaemu_cart_size"
            or "segaemu_cd_latency" or "segaemu_cd_precache";

    private static IReadOnlyList<EmulationSettingsField> MegaCdDriveFields(MachineConfiguration configuration)
    {
        if (configuration.EmulatorId != Emulators.SegaEmu.Constants.CoreConstants.Id
            || !ModelConstants.HasMegaCd(configuration.Model) && !configuration.MegaCdEnabled)
            return [];
        bool hasMemoryCartridge = CartridgeStackFunctions.Read(configuration).Contains(CartridgeStackFunctions.CdMemory);
        return new Engine().Adapter(configuration).GetMachineSettingsBlocks(configuration)
            .SelectMany(block => block.Fields)
            .Where(field => IsMegaCdDriveField(field.Id)
                && (hasMemoryCartridge || field.Id is not ("segaemu_cart_bram" or "segaemu_cart_size")))
            .Select(field => field.Id == "segaemu_cart_size"
                ? field with { Value = field.Value == "disabled" ? "4meg" : field.Value,
                    Choices = field.Choices?.Where(choice => choice.Id != "disabled").ToArray() }
                : field).ToArray();
    }

    private static IReadOnlyList<EmulationSettingsBlock> MegaCdDriveBlocks(IReadOnlyList<EmulationSettingsField> fields)
    {
        if (fields.Count == 0) return [];
        var groups = new List<EmulationSettingsBlock>();
        Add("disc", "Emulation.Storage.Cd.Device", fields.Where(field => field.Id is "segaemu_cd_latency" or "segaemu_cd_precache"));
        Add("internal", "Emulation.Option.segaemu.segaemu_system_bram", fields.Where(field => field.Id == "segaemu_system_bram"));
        Add("backup-cartridge", "Emulation.Option.segaemu.segaemu_cart_bram", fields.Where(field => field.Id is "segaemu_cart_bram" or "segaemu_cart_size"));
        return groups;

        void Add(string id, string title, IEnumerable<EmulationSettingsField> entries)
        {
            var group = entries.ToArray();
            if (group.Length > 0)
                groups.Add(new(id, EmulationMachineTab.Storage, title, group, Columns: 1));
        }
    }

    internal static EmulationStorageSettings Describe(MachineConfiguration configuration)
    {
        var model = ModelCatalog.Get(configuration.Model);
        var contentExtensions = new Engine().Adapter(configuration).SupportedContentExtensions;
        var cartridgeFormats = model.Id switch
        {
            ModelConstants.MegaDrive or ModelConstants.MegaDriveII or ModelConstants.MegaCd or ModelConstants.MegaCdSecond or ModelConstants.ThirtyTwoX or ModelConstants.MegaCdThirtyTwoX or ModelConstants.Pico => StorageSettingsFunctionsConstants.MegaDriveCartridgeExtensions,
            ModelConstants.Naomi or ModelConstants.Naomi2 or ModelConstants.Atomiswave or ModelConstants.SystemSp or ModelConstants.StV or ModelConstants.Model3 => StorageSettingsFunctionsConstants.ArcadeCartridgeExtensions,
            _ => StorageSettingsFunctionsConstants.EightBitCartridgeExtensions
        };
        var options = configuration.Options ?? new Dictionary<string, string>();
        string? converter = configuration.EmulatorId == Emulators.SegaEmu.Constants.CoreConstants.Id
            ? ModelConstants.GetMasterSystemConverter(model.Id, options) : null;
        bool configurableCartridge = configuration.EmulatorId == Emulators.SegaEmu.Constants.CoreConstants.Id
            && ModelConstants.IsMegaDriveFamily(model.Id);
        var supportsCard = model.SupportsSegaCardSlot || converter == ModelConstants.MasterSystemConverterI;
        var supportsGlasses = model.SupportsThreeDGlasses || converter == ModelConstants.MasterSystemConverterI;
        string? accessory = configuration.EmulatorId == Emulators.SegaEmu.Constants.CoreConstants.Id
            && ModelConstants.IsMegaDriveFamily(model.Id) && converter is null ? options.GetValueOrDefault("segaemu_lock_on") switch
        {
            "sonic & knuckles" => "Sonic & Knuckles",
            "game genie" => "Game Genie",
            "action replay (pro)" => "Action Replay (Pro)",
            _ => null
        } : null;
        var devices = new List<EmulationMediaDevice>();
        for (var index = 0; index < model.MaximumFloppyDriveCount; index++)
            devices.Add(new EmulationMediaDevice(new EmulationMediaSlot(EmulationMediaCategory.FloppyDrive, index), EmulationMediaType.Floppy,
                contentExtensions.Where(StorageSettingsFunctionsConstants.FloppyExtensions.Contains).ToArray(),
                DisplayLabel: index == 0 ? StorageSettingsFunctionsConstants.FloppyDriveLabel : StorageSettingsFunctionsConstants.SecondFloppyDriveLabel,
                IsPermanent: index < model.BuiltInFloppyDriveCount));
        if (model.SupportsTapeDrive)
            devices.Add(new EmulationMediaDevice(EmulationMediaSlot.Tape0, EmulationMediaType.Tape,
                contentExtensions.Where(StorageSettingsFunctionsConstants.TapeExtensions.Contains).ToArray(),
                RequiresMachineRecreation: true,
                DisplayLabel: StorageSettingsFunctionsConstants.TapeDriveLabel,
                IsPermanent: model.HasBuiltInTapeDrive));
        if (model.SupportsCartridgeSlot)
            devices.Add(new EmulationMediaDevice(EmulationMediaSlot.Cartridge0, EmulationMediaType.Cartridge,
                contentExtensions.Where(extension => (converter is null ? cartridgeFormats.Contains(extension) : extension == ".sms")
                    && (extension != StorageSettingsFunctionsConstants.ThirtyTwoX
                        || !ModelConstants.IsMegaDriveFamily(model.Id)
                        || ModelConstants.HasThirtyTwoX(model.Id) || configuration.ThirtyTwoXEnabled))
                    .ToArray(), RequiresMachineRecreation: true,
                DisplayLabel: converter is not null ? "Master System · " + StorageSettingsFunctionsConstants.CartridgeSlotLabel
                    : accessory is null ? StorageSettingsFunctionsConstants.CartridgeSlotLabel
                    : StorageSettingsFunctionsConstants.CartridgeSlotLabel + " · " + accessory,
                IsPermanent: model.HasBuiltInCartridgeSlot,
                ConfigurationKind: configurableCartridge ? EmulationStorageConfigurationKind.CartridgeSlot : EmulationStorageConfigurationKind.None)
            {
                DescribeCartridgeOptions = configurableCartridge ? values => CartridgeStackFunctions.Describe(configuration, values) : null
            });
        var megaCdEnabled = model.Id == ModelConstants.MegaDrive && configuration.MegaCdEnabled && configuration.MegaCdModel is ModelConstants.MegaCdI or ModelConstants.MegaCdII;
        var threeDGlassesEnabled = supportsGlasses && OptionEnabled(options, SettingsConstants.MasterSystemThreeDGlasses);
        if (supportsCard)
            devices.Add(new EmulationMediaDevice(EmulationMediaSlot.Cartridge1, EmulationMediaType.Cartridge,
                threeDGlassesEnabled ? [] : [StorageSettingsFunctionsConstants.Mv], RequiresMachineRecreation: true,
                DisplayLabel: StorageSettingsFunctionsConstants.SegaCardSlotLabel,
                IsPermanent: model.HasBuiltInSegaCardSlot || supportsCard && converter is not null,
                ConfigurationKind: supportsGlasses ? EmulationStorageConfigurationKind.CartridgeSlot : EmulationStorageConfigurationKind.None,
                ConfigurationOptionResourceKey: supportsGlasses ? SettingsDescriptionFunctionsConstants.ResourceMasterSystemThreeDGlasses : null,
                ConfigurationOptionDetailedResourceKey: supportsGlasses ? SettingsDescriptionFunctionsConstants.ResourceMasterSystemThreeDGlassesHelp : null));
        IReadOnlyList<string> opticalExtensions = contentExtensions.Where(StorageSettingsFunctionsConstants.OpticalExtensions.Contains).ToArray();
        var driveFields = MegaCdDriveFields(configuration);
        if (model.SupportsCompactDiscDrive || megaCdEnabled)
            devices.Add(new EmulationMediaDevice(EmulationMediaSlot.Cd0, EmulationMediaType.CompactDisc,
                opticalExtensions, RequiresMachineRecreation: true,
                DisplayLabel: StorageSettingsFunctionsConstants.CompactDiscDriveLabel,
                IsPermanent: model.HasBuiltInCompactDiscDrive,
                ConfigurationKind: driveFields.Count > 0 ? EmulationStorageConfigurationKind.CompactDiscDrive : EmulationStorageConfigurationKind.None,
                ConfigurationFields: driveFields,
                ConfigurationBlocks: MegaCdDriveBlocks(driveFields)));
        var configuredFloppies = Math.Clamp(OptionInt(options, StorageSettingsFunctionsConstants.FloppyDriveCountOption, model.BuiltInFloppyDriveCount), model.BuiltInFloppyDriveCount, model.MaximumFloppyDriveCount);
        var configured = devices.Where(device => device.Slot.Category switch
        {
            EmulationMediaCategory.FloppyDrive => device.Slot.Index < configuredFloppies,
            EmulationMediaCategory.TapeDrive => model.HasBuiltInTapeDrive
                || OptionBool(options, StorageSettingsFunctionsConstants.TapeDriveEnabledOption),
            EmulationMediaCategory.CartridgeSlot => device.Slot.Index == 0
                ? model.HasBuiltInCartridgeSlot
                    || OptionBool(options, StorageSettingsFunctionsConstants.CartridgeSlotEnabledOption)
                : model.HasBuiltInSegaCardSlot || converter == ModelConstants.MasterSystemConverterI
                    || OptionBool(options, StorageSettingsFunctionsConstants.SegaCardSlotEnabledOption),
            EmulationMediaCategory.CompactDiscDrive => model.HasBuiltInCompactDiscDrive
                || megaCdEnabled
                || OptionBool(options, StorageSettingsFunctionsConstants.CompactDiscDriveEnabledOption),
            _ => false
        }).Select(device => device.Slot).ToArray();
        var mounted = EmulationMediaConversionFunctions.ToCommon(configuration.Media ?? []);
        var deviceSettings = new List<EmulationStorageDeviceSettings>();
        if (configurableCartridge)
            deviceSettings.Add(new(EmulationMediaSlot.Cartridge0, Cartridge: new(Options: CartridgeStackFunctions.EditorValues(configuration))));
        if (driveFields.Count > 0)
            deviceSettings.Add(new(EmulationMediaSlot.Cd0, CompactDisc: new(Options: driveFields
                .Where(field => field.Value is not null)
                .ToDictionary(field => field.Id, field => field.Value!, StringComparer.Ordinal))));
        if (supportsCard)
            deviceSettings.Add(new(EmulationMediaSlot.Cartridge1, Cartridge: new(threeDGlassesEnabled)));
        return new EmulationStorageSettings(devices, configured, mounted, deviceSettings);
    }

    internal static MachineConfiguration Apply(MachineConfiguration configuration, EmulationStorageSettings settings)
    {
        var model = ModelCatalog.Get(configuration.Model);
        var options = new Dictionary<string, string>(configuration.Options ?? new Dictionary<string, string>(), StringComparer.Ordinal);
        var cartridge = (settings.DeviceSettings ?? []).FirstOrDefault(item => item.Slot == EmulationMediaSlot.Cartridge0)?.Cartridge;
        bool stackChanged = configuration.EmulatorId == Emulators.SegaEmu.Constants.CoreConstants.Id
            && ModelConstants.IsMegaDriveFamily(model.Id) && cartridge?.Options is { } stackValues
            && CartridgeStackFunctions.Apply(configuration, stackValues, options);
        if ((settings.DeviceSettings ?? []).FirstOrDefault(item => item.Slot == EmulationMediaSlot.Cd0)?.CompactDisc?.Options is { } driveOptions)
            foreach (var field in MegaCdDriveFields(configuration with { Options = options }))
                if (driveOptions.TryGetValue(field.Id, out string? value)
                    && field.Choices?.Any(choice => choice.Id == value) == true)
                    options[field.Id] = value;
        var converter = configuration.EmulatorId == Emulators.SegaEmu.Constants.CoreConstants.Id
            ? ModelConstants.GetMasterSystemConverter(model.Id, options) : null;
        var threeDGlassesEnabled = (model.SupportsThreeDGlasses || converter == ModelConstants.MasterSystemConverterI)
            && (settings.DeviceSettings ?? []).FirstOrDefault(item => item.Slot == EmulationMediaSlot.Cartridge1)?.Cartridge ?.OptionEnabled == true;
        var media = settings.MountedMedia.Where(item => !threeDGlassesEnabled || item.Slot != EmulationMediaSlot.Cartridge1)
            .Select(item => new MediaConfiguration(
            item.Path, item.Type switch
            {
                EmulationMediaType.Floppy => MediaCategory.Floppy,
                EmulationMediaType.Tape => MediaCategory.Tape,
                EmulationMediaType.Cartridge => MediaCategory.Cartridge,
                EmulationMediaType.CompactDisc => MediaCategory.CompactDisc,
                _ => throw new ArgumentOutOfRangeException(nameof(settings), item.Type, null)
            }, item.Slot, IsReadOnly: item.IsReadOnly, IsInserted: item.IsInserted,
            MountOrder: item.Slot.Index))
            .Where(item => !stackChanged || ConfigurationValidationFunctions.Supports(model, item.Category, item.Slot, options)
                && !ConfigurationValidationFunctions.IsIncompatibleMedia(model, item, options))
            .ToArray();
        foreach (var item in media)
            if (!ConfigurationValidationFunctions.Supports(model, item.Category, item.Slot, options)
                || ConfigurationValidationFunctions.IsIncompatibleMedia(model, item, options)
                || item.Slot == EmulationMediaSlot.Cartridge1 && IsSegaCardLocked(model, options))
                throw new ArgumentOutOfRangeException(nameof(settings), item.Category, null);
        options = new Dictionary<string, string>(options, StringComparer.Ordinal)
        {
            [StorageSettingsFunctionsConstants.FloppyDriveCountOption] = settings.ConfiguredSlots.Count(slot => slot.Category == EmulationMediaCategory.FloppyDrive).ToString(System.Globalization.CultureInfo.InvariantCulture),
            [StorageSettingsFunctionsConstants.TapeDriveEnabledOption] = settings.ConfiguredSlots.Contains(EmulationMediaSlot.Tape0).ToString(),
            [StorageSettingsFunctionsConstants.CartridgeSlotEnabledOption] = settings.ConfiguredSlots.Contains(EmulationMediaSlot.Cartridge0).ToString(),
            [StorageSettingsFunctionsConstants.SegaCardSlotEnabledOption] = settings.ConfiguredSlots.Contains(EmulationMediaSlot.Cartridge1).ToString(),
            [SettingsConstants.MasterSystemThreeDGlasses] = threeDGlassesEnabled.ToString(),
            [StorageSettingsFunctionsConstants.CompactDiscDriveEnabledOption] = settings.ConfiguredSlots.Contains(EmulationMediaSlot.Cd0).ToString()
        };
        return configuration with { Media = media, Options = options };
    }

    private static int OptionInt(IReadOnlyDictionary<string, string> options, string key, int defaultValue) => options.TryGetValue(key, out var value) && int.TryParse(value, out var parsed) ? parsed : defaultValue;

    private static bool OptionBool(IReadOnlyDictionary<string, string> options, string key) => options.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed) && parsed;

    private static bool OptionEnabled(IReadOnlyDictionary<string, string> options, string key) => OptionBool(options, key) || options.TryGetValue(key, out var value) && value.Equals(SettingsDescriptionFunctionsConstants.Enabled, StringComparison.OrdinalIgnoreCase);

    private static bool IsSegaCardLocked(Model model, IReadOnlyDictionary<string, string> options) =>
        (model.SupportsThreeDGlasses || ModelConstants.GetMasterSystemConverter(model.Id, options) == ModelConstants.MasterSystemConverterI)
        && OptionEnabled(options, SettingsConstants.MasterSystemThreeDGlasses);
}

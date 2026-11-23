namespace RetroMediaLab.Emulation.Sega.Common.Machines.Common.Functions;

internal static partial class SettingsDescriptionFunctions
{
    internal static IReadOnlyList<EmulationSettingsBlock> Create(MachineConfiguration configuration)
    {
        var model = ModelCatalog.Get(configuration.Model);
        var adapter = new Engine().Adapter(configuration);
        var options = configuration.Options ?? new Dictionary<string, string>();
        var memoryKib = adapter.GetMemoryKib(configuration);
        long ramBytes = model.RamBytes ?? checked((long)((memoryKib is > 0 ? memoryKib.Value : model.RamKib) * 1024m));
        string ram = model.SupportsRamConfiguration
            ? options.GetValueOrDefault(SettingsConstants.Ram, model.RamKib.ToString(System.Globalization.CultureInfo.InvariantCulture))
            : ramBytes < 1024 ? ramBytes.ToString(System.Globalization.CultureInfo.InvariantCulture) + " B"
            : (ramBytes / 1024m).ToString(System.Globalization.CultureInfo.InvariantCulture) + " KiB";
        var processors = string.Join(" / ", model.Processors);
        var hardwareFields = new List<EmulationSettingsField>();
        EmulationSettingsField? systemRegion = null;
        var biosOptions = new List<EmulationSettingsField>();
        bool segaEmu = configuration.EmulatorId == Emulators.SegaEmu.Constants.CoreConstants.Id;
        bool sg1000 = segaEmu && model.Id == ModelConstants.Sg1000;
        bool megaDrive = segaEmu && ModelConstants.IsMegaDriveFamily(model.Id);
        bool has32X = megaDrive && (ModelConstants.HasThirtyTwoX(model.Id) || configuration.ThirtyTwoXEnabled);
        string? frequency = model.CpuFrequency;
        if (has32X && !ModelConstants.HasThirtyTwoX(model.Id))
        {
            processors = ModelConstants.CpuHitachiSh2 + " ×2 / " + processors;
            frequency = "23 MHz ×2 / " + frequency;
        }
        if (megaDrive)
        {
            if (ModelConstants.HasMegaCd(model.Id))
                hardwareFields.Add(Select(SettingsConstants.MegaDriveModel, EmulationMachineTab.General,
                        SettingsDescriptionFunctionsConstants.Hardware,
                        SettingsDescriptionFunctionsConstants.ResourceMegaDriveModel,
                        options.GetValueOrDefault(SettingsConstants.MegaDriveModel, ModelConstants.MegaDriveModelI),
                        [new(ModelConstants.MegaDriveModelI, SettingsDescriptionFunctionsConstants.ResourceMegaDriveModelI),
                         new(ModelConstants.MegaDriveModelII, SettingsDescriptionFunctionsConstants.ResourceMegaDriveModelII)], true) with { RefreshSettingsOnChange = true });
            hardwareFields.Add(Select(SettingsConstants.MegaDriveRevision, EmulationMachineTab.General,
                SettingsDescriptionFunctionsConstants.Hardware, "Emulation.Sega.MegaDrive.Revision",
                configuration.MegaDriveRevision, configuration.MegaDriveRevisions.Select(revision => Invariant(revision, revision)), true)
                with { RefreshSettingsOnChange = true });
            systemRegion = Select(SettingsConstants.MegaDriveRegion, EmulationMachineTab.Cpu,
                        SettingsDescriptionFunctionsConstants.Cpu,
                        SettingsDescriptionFunctionsConstants.ResourceMegaDriveRegion,
                        options.GetValueOrDefault(SettingsConstants.MegaDriveRegion, ModelConstants.RegionAutomatic),
                        [new(ModelConstants.RegionAutomatic, SettingsDescriptionFunctionsConstants.ResourceMegaDriveRegionAutomatic),
                         new(ModelConstants.RegionNtscU, SettingsDescriptionFunctionsConstants.ResourceMegaDriveRegionNtscU),
                         new(ModelConstants.RegionNtscJ, SettingsDescriptionFunctionsConstants.ResourceMegaDriveRegionNtscJ),
                         new(ModelConstants.RegionPal, SettingsDescriptionFunctionsConstants.ResourceMegaDriveRegionPal)], true);

            if (ModelConstants.HasMegaCd(model.Id))
            {
                biosOptions.Add(Toggle(SettingsConstants.MegaCdRegionFree, EmulationMachineTab.Cpu,
                        SettingsDescriptionFunctionsConstants.Firmware, "Emulation.Sega.MegaCd.RegionFree",
                        options.GetValueOrDefault(SettingsConstants.MegaCdRegionFree) is "enabled" or "True"));
            }

            if (configuration.HasMegaDriveExtensionPort)
            {
                hardwareFields.Add(Information("hardware.ext", EmulationMachineTab.General,
                    SettingsDescriptionFunctionsConstants.Hardware, "Emulation.Sega.MegaDrive.Ext", "DE-9"));
            }
        }
        else if (model.Id == ModelConstants.MegaDrive)
        {
            hardwareFields.AddRange(new EmulationSettingsField[]
            {
                Select(SettingsConstants.MegaDriveModel, EmulationMachineTab.General,
                    SettingsDescriptionFunctionsConstants.Hardware,
                    SettingsDescriptionFunctionsConstants.ResourceMegaDriveModel,
                    options.GetValueOrDefault(SettingsConstants.MegaDriveModel, ModelConstants.MegaDriveModelI),
                    [new(ModelConstants.MegaDriveModelI, SettingsDescriptionFunctionsConstants.ResourceMegaDriveModelI),
                     new(ModelConstants.MegaDriveModelII, SettingsDescriptionFunctionsConstants.ResourceMegaDriveModelII)], true),
                Select(SettingsConstants.MegaCdModel, EmulationMachineTab.General,
                    SettingsDescriptionFunctionsConstants.Hardware,
                    SettingsDescriptionFunctionsConstants.ResourceMegaCdModel,
                    options.GetValueOrDefault(SettingsConstants.MegaCdModel, ModelConstants.MegaCdI),
                    [new(ModelConstants.MegaCdI, SettingsDescriptionFunctionsConstants.ResourceMegaCdI),
                     new(ModelConstants.MegaCdII, SettingsDescriptionFunctionsConstants.ResourceMegaCdII)], true),
                Toggle(SettingsConstants.MegaCdEnabled, EmulationMachineTab.General,
                    SettingsDescriptionFunctionsConstants.Hardware,
                    SettingsDescriptionFunctionsConstants.ResourceMegaCdEnabled,
                    options.GetValueOrDefault(SettingsConstants.MegaCdEnabled, SettingsDescriptionFunctionsConstants.Disabled)
                        == SettingsDescriptionFunctionsConstants.Enabled, true),
                Toggle(SettingsConstants.MegaDriveThirtyTwoX, EmulationMachineTab.General,
                    SettingsDescriptionFunctionsConstants.Hardware,
                    SettingsDescriptionFunctionsConstants.ResourceMegaDriveThirtyTwoX,
                    options.GetValueOrDefault(SettingsConstants.MegaDriveThirtyTwoX, SettingsDescriptionFunctionsConstants.Disabled)
                        == SettingsDescriptionFunctionsConstants.Enabled, true),
                Select(SettingsConstants.MegaDriveRegion, EmulationMachineTab.General,
                    SettingsDescriptionFunctionsConstants.Hardware,
                    SettingsDescriptionFunctionsConstants.ResourceMegaDriveRegion,
                    options.GetValueOrDefault(SettingsConstants.MegaDriveRegion, ModelConstants.RegionAutomatic),
                    [new(ModelConstants.RegionAutomatic, SettingsDescriptionFunctionsConstants.ResourceMegaDriveRegionAutomatic),
                     new(ModelConstants.RegionNtscU, SettingsDescriptionFunctionsConstants.ResourceMegaDriveRegionNtscU),
                     new(ModelConstants.RegionNtscJ, SettingsDescriptionFunctionsConstants.ResourceMegaDriveRegionNtscJ),
                     new(ModelConstants.RegionPal, SettingsDescriptionFunctionsConstants.ResourceMegaDriveRegionPal),
                     new(ModelConstants.RegionSecam, SettingsDescriptionFunctionsConstants.ResourceMegaDriveRegionSecam)], true),
                Select(SettingsConstants.MegaDriveVideoStandard, EmulationMachineTab.General,
                    SettingsDescriptionFunctionsConstants.Hardware,
                    SettingsDescriptionFunctionsConstants.ResourceMegaDriveVideoStandard,
                    options.GetValueOrDefault(SettingsConstants.MegaDriveVideoStandard, ModelConstants.VideoStandardAutomatic),
                    [new(ModelConstants.VideoStandardAutomatic, SettingsDescriptionFunctionsConstants.ResourceMegaDriveVideoStandardAutomatic),
                     new(ModelConstants.VideoStandard50Hz, SettingsDescriptionFunctionsConstants.ResourceMegaDriveVideoStandard50Hz),
                     new(ModelConstants.VideoStandard60Hz, SettingsDescriptionFunctionsConstants.ResourceMegaDriveVideoStandard60Hz)], true)
            });
        }
        EmulationSettingsBlock[] blocks =
        [
            Block(SettingsDescriptionFunctionsConstants.General, EmulationMachineTab.General,
                SettingsDescriptionFunctionsConstants.ResourceGeneral,
                SettingsDescriptionFunctionsConstants.IconGeneral, 2,
                Information(SettingsConstants.Model, EmulationMachineTab.General, SettingsDescriptionFunctionsConstants.General, SettingsDescriptionFunctionsConstants.ResourceMachineModel, model.DisplayName),
                Information(SettingsConstants.Emulator, EmulationMachineTab.General, SettingsDescriptionFunctionsConstants.General, SettingsDescriptionFunctionsConstants.ResourceEmulator, adapter.Definition.DisplayName)),
            Block(SettingsDescriptionFunctionsConstants.Hardware, EmulationMachineTab.General, "Options.HardwareTitle", SettingsDescriptionFunctionsConstants.IconGeneral, 2, hardwareFields.ToArray()),
            Block(SettingsDescriptionFunctionsConstants.Cpu, EmulationMachineTab.Cpu,
                SettingsDescriptionFunctionsConstants.ResourceCpuProcessor,
                SettingsDescriptionFunctionsConstants.IconCpu, 2,
                Information(SettingsConstants.Model + ".cpu", EmulationMachineTab.Cpu, SettingsDescriptionFunctionsConstants.Cpu, SettingsDescriptionFunctionsConstants.ResourceCpuModel, processors),
                Information(SettingsConstants.Model + ".frequency", EmulationMachineTab.Cpu,
                    SettingsDescriptionFunctionsConstants.Cpu,
                    SettingsDescriptionFunctionsConstants.ResourceCpuSpeed,
                    frequency ?? SettingsDescriptionFunctionsConstants.CpuFrequency)),
            Block(SettingsDescriptionFunctionsConstants.Memory, EmulationMachineTab.Ram,
                SettingsDescriptionFunctionsConstants.ResourceMemoryMain,
                SettingsDescriptionFunctionsConstants.IconMemory, 1,
                model.SupportsRamConfiguration
                    ? Select(SettingsConstants.Ram, EmulationMachineTab.Ram, SettingsDescriptionFunctionsConstants.Memory, SettingsDescriptionFunctionsConstants.ResourceMemoryMain, ram, MemoryChoices(model), true)
                    : Information(SettingsConstants.Ram, EmulationMachineTab.Ram, SettingsDescriptionFunctionsConstants.Memory, SettingsDescriptionFunctionsConstants.ResourceMemoryMain, ram) with { NumericValue = ramBytes }),
            Block(SettingsDescriptionFunctionsConstants.Firmware, EmulationMachineTab.Rom,
                SettingsDescriptionFunctionsConstants.ResourceRom,
                SettingsDescriptionFunctionsConstants.IconFirmware, 2,
                FirmwareConfigurationFunctions.Fields(configuration, adapter).ToArray()),
            Block(SettingsDescriptionFunctionsConstants.Video, EmulationMachineTab.Video,
                SettingsDescriptionFunctionsConstants.ResourceVideo,
                SettingsDescriptionFunctionsConstants.IconVideo, 2,
                Information(SettingsConstants.VideoChip, EmulationMachineTab.Video,
                    SettingsDescriptionFunctionsConstants.Video,
                    SettingsDescriptionFunctionsConstants.ResourceVideoChip,
                    has32X ? ModelConstants.VideoThirtyTwoX : model.VideoChip ?? SettingsDescriptionFunctionsConstants.Unknown)),
            Block(SettingsDescriptionFunctionsConstants.Audio, EmulationMachineTab.Audio,
                SettingsDescriptionFunctionsConstants.ResourceAudio,
                SettingsDescriptionFunctionsConstants.IconAudio, 2,
                Toggle(SettingsConstants.AudioEnabled, EmulationMachineTab.Audio, SettingsDescriptionFunctionsConstants.Audio, SettingsDescriptionFunctionsConstants.ResourceAudioEnabled, configuration.AudioEnabled),
                AudioOutput(configuration.Audio?.OutputDeviceId),
                Select(SettingsConstants.AudioLatency, EmulationMachineTab.Audio,
                    SettingsDescriptionFunctionsConstants.Audio,
                    SettingsDescriptionFunctionsConstants.ResourceAudioLatency,
                    (configuration.Audio?.LatencyMilliseconds ?? AudioConstants.DefaultLatencyMilliseconds).ToString(),
                    AudioConstants.LatencyChoicesMilliseconds.Select(value => Invariant(value.ToString(), $"{value} ms", value))),
                Information(SettingsConstants.AudioChip, EmulationMachineTab.Audio,
                    SettingsDescriptionFunctionsConstants.Audio,
                    SettingsDescriptionFunctionsConstants.ResourceAudioChip,
                    megaDrive ? MegaDriveAudioChip(configuration) : model.AudioChip ?? SettingsDescriptionFunctionsConstants.Unknown))
        ];
        var result = blocks.Where(block => block.Fields.Count > 0).ToList();
        if (model.Id is ModelConstants.Sg1000 or ModelConstants.Sc3000 or ModelConstants.Sf7000
            or ModelConstants.MarkIII or ModelConstants.MasterSystem or ModelConstants.MasterSystemII or ModelConstants.GameGear)
        {
            const string videoMemory = "Emulation.Memory.Video";
            result.Add(Block(videoMemory, EmulationMachineTab.Video, videoMemory,
                SettingsDescriptionFunctionsConstants.IconMemory, 1,
                Information("configuration.videoMemory", EmulationMachineTab.Video, videoMemory, videoMemory,
                    "16 KiB")));
        }
        if (sg1000 && configuration.IsSg1000II)
        {
            const string extensions = "Emulation.Memory.Extensions";
            result.Add(Block("sg1000-ram-extension", EmulationMachineTab.Ram, extensions,
                SettingsDescriptionFunctionsConstants.IconMemory, 1,
                Toggle(MachineConfiguration.Sg1000RamExtensionOption, EmulationMachineTab.Ram, "sg1000-ram-extension",
                    extensions, configuration.HasSg1000RamExtension) with { NumericValue = 7 * 1024, RequiresRestart = true }));
        }
        foreach (var originalBlock in adapter.GetMachineSettingsBlocks(configuration))
        {
            var coreBlock = originalBlock;
            if (sg1000)
            {
                var modelField = coreBlock.Fields.FirstOrDefault(field => field.Id == MachineConfiguration.Sg1000ModelOption);
                if (modelField is not null)
                {
                    var cpuBlock = result.Find(block => block.Tab == EmulationMachineTab.Cpu)!;
                    result[result.IndexOf(cpuBlock)] = cpuBlock with
                    {
                        Fields = [.. cpuBlock.Fields, modelField with
                        {
                            Tab = EmulationMachineTab.Cpu, BlockId = cpuBlock.Id,
                            Value = configuration.IsSg1000II ? MachineConfiguration.Sg1000SecondModel : "sg-1000",
                            Choices = modelField.Choices!.Where(choice => choice.Id != MachineConfiguration.Sg1000LegacyExtendedModel).ToArray(),
                            RefreshSettingsOnChange = true
                        }]
                    };
                }
                coreBlock = coreBlock with
                {
                    Fields = coreBlock.Fields.Where(field => field.Id is not
                        (MachineConfiguration.Sg1000ModelOption or "segaemu_vdp_mode" or "segaemu_left_border"
                        or "segaemu_render" or "segaemu_blargg_ntsc_filter" or "segaemu_aspect_ratio"
                        or "segaemu_overscan" or "segaemu_sound_output")).Select(field => field.Id switch
                        {
                            "segaemu_frameskip" => field with { RefreshSettingsOnChange = true },
                            "segaemu_frameskip_threshold" => field with { IsVisible = options.GetValueOrDefault("segaemu_frameskip") == "manual" },
                            _ => field
                        }).ToArray()
                };
            }
            if (segaEmu && Emulators.SegaEmu.Constants.OptionMachineConstants.MasterSystem.Contains(model.Id))
                coreBlock = coreBlock with
                {
                    Fields = coreBlock.Fields.Where(field => field.Id != "segaemu_bios"
                        && (field.Id != "segaemu_aspect_ratio" || model.Id == ModelConstants.MarkIII)).Select(field =>
                        field.Id == "segaemu_region_detect" ? field with { RefreshSettingsOnChange = true } : field).ToArray()
                };
            if (ModelConstants.IsMegaDriveFamily(model.Id) && configuration.EmulatorId == Emulators.SegaEmu.Constants.CoreConstants.Id)
            {
                var fields = coreBlock.Fields.Where(field => field.Id is not
                    ("segaemu_region_detect" or "segaemu_vdp_mode" or "segaemu_bios" or "segaemu_add_on" or "segaemu_ym2612" or "segaemu_lock_on")
                    && !StorageSettingsFunctions.IsMegaCdDriveField(field.Id)).ToArray();
                if (fields.Length == 0) continue;
                coreBlock = coreBlock with { Fields = fields };
            }
            if (coreBlock.Fields.Count == 0) continue;
            var existingIndex = result.FindIndex(block => block.Tab == coreBlock.Tab);
            if (existingIndex < 0)
            {
                result.Add(coreBlock);
                continue;
            }
            var existing = result[existingIndex];
            result[existingIndex] = existing with
            {
                Fields = existing.Fields.Concat(coreBlock.Fields.Select(field => field with
                {
                    BlockId = existing.Id
                })).ToArray(),
                Columns = Math.Min(existing.Columns, coreBlock.Columns)
            };
        }
        var cpu = result.Find(block => block.Tab == EmulationMachineTab.Cpu)!;
        static bool IsClockField(EmulationSettingsField field) =>
            field.Id == SettingsConstants.Model + ".frequency" || field.Id == "segaemu_overclock";
        string accelerationId = cpu.Id + ".acceleration";
        result[result.IndexOf(cpu)] = cpu with { IsCompact = true,
            Fields = cpu.Fields.Where(field => !IsClockField(field))
                .Concat(systemRegion is null ? [] : new[] { systemRegion }).ToArray() };
        result.Add(cpu with { Id = accelerationId, IsCompact = true,
            TitleResourceKey = "Emulation.Cpu.Acceleration",
            Fields = cpu.Fields.Where(IsClockField).Select(field => field with { BlockId = accelerationId }).ToArray() });
        if (segaEmu)
        {
            var video = result.Find(block => block.Tab == EmulationMachineTab.Video)!;
            var rendering = video.Fields.Where(field => field.Id is "segaemu_render" or "segaemu_frameskip"
                or "segaemu_frameskip_threshold" or "segaemu_no_sprite_limit"
                or "segaemu_enhanced_vscroll" or "segaemu_enhanced_vscroll_limit").ToArray();
            var signal = video.Fields.Where(field => field.Id == "segaemu_blargg_ntsc_filter").ToArray();
            result[result.IndexOf(video)] = video with { IsCompact = true,
                Fields = video.Fields.Except(rendering).Except(signal).ToArray() };
            AddVideoBlock(".rendering", "Emulation.Video.Settings.Rendering", rendering);
            AddVideoBlock(".signal", "Emulation.Video.Settings.SignalSimulation", signal);

            void AddVideoBlock(string suffix, string title, IReadOnlyList<EmulationSettingsField> fields)
            {
                if (fields.Count == 0) return;
                string id = video.Id + suffix;
                result.Add(video with { Id = id, TitleResourceKey = title, IsCompact = true,
                    Fields = fields.Select(field => field with { BlockId = id }).ToArray() });
            }
        }
        PlaceFirmware(result, configuration, adapter, biosOptions);
        SplitAudio(result);
        return result;
    }

    private static void PlaceFirmware(List<EmulationSettingsBlock> blocks, MachineConfiguration configuration,
        IEmulatorAdapter adapter, IReadOnlyList<EmulationSettingsField> biosOptions)
    {
        var firmware = blocks.Find(block => block.Tab == EmulationMachineTab.Rom);
        if (firmware is null && biosOptions.Count == 0) return;
        if (firmware is not null) blocks.Remove(firmware);
        firmware ??= Block(SettingsDescriptionFunctionsConstants.Firmware, EmulationMachineTab.Rom,
            SettingsDescriptionFunctionsConstants.ResourceRom, SettingsDescriptionFunctionsConstants.IconFirmware, 1);
        var files = adapter.GetFirmwareSlots(configuration).ToDictionary(slot => slot.FieldId, slot => slot.FileName, StringComparer.Ordinal);
        bool Is32X(EmulationSettingsField field) => files.GetValueOrDefault(field.Id)
            is "32X_G_BIOS.BIN" or "32X_M_BIOS.BIN" or "32X_S_BIOS.BIN";
        bool IsCartridge(EmulationSettingsField field) => files.GetValueOrDefault(field.Id)
            is "sk.bin" or "sk2chip.bin" or "ggenie.bin" or "areplay.bin";
        if (biosOptions.Count > 0)
            blocks.Add(firmware with { Id = firmware.Id + ".cpu", Tab = EmulationMachineTab.Cpu,
                Fields = biosOptions.Select(field => field with { BlockId = firmware.Id + ".cpu" }).ToArray(),
                TitleResourceKey = "Emulation.Firmware", IsCompact = true, Columns = 1 });
        var rom = firmware.Fields.Where(field => !Is32X(field) && !IsCartridge(field)).ToArray();
        if (rom.Length > 0)
            blocks.Add(firmware with { Fields = rom, IsCompact = true, Columns = 1 });
        AddRomBlock(".32x", "Emulation.Sega.Help.MegaDrive.32X.Short", firmware.Fields.Where(Is32X));
        AddRomBlock(".cartridge", "Emulation.Option.segaemu.segaemu_lock_on", firmware.Fields.Where(IsCartridge));

        void AddRomBlock(string suffix, string title, IEnumerable<EmulationSettingsField> fields)
        {
            string id = firmware.Id + suffix;
            var entries = fields.Select(field => field with { BlockId = id }).ToArray();
            if (entries.Length > 0)
                blocks.Add(firmware with { Id = id, TitleResourceKey = title, Fields = entries, IsCompact = true });
        }
    }

    private static void SplitAudio(List<EmulationSettingsBlock> blocks)
    {
        var audio = blocks.Find(block => block.Tab == EmulationMachineTab.Audio)!;
        static bool IsHostAudio(EmulationSettingsField field) =>
            field.Id == SettingsConstants.AudioEnabled || field.Id == SettingsConstants.AudioLatency
            || field.ChoiceSource == EmulationSettingsChoiceSource.AudioOutputDevices;
        static bool IsMixingField(EmulationSettingsField field) => field.Id.EndsWith("_volume", StringComparison.Ordinal)
            || field.Id.EndsWith("_preamp", StringComparison.Ordinal) || field.Id.Contains("_audio_eq_", StringComparison.Ordinal);
        var emulated = audio.Fields.Where(field => !IsHostAudio(field)).ToArray();
        blocks.Remove(audio);
        blocks.Add(audio with { Fields = emulated.Where(field => !IsMixingField(field)).ToArray(), IsCompact = true });
        var mixing = emulated.Where(IsMixingField).ToArray();
        var psg = mixing.Where(field => field.Id.StartsWith("segaemu_psg_", StringComparison.Ordinal)).ToArray();
        var fm = mixing.Where(field => field.Id.StartsWith("segaemu_md_channel_", StringComparison.Ordinal)
            || field.Id == "segaemu_fm_preamp").ToArray();
        AddAudioBlock(".mixing", "Emulation.Audio.Mixer", mixing.Except(psg).Except(fm));
        AddAudioBlock(".psg", "Emulation.Sega.Audio.Psg", psg);
        AddAudioBlock(".fm", "Emulation.Sega.Audio.Fm", fm);

        void AddAudioBlock(string suffix, string title, IEnumerable<EmulationSettingsField> settings)
        {
            string id = audio.Id + suffix;
            var fields = settings.Select(field => field with { BlockId = id }).ToArray();
            if (fields.Length != 0)
                blocks.Add(audio with { Id = id, TitleResourceKey = title, IsCompact = true, Fields = fields });
        }
        blocks.Add(audio with { Id = audio.Id + ".host", Fields = audio.Fields.Where(IsHostAudio)
            .Select(field => field with { BlockId = audio.Id + ".host" }).ToArray(), IsCompact = true });
    }

    private static string MegaDriveAudioChip(MachineConfiguration configuration)
    {
        string chips = configuration.HasIntegratedMegaDriveFm ? "Yamaha YM3438 / SN76489" : "Yamaha YM2612 / SN76489";
        if (ModelConstants.HasMegaCd(configuration.Model) || configuration.MegaCdEnabled)
            chips += " / RF5C164";
        if (ModelConstants.HasThirtyTwoX(configuration.Model) || configuration.ThirtyTwoXEnabled)
            chips += " / PWM";
        return chips;
    }

    private static IEnumerable<EmulationSettingsChoice> MemoryChoices(Model model)
    {
        var values = new[] { model.RamKib, 64, 128, 192, 576 }.Where(value => value >= model.RamKib).Distinct().Order();
        return values.Select(value => Invariant(value.ToString(), $"{value} KiB", value));
    }

}

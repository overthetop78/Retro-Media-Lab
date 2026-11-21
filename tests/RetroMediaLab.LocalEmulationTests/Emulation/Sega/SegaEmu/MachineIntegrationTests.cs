using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Contracts;
using System.Buffers.Binary;
using System.Reflection;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction;
using RetroMediaLab.Emulation.Sega.Common.Machines.Common.Constants;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Sega.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Sega.Common.Machines.Common.Functions;
using RetroMediaLab.Emulation.Sega.Common.Services;
using RetroMediaLab.Emulation.Sega.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Functions;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Boards;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Factories;

namespace RetroMediaLab.Tests.Emulation.Sega.SegaEmu;

public sealed class MachineIntegrationTests
{
    public static TheoryData<string, string, byte, string> Machines => new()
    {
        { ModelConstants.Sg1000, "Sg1000Machine", 1, ".sg" },
        { ModelConstants.Sc3000, "Sc3000Machine", 1, ".sg" },
        { ModelConstants.MarkIII, "MarkIIIMachine", 0x10, ".sms" },
        { ModelConstants.MasterSystem, "MasterSystemMachine", 0x20, ".sms" },
        { ModelConstants.MasterSystem, "MasterSystemMachine", 0x20, ".mv" },
        { ModelConstants.MasterSystemII, "MasterSystemMachine", 0x21, ".sms" },
        { ModelConstants.GameGear, "GameGearMachine", 0x40, ".gg" },
        { ModelConstants.MegaDrive, "MegaDriveMachine", 0x80, ".md" },
        { ModelConstants.Pico, "PicoMachine", 0x82, ".md" },
    };

    [Theory, MemberData(nameof(Machines))]
    public void MachineRunsAndRestoresItsInMemoryProgram(string model, string type, byte hardware, string extension)
    {
        SegaBoard? board = null;
        try
        {
            board = BoardFactory.Create(model, Resources());
            Assert.Equal(type, board.GetType().Name);
            Assert.True(board.Load("synthetic" + extension, Program(hardware)));
            Assert.Equal(hardware, board.Hardware);
            board.RefreshConfiguration();
            short[] audio = new short[4096];
            for (int frame = 0; frame < 3; frame++) Assert.InRange(board.RunFrame(audio), 700, 900);
            byte[] saved = new byte[board.StateSize];
            Assert.Equal(saved.Length, board.SaveState(saved));
            board.RunFrame(audio);
            byte[] expected = new byte[board.StateSize]; board.SaveState(expected);
            Assert.Equal(saved.Length, board.LoadState(saved));
            board.RunFrame(audio);
            byte[] replay = new byte[board.StateSize]; board.SaveState(replay);
            Assert.Equal(expected, replay);
            if (hardware < 0x80)
            {
                foreach (string field in new[] { "_mainCpuComponent", "_mdComponent", "_cdComponent", "_cdCartridgeComponent", "_megaSdComponent", "_yx5200Component" })
                    Assert.Null(typeof(SegaBoard).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(board));
            }
        }
        finally { board?.Dispose(); }
    }

    [Theory]
    [InlineData(ModelConstants.MegaDrive, false, false)]
    [InlineData(ModelConstants.MegaDriveII, false, true)]
    [InlineData(ModelConstants.MegaCd, true, true)]
    [InlineData(ModelConstants.MegaCdSecond, true, false)]
    public void MegaDriveConfigurationsExposeTheirOwnHardware(string model, bool hasCd, bool has32X)
    {
        var machine = new MachineConfiguration(model, CoreConstants.Id, new Dictionary<string, string>
        {
            [SettingsConstants.MegaDriveThirtyTwoX] = has32X ? "enabled" : "disabled"
        });
        var settings = SettingsDescriptionFunctions.Create(machine).SelectMany(block => block.Fields).ToArray();
        Assert.Equal(hasCd, settings.Any(field => field.Id == SettingsConstants.MegaDriveModel));
        Assert.Contains(settings, field => field.Id == SettingsConstants.MegaDriveRegion
            && field.Tab == RetroMediaLab.Emulation.Enums.EmulationMachineTab.Cpu);
        Assert.DoesNotContain(settings, field => field.Id == SettingsConstants.MegaCdModel || field.Id == "segaemu_ym2612");
        Assert.Equal(hasCd, settings.Any(field => field.Id == RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants.FirmwareConstants.MegaCdField
            && field.Tab == RetroMediaLab.Emulation.Enums.EmulationMachineTab.Rom));
        Assert.Equal(has32X, settings.Any(field => field.Id == "configuration.firmware.segaemu.12"
            && field.Tab == RetroMediaLab.Emulation.Enums.EmulationMachineTab.Rom));
        Assert.Equal(model != ModelConstants.MegaDriveII, settings.Any(field => field.Id == "hardware.ext"));
        Assert.Equal(model == ModelConstants.MegaCdSecond, machine.MegaCdModel == ModelConstants.MegaCdII);
        var device = StorageSettingsFunctions.Describe(machine).AvailableDevices.Single(device => device.Slot == EmulationMediaSlot.Cartridge0);
        Assert.Equal(has32X, device.AcceptedExtensions.Contains(".32x"));

        SegaBoard? board = null;
        try
        {
            board = BoardFactory.Create(model, Resources());
            Assert.Equal(hasCd ? SystemHardware.MegaCd : SystemHardware.MegaDrive, board.Hardware);
        }
        finally { board?.Dispose(); }
    }

    [Theory]
    [InlineData("pal", "bios_E.sms", "configuration.firmware.segaemu.4")]
    [InlineData("ntsc-u", "bios_U.sms", "configuration.firmware.segaemu.5")]
    [InlineData("ntsc-j", "bios_J.sms", "configuration.firmware.segaemu.6")]
    [InlineData("auto", "bios.sms", "configuration.firmware.segaemu.masterSystem")]
    public void MasterSystemShowsOneBiosInRomForTheRegionSelectedInCpu(string region, string biosFile, string firmwareId)
    {
        var configuration = new MachineConfiguration(ModelConstants.MasterSystem, CoreConstants.Id,
            new Dictionary<string, string> { ["segaemu_region_detect"] = region });
        var settings = SettingsDescriptionFunctions.Create(configuration);
        var bios = Assert.Single(settings, block => block.Tab == RetroMediaLab.Emulation.Enums.EmulationMachineTab.Rom);
        Assert.True(bios.IsCompact);
        Assert.Equal(firmwareId, Assert.Single(bios.Fields).Id);
        var fields = settings.SelectMany(block => block.Fields).ToArray();
        var option = Assert.Single(fields, field => field.Id == "segaemu_region_detect");
        Assert.Equal(RetroMediaLab.Emulation.Enums.EmulationMachineTab.Cpu, option.Tab);
        Assert.True(option.RefreshSettingsOnChange);
        Assert.Equal(new[] { "USA", "EUR", "JAP" }, option.Choices!.Skip(1).Select(choice => choice.InvariantDisplayValue));
        Assert.DoesNotContain(fields, field => field.Id == "segaemu_bios");
        foreach (var name in new[] { "bios_E.sms", "bios_U.sms", "bios_J.sms" })
        {
            var firmware = new Firmware("renamed.sms", 8192, "test", "test", DateTime.UnixEpoch,
                FirmwareType.SystemRom, true, true, null, null, [configuration.Model], [name]);
            var candidate = Assert.Single(FirmwareConfigurationFunctions.Candidates(configuration.Model,
                firmware, FirmwareFunctions.Slots(configuration)));
            Assert.Equal(region == "auto" || name == biosFile ? RetroMediaLab.Emulation.Enums.EmulationFirmwareCompatibility.Official
                : RetroMediaLab.Emulation.Enums.EmulationFirmwareCompatibility.Incompatible, candidate.Compatibility);
        }
    }

    [Fact]
    public void MegaDriveSettingsShowActualRamAndHideAnEmptyRomTab()
    {
        var module = new Engine();
        var configuration = new MachineConfiguration(ModelConstants.MegaDrive, CoreConstants.Id);
        var settings = module.Describe(configuration.Model, configuration);
        var ram = Assert.Single(settings.Blocks.SelectMany(block => block.Fields), field => field.Id == SettingsConstants.Ram);
        Assert.Equal("64 KiB", ram.Value);
        Assert.Equal(65536L, ram.NumericValue);
        Assert.False(settings.Visibility.Tabs[RetroMediaLab.Emulation.Enums.EmulationMachineTab.Rom]);
        var mounted = configuration with { Media = [new("game.md", MediaCategory.Cartridge, EmulationMediaSlot.Cartridge0)] };
        var mountedSettings = module.Describe(mounted.Model, mounted);
        Assert.False(mountedSettings.Visibility.Tabs[RetroMediaLab.Emulation.Enums.EmulationMachineTab.Rom]);
    }

    [Theory]
    [InlineData(ModelConstants.MegaCd)]
    [InlineData(ModelConstants.MegaCdSecond)]
    public void MegaCdDriveSettingsAndBackupCartridgeFollowTheMountedStack(string model)
    {
        var configuration = new MachineConfiguration(model, CoreConstants.Id);
        Assert.DoesNotContain(SettingsDescriptionFunctions.Create(configuration).SelectMany(block => block.Fields),
            field => StorageSettingsFunctions.IsMegaCdDriveField(field.Id));
        var storage = StorageSettingsFunctions.Describe(configuration);
        var drive = storage.AvailableDevices.Single(device => device.Slot == EmulationMediaSlot.Cd0);
        Assert.Equal(RetroMediaLab.Emulation.Enums.EmulationStorageConfigurationKind.CompactDiscDrive, drive.ConfigurationKind);
        Assert.Contains(drive.ConfigurationFields!, field => field.Id == "segaemu_cd_latency");
        Assert.DoesNotContain(drive.ConfigurationFields!, field => field.Id == "segaemu_cart_size");
        Assert.Equal(new[] { "disc", "internal" }, drive.ConfigurationBlocks!.Select(block => block.Id));
        Assert.Equal(new[] { "segaemu_cd_latency", "segaemu_cd_precache" }, drive.ConfigurationBlocks![0].Fields.Select(field => field.Id));
        Assert.Equal(0, ConfiguredCartridge(configuration));

        var options = new Dictionary<string, string> { [CartridgeStackFunctions.Option] = CartridgeStackFunctions.CdMemory };
        var withMemory = configuration with { Options = options };
        Assert.Equal(6, ConfiguredCartridge(withMemory));
        var equipped = StorageSettingsFunctions.Describe(withMemory);
        var equippedDrive = equipped.AvailableDevices.Single(device => device.Slot == EmulationMediaSlot.Cd0);
        var backupCartridge = Assert.Single(equippedDrive.ConfigurationBlocks!, block => block.Id == "backup-cartridge");
        Assert.Equal(new[] { "segaemu_cart_bram", "segaemu_cart_size" }, backupCartridge.Fields.Select(field => field.Id));
        var updated = StorageSettingsFunctions.Apply(withMemory, equipped with { DeviceSettings =
            [new(EmulationMediaSlot.Cd0, CompactDisc: new(Options: new Dictionary<string, string>
                { ["segaemu_cart_size"] = "256k", ["segaemu_cd_latency"] = "disabled" }))] });
        Assert.Equal("disabled", updated.Options!["segaemu_cd_latency"]);
        Assert.Equal(2, ConfiguredCartridge(updated));
        var empty = StorageSettingsFunctions.Apply(updated, equipped with { DeviceSettings =
            [new(EmulationMediaSlot.Cartridge0, Cartridge: new(Options: new Dictionary<string, string>
                { [CartridgeStackFunctions.Option + ".0"] = "none" }))] });
        Assert.Equal(0, ConfiguredCartridge(empty));

        static int ConfiguredCartridge(MachineConfiguration machine)
        {
            var core = new RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Services.ManagedCore();
            try
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                core.GetType().GetField("_configuration", flags)!.SetValue(core, machine);
                var configured = (SystemConfiguration)core.GetType().GetMethod("CreateSettings", flags)!.Invoke(core, null)!;
                return configured.CdCartridgeId;
            }
            finally { core.Dispose(); }
        }
    }

    [Theory]
    [InlineData(CartridgeStackFunctions.GameGenie, CartridgeStackFunctions.ActionReplay)]
    [InlineData(CartridgeStackFunctions.ActionReplay, CartridgeStackFunctions.GameGenie)]
    public void CartridgeStackKeepsCheatOrderAndRemovesUnavailableChoices(string first, string second)
    {
        var configuration = new MachineConfiguration(ModelConstants.MegaCdSecond, CoreConstants.Id);
        var options = new Dictionary<string, string>();
        var values = new Dictionary<string, string>
        {
            [CartridgeStackFunctions.Option + ".0"] = CartridgeStackFunctions.ThirtyTwoX,
            [CartridgeStackFunctions.Option + ".1"] = first,
            [CartridgeStackFunctions.Option + ".2"] = second,
            [CartridgeStackFunctions.Option + ".3"] = CartridgeStackFunctions.CdMemory
        };
        Assert.True(CartridgeStackFunctions.Apply(configuration, values, options));
        var configured = configuration with { Options = options };
        Assert.Equal(new[] { CartridgeStackFunctions.ThirtyTwoX, first, second, CartridgeStackFunctions.CdMemory }, CartridgeStackFunctions.Read(configured));
        Assert.True(configured.ThirtyTwoXEnabled);
        var editors = CartridgeStackFunctions.Describe(configured, values);
        Assert.DoesNotContain(editors[2].Choices!, choice => choice.Id == first || choice.Id == CartridgeStackFunctions.ThirtyTwoX);
        Assert.Contains(FirmwareFunctions.Slots(configured), slot => slot.FileName == "ggenie.bin");
        Assert.Contains(FirmwareFunctions.Slots(configured), slot => slot.FileName == "areplay.bin");
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void StackedCheatFirmwareReleasesTheBusToTheNextAccessory(bool genieFirst, bool enable32X)
    {
        byte[] genie = Program(SystemHardware.MegaDrive);
        byte[] replay = new byte[0x10000];
        genie.CopyTo(replay, 0);
        replay[1] = 0x60;
        "ACTION REPLAY II"u8.CopyTo(replay.AsSpan(0x3c6));
        genie[0x300] = 0x11;
        replay[0x300] = 0x22;
        var resources = Resources() with
        {
            LoadFirmware = (id, destination) =>
            {
                byte[]? firmware = id == 2 ? genie : id == 3 ? replay : null;
                if (firmware is null) return 0;
                firmware.AsSpan().CopyTo(destination.Span);
                return firmware.Length;
            }
        };
        resources.Configuration.Enable32X = enable32X;
        resources.Configuration.Cartridge.CheatAdapters = genieFirst ? [(byte)1, (byte)2] : [(byte)2, (byte)1];
        SegaBoard? board = null;
        try
        {
            board = BoardFactory.Create(ModelConstants.MegaDrive, resources);
            byte[] game = Program(SystemHardware.MegaDrive);
            game[0x300] = 0x33;
            Assert.True(board.Load("stack.md", game));
            Assert.Equal(enable32X, board.Has32X);
            var buses = (RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Cartridge.CartridgeAddressSpace)
                typeof(SegaBoard).GetField("_addresses", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(board)!;
            Assert.Equal(genieFirst ? 0x11 : 0x22, buses.Main[0].Data.Span[0x301]);
            if (genieFirst)
            {
                buses.Main[0x10].write16!(0x100078, 0xffff);
                Assert.Equal(0x11, buses.Main[0].Data.Span[0x301]);
                buses.Main[0].write16!(0, 0x400);
            }
            else buses.Main[0x10].write16!(0x100078, 0xffff);
            Assert.Equal(genieFirst ? 0x22 : 0x11, buses.Main[0].Data.Span[0x301]);
            if (genieFirst) buses.Main[0x10].write16!(0x100078, 0xffff);
            else buses.Main[0].write16!(0, 0x400);
            Assert.Equal(0x33, buses.Main[0].Data.Span[0x301]);
        }
        finally { board?.Dispose(); }
    }

    [Theory]
    [InlineData(1, 0x80)]
    [InlineData(2, 0xc0)]
    [InlineData(3, 0x00)]
    public void MegaDriveCartReadsConsoleRegionEvenWithAnImportedHeader(byte region, uint flags)
    {
        var resources = Resources();
        resources.Configuration.region_detect = region;
        var rom = Program(SystemHardware.MegaDrive);
        rom[0x1f0] = (byte)'J';
        SegaBoard? board = null;
        try
        {
            board = BoardFactory.Create(ModelConstants.MegaDrive, resources);
            Assert.True(board.Load("import.md", rom));
            Assert.Equal(flags, board.Input.Read68K(0) & 0xc0u);
            Assert.True(board.RunFrame(new short[4096]) > 0);
        }
        finally { board?.Dispose(); }
    }

    [Theory]
    [InlineData(ModelConstants.MegaDriveModelI, "VA4", false, true)]
    [InlineData(ModelConstants.MegaDriveModelI, "VA5", false, true)]
    [InlineData(ModelConstants.MegaDriveModelI, ModelConstants.MegaDriveVa6, true, true)]
    [InlineData(ModelConstants.MegaDriveModelI, ModelConstants.MegaDriveVa65, true, false)]
    [InlineData(ModelConstants.MegaDriveModelII, "VA0", true, false)]
    public void MegaDriveRevisionSelectsTmssAndTheExtensionPort(string model, string revision, bool tmss, bool ext)
    {
        var configuration = new MachineConfiguration(ModelConstants.MegaDrive, CoreConstants.Id,
            new Dictionary<string, string>
            {
                [SettingsConstants.MegaDriveModel] = model,
                [SettingsConstants.MegaDriveRevision] = revision
            });
        Assert.Equal(tmss, configuration.HasMegaDriveTmss);
        Assert.Equal(ext, configuration.HasMegaDriveExtensionPort);

        var resources = Resources();
        resources.Configuration.bios = tmss ? (byte)1 : (byte)0;
        SegaBoard? board = null;
        try
        {
            board = BoardFactory.Create(ModelConstants.MegaDrive, resources);
            Assert.True(board.Load("test.md", Program(SystemHardware.MegaDrive)));
            Assert.Equal(tmss ? 1u : 0u, board.Input.Read68K(0) & 1u);
        }
        finally { board?.Dispose(); }
    }

    [Theory]
    [InlineData(ModelConstants.MegaCd, 1, 59, 61, false)]
    [InlineData(ModelConstants.MegaCdThirtyTwoX, 2, 49, 51, true)]
    public void MegaCdFirmwareBootKeepsItsRegionAndSelectedExtensions(string model, byte region, int minFps, int maxFps, bool has32X)
    {
        byte[] bios = Program(SystemHardware.MegaDrive);
        var resources = Resources() with
        {
            LoadFirmware = (id, destination) =>
            {
                if ((byte)id != SystemHardware.MegaCd) return 0;
                bios.AsSpan().CopyTo(destination.Span);
                return bios.Length;
            }
        };
        resources.Configuration.region_detect = region;
        SegaBoard? board = null;
        try
        {
            board = BoardFactory.Create(model, resources);
            Assert.True(board.Load(string.Empty));
            Assert.Equal(SystemHardware.MegaCd, board.Hardware);
            Assert.Equal(has32X, board.Has32X);
            Assert.InRange(board.NominalFrameRate, minFps, maxFps);
        }
        finally { board?.Dispose(); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MegaDriveExtensionPortOnlyAcceptsConnectionsWhenPresent(bool present)
    {
        var resources = Resources();
        resources.Configuration.HasExtensionPort = present;
        SegaBoard? board = null;
        try
        {
            board = BoardFactory.Create(ModelConstants.MegaDrive, resources);
            Assert.True(board.Load("test.md", Program(SystemHardware.MegaDrive)));
            Assert.Equal(present, board.Input.HasExtensionPort);
            if (!present)
            {
                Assert.Throws<InvalidOperationException>(() => board.Input.ConnectExtensionPort(() => 0x2a, null));
                return;
            }

            int written = -1;
            board.Input.ConnectExtensionPort(() => 0x2a, (data, _) => written = data);
            board.Input.Write68K(6, 0);
            Assert.Equal(0x2au, board.Input.Read68K(3) & 0x7fu);
            board.Input.Write68K(3, 0x17);
            Assert.Equal(0x17, written);
        }
        finally { board?.Dispose(); }
    }

    [Theory]
    [InlineData(ModelConstants.MegaDrive, ModelConstants.MegaDriveModelI, ModelConstants.MasterSystemConverterI, true, true)]
    [InlineData(ModelConstants.MegaDrive, ModelConstants.MegaDriveModelI, ModelConstants.MasterSystemConverterII, true, false)]
    [InlineData(ModelConstants.MegaDrive, ModelConstants.MegaDriveModelII, ModelConstants.MasterSystemConverterI, false, false)]
    [InlineData(ModelConstants.MegaDrive, ModelConstants.MegaDriveModelII, ModelConstants.MasterSystemConverterII, true, false)]
    [InlineData(ModelConstants.MegaCd, ModelConstants.MegaDriveModelI, ModelConstants.MasterSystemConverterI, false, false)]
    [InlineData(ModelConstants.ThirtyTwoX, ModelConstants.MegaDriveModelI, ModelConstants.MasterSystemConverterI, false, false)]
    [InlineData(ModelConstants.MegaCdThirtyTwoX, ModelConstants.MegaDriveModelI, ModelConstants.MasterSystemConverterI, false, false)]
    public void MasterSystemConverterExposesOnlyActualMediaSlots(string machine, string model, string converter, bool acceptsSms, bool acceptsCard)
    {
        var configuration = new MachineConfiguration(machine, CoreConstants.Id, new Dictionary<string, string>
        {
            [SettingsConstants.MegaDriveModel] = model,
            [SettingsConstants.MasterSystemConverter] = converter
        });
        var devices = StorageSettingsFunctions.Describe(configuration).AvailableDevices;
        Assert.Equal(acceptsSms, devices.Single(device => device.Slot == EmulationMediaSlot.Cartridge0).AcceptedExtensions.Contains(".sms"));
        Assert.Equal(acceptsCard, devices.Any(device => device.Slot == EmulationMediaSlot.Cartridge1));
        if (acceptsCard)
        {
            var card = devices.Single(device => device.Slot == EmulationMediaSlot.Cartridge1);
            Assert.Equal([".mv"], card.AcceptedExtensions);
            Assert.True(card.IsPermanent);
            Assert.NotNull(card.ConfigurationOptionResourceKey);
        }
    }

    [Theory]
    [InlineData("game genie", "ggenie.bin")]
    [InlineData("action replay (pro)", "areplay.bin")]
    [InlineData("sonic & knuckles", "sk.bin")]
    public void LockOnFirmwareFollowsTheSelectedAccessory(string accessory, string firmware)
    {
        var configuration = new MachineConfiguration(ModelConstants.MegaDrive, CoreConstants.Id,
            new Dictionary<string, string> { ["segaemu_lock_on"] = accessory });
        Assert.Contains(FirmwareFunctions.Slots(configuration), slot => slot.FileName == firmware);
        Assert.DoesNotContain(FirmwareFunctions.Slots(configuration with { Options = null }),
            slot => slot.FileName == firmware);
    }

    [Fact]
    public void MegaCdHasItsOwnBoardWithoutOpeningAnImage()
    {
        SegaBoard? board = null;
        try { board = BoardFactory.Create(ModelConstants.MegaCd, Resources()); Assert.Equal("MegaCdMachine", board.GetType().Name); Assert.Equal(0x84, board.Hardware); }
        finally { board?.Dispose(); }
    }

    [Fact]
    public void InvalidSavedStateDoesNotMutateOrCloseTheRunningMachine()
    {
        SegaBoard? board = null;
        try
        {
            board = BoardFactory.Create(ModelConstants.Sg1000, Resources());
            Assert.True(board.Load("synthetic.sg",Program(1)));
            short[] audio = new short[4096]; board.RunFrame(audio);
            byte[] state = new byte[board.StateSize]; board.SaveState(state);
            Assert.Equal(0,board.LoadState(state.AsSpan(0,32)));
            byte[] after = new byte[board.StateSize]; board.SaveState(after);
            Assert.Equal(state,after);
            byte[] incompatible = state.ToArray(); incompatible[16] = 0x80;
            Assert.Equal(0,board.LoadState(incompatible));
            Assert.InRange(board.RunFrame(audio),700,900);
        }
        finally { board?.Dispose(); }
    }

    [Fact]
    public void MasterSystemJoypadReachesTheHardwarePort()
    {
        SegaBoard? board = null;
        try
        {
            board = BoardFactory.Create(ModelConstants.MasterSystem, Resources());
            Assert.True(board.Load("synthetic.sms", Program(0x20)));
            board.ConfigureInputPort(0, 1, 2);
            ICpuZ80Bus bus = board;
            board.Input.DigitalInputs[0] = 0;
            byte released = bus.ReadPort(0xdc);
            board.Input.DigitalInputs[0] = 16;
            byte pressed = bus.ReadPort(0xdc);
            Assert.Equal(0x10, (released ^ pressed) & 0x10);
            Assert.Equal(0, pressed & 0x10);
        }
        finally { board?.Dispose(); }
    }

    [Theory]
    [InlineData("sg-1000", null, 0)]
    [InlineData("sg-1000 II", null, 0)]
    [InlineData("sg-1000 II", "enabled", 7168)]
    [InlineData("sg-1000 II + ram ext.", null, 7168)]
    public void Sg1000ModelAndRamReflectTheSelectedHardware(string model, string? extension, long extensionBytes)
    {
        var options = new Dictionary<string, string> { [MachineConfiguration.Sg1000ModelOption] = model };
        if (extension is not null) options[MachineConfiguration.Sg1000RamExtensionOption] = extension;
        var configuration = new MachineConfiguration(ModelConstants.Sg1000, CoreConstants.Id, options);
        var fields = SettingsDescriptionFunctions.Create(configuration).SelectMany(block => block.Fields).ToArray();
        var selector = Assert.Single(fields, field => field.Id == MachineConfiguration.Sg1000ModelOption);
        Assert.Equal(RetroMediaLab.Emulation.Enums.EmulationMachineTab.Cpu, selector.Tab);
        Assert.Equal(new[] { "sg-1000", "sg-1000 II" }, selector.Choices!.Select(choice => choice.Id));
        var memory = Assert.Single(fields, field => field.Id == SettingsConstants.Ram);
        Assert.Equal(1024L, memory.NumericValue);
        var expansion = fields.SingleOrDefault(field => field.Id == MachineConfiguration.Sg1000RamExtensionOption);
        Assert.Equal(model != "sg-1000", expansion is not null);
        Assert.Equal(extensionBytes, expansion?.Value == "enabled" ? expansion.NumericValue : 0);
        Assert.DoesNotContain(fields, field => field.Id is "segaemu_vdp_mode" or "segaemu_left_border" or "segaemu_render" or "segaemu_sound_output");
        var resources = Resources();
        resources.Configuration.system = configuration.HasSg1000RamExtension ? SystemHardware.Sg1000IIExtendedRam
            : configuration.IsSg1000II ? SystemHardware.Sg1000II : SystemHardware.Sg1000;
        SegaBoard? board = null;
        try
        {
            board = BoardFactory.Create(ModelConstants.Sg1000, resources);
            Assert.True(board.Load("synthetic.sg", Program(resources.Configuration.system)));
            ICpuZ80Bus bus = board;
            bus.WriteMemory(0xc000, 0x21);
            bus.WriteMemory(0xc400, 0x7e);
            Assert.Equal(extensionBytes == 0 ? (byte)0x7e : (byte)0x21, bus.ReadMemory(0xc000));
        }
        finally { board?.Dispose(); }
    }

    [Theory]
    [InlineData("Mouse:Right")]
    [InlineData("Mouse:Middle")]
    [InlineData("Mouse:XButton1")]
    [InlineData("Mouse:XButton2")]
    [InlineData("Keyboard:Space")]
    [InlineData("Controller:test:ButtonA")]
    [InlineData("")]
    public void TerebiStylusUsesItsSavedHostBinding(string binding)
    {
        var engine = new Engine();
        var configuration = new MachineConfiguration(ModelConstants.Sg1000, CoreConstants.Id);
        var settings = engine.DescribeInputSettings(configuration);
        var mouse = Assert.IsType<EmulationInputBindingSet>(settings.Mouse);
        var action = Assert.Single(mouse.Definitions).Id;
        Assert.Equal("Mouse:Left", mouse.Values[action]);
        var saved = Assert.IsType<MachineConfiguration>(engine.ApplyInputSettings(configuration, settings with
        {
            Mouse = mouse with { Values = new Dictionary<string, string> { [action] = binding } }
        }));
        string json = System.Text.Json.JsonSerializer.Serialize(saved);
        var reloaded = System.Text.Json.JsonSerializer.Deserialize<MachineConfiguration>(json)!;
        Assert.Equal(binding, engine.DescribeInputSettings(reloaded).Mouse!.Values[action]);
        var raw = EmulationInputSnapshot.Empty with
        {
            Keys = binding == "Keyboard:Space" ? new HashSet<EmulationKey> { EmulationKey.Space } : EmulationInputSnapshot.Empty.Keys,
            Pointer = new EmulationPointerState(4, -2, 0, false, binding == "Mouse:Right", binding == "Mouse:Middle",
                binding == "Mouse:XButton1", binding == "Mouse:XButton2"),
            Controllers = binding == "Controller:test:ButtonA"
                ? [EmulationControllerState.Empty with { DeviceId = "test", Buttons = 1u << 8 }] : []
        };
        var adapter = Assert.IsAssignableFrom<RetroMediaLab.Emulation.Sega.Common.Interfaces.IEmulatorInputMapper>(engine.SelectedAdapter(reloaded));
        var projected = InputSnapshotFunctions.Apply(raw, reloaded, adapter);
        Assert.Equal(binding.Length != 0, projected.Pointer.Left);
        Assert.Equal(4, projected.Pointer.DeltaX);
        Assert.Equal(-2, projected.Pointer.DeltaY);
        Assert.False(InputSnapshotFunctions.Apply(EmulationInputSnapshot.Empty with
            { Pointer = EmulationInputSnapshot.Empty.Pointer with { Left = true } }, reloaded, adapter).Pointer.Left);
    }

    [Theory]
    [InlineData(ModelConstants.MasterSystem, 2)]
    [InlineData(ModelConstants.MasterSystemII, 1)]
    public void MasterSystemStorageHasOnlyItsFixedPorts(string model, int portCount)
    {
        var engine = new Engine();
        var configuration = Assert.IsType<MachineConfiguration>(engine.CreateConfiguration(model))
            with { EmulatorId = CoreConstants.Id };
        var storage = StorageSettingsFunctions.Describe(configuration);
        Assert.Equal(portCount, storage.AvailableDevices.Count);
        Assert.Equal(portCount, storage.ConfiguredSlots.Count);
        Assert.All(storage.AvailableDevices, device => Assert.True(device.IsPermanent));
        Assert.Contains(EmulationMediaSlot.Cartridge0, storage.ConfiguredSlots);
        Assert.Equal(portCount == 2, storage.ConfiguredSlots.Contains(EmulationMediaSlot.Cartridge1));
        var attemptedRemoval = StorageSettingsFunctions.Apply(configuration, storage with { ConfiguredSlots = [] });
        Assert.Equal(storage.ConfiguredSlots.ToArray(), StorageSettingsFunctions.Describe(attemptedRemoval).ConfiguredSlots.ToArray());
        Assert.DoesNotContain(engine.Describe(model, configuration).Blocks.SelectMany(block => block.Fields),
            field => field.Id is SettingsConstants.MasterSystemVariant or "segaemu_master_system_model");
    }

    [Theory]
    [InlineData(SettingsConstants.MasterSystemVariant, ModelConstants.MasterSystemSmsIi)]
    [InlineData("segaemu_master_system_model", "master system II")]
    public void LegacyMasterSystemIIConfigurationKeepsItsCartridgeAndSelectsTheNewModel(string key, string value)
    {
        var id = Guid.NewGuid();
        var cartridge = new MediaConfiguration("game.sms", MediaCategory.Cartridge, EmulationMediaSlot.Cartridge0);
        var card = new MediaConfiguration("game.mv", MediaCategory.Cartridge, EmulationMediaSlot.Cartridge1);
        var legacy = new MachineConfiguration(ModelConstants.MasterSystem, CoreConstants.Id,
            new Dictionary<string, string> { [key] = value }, Id: id, Media: [cartridge, card]);
        var migrated = ConfigurationStore.MigrateMasterSystem(legacy);
        Assert.Equal(id, migrated.Id);
        Assert.Equal(ModelConstants.MasterSystemII, migrated.Model);
        Assert.Equal(CoreConstants.Id, migrated.EmulatorId);
        Assert.Equal(cartridge, Assert.Single(migrated.Media!));
        Assert.False(migrated.Options!.ContainsKey(key));
    }

    internal static MachineResources Resources() => new(new(), new byte[1024 * 626 * 4], 1024, 626, 4096,
        44100, _ => null, (_, _) => 0, CancellationToken.None);

    internal static byte[] Program(byte hardware)
    {
        byte[] rom = new byte[0x8000];
        if (hardware < 0x80) new byte[] { 0x21,0x00,0xc0,0x34,0xc3,0x03,0x00 }.CopyTo(rom,0);
        else
        {
            BinaryPrimitives.WriteUInt32BigEndian(rom, 0x00ff1000);
            BinaryPrimitives.WriteUInt32BigEndian(rom.AsSpan(4), 0x200);
            (hardware == 0x82 ? "SEGA PICO       "u8 : "SEGA MEGA DRIVE "u8).CopyTo(rom.AsSpan(0x100));
            "U"u8.CopyTo(rom.AsSpan(0x1f0));
            new byte[] { 0x52,0x39,0x00,0xff,0x00,0x00,0x60,0xf8 }.CopyTo(rom,0x200);
        }
        return rom;
    }
}

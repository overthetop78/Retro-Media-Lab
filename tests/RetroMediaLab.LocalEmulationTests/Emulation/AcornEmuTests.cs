using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;
using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Services;
using RetroMediaLab.Chips.CPU.Common.Interfaces;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Interfaces;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Contracts;
using System.IO;
using BbcFirmware = RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Functions.BbcMicroFirmwareFunctions;
using NativeFirmware = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions.FirmwareFunctions;
using NativeModelCatalog = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Dictionaries.ModelCatalog;
using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Dictionaries;
using ModelAMachine = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services.BbcMicroMachine;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Common.Services;

namespace RetroMediaLab.Tests.Emulation;

/// <summary>Vérifications autonomes : aucun firmware, fichier, processus ou périphérique externe.</summary>
public sealed class AcornEmuTests
{
    [Fact]
    public void WarmRestartExecutesTheResetVectorAndPreservesRamWhileColdRestartClearsIt()
    {
        var mos = new byte[16384];
        mos[0x3ffc] = 0;
        mos[0x3ffd] = 0xc0;
        mos[0] = 0xea;
        var machine = new ModelAMachine(mos, new byte[16384]);
        machine.Cpu.Step();
        Assert.Equal(0xc001, machine.Cpu.PC);
        machine.Write(0x1234, 0x5a);
        machine.Reset(false);
        Assert.Equal(0xc000, machine.Cpu.PC);
        Assert.Equal(0x5a, machine.Read(0x1234));
        machine.Cpu.Step();
        machine.Reset(true);
        Assert.Equal(0xc000, machine.Cpu.PC);
        Assert.Equal(0, machine.Read(0x1234));
    }
    [Fact]
    public async Task RuntimeExposesOnlyAttachedReadersWhileStorageKeepsAvailableReaders()
    {
        var engine = new Engine();
        var configuration = new MachineConfiguration(MachineModel.BbcMicroModelA, core: Emulator.AcornEmu);
        var services = new EmulationRuntimeServices("sessions", "states", "converted", "host", (_, _) => throw new InvalidOperationException("Aucun cœur ne doit démarrer."));
        var storage = engine.DescribeStorageSettings(configuration);
        Assert.Empty(storage.ConfiguredSlots);
        Assert.NotEmpty(storage.AvailableDevices);
        var empty = await engine.CreateRuntimeAsync(configuration, services);
        Assert.Empty(empty.MediaDevices);
        foreach (var device in storage.AvailableDevices)
        {
            var attached = engine.ApplyStorageSettings(configuration, storage with { ConfiguredSlots = [device.Slot] });
            var runtime = await engine.CreateRuntimeAsync(attached, services);
            Assert.Equal(device.Slot, Assert.Single(runtime.MediaDevices).Slot);
            Assert.Equal(storage.AvailableDevices.Count, engine.DescribeStorageSettings(attached).AvailableDevices.Count);
            var detached = engine.ApplyStorageSettings(attached, engine.DescribeStorageSettings(attached) with { ConfiguredSlots = [], DeviceSettings = [] });
            Assert.Empty((await engine.CreateRuntimeAsync(detached, services)).MediaDevices);
        }
    }

    [Theory]
    [InlineData("0d9bcaf6a393c9ce2359ed700ddb53c232c2c45d", EmulationFirmwareCompatibility.Official, EmulationFirmwareCompatibility.Incompatible)]
    [InlineData("890f6e3e7fab3340f75b85e93ff29332bc9ecb2e", EmulationFirmwareCompatibility.Official)]
    [InlineData("4a7393f3a45ea309f744441c16723e2ef447a281", EmulationFirmwareCompatibility.Official)]
    [InlineData("4b0ece6dc139d5d3f4fabd023716fb6f25149b80", EmulationFirmwareCompatibility.Official, EmulationFirmwareCompatibility.Incompatible)]
    [InlineData("dca33995c0d008a527efe923d03333394b01022c", EmulationFirmwareCompatibility.Official, EmulationFirmwareCompatibility.Incompatible)]
    [InlineData("d35f6723132aabe3c4d00fc16fd9ecc6768df753", EmulationFirmwareCompatibility.Official, EmulationFirmwareCompatibility.Incompatible)]
    [InlineData("2b6aecd33a43f296c20832524e47cc7e3a9c3b17", EmulationFirmwareCompatibility.Unknown, EmulationFirmwareCompatibility.Incompatible)]
    [InlineData("b369809275cb67dfd8a749265e91adb2d2558ae6", EmulationFirmwareCompatibility.Incompatible)]
    public void FirmwareCompatibilityIsSharedByStandardModelsWithoutExtendingOtherVariants(string sha1, EmulationFirmwareCompatibility compatibility, EmulationFirmwareCompatibility masterCompatibility = EmulationFirmwareCompatibility.Unknown)
    {
        Assert.Equal(compatibility, AcornBBCMicroModel.Get(MachineModel.BbcMicroModelA, sha1));
        Assert.Equal(compatibility, AcornBBCMicroModel.Get(MachineModel.BbcMicroModelB, sha1));
        Assert.Equal(masterCompatibility, AcornBBCMicroModel.Get(MachineModel.BbcMaster128, sha1));
        Assert.Equal(EmulationFirmwareCompatibility.Unknown, AcornBBCMicroModel.Get(MachineModel.BbcMicroModelA, new string('0', 40)));
    }

    [Fact]
    public void AcornEmuSelectsRegisteredModelsWithoutSubstitutingModelA()
    {
        var factory = new RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Factories.AcornEmuMachineFactory();
        Assert.True(factory.Definition.MachineIds.SetEquals(NativeModelCatalog.All.Select(model => model.Model.ToString())));
        var model = NativeModelCatalog.Get(MachineModel.BbcMicroModelA);
        Assert.Equal(640, model.Width); Assert.Equal(256, model.Height);
        Assert.InRange(model.FramesPerSecond, 50.08, 50.09);
        Assert.Throws<NotSupportedException>(() => NativeModelCatalog.Get((MachineModel)int.MaxValue));
        Assert.All(factory.GetFirmwareSlots(new MachineConfiguration(MachineModel.BbcMicroModelA)), slot => Assert.Contains(nameof(MachineModel.BbcMicroModelA), slot.MachineIds!));
    }

    [Theory]
    [InlineData(8192, 1)]
    [InlineData(16384, 1)]
    [InlineData(32768, 1)]
    public void UnknownRomRemainsVisibleAndSelectable(int size, int candidates)
    {
        var image = new byte[size];
        var entries = BbcFirmware.Inspect(image, "unknown.rom", MachineModel.BbcMicroModelA, NativeFirmware.Field);
        Assert.Equal(candidates, entries.Count);
        Assert.All(entries, entry =>
        {
            Assert.Equal(EmulationFirmwareCompatibility.Unknown, entry.Compatibility);
            Assert.Null(entry.DestinationFieldId);
            foreach (var field in new[] { "acornemu.mos", "acornemu.basic", "acornemu.dfs" })
            {
                var selectable = Assert.Single(RetroMediaLab.App.Controllers.Emulation.Firmware.EmulationFirmwareManagementController.CandidatesForField([entry], field));
                Assert.Equal(field, selectable.DestinationFieldId);
                Assert.Equal(EmulationFirmwareCompatibility.Unknown, selectable.Compatibility);
            }
        });
    }

    [Fact]
    public void UnknownLanguageHeaderDoesNotRestrictTheDestination()
    {
        var image = new byte[16384];
        image[6] = 0x60; image[7] = 14; image[14] = 0; image[15] = (byte)'('; image[16] = (byte)'C'; image[17] = (byte)')';
        var entry = Assert.Single(BbcFirmware.Inspect(image, "renamed.bin", MachineModel.BbcMicroModelA, NativeFirmware.Field));
        Assert.Null(entry.DestinationFieldId);
        Assert.Equal(EmulationFirmwareCompatibility.Unknown, entry.Compatibility);
    }

    [Fact]
    public void UnknownResetVectorDoesNotRestrictTheDestination()
    {
        var image = new byte[16384]; image[^4] = 0xcd; image[^3] = 0xd9;
        var entry = Assert.Single(BbcFirmware.Inspect(image, "renamed.bin", MachineModel.BbcMicroModelA, NativeFirmware.Field));
        Assert.Null(entry.DestinationFieldId);
    }

    [Fact]
    public void UnknownServiceHeaderRemainsSelectableAndKnownDfsRequiresTheMachineExtension()
    {
        var image = new byte[8192]; image[6] = 0x82; image[7] = 16;
        image[17] = (byte)'('; image[18] = (byte)'C'; image[19] = (byte)')';
        var candidate = Assert.Single(BbcFirmware.Inspect(image, "service.rom", MachineModel.BbcMicroModelA, NativeFirmware.Field));
        Assert.Null(candidate.DestinationFieldId); Assert.Equal(EmulationFirmwareCompatibility.Unknown, candidate.Compatibility);
        Assert.Equal(16384, BbcFirmware.GetRomBytes(image, FirmwareCategory.DiscFilingSystem).Length);
        Assert.Equal(EmulationFirmwareCompatibility.Compatible, AcornBBCMicroModel.Get(MachineModel.BbcMicroModelA, "dbbec4d2540a854c120be3194c7566a2b79d153b"));
        Assert.Throws<InvalidDataException>(() => BbcFirmware.GetRomBytes(new byte[32768], FirmwareCategory.DiscFilingSystem));
    }

    [Fact]
    public void CombinedAndEightKibRomsPreserveTheirDocumentedLayout()
    {
        var combined = new byte[32768]; combined[0] = 0x12; combined[16384] = 0x34;
        Assert.Equal(0x12, BbcFirmware.GetRomBytes(combined, FirmwareCategory.Basic)[0]);
        Assert.Equal(0x34, BbcFirmware.GetRomBytes(combined, FirmwareCategory.OperatingSystem)[0]);
        var small = new byte[8192]; small[8191] = 0x56;
        var padded = BbcFirmware.GetRomBytes(small, FirmwareCategory.Basic);
        Assert.Equal(0x56, padded[8191]); Assert.All(padded[8192..], value => Assert.Equal(255, value));
        Assert.Throws<InvalidDataException>(() => BbcFirmware.GetRomBytes(small, FirmwareCategory.OperatingSystem));
        Assert.Throws<InvalidDataException>(() => BbcFirmware.GetRomBytes(new byte[42], FirmwareCategory.Basic));
        Assert.Equal(EmulationFirmwareCompatibility.Unknown, Assert.Single(BbcFirmware.Inspect(new byte[42], "bad.rom", MachineModel.BbcMicroModelA, NativeFirmware.Field)).Compatibility);
    }

    [Fact]
    public void HostSpacePreservesHeldShift()
    {
        var keyboard = new Keyboard();
        keyboard.SetInput(new HashSet<EmulationKey> { EmulationKey.Space, EmulationKey.LeftShift }, new Dictionary<EmulationKey, char> { [EmulationKey.Space] = ' ' });
        Assert.Equal(128, keyboard.Read(0) & 128);
        Assert.Equal(128, keyboard.Read(0x62) & 128);
    }
    [Fact]
    public void HostCharactersSurviveProcessProtocolWithoutConsumingNextSnapshot()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true);
        var input = EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { EmulationKey.D3 }, HostCharacters = new Dictionary<EmulationKey, char> { [EmulationKey.D3] = '"' } };
        RetroMediaLab.Emulation.EmulationSdk.HostProtocol.WriteInput(writer, input);
        RetroMediaLab.Emulation.EmulationSdk.HostProtocol.WriteInput(writer, EmulationInputSnapshot.Empty);
        writer.Flush(); stream.Position = 0;
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true);
        var received = RetroMediaLab.Emulation.EmulationSdk.HostProtocol.ReadInput(reader, "test");
        Assert.Equal('"', received.HostCharacters![EmulationKey.D3]);
        Assert.Contains(EmulationKey.D3, received.Keys);
        Assert.Null(RetroMediaLab.Emulation.EmulationSdk.HostProtocol.ReadInput(reader, "test").HostCharacters);
        Assert.Equal(stream.Length, stream.Position);
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(513)]
    public void ProcessProtocolRejectsInvalidHostCharacterCounts(int count)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true);
        writer.Write(0); writer.Write(count); writer.Flush(); stream.Position = 0;
        using var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true);
        Assert.Throws<InvalidDataException>(() => RetroMediaLab.Emulation.EmulationSdk.HostProtocol.ReadInput(reader, "test"));
    }
    [Theory]
    [InlineData(EmulationKey.D3, '"', false, 0x31, true)]
    [InlineData(EmulationKey.D3, '3', true, 0x11, false)]
    [InlineData(EmulationKey.D2, '@', true, 0x47, false)]
    [InlineData(EmulationKey.Equals, '=', false, 0x17, true)]
    [InlineData(EmulationKey.Oem102, '<', false, 0x66, true)]
    public void HostSymbolsChooseBbcContactAndShift(EmulationKey source, char character, bool hostShift, int position, bool bbcShift)
    {
        var keyboard = new Keyboard();
        var keys = new HashSet<EmulationKey> { source };
        if (hostShift) keys.Add(EmulationKey.LeftShift);
        keyboard.SetInput(keys, new Dictionary<EmulationKey, char> { [source] = character });
        Assert.Equal(128, keyboard.Read((byte)position) & 128);
        Assert.Equal(bbcShift ? 128 : 0, keyboard.Read(0) & 128);
        keyboard.SetInput(new HashSet<EmulationKey>());
        Assert.Equal(0, keyboard.Read((byte)position) & 128);
        Assert.Equal(0, keyboard.Read(0) & 128);
    }

    [Theory]
    [InlineData(EmulationKey.Numpad3, 0x11, false)]
    [InlineData(EmulationKey.NumpadPlus, 0x57, true)]
    [InlineData(EmulationKey.NumpadMultiply, 0x48, true)]
    [InlineData(EmulationKey.NumpadDivide, 0x68, false)]
    [InlineData(EmulationKey.NumpadPeriod, 0x67, false)]
    [InlineData(EmulationKey.NumpadEnter, 0x49, false)]
    public void NumericKeypadUsesExistingBbcKeys(EmulationKey key, int position, bool shift)
    {
        var keyboard = new Keyboard();
        keyboard.SetInput(new HashSet<EmulationKey> { key });
        Assert.Equal(128, keyboard.Read((byte)position) & 128);
        Assert.Equal(shift ? 128 : 0, keyboard.Read(0) & 128);
    }

    [Fact]
    public void UnsupportedHostCharacterDoesNotTypeItsPhysicalDigit()
    {
        var keyboard = new Keyboard();
        keyboard.SetInput(new HashSet<EmulationKey> { EmulationKey.D2 }, new Dictionary<EmulationKey, char> { [EmulationKey.D2] = 'é' });
        Assert.False(keyboard.ColumnPressed(1));
    }

    [Fact]
    public void HostCharactersSurviveCommonAndAreCopiedByFrameStorage()
    {
        var characters = new Dictionary<EmulationKey, char> { [EmulationKey.D3] = '"' };
        var source = EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { EmulationKey.D3 }, HostCharacters = characters };
        var mapped = RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Functions.InputSnapshotFunctions.Apply(source, (InputConfiguration?)null);
        var store = new RetroMediaLab.Emulation.Services.EmulationInputFrameStore();
        store.Update(mapped);
        characters[EmulationKey.D3] = '3';
        store.Poll();
        Assert.Contains(EmulationKey.D3, store.Polled.Keys);
        Assert.Equal('"', store.Polled.HostCharacters![EmulationKey.D3]);
        store.Update(EmulationInputSnapshot.Empty); store.Poll();
        Assert.Null(store.Polled.HostCharacters);
    }

    [Fact]
    public void CommonDiscoversModelAAndPreservesExternalFirmwareOptions()
    {
        var engine = new Engine();
        var configuration = new MachineConfiguration(MachineModel.BbcMicroModelA);
        Assert.Equal(Emulator.AcornEmu, configuration.Core);
        var updated = Assert.IsType<MachineConfiguration>(engine.ApplySettings(configuration, new Dictionary<string, string?>
        {
            ["acornemu.mos"] = "external-mos.rom",
            ["acornemu.basic"] = "external-basic.rom"
        }));
        Assert.Equal("external-mos.rom", updated.Options["acornemu.mos"]);
        Assert.Equal("external-basic.rom", updated.Options["acornemu.basic"]);
    }
    [Fact]
    public void ModelAHasOneRamBankAndDoesNotWriteRom()
    {
        var mos = new byte[16384];
        var basic = new byte[16384];
        basic[0] = 0x55;
        var machine = new ModelAMachine(mos, basic);
        machine.Write(0x4001, 0x6a);
        machine.Write(0x8000, 0xff);
        Assert.Equal(0x6a, machine.Read(0x0001));
        Assert.Equal(0x55, machine.Read(0x8000));
    }

    [Theory]
    [InlineData(16384, true)]
    [InlineData(32768, false)]
    public void ModelARamExpansionChangesCpuAndVideoAddressing(int bytes, bool mirrored)
    {
        var machine = new ModelAMachine(new byte[16384], new byte[16384], bytes);
        machine.Write(0x0001, 0x12);
        machine.Write(0x4001, 0x34);
        Assert.Equal(mirrored ? 0x34 : 0x12, machine.Read(0x0001));
        Assert.Equal(0x34, machine.Read(0x4001));
        ConfigureModeFour(machine);
        machine.Write(0x1800, 0); machine.Write(0x5800, 0x80);
        AdvanceCompletedBitmap(machine);
        var image = machine.Video.Render();
        Assert.Equal(255, image[2]);
        Assert.Equal(mirrored ? 0x80 : 0, machine.Read(0x1800));
        machine.Reset(true);
        Assert.Equal(0, machine.Read(0x4001));
    }

    [Fact]
    public void ModelAMemorySettingIsAppliedValidatedAndSummarized()
    {
        const string fieldId = "acorn_main_memory";
        var configuration = new MachineConfiguration(MachineModel.BbcMicroModelA, core: Emulator.AcornEmu);
        var engine = new Engine();
        var fields = RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Functions.SettingsDescriptionFunctions.Create(configuration).SelectMany(block => block.Fields);
        var field = Assert.Single(fields, field => field.Id == fieldId);
        Assert.Equal("16", field.Value);
        Assert.Equal(new[] { "16", "32" }, field.Choices!.Select(choice => choice.Id));
        Assert.True(field.RequiresRestart);
        var expanded = Assert.IsType<MachineConfiguration>(engine.ApplySettings(configuration, new Dictionary<string, string?> { [fieldId] = "32" }));
        var adapter = (RetroMediaLab.Emulation.Acorn.Common.Interfaces.IEmulatorSettings)new RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Factories.AcornEmuMachineFactory();
        Assert.Equal(32m, adapter.GetMemoryKib(expanded));
        Assert.Contains("RAM 32 KiB", adapter.Summarize(expanded).Details);
        Assert.Throws<ArgumentException>(() => engine.ApplySettings(configuration, new Dictionary<string, string?> { [fieldId] = "64" }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ModelAMachine(new byte[16384], new byte[16384], 65536));
    }
    [Theory]
    [InlineData(0x00)]
    [InlineData(0x01)]
    [InlineData(0x80)]
    [InlineData(0x55)]
    [InlineData(0xaa)]
    [InlineData(0xff)]
    public void ModeFourExpandsEachMemoryBitToTwoDisplayPixels(byte pattern)
    {
        var machine = new ModelAMachine(new byte[16384], new byte[16384]);
        ConfigureModeFour(machine);
        machine.Write(0x5800, pattern);
        AdvanceCompletedBitmap(machine);
        var image = machine.Video.Render();
        for (var pixel = 0; pixel < 16; pixel++)
        {
            var expected = ((pattern >> (7 - pixel / 2)) & 1) * 255;
            for (var channel = 0; channel < 3; channel++)
                Assert.Equal(expected, image[pixel * 4 + channel]);
        }
        // L'octet suivant vaut zéro : ne pas confondre deux pixels et deux cellules.
        for (var pixel = 16; pixel < 32; pixel++) Assert.Equal(0, image[pixel * 4 + 2]);
    }

    private static void ConfigureModeFour(ModelAMachine machine)
    {
        byte[] registers = [63, 40, 51, 0x24, 38, 0, 32, 34, 0, 7, 32, 0, 0x0b, 0];
        for (byte index = 0; index < registers.Length; index++)
        {
            machine.Write(0xfe00, index);
            machine.Write(0xfe01, registers[index]);
        }
        machine.Write(0xfe20, 0x88);
        // Le bit 7 du registre à décalage sélectionne la moitié de la palette.
        // Les trois autres prises restent actives : programmer les huit alias par couleur.
        for (var index = 0; index < 16; index++)
            machine.Write(0xfe21, (byte)((index << 4) | (index < 8 ? 7 : 0)));
    }

    private static void AdvanceCompletedBitmap(ModelAMachine machine)
    {
        const int cyclesPerField = 64 * 2 * 39 * 8;
        for (var cycle = 0; cycle < cyclesPerField * 4 && machine.Video.Frame < 3; cycle++)
            machine.Video.Tick();
        Assert.Equal(3L, machine.Video.Frame);
    }
}

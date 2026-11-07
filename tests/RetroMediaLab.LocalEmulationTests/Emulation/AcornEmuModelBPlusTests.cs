using System.Net.Http;
using System.Text.Json;
using B2Options = RetroMediaLab.Emulation.Acorn.Emulators.B2.Constants.OptionConstants;
using RetroMediaLab.Emulation;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Acorn.Common.Services;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Dictionaries;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Factories;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Functions;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.ModelBPlus.Services;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.ModelBPlus.Constants.ModelBPlusConstants;
using E = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.EmulatorConstants;
using S = RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Constants.SettingsConstants;
using F = RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Constants.BbcMicroBPlusFirmwareConstants;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuModelBPlusTests
{
    [Theory]
    [InlineData(C.Memory64, 64)]
    [InlineData(C.Memory128, 128)]
    public void OneModelHasBothCapacitiesAndAPermanentWd1770Drive(string capacity, int expected)
    {
        var engine = new Engine();
        var configuration = new MachineConfiguration(MachineModel.BbcMicroModelBPlus, core: Emulator.AcornEmu);
        configuration = Assert.IsType<MachineConfiguration>(engine.ApplySettings(configuration, new Dictionary<string, string?> { [S.MainMemory] = capacity }));
        var factory = new AcornEmuMachineFactory();
        Assert.Equal(MachineModel.BbcMicroModelBPlus, configuration.Model);
        Assert.Equal((decimal)expected, factory.GetMemoryKib(configuration));
        Assert.Equal(HardwareCpu.Rockwell6512, BbcMicroModelCatalog.All.Single(model => model.Model == configuration.Model).DefaultCpu);
        Assert.Single(engine.Machines, machine => machine.Id == configuration.MachineId);
        Assert.DoesNotContain(engine.Machines, machine => machine.Id == nameof(MachineModel.BbcMicroModelBPlus128));
        var drive = Assert.Single(factory.DescribeMediaDevices(configuration));
        Assert.Equal(EmulationMediaSlot.Floppy0, drive.Slot);
        Assert.True(drive.IsPermanent);
        Assert.True(StorageFunctions.UsesWd1770(configuration));
        var settings = factory.DescribeStorage(configuration);
        configuration = factory.ApplyStorage(configuration, settings with { ConfiguredSlots = [] });
        Assert.Contains(EmulationMediaSlot.Floppy0, factory.DescribeStorage(configuration).ConfiguredSlots);
        var slots = factory.GetFirmwareSlots(configuration);
        Assert.DoesNotContain(slots, slot => slot.FieldId == E.BasicFirmware);
        Assert.Equal(5, slots.Count(slot => slot.FieldId.StartsWith(C.ChipPrefix, StringComparison.Ordinal)));
        Assert.Contains(slots, slot => slot.FieldId == E.SystemFirmware && slot.IsRequired);
        Assert.Contains(slots, slot => slot.FieldId == "acornemu.floppy.rom1770" && slot.IsRequired);
    }

    [Fact]
    public void MosInstructionsAccessShadowWhileMainRamCodeKeepsItsOwnScreenBytes()
    {
        byte[] code = [0xa9, 0x22, 0x8d, 0x00, 0x30, 0x4c, 0x00, 0x01];
        var board = Board(C.BaseBytes, code);
        board.Write(0x3000, 0x11);
        board.Write(0x0100, 0xa9); board.Write(0x0101, 0x33);
        board.Write(0x0102, 0x8d); board.Write(0x0103, 0x00); board.Write(0x0104, 0x30);
        board.Write(0xfe37, 0x80);
        board.Cpu.Step(); board.Cpu.Step();
        Assert.Equal(0x22, board.Read(0x3000));
        Assert.Equal(0x22, board.Video.ReadMemory!(0x3000));
        board.Cpu.Step(); board.Cpu.Step();
        Assert.Equal(0x11, board.Read(0x3000));
        board.Cpu.Step();
        Assert.Equal(0x33, board.Read(0x3000));
        Assert.Equal(0x22, board.Video.ReadMemory!(0x3000));
        board.Write(0xfe34, 0);
        Assert.Equal(0x33, board.Video.ReadMemory!(0x3000));
    }

    [Theory]
    [InlineData(0x9fff, false)]
    [InlineData(0xa000, true)]
    [InlineData(0xafff, true)]
    [InlineData(0xb000, false)]
    [InlineData(0xc000, true)]
    [InlineData(0xdfff, true)]
    [InlineData(0xe000, false)]
    public void ShadowAccessFollowsTheInstructionRegionAndThePrivateRamLatch(int instruction, bool expected)
    {
        var memory = new ModelBPlusMemory();
        memory.Select(0xfe33, 0x80);
        memory.Select(0xfe36, 0x80);
        Assert.Equal(expected, memory.UsesShadow(0x3000, (ushort)instruction));
        Assert.False(memory.UsesShadow(0x2fff, (ushort)instruction));
        Assert.False(memory.UsesShadow(0x8000, (ushort)instruction));
        memory.Select(0xfe30, 0);
        Assert.Equal(instruction is >= 0xc000 and < 0xe000, memory.UsesShadow(0x3000, (ushort)instruction));
    }

    [Fact]
    public void ExpandedBanksAndPrivateRamStayIndependentAndBasicHasBothAliases()
    {
        var board = Board(C.ExpandedBytes, [0x4c, 0x00, 0xc0]);
        for (var index = 0; index < C.RamBanks.Count; index++)
        {
            board.Write(0xfe30, (byte)C.RamBanks[index]);
            board.Write(0x8000, (byte)(index + 1));
            board.Write(0xbfff, (byte)(index + 8));
        }
        board.Write(0xfe33, 0x80);
        board.Write(0x8000, 0x77);
        board.Write(0xafff, 0x88);
        Assert.Equal(0x88, board.Read(0xafff));
        for (var index = 0; index < C.RamBanks.Count; index++)
        {
            board.Write(0xfe30, (byte)C.RamBanks[index]);
            Assert.Equal(index + 1, board.Read(0x8000));
            Assert.Equal(index + 8, board.Read(0xbfff));
        }
        board.Write(0xfe30, 0x80);
        Assert.Equal(0x77, board.Read(0x8000));
        board.Write(0xfe30, 14); Assert.Equal(0xa5, board.Read(0x8000));
        board.Write(0xfe30, 15); Assert.Equal(0xa5, board.Read(0x8000));
        var smaller = Board(C.BaseBytes, [0x4c, 0x00, 0xc0]);
        smaller.Write(0xfe30, 0); smaller.Write(0x8000, 0x77);
        Assert.Equal(0xff, smaller.Read(0x8000));
    }

    [Theory]
    [InlineData(C.SmallChipBytes)]
    [InlineData(C.BankBytes)]
    [InlineData(C.ChipBytes)]
    public void PhysicalRomChipsExposeTwoBanksWithTheCorrectMirrors(int length)
    {
        var image = Enumerable.Repeat((byte)0x12, length).ToArray();
        if (length == C.ChipBytes) Array.Fill(image, (byte)0x34, C.BankBytes, C.BankBytes);
        var split = ModelBPlusFunctions.SplitChip(image);
        Assert.All(split.First, value => Assert.Equal(0x12, value));
        Assert.All(split.Second, value => Assert.Equal(length == C.ChipBytes ? 0x34 : 0x12, value));
    }

    [Fact]
    public void FirmwareCompatibilityDistinguishesTheCombinedSystemRomFromOlderMos()
    {
        Assert.Equal(EmulationFirmwareCompatibility.Official, AcornBBCMicroModel.Get(MachineModel.BbcMicroModelBPlus, F.SystemRomSha1));
        Assert.Equal(EmulationFirmwareCompatibility.Incompatible, AcornBBCMicroModel.Get(MachineModel.BbcMicroModelBPlus, "0d9bcaf6a393c9ce2359ed700ddb53c232c2c45d"));
        Assert.NotEqual(EmulationFirmwareCompatibility.Incompatible, AcornBBCMicroModel.Get(MachineModel.BbcMicroModelBPlus, F.Dfs223Sha1));
    }

    [Theory]
    [InlineData(MachineModel.BbcMicroModelBPlus)]
    [InlineData(MachineModel.BbcMicroModelBPlus128)]
    public void BothB2CapacitiesRemainAvailableIncludingExistingConfigurations(MachineModel model)
    {
        var engine = new Engine();
        var configuration = new MachineConfiguration(model, core: Emulator.B2);
        foreach (var capacity in new[] { C.Memory64, C.Memory128 })
        {
            configuration = Assert.IsType<MachineConfiguration>(engine.ApplySettings(configuration, new Dictionary<string, string?> { [S.MainMemory] = capacity }));
            Assert.Equal(model, configuration.Model);
            Assert.Equal(capacity == C.Memory128 ? B2Options.ModelBPlus128 : B2Options.ModelBPlus, configuration.Options[B2Options.Model]);
        }
    }

    [Theory]
    [InlineData(F.SystemRomSha1, EmulationFirmwareCompatibility.Official, EmulationFirmwareCompatibility.Official)]
    [InlineData("b369809275cb67dfd8a749265e91adb2d2558ae6", EmulationFirmwareCompatibility.Incompatible, EmulationFirmwareCompatibility.Incompatible)]
    [InlineData(F.Dfs226Sha1, EmulationFirmwareCompatibility.Official, EmulationFirmwareCompatibility.Official)]
    [InlineData("8564d944fedd17caf2023454ba5a7c3947aedc1f", EmulationFirmwareCompatibility.Unknown, EmulationFirmwareCompatibility.Unknown)]
    public void PhysicalSocketsRespectAllDeclaredRolesAndRejectSpeech(string hash, EmulationFirmwareCompatibility classified, EmulationFirmwareCompatibility socket)
    {
        Assert.Equal(classified, AcornBBCMicroModel.Get(MachineModel.BbcMicroModelBPlus, hash));
        var candidate = new EmulationFirmwareCandidate(hash + ":role", string.Empty, string.Empty, null, classified);
        Assert.Equal(socket, AcornEmuMachineFactory.SocketCompatibility(MachineModel.BbcMicroModelBPlus, candidate));
    }

    [Fact]
    public void FirmwareCatalogIdentifiesImportedRomsWithoutBadImages()
    {
        var library = RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Dictionaries.AcornFirmware.BySha1;
        Assert.True(library.Count > 3000);
        Assert.DoesNotContain(library.Values, entry => entry.Name.Contains("[bad", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(FirmwareCategory.DiscFilingSystem, RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Dictionaries.AcornFirmware.BySha1["301fd05c475a629c4bec70510d4507256a5b00d8"].Category);
    }

    [Theory]
    [InlineData("6c8daba70374e5aa3a6402f24cdc5f8677d58a0f", EmulationFirmwareCompatibility.Official, EmulationFirmwareCompatibility.Official)]
    [InlineData("0d9bcaf6a393c9ce2359ed700ddb53c232c2c45d", EmulationFirmwareCompatibility.Official, EmulationFirmwareCompatibility.Incompatible)]
    [InlineData(F.SystemRomSha1, EmulationFirmwareCompatibility.Incompatible, EmulationFirmwareCompatibility.Official)]
    [InlineData("1ab2c50a4b26e11772564fabf4e3873fbab4c72d", EmulationFirmwareCompatibility.Incompatible, EmulationFirmwareCompatibility.Unknown)]
    [InlineData("8564d944fedd17caf2023454ba5a7c3947aedc1f", EmulationFirmwareCompatibility.Unknown, EmulationFirmwareCompatibility.Unknown)]
    [InlineData("be10872aeb88714bd56462a2e86929953dee1c01", EmulationFirmwareCompatibility.Incompatible, EmulationFirmwareCompatibility.Incompatible)]
    public void CategoryMembershipAndSpecificModelRulesRemainDistinct(string sha1, EmulationFirmwareCompatibility standard, EmulationFirmwareCompatibility plus)
    {
        Assert.Equal(standard, AcornBBCMicroModel.Get(MachineModel.BbcMicroModelA, sha1.ToUpperInvariant()));
        Assert.Equal(standard, AcornBBCMicroModel.Get(MachineModel.BbcMicroModelB, sha1));
        Assert.Equal(plus, AcornBBCMicroModel.Get(MachineModel.BbcMicroModelBPlus, sha1));
        Assert.Equal(plus, AcornBBCMicroModel.Get(MachineModel.BbcMicroModelBPlus128, sha1));
        Assert.Equal(EmulationFirmwareCompatibility.Unknown, AcornBBCMicroModel.Get(MachineModel.BbcMicroModelA, "not-a-known-rom"));
    }

    [Fact]
    public void RecognizedWrongRoleIsRejectedByTheSharedSelector()
    {
        var candidate = new EmulationFirmwareCandidate(F.SystemRomSha1, "system.rom", "MOS / BASIC II", "2.00", EmulationFirmwareCompatibility.Official, E.SystemFirmware);
        Assert.Empty(RetroMediaLab.App.Controllers.Emulation.Firmware.EmulationFirmwareManagementController.CandidatesForField([candidate], E.BasicFirmware));
    }

    [Fact]
    public void CombinedRomIsAllowedAsBasicOnAStandardModelEvenWhenItsMosIsIncompatible()
    {
        Assert.Equal(EmulationFirmwareCompatibility.Incompatible, AcornBBCMicroModel.Get(MachineModel.BbcMicroModelA, F.SystemRomSha1, FirmwareCategory.OperatingSystem));
        Assert.Equal(EmulationFirmwareCompatibility.Official, AcornBBCMicroModel.Get(MachineModel.BbcMicroModelA, F.SystemRomSha1, FirmwareCategory.Basic));
        Assert.Equal(E.BasicFirmware, FirmwareFunctions.Field(FirmwareCategory.Basic));
        Assert.Null(FirmwareFunctions.Field(FirmwareCategory.Language));
        Assert.Null(FirmwareFunctions.Field(FirmwareCategory.Application));
    }

    [Fact]
    public void DnfsBelongsToBothDiscAndNetworkListsAndOtherRomsCannotUseTheBasicField()
    {
        const string dnfs = "7e3c536baeae84d6498a14e8405319e01ee78232";
        Assert.Contains(FirmwareCategory.DiscFilingSystem, AcornBBCMicroModel.Roles(MachineModel.BbcMicroModelA, dnfs));
        Assert.Contains(FirmwareCategory.Network, AcornBBCMicroModel.Roles(MachineModel.BbcMicroModelA, dnfs));
        Assert.Equal(EmulationFirmwareCompatibility.Incompatible, AcornBBCMicroModel.Get(MachineModel.BbcMicroModelA, dnfs, FirmwareCategory.Basic));
        Assert.Equal(EmulationFirmwareCompatibility.Unknown, AcornBBCMicroModel.Get(MachineModel.BbcMicroModelA, "88c93df1854f5fbe6cd6e5f0e29a8bf4ea3b5614", FirmwareCategory.Language));
        Assert.Equal(EmulationFirmwareCompatibility.Incompatible, AcornBBCMicroModel.Get(MachineModel.BbcMicroModelA, "88c93df1854f5fbe6cd6e5f0e29a8bf4ea3b5614", FirmwareCategory.Basic));
    }

    [Theory]
    [InlineData(MachineModel.BbcMicroModelA)]
    [InlineData(MachineModel.BbcMicroModelB)]
    [InlineData(MachineModel.BbcMicroModelBPlus)]
    [InlineData(MachineModel.BbcMicroModelBPlus128)]
    [InlineData(MachineModel.BbcMaster128)]
    public void BreakCanBeReassignedForEveryBbcModel(MachineModel model)
    {
        var configuration = new MachineConfiguration(model);
        var definition = Assert.Single(RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Functions.BbcMicroInputSettingsFunctions.KeyboardDefinitions(configuration), item => item.Id == nameof(EmulationKey.Break));
        Assert.Equal(nameof(EmulationKey.Break), definition.DefaultBinding);
        using var http = new HttpClient();
        var manager = new RetroMediaLab.Emulation.Acorn.Modules.AcornEmulationModule("unused", "unused", http, "unused");
        var settings = manager.DescribeInputSettings(configuration);
        var values = new Dictionary<string, string>(settings.Keyboard!.Values);
        values[nameof(EmulationKey.Break)] = nameof(EmulationKey.F6);
        var changed = manager.ApplyInputSettings(configuration, settings with { Keyboard = settings.Keyboard with { Values = values } });
        var restored = JsonSerializer.Deserialize<MachineConfiguration>(JsonSerializer.Serialize(changed, changed.GetType()))!;
        Assert.Equal(nameof(EmulationKey.F6), manager.DescribeInputSettings(restored).Keyboard!.Values[nameof(EmulationKey.Break)]);
        var input = restored.Input;
        var snapshot = EmulationInputSnapshot.Empty with { Keys = new HashSet<EmulationKey> { EmulationKey.LeftShift, EmulationKey.F6 } };
        var mapped = RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Functions.InputSnapshotFunctions.Apply(snapshot, input);
        Assert.Contains(EmulationKey.Break, mapped.Keys);
        Assert.Contains(EmulationKey.LeftShift, mapped.Keys);
    }

    private static BbcMicroMachine Board(int capacity, byte[] code)
    {
        var mos = new byte[C.BankBytes];
        code.CopyTo(mos, 0);
        mos[^4] = 0; mos[^3] = 0xc0;
        return new(mos, Enumerable.Repeat((byte)0xa5, C.BankBytes).ToArray(), C.MainBytes, expandedIo: true, modelBPlusMemory: capacity);
    }
}

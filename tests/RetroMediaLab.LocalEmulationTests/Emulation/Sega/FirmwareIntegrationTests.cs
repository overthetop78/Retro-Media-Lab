using System.Reflection;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Sega.Common.Interfaces;
using RetroMediaLab.Emulation.Sega.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Sega.Common.Machines.Common.Dictionaries;
using RetroMediaLab.Emulation.Sega.Common.Machines.Common.Functions;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Dictionaries;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Functions;
using GX = RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS;
using SE = RetroMediaLab.Emulation.Sega.Emulators.SegaEmu;

namespace RetroMediaLab.Tests.Emulation.Sega;

public sealed class FirmwareIntegrationTests
{
    [Theory]
    [InlineData("segaemu", "MasterSystem", "segaemu_bios", "enabled", "disabled")]
    [InlineData("genesisplusgxcs", "MasterSystem", "genesis_plus_gx_bios", "enabled", "disabled")]
    [InlineData("genesisplusgx", "MasterSystem", "genesis_plus_gx_bios", "enabled", "disabled")]
    [InlineData("genesis_plus_gx_wide", "MasterSystem", "genesis_plus_gx_wide_bios", "enabled", "disabled")]
    [InlineData("gearsystem", "MasterSystem", "gearsystem_bios_sms", "Enabled", "Disabled")]
    [InlineData("gearsystem", "GameGear", "gearsystem_bios_gg", "Enabled", "Disabled")]
    [InlineData("smsplus", "MasterSystem", "smsplus_sms_bios", "auto", "disabled")]
    public void BootRomIsEnabledByDefaultAndAnExplicitDisableIsPreserved(string id, string model, string key, string enabled, string disabled)
    {
        Assert.Equal(enabled, CoreSettingsFunctions.Configure(new(model, id)).Options![key]);
        Assert.Equal(disabled, CoreSettingsFunctions.Configure(new(model, id,
            Options: new Dictionary<string, string> { [key] = disabled })).Options![key]);
    }

    [Fact]
    public void MultivisionIsNotAdvertisedAsAnSg1000Bios()
    {
        Assert.True(FirmwareCatalog.TryIdentify(string.Empty, "6d7c64dd60dee4a33061d3d3a7c2ed190d895cdb", out var identity));
        Assert.Empty(identity!.Models);
        Assert.Empty(identity.FileNames);
        Assert.All(CoreCatalog.All, core => Assert.Empty(FirmwareFunctions.Slots(new("Sg1000", core.Id))));
        Assert.True(((IEmulatorCapabilities)new SE.Factories.MachineFactory()).SupportsNoContent);
        Assert.True(((IEmulatorCapabilities)new GX.Factories.MachineFactory()).SupportsNoContent);
    }

    [Fact]
    public void RenamedKnownBiosOnlyTargetsItsRealRegionAndMachine()
    {
        Firmware bios = Identified("e66fa1dc5820d254611fdcdba0662372", "bios_E.sms");
        foreach (var core in CoreCatalog.All)
        {
            var sms = FirmwareConfigurationFunctions.Candidates("MasterSystem", bios, FirmwareFunctions.Slots(new("MasterSystem", core.Id)));
            Assert.All(sms, value => Assert.Equal(EmulationFirmwareCompatibility.Incompatible, value.Compatibility));
            foreach (string model in new[] { "MegaDrive", "MegaCd" })
            {
                var slots = FirmwareFunctions.Slots(new(model, core.Id));
                var candidates = FirmwareConfigurationFunctions.Candidates(model, bios, slots);
                var accepted = candidates.Where(value => value.Compatibility == EmulationFirmwareCompatibility.Official).ToArray();
                Assert.Equal(slots.Count(slot => slot.FileName == "bios_CD_E.bin"), accepted.Length);
                Assert.All(accepted, value => Assert.Equal("bios_CD_E.bin", slots.Single(slot => slot.FieldId == value.DestinationFieldId).FileName));
            }
        }
    }

    [Fact]
    public void UnknownFileNameNeverGrantsCompatibility()
    {
        Firmware unknown = new("bios_E.sms", 8192, "unknown", "unknown", DateTime.UnixEpoch,
            RetroMediaLab.Emulation.Sega.Common.Machines.Common.Enums.FirmwareType.SystemRom, false, false, null, null, []);
        var candidates = FirmwareConfigurationFunctions.Candidates("MasterSystem", unknown,
            FirmwareFunctions.Slots(new("MasterSystem", "segaemu")));
        var candidate = Assert.Single(candidates);
        Assert.Equal(EmulationFirmwareCompatibility.Unknown, candidate.Compatibility);
        Assert.Null(candidate.DestinationFieldId);
    }

    [Fact]
    public void BuiltInSmsGamesHaveTheirOwnIdentityAndOnlyCompatibleBiosSlots()
    {
        Firmware sonic = Identified("4187d96beaf36385e681a3cf3bd1663d", "unrelated.bin");
        foreach (var core in CoreCatalog.All)
        {
            var slots = FirmwareFunctions.Slots(new("MasterSystem", core.Id));
            var accepted = FirmwareConfigurationFunctions.Candidates("MasterSystem", sonic, slots)
                .Where(value => value.Compatibility == EmulationFirmwareCompatibility.Official).ToArray();
            Assert.All(accepted, value => Assert.Contains(slots.Single(slot => slot.FieldId == value.DestinationFieldId).FileName,
                new[] { "bios.sms", "bios_E.sms" }));
            if (slots.Any(slot => slot.FileName == "bios_E.sms")) Assert.Single(accepted);
        }
        Assert.Contains("Sonic", sonic.Name);
    }

    [Theory]
    [InlineData("segaemu")]
    [InlineData("genesisplusgx")]
    [InlineData("genesisplusgxcs")]
    [InlineData("genesis_plus_gx_wide")]
    public void LockOnCartridgesAreExposedAndNeverAssignedToTheTmssBios(string id)
    {
        var slots = FirmwareFunctions.Slots(new("MegaDrive", id));
        Assert.Equal(4, slots.Count(slot => new[] { "sk.bin", "sk2chip.bin", "ggenie.bin", "areplay.bin" }.Contains(slot.FileName)));
        foreach (var (hash, role, official) in new[]
        {
            ("4ea493ea4e9f6c9ebfccbdb15110367e", "sk.bin", true),
            ("b4e76e416b887f4e7413ba76fa735f16", "sk2chip.bin", true),
            ("e8af7fe115a75c849f6aab3701e7799b", "ggenie.bin", false),
            ("a0028b3043f9d59ceeb03da5b073b30d", "areplay.bin", false)
        })
        {
            var candidate = Assert.Single(FirmwareConfigurationFunctions.Candidates("MegaDrive", Identified(hash, "renamed.md"), slots));
            Assert.Equal(role, slots.Single(slot => slot.FieldId == candidate.DestinationFieldId).FileName);
            Assert.Equal(official ? EmulationFirmwareCompatibility.Official : EmulationFirmwareCompatibility.Compatible, candidate.Compatibility);
        }
    }

    [Fact]
    public void DedicatedSaturnFieldsRetainTheirRegionalIdentities()
    {
        var slots = FirmwareFunctions.Slots(new("Saturn", "mednafen_saturn"));
        var candidate = Assert.Single(FirmwareConfigurationFunctions.Candidates("Saturn",
            Identified("85ec9ca47d8f6807718151cbcca8b964", "renamed.bin"), slots));
        Assert.Equal("sega_101.bin", slots.Single(slot => slot.FieldId == candidate.DestinationFieldId).FileName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EmptySmsCartridgePortExecutesTheBootRomWithoutOpeningAGame(bool segaEmu)
    {
        byte[] bios = new byte[8192];
        new byte[] { 0x21, 0x00, 0xc0, 0x34, 0xc3, 0x03, 0x00 }.CopyTo(bios, 0);
        IDisposable? board = null;
        try
        {
            var framebuffer = new byte[4096 * 626];
            int Load(int _, Memory<byte> destination) { bios.AsMemory().CopyTo(destination); return bios.Length; }
            if (segaEmu)
            {
                var settings = new SE.Common.SystemConfiguration { system = 0x21, bios = 1 };
                var system = SE.Factories.BoardFactory.Create("MasterSystem", new(settings, framebuffer, 1024, 626, 4096, 44100,
                    _ => throw new InvalidOperationException("No cartridge file may be requested."), Load, CancellationToken.None));
                board = system; Assert.True(system.Load(string.Empty)); system.RunFrame(new short[9600]);
            }
            else
            {
                var settings = new GX.SystemConfiguration { system = 0x21, bios = 1 };
                var system = new GX.SystemCore(0x21, settings, framebuffer, 1024, 626, 4096, 44100,
                    _ => throw new InvalidOperationException("No cartridge file may be requested."), Load, CancellationToken.None);
                board = system; Assert.True(system.Load(string.Empty)); system.RunFrame(new short[9600]);
            }
            var cart = Read(board, "cart");
            Assert.Equal(0u, Read(cart, "romsize"));
            var sms = Read(board, "_sms");
            Assert.Equal((ushort)0, Read(Read(sms, "cart_rom"), "pages"));
            Assert.Equal((ushort)8, Read(Read(sms, "bios_rom"), "pages"));
            Assert.NotEqual((byte)0, ((byte[])Read(board, "work_ram"))[0]);
        }
        finally { board?.Dispose(); }
    }

    private static Firmware Identified(string md5, string name)
    {
        Assert.True(FirmwareCatalog.TryIdentify(md5, string.Empty, out var identity));
        return new(name, 0, md5, md5, DateTime.UnixEpoch,
            RetroMediaLab.Emulation.Sega.Common.Machines.Common.Enums.FirmwareType.SystemRom,
            true, identity!.IsOfficial, identity.Name, identity.Version, identity.Models, identity.FileNames);
    }

    internal static object Read(object target, string name)
    {
        for (Type? type = target.GetType(); type is not null; type = type.BaseType)
            if (type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) is { } field)
                return field.GetValue(target)!;
        throw new MissingFieldException(target.GetType().FullName, name);
    }
}

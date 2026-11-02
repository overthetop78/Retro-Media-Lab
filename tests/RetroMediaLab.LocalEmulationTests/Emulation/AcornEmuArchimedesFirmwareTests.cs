using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Dictionaries;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Dictionaries;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Acorn.Common.Machines.Archimedes.Dictionaries;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuArchimedesFirmwareTests
{
    [Theory]
    [InlineData("6aebd686d97dfdf6726fa5f3246ef35b840b286d", "Arthur", "0.30")]
    [InlineData("1181ff9c2c2f3d6d414054ec33b2260404bafc81", "Arthur", "1.20")]
    [InlineData("b82a78830dac386f9b649b6d32a34f9c6910546d", "RISC OS", "2.00")]
    [InlineData("9ed7dee8553d96a6d58dd23d31682234f57beb62", "RISC OS", "2.01")]
    [InlineData("b4fa494d51dd704c9da9653c2acc6acd08cc8cf4", "RISC OS", "3.00")]
    [InlineData("68533e0a93657a879c1697c04f32d517345b1a61", "RISC OS", "3.10")]
    [InlineData("3487729e87bebc9cb51665838c27beff22f7b3bd", "RISC OS", "3.11")]
    public void ArmSystemImagesAreIdentifiedWithoutBeingAcceptedAsBbcFirmware(string hash, string name, string version)
    {
        var firmware = AcornFirmware.BySha1[hash.ToUpperInvariant()];
        Assert.Equal(name, firmware.Name);
        Assert.Equal(version, firmware.Version);
        Assert.Equal(FirmwareCategory.OperatingSystem, firmware.Category);
        Assert.False(firmware.Combined);
        foreach (var role in Enum.GetValues<FirmwareCategory>())
        {
            Assert.Equal(role == FirmwareCategory.OperatingSystem
                ? EmulationFirmwareCompatibility.Official : EmulationFirmwareCompatibility.Incompatible,
                AcornArchimedesModel.Get(hash.ToUpperInvariant(), role, supportsArthur: true));
            Assert.Equal(role == FirmwareCategory.OperatingSystem && name != "Arthur"
                ? EmulationFirmwareCompatibility.Official : EmulationFirmwareCompatibility.Incompatible,
                AcornArchimedesModel.Get(hash, role, supportsArthur: false));
        }
        Assert.Equal(new[] { FirmwareCategory.OperatingSystem },
            AcornFirmware.ByCategory.Where(category => category.Value.Contains(hash)).Select(category => category.Key));
        foreach (var model in new[] { MachineModel.BbcMicroModelA, MachineModel.BbcMicroModelB,
            MachineModel.BbcMicroModelBPlus, MachineModel.BbcMaster128, MachineModel.BbcMasterET, MachineModel.BbcMasterCompact })
        {
            Assert.Equal(EmulationFirmwareCompatibility.Incompatible,
                AcornBBCMicroModel.Get(model, hash, FirmwareCategory.OperatingSystem));
            Assert.Equal(EmulationFirmwareCompatibility.Incompatible,
                AcornBBCMicroModel.Get(model, hash, FirmwareCategory.Basic));
        }
    }

    [Fact]
    public void UnknownSystemImagesRemainUnknownAndRecognizedBbcRomsAreIncompatible()
    {
        foreach (var supportsArthur in new[] { false, true })
        {
            Assert.Equal(EmulationFirmwareCompatibility.Unknown,
                AcornArchimedesModel.Get("unknown-rom", FirmwareCategory.OperatingSystem, supportsArthur));
            Assert.Equal(EmulationFirmwareCompatibility.Incompatible,
                AcornArchimedesModel.Get("0d9bcaf6a393c9ce2359ed700ddb53c232c2c45d", FirmwareCategory.OperatingSystem, supportsArthur));
        }
    }
}

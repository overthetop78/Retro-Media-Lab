using RetroMediaLab.Emulation.Atari.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Atari.Common.Machines.Common.Contracts;
using AtariMachineConfiguration = RetroMediaLab.Emulation.Atari.Common.Machines.Common.Contracts.MachineConfiguration;
using AtariModelCatalog = RetroMediaLab.Emulation.Atari.Common.Machines.Common.Dictionaries.ModelCatalog;
using AmigaModelCatalog = RetroMediaLab.Emulation.Commodore.Common.Machines.Common.Dictionaries.ModelCatalog;
using RetroMediaLab.Emulation.Commodore.Common.Machines.Common.Dictionaries;
namespace RetroMediaLab.Tests.Emulation.MachineAdapters;
internal static class MachineCapabilitiesScenarios
{
    public static void Atari(string model, string family, string adapter)
    {
        var parsed = AtariModelCatalog.Parse(model);
        var configuration = new AtariMachineConfiguration(parsed);
        Assert.Equal(model, configuration.MachineId); Assert.Equal(family, configuration.Family.ToString()); Assert.Equal(adapter, configuration.Core.ToString());
        Assert.Contains(AtariModelCatalog.All, item => item.Id == model);
        var storage = RetroMediaLab.Emulation.Atari.Common.Machines.Common.Functions.StorageSettingsFunctions.Describe(configuration);
        Assert.Equal(storage.AvailableDevices.Count, storage.AvailableDevices.Select(d=>d.Slot).Distinct().Count());
        foreach(var device in storage.AvailableDevices)
        {
            var media = RetroMediaLab.Emulation.Atari.Common.Machines.Common.Functions.EmulationMediaConversionFunctions.ToModule(new("virtual-media",device.Slot,device.MediaType,false,true),[]);
            var accepted = new AtariMachineConfiguration(parsed,media:[media]);
            Assert.Equal(media,Assert.Single(accepted.Media));
            Assert.Throws<ArgumentException>(()=>new AtariMachineConfiguration(parsed,media:[media,media]));
            Assert.Throws<ArgumentException>(()=>new AtariMachineConfiguration(parsed,media:[media with {Path=" "}]));
            Assert.Throws<ArgumentException>(()=>new AtariMachineConfiguration(parsed,media:[media with {Slot=new(device.Slot.Category,99)}]));
        }
    }
    public static void Amiga(string model, string cpu, string chipset, int chipMemory)
    {
        var definition = Assert.IsType<RetroMediaLab.Emulation.Commodore.Common.Machines.AmigaComputers.Contracts.AmigaModel>(AmigaModelCatalog.Get(model));
        Assert.Equal(cpu, ((int)definition.DefaultCpu).ToString()); Assert.Equal(chipset, definition.Chipset); Assert.Equal(chipMemory, definition.ChipMemoryKib);
        Assert.Equal(2, definition.ControllerPortCount);
        Assert.Contains(MachineCatalog.All, item => item.Id == model);
        var configuration = new RetroMediaLab.Emulation.Commodore.Common.Machines.Common.Contracts.MachineConfiguration(model,
            RetroMediaLab.Emulation.Commodore.Common.Machines.Common.Enums.Emulator.PUAE,
            FirmwarePaths:new Dictionary<string,string>{{"configuration.kickstartPath",
            "virtual-rom"}},
            Options:new Dictionary<string,string>{{"retromedialab_floppy_drive_count",
            "99"},
            {"retromedialab_hard_drive_count",
            "99"}});
        var storage = RetroMediaLab.Emulation.Commodore.Common.Machines.Common.Functions.StorageSettingsFunctions.Describe(configuration);
        Assert.Equal(definition.MaximumFloppyDrives,storage.ConfiguredSlots.Count(s=>s.Category==RetroMediaLab.Emulation.Enums.EmulationMediaCategory.FloppyDrive));
        Assert.Equal(definition.MaximumHardDrives,storage.ConfiguredSlots.Count(s=>s.Category==RetroMediaLab.Emulation.Enums.EmulationMediaCategory.HardDisk));
        Assert.Equal(definition.HasCdDrive,storage.ConfiguredSlots.Contains(RetroMediaLab.Emulation.Contracts.EmulationMediaSlot.Cd0));
        var empty = RetroMediaLab.Emulation.Commodore.Common.Machines.Common.Functions.StorageSettingsFunctions.Describe(configuration with { Options=new Dictionary<string,string>{{"retromedialab_floppy_drive_count","-1"},{"retromedialab_hard_drive_count","-1"}} });
        Assert.All(empty.ConfiguredSlots,slot=>Assert.Equal(RetroMediaLab.Emulation.Contracts.EmulationMediaSlot.Cd0,slot));
    }
    public static void Invalid()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AtariModelCatalog.Parse("absent"));
        Assert.Throws<ArgumentOutOfRangeException>(() => AmigaModelCatalog.Get("absent"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AtariMachineConfiguration(MachineModel.St, schemaVersion: -1));
        var firmware = new FirmwareConfiguration(FirmwareCategory.Tos, "virtual", true);
        Assert.Throws<ArgumentException>(() => new AtariMachineConfiguration(MachineModel.St, firmwares: [firmware, firmware]));
        Assert.Throws<ArgumentException>(() => new AtariMachineConfiguration(MachineModel.Atari2600, firmwares: [firmware]));
        Assert.Throws<ArgumentException>(() => new AtariMachineConfiguration(MachineModel.St, firmwares: [firmware with { Path = " " }]));
    }
}

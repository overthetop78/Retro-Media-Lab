using RetroMediaLab.Emulation.Atari.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Atari.Common.Machines.Common.Functions;
using RetroMediaLab.Emulation.Atari.Common.Machines.Atari8Bit.Functions;
using RetroMediaLab.Emulation.Atari.Emulators.Atari800.Functions;
using RetroMediaLab.Emulation.Atari.Emulators.Common.Interop.Functions;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using AtariMachineConfiguration = RetroMediaLab.Emulation.Atari.Common.Machines.Common.Contracts.MachineConfiguration;
using AmigaMachineConfiguration = RetroMediaLab.Emulation.Commodore.Common.Machines.Common.Contracts.MachineConfiguration;
using AmigaMediaCategory = RetroMediaLab.Emulation.Commodore.Common.Machines.Common.Enums.MediaCategory;
namespace RetroMediaLab.Tests.Emulation.MachineAdapters;
internal static class MachineConfigurationMappingScenarios
{
    public static void AtariOption(string source,string value,string target,string? expected)
    {
        var configuration = new AtariMachineConfiguration(MachineModel.Ste,options:new Dictionary<string,string>{{source,value},{"custom","kept"}});
        var mapped = StLegacyOptionFunctions.Apply(configuration);
        if(expected is null) Assert.False(mapped.ContainsKey(target)); else Assert.Equal(expected,mapped[target]);
        Assert.Equal(value,configuration.Options[source]); Assert.Equal("kept",mapped["custom"]);
    }
    public static void AmigaOptions()
    {
        using var http = new System.Net.Http.HttpClient();
        var module = new RetroMediaLab.Emulation.Commodore.Modules.CommodoreEmulationModule("virtual-config","virtual-base",http,"virtual-core");
        var original = new AmigaMachineConfiguration("A500", RetroMediaLab.Emulation.Commodore.Common.Machines.Common.Enums.Emulator.PUAE, FirmwarePaths: new Dictionary<string,string> {{"configuration.kickstartPath","original-rom"}}, Options: new Dictionary<string,string>(), Id: Guid.NewGuid());
        var mapped = Assert.IsType<AmigaMachineConfiguration>(module.ApplySettings(original,new Dictionary<string,string?>
        {
            ["retromedialab_amiga_crop"]="automatic",["configuration.kickstartPath"]="selected-rom",
            ["configuration.extendedRomPath"]=" ",["configuration.romKeyPath"]="selected-key",
            ["configuration.audioEnabled"]="disabled",["configuration.audioLatency"]="75",
            ["configuration.audioOutput"]="virtual-audio",["configuration.audioStereoSeparation"]="50",
            ["configuration.cpuSpeed"]="200|4",["configuration.parallelJoystickAdapter"]="enabled",
            ["custom"]="kept",["retromedialab_amiga_floppy_multidrive"]=null
        }));
        Assert.Equal("automatic",mapped.Options!["retromedialab_amiga_crop"]); Assert.Equal("selected-rom",mapped.FirmwarePath("configuration.kickstartPath"));
        Assert.Empty(mapped.FirmwarePath("configuration.extendedRomPath")); Assert.Equal("selected-key",mapped.FirmwarePath("configuration.romKeyPath")); Assert.False(mapped.AudioEnabled);
        Assert.Equal(75,mapped.Audio!.LatencyMilliseconds); Assert.Equal(50,mapped.Audio.StereoSeparation); Assert.Equal("virtual-audio",mapped.Audio.OutputDeviceId);
        Assert.True(mapped.Input!.ParallelJoystickAdapterEnabled); Assert.Equal("kept",mapped.Options["custom"]);
        Assert.Equal("200",mapped.Options["retromedialab_amiga_cpu_throttle"]); Assert.Equal("4",mapped.Options["retromedialab_amiga_cpu_multiplier"]);
        Assert.False(mapped.Options.ContainsKey("retromedialab_amiga_floppy_multidrive")); Assert.False(mapped.Options.ContainsKey("configuration.cpuSpeed"));
        Assert.Equal("original-rom",original.FirmwarePath("configuration.kickstartPath")); Assert.False(original.Options!.ContainsKey("retromedialab_amiga_crop")); Assert.Equal(original.Id,mapped.Id);
        var storage = RetroMediaLab.Emulation.Commodore.Common.Machines.Common.Functions.StorageSettingsFunctions.Describe(mapped);
        var changed = RetroMediaLab.Emulation.Commodore.Common.Machines.Common.Functions.StorageSettingsFunctions.Apply(mapped,storage with
        {
            ConfiguredSlots=[EmulationMediaSlot.Floppy0], MountedMedia=[new("virtual.adf",EmulationMediaSlot.Floppy0,EmulationMediaType.Floppy,true,true)]
        });
        Assert.Equal("virtual.adf",changed.InitialDiskPath); Assert.True(Assert.Single(changed.Media!).IsReadOnly);
        Assert.Equal("automatic",changed.Options!["retromedialab_amiga_crop"]);

        var native = RetroMediaLab.Emulation.Commodore.Emulators.UAE.Common.Interop.Functions.UaeOptionFunctions.ToNative(mapped);
        Assert.Equal("automatic", native.Options!["puae_crop"]);
        Assert.Equal("kept", native.Options["custom"]);
        Assert.False(native.Options.ContainsKey("retromedialab_amiga_crop"));
    }
    public static void AtariModule(string model)
    {
        using var http = new System.Net.Http.HttpClient();
        var module = new RetroMediaLab.Emulation.Atari.Modules.AtariEmulationModule("virtual-config","virtual-base",http,"virtual-core");
        var original = new AtariMachineConfiguration(Enum.Parse<MachineModel>(model),options:new Dictionary<string,string>{{"removed","old"},{"preserved","kept"}});
        var mapped = Assert.IsType<AtariMachineConfiguration>(module.ApplySettings(original,new Dictionary<string,string?>{{"removed",null},{"specific","synthetic"}}));
        Assert.False(mapped.Options.ContainsKey("removed")); Assert.Equal("synthetic",mapped.Options["specific"]); Assert.Equal("kept",mapped.Options["preserved"]);
        Assert.Equal(original.Model,mapped.Model); Assert.Equal(original.Core,mapped.Core); Assert.Equal(original.Id,mapped.Id);
        Assert.Equal("old",original.Options["removed"]); Assert.False(original.Options.ContainsKey("specific"));
        var runtimeOptions=module.RuntimeOptions(mapped);
        Assert.Equal("synthetic",runtimeOptions["specific"]); Assert.Equal("kept",runtimeOptions["preserved"]);
        if(original.Family==MachineFamily.St)
        {
            Assert.Equal(model is "Ste" or "MegaSte" ? "ste" : model=="Tt" ? "tt" : model=="Falcon" ? "falcon" : "st",runtimeOptions["hatari_machinetype"]);
        }
        else Assert.False(runtimeOptions.ContainsKey("hatari_machinetype"));
    }
    public static void EightBitNormalization()
    {
        var original = new AtariMachineConfiguration(MachineModel.Atari400,options:new Dictionary<string,string>
        {{"retromedialab_atari_video_standard","bad"},{"retromedialab_atari_8bit_basic_enabled","bad"},{"retromedialab_atari_8bit_show_activity","bad"},{"retromedialab_atari_8bit_axlon_shadow","enabled"},{"custom","kept"}});
        var options = EightBitSettingsFunctions.Normalize(original);
        Assert.Equal("Ntsc",options["retromedialab_atari_video_standard"]);
        Assert.Equal("disabled",options["retromedialab_atari_8bit_basic_enabled"]); Assert.Equal("enabled",options["retromedialab_atari_8bit_show_activity"]);
        Assert.Equal("disabled",options["retromedialab_atari_8bit_axlon_shadow"]); Assert.Equal("kept",options["custom"]);
        Assert.Equal("bad",original.Options["retromedialab_atari_video_standard"]);
        var native = Atari800OptionFunctions.ToNative(options);
        Assert.Equal("disabled",native["atari800_internalbasic"]);
        Assert.Equal("enabled",native["atari800_show_diskled"]);
        Assert.False(native.ContainsKey("retromedialab_atari_8bit_basic_enabled"));
    }
    public static void Options()
    {
        var options = new Dictionary<string, string> { ["retromedialab_atari_main_memory"] = "4194304", ["retromedialab_atari_cpu_frequency"] = "16", ["custom"] = "kept" };
        var configuration = new AtariMachineConfiguration(MachineModel.Ste, options: options);
        var mapped = StLegacyOptionFunctions.Apply(configuration);
        Assert.Equal("4", mapped["hatari_ramsize"]); Assert.Equal("16", mapped["hatari_cpu_freq"]); Assert.Equal("ste", mapped["hatari_machinetype"]);
        Assert.Equal("kept", mapped["custom"]); Assert.Equal(3, options.Count); Assert.Equal(3, configuration.Options.Count);
        var amiga = new AmigaMachineConfiguration("A500",
            RetroMediaLab.Emulation.Commodore.Common.Machines.Common.Enums.Emulator.PUAE,
            FirmwarePaths: new Dictionary<string,string> {{"configuration.kickstartPath",
            "virtual-rom"}},
            Options: new Dictionary<string,string>(),
            Id: Guid.NewGuid(),
            InitialDiskPath: "virtual-floppy");
        Assert.Equal("A500", amiga.Model); Assert.Equal("virtual-rom", amiga.FirmwarePath("configuration.kickstartPath")); Assert.Equal("virtual-floppy", amiga.InitialDiskPath);
        Assert.NotEqual(Guid.Empty, amiga.EnsureId().Id); Assert.Equal(amiga.Id, amiga.EnsureId().Id);
    }
    public static void Media()
    {
        var mapped = RetroMediaLab.Emulation.Commodore.Common.Machines.Common.Functions.EmulationMediaConversionFunctions.ToCommon([ new("disk-a", AmigaMediaCategory.Floppy), new("hard-disk", AmigaMediaCategory.HardDrive), new("disk-b", AmigaMediaCategory.Floppy)]);
        Assert.Equal(new[] { EmulationMediaSlot.Floppy0, EmulationMediaSlot.HardDisk0, EmulationMediaSlot.Floppy1 }, mapped.Select(item => item.Slot));
        Assert.Equal(Path.GetFullPath("disk-b"), mapped[2].Path);
        var original = new EmulationMedia("virtual", EmulationMediaSlot.Floppy1, EmulationMediaType.Floppy, true, false);
        var atari = RetroMediaLab.Emulation.Atari.Common.Machines.Common.Functions.EmulationMediaConversionFunctions.ToModule(original, []);
        Assert.Equal(original, RetroMediaLab.Emulation.Atari.Common.Machines.Common.Functions.EmulationMediaConversionFunctions.ToCommon(atari));
    }
}

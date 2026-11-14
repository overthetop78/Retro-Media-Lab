using System.Globalization;
using System.IO;
using System.Reflection;
using RetroMediaLab.App.Functions.Emulation.Machine;
using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Sega.Common.Interfaces;
using RetroMediaLab.Emulation.Sega.Common.Machines.Common.Contracts;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Functions;
using GX = RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS;
using SE = RetroMediaLab.Emulation.Sega.Emulators.SegaEmu;

namespace RetroMediaLab.Tests.Emulation.Sega.SegaEmu;

public sealed class OptionIntegrationTests
{
    [Fact]
    public void OnlyChangedHotOptionsReachTheCore()
    {
        EmulationOption[] available = [Option("changed", "disabled"), Option("unchanged", "enabled"), Option("cold", "disabled", true)];
        var changes = EmulationRuntimeOptionFunctions.HotChanges(new Dictionary<string,string>
        {
            ["changed"]="enabled", ["unchanged"]="enabled", ["cold"]="enabled", ["unknown"]="enabled"
        }, available);
        Assert.Equal("changed", Assert.Single(changes).Key);
    }

    [Theory]
    [InlineData("segaemu")]
    [InlineData("genesisplusgxcs")]
    public void ReapplyingCurrentValuesPreservesTheRunningBoard(string id)
    {
        Fixture? fixture = null;
        try
        {
            fixture = Create(id, "MasterSystem");
            fixture.Core.RunFrame();
            object board = Read(fixture.Core, "_system")!;
            byte[] state = fixture.Core.SaveState();
            foreach (var option in fixture.Core.Options)
            {
                string value=fixture.Configuration.Options!.GetValueOrDefault(option.Key,option.DefaultValue);
                Assert.True(option.Values.Any(candidate=>candidate.Value==value),$"Invalid configured value: {id}: {option.Key}={value}");
                fixture.Core.SetOption(option.Key,value);
            }
            Assert.Same(board, Read(fixture.Core, "_system"));
            Assert.Equal(state, fixture.Core.SaveState());
        }
        finally { fixture?.Dispose(); }
    }

    [Theory]
    [InlineData("segaemu")]
    [InlineData("genesisplusgxcs")]
    public void PresentationChangesPreserveTheBoardAndRemainLive(string id)
    {
        Fixture? fixture = null;
        try
        {
            fixture = Create(id, "MasterSystem"); fixture.Core.RunFrame();
            object board = Read(fixture.Core,"_system")!;
            string prefix = id == "segaemu" ? "segaemu_" : "genesis_plus_gx_";
            foreach (string suffix in new[] {"aspect_ratio", "overscan", "left_border", "blargg_ntsc_filter", "render", "frameskip", "frameskip_threshold", "no_sprite_limit"})
            {
                EmulationOption option = fixture.Core.Options.Single(option=>option.Key==prefix+suffix);
                Assert.False(option.RequiresRestart);
                foreach (var value in option.Values)
                {
                    fixture.Core.SetOption(option.Key,value.Value); fixture.Core.RunFrame();
                    Assert.Same(board,Read(fixture.Core,"_system"));
                    Assert.InRange(fixture.Core.LatestVideoFrame!.Width,1,1024);
                    Assert.InRange(fixture.Core.LatestVideoFrame.Height,1,626);
                }
            }
        }
        finally {fixture?.Dispose();}
    }

    [Theory]
    [InlineData("segaemu")]
    [InlineData("genesisplusgxcs")]
    public void AspectRatioChoicesActuallyChangeThePublishedFrame(string id)
    {
        Fixture? fixture = null;
        try
        {
            fixture=Create(id,"GameGear");
            string key=(id=="segaemu" ? "segaemu_" : "genesis_plus_gx_")+"aspect_ratio";
            fixture.Core.SetOption(key,"Uncorrected");fixture.Core.RunFrame();
            VideoFrame frame=fixture.Core.LatestVideoFrame!;
            Assert.InRange(Math.Abs(frame.AspectRatio-(float)frame.Width/frame.Height),0,0.00001f);
            fixture.Core.SetOption(key,"4:3");fixture.Core.RunFrame();
            Assert.InRange(Math.Abs(fixture.Core.LatestVideoFrame!.AspectRatio-4f/3f),0,0.00001f);
        }
        finally {fixture?.Dispose();}
    }

    [Theory]
    [InlineData("segaemu")]
    [InlineData("genesisplusgxcs")]
    public void GameGearExtendedDisplayUpdatesBothDimensionsWithoutReset(string id)
    {
        Fixture? fixture=null;
        try
        {
            fixture=Create(id,"GameGear");fixture.Core.RunFrame();
            Assert.Equal(160,fixture.Core.LatestVideoFrame!.Width);Assert.Equal(144,fixture.Core.LatestVideoFrame.Height);
            object board=Read(fixture.Core,"_system")!;
            string prefix=id=="segaemu" ? "segaemu_" : "genesis_plus_gx_";
            fixture.Core.SetOption(prefix+"gg_extra","enabled");fixture.Core.RunFrame();
            Assert.Same(board,Read(fixture.Core,"_system"));
            Assert.Equal(256,fixture.Core.LatestVideoFrame!.Width);Assert.Equal(192,fixture.Core.LatestVideoFrame.Height);
            fixture.Core.SetOption(prefix+"overscan","full");fixture.Core.RunFrame();
            Assert.Equal(284,fixture.Core.LatestVideoFrame!.Width);Assert.Equal(240,fixture.Core.LatestVideoFrame.Height);
        }
        finally {fixture?.Dispose();}
    }

    [Fact]
    public void EveryDeclaredManagedOptionResourceResolvesThroughTheModule()
    {
        var engine=new RetroMediaLab.Emulation.Sega.Common.Services.Engine();
        string[] cultures=["en-US","fr-FR","ar-SA","cs-CZ","da-DK","de-DE","el-GR","es-ES","fi-FI","he-IL","hu-HU","id-ID","it-IT","ja-JP","ko-KR","nb-NO","nl-NL","pl-PL","pt-BR","pt-PT","ro-RO","ru-RU","sv-SE","th-TH","tr-TR","uk-UA","vi-VN","zh-Hans","zh-Hant"];
        var keys=SE.Constants.OptionConstants.All.Concat(GX.Constants.OptionConstants.All)
            .SelectMany(option=>new[] {option.Name,option.Description}.Concat(option.Values.Select(value=>value.Label)))
            .Where(key=>key is not null && key.StartsWith("Emulation.",StringComparison.Ordinal)).Distinct().ToArray();
        foreach(string culture in cultures)
            foreach(string? key in keys)
                Assert.True(engine.TryGetString(key!,CultureInfo.GetCultureInfo(culture),out string value) && !string.IsNullOrWhiteSpace(value),$"{culture}: {key}");
    }

    [Fact]
    public void AdvancedAudioSelectorControlsTheExistingFieldsForAllThreePorts()
    {
        foreach(string id in new[] {"genesisplusgx", "genesis_plus_gx_wide", "genesisplusgxcs"})
        {
            MachineConfiguration configuration=CoreSettingsFunctions.Configure(new("MegaDrive",id));
            var hidden=CoreSettingsFunctions.Blocks(configuration).SelectMany(block=>block.Fields).ToArray();
            var toggle=Assert.Single(hidden,field=>field.Id.EndsWith("_show_advanced_audio_settings",StringComparison.Ordinal));
            Assert.True(toggle.RefreshSettingsOnChange);Assert.False(toggle.RequiresRestart);
            var channels=hidden.Where(field=>field.Id.Contains("_channel_",StringComparison.Ordinal)).ToArray();
            Assert.NotEmpty(channels);Assert.All(channels,field=>Assert.False(field.IsVisible));
            configuration=configuration with {Options=new Dictionary<string,string>(configuration.Options!){[toggle.Id]="enabled"}};
            Assert.All(CoreSettingsFunctions.Blocks(configuration).SelectMany(block=>block.Fields)
                .Where(field=>field.Id.Contains("_channel_",StringComparison.Ordinal)),field=>Assert.True(field.IsVisible));
        }
    }

    [Theory]
    [InlineData("segaemu")]
    [InlineData("genesisplusgxcs")]
    public void CachePolicyAndMixingVolumesDoNotRecreateTheBoard(string id)
    {
        Fixture? fixture=null;
        try
        {
            fixture=Create(id,"MasterSystem");fixture.Core.RunFrame();
            object board=Read(fixture.Core,"_system")!;
            string prefix=id=="segaemu" ? "segaemu_" : "genesis_plus_gx_";
            Assert.False(fixture.Core.Options.Single(option=>option.Key==prefix+"cd_precache").RequiresRestart);
            fixture.Core.SetOption(prefix+"cd_precache","enabled");
            fixture.Core.SetOption(prefix+"psg_channel_0_volume","0");
            fixture.Core.SetOption(prefix+"no_sprite_limit","enabled");
            Assert.Same(board,Read(fixture.Core,"_system"));
            if(id=="segaemu")
            {
                var settings=(SE.Common.SystemConfiguration)Read(fixture.Core,"_settings")!;
                Assert.True(settings.PrecacheCd);Assert.True(settings.Video.DisableSpriteLimit);
                Assert.Equal(0,((SE.Common.Audio.SoundUnit)Read(board,"_sound")!).Psg.ChannelVolumes[0]);
            }
            else
            {
                var settings=(GX.SystemConfiguration)Read(fixture.Core,"_settings")!;
                Assert.True(settings.PrecacheCd);Assert.True(settings.Video.DisableSpriteLimit);
                Assert.Equal(0,((GX.Sound.SoundUnit)Read(board,"_sound")!).Psg.ChannelVolumes[0]);
            }
        }
        finally {fixture?.Dispose();}
    }

    [Fact]
    public void NativePortPsgChannelGainChangesTheProducedWaveform()
    {
        Assert.Contains(PsgTone(100),sample=>sample!=0);
        Assert.All(PsgTone(0),sample=>Assert.Equal(0,sample));
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public void NativePortControlsEachProgrammedFmChannel(int channel)
    {
        Assert.Contains(FmTone(channel,100),sample=>sample!=0);
        Assert.All(FmTone(channel,0),sample=>Assert.Equal(0,sample));
    }

    [Fact]
    public void NativePortCacheClosesItsSourceAndPreservesItsBytes()
    {
        GX.Cd.Cdd? drive=null;Stream? cached=null;
        var source=new TrackedStream([1,2,3,4]);
        try
        {
            drive=new(new CdBus(),new GX.Sound.BlipBuffer(1024),_=>source,CancellationToken.None){PrecacheImages=true};
            cached=(Stream)typeof(GX.Cd.Cdd).GetMethod("OpenFile",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(drive,["synthetic.bin"])!;
            Assert.True(source.Closed);Assert.IsType<MemoryStream>(cached);
            byte[] data=new byte[4];cached.ReadExactly(data);Assert.Equal(new byte[]{1,2,3,4},data);
        }
        finally {try {cached?.Dispose();} finally {try {drive?.Dispose();} finally {if(!source.Closed) source.Dispose();}}}
    }

    private sealed class TrackedStream(byte[] bytes):MemoryStream(bytes,false)
    {
        public bool Closed {get;private set;}
        protected override void Dispose(bool disposing) {try {base.Dispose(disposing);} finally {Closed=true;}}
    }
    private sealed class CdBus:GX.Cd.ICddBus
    {
        public GX.Cd.Register16[] Registers {get;}=new GX.Cd.Register16[256];
        public byte PendingInterrupts {get;set;}
        public Memory<byte> CdcRam {get;}=new byte[0x10000];
        public bool MegaSdEnabled=>false;
        public void UpdateSubIrq(uint mask) {}
        public void UpdateDecoder(uint header) {}
        public void UpdateMegaSdCdda(uint samples) {}
    }
    private static short[] PsgTone(int volume)
    {
        GX.Sound.BlipBuffer output=new(1024);output.SetRates(53693175,44100);
        GX.Sound.Psg psg=new(output,GX.Sound.PsgType.Integrated);psg.Reset();psg.ChannelVolumes[0]=volume;
        psg.Configure(0,100,0xff);psg.Write(0,0x84);psg.Write(0,0x10);psg.Write(0,0x90);
        psg.EndFrame(896040);output.EndFrame(896040);
        short[] samples=new short[output.SamplesAvailable*2];output.ReadSamples(samples,samples.Length/2);return samples;
    }
    private static int[] FmTone(int channel,int volume)
    {
        GX.Sound.Ym2612 fm=new();fm.Configure(GX.Sound.Ym2612.Integrated);fm.Reset();
        fm.ChannelVolumes.Fill(0);fm.ChannelVolumes[channel]=volume;
        uint bank=channel<3 ? 0u : 2u,local=(uint)channel%3;
        void Write(uint address,uint value) {fm.Write(bank,address);fm.Write(bank+1,value);}
        for(uint slot=0;slot<4;slot++)
        {uint offset=slot*4+local;Write(0x30+offset,1);Write(0x40+offset,0);Write(0x50+offset,31);Write(0x60+offset,0);Write(0x70+offset,0);Write(0x80+offset,0x0f);}
        Write(0xb0+local,7);Write(0xb4+local,0xc0);Write(0xa4+local,0x22);Write(0xa0+local,0x69);
        fm.Write(0,0x28);fm.Write(1,0xf0u|(uint)(channel<3 ? channel : channel+1));
        int[] samples=new int[2048];fm.Update(samples,samples.Length/2);return samples;
    }

    private sealed record Fixture(IEmulatorCore Core,MachineConfiguration Configuration) : IDisposable
    { public void Dispose()=>Core.Dispose(); }

    private static Fixture Create(string id,string model)
    {
        IEmulatorCore core=id=="segaemu" ? new SE.Services.ManagedCore() : new GX.Services.ManagedCore();
        IDisposable? board=null;bool transferred=false,completed=false;
        try
        {
            MachineConfiguration configuration=CoreSettingsFunctions.Configure(new(model,id));
            Write(core,"_configuration",configuration);
            object settings=core.GetType().GetMethod("CreateSettings",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(core,null)!;
            Write(core,"_settings",settings);
            byte[] bitmap=(byte[])Read(core,"_bitmap")!;
            byte[] rom=new byte[0x8000];new byte[]{0x21,0x00,0xc0,0x34,0xc3,0x03,0x00}.CopyTo(rom,0);
            byte hardware=model=="GameGear" ? (byte)0x40 : (byte)0x21;
            string name=model=="GameGear" ? "synthetic.gg" : "synthetic.sms";
            if(id=="segaemu")
            {
                var machine=SE.Factories.BoardFactory.Create(model,new((SE.Common.SystemConfiguration)settings,bitmap,1024,626,4096,44100,_=>null,(_,_)=>0,CancellationToken.None));
                board=machine;Assert.True(machine.Load(name,rom));machine.RefreshConfiguration();
            }
            else
            {
                var machine=new GX.SystemCore(hardware,(GX.SystemConfiguration)settings,bitmap,1024,626,4096,44100,_=>null,(_,_)=>0,CancellationToken.None);
                board=machine;Assert.True(machine.Load(name,rom));machine.RefreshConfiguration();
            }
            Write(core,"_system",board);transferred=true;completed=true;return new(core,configuration);
        }
        finally
        {
            if(!completed) {try {core.Dispose();} finally {if(!transferred)board?.Dispose();}}
        }
    }
    private static object? Read(object target,string name)
    {
        for(Type? type=target.GetType();type is not null;type=type.BaseType)
            if(type.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly) is {} field) return field.GetValue(target);
        throw new MissingFieldException(target.GetType().FullName,name);
    }
    private static void Write(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(target,value);
    private static EmulationOption Option(string key,string current,bool cold=false)=>new(key,key,null,null,"disabled",current,[],RequiresRestart:cold);
}

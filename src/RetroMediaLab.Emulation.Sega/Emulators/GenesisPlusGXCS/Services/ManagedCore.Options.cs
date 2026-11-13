using System.Globalization;
using System.IO;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Constants;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Sound;
using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Services;

internal sealed partial class ManagedCore
{
    private static byte HardwareForModel(string model) => model switch
    {
        ModelConstants.Sg1000 or ModelConstants.Sc3000 => SystemHardware.Sg1000,
        ModelConstants.MarkIII => SystemHardware.MarkIII, ModelConstants.MasterSystem => SystemHardware.MasterSystemII,
        ModelConstants.GameGear => SystemHardware.GameGear, ModelConstants.Pico => SystemHardware.Pico,
        ModelConstants.MegaCd => SystemHardware.MegaCd, _ => SystemHardware.MegaDrive
    };
    private string GetOption(string suffix)
    {
        string key = "genesis_plus_gx_" + suffix;
        return _configuration?.Options?.GetValueOrDefault(key) ?? CoreConstants.Definition.Options.FirstOrDefault(option => option.Key == key)?.DefaultValue ?? "";
    }
    private int Number(string suffix, int fallback)
    {
        string text = GetOption(suffix).TrimEnd('%');
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;
    }
    private SystemConfiguration CreateSettings()
    {
        SystemConfiguration settings = new();
        FillSettings(settings);
        for (int port = 0; port < 2; port++)
        {
            PeripheralCategory peripheral = _configuration!.Controllers?.ElementAtOrDefault(port) ?? PeripheralCategory.Automatic;
            (settings.PortSystems[port], settings.PadTypes[port]) = ControllerType(peripheral);
        }
        return settings;
    }
    private void FillSettings(SystemConfiguration settings)
    {
        settings.system = GetOption("system_hw") switch
        {
            "sg-1000" => 1, "sg-1000 II" => 2, "sg-1000 II + ram ext." => 3, "mark-III" => 0x10,
            "master system" => 0x20, "master system II" => 0x21, "game gear" => 0x40,
            "mega drive / genesis" => 0x80, _ => 0
        };
        if (settings.system == 0) settings.system = HardwareForModel(_configuration!.Model);
        settings.region_detect = GetOption("region_detect") switch { "ntsc-u" => 1, "pal" => 2, "ntsc-j" => 3, _ => 0 };
        settings.vdp_mode = GetOption("vdp_mode") switch { "60hz" => 1, "50hz" => 2, _ => 0 };
        settings.bios = GetOption("bios") == "enabled" ? (byte)3 : (byte)0;
        settings.force_dtack = GetOption("force_dtack") == "enabled" ? (byte)1 : (byte)0;
        settings.addr_error = GetOption("addr_error") == "disabled" ? (byte)0 : (byte)1;
        settings.cd_latency = GetOption("cd_latency") == "disabled" ? (byte)0 : (byte)1;
        settings.add_on = GetOption("add_on") switch { "sega/mega cd" => 1, "megasd" => 2, "none" => 3, _ => 0 };
        settings.lock_on = GetOption("lock_on") switch { "game genie" => 1, "action replay (pro)" => 2, "sonic & knuckles" => 3, _ => 0 };
        settings.cdda_volume = (short)Number("cdda_volume", 100); settings.pcm_volume = (short)Number("pcm_volume", 100);
        SoundConfiguration sound = settings.Sound;
        sound.Ym2413Mode = GetOption("ym2413") switch { "enabled" => 1, "disabled" => 0, _ => 2 };
        sound.UseOpll = GetOption("ym2413_core") == "nuked";
        string fm = GetOption("ym2612");
        sound.UseYm3438 = fm.StartsWith("nuked", StringComparison.Ordinal);
        sound.Ym3438Type = fm == "nuked (ym2612)" ? Ym3438.Ym2612Mode : Ym3438.ReadMode;
        sound.Ym2612Type = fm == "mame (asic ym3438)" ? Ym2612.Integrated : fm == "mame (ym2612)" ? Ym2612.Discrete : Ym2612.Enhanced;
        sound.Mono = GetOption("sound_output") == "mono";
        sound.Filter = GetOption("audio_filter") switch { "low-pass" => 1, "EQ" => 2, _ => 0 };
        sound.LowPassRange = (uint)(Number("lowpass_range", 60) * 65536 / 100);
        sound.PsgPreamp = (uint)Number("psg_preamp", 150); sound.FmPreamp = Number("fm_preamp", 100);
        sound.LowGain = Number("audio_eq_low", 100); sound.MidGain = Number("audio_eq_mid", 100); sound.HighGain = Number("audio_eq_high", 100);
        Vdp.Configuration video = settings.Video;
        video.overscan = GetOption("overscan") switch { "top/bottom" => 1, "left/right" => 2, "full" => 3, _ => 0 };
        video.gg_extra = GetOption("gg_extra") == "enabled" ? 1 : 0;
        video.render = GetOption("render") == "single field" ? 0 : 1;
        video.lcd = GetOption("lcd_filter") == "enabled" ? 204 : 0;
        video.enhanced_vscroll = GetOption("enhanced_vscroll") == "enabled" ? 1 : 0;
        video.enhanced_vscroll_limit = Number("enhanced_vscroll_limit", 8);
        string ntsc = GetOption("blargg_ntsc_filter"); video.ntsc = ntsc is "" or "disabled" ? 0 : 1;
        video.MdNtscSetup = NtscPreset(ntsc, true); video.SmsNtscSetup = NtscPreset(ntsc, false);
    }
    private static NtscSetup NtscPreset(string option, bool md) => option switch
    {
        "monochrome" => NtscFilter.Monochrome(md), "svideo" => NtscFilter.SVideo(md),
        "rgb" => NtscFilter.Rgb(md), _ => NtscFilter.Composite(md)
    };
    public void SetOption(string key, string value)
    {
        if (!CoreConstants.Definition.Options.Any(option => option.Key == key && option.Values.Any(candidate => candidate.Value == value)))
            throw new ArgumentException(nameof(value));
        Dictionary<string, string> options = new(_configuration!.Options ?? new Dictionary<string, string>(), StringComparer.Ordinal) { [key] = value };
        _configuration = _configuration with { Options = options };
        if (key is "genesis_plus_gx_system_hw" or "genesis_plus_gx_bios" or "genesis_plus_gx_region_detect" or "genesis_plus_gx_vdp_mode" or
            "genesis_plus_gx_lock_on" or "genesis_plus_gx_ym2612" or "genesis_plus_gx_ym2413" or "genesis_plus_gx_ym2413_core") StartSystem();
        else { FillSettings(_settings); _system!.RefreshConfiguration(Number("overclock", 100)); }
    }
    private int LoadFirmware(int identifier, Memory<byte> destination)
    {
        byte hardware = (byte)identifier, region = (byte)(identifier >> 8);
        string filename = identifier switch
        {
            0 => "sk.bin", 1 => "sk2chip.bin", 2 => "ggenie.bin", 3 => "areplay.bin",
            _ => hardware switch
            {
                SystemHardware.MegaDrive => "bios_MD.bin",
                SystemHardware.MegaCd => region == SystemHardware.Usa ? "bios_CD_U.bin" : region == SystemHardware.Europe ? "bios_CD_E.bin" : "bios_CD_J.bin",
                SystemHardware.GameGear => "bios.gg",
                _ => region == SystemHardware.Usa ? "bios_U.sms" : region == SystemHardware.Europe ? "bios_E.sms" : "bios_J.sms"
            }
        };
        string path = Path.Combine(_systemDirectory, filename);
        if (!File.Exists(path)) return 0;
        Stream? stream = null;
        try
        {
            stream = File.OpenRead(path);
            int total = 0;
            while (total < destination.Length)
            {
                int read = stream.Read(destination.Span[total..]);
                if (read == 0) break;
                total += read;
            }
            return total;
        }
        finally { stream?.Dispose(); }
    }
}

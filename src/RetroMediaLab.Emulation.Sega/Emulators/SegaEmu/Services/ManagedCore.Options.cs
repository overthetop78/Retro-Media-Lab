using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Contracts;

using Ym2612 = RetroMediaLab.Chips.Audio.Yamaha.YM2612.Instruction.Ym2612;
using Ym3438 = RetroMediaLab.Chips.Audio.Yamaha.YM3438.Instruction.Ym3438;
using System.Globalization;
using System.IO;
using RetroMediaLab.Emulation.Sega.Emulators.Common.Interop.Functions;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;

using RetroMediaLab.Chips.Video.Sega.VDP;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Services;

internal sealed partial class ManagedCore
{
    private static byte HardwareForModel(string model) => model switch
    {
        ModelConstants.Sg1000 or ModelConstants.Sc3000 or ModelConstants.Sf7000 => SystemHardware.Sg1000,
        ModelConstants.MarkIII => SystemHardware.MarkIII, ModelConstants.MasterSystem => SystemHardware.MasterSystem,
        ModelConstants.MasterSystemII => SystemHardware.MasterSystemII,
        ModelConstants.GameGear => SystemHardware.GameGear, ModelConstants.Pico => SystemHardware.Pico,
        ModelConstants.MegaCd or ModelConstants.MegaCdSecond or ModelConstants.MegaCdThirtyTwoX => SystemHardware.MegaCd, _ => SystemHardware.MegaDrive
    };
    private string GetOption(string suffix)
    {
        string key = "segaemu_" + suffix;
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
        string model = _configuration!.Model;
        bool hasMegaCd = ModelConstants.HasMegaCd(model) || model == ModelConstants.MegaDrive && _configuration.MegaCdEnabled;
        settings.HasExtensionPort = ModelConstants.IsMegaDriveFamily(model)
            && _configuration.HasMegaDriveExtensionPort;
        settings.UsePowerBaseConverter = GetSelectedMasterSystemConverter() is not null;
        settings.AllowAutomatic32X = !ModelConstants.IsMegaDriveFamily(model);
        settings.Enable32X = ModelConstants.HasThirtyTwoX(model)
            || ModelConstants.IsMegaDriveFamily(model) && _configuration.ThirtyTwoXEnabled;
        string hardware = _configuration!.Model switch
        {
            ModelConstants.Sg1000 or ModelConstants.Sc3000 => GetOption("sg1000_model"),
            _ => "auto"
        };
        settings.system = hardware switch
        {
            "sg-1000" => 1, "sg-1000 II" => 2, "sg-1000 II + ram ext." => 3, "mark-III" => 0x10,
            "game gear" => 0x40,
            "mega drive / genesis" => 0x80, _ => 0
        };
        if (settings.system == 0) settings.system = HardwareForModel(_configuration!.Model);
        settings.region_detect = GetOption("region_detect") switch { "ntsc-u" => 1, "pal" => 2, "ntsc-j" => 3, _ => 0 };
        settings.vdp_mode = GetOption("vdp_mode") switch { "60hz" => 1, "50hz" => 2, _ => 0 };
        if (model == ModelConstants.Sg1000)
        {
            settings.system = _configuration.HasSg1000RamExtension ? SystemHardware.Sg1000IIExtendedRam
                : _configuration.IsSg1000II ? SystemHardware.Sg1000II : SystemHardware.Sg1000;
            settings.vdp_mode = 0;
            if (settings.region_detect == 0)
            {
                string title = Path.GetFileNameWithoutExtension(_contentPath);
                if (title.Contains("(Europe)", StringComparison.OrdinalIgnoreCase)
                    || title.Contains("(EUR)", StringComparison.OrdinalIgnoreCase))
                    settings.region_detect = 2;
            }
        }
        if (ModelConstants.IsMegaDriveFamily(model))
        {
            settings.vdp_mode = 0;
            settings.region_detect = _configuration.Options?.GetValueOrDefault(SettingsConstants.MegaDriveRegion) switch
            {
                ModelConstants.RegionNtscU => 1,
                ModelConstants.RegionPal => 2,
                ModelConstants.RegionNtscJ => 3,
                _ => settings.region_detect
            };
            if (hasMegaCd && _megaCdBiosRegion != 0)
            {
                if (settings.region_detect != 0 && settings.region_detect != _megaCdBiosRegion)
                    throw new InvalidDataException(ErrorMessages.ContentRefused() + $" BIOS {RegionLabel(_megaCdBiosRegion)} / {RegionLabel(settings.region_detect)}.");
                settings.region_detect = _megaCdBiosRegion;
            }
        }
        // With an empty cartridge port the physical boot ROM is the only software present.
        settings.bios = ModelConstants.IsMegaDriveFamily(model) ? (_configuration.HasMegaDriveTmss ? (byte)1 : (byte)0)
            : _contentPath.Length == 0 ? (byte)1 : GetOption("bios") == "enabled" ? (byte)3 : (byte)0;
        if (Constants.OptionMachineConstants.MasterSystem.Contains(model))
        {
            bool hasBios = File.Exists(Path.Combine(_systemDirectory, FirmwareFunctions.MasterSystemBiosFile(_configuration)));
            settings.bios = hasBios ? (_contentPath.Length == 0 ? (byte)1 : (byte)3) : (byte)0;
        }
        if (_contentPath.Length == 0 && settings.region_detect == 0)
        {
            string? pattern = _configuration!.Model switch
            {
                ModelConstants.MasterSystem or ModelConstants.MasterSystemII => "bios_{0}.sms",
                _ => null
            };
            if (pattern is not null)
                foreach (var (region, code) in new[] { ("U", 1), ("E", 2), ("J", 3) })
                    if (File.Exists(Path.Combine(_systemDirectory, string.Format(CultureInfo.InvariantCulture, pattern, region))))
                    { settings.region_detect = (byte)code; break; }
        }
        settings.force_dtack = GetOption("force_dtack") == "enabled" ? (byte)1 : (byte)0;
        settings.addr_error = GetOption("addr_error") == "disabled" ? (byte)0 : (byte)1;
        settings.cd_latency = GetOption("cd_latency") == "disabled" ? (byte)0 : (byte)1;
        settings.add_on = hasMegaCd ? (byte)1 : GetOption("add_on") == "megasd" ? (byte)2 : (byte)3;
        string[]? cartridgeStack = ModelConstants.IsMegaDriveFamily(model)
            && _configuration.Options?.ContainsKey(CartridgeStackFunctions.Option) == true
            ? CartridgeStackFunctions.Read(_configuration) : null;
        settings.lock_on = settings.UsePowerBaseConverter ? (byte)0 : (byte)(GetOption("lock_on") switch { "game genie" => 1, "action replay (pro)" => 2, "sonic & knuckles" => 3, _ => 0 });
        if (cartridgeStack is not null)
        {
            settings.lock_on = cartridgeStack.Contains(CartridgeStackFunctions.SonicAndKnuckles) ? (byte)3 : (byte)0;
            settings.Cartridge.CheatAdapters = cartridgeStack.Where(item =>
                item is CartridgeStackFunctions.GameGenie or CartridgeStackFunctions.ActionReplay)
                .Select(item => item == CartridgeStackFunctions.GameGenie ? (byte)1 : (byte)2).ToArray();
        }
        settings.cdda_volume = (short)Number("cdda_volume", 100); settings.pcm_volume = (short)Number("pcm_volume", 100);
        settings.CdCartridgeId = GetOption("cart_size") switch
        {
            "disabled" => 0, "128k" => 1, "256k" => 2, "512k" => 3,
            "1meg" => 4, "2meg" => 5, _ => 6
        };
        if (!hasMegaCd || !CartridgeStackFunctions.Read(_configuration).Contains(CartridgeStackFunctions.CdMemory))
            settings.CdCartridgeId = 0;
        else if (settings.CdCartridgeId == 0)
            settings.CdCartridgeId = 6;
        settings.PrecacheCd = GetOption("cd_precache") == "enabled";
        SoundConfiguration sound = settings.Sound;
        sound.Ym2413Mode = GetOption("ym2413") switch { "enabled" => 1, "disabled" => 0, _ => 2 };
        sound.UseOpll = GetOption("ym2413_core") == "nuked";
        string fm = GetOption("ym2612");
        sound.UseYm3438 = fm.StartsWith("nuked", StringComparison.Ordinal);
        sound.Ym3438Type = fm == "nuked (ym2612)" ? Ym3438.Ym2612Mode : Ym3438.ReadMode;
        sound.Ym2612Type = fm == "mame (asic ym3438)" ? Ym2612.Integrated : fm == "mame (ym2612)" ? Ym2612.Discrete : Ym2612.Enhanced;
        if (ModelConstants.IsMegaDriveFamily(model))
        {
            sound.UseYm3438 = false;
            sound.Ym2612Type = _configuration.HasIntegratedMegaDriveFm ? Ym2612.Integrated : Ym2612.Discrete;
        }
        sound.Mono = model == ModelConstants.Sg1000 || GetOption("sound_output") == "mono";
        sound.Filter = GetOption("audio_filter") switch { "low-pass" => 1, "EQ" => 2, _ => 0 };
        sound.LowPassRange = (uint)(Number("lowpass_range", 60) * 65536 / 100);
        sound.PsgPreamp = (uint)Number("psg_preamp", 150); sound.FmPreamp = Number("fm_preamp", 100);
        sound.LowGain = Number("audio_eq_low", 100); sound.MidGain = Number("audio_eq_mid", 100); sound.HighGain = Number("audio_eq_high", 100);
        for (int channel = 0; channel < sound.PsgChannelVolumes.Length; channel++)
            sound.PsgChannelVolumes[channel] = Number($"psg_channel_{channel}_volume", 100);
        for (int channel = 0; channel < sound.MdChannelVolumes.Length; channel++)
            sound.MdChannelVolumes[channel] = Number($"md_channel_{channel}_volume", 100);
        for (int channel = 0; channel < sound.SmsChannelVolumes.Length; channel++)
            sound.SmsChannelVolumes[channel] = Number($"sms_fm_channel_{channel}_volume", 100);
        Vdp.Configuration video = settings.Video;
        video.DisableSpriteLimit = GetOption("no_sprite_limit") == "enabled";
        video.overscan = GetOption("overscan") switch { "top/bottom" => 1, "left/right" => 2, "full" => 3, _ => 0 };
        video.gg_extra = GetOption("gg_extra") == "enabled" ? 1 : 0;
        video.render = model == ModelConstants.Sg1000 || GetOption("render") == "single field" ? 0 : 1;
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
        if (_system is Machines.Sf7000.Sf7000Machine sf7000 && key == Machines.Sf7000.Sf7000PrintFile.DirectoryOption)
        {
            sf7000.ConnectPrinter(value);
            _configuration = _configuration! with
            {
                Options = new Dictionary<string, string>(_configuration.Options ?? new Dictionary<string, string>(), StringComparer.Ordinal)
                {
                    [key] = value
                }
            };
            return;
        }
        var option = CoreConstants.Definition.Options.SingleOrDefault(candidate => candidate.Key == key);
        if (option is null || !Emulators.Common.Interop.Functions.CoreSettingsFunctions.IsValidValue(option, value, _configuration?.Model))
            throw new ArgumentException(nameof(value));
        if (string.Equals(GetOption(key[8..]), value, StringComparison.Ordinal)) return;
        if (option.RequiresRestart) SavePersistent();
        Dictionary<string, string> options = new(_configuration!.Options ?? new Dictionary<string, string>(), StringComparer.Ordinal) { [key] = value };
        _configuration = _configuration with { Options = options };
        if (option.RequiresRestart) StartSystem(savePrevious: false);
        else if (_system is { } system) { FillSettings(_settings); system.RefreshConfiguration(Number("overclock", 100)); }
    }
    private byte _megaCdBiosRegion;

    private byte PrepareMegaCdBios(MachineConfiguration configuration)
    {
        if (!ModelConstants.HasMegaCd(configuration.Model) &&
            !(configuration.Model == ModelConstants.MegaDrive && configuration.MegaCdEnabled)) return 0;
        string destination = Path.Combine(_systemDirectory, Constants.FirmwareConstants.MegaCdFile);
        if (!File.Exists(destination))
        {
            string? preferred = configuration.Options?.GetValueOrDefault(SettingsConstants.MegaDriveRegion)
                ?? configuration.Options?.GetValueOrDefault("segaemu_region_detect");
            byte requested = preferred switch { "ntsc-u" => 1, "pal" => 2, "ntsc-j" => 3, _ => 0 };
            var legacy = Constants.FirmwareConstants.MegaCdRegions
                .Where(item => configuration.FirmwarePaths?.ContainsKey(item.LegacyField) == true)
                .OrderByDescending(item => item.Region == requested)
                .FirstOrDefault();
            if (legacy.LegacyField is null) return 0;
            File.Copy(configuration.FirmwarePaths![legacy.LegacyField], destination, overwrite: true);
            byte detected = MegaCdBiosRegion(configuration.FirmwarePaths[legacy.LegacyField]);
            return detected != 0 ? detected : legacy.Region;
        }
        byte detectedRegion = MegaCdBiosRegion(destination);
        if (detectedRegion != 0) return detectedRegion;
        bool regionFree = configuration.Options?.GetValueOrDefault(SettingsConstants.MegaCdRegionFree) is "enabled" or "True";
        if (regionFree) return 0;

        Span<byte> field = stackalloc byte[16];
        using var stream = File.OpenRead(destination);
        stream.Position = 0x1f0;
        int length = stream.ReadAtLeast(field, field.Length, throwOnEndOfStream: false);
        string header = System.Text.Encoding.ASCII.GetString(field[..length]).TrimEnd('\0', ' ').TrimStart(' ').ToUpperInvariant();
        int regions = header switch
        {
            "J" or "J 2" or "JAPAN" or "1" => 1,
            "U" or "U 2" or "USA" or "4" => 4,
            "E" or "E 2" or "EUR" or "EUROPE" or "8" => 8,
            "JUE" or "D" or "F" => 13,
            _ => 0
        };
        return regions switch
        {
            1 => 3, 4 => 1, 8 => 2,
            0 => throw new InvalidDataException(ErrorMessages.ContentRefused() + " Mega-CD BIOS (JPN / USA / EUR / Region Free)."),
            _ => 0
        };
    }

    private static byte MegaCdBiosRegion(string path)
    {
        var files = FirmwareCatalog.Inspect(path).ExpectedFileNames;
        if (files is null) return 0;
        var matchingRegions = Constants.FirmwareConstants.MegaCdRegions
            .Where(item => files.Contains(item.File, StringComparer.OrdinalIgnoreCase)).ToArray();
        return matchingRegions.Length == 1 ? matchingRegions[0].Region : (byte)0;
    }

    private static string RegionLabel(byte region) => region switch
    {
        1 => "NTSC-U", 2 => "PAL", 3 => "NTSC-J", _ => "AUTO"
    };

    private int LoadFirmware(int identifier, Memory<byte> destination)
    {
        byte hardware = (byte)identifier, region = (byte)(identifier >> 8);
        string filename = identifier switch
        {
            Machines.Sf7000.Sf7000Machine.IplFirmwareId => Constants.FirmwareConstants.Sf7000Ipl,
            0x3200 => "32X_G_BIOS.BIN", 0x3201 => "32X_M_BIOS.BIN", 0x3202 => "32X_S_BIOS.BIN",
            0 => "sk.bin", 1 => "sk2chip.bin", 2 => "ggenie.bin", 3 => "areplay.bin",
            _ => hardware switch
            {
                SystemHardware.MegaDrive => "bios_MD.bin",
                SystemHardware.MegaCd => Constants.FirmwareConstants.MegaCdFile,
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

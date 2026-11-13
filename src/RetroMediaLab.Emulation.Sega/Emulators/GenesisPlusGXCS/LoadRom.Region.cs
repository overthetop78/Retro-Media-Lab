namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS;

internal sealed partial class SystemCore
{
    public void GetRegion(bool detectFromHeader)
    {
        if (detectFromHeader)
        {
            if (system_hw == SystemHardware.MegaCd)
                region_code = cart.rom[0x20b] switch { 0x64 => SystemHardware.Europe, 0xa1 => SystemHardware.JapanNtsc, _ => SystemHardware.Usa };
            else if ((system_hw & 0x80) != 0)
            {
                int country = 0;
                string header = rominfo.country;
                if (header.StartsWith("eur", StringComparison.Ordinal) || header.StartsWith("EUR", StringComparison.Ordinal) || header.StartsWith("Eur", StringComparison.Ordinal)) country |= 8;
                else if (header.StartsWith("jap", StringComparison.Ordinal) || header.StartsWith("JAP", StringComparison.Ordinal)) country |= 1;
                else if (header.StartsWith("usa", StringComparison.Ordinal) || header.StartsWith("USA", StringComparison.Ordinal)) country |= 4;
                else
                {
                    for (int i = 0; i < 4; i++)
                    {
                        char value = i < header.Length ? char.ToUpperInvariant(header[i]) : '\0';
                        if (value == 'U') country |= 4;
                        else if (value is 'J' or 'K') country |= 1;
                        else if (value == 'E') country |= 8;
                        else if (value < 16) country |= value;
                        else if (value is >= '0' and <= '9') country |= value - '0';
                        else if (value is >= 'A' and <= 'F') country |= value - 'A' + 10;
                    }
                }
                region_code = (country & 4) != 0 ? SystemHardware.Usa : (country & 1) != 0 ? SystemHardware.JapanNtsc :
                    (country & 8) != 0 ? SystemHardware.Europe : (country & 2) != 0 ? SystemHardware.JapanPal : SystemHardware.Usa;
                string product = rominfo.product;
                if (product.Contains("T-45033", StringComparison.Ordinal) && rominfo.checksum == 0x0f81 ||
                    product.Contains("T-69046-50", StringComparison.Ordinal) || product.Contains("T-120106-00", StringComparison.Ordinal) ||
                    product.Contains("T-97126 -50", StringComparison.Ordinal) || product.Contains("T-113026-50", StringComparison.Ordinal) ||
                    product.Contains("T-70096 -00", StringComparison.Ordinal) || rominfo.checksum == 0 && rominfo.realchecksum == 0x1f7f)
                    region_code = SystemHardware.Europe;
                else if (rominfo.realchecksum == 0x532e && product.Contains("1011-00", StringComparison.Ordinal)) region_code = SystemHardware.JapanNtsc;
            }
            else region_code = (byte)_sms.DetectRegion();
            rom_region = region_code;
        }
        else region_code = rom_region;
        region_code = config.region_detect switch { 1 => SystemHardware.Usa, 2 => SystemHardware.Europe, 3 => SystemHardware.JapanNtsc, 4 => SystemHardware.JapanPal, _ => region_code };
        vdp_pal = (byte)(region_code >> 6 & 1);
        system_clock = vdp_pal != 0 ? SystemHardware.PalClock : SystemHardware.NtscClock;
        if (config.vdp_mode == 1) vdp_pal = 0;
        else if (config.vdp_mode == 2) vdp_pal = 1;
        if (config.master_clock == 1) system_clock = SystemHardware.NtscClock;
        else if (config.master_clock == 2) system_clock = SystemHardware.PalClock;
    }

    private void ConfigureRomInputs()
    {
        Span<int> previous = _input.OriginalPortSystems;
        Span<byte> ports = _input.PortSystems;
        for (int i = 0; i < 2; i++) if (previous[i] != -1) ports[i] = (byte)previous[i];
        _input.GunOffsetX = ports[1] == 3 ? 64 : 0;
        _input.GunOffsetY = 0;
        string title = rominfo.international;
        byte device = 3;
        int x, y = 0;
        if (title.Contains("MENACER", StringComparison.Ordinal)) x = 82;
        else if (title.Contains("T2 ; THE ARCADE GAME", StringComparison.Ordinal)) { x = 133; y = -8; }
        else if (title.Contains("BODY COUNT", StringComparison.Ordinal)) { x = 68; y = -24; }
        else if (title.Contains("CORPSE KILLER", StringComparison.Ordinal)) { x = 64; y = -8; }
        else if (title.Contains("CRIME PATROL", StringComparison.Ordinal)) x = 61;
        else if (title.Contains("MAD DOG II THE LOST GOLD", StringComparison.Ordinal)) { x = 70; y = 18; }
        else if (title.Contains("MAD DOG MCCREE", StringComparison.Ordinal)) x = 49;
        else if (title.Contains("WHO SHOT JOHNNY ROCK?", StringComparison.Ordinal)) { x = 60; y = 30; }
        else if (title.Contains("LETHAL ENFORCERS", StringComparison.Ordinal) || title.Contains("SNATCHER", StringComparison.Ordinal))
        { device = 4; x = title.Contains("GUN FIGHTERS", StringComparison.Ordinal) ? 24 : 0; }
        else return;
        for (int i = 0; i < 2; i++) if (previous[i] == -1) previous[i] = ports[i];
        ports[0] = 1; ports[1] = device;
        _input.GunOffsetX = x; _input.GunOffsetY = y;
    }
}

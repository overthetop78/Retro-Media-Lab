using System.Buffers.Binary;
using System.Globalization;
using System.IO;
using System.Text;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS;

internal sealed partial class SystemCore
{
    private byte romtype, rom_region;
    public byte DetectedHardware => romtype;
    public byte Region => region_code;
    public static int FirmwareId(byte hardware, byte region) => hardware | region << 8;

    public bool Load(string filename, ReadOnlySpan<byte> cartridgeImage = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        bool completed = false;
        try
        {
            _cancellation.ThrowIfCancellationRequested();
            _mediaPath = filename;
            bool noContent = filename.Length == 0 && cartridgeImage.IsEmpty;
            if (noContent) cart.rom.AsSpan().Fill(0xff);
            int size = noContent ? 0 : _cd.Drive.Load(filename, cart.rom);
            if (size < 0) return false;
            if (size != 0) system_hw = SystemHardware.MegaCd;
            else
            {
                int maximum = _cd.Drive.Loaded != 0 ? 0x800000 : cart.rom.Length;
                if (!cartridgeImage.IsEmpty)
                {
                    size = Math.Min(maximum, cartridgeImage.Length);
                    cartridgeImage[..size].CopyTo(cart.rom);
                }
                else if (!noContent)
                {
                    Stream? stream = null;
                    try
                    {
                        stream = _openFile(filename);
                        if (stream is null) return false;
                        while (size < maximum)
                        {
                            _cancellation.ThrowIfCancellationRequested();
                            int count = stream.Read(cart.rom.AsSpan(size, maximum - size));
                            if (count == 0) break;
                            size += count;
                        }
                    }
                    finally { stream?.Dispose(); }
                }
                if (size > 0x800000) system_bios &= unchecked((byte)~0x10);
                else if (size > 0x400000) system_bios &= unchecked((byte)~0x60);
                else if (size <= 0 && !noContent) return false;
                string extension = Path.GetExtension(filename).ToUpperInvariant();
                system_hw = noContent ? system_hw : extension switch
                {
                    ".SMS" => SystemHardware.MasterSystemII,
                    ".GG" => SystemHardware.GameGear,
                    ".SG" => SystemHardware.Sg1000,
                    _ => SystemHardware.MegaDrive
                };
                if (system_hw == SystemHardware.MegaDrive)
                {
                    if (extension == ".MDX")
                    {
                        for (int i = 4; i < size - 1; i++) cart.rom[i - 4] = (byte)(cart.rom[i] ^ 0x40);
                        size -= 5;
                    }
                    if (Matches(0x100, "ESAGM GE ARDVI E") || Matches(0x100, "ESAGG NESESI") ||
                        Matches(0x80100, "ESAGM GE ARDVI E") || Matches(0x80100, "ESAGG NESESI"))
                        SwapWords(cart.rom.AsSpan(0, (size + 1) & ~1));
                }
                if (!Matches(0x100, "SEGA") && ((size / 512) & 1) != 0 && size % 512 == 0)
                {
                    size -= 512;
                    cart.rom.AsSpan(512, size).CopyTo(cart.rom);
                    if (system_hw == SystemHardware.MegaDrive)
                    {
                        Span<byte> block = stackalloc byte[0x4000];
                        for (int offset = 0; offset < size / 0x4000 * 0x4000; offset += 0x4000)
                        {
                            cart.rom.AsSpan(offset, 0x4000).CopyTo(block);
                            for (int i = 0; i < 0x2000; i++)
                            {
                                cart.rom[offset + i * 2] = block[0x2000 + i];
                                cart.rom[offset + i * 2 + 1] = block[i];
                            }
                        }
                    }
                }
            }
            cart.romsize = checked((uint)size);
            GetRomInformation();
            GetRegion(true);
            if (system_hw == SystemHardware.MegaDrive) SwapWords(cart.rom.AsSpan(0, (size + 1) & ~1));
            if (rominfo.consoletype.Contains("SEGA PICO", StringComparison.Ordinal)) system_hw = SystemHardware.Pico;
            romtype = system_hw;
            if (system_hw == SystemHardware.MegaCd)
            {
                if (LoadBios(SystemHardware.MegaCd) == 0) return false;
                cd_boot = 0;
            }
            else if (!noContent && system_hw == SystemHardware.MegaDrive && cart.romsize <= 0x800000 && config.add_on != 3)
            {
                if (_cd.Drive.Loaded == 0) _cd.Drive.Load(Path.ChangeExtension(filename, ".chd"), _cd.DataController.Ram.Span);
                if ((rominfo.peripherals & 0x1000) != 0 || rominfo.domestic.Contains("FLUX", StringComparison.Ordinal) ||
                    config.add_on == 1 || (config.add_on | _cd.Drive.Loaded) == 1)
                {
                    if (LoadBios(SystemHardware.MegaCd) != 0)
                    {
                        if (_cd.Drive.Loaded == 0) _cd.Drive.Load(Path.ChangeExtension(filename, ".iso"), _cd.DataController.Ram.Span);
                        system_hw = SystemHardware.MegaCd; cd_boot = 0x40;
                    }
                    else _cd.Drive.Unload();
                }
                else if (rominfo.ROMType.Contains("BR", StringComparison.Ordinal))
                {
                    system_hw = SystemHardware.MegaCd; cd_boot = 0;
                    DetectCdHardware(rominfo.domestic);
                    cart.rom.AsSpan(0, _cd.BootRom.Length).CopyTo(_cd.BootRom.Span);
                    system_bios = (byte)((system_bios | 0x10) & 0xf0 | region_code >> 4);
                }
            }
            if (config.system == SystemHardware.MegaDrive)
            {
                if ((system_hw & 0x80) == 0) system_hw = SystemHardware.PowerBaseConverter;
            }
            else if (config.system == SystemHardware.GameGear)
            {
                if (system_hw != SystemHardware.GameGear) system_hw = SystemHardware.GameGearMasterSystem;
            }
            else if (config.system != 0) system_hw = config.system;
            if ((config.bios & 1) != 0 && system_hw == SystemHardware.MegaDrive)
            {
                int bytes = _loadFirmware(FirmwareId(SystemHardware.MegaDrive, region_code), boot_rom);
                if (bytes > 0)
                {
                    SwapWords(boot_rom.AsSpan(0, (bytes + 1) & ~1));
                    for (int i = bytes; i < boot_rom.Length; i++) boot_rom[i] = boot_rom[i % bytes];
                    system_bios |= SystemHardware.MegaDrive;
                }
            }
            config.PortSystems.CopyTo(_input.PortSystems);
            ConfigureRomInputs();
            _cd.PcmChip.Volume = config.pcm_volume;
            _cd.Drive.AudioVolume = config.cdda_volume;
            _cd.Drive.LatencyFactor = config.cd_latency;
            InitializeSystem();
            Reset();
            completed = true;
            return true;
        }
        finally { if (!completed) Dispose(); }
    }

    private int LoadBios(int hardware)
    {
        if (hardware == SystemHardware.MegaCd)
        {
            if ((system_bios & 0x10) != 0 && (system_bios & 0x0c) == region_code >> 4) return -1;
            int size = _loadFirmware(FirmwareId((byte)hardware, region_code), _cd.BootRom);
            if (size > 0)
            {
                DetectCdHardware(Encoding.Latin1.GetString(_cd.BootRom.Span.Slice(0x120, 16)));
                SwapWords(_cd.BootRom.Span[..((size + 1) & ~1)]);
                system_bios = (byte)((system_bios | 0x10) & 0xf0 | region_code >> 4);
            }
            return size;
        }
        if (hardware is SystemHardware.GameGear or SystemHardware.GameGearMasterSystem)
        {
            if ((system_bios & 0x40) != 0) return -1;
            system_bios &= unchecked((byte)~0x60);
            int size = cart.romsize <= 0x400000 ? _loadFirmware(FirmwareId(SystemHardware.GameGear, region_code), cart.rom.AsMemory(0x400000, 0x400000)) : 0;
            if (size > 0) system_bios |= 0x40;
            return size;
        }
        if (hardware is SystemHardware.MasterSystem or SystemHardware.MasterSystemII)
        {
            if ((system_bios & 0x20) != 0 && (system_bios & 0x0c) == region_code >> 4) return -1;
            system_bios &= unchecked((byte)~0x60);
            int size = cart.romsize <= 0x400000 ? _loadFirmware(FirmwareId(SystemHardware.MasterSystem, region_code), cart.rom.AsMemory(0x400000, 0x400000)) : 0;
            if (size > 0) system_bios = (byte)((system_bios | 0x20) & 0xf0 | region_code >> 4);
            return size;
        }
        system_bios &= unchecked((byte)~0x70);
        return 0;
    }

    private void DetectCdHardware(string title) => _cd.HardwareType = title.StartsWith("WONDER-MEGA BOOT", StringComparison.Ordinal) ? (byte)1 :
        title.StartsWith("WONDERMEGA2 BOOT", StringComparison.Ordinal) ? (byte)2 : title.StartsWith("CDX BOOT ROM", StringComparison.Ordinal) ? (byte)3 : (byte)0;
    private static void SwapWords(Span<byte> bytes)
    {
        for (int i = 0; i < bytes.Length; i += 2) (bytes[i], bytes[i + 1]) = (bytes[i + 1], bytes[i]);
    }
    private bool Matches(int offset, string text)
    {
        for (int i = 0; i < text.Length; i++) if (cart.rom[offset + i] != (byte)text[i]) return false;
        return true;
    }

    private void GetRomInformation()
    {
        rominfo.consoletype = rominfo.copyright = rominfo.domestic = rominfo.international = rominfo.ROMType = rominfo.product = rominfo.country = "";
        rominfo.checksum = rominfo.realchecksum = rominfo.peripherals = 0;
        rominfo.romstart = rominfo.romend = 0;
            if (cart.romsize == 0) return;
        if ((system_hw & 0x80) != 0)
        {
            rominfo.consoletype = HeaderText(256, 16); rominfo.copyright = HeaderText(272, 16);
            rominfo.domestic = HeaderName(288); rominfo.international = HeaderName(336);
            rominfo.ROMType = HeaderText(384, 2); rominfo.product = HeaderText(386, 12);
            rominfo.checksum = BinaryPrimitives.ReadUInt16BigEndian(cart.rom.AsSpan(398));
            // The Windows source copies these uint32 header fields directly.
            rominfo.romstart = BinaryPrimitives.ReadUInt32LittleEndian(cart.rom.AsSpan(416));
            rominfo.romend = BinaryPrimitives.ReadUInt32LittleEndian(cart.rom.AsSpan(420));
            rominfo.country = HeaderText(496, 16);
            for (int i = 0x200; i < cart.romsize; i += 2)
                rominfo.realchecksum = unchecked((ushort)(rominfo.realchecksum + (cart.rom[i] << 8) + cart.rom[i + 1]));
            const string codes = "J6KPBFL40RTVCMG";
            for (int i = 0; i < 14; i++)
                for (int j = 0; j < codes.Length; j++)
                    if (cart.rom[400 + i] == codes[j]) rominfo.peripherals |= (ushort)(1 << j);
        }
        else
        {
            int offset = Matches(0x1ff0, "TMR SEGA") ? 0x1ff0 : Matches(0x3ff0, "TMR SEGA") ? 0x3ff0 : Matches(0x7ff0, "TMR SEGA") ? 0x7ff0 : 0;
            if (offset == 0 || offset >= cart.romsize) return;
            rominfo.checksum = BinaryPrimitives.ReadUInt16LittleEndian(cart.rom.AsSpan(offset + 10));
            rominfo.product = (cart.rom[offset + 14] >> 4).ToString("D2", CultureInfo.InvariantCulture) +
                cart.rom[offset + 13].ToString("x2", CultureInfo.InvariantCulture) + cart.rom[offset + 12].ToString("x2", CultureInfo.InvariantCulture) + "-" + (cart.rom[offset + 14] & 15).ToString(CultureInfo.InvariantCulture);
            int country = cart.rom[offset + 15] >> 4;
            rominfo.country = country switch { 3 => "SMS Japan", 4 => "SMS Export", 5 => "GG Japan", 6 => "GG Export", 7 => "GG International", _ => "Unknown (" + country.ToString(CultureInfo.InvariantCulture) + ")" };
            rominfo.romend = (cart.rom[offset + 15] & 15) switch { 0 => 0x3ffff, 1 => 0x7ffff, 2 => 0xfffff, 10 => 0x1fff, 11 => 0x3fff, 12 => 0x7fff, 13 => 0xbfff, 14 => 0xffff, 15 => 0x1ffff, _ => 0u };
        }
    }
    private string HeaderText(int offset, int length)
    {
        ReadOnlySpan<byte> bytes = cart.rom.AsSpan(offset, length);
        int zero = bytes.IndexOf((byte)0);
        return Encoding.Latin1.GetString(zero >= 0 ? bytes[..zero] : bytes);
    }
    private string HeaderName(int offset)
    {
        Span<byte> bytes = stackalloc byte[48];
        int length = 1; bytes[0] = cart.rom[offset];
        for (int i = 1; i < 48; i++) if (bytes[length - 1] != 32 || cart.rom[offset + i] != 32) bytes[length++] = cart.rom[offset + i];
        int zero = bytes[..length].IndexOf((byte)0);
        return Encoding.Latin1.GetString(bytes[..(zero >= 0 ? zero : length)]);
    }
}

using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Common;
using RetroMediaLab.Chips.Common.Memory;
using System.IO;
using System.Buffers.Binary;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Services;

internal sealed partial class ManagedCore
{
    private int _skippedFrames;
    private bool ShouldSkipFrame()
    {
        string option = GetOption("frameskip");
        if (option == "disabled" || _audioStatus is not { IsActive: true, CapacityFrames: > 0 } status)
        { _skippedFrames = 0; return false; }
        bool skip = option == "auto" ? status.BufferedFrames == 0 : status.BufferedFrames * 100L / status.CapacityFrames < Number("frameskip_threshold", 33);
        if (skip && _skippedFrames < 30) { _skippedFrames++; return true; }
        _skippedFrames = 0; return false;
    }
    private void PublishVideo(SegaBoard system)
    {
        int width = system.Video.OutputWidth;
        int height = system.Video.ViewportHeight + system.Video.ViewportY * 2;
        int normalWidth = system.Video.ViewportWidth + system.Video.ViewportX * 2;
        bool interlaced = system.Video.interlaced != 0 && _settings.Video.render != 0;
        float aspect = CalculateAspect(system, normalWidth, height);
        if (interlaced) height *= 2;
        int bytesPerPixel = system.Video.EffectivePixelDepth == 32 ? 4 : 2;
        int left = GetOption("left_border") switch { "left border" => 8, "left & right borders" => 8, _ => 0 };
        int right = GetOption("left_border") == "left & right borders" ? 8 : 0;
        if (system.Video.ViewportX != 0 || (system.Video.reg[0] & 0x20) == 0 || system.Hardware is not (0x10 or 0x20 or 0x21 or 0x81)) left = right = 0;
        int pitch = (width - left - right) * bytesPerPixel;
        byte[] pixels = new byte[pitch * height];
        for (int line = 0; line < height; line++)
            _bitmap.AsSpan(line * BitmapPitch + left * bytesPerPixel, pitch).CopyTo(pixels.AsSpan(line * pitch));
        if (GetOption("gun_cursor") == "enabled")
            DrawGunCursors(system, pixels, width - left - right, height, pitch, bytesPerPixel, left, normalWidth, interlaced);
        LatestVideoFrame = new(pixels, width - left - right, height, pitch,
            bytesPerPixel == 4 ? EmulationPixelFormat.Xrgb8888 : EmulationPixelFormat.Rgb565, aspect, _sequence, _timestamp);
    }
    private static void DrawGunCursors(SegaBoard system, Span<byte> pixels, int width, int height,
        int pitch, int bytesPerPixel, int croppedLeft, int normalWidth, bool interlaced)
    {
        for (int device = 0; device < system.Input.DeviceTypes.Length; device++)
        {
            if (system.Input.DeviceTypes[device] != 4) continue;
            int inputX = system.Input.AnalogInputs[device * 2], inputY = system.Input.AnalogInputs[device * 2 + 1];
            if (inputX < 0 || inputY < 0 || inputX >= system.Video.ViewportWidth || inputY >= system.Video.ViewportHeight) continue;
            int x = (inputX + system.Video.ViewportX) * system.Video.OutputWidth / normalWidth - croppedLeft;
            int y = (inputY + system.Video.ViewportY) * (interlaced ? 2 : 1);
            uint color = device == 0 ? 0x000000ffu : 0x00ff0000u;
            ushort color565 = device == 0 ? (ushort)0x001f : (ushort)0xf800;
            for (int offset = -5; offset <= 5; offset++)
            {
                WriteCursorPixel(pixels, x + offset, y, width, height, pitch, bytesPerPixel, color, color565);
                WriteCursorPixel(pixels, x, y + offset, width, height, pitch, bytesPerPixel, color, color565);
            }
        }
    }
    private static void WriteCursorPixel(Span<byte> pixels, int x, int y, int width, int height,
        int pitch, int bytesPerPixel, uint color, ushort color565)
    {
        if ((uint)x >= width || (uint)y >= height) return;
        Span<byte> pixel = pixels.Slice(y * pitch + x * bytesPerPixel, bytesPerPixel);
        if (bytesPerPixel == 4) BinaryPrimitives.WriteUInt32LittleEndian(pixel, color);
        else BinaryPrimitives.WriteUInt16LittleEndian(pixel, color565);
    }
    private float CalculateAspect(SegaBoard system, int width, int height)
    {
        string aspect = GetOption("aspect_ratio");
        if (aspect == "4:3") return 4f / 3f;
        if (aspect == "Uncorrected") return (float)width / height;
        if (aspect is not ("NTSC PAR" or "PAL PAR") && system.Hardware is 0x40 or 0x41 && _settings.Video.overscan == 0 && _settings.Video.gg_extra == 0)
            return 1.2f * width / height;
        double dotRate = system.MasterClock / (system.Video.ViewportWidth == 320 ? 8.0 : 10.0);
        double sampleRate = aspect == "NTSC PAR" ? 135000000.0 / 11 : aspect == "PAL PAR" ? 14750000 : system.Video.vdp_pal != 0 ? 14750000 : 135000000.0 / 11;
        return (float)(sampleRate / dotRate * width / (height * 2.0));
    }
    private string PersistentPath(string kind)
    {
        string identity = _contentPath.Length == 0 ? "system" : Path.GetFileNameWithoutExtension(_contentPath);
        if (kind == "brm" && GetOption("system_bram") == "per bios") identity = "bios-" + _system!.Region.ToString("x2");
        if (kind == "crm" && GetOption("cart_bram") == "per cart") identity = "cd-cartridge";
        return Path.Combine(_saveDirectory, identity + ".segaemu." + kind);
    }
    private void LoadPersistent()
    {
        ReadPersistent(PersistentPath("srm"), _system!.Backup.Data);
        if (_system.Hardware == SystemHardware.MegaCd)
        {
            ReadPersistent(PersistentPath("brm"), _system.CdBackupRam.Span);
            FormatCdBackup(_system.CdBackupRam.Span, internalRam: true);
            if (!_system.CdCartridgeRam.IsEmpty)
            {
                ReadPersistent(PersistentPath("crm"), _system.CdCartridgeRam.Span);
                FormatCdBackup(_system.CdCartridgeRam.Span, internalRam: false);
            }
        }
    }
    private static void ReadPersistent(string path, Span<byte> destination)
    {
        if (!File.Exists(path)) return;
        Stream? stream = null;
        try
        {
            stream = File.OpenRead(path);
            while (!destination.IsEmpty)
            {
                int read = stream.Read(destination);
                if (read == 0) break;
                destination = destination[read..];
            }
        }
        finally { stream?.Dispose(); }
    }
    private static ReadOnlySpan<byte> BackupFormat =>
    [
        0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0x5f,0,0,0,0,0x40,
        0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
        0x53,0x45,0x47,0x41,0x5f,0x43,0x44,0x5f,0x52,0x4f,0x4d,0,1,0,0,0,
        0x52,0x41,0x4d,0x5f,0x43,0x41,0x52,0x54,0x52,0x49,0x44,0x47,0x45,0x5f,0x5f,0x5f
    ];
    private static void FormatCdBackup(Span<byte> ram, bool internalRam)
    {
        if (ram[^32..].SequenceEqual(BackupFormat[32..])) return;
        ram[..^64].Clear(); BackupFormat.CopyTo(ram[^64..]);
        Span<byte> format = ram[^64..];
        int blocks = ram.Length / 64 - 3;
        for (int index = 0x10; index <= 0x16; index += 2)
        { format[index] = internalRam ? (byte)0 : (byte)(blocks >> 8); format[index + 1] = (byte)blocks; }
    }
    private void SavePersistent()
    {
        if (_system is null || string.IsNullOrEmpty(_saveDirectory)) return;
        if (_system.Backup.Enabled) File.WriteAllBytes(PersistentPath("srm"), _system.Backup.Data);
        if (_system.Hardware == SystemHardware.MegaCd)
        {
            if (_system.CdBackupRam.Span[^32..].SequenceEqual(BackupFormat[32..])) File.WriteAllBytes(PersistentPath("brm"), _system.CdBackupRam.Span);
            if (!_system.CdCartridgeRam.IsEmpty && _system.CdCartridgeRam.Span[^32..].SequenceEqual(BackupFormat[32..])) File.WriteAllBytes(PersistentPath("crm"), _system.CdCartridgeRam.Span);
        }
    }
}

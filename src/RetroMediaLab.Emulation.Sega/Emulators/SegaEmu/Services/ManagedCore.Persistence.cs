using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Constants;
using System.IO;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Services;

internal sealed partial class ManagedCore
{
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
        if (_saturn is { } saturn)
        {
            FileStream? backup = null;
            try
            {
                backup = File.Create(SaturnBackupPath);
                saturn.SaveBackupRam(backup);
            }
            finally { backup?.Dispose(); }
            return;
        }
        if (_system is null || string.IsNullOrEmpty(_saveDirectory)) return;
        if (_system.Backup.Enabled) File.WriteAllBytes(PersistentPath("srm"), _system.Backup.Data);
        if (_system.Hardware == SystemHardware.MegaCd)
        {
            if (_system.CdBackupRam.Span[^32..].SequenceEqual(BackupFormat[32..])) File.WriteAllBytes(PersistentPath("brm"), _system.CdBackupRam.Span);
            if (!_system.CdCartridgeRam.IsEmpty && _system.CdCartridgeRam.Span[^32..].SequenceEqual(BackupFormat[32..])) File.WriteAllBytes(PersistentPath("crm"), _system.CdCartridgeRam.Span);
        }
    }
}

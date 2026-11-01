using System.IO;
namespace RetroMediaLab.Emulation.Commodore.Common.Machines.AmigaComputers.Contracts;

public sealed record AmigaFirmware(string Path, long Size, string Md5, string Sha256, DateTime LastWriteTimeUtc, FirmwareType Type, bool IsKnown, bool IsOfficial, string? Name, string? Version, IReadOnlyList<string> CompatibleModels);


using System.IO;
namespace RetroMediaLab.Emulation.Mattel.Common.Machines.Common.Contracts;

public sealed record FirmwareConfiguration(FirmwareCategory Category, string Path, bool IsRequired, bool IsOriginalFirmware = true);

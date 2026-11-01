using RetroMediaLab.Emulation.Apple.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Apple.Common.Constants;
namespace RetroMediaLab.Emulation.Apple.Common.Machines.Common.Contracts;

public sealed record FirmwareConfiguration(FirmwareCategory Category, string Path, bool IsRequired, bool IsOriginalFirmware = true);

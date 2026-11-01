namespace RetroMediaLab.Emulation.Wenquxing.Common.Machines.Common.Contracts;

public sealed record FirmwareConfiguration(IReadOnlyDictionary<FirmwareCategory, string> Images);

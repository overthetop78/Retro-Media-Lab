using RetroMediaLab.Emulation.Constants;

namespace RetroMediaLab.Emulation.Contracts;

public sealed record EmulationCompactDiscDriveSettings(string Speed = EmulationCompactDiscDriveSettingsConstants.DefaultSpeed, bool CacheImage = false, bool IgnoreErrors = false, IReadOnlyDictionary<string, string>? Options = null);

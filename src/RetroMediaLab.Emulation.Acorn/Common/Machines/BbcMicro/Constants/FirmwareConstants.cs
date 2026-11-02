namespace RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Constants;

/// <summary>Capacité d'une banque ROM BBC ; distincte de la quantité de RAM et des chemins natifs des émulateurs.</summary>
internal static class BbcMicroFirmwareConstants
{
    internal const string SoftwareExplanation = "Emulation.Acorn.Firmware.SoftwareRequirements";
    internal const string DiskExplanation = "Emulation.Acorn.Firmware.DiskRequirements";
    internal const string NetworkExplanation = "Emulation.Acorn.Firmware.NetworkRequirements";
    internal const string SpeechExplanation = "Emulation.Acorn.Firmware.SpeechRequirements";
    internal const string SystemExplanation = "Emulation.Acorn.Firmware.SystemRequirements";
    internal const string HardwareExplanation = "Emulation.Acorn.Firmware.HardwareRequirements";
    internal const int MaximumRomImageBytes = 131_072;
    internal const int BankSize = 16_384;
    /// <summary>96 glyphes de dix lignes ; la ROM interne SAA505x ne contient pas les codes de contrôle.</summary>
    internal const int CharacterRomSize = RetroMediaLab.Chips.Video.Philips.SAA50xx.SAA5050.Services.TeletextSaa5050.CharacterRomSize;
}

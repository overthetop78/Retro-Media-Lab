using System.Text.RegularExpressions;

namespace RetroMediaLab.App.Contracts.Localization;

internal sealed record ExplorerWarningPattern(string Key, Regex Regex);

using System.Windows;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.App.Contracts.Emulation.Settings;

internal sealed record EmulationSettingsControlField(string Label, FrameworkElement Control, string? Explanation = null, string? DetailedExplanation = null, EmulationSettingsField? Definition = null);

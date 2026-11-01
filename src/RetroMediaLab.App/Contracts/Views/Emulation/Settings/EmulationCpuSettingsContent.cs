using System.Windows.Controls;
using RetroMediaLab.App.Contracts.Emulation.Settings;

namespace RetroMediaLab.App.Contracts.Views.Emulation.Settings;

internal sealed record EmulationCpuSettingsContent(
    EmulationSettingsControlField CpuModel,
    TextBlock CpuSummary,
    EmulationSettingsControlField? Precision,
    EmulationSettingsControlField? Fpu,
    EmulationSettingsControlField OriginalSpeed,
    EmulationSettingsControlField? CpuSpeed,
    IReadOnlyList<EmulationSettingsControlField>? ProcessorFields = null);

using RetroMediaLab.App.Contracts.Emulation.Settings;
using System.Windows.Controls;

namespace RetroMediaLab.App.Contracts.Emulation.Memory;

internal sealed record EmulationMemorySettingsContent(
    IReadOnlyList<EmulationSettingsControlField> MainMemory,
    IReadOnlyList<EmulationSettingsControlField> MemoryExtensions,
    TextBlock TotalMemory);

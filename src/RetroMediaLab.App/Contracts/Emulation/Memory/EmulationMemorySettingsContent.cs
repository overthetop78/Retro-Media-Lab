using RetroMediaLab.App.Contracts.Emulation.Settings;
using System.Windows.Controls;

namespace RetroMediaLab.App.Contracts.Emulation.Memory;

internal sealed record EmulationMemorySettingsContent(
    IReadOnlyList<EmulationSettingsControlField> MainMemory,
    TextBlock MainMemoryHint,
    IReadOnlyList<EmulationSettingsControlField> MemoryExtensions,
    TextBlock MemoryExtensionsHint,
    TextBlock TotalMemory);

using RetroMediaLab.Emulation.Apple.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation.Apple.Common.Constants;
namespace RetroMediaLab.Emulation.Apple.Common.Machines.Common.Contracts;

public sealed record MediaConfiguration(string Path, MediaCategory Category, EmulationMediaSlot Slot, string? Label = null, bool IsReadOnly = false, bool IsInserted = true);

using RetroMediaLab.Chips.Memory.Toshiba.TMM23xx.TMM23256.Models;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Enums;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Contracts;

/// <summary>Physical ROM selection, independent of host file loading. Null part
/// profiles select an explicitly uncharacterized logical peripheral, never an
/// invented manufacturer/speed grade. Named SRAM cartridge layouts select their
/// documented part profiles; the Castle ROM mask grade remains explicitly unqualified.
/// The internal16KiB option is off by default.</summary>
internal sealed record Sg1000RomConfiguration(
    bool InternalGameEnabled = false,
    Sg1000CartridgeLayout CartridgeLayout = Sg1000CartridgeLayout.Standard,
    Tmm23256Timing? CartridgeLowProfile = null,
    Tmm23256Timing? CartridgeHighProfile = null,
    Tmm23256Timing? InternalGameProfile = null);

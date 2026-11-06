using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Contracts;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.ModelBPlus.Contracts;
using RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Contracts;
using RetroMediaLab.Chips.IO.NEC.UPD700x.UPD7002.Contracts;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Enums;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Contracts;

/// <summary>Données de la carte commune BBC ; la sérialisation est assurée par EmulatorCore.</summary>
internal sealed record BbcMicroState
{
    public required byte[] Ram { get; init; }
    public required ModelBPlusMemoryState? PlusMemory { get; init; }
    public required int CyclesPerFrame { get; init; }
    public required Cpu6502State Cpu { get; init; }
    public required Via6522State SystemVia { get; init; }
    public required Via6522State? UserVia { get; init; }
    public required Adc7002State? Adc { get; init; }
    public required KeyboardState Keyboard { get; init; }
    public required SidewaysRamState SidewaysRam { get; init; }
    public required MemoryExtensionState MemoryExtension { get; init; }
    public required AudioState Audio { get; init; }
    public required VideoState Video { get; init; }
    public required SerialState Serial { get; init; }
    public required CassetteState Cassette { get; init; }
    public required DisksState Disks { get; init; }
    public required EconetState Econet { get; init; }
    public required long MasterClockWakeTick { get; init; }
    public required long WinchesterWakeTick { get; init; }
    public required long AdcWakeTick { get; init; }
    public required byte FireInputs { get; init; }
    public required bool CassetteClockRequired { get; init; }
    public required byte Latch { get; init; }
    public required byte SoundDataBus { get; init; }
    public required int KeyboardColumn { get; init; }
    public required long Ticks { get; init; }
    public required long NextFrame { get; init; }
    public required bool Break { get; init; }
    public required BbcInterruptSources InterruptSources { get; init; }
    public required BbcNmiSources NmiSources { get; init; }
    public required bool ViaTrailingEdgePending { get; init; }
}

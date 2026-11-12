using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Models;

namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;

/// <summary>État du CPU seul ; aucune mémoire de carte ni référence hôte n'est possédée.</summary>
public sealed record CpuZ80State(int FormatVersion, Z80Registers Registers, Z80BusState Bus,
    Z80InterruptState Interrupts, CpuZ80Flow Flow, CpuZ80Operation Operation,
    bool Started, bool Faulted, bool PrefixEd, byte Opcode, byte RegisterCode,
    byte PairCode, byte Low, ushort Address, ushort Word, ushort InstructionAddress,
    long InstructionsCompleted, int InternalClocksRemaining, CpuZ80Index Index,
    Z80HardwareProfile HardwareProfile, bool InstructionFlagsModified, Z80ControlState Control);

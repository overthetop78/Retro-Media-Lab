namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;

/// <summary>Transactions du bus ; ne représente pas les familles d'instructions.</summary>
public enum Z80BusCycle
{
    OpcodeFetch,
    MemoryRead,
    MemoryWrite,
    IoRead,
    IoWrite,
    InterruptAcknowledge,
}

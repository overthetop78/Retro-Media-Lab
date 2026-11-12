using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;

namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Models;

/// <summary>Paramètres immuables d'un front, préparés depuis le profil temporel.
/// Aucun état mutable du CPU ni anticipation d'une horloge non reçue.</summary>
internal readonly struct Z80BusStep(ulong mask, ulong targets, uint outputs,
    long[] delays, Z80BusStepAction action)
{
    internal readonly ulong Mask = mask, Targets = targets;
    internal readonly uint Outputs = outputs;
    internal readonly long[] Delays = delays;
    internal readonly Z80BusStepAction Action = action;
}

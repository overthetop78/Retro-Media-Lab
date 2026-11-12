using System.Text.Json.Serialization;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;

namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;

/// <summary>Niveaux reçus, échantillonnages et continuation de l'unique séquenceur CPU.</summary>
public struct Z80ControlState
{
    [JsonInclude] public bool ResetRequested;
    [JsonInclude] public bool ResetSampled;
    [JsonInclude] public bool ResetAtOpcodeT2;
    [JsonInclude] public bool ResetHolding;
    [JsonInclude] public bool SpecialResetPending;
    [JsonInclude] public bool BusRequested;
    [JsonInclude] public bool BusGranted;
    [JsonInclude] public bool BusReleaseSampled;
    [JsonInclude] public bool ResumePending;
    [JsonInclude] public bool SampledBusRequest;
    [JsonInclude] public bool SampledInt;
    [JsonInclude] public bool SampledNmi;
    [JsonInclude] public bool Halted;
    [JsonInclude] public bool InterruptSequence;
    [JsonInclude] public bool? PendingHalt;
    [JsonInclude] public CpuZ80Continuation Deferred;
}

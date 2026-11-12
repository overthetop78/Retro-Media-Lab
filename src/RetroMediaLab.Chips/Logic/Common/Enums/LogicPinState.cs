namespace RetroMediaLab.Chips.Logic.Common.Enums;

/// <summary>Unknown includes an unresolved driver; HighImpedance is a known disabled driver.</summary>
public enum LogicPinState : byte
{
    Unknown,
    Low,
    High,
    HighImpedance
}

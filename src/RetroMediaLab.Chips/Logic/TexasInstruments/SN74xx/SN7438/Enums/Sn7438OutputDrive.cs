namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN7438.Enums;

/// <summary>Transistor drive, not the voltage of an externally resolved wire.</summary>
public enum Sn7438OutputDrive : byte
{
    Unknown = 0,
    Sinking = 2,
    Released = 3
}

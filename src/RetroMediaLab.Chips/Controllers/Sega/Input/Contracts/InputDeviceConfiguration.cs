namespace RetroMediaLab.Chips.Controllers.Sega.Input;

/// <summary>Resolved peripheral selection; cartridge metadata belongs to the board.</summary>
public sealed record InputDeviceConfiguration(byte AutomaticPadType = 2,
    byte PrimaryDevice = byte.MaxValue, bool AdditionalPads = false);

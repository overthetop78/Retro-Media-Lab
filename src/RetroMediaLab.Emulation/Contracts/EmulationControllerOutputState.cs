using RetroMediaLab.Emulation.Enums;

namespace RetroMediaLab.Emulation.Contracts;

public sealed record EmulationControllerOutputState(string DeviceId, EmulationControllerMotor Motor, float Strength);

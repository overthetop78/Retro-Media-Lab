using RetroMediaLab.Emulation.Enums;

namespace RetroMediaLab.Emulation.Contracts;

public sealed record EmulationControllerOutputBinding(string DeviceId, EmulationControllerMotor Motor);

using RetroMediaLab.Emulation.Acorn.Emulators.B2.Enums;

namespace RetroMediaLab.Emulation.Acorn.Emulators.B2.Contracts;

public sealed record B2ModelDefinition(B2Model Model, MachineModel MachineModel, string NativeValue);

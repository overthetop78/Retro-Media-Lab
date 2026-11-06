namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.ModelBPlus.Contracts;

internal sealed record ModelBPlusMemoryState(byte[] Shadow, byte[] Private, bool Paged, bool ShadowEnabled);

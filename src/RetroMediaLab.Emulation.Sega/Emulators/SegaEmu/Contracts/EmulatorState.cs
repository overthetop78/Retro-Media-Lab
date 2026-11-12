using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Contracts;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Contracts;

internal sealed record EmulatorState(int Version, Sg1000BoardState Board, long VideoSequence,
    long AudioSequence, long AudioFrames, AudioChunk[] Audio, AudioChunk? LatestAudio,
    PeripheralCategory[] Controllers);

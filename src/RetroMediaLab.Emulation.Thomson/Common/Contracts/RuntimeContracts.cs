using RetroMediaLab.Emulation.Contracts;
using RetroMediaLab.Emulation.Enums;
using RetroMediaLab.Emulation.Thomson.Common.Machines.Common.Enums;
using RetroMediaLab.Emulation;

namespace RetroMediaLab.Emulation.Thomson.Common.Contracts;

public sealed record RuntimeGeometry(int Width, int Height, int Pitch, float AspectRatio);
public sealed record RuntimeStatus(
    MachineModel Model,
    EmulationVideoStandard? Region,
    double FramesPerSecond,
    int SampleRate,
    RuntimeGeometry? Geometry,
    string CoreName,
    IReadOnlyDictionary<EmulationMediaSlot, bool> MediaActivity,
    IReadOnlyDictionary<int, bool> LedStates,
    int BufferedAudioFrames,
    long AudioOverrunCount,
    long AudioUnderrunCount,
    HostProcessState HostProcessState,
    int? HostProcessId
);
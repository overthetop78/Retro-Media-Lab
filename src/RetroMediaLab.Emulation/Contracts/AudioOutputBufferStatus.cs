namespace RetroMediaLab.Emulation.Contracts;

public readonly record struct AudioOutputBufferStatus(int SampleRate, int BufferedFrames, int CapacityFrames, bool IsActive);

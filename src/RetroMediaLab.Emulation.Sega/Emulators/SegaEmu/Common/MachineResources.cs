using System.IO;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Common;

/// <summary>Session-owned buffers and host services borrowed by one machine board.</summary>
internal sealed record MachineResources(SystemConfiguration Configuration, byte[] Framebuffer,
    int Width, int Height, int Pitch, int SampleRate, Func<string,Stream?> OpenFile,
    Func<int,Memory<byte>,int> LoadFirmware, CancellationToken Cancellation);

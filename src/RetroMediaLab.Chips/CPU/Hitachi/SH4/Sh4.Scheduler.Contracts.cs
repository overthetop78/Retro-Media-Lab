namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal delegate int Sh4SchedulerCallback(int tag, int scheduledCycles, int jitter, nint argument);

internal sealed partial class Sh4Scheduler
{
    internal const int MainClock = 200_000_000;
    internal const int TimeSlice = 448;

    internal Sh4SchedulerSaveSlots SaveSlots { get; set; }

    internal static uint CyclesForTransfer(uint size, uint unitsPerSecond)
        => (uint)((ulong)MainClock * size / unitsPerSecond);
}

internal readonly record struct Sh4SchedulerSaveSlots(
    int Aica, int AicaRtc, int GdRom, int Maple, int AicaDma, int RenderEnd, int VBlank);

using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;

namespace RetroMediaLab.Chips.EmuChip.Video.TexasInstruments.TMS99xx.TMS9918A.Services;

// Live registers of the one VDP engine. Immutable public state is constructed
// only for an observation, rather than read back through snapshot accessors.
internal struct Tms9918ClockRegisters(bool masterHigh, int halfEdgePhase,
    bool cpuClockHigh, bool pixelClockHigh, long halfEdges,
    int gromHalfEdgePhase = 0, bool gromClockHigh = false)
{
    internal bool MasterHigh = masterHigh, CpuClockHigh = cpuClockHigh,
        PixelClockHigh = pixelClockHigh, GromClockHigh = gromClockHigh;
    internal int HalfEdgePhase = halfEdgePhase, GromHalfEdgePhase = gromHalfEdgePhase;
    internal long HalfEdges = halfEdges;

    public static implicit operator Tms9918ClockRegisters(Tms9918ClockState s)
        => new(s.MasterHigh, s.HalfEdgePhase, s.CpuClockHigh, s.PixelClockHigh,
            s.HalfEdges, s.GromHalfEdgePhase, s.GromClockHigh);
    public static implicit operator Tms9918ClockState(Tms9918ClockRegisters s)
        => new(s.MasterHigh, s.HalfEdgePhase, s.CpuClockHigh, s.PixelClockHigh,
            s.HalfEdges, s.GromHalfEdgePhase, s.GromClockHigh);
}

internal struct Tms9918RasterRegisters(int dot, int line, long frame)
{
    internal int Dot = dot, Line = line;
    internal long Frame = frame;
    public static implicit operator Tms9918RasterRegisters(Tms9918RasterState s) => new(s.Dot, s.Line, s.Frame);
    public static implicit operator Tms9918RasterState(Tms9918RasterRegisters s) => new(s.Dot, s.Line, s.Frame);
}

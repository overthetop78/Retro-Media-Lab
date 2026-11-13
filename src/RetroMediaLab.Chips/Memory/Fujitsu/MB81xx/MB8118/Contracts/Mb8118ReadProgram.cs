using RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Models;

namespace RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Contracts;

/// <summary>Immutable electrical grid prepared once by a memory for recipients
/// using the same hardware timing. No cells or mutable chip state are shared.
/// The receiving memory still qualifies its own input boundary and data.</summary>
public readonly struct Mb8118ReadProgram
{
    internal readonly Mb8118Timing Profile;
    internal readonly long RowTime, RasFall, CasFall, RasRise, CasRise, ResponseTime, ReleaseTime;
    internal readonly byte Row, Column;
    internal readonly bool RowKnown, ColumnKnown, Predictable, ReadAddressKnown, CompleteReadTimingQualified;
    internal readonly int ReadAddress;

    internal Mb8118ReadProgram(Mb8118Timing profile, in Mb8118ReadCycle cycle,
        long responseTime, long releaseTime, bool predictable, bool readAddressKnown, bool completeReadTimingQualified)
    {
        Profile = profile;
        RowTime = cycle.RowTime; RasFall = cycle.RasFall; CasFall = cycle.CasFall;
        RasRise = cycle.RasRise; CasRise = cycle.CasRise;
        Row = cycle.Row; Column = cycle.Column;
        RowKnown = cycle.RowKnown; ColumnKnown = cycle.ColumnKnown;
        ResponseTime = responseTime; ReleaseTime = releaseTime;
        Predictable = predictable; ReadAddressKnown = readAddressKnown;
        CompleteReadTimingQualified = completeReadTimingQualified;
        ReadAddress = cycle.Row | (cycle.Column << 7);
    }
}

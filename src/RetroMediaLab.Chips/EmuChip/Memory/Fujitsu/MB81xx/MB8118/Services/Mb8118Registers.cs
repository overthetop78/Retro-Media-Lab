using RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Contracts;
using RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Models;

namespace RetroMediaLab.Chips.EmuChip.Memory.Fujitsu.MB81xx.MB8118.Services;

internal struct Mb8118InputLevels(byte address, bool addressKnown,
    bool rasHigh, bool casHigh, bool writeHigh, bool? data)
{
    internal byte Address = address;
    internal bool AddressKnown = addressKnown, RasHigh = rasHigh, CasHigh = casHigh, WriteHigh = writeHigh;
    internal bool? Data = data;
    public static implicit operator Mb8118InputLevels(Mb8118Pins p)
        => new(p.Address, p.AddressKnown, p.RasHigh, p.CasHigh, p.WriteHigh, p.Data);
    public static implicit operator Mb8118Pins(Mb8118InputLevels p)
        => new(p.Address, p.AddressKnown, p.RasHigh, p.CasHigh, p.WriteHigh, p.Data);
}

// A local immutable projection of the selected profile, not another profile.
internal readonly struct Mb8118LocalTiming
{
    private readonly Mb8118Timing _profile;
    internal readonly long RasAccess, CasAccess, OutputOff, Cycle, ReadWriteCycle, PageCycle,
        RasPrecharge, RasPulse, RasHold, CasPrecharge, PagePrecharge, CasPulse, CasHold,
        RasCasMinimum, ColumnFromRas, WriteHold, WriteHoldFromRas, WriteToRas, WriteToCas,
        CasToWrite, ReadHoldFromRas, MaximumOutputDelay;
    internal long[] Signature => _profile.Signature;
    internal Mb8118LocalTiming(Mb8118Timing p)
    {
        _profile = p;
        RasAccess = p.RasAccess; CasAccess = p.CasAccess; OutputOff = p.OutputOff;
        Cycle = p.Cycle; ReadWriteCycle = p.ReadWriteCycle; PageCycle = p.PageCycle;
        RasPrecharge = p.RasPrecharge; RasPulse = p.RasPulse; RasHold = p.RasHold;
        CasPrecharge = p.CasPrecharge; PagePrecharge = p.PagePrecharge;
        CasPulse = p.CasPulse; CasHold = p.CasHold; RasCasMinimum = p.RasCasMinimum;
        ColumnFromRas = p.ColumnFromRas; WriteHold = p.WriteHold;
        WriteHoldFromRas = p.WriteHoldFromRas; WriteToRas = p.WriteToRas;
        WriteToCas = p.WriteToCas; CasToWrite = p.CasToWrite;
        ReadHoldFromRas = p.ReadHoldFromRas;
        MaximumOutputDelay = Math.Max(RasAccess, OutputOff);
    }
}

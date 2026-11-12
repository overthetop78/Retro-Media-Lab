namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Constants;

/// <summary>Indices internes de propagation ; distincts des numéros de broche du boîtier.</summary>
internal static class Z80BusLines
{
    internal const int Address = 0, Data = 1, Memory = 2, Io = 3, Read = 4, Write = 5, M1 = 6, Refresh = 7;
    internal const int Halt = 8, BusAcknowledge = 9;
    internal const int Count = 10;
}

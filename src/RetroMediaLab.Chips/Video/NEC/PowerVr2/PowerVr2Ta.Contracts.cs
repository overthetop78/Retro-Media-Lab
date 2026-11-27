namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal static unsafe partial class PowerVr2Ta
{
    internal const uint SZ32 = 1;
    internal const uint SZ64 = 2;
}

internal sealed partial class TaTypeLut
{
    internal const uint InvalidType = uint.MaxValue;

    internal static TaTypeLut Instance { get; } = new();

    internal readonly uint[] Table = new uint[256];
}

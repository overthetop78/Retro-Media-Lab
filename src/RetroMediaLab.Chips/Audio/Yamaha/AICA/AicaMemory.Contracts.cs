using System.Numerics;

namespace RetroMediaLab.Chips.Audio.Yamaha.AICA;

internal static partial class AicaMemory
{
    internal const int RegisterSize = 0x8000;

    internal static partial T ReadRegisterInternal<T>(uint address) where T : unmanaged, IBinaryInteger<T>;
    internal static partial void WriteRegisterInternal<T>(uint address, T data) where T : unmanaged, IBinaryInteger<T>;
    internal static partial void InitMem();
    internal static partial void TermMem();
}

using System.Numerics;
using System.Runtime.CompilerServices;

namespace RetroMediaLab.Chips.Audio.Yamaha.AICA;

internal static unsafe partial class AicaArmMemory
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T ReadMemory<T>(uint address) where T : unmanaged, IBinaryInteger<T>
    {
        address &= 0x00FFFFFF;
        if (address < 0x800000)
        {
            T value = *(T*)&AicaInterface.Ram[address & (AicaInterface.RamMask - (uint)(sizeof(T) - 1))];

            if (sizeof(T) == sizeof(uint) && (address & 3) != 0)
            {
                int shift = (int)(address & 3) * 8;
                return (value >>> shift) | (value << (32 - shift));
            }
            else
            {
                return value;
            }
        }
        else
        {
            return ReadRegister<T>(address);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteMemory<T>(uint address, T data) where T : unmanaged, IBinaryInteger<T>
    {
        address &= 0x00FFFFFF;
        if (address < 0x800000)
        {
            *(T*)&AicaInterface.Ram[address & (AicaInterface.RamMask - (uint)(sizeof(T) - 1))] = data;
        }
        else
        {
            WriteRegister(address, data);
        }
    }
}

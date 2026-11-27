using System.Numerics;
using RetroMediaLab.Chips.CPU.ARM.ARMv3.ARM7;

namespace RetroMediaLab.Chips.Audio.Yamaha.AICA;

internal static partial class AicaArmMemory
{
    private const uint RegisterL = 0x2D00;
    private const uint RegisterM = 0x2D04;

    internal static bool AicaInterrupt;
    internal static uint AicaRegisterL;
    internal static bool E68kOutput;
    internal static uint E68kRegisterL;
    internal static uint E68kRegisterM;

    private static void UpdateE68k()
    {
        if (!E68kOutput && AicaInterrupt)
        {
            E68kOutput = true;
            E68kRegisterL = AicaRegisterL;
            Arm7Di.UpdateInterruptController();
        }
    }

    internal static void InterruptChange(uint bits, uint level)
    {
        AicaInterrupt = bits != 0;
        if (AicaInterrupt)
            AicaRegisterL = level;
        UpdateE68k();
    }

    private static void AcceptE68kInterrupt()
    {
        E68kOutput = false;
        UpdateE68k();
        Arm7Di.UpdateInterruptController();
    }

    internal static T ReadRegister<T>(uint address) where T : unmanaged, IBinaryInteger<T>
    {
        address &= 0x7FFF;
        if (address == RegisterL)
            return T.CreateTruncating(E68kRegisterL);
        else if (address == RegisterM)
            return T.CreateTruncating(E68kRegisterM);
        else if (System.Runtime.CompilerServices.Unsafe.SizeOf<T>() == sizeof(uint))
            return T.CreateTruncating(AicaMemory.ReadRegisterInternal<ushort>(address));
        else
            return AicaMemory.ReadRegisterInternal<T>(address);
    }

    internal static void WriteRegister<T>(uint address, T data) where T : unmanaged, IBinaryInteger<T>
    {
        address &= 0x7FFF;
        if (address == RegisterL)
        {
            return;
        }
        else if (address == RegisterM)
        {
            if ((data & T.One) != T.Zero)
                AcceptE68kInterrupt();
        }
        else
        {
            if (System.Runtime.CompilerServices.Unsafe.SizeOf<T>() == sizeof(uint))
                AicaMemory.WriteRegisterInternal(address, ushort.CreateTruncating(data));
            else
                AicaMemory.WriteRegisterInternal(address, data);
        }
    }
}

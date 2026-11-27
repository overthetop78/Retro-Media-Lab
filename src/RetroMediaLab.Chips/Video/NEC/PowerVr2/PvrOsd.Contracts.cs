using System;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal static unsafe partial class PvrOsd
{
    internal static bool CrosshairNeeded(int port)
    {
        if (port < 0 || port >= 4)
            return false;
        if (PvrConfiguration.CrosshairColor[port] == 0)
            return false;
        if (PvrConfiguration.IsArcade)
        {
            if (!PvrRuntime.IsLightgunGame || (port >= 2 && !PvrRuntime.IsFourPlayerGame))
                return false;
        }
        else if (PvrConfiguration.MapleMainDevices[port] != MapleDeviceType.LightGun)
        {
            return false;
        }
        return true;
    }

    internal static void BlankVmus()
    {
        Array.Clear(VmuLcdData);
        Array.Clear(VmuLastChanged);
    }
}

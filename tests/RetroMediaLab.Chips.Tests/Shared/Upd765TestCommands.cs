using C = RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Constants.Upd765Constants;
using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Enums;
using RetroMediaLab.Chips.Controllers.NEC.UPD765.UPD765.Services;

namespace RetroMediaLab.Chips.Tests.Shared;
internal static class Upd765TestCommands
{
    internal static void Specify(Upd765 device, bool pio) => Send(device, 3, 0xf1, pio ? (byte)3 : (byte)2);
    internal static void Send(Upd765 device, params byte[] bytes)
    {
        foreach (var value in bytes)
        {
            Wait(device, () => (device.ReadMainStatus() & 0xc0) == 0x80);
            device.WriteFifo(value);
            device.Advance(C.RegisterClocks);
        }
        Wait(device, () =>
        {
            var status = device.ReadMainStatus();
            return device.Active || status == 0 || (status & C.RequestForMaster) != 0;
        });
    }

    internal static byte[] Result(Upd765 device, int count = 7)
    {
        var bytes = new byte[count];
        for (var index = 0; index < count; index++)
        {
            Wait(device, () => (device.ReadMainStatus() & 0xf0) == 0xd0);
            bytes[index] = device.ReadFifo();
        }

        return bytes;
    }

    internal static void WaitByte(Upd765 device, bool pio, bool writing) => Wait(device, () => pio ? (device.ReadMainStatus() & 0xf0) == (writing ? 0xb0 : 0xf0) : device.Drq);
    internal static void Event(Upd765 device)
    {
        var ticks = device.NextEventTicks;
        Assert.InRange(ticks, 1, int.MaxValue);
        device.Advance((int)ticks);
    }

    internal static void Wait(Upd765 device, Func<bool> condition)
    {
        for (var attempt = 0; !condition() && attempt < 10000; attempt++)
            Event(device);
        Assert.True(condition());
    }
}

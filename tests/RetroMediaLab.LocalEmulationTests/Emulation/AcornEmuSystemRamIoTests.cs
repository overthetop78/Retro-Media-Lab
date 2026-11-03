using System.Reflection;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Enums;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuSystemRamIoTests
{
    [Fact]
    public void RepeatedSegmentWritesRefreshEverySelectedDigitThroughTheNative8154()
    {
        var board = new AcornSystemMachine(SystemCpuCard.Keypad6502, new byte[512], [], [], false);
        try
        {
            var keyboard = (SystemKeyboard)typeof(AcornSystemMachine)
                .GetField("_keyboard", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(board)!;
            board.Write(0x0e22, 0x0f);
            board.Write(0x0e23, 0xff);
            keyboard.BeginDisplayFrame();
            for (byte digit = 0; digit < 8; digit++)
            {
                board.Write(0x0e20, digit);
                board.Write(0x0e21, 0x3f);
            }
            Assert.Equal(Enumerable.Repeat((byte)0x3f, 8), keyboard.DisplaySegments.ToArray());
        }
        finally { board.Reset(true); }
    }
}

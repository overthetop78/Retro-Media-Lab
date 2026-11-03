using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;
using C = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Constants.Via6522Constants;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuCrtcWiringTests
{
    private const ushort CrtcAddress = 0xfe00;
    private const ushort CrtcData = 0xfe01;
    private const ushort UlaControl = 0xfe20;
    private const ushort SystemVia = 0xfe40;
    private const int CharacterPeriod = 32;
    private const int SyncLines = 3;
    private const int TraceClocks = 4096;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SystemViaSeesBothRealVsyncEdgesAndProgrammedWidth(bool rising)
    {
        var board = new BbcMicroMachine(new byte[16384], new byte[16384], 32768, expandedIo: true);
        try
        {
            Set(board, 0, CharacterPeriod - 1); Set(board, 1, 8); Set(board, 2, 12);
            Set(board, 3, (SyncLines << 4) | 3); Set(board, 4, 31);
            Set(board, 5, 0); Set(board, 6, 2); Set(board, 7, 4); Set(board, 8, 0); Set(board, 9, 0);
            board.Write(UlaControl, 16); // A character clock for each 2 MHz board clock.
            board.Write(SystemVia + C.Peripheral, rising ? C.Ca1Rising : (byte)0);
            board.Write(SystemVia + C.Flags, C.Ca1Interrupt);
            var previous = board.Video.VSync;
            var pulseStart = -1;
            var pulses = 0;
            var interrupts = 0;
            for (var clock = 0; clock < TraceClocks; clock++)
            {
                board.Tick();
                var current = board.Video.VSync;
                var edge = current != previous;
                var expectedInterrupt = edge && current == rising;
                Assert.Equal(expectedInterrupt, (board.Read(SystemVia + C.Flags) & C.Ca1Interrupt) != 0);
                if (edge)
                {
                    if (current) pulseStart = clock;
                    else if (pulseStart >= 0)
                    {
                        Assert.Equal(CharacterPeriod * SyncLines, clock - pulseStart);
                        pulses++;
                    }
                }
                if (expectedInterrupt)
                {
                    interrupts++;
                    board.Write(SystemVia + C.Flags, C.Ca1Interrupt);
                }
                previous = current;
            }
            Assert.True(pulses >= 3);
            Assert.InRange(interrupts, pulses, pulses + 1);
        }
        finally { board.Reset(true); }
    }

    private static void Set(BbcMicroMachine board, byte register, int value)
    {
        board.Write(CrtcAddress, register); board.Write(CrtcData, (byte)value);
    }
}

using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.BbcMicro.Common.Services;
using C = RetroMediaLab.Chips.IO.MOS.MCS6500.Chip6522.Constants.Via6522Constants;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuViaClockTests
{
    [Theory]
    [InlineData(0xfe40, C.Timer1Low, C.Timer1Interrupt)]
    [InlineData(0xfe60, C.Timer1Low, C.Timer1Interrupt)]
    [InlineData(0xfe40, C.Timer2Low, C.Timer2Interrupt)]
    [InlineData(0xfe60, C.Timer2Low, C.Timer2Interrupt)]
    public void BothViasPublishTheirTimerTimeoutAtTheNextOneMhzLeadingEdge(int start, int counter, byte flag)
    {
        var board = new BbcMicroMachine(new byte[16384], new byte[16384], 32768, expandedIo: true);
        try
        {
            var tick = board.Cpu.Cycles;
            board.Write((ushort)(start + C.Enable), (byte)(C.InterruptSummary | flag));
            board.Write((ushort)(start + counter), 0);
            // CPU cycles and board cycles start together. Align the bus write with a falling 1 MHz edge.
            if ((tick & 1) == 0) { board.Tick(); board.Read(0); tick++; }
            board.Tick();
            board.Write((ushort)(start + counter + 1), 0);
            Assert.Equal(0, board.Read((ushort)(start + C.Flags)) & flag);

            board.Tick(); // Rising edge +0.5 us: no timeout yet.
            board.Read(0);
            Assert.Equal(0, board.Read((ushort)(start + C.Flags)) & flag);
            board.Tick(); // Falling edge +1 us: counter underflows after the bus access.
            Assert.Equal(0, board.Read((ushort)(start + C.Flags)) & flag);
            Assert.Equal(0, board.Read((ushort)(start + C.Flags)) & flag);
            board.Tick(); // Rising edge +1.5 us: IRQ is now visible.
            board.Read(0);
            Assert.Equal(flag, board.Read((ushort)(start + C.Flags)) & flag);
            Assert.True(board.Irq);
        }
        finally { board.Reset(true); }
    }
}

using System.Buffers.Binary;
using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Services;

namespace RetroMediaLab.Tests.Emulation;

public sealed class AcornEmuArchimedesSerialTests
{
    private const uint SerialData = 0x03230000;
    private const uint SerialStatus = SerialData + 4;
    private const uint SerialCommand = SerialData + 8;
    private const uint SerialControl = SerialData + 12;
    private const uint IocIrqStatusB = 0x03200020;
    private const uint IocIrqMaskB = 0x03200028;
    private const uint SerialIrq = 4;
    private const int IocByteShift = 16;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SerialInterruptReachesIocDespiteDisconnectedCtsAndIsAcknowledgedByStatus(bool cold)
    {
        var rom = new byte[256];
        for (var offset = 0; offset < rom.Length; offset += 4)
            BinaryPrimitives.WriteUInt32LittleEndian(rom.AsSpan(offset), 0xeafffffe);
        var machine = new ArchimedesMachine(rom, 1 << 20);
        try
        {
            machine.Reset(cold);
            Write(machine, IocIrqMaskB, SerialIrq << IocByteShift);
            Write(machine, SerialControl, 0x1f);
            Write(machine, SerialCommand, 5);
            machine.RunFrame();
            Assert.Equal(SerialIrq, Read(machine, IocIrqStatusB) & SerialIrq);
            Assert.True(machine.Irq);
            Assert.Equal(0xe0u, Read(machine, SerialStatus) & 0xf0); // CTS masks TDRE, not the periodic TX IRQ.
            Assert.Equal(0u, Read(machine, IocIrqStatusB) & SerialIrq);
            Assert.False(machine.Irq);
            machine.RunFrame();
            Assert.True(machine.Irq);
            Write(machine, SerialCommand, 1);
            Read(machine, SerialStatus);
            machine.RunFrame();
            Assert.False(machine.Irq);
            Write(machine, SerialStatus, 0);
            Assert.Equal(0u, Read(machine, SerialCommand)); // Rockwell reset, not the MOS 02 variant.
        }
        finally { machine.Reset(true); machine.DrainAudio(); }
    }

    private static uint Read(ArchimedesMachine machine, uint address)
    {
        Assert.True(machine.TryReadWord(address, false, false, false, out var value));
        return value;
    }

    private static void Write(ArchimedesMachine machine, uint address, uint value) =>
        Assert.True(machine.TryWriteWord(address, value, false, false));
}

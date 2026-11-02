using S = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Intel80186.Constants.ChipSelect80186Constants;
using R = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Intel80186.Constants.Timer80186Constants;
using I = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Intel80186.Constants.Interrupt80186Constants;
using P = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Intel80186.Constants.Peripheral80186Constants;
using T = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Intel80186.Constants.Cpu80186Timings;
using static RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Intel80186.Constants.Cpu80186Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Intel80186.Services;

internal sealed partial class Cpu80186
{
    private ushort _peripheralRelocation = P.ResetRelocation;

    private bool IsPeripheralAddress(int address, bool memory) =>
        ((_peripheralRelocation & P.MemoryMapped) != 0) == memory &&
        (address >> P.PageShift) == (_peripheralRelocation & (memory ? P.MemoryPageMask : P.IoPageMask));

    private ushort ReadPeripheral(int address)
    {
        var offset = address & P.RegisterMask;
        if (offset == P.RelocationRegister) return _peripheralRelocation;
        if (offset is >= S.UpperRegister and <= S.MiddleSizeRegister) return ReadChipSelectRegister(offset);
        if (offset is >= I.Vector and <= I.LastControl) return ReadInterruptRegister(offset);
        if (offset is >= R.RegisterBase and <= R.LastRegister) return ReadTimerRegister(offset);
        if (DecodeDmaRegister(offset, out var channel, out var register)) return ReadDmaRegister(channel, register);
        return 0;
    }

    private void WritePeripheral(int address, ushort value)
    {
        var offset = address & P.RegisterMask;
        if (offset == P.RelocationRegister) _peripheralRelocation = value;
        else if (offset is >= S.UpperRegister and <= S.MiddleSizeRegister) WriteChipSelectRegister(offset, value);
        else if (offset is >= I.Vector and <= I.LastControl) WriteInterruptRegister(offset, value);
        else if (offset is >= R.RegisterBase and <= R.LastRegister) WriteTimerRegister(offset, value);
        else if (DecodeDmaRegister(offset, out var channel, out var register)) WriteDmaRegister(channel, register, value);
    }

    private static ushort SwapPeripheralWord(ushort value) => (ushort)(value << P.ByteShift | value >> P.ByteShift);
    private static int PeripheralByteShift(int address) => (address & 1) * P.ByteShift;
    private ushort ReadPeripheralWord(int address)
    {
        var value = ReadPeripheral(address);
        return (address & 1) == 0 ? value : SwapPeripheralWord(value);
    }
    private void WritePeripheralWord(int address, ushort value) =>
        WritePeripheral(address, (address & 1) == 0 ? value : SwapPeripheralWord(value));

    private ushort ReadInternalBus(int address, bool memory, bool word)
    {
        try
        {
            BeginExternalBus(address, memory, waitStates: false);
            return word ? ReadPeripheralWord(address) : (byte)(ReadPeripheral(address) >> PeripheralByteShift(address));
        }
        finally { EndExternalBus(); }
    }

    private void WriteInternalBus(int address, bool memory, bool word, ushort value)
    {
        try
        {
            BeginExternalBus(address, memory, waitStates: false);
            if (word) WritePeripheralWord(address, value);
            else WritePeripheral(address, (ushort)(value << PeripheralByteShift(address)));
        }
        finally { EndExternalBus(); }
    }

    private byte ReadExternalByte(int address, bool memory)
    {
        try
        {
            BeginExternalBus(address, memory);
            return memory ? read(address) : input((ushort)address);
        }
        finally { EndExternalBus(); }
    }

    private ushort ReadExternalWord(int address, bool memory)
    {
        try
        {
            BeginExternalBus(address, memory);
            return memory ? (ushort)(read(address) | read((address + 1) & AddressMask) << P.ByteShift)
                : (ushort)(input((ushort)address) | input((ushort)(address + 1)) << P.ByteShift);
        }
        finally { EndExternalBus(); }
    }

    private void WriteExternalByte(int address, bool memory, byte value)
    {
        try
        {
            BeginExternalBus(address, memory);
            if (memory) write(address, value); else output((ushort)address, value);
        }
        finally { EndExternalBus(); }
    }

    private void WriteExternalWord(int address, bool memory, ushort value)
    {
        try
        {
            BeginExternalBus(address, memory);
            if (memory)
            {
                write(address, (byte)value);
                write((address + 1) & AddressMask, (byte)(value >> P.ByteShift));
            }
            else
            {
                output((ushort)address, (byte)value);
                output((ushort)(address + 1), (byte)(value >> P.ByteShift));
            }
        }
        finally { EndExternalBus(); }
    }

    private byte ReadMemoryByte(int address) => IsPeripheralAddress(address, true)
        ? (byte)ReadInternalBus(address, true, false) : ReadExternalByte(address, true);

    private ushort ReadMemoryWord(int address)
    {
        if (IsPeripheralAddress(address, true)) return ReadInternalBus(address, true, true);
        if ((address & 1) == 0) return ReadExternalWord(address, true);
        _busPenalty += T.UnalignedWord;
        return (ushort)(ReadMemoryByte(address) | ReadMemoryByte((address + 1) & AddressMask) << P.ByteShift);
    }

    private void WriteMemoryByte(int address, byte value)
    {
        if (IsPeripheralAddress(address, true)) WriteInternalBus(address, true, false, value);
        else WriteExternalByte(address, true, value);
    }

    private void WriteMemoryWord(int address, ushort value)
    {
        if (IsPeripheralAddress(address, true)) { WriteInternalBus(address, true, true, value); return; }
        if ((address & 1) == 0) { WriteExternalWord(address, true, value); return; }
        _busPenalty += T.UnalignedWord;
        WriteMemoryByte(address, (byte)value);
        WriteMemoryByte((address + 1) & AddressMask, (byte)(value >> P.ByteShift));
    }

    internal byte ReadIoByte(ushort port) => IsPeripheralAddress(port, false)
        ? (byte)ReadInternalBus(port, false, false) : ReadExternalByte(port, false);

    internal ushort ReadIoWord(ushort port)
    {
        if (IsPeripheralAddress(port, false)) return ReadInternalBus(port, false, true);
        if ((port & 1) == 0) return ReadExternalWord(port, false);
        _busPenalty += T.UnalignedWord;
        return (ushort)(ReadIoByte(port) | ReadIoByte((ushort)(port + 1)) << P.ByteShift);
    }

    internal void WriteIoByte(ushort port, byte value)
    {
        if (IsPeripheralAddress(port, false)) WriteInternalBus(port, false, false, value);
        else WriteExternalByte(port, false, value);
    }

    private void WriteIoAccumulatorByte(ushort port)
    {
        // OUT AL présente aussi AH sur le bus interne 16 bits du 80186.
        if (IsPeripheralAddress(port, false)) WriteInternalBus(port, false, true, _registers[Ax]);
        else WriteExternalByte(port, false, (byte)_registers[Ax]);
    }

    internal void WriteIoWord(ushort port, ushort value)
    {
        if (IsPeripheralAddress(port, false)) { WriteInternalBus(port, false, true, value); return; }
        if ((port & 1) == 0) { WriteExternalWord(port, false, value); return; }
        _busPenalty += T.UnalignedWord;
        WriteIoByte(port, (byte)value);
        WriteIoByte((ushort)(port + 1), (byte)(value >> P.ByteShift));
    }

    private int EscapeInstruction()
    {
        if ((_peripheralRelocation & P.EscapeTrap) != 0)
        {
            Interrupt(P.EscapeVector, fault: true);
            return 0;
        }
        var operand = Decode(Fetch());
        if (!operand.Register) ReadWord(operand.Segment, operand.Offset);
        return T.Escape;
    }
}

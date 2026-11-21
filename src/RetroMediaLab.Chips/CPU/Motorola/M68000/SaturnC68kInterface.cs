// Copyright 2007 Guillaume Duhamel
// Translated from Yabause m68kc68k.c.
// SPDX-License-Identifier: GPL-2.0-or-later

using System.IO;

namespace RetroMediaLab.Chips.CPU.Motorola.M68000;

internal sealed class SaturnC68kInterface
{
    private SaturnC68k? _core;
    private SaturnC68k Core => _core!;

    internal int Initialize()
    {
        var core = new SaturnC68k();
        core.Initialize(interruptCallback: null);
        _core = core;
        return 0;
    }

    internal void Deinitialize() => _core = null;

    internal void Reset() => Core.Reset();

    internal int Execute(int cycles) => Core.Execute(cycles);

    internal void Sync()
    {
    }

    internal uint GetDataRegister(uint number) => Core.GetDataRegister(number);
    internal uint GetAddressRegister(uint number) => Core.GetAddressRegister(number);
    internal uint GetProgramCounter() => Core.GetProgramCounter();
    internal uint GetStatusRegister() => Core.GetStatusRegister();
    internal uint GetUserStackPointer() => Core.GetUserStackPointer();
    internal uint GetSupervisorStackPointer() => Core.GetSupervisorStackPointer();

    internal void SetDataRegister(uint number, uint value) => Core.SetDataRegister(number, value);
    internal void SetAddressRegister(uint number, uint value) => Core.SetAddressRegister(number, value);
    internal void SetProgramCounter(uint value) => Core.SetProgramCounter(value);
    internal void SetStatusRegister(uint value) => Core.SetStatusRegister(value);
    internal void SetUserStackPointer(uint value) => Core.SetUserStackPointer(value);
    internal void SetSupervisorStackPointer(uint value) => Core.SetSupervisorStackPointer(value);

    internal void SetFetch(uint lowAddress, uint highAddress, byte[] buffer)
        => Core.SetFetch(lowAddress, highAddress, buffer);

    internal void SetInterruptLevel(int level) => Core.SetInterruptLevel(level);

    internal void WriteNotify(uint address, uint size)
    {
    }

    internal void SetReadByte(Func<uint, uint> read) => Core.SetReadByte(read);
    internal void SetReadWord(Func<uint, uint> read) => Core.SetReadWord(read);
    internal void SetWriteByte(Action<uint, uint> write) => Core.SetWriteByte(write);
    internal void SetWriteWord(Action<uint, uint> write) => Core.SetWriteWord(write);

    internal void SaveState(BinaryWriter writer) => Core.SaveState(writer);
    internal void LoadState(BinaryReader reader) => Core.LoadState(reader);
}

internal sealed partial class SaturnC68k
{
    internal void SaveState(BinaryWriter writer)
    {
        for (int i = 0; i < 8; i++)
            writer.Write(_data[i]);
        for (int i = 0; i < 8; i++)
            writer.Write(_address[i]);

        writer.Write(_carry);
        writer.Write(_overflow);
        writer.Write(_notZero);
        writer.Write(_negative);
        writer.Write(_extend);
        writer.Write(_interruptMask);
        writer.Write(_supervisor);
        writer.Write(_usp);
        writer.Write(GetProgramCounter());
        writer.Write(_status);
        writer.Write(_irqLine);
        writer.Write(_cycleToDo);
        writer.Write(_cycleIo);
        writer.Write(_cycleSup);
        writer.Write(_dirty1);
    }

    internal void LoadState(BinaryReader reader)
    {
        for (int i = 0; i < 8; i++)
            _data[i] = reader.ReadUInt32();
        for (int i = 0; i < 8; i++)
            _address[i] = reader.ReadUInt32();

        _carry = reader.ReadUInt32();
        _overflow = reader.ReadUInt32();
        _notZero = reader.ReadUInt32();
        _negative = reader.ReadUInt32();
        _extend = reader.ReadUInt32();
        _interruptMask = reader.ReadUInt32();
        _supervisor = reader.ReadUInt32();
        _usp = reader.ReadUInt32();
        SetProgramCounter(reader.ReadUInt32());
        _status = reader.ReadUInt32();
        _irqLine = reader.ReadInt32();
        _cycleToDo = reader.ReadInt32();
        _cycleIo = reader.ReadInt32();
        _cycleSup = reader.ReadInt32();
        _dirty1 = reader.ReadUInt32();
    }
}

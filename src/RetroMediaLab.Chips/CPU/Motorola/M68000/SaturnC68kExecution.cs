// Copyright 2003-2004 Stephane Dallongeville
// Translated from Yabause c68k/c68kexec.c.
// SPDX-License-Identifier: GPL-2.0-or-later

namespace RetroMediaLab.Chips.CPU.Motorola.M68000;

internal sealed partial class SaturnC68k
{
    // Exception cycles from Musashi, as used by C68K.
    private static readonly int[] ExceptionCycles =
    [
        4, 4, 50, 50, 34, 38, 40, 34, 34, 34, 4, 4, 4, 4, 4, 44,
        4, 4, 4, 4, 4, 4, 4, 4, 44, 44, 44, 44, 44, 44, 44, 44,
        34, 34, 34, 34, 34, 34, 34, 34, 34, 34, 34, 34, 34, 34, 34, 34,
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
    ];

    internal enum OpcodeExit
    {
        Next,
        End,
        ReallyEnd,
    }

    internal delegate OpcodeExit OpcodeHandler(SaturnC68k cpu, ushort opcode, ref uint pc, ref int cycles);

    internal int Execute(int cycles)
    {
        uint pc = _pc;
        if ((_status & (Running | Disabled | Faulted)) != 0)
        {
            if (!JumpTable.IsValueCreated)
            {
                _ = JumpTable.Value;
                return 0;
            }

            return unchecked((int)(_status | 0x80000000u));
        }

        if (cycles <= 0)
            return -cycles;

        _cycleToDo = cycles;
        try
        {
            CheckInterrupt(ref pc, ref cycles, ExceptionCycles);
            if ((_status & (Halted | Waiting)) != 0)
                return _cycleToDo;

            _cycleSup = 0;
            _status |= Running;

            OpcodeHandler[] jumpTable = JumpTable.Value;
            while (true)
            {
                ushort opcode = Next(ref pc, cycles);
                OpcodeExit exit = jumpTable[opcode](this, opcode, ref pc, ref cycles);
                if (exit == OpcodeExit.Next)
                    continue;
                if (exit == OpcodeExit.ReallyEnd)
                    break;

                CheckInterrupt(ref pc, ref cycles, ExceptionCycles);
                cycles += _cycleSup;
                if (cycles <= 0)
                    break;

                _cycleSup = 0;
            }
            return _cycleToDo - cycles;
        }
        finally
        {
            _status &= ~Running;
            _pc = pc;
        }
    }
}

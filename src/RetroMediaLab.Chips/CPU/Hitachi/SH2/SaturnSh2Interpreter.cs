// Copyright 2003-2005 Guillaume Duhamel
// Copyright 2004-2007, 2013 Theo Berkau
// Copyright 2005 Fabien Coulon
// Translated from Yabause sh2int.c.
// SPDX-License-Identifier: GPL-2.0-or-later

using RetroMediaLab.Chips.Controllers.Sega;
using static RetroMediaLab.Chips.Controllers.Sega.SaturnMemoryAccess;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH2;

internal static partial class SaturnSh2Interpreter
{
    private delegate void Opcode(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory);
    private delegate ushort Fetch(SaturnMemory.Sh2MemoryMap memory, uint address);

    private static readonly Opcode[] Sh2Opcodes = InitializeOpcodes(false);
    private static readonly Fetch[] Sh2FetchList = InitializeFetchList(false);

    private static class Sh1Tables
    {
        internal static readonly Opcode[] Opcodes = InitializeOpcodes(true);
        internal static readonly Fetch[] FetchList = InitializeFetchList(true);
    }

    private static Opcode[] InitializeOpcodes(bool isSh1)
    {
        var opcodes = new Opcode[0x10000];
        for (int i = 0; i < opcodes.Length; i++)
            opcodes[i] = Decode(isSh1, (ushort)i);
        return opcodes;
    }

    private static Fetch[] InitializeFetchList(bool isSh1)
    {
        var fetchList = new Fetch[0x100];
        for (int i = 0; i < fetchList.Length; i++)
        {
            if (isSh1)
            {
                fetchList[i] = i switch
                {
                    0x00 => FetchSh1Rom,
                    0x90 => FetchSh1Dram,
                    0xF0 => FetchSh1MpegRom,
                    _ => FetchInvalid
                };
            }
            else
            {
                fetchList[i] = i switch
                {
                    0x00 => FetchBios,
                    0x02 => FetchLowWram,
                    0x20 => FetchCs0,
                    0x5C => FetchVram,
                    >= 0x60 and <= 0x6F => FetchHighWram,
                    _ => FetchInvalid
                };
            }
        }

        return fetchList;
    }

    private static ushort FetchSh1Rom(SaturnMemory.Sh2MemoryMap memory, uint address) =>
        T2ReadWord(memory.Sh1!.Memory.Sh1Rom, address & 0xFFFF);

    private static ushort FetchSh1Dram(SaturnMemory.Sh2MemoryMap memory, uint address) =>
        T2ReadWord(memory.Sh1!.Memory.Sh1Dram, address & 0x7FFFF);

    private static ushort FetchSh1MpegRom(SaturnMemory.Sh2MemoryMap memory, uint address) =>
        T2ReadWord(memory.Sh1!.Memory.Sh1MpegRom, address & 0x7FFF);

    private static ushort FetchBios(SaturnMemory.Sh2MemoryMap memory, uint address) =>
        memory.CacheEmulationEnabled ? memory.Read16(address) : memory.InstructionMemory.BiosRomMemoryReadWord(address);

    private static ushort FetchCs0(SaturnMemory.Sh2MemoryMap memory, uint address) =>
        memory.CacheEmulationEnabled ? memory.Read16(address) : memory.ReadCartridgeInstruction(address);

    private static ushort FetchLowWram(SaturnMemory.Sh2MemoryMap memory, uint address) =>
        memory.CacheEmulationEnabled ? memory.Read16(address) : memory.InstructionMemory.LowWramMemoryReadWord(address);

    private static ushort FetchHighWram(SaturnMemory.Sh2MemoryMap memory, uint address) =>
        memory.CacheEmulationEnabled ? memory.Read16(address) : memory.InstructionMemory.HighWramMemoryReadWord(address);

    private static ushort FetchVram(SaturnMemory.Sh2MemoryMap memory, uint address) =>
        memory.InstructionVdp1.RamReadWord(address);

    private static ushort FetchInvalid(SaturnMemory.Sh2MemoryMap memory, uint address) => 0xFFFF;

    private static ushort FetchInstruction(SaturnMemory.Sh2MemoryMap memory, uint address, Fetch[] fetchList)
    {
        if (memory.CacheEmulationEnabled && (address & 0xC0000000) == 0xC0000000)
            return memory.ReadDataArrayInstruction(address);

        return fetchList[(address >> 20) & 0xFF](memory, address);
    }

    private static void Delay(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory, uint address)
    {
        if (memory.Sh1 is not null)
        {
            state.Instruction = FetchInstruction(memory, address, Sh1Tables.FetchList);
            Sh1Tables.Opcodes[state.Instruction](state, memory);
        }
        else
        {
            state.Instruction = FetchInstruction(memory, address, Sh2FetchList);
            Sh2Opcodes[state.Instruction](state, memory);
        }
        state.PC = unchecked(state.PC - 2);
    }

    internal static bool CheckWait(uint address, int size, Func<uint, int, bool> checkABusWait)
    {
        if ((address & 0x0FF00000) == 0x05800000)
        {
            if (checkABusWait(address, size))
                return true;
        }

        return false;
    }

    private static void Add(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            state.R[((state.Instruction & 0x0F00) >> 8)] += state.R[((state.Instruction & 0x00F0) >> 4)];
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Addi(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int cd = (int)(sbyte)(state.Instruction & 0x00FF);
            int b = (state.Instruction & 0x0F00) >> 8;

            state.R[b] += (uint)cd;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Addc(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint tmp0, tmp1;
            int source = (state.Instruction & 0x00F0) >> 4;
            int dest = (state.Instruction & 0x0F00) >> 8;

            tmp1 = state.R[source] + state.R[dest];
            tmp0 = state.R[dest];

            state.R[dest] = tmp1 + state.SR.T;
            if (tmp0 > tmp1)
                state.SR.T = 1;
            else
                state.SR.T = 0;
            if (tmp1 > state.R[dest])
                state.SR.T = 1;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Addv(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int dest, src, ans;
            int n = (state.Instruction & 0x0F00) >> 8;
            int m = (state.Instruction & 0x00F0) >> 4;

            if ((int) state.R[n] >= 0)
                dest = 0;
            else
                dest = 1;

            if ((int) state.R[m] >= 0)
                src = 0;
            else
                src = 1;

            src += dest;
            state.R[n] += state.R[m];

            if ((int) state.R[n] >= 0)
                ans = 0;
            else
                ans = 1;

            ans += dest;

            if (src == 0 || src == 2)
                if (ans == 1)
                    state.SR.T = 1;
                else
                    state.SR.T = 0;
            else
                state.SR.T = 0;

            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void And(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            state.R[((state.Instruction & 0x0F00) >> 8)] &= state.R[((state.Instruction & 0x00F0) >> 4)];
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Andi(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            state.R[0] &= (uint)(state.Instruction & 0x00FF);
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Bf(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            if (state.SR.T == 0)
            {
                int disp = (int)(sbyte)state.Instruction;

                state.PC = state.PC + (uint)(disp << 1) + 4;
                state.Cycles += 3;
            }
            else
            {
                state.PC += 2;
                state.Cycles++;
            }
        }
    }

    private static void Bt(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            if (state.SR.T == 1)
            {
                int disp = (int)(sbyte)state.Instruction;

                state.PC = state.PC + (uint)(disp << 1) + 4;
                state.Cycles += 3;
            }
            else
            {
                state.PC += 2;
                state.Cycles++;
            }
        }
    }

    private static void Clrmac(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            state.MACH = 0;
            state.MACL = 0;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Clrt(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            state.SR.T = 0;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Cmpeq(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            if (state.R[((state.Instruction & 0x0F00) >> 8)] == state.R[((state.Instruction & 0x00F0) >> 4)])
                state.SR.T = 1;
            else
                state.SR.T = 0;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Cmpge(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            if ((int)state.R[((state.Instruction & 0x0F00) >> 8)] >=
                (int)state.R[((state.Instruction & 0x00F0) >> 4)])
                state.SR.T = 1;
            else
                state.SR.T = 0;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Cmpgt(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            if ((int)state.R[((state.Instruction & 0x0F00) >> 8)]>(int)state.R[((state.Instruction & 0x00F0) >> 4)])
                state.SR.T = 1;
            else
                state.SR.T = 0;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Cmphi(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            if ((uint)state.R[((state.Instruction & 0x0F00) >> 8)] >
                (uint)state.R[((state.Instruction & 0x00F0) >> 4)])
                state.SR.T = 1;
            else
                state.SR.T = 0;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Cmphs(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            if ((uint)state.R[((state.Instruction & 0x0F00) >> 8)] >=
                (uint)state.R[((state.Instruction & 0x00F0) >> 4)])
                state.SR.T = 1;
            else
                state.SR.T = 0;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Cmpim(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int imm;
            int i = (state.Instruction & 0x00FF);

            imm = (int)(sbyte)i;

            if (state.R[0] == (uint) imm)
                state.SR.T = 1;
            else
                state.SR.T = 0;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Cmppl(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            if ((int)state.R[((state.Instruction & 0x0F00) >> 8)]>0)
                state.SR.T = 1;
            else
                state.SR.T = 0;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Cmppz(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            if ((int)state.R[((state.Instruction & 0x0F00) >> 8)] >= 0)
                state.SR.T = 1;
            else
                state.SR.T = 0;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Cmpstr(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint temp;
            uint HH, HL, LH, LL;
            int m = (state.Instruction & 0x00F0) >> 4;
            int n = (state.Instruction & 0x0F00) >> 8;
            temp = state.R[n] ^ state.R[m];
            HH = (temp >> 24) & 0x000000FF;
            HL = (temp >> 16) & 0x000000FF;
            LH = (temp >> 8) & 0x000000FF;
            LL = temp & 0x000000FF;
            HH = HH != 0 && HL != 0 && LH != 0 && LL != 0 ? 1u : 0u;
            if (HH == 0)
                state.SR.T = 1;
            else
                state.SR.T = 0;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Div0s(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = (state.Instruction & 0x00F0) >> 4;
            int n = (state.Instruction & 0x0F00) >> 8;
            if ((state.R[n] & 0x80000000) == 0)
                state.SR.Q = 0;
            else
            state.SR.Q = 1;
            if ((state.R[m] & 0x80000000) == 0)
                state.SR.M = 0;
            else
            state.SR.M = 1;
            state.SR.T = state.SR.M != state.SR.Q ? 1u : 0u;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Div0u(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            state.SR.M = state.SR.Q = state.SR.T = 0;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Div1(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint tmp0;
            byte oldQ, tmp1;
            int m = (state.Instruction & 0x00F0) >> 4;
            int n = (state.Instruction & 0x0F00) >> 8;

            oldQ = (byte)state.SR.Q;
            state.SR.Q = (0x80000000 & state.R[n]) != 0 ? 1u : 0u;
            state.R[n] <<= 1;
            state.R[n] |= (uint) state.SR.T;

            switch (oldQ)
            {
                case 0:
                    switch (state.SR.M)
                    {
                        case 0:
                            tmp0 = state.R[n];
                            state.R[n] -= state.R[m];
                            tmp1 = state.R[n] > tmp0 ? (byte)1 : (byte)0;
                            switch (state.SR.Q)
                            {
                                case 0:
                                    state.SR.Q = tmp1;
                                    break;
                                case 1:
                                    state.SR.Q = tmp1 == 0 ? 1u : 0u;
                                    break;
                            }
                            break;
                        case 1:
                            tmp0 = state.R[n];
                            state.R[n] += state.R[m];
                            tmp1 = state.R[n] < tmp0 ? (byte)1 : (byte)0;
                            switch (state.SR.Q)
                            {
                                case 0:
                                    state.SR.Q = tmp1 == 0 ? 1u : 0u;
                                    break;
                                case 1:
                                    state.SR.Q = tmp1;
                                    break;
                            }
                            break;
                    }
                    break;
                case 1:
                    switch (state.SR.M)
                    {
                        case 0:
                            tmp0 = state.R[n];
                            state.R[n] += state.R[m];
                            tmp1 = state.R[n] < tmp0 ? (byte)1 : (byte)0;
                            switch (state.SR.Q)
                            {
                                case 0:
                                    state.SR.Q = tmp1;
                                    break;
                                case 1:
                                    state.SR.Q = tmp1 == 0 ? 1u : 0u;
                                    break;
                            }
                            break;
                        case 1:
                            tmp0 = state.R[n];
                            state.R[n] -= state.R[m];
                            tmp1 = state.R[n] > tmp0 ? (byte)1 : (byte)0;
                            switch (state.SR.Q)
                            {
                                case 0:
                                    state.SR.Q = tmp1 == 0 ? 1u : 0u;
                                    break;
                                case 1:
                                    state.SR.Q = tmp1;
                                    break;
                            }
                            break;
                    }
                    break;
            }
            state.SR.T = state.SR.Q == state.SR.M ? 1u : 0u;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Dmuls(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint RnL, RnH, RmL, RmH, Res0, Res1, Res2;
            uint temp0, temp1, temp2, temp3;
            int tempm, tempn, fnLmL;
            int m = (state.Instruction & 0x00F0) >> 4;
            int n = (state.Instruction & 0x0F00) >> 8;

            tempn = (int)state.R[n];
            tempm = (int)state.R[m];
            if (tempn < 0)
                tempn = 0 - tempn;
            if (tempm < 0)
                tempm = 0 - tempm;
            if ((int) (state.R[n] ^ state.R[m]) < 0)
                fnLmL = -1;
            else
                fnLmL = 0;

            temp1 = (uint) tempn;
            temp2 = (uint) tempm;

            RnL = temp1 & 0x0000FFFF;
            RnH = (temp1 >> 16) & 0x0000FFFF;
            RmL = temp2 & 0x0000FFFF;
            RmH = (temp2 >> 16) & 0x0000FFFF;

            temp0 = RmL * RnL;
            temp1 = RmH * RnL;
            temp2 = RmL * RnH;
            temp3 = RmH * RnH;

            Res2 = 0;
            Res1 = temp1 + temp2;
            if (Res1 < temp1)
                Res2 += 0x00010000;

            temp1 = (Res1 << 16) & 0xFFFF0000;
            Res0 = temp0 + temp1;
            if (Res0 < temp0)
                Res2++;

            Res2 = Res2 + ((Res1 >> 16) & 0x0000FFFF) + temp3;

            if (fnLmL < 0)
            {
                Res2 = ~Res2;
                if (Res0 == 0)
                    Res2++;
                else
                    Res0 =(~Res0) + 1;
            }
            state.MACH = Res2;
            state.MACL = Res0;
            state.PC += 2;
            state.Cycles += 2;
        }
    }

    private static void Dmulu(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint RnL, RnH, RmL, RmH, Res0, Res1, Res2;
            uint temp0, temp1, temp2, temp3;
            int m = (state.Instruction & 0x00F0) >> 4;
            int n = (state.Instruction & 0x0F00) >> 8;

            RnL = state.R[n] & 0x0000FFFF;
            RnH = (state.R[n] >> 16) & 0x0000FFFF;
            RmL = state.R[m] & 0x0000FFFF;
            RmH = (state.R[m] >> 16) & 0x0000FFFF;

            temp0 = RmL * RnL;
            temp1 = RmH * RnL;
            temp2 = RmL * RnH;
            temp3 = RmH * RnH;

            Res2 = 0;
            Res1 = temp1 + temp2;
            if (Res1 < temp1)
                Res2 += 0x00010000;

            temp1 = (Res1 << 16) & 0xFFFF0000;
            Res0 = temp0 + temp1;
            if (Res0 < temp0)
                Res2++;

            Res2 = Res2 + ((Res1 >> 16) & 0x0000FFFF) + temp3;

            state.MACH = Res2;
            state.MACL = Res0;
            state.PC += 2;
            state.Cycles += 2;
        }
    }

    private static void Dt(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int n = (state.Instruction & 0x0F00) >> 8;

            state.R[n]--;
            if (state.R[n] == 0)
                state.SR.T = 1;
            else
                state.SR.T = 0;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Extsb(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = (state.Instruction & 0x00F0) >> 4;
            int n = (state.Instruction & 0x0F00) >> 8;

            state.R[n] = (uint)(sbyte)state.R[m];
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Extsw(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = (state.Instruction & 0x00F0) >> 4;
            int n = (state.Instruction & 0x0F00) >> 8;

            state.R[n] = (uint)(short)state.R[m];
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Extub(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = (state.Instruction & 0x00F0) >> 4;
            int n = (state.Instruction & 0x0F00) >> 8;

            state.R[n] = (uint)(byte)state.R[m];
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Extuw(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = (state.Instruction & 0x00F0) >> 4;
            int n = (state.Instruction & 0x0F00) >> 8;

            state.R[n] = (uint)(ushort)state.R[m];
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Ldcgbr(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            state.GBR = state.R[((state.Instruction & 0x0F00) >> 8)];
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Ldcsr(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            state.SR.All = state.R[((state.Instruction & 0x0F00) >> 8)] & 0x000003F3;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Ldcvbr(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = (state.Instruction & 0x0F00) >> 8;

            state.VBR = state.R[m];
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Ldsmach(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            state.MACH = state.R[((state.Instruction & 0x0F00) >> 8)];
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Ldsmacl(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            state.MACL = state.R[((state.Instruction & 0x0F00) >> 8)];
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Ldspr(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            state.PR = state.R[((state.Instruction & 0x0F00) >> 8)];
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Mov(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            state.R[((state.Instruction & 0x0F00) >> 8)] = state.R[((state.Instruction & 0x00F0) >> 4)];
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Mova(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int disp = (state.Instruction & 0x00FF);

            state.R[0] = ((state.PC + 4) & 0xFFFFFFFC) + (uint)(disp << 2);
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Movi(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int i = (state.Instruction & 0x00FF);
            int n = (state.Instruction & 0x0F00) >> 8;

            state.R[n] = (uint)(sbyte)i;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Movt(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            state.R[((state.Instruction & 0x0F00) >> 8)] = (0x00000001 & state.SR.All);
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Mull(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = (state.Instruction & 0x00F0) >> 4;
            int n = (state.Instruction & 0x0F00) >> 8;

            state.MACL = state.R[n] * state.R[m];
            state.PC += 2;
            state.Cycles += 2;
        }
    }

    private static void Muls(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = (state.Instruction & 0x00F0) >> 4;
            int n = (state.Instruction & 0x0F00) >> 8;

            state.MACL = (uint)((int)(short)state.R[n] * (int)(short)state.R[m]);
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Mulu(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = (state.Instruction & 0x00F0) >> 4;
            int n = (state.Instruction & 0x0F00) >> 8;

            state.MACL = ((uint)(ushort)state.R[n] * (uint)(ushort)state.R[m]);
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Neg(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            state.R[((state.Instruction & 0x0F00) >> 8)] = 0 - state.R[((state.Instruction & 0x00F0) >> 4)];
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Negc(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint temp;
            int m = (state.Instruction & 0x00F0) >> 4;
            int n = (state.Instruction & 0x0F00) >> 8;

            temp = 0 - state.R[m];
            state.R[n] = temp - state.SR.T;
            if (0 < temp)
                state.SR.T = 1;
            else
                state.SR.T = 0;
            if (temp < state.R[n])
                state.SR.T = 1;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Nop(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Not(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            state.R[((state.Instruction & 0x0F00) >> 8)] = ~state.R[((state.Instruction & 0x00F0) >> 4)];
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Or(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            state.R[((state.Instruction & 0x0F00) >> 8)] |= state.R[((state.Instruction & 0x00F0) >> 4)];
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Ori(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            state.R[0] |= (uint)(state.Instruction & 0x00FF);
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Rotcl(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int temp;
            int n = (state.Instruction & 0x0F00) >> 8;

            if ((state.R[n] & 0x80000000) == 0)
                temp = 0;
            else
                temp = 1;

            state.R[n] <<= 1;

            if (state.SR.T == 1)
                state.R[n] |= 0x00000001;
            else
                state.R[n] &= 0xFFFFFFFE;

            if (temp == 1)
                state.SR.T = 1;
            else
                state.SR.T = 0;

            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Rotcr(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int temp;
            int n = (state.Instruction & 0x0F00) >> 8;

            if ((state.R[n] & 0x00000001) == 0)
                temp = 0;
            else
                temp = 1;

            state.R[n] >>= 1;

            if (state.SR.T == 1)
                state.R[n] |= 0x80000000;
            else
                state.R[n] &= 0x7FFFFFFF;

            if (temp == 1)
                state.SR.T = 1;
            else
                state.SR.T = 0;

            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Rotl(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int n = (state.Instruction & 0x0F00) >> 8;

            if ((state.R[n] & 0x80000000) == 0)
                state.SR.T = 0;
            else
                state.SR.T = 1;

            state.R[n] <<= 1;

            if (state.SR.T == 1)
                state.R[n] |= 0x00000001;
            else
                state.R[n] &= 0xFFFFFFFE;

            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Rotr(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int n = (state.Instruction & 0x0F00) >> 8;

            if ((state.R[n] & 0x00000001) == 0)
                state.SR.T = 0;
            else
                state.SR.T = 1;

            state.R[n] >>= 1;

            if (state.SR.T == 1)
                state.R[n] |= 0x80000000;
            else
                state.R[n] &= 0x7FFFFFFF;

            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Sett(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            state.SR.T = 1;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Shal(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int n = (state.Instruction & 0x0F00) >> 8;
            if ((state.R[n] & 0x80000000) == 0)
                state.SR.T = 0;
            else
                state.SR.T = 1;
            state.R[n] <<= 1;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Shar(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int temp;
            int n = (state.Instruction & 0x0F00) >> 8;

            if ((state.R[n] & 0x00000001) == 0)
                state.SR.T = 0;
            else
                state.SR.T = 1;

            if ((state.R[n] & 0x80000000) == 0)
                temp = 0;
            else
                temp = 1;

            state.R[n] >>= 1;

            if (temp == 1)
                state.R[n] |= 0x80000000;
            else
                state.R[n] &= 0x7FFFFFFF;

            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Shll(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int n = (state.Instruction & 0x0F00) >> 8;

            if ((state.R[n] & 0x80000000) == 0)
                state.SR.T = 0;
            else
                state.SR.T = 1;

            state.R[n] <<= 1;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Shll2(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            state.R[((state.Instruction & 0x0F00) >> 8)] <<= 2;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Shll8(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            state.R[((state.Instruction & 0x0F00) >> 8)] <<= 8;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Shll16(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            state.R[((state.Instruction & 0x0F00) >> 8)] <<= 16;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Shlr(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int n = (state.Instruction & 0x0F00) >> 8;

            if ((state.R[n] & 0x00000001) == 0)
                state.SR.T = 0;
            else
                state.SR.T = 1;

            state.R[n] >>= 1;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Shlr2(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int n = (state.Instruction & 0x0F00) >> 8;
            state.R[n] >>= 2;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Shlr8(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int n = (state.Instruction & 0x0F00) >> 8;
            state.R[n] >>= 8;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Shlr16(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int n = (state.Instruction & 0x0F00) >> 8;
            state.R[n] >>= 16;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Stcgbr(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int n = (state.Instruction & 0x0F00) >> 8;
            state.R[n] = state.GBR;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Stcsr(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int n = (state.Instruction & 0x0F00) >> 8;
            state.R[n] = state.SR.All;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Stcvbr(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int n = (state.Instruction & 0x0F00) >> 8;
            state.R[n] = state.VBR;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Stsmach(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int n = (state.Instruction & 0x0F00) >> 8;
            state.R[n] = state.MACH;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Stsmacl(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int n = (state.Instruction & 0x0F00) >> 8;
            state.R[n] = state.MACL;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Stspr(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int n = (state.Instruction & 0x0F00) >> 8;
            state.R[n] = state.PR;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Sub(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = (state.Instruction & 0x00F0) >> 4;
            int n = (state.Instruction & 0x0F00) >> 8;
            state.R[n] -= state.R[m];
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Subc(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = (state.Instruction & 0x00F0) >> 4;
            int n = (state.Instruction & 0x0F00) >> 8;
            uint tmp0, tmp1;

            tmp1 = state.R[n] - state.R[m];
            tmp0 = state.R[n];
            state.R[n] = tmp1 - state.SR.T;

            if (tmp0 < tmp1)
                state.SR.T = 1;
            else
                state.SR.T = 0;

            if (tmp1 < state.R[n])
                state.SR.T = 1;

            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Subv(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = (state.Instruction & 0x00F0) >> 4;
            int n = (state.Instruction & 0x0F00) >> 8;
            int dest, src, ans;

            if ((int)state.R[n] >= 0)
                dest = 0;
            else
                dest = 1;

            if ((int)state.R[m] >= 0)
                src = 0;
            else
                src = 1;

            src += dest;
            state.R[n] -= state.R[m];

            if ((int)state.R[n] >= 0)
                ans = 0;
            else
                ans = 1;

            ans += dest;

            if (src == 1)
            {
            if (ans == 1)
                state.SR.T = 1;
            else
                state.SR.T = 0;
            }
            else
                state.SR.T = 0;

            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Swapb(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint temp0, temp1;
            int m = (state.Instruction & 0x00F0) >> 4;
            int n = (state.Instruction & 0x0F00) >> 8;

            temp0 = state.R[m] & 0xffff0000;
            temp1 = (state.R[m] & 0x000000ff) << 8;
            state.R[n] = (state.R[m] >> 8) & 0x000000ff;
            state.R[n] = state.R[n] | temp1 | temp0;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Swapw(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint temp;
            int m = (state.Instruction & 0x00F0) >> 4;
            int n = (state.Instruction & 0x0F00) >> 8;
            temp = (state.R[m] >> 16) & 0x0000FFFF;
            state.R[n] = state.R[m] << 16;
            state.R[n] |= temp;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Tst(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = (state.Instruction & 0x00F0) >> 4;
            int n = (state.Instruction & 0x0F00) >> 8;

            if ((state.R[n] & state.R[m]) == 0)
                state.SR.T = 1;
            else
                state.SR.T = 0;

            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Tsti(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int temp;
            int i = (state.Instruction & 0x00FF);

            temp = (int)(state.R[0] & (uint)i);

            if (temp == 0)
                state.SR.T = 1;
            else
                state.SR.T = 0;

            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Xor(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int b = (state.Instruction & 0x0F00) >> 8;
            int c = (state.Instruction & 0x00F0) >> 4;

            state.R[b] ^= state.R[c];
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Xori(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int source = (state.Instruction & 0x00FF);
            state.R[0] ^= (uint)source;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Xtrct(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint temp;
            int m = (state.Instruction & 0x00F0) >> 4;
            int n = (state.Instruction & 0x0F00) >> 8;

            temp = (state.R[m] << 16) & 0xFFFF0000;
            state.R[n] = (state.R[n] >> 16) & 0x0000FFFF;
            state.R[n] |= temp;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Sleep(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            state.Cycles += 3;
        }
    }

    private static void Undecoded(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        if (memory.HandleBiosFunction(state))
            return;

        unchecked
        {
            uint vectnum;

            state.R[15]-=4;
            memory.Write32(state.R[15], state.SR.All);

            state.R[15]-=4;
            memory.Write32(state.R[15], state.PC + 2);

            vectnum = 4;

            state.PC = memory.Read32(state.VBR + (vectnum << 2));
            state.Cycles++;
        }
    }

    private static void Andm(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int temp;
            int source = (state.Instruction & 0x00FF);

            temp = (int)memory.Read8(state.GBR + state.R[0]);
            temp &= source;
            memory.Write8((state.GBR + state.R[0]), (byte)temp);
            state.PC += 2;
            state.Cycles += 3;
        }
    }

    private static void Bfs(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            if (state.SR.T == 0)
            {
                int disp = (int)(sbyte)state.Instruction;
                uint temp = state.PC;

                state.PC = state.PC + (uint)(disp << 1) + 4;

                state.Cycles += 2;
                Delay(state, memory, temp + 2);
            }
            else
            {
                state.PC += 2;
                state.Cycles++;
            }
        }
    }

    private static void Bra(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int disp = (state.Instruction & 0x0FFF);
            uint temp = state.PC;

            if ((disp & 0x800) != 0)
                disp |= (int)0xFFFFF000;

            state.PC = state.PC + (uint)(disp << 1) + 4;

            state.Cycles += 2;
            Delay(state, memory, temp + 2);
        }
    }

    private static void Braf(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint temp;
            int m = ((state.Instruction & 0x0F00) >> 8);

            temp = state.PC;
            state.PC += state.R[m] + 4;

            state.Cycles += 2;
            Delay(state, memory, temp + 2);
        }
    }

    private static void Bsr(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint temp;
            int disp = (state.Instruction & 0x0FFF);

            temp = state.PC;
            if ((disp & 0x800) != 0) disp |= (int)0xFFFFF000;
            state.PR = state.PC + 4;
            state.PC = state.PC + (uint)(disp << 1) + 4;

            state.Cycles += 2;
            Delay(state, memory, temp + 2);
        }
    }

    private static void Bsrf(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint temp = state.PC;
            state.PR = state.PC + 4;
            state.PC += state.R[((state.Instruction & 0x0F00) >> 8)] + 4;
            state.Cycles += 2;
            Delay(state, memory, temp + 2);
        }
    }

    private static void Bts(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            if (state.SR.T != 0)
            {
                int disp = (int)(sbyte)state.Instruction;
                uint temp = state.PC;

                state.PC += (uint)((disp << 1) + 4);
                state.Cycles += 2;
                Delay(state, memory, temp + 2);
            }
            else
            {
                state.PC+=2;
                state.Cycles++;
            }
        }
    }

    private static void Jmp(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint temp;
            int m = ((state.Instruction & 0x0F00) >> 8);

            temp = state.PC;
            state.PC = state.R[m];
            state.Cycles += 2;
            Delay(state, memory, temp + 2);
        }
    }

    private static void Jsr(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint temp;
            int m = ((state.Instruction & 0x0F00) >> 8);

            temp = state.PC;
            state.PR = state.PC + 4;
            state.PC = state.R[m];
            state.Cycles += 2;
            Delay(state, memory, temp + 2);
        }
    }

    private static void Ldcmgbr(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = ((state.Instruction & 0x0F00) >> 8);

            state.GBR = memory.Read32(state.R[m]);
            state.R[m] += 4;
            state.PC += 2;
            state.Cycles += 3;
        }
    }

    private static void Ldcmsr(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = ((state.Instruction & 0x0F00) >> 8);

            state.SR.All = memory.Read32(state.R[m]) & 0x000003F3;
            state.R[m] += 4;
            state.PC += 2;
            state.Cycles += 3;
        }
    }

    private static void Ldcmvbr(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = ((state.Instruction & 0x0F00) >> 8);

            state.VBR = memory.Read32(state.R[m]);
            state.R[m] += 4;
            state.PC += 2;
            state.Cycles += 3;
        }
    }

    private static void Ldsmmach(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = ((state.Instruction & 0x0F00) >> 8);
            state.MACH = memory.Read32(state.R[m]);
            state.R[m] += 4;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Ldsmmacl(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = ((state.Instruction & 0x0F00) >> 8);
            state.MACL = memory.Read32(state.R[m]);
            state.R[m] += 4;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Ldsmpr(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = ((state.Instruction & 0x0F00) >> 8);
            state.PR = memory.Read32(state.R[m]);
            state.R[m] += 4;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Macl(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint RnL, RnH, RmL, RmH, Res0, Res1, Res2;
            uint temp0, temp1, temp2, temp3;
            int tempm, tempn, fnLmL;
            int m = ((state.Instruction & 0x00F0) >> 4);
            int n = ((state.Instruction & 0x0F00) >> 8);

            tempn = (int)memory.Read32(state.R[n]);
            state.R[n] += 4;
            tempm = (int)memory.Read32(state.R[m]);
            state.R[m] += 4;

            if ((int) (tempn ^ tempm) < 0)
                fnLmL = -1;
            else
                fnLmL = 0;
            if (tempn < 0)
                tempn = 0 - tempn;
            if (tempm < 0)
                tempm = 0 - tempm;

            temp1 = (uint) tempn;
            temp2 = (uint) tempm;

            RnL = temp1 & 0x0000FFFF;
            RnH = (temp1 >> 16) & 0x0000FFFF;
            RmL = temp2 & 0x0000FFFF;
            RmH = (temp2 >> 16) & 0x0000FFFF;

            temp0 = RmL * RnL;
            temp1 = RmH * RnL;
            temp2 = RmL * RnH;
            temp3 = RmH * RnH;

            Res2 = 0;
            Res1 = temp1 + temp2;
            if (Res1 < temp1)
                Res2 += 0x00010000;

            temp1 = (Res1 << 16) & 0xFFFF0000;
            Res0 = temp0 + temp1;
            if (Res0 < temp0)
                Res2++;

            Res2 = Res2 + ((Res1 >> 16) & 0x0000FFFF) + temp3;

            if (fnLmL < 0)
            {
                Res2 = ~Res2;
                if (Res0==0)
                    Res2++;
                else
                    Res0 = (~Res0) + 1;
            }
            if (state.SR.S == 1)
            {
                Res0 = state.MACL + Res0;
                if (state.MACL>Res0)
                    Res2++;
                if ((state.MACH & 0x00008000) == 0)
                    Res2 += state.MACH | 0xFFFF0000;
                Res2+=(state.MACH & 0x0000FFFF);
                if (((int)Res2<0)&& (Res2<0xFFFF8000))
                {
                    Res2 = 0x00008000;
                    Res0 = 0x00000000;
                }
                if (((int)Res2>0)&& (Res2>0x00007FFF))
                {
                    Res2 = 0x00007FFF;
                    Res0 = 0xFFFFFFFF;
                }

                state.MACH = Res2;
                state.MACL = Res0;
            }
            else
            {
                Res0 = state.MACL + Res0;
                if (state.MACL>Res0)
                    Res2++;
                Res2+=state.MACH;

                state.MACH = Res2;
                state.MACL = Res0;
            }

            state.PC+=2;
            state.Cycles += 3;
        }
    }

    private static void Macw(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int tempm, tempn, dest, src, ans;
            uint templ;
            int m = ((state.Instruction & 0x00F0) >> 4);
            int n = ((state.Instruction & 0x0F00) >> 8);

            tempn = (int)memory.Read16(state.R[n]);
            state.R[n]+=2;
            tempm = (int)memory.Read16(state.R[m]);
            state.R[m]+=2;
            templ = state.MACL;
            tempm = ((int)(short)tempn * (int)(short)tempm);

            if ((int)state.MACL>=0)
                dest = 0;
            else
                dest = 1;
            if ((int)tempm>=0)
            {
                src = 0;
                tempn = 0;
            }
            else
            {
                src = 1;
                tempn = -1;
            }
            src+=dest;
            state.MACL += (uint)tempm;
            if ((int)state.MACL>=0)
                ans = 0;
            else
                ans = 1;
            ans+=dest;
            if (state.SR.S == 1)
            {
                if (ans==1)
                {
                    if (src==0)
                        state.MACL = 0x7FFFFFFF;
                    if (src==2)
                        state.MACL = 0x80000000;
                }
            }
            else
            {
                state.MACH += (uint)tempn;
                if (templ>state.MACL)
                    state.MACH+=1;
            }
            state.PC+=2;
            state.Cycles += 3;
        }
    }

    private static void Movbl(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = ((state.Instruction & 0x00F0) >> 4);
            int n = ((state.Instruction & 0x0F00) >> 8);

            state.R[n] = (uint)(sbyte)memory.Read8(state.R[m]);
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Movbl0(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = ((state.Instruction & 0x00F0) >> 4);
            int n = ((state.Instruction & 0x0F00) >> 8);

            state.R[n] = (uint)(sbyte)memory.Read8(state.R[m] + state.R[0]);
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Movbl4(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = ((state.Instruction & 0x00F0) >> 4);
            uint disp = (uint)(state.Instruction & 0x000F);

            state.R[0] = (uint)(sbyte)memory.Read8(state.R[m] + disp);
            state.PC+=2;
            state.Cycles++;
        }
    }

    private static void Movblg(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint disp = (uint)(state.Instruction & 0x00FF);

            state.R[0] = (uint)(sbyte)memory.Read8(state.GBR + disp);
            state.PC+=2;
            state.Cycles++;
        }
    }

    private static void Movbm(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = ((state.Instruction & 0x00F0) >> 4);
            int n = ((state.Instruction & 0x0F00) >> 8);

            memory.Write8((state.R[n] - 1), (byte)state.R[m]);
            state.R[n] -= 1;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Movbp(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = ((state.Instruction & 0x00F0) >> 4);
            int n = ((state.Instruction & 0x0F00) >> 8);

            state.R[n] = (uint)(sbyte)memory.Read8(state.R[m]);
            if (n != m)
            state.R[m] += 1;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Movbs(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int b = ((state.Instruction & 0x0F00) >> 8);
            int c = ((state.Instruction & 0x00F0) >> 4);

            memory.Write8(state.R[b], (byte)state.R[c]);
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Movbs0(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            memory.Write8(state.R[((state.Instruction & 0x0F00) >> 8)] + state.R[0], (byte)state.R[((state.Instruction & 0x00F0) >> 4)]);
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Movbs4(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint disp = (uint)(state.Instruction & 0x000F);
            int n = ((state.Instruction & 0x00F0) >> 4);

            memory.Write8(state.R[n] + disp, (byte)state.R[0]);
            state.PC+=2;
            state.Cycles++;
        }
    }

    private static void Movbsg(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint disp = (uint)(state.Instruction & 0x00FF);

            memory.Write8(state.GBR + disp, (byte)state.R[0]);
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Movli(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint disp = (uint)(state.Instruction & 0x00FF);
            int n = ((state.Instruction & 0x0F00) >> 8);

            state.R[n] = memory.Read32(((state.PC + 4) & 0xFFFFFFFC) + (disp << 2));
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Movll(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint addr = state.R[((state.Instruction & 0x00F0) >> 4)];

            if (memory.CheckWait(addr, 2))
            {
                state.Cycles++;
                return;
            }

            state.R[((state.Instruction & 0x0F00) >> 8)] = memory.Read32(addr);
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Movll0(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            state.R[((state.Instruction & 0x0F00) >> 8)] = memory.Read32(state.R[((state.Instruction & 0x00F0) >> 4)] + state.R[0]);
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Movll4(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = ((state.Instruction & 0x00F0) >> 4);
            uint disp = (uint)(state.Instruction & 0x000F);
            int n = ((state.Instruction & 0x0F00) >> 8);

            state.R[n] = memory.Read32(state.R[m] + (disp << 2));
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Movllg(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint disp = (uint)(state.Instruction & 0x00FF);

            state.R[0] = memory.Read32(state.GBR + (disp << 2));
            state.PC+=2;
            state.Cycles++;
        }
    }

    private static void Movlm(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = ((state.Instruction & 0x00F0) >> 4);
            int n = ((state.Instruction & 0x0F00) >> 8);

            memory.Write32(state.R[n] - 4, state.R[m]);
            state.R[n] -= 4;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Movlp(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = ((state.Instruction & 0x00F0) >> 4);
            int n = ((state.Instruction & 0x0F00) >> 8);

            state.R[n] = memory.Read32(state.R[m]);
            if (n != m) state.R[m] += 4;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Movls(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int b = ((state.Instruction & 0x0F00) >> 8);
            int c = ((state.Instruction & 0x00F0) >> 4);

            memory.Write32(state.R[b], state.R[c]);
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Movls0(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            memory.Write32(state.R[((state.Instruction & 0x0F00) >> 8)] + state.R[0], state.R[((state.Instruction & 0x00F0) >> 4)]);
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Movls4(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = ((state.Instruction & 0x00F0) >> 4);
            uint disp = (uint)(state.Instruction & 0x000F);
            int n = ((state.Instruction & 0x0F00) >> 8);

            memory.Write32(state.R[n] + (disp << 2), state.R[m]);
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Movlsg(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint disp = (uint)(state.Instruction & 0x00FF);

            memory.Write32(state.GBR + (disp << 2), state.R[0]);
            state.PC+=2;
            state.Cycles++;
        }
    }

    private static void Movwi(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint disp = (uint)(state.Instruction & 0x00FF);
            int n = ((state.Instruction & 0x0F00) >> 8);

            state.R[n] = (uint)(short)memory.Read16(state.PC + (disp << 1) + 4);
            state.PC+=2;
            state.Cycles++;
        }
    }

    private static void Movwl(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = ((state.Instruction & 0x00F0) >> 4);
            int n = ((state.Instruction & 0x0F00) >> 8);
            uint addr = state.R[m];

            if (memory.CheckWait(addr, 1))
            {
                state.Cycles++;
                return;
            }

            state.R[n] = (uint)(short)memory.Read16(addr);
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Movwl0(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = ((state.Instruction & 0x00F0) >> 4);
            int n = ((state.Instruction & 0x0F00) >> 8);

            state.R[n] = (uint)(short)memory.Read16(state.R[m] + state.R[0]);
            state.PC+=2;
            state.Cycles++;
        }
    }

    private static void Movwl4(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = ((state.Instruction & 0x00F0) >> 4);
            uint disp = (uint)(state.Instruction & 0x000F);

            state.R[0] = (uint)(short)memory.Read16(state.R[m] + (disp << 1));
            state.PC+=2;
            state.Cycles++;
        }
    }

    private static void Movwlg(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint disp = (uint)(state.Instruction & 0x00FF);

            state.R[0] = (uint)(short)memory.Read16(state.GBR + (disp << 1));
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Movwm(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = ((state.Instruction & 0x00F0) >> 4);
            int n = ((state.Instruction & 0x0F00) >> 8);

            memory.Write16(state.R[n] - 2, (ushort)state.R[m]);
            state.R[n] -= 2;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Movwp(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = ((state.Instruction & 0x00F0) >> 4);
            int n = ((state.Instruction & 0x0F00) >> 8);

            state.R[n] = (uint)(short)memory.Read16(state.R[m]);
            if (n != m)
                state.R[m] += 2;
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Movws(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int m = ((state.Instruction & 0x00F0) >> 4);
            int n = ((state.Instruction & 0x0F00) >> 8);

            memory.Write16(state.R[n], (ushort)state.R[m]);
            state.PC += 2;
            state.Cycles++;
        }
    }

    private static void Movws0(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            memory.Write16(state.R[((state.Instruction & 0x0F00) >> 8)] + state.R[0], (ushort)state.R[((state.Instruction & 0x00F0) >> 4)]);
            state.PC+=2;
            state.Cycles++;
        }
    }

    private static void Movws4(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint disp = (uint)(state.Instruction & 0x000F);
            int n = ((state.Instruction & 0x00F0) >> 4);

            memory.Write16(state.R[n] + (disp << 1), (ushort)state.R[0]);
            state.PC+=2;
            state.Cycles++;
        }
    }

    private static void Movwsg(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint disp = (uint)(state.Instruction & 0x00FF);

            memory.Write16(state.GBR + (disp << 1), (ushort)state.R[0]);
            state.PC+=2;
            state.Cycles++;
        }
    }

    private static void Orm(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int temp;
            int source = (state.Instruction & 0x00FF);

            temp = (int)memory.Read8(state.GBR + state.R[0]);
            temp |= source;
            memory.Write8(state.GBR + state.R[0], (byte)temp);
            state.PC += 2;
            state.Cycles += 3;
        }
    }

    private static void Rte(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint temp;
            temp = state.PC;
            state.PC = memory.Read32(state.R[15]);
            state.R[15] += 4;
            state.SR.All = memory.Read32(state.R[15]) & 0x000003F3;
            state.R[15] += 4;
            state.Cycles += 4;
            Delay(state, memory, temp + 2);
        }
    }

    private static void Rts(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint temp;

            temp = state.PC;
            state.PC = state.PR;

            state.Cycles += 2;
            Delay(state, memory, temp + 2);
        }
    }

    private static void Stcmgbr(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int n = ((state.Instruction & 0x0F00) >> 8);
            state.R[n]-=4;
            memory.Write32(state.R[n], state.GBR);
            state.PC+=2;
            state.Cycles += 2;
        }
    }

    private static void Stcmsr(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int n = ((state.Instruction & 0x0F00) >> 8);
            state.R[n]-=4;
            memory.Write32(state.R[n], state.SR.All);
            state.PC+=2;
            state.Cycles += 2;
        }
    }

    private static void Stcmvbr(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int n = ((state.Instruction & 0x0F00) >> 8);
            state.R[n]-=4;
            memory.Write32(state.R[n], state.VBR);
            state.PC+=2;
            state.Cycles += 2;
        }
    }

    private static void Stsmmach(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int n = ((state.Instruction & 0x0F00) >> 8);
            state.R[n] -= 4;
            memory.Write32(state.R[n], state.MACH);
            state.PC+=2;
            state.Cycles++;
        }
    }

    private static void Stsmmacl(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int n = ((state.Instruction & 0x0F00) >> 8);
            state.R[n] -= 4;
            memory.Write32(state.R[n], state.MACL);
            state.PC+=2;
            state.Cycles++;
        }
    }

    private static void Stsmpr(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int n = ((state.Instruction & 0x0F00) >> 8);
            state.R[n] -= 4;
            memory.Write32(state.R[n], state.PR);
            state.PC+=2;
            state.Cycles++;
        }
    }

    private static void Tas(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int temp;
            int n = ((state.Instruction & 0x0F00) >> 8);

            temp = (int)memory.Read8(state.R[n]);

            if (temp==0)
                state.SR.T = 1;
            else
                state.SR.T = 0;

            temp|=0x00000080;
            memory.Write8(state.R[n], (byte)temp);
            state.PC+=2;
            state.Cycles += 4;
        }
    }

    private static void Trapa(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            uint imm = (uint)(state.Instruction & 0x00FF);

            state.R[15]-=4;
            memory.Write32(state.R[15], state.SR.All);
            state.R[15]-=4;
            memory.Write32(state.R[15], state.PC + 2);
            state.PC = memory.Read32(state.VBR + (imm << 2));
            state.Cycles += 8;
        }
    }

    private static void Tstm(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int temp;
            int i = (state.Instruction & 0x00FF);

            temp = (int)memory.Read8(state.GBR + state.R[0]);
            temp&=i;

            if (temp==0)
                state.SR.T = 1;
            else
                state.SR.T = 0;

            state.PC+=2;
            state.Cycles += 3;
        }
    }

    private static void Xorm(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        unchecked
        {
            int source = (state.Instruction & 0x00FF);
            int temp;

            temp = (int)memory.Read8(state.GBR + state.R[0]);
            temp ^= source;
            memory.Write8(state.GBR + state.R[0], (byte)temp);
            state.PC += 2;
            state.Cycles += 3;
        }
    }

    private static Opcode Decode(bool isSh1, ushort instruction)
    {
        switch ((instruction >> 12))
        {
            case 0:
                switch ((instruction & 0xF))
                {
                    case 2:
                        switch (((instruction >> 4) & 0xF))
                        {
                            case 0: return Stcsr;
                            case 1: return Stcgbr;
                            case 2: return Stcvbr;
                            default: return Undecoded;
                        }

                    case 3:
                        if (isSh1)
                            return Undecoded;
                        else
                        {
                            switch (((instruction >> 4) & 0xF))
                            {
                                case 0: return Bsrf;
                                case 2: return Braf;
                                default: return Undecoded;
                            }
                        }
                    case 4: return Movbs0;
                    case 5: return Movws0;
                    case 6: return Movls0;
                    case 7: return isSh1 ? Undecoded : Mull;
                    case 8:
                        switch (((instruction >> 4) & 0xF))
                        {
                            case 0: return Clrt;
                            case 1: return Sett;
                            case 2: return Clrmac;
                            default: return Undecoded;
                        }
                    case 9:
                        switch (((instruction >> 4) & 0xF))
                        {
                            case 0: return Nop;
                            case 1: return Div0u;
                            case 2: return Movt;
                            default: return Undecoded;
                        }
                    case 10:
                        switch (((instruction >> 4) & 0xF))
                        {
                            case 0: return Stsmach;
                            case 1: return Stsmacl;
                            case 2: return Stspr;
                            default: return Undecoded;
                        }
                    case 11:
                        switch (((instruction >> 4) & 0xF))
                        {
                            case 0: return Rts;
                            case 1: return Sleep;
                            case 2: return Rte;
                            default: return Undecoded;
                        }
                    case 12: return Movbl0;
                    case 13: return Movwl0;
                    case 14: return Movll0;
                    case 15: return isSh1 ? Undecoded : Macl;
                    default: return Undecoded;
                }

            case 1: return Movls4;
            case 2:
                switch ((instruction & 0xF))
                {
                    case 0: return Movbs;
                    case 1: return Movws;
                    case 2: return Movls;
                    case 4: return Movbm;
                    case 5: return Movwm;
                    case 6: return Movlm;
                    case 7: return Div0s;
                    case 8: return Tst;
                    case 9: return And;
                    case 10: return Xor;
                    case 11: return Or;
                    case 12: return Cmpstr;
                    case 13: return Xtrct;
                    case 14: return Mulu;
                    case 15: return Muls;
                    default: return Undecoded;
                }

            case 3:
                switch ((instruction & 0xF))
                {
                    case 0: return Cmpeq;
                    case 2: return Cmphs;
                    case 3: return Cmpge;
                    case 4: return Div1;
                    case 5: return isSh1 ? Undecoded : Dmulu;
                    case 6: return Cmphi;
                    case 7: return Cmpgt;
                    case 8: return Sub;
                    case 10: return Subc;
                    case 11: return Subv;
                    case 12: return Add;
                    case 13: return isSh1 ? Undecoded : Dmuls;
                    case 14: return Addc;
                    case 15: return Addv;
                    default: return Undecoded;
                }

            case 4:
                switch ((instruction & 0xF))
                {
                    case 0:
                        switch (((instruction >> 4) & 0xF))
                        {
                            case 0: return Shll;
                            case 1: return isSh1 ? Undecoded : Dt;
                            case 2: return Shal;
                            default: return Undecoded;
                        }
                    case 1:
                        switch (((instruction >> 4) & 0xF))
                        {
                            case 0: return Shlr;
                            case 1: return Cmppz;
                            case 2: return Shar;
                            default: return Undecoded;
                        }
                    case 2:
                        switch (((instruction >> 4) & 0xF))
                        {
                            case 0: return Stsmmach;
                            case 1: return Stsmmacl;
                            case 2: return Stsmpr;
                            default: return Undecoded;
                        }
                    case 3:
                        switch (((instruction >> 4) & 0xF))
                        {
                            case 0: return Stcmsr;
                            case 1: return Stcmgbr;
                            case 2: return Stcmvbr;
                            default: return Undecoded;
                        }
                    case 4:
                        switch (((instruction >> 4) & 0xF))
                        {
                            case 0: return Rotl;
                            case 2: return Rotcl;
                            default: return Undecoded;
                        }
                    case 5:
                        switch (((instruction >> 4) & 0xF))
                        {
                            case 0: return Rotr;
                            case 1: return Cmppl;
                            case 2: return Rotcr;
                            default: return Undecoded;
                        }
                    case 6:
                        switch (((instruction >> 4) & 0xF))
                        {
                            case 0: return Ldsmmach;
                            case 1: return Ldsmmacl;
                            case 2: return Ldsmpr;
                            default: return Undecoded;
                        }
                    case 7:
                        switch (((instruction >> 4) & 0xF))
                        {
                            case 0: return Ldcmsr;
                            case 1: return Ldcmgbr;
                            case 2: return Ldcmvbr;
                            default: return Undecoded;
                        }
                    case 8:
                        switch (((instruction >> 4) & 0xF))
                        {
                            case 0: return Shll2;
                            case 1: return Shll8;
                            case 2: return Shll16;
                            default: return Undecoded;
                        }
                    case 9:
                        switch (((instruction >> 4) & 0xF))
                        {
                            case 0: return Shlr2;
                            case 1: return Shlr8;
                            case 2: return Shlr16;
                            default: return Undecoded;
                        }
                    case 10:
                        switch (((instruction >> 4) & 0xF))
                        {
                            case 0: return Ldsmach;
                            case 1: return Ldsmacl;
                            case 2: return Ldspr;
                            default: return Undecoded;
                        }
                    case 11:
                        switch (((instruction >> 4) & 0xF))
                        {
                            case 0: return Jsr;
                            case 1: return Tas;
                            case 2: return Jmp;
                            default: return Undecoded;
                        }
                    case 14:
                        switch (((instruction >> 4) & 0xF))
                        {
                            case 0: return Ldcsr;
                            case 1: return Ldcgbr;
                            case 2: return Ldcvbr;
                            default: return Undecoded;
                        }
                    case 15: return Macw;
                    default: return Undecoded;
                }
            case 5: return Movll4;
            case 6:
                switch ((instruction & 0xF))
                {
                    case 0: return Movbl;
                    case 1: return Movwl;
                    case 2: return Movll;
                    case 3: return Mov;
                    case 4: return Movbp;
                    case 5: return Movwp;
                    case 6: return Movlp;
                    case 7: return Not;
                    case 8: return Swapb;
                    case 9: return Swapw;
                    case 10: return Negc;
                    case 11: return Neg;
                    case 12: return Extub;
                    case 13: return Extuw;
                    case 14: return Extsb;
                    case 15: return Extsw;
                    default: return Undecoded;
                }

            case 7: return Addi;
            case 8:
                switch (((instruction >> 8) & 0xF))
                {
                    case 0: return Movbs4;
                    case 1: return Movws4;
                    case 4: return Movbl4;
                    case 5: return Movwl4;
                    case 8: return Cmpim;
                    case 9: return Bt;
                    case 11: return Bf;
                    case 13: return isSh1 ? Undecoded : Bts;
                    case 15: return isSh1 ? Undecoded : Bfs;
                    default: return Undecoded;
                }
            case 9: return Movwi;
            case 10: return Bra;
            case 11: return Bsr;
            case 12:
                switch (((instruction >> 8) & 0xF))
                {
                    case 0: return Movbsg;
                    case 1: return Movwsg;
                    case 2: return Movlsg;
                    case 3: return Trapa;
                    case 4: return Movblg;
                    case 5: return Movwlg;
                    case 6: return Movllg;
                    case 7: return Mova;
                    case 8: return Tsti;
                    case 9: return Andi;
                    case 10: return Xori;
                    case 11: return Ori;
                    case 12: return Tstm;
                    case 13: return Andm;
                    case 14: return Xorm;
                    case 15: return Orm;
                    default: return Undecoded;
                }

            case 13: return Movli;
            case 14: return Movi;
            default: return Undecoded;
        }
    }

    internal static void HandleInterrupts(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory)
    {
        if (state.NumberOfInterrupts == 0)
            return;

        SaturnSh2State.Interrupt interrupt = state.Interrupts[state.NumberOfInterrupts - 1];
        if (interrupt.Level <= state.SR.I)
            return;

        state.R[15] -= 4;
        memory.Write32(state.R[15], state.SR.All);
        state.R[15] -= 4;
        memory.Write32(state.R[15], state.PC);
        state.SR.I = interrupt.Level;
        state.PC = memory.Read32(state.VBR + ((uint)interrupt.Vector << 2));
        state.NumberOfInterrupts--;
        state.IsIdle = false;
    }

    internal static void Execute(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory, uint cycles)
    {
        HandleInterrupts(state, memory);

        if (!memory.CacheEmulationEnabled)
        {
            if (state.IsIdle)
                ParseIdle(state, memory, cycles);
            else
                CheckIdle(state, memory, cycles);
        }

        while (state.Cycles < cycles)
        {
            state.Instruction = FetchInstruction(memory, state.PC, Sh2FetchList);
            Sh2Opcodes[state.Instruction](state, memory);
        }

    }

    internal static void ExecuteSh1(SaturnSh2State state, SaturnMemory.Sh2MemoryMap memory, uint cycles)
    {
        var sh1 = memory.Sh1!;
        HandleInterrupts(state, memory);

        while (state.Cycles < cycles)
        {
            uint cyclesBefore = state.Cycles;
            state.Instruction = FetchInstruction(memory, state.PC, Sh1Tables.FetchList);
            Sh1Tables.Opcodes[state.Instruction](state, memory);
            sh1.ExecuteDma(unchecked((int)(state.Cycles - cyclesBefore)));
        }
    }

    internal static void SendInterrupt(SaturnSh2State state, byte vector, byte level)
    {
        for (uint i = 0; i < state.NumberOfInterrupts; i++)
        {
            if (state.Interrupts[i].Vector == vector)
                return;
        }

        state.Interrupts[state.NumberOfInterrupts] = new SaturnSh2State.Interrupt(vector, level);
        state.NumberOfInterrupts++;

        for (uint i = 0; i < state.NumberOfInterrupts - 1; i++)
        {
            for (uint j = i + 1; j < state.NumberOfInterrupts; j++)
            {
                if (state.Interrupts[i].Level > state.Interrupts[j].Level)
                {
                    SaturnSh2State.Interrupt interrupt = state.Interrupts[i];
                    state.Interrupts[i] = state.Interrupts[j];
                    state.Interrupts[j] = interrupt;
                }
            }
        }
    }
}

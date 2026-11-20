// Copyright 2005-2006 Fabien Coulon
// Translated from Yabause sh2idle.c.
// SPDX-License-Identifier: GPL-2.0-or-later

using MemoryMap = RetroMediaLab.Chips.Controllers.Sega.SaturnMemory.Sh2MemoryMap;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH2;

internal static partial class SaturnSh2Interpreter
{
    private const uint MaxIdleCheckCycles = 14;

    private struct IdleRegisterMarkers
    {
        private const uint Macl = 0x10000;
        private const uint Mach = 0x20000;
        private const uint Mac = 0x30000;
        private const uint SrT = 0x40000;
        private const uint SrQ = 0x80000;
        private const uint SrS = 0x100000;
        private const uint SrM = 0x400000;
        private const uint Sr = 0x7C0000;
        private const uint Pr = 0x800000;
        private const uint Gbr = 0x1000000;
        private const uint Vbr = 0x2000000;
        private const uint Constant = 0x4000000;
        private const uint R0 = 1;
        private const uint R15 = 1 << 15;

        internal uint Deterministic;
        private uint _changed;

        private readonly bool IsDeterministic(uint registers) =>
            (Deterministic & registers) == registers;

        private bool DelayCheck(MemoryMap memory, uint pc) =>
            CheckInstruction(memory, Sh2FetchList[(pc >> 20) & 0xFF](memory, pc), pc);

        private void Implies(uint destination, bool source)
        {
            if (source)
                Deterministic |= destination;
            else
                _changed |= destination;
        }

        internal void MarkUnchangedRegisters()
        {
            Deterministic = ~_changed | Constant;
        }

        internal bool CheckInstruction(MemoryMap memory, ushort instruction, uint pc)
        {
            int b = (instruction >> 8) & 0xF;
            int c = (instruction >> 4) & 0xF;
            int d = instruction & 0xF;
            uint destRb = 1u << b;
            uint destRc = 1u << c;

            switch (instruction >> 12)
            {
                case 0:
                    switch (d)
                    {
                        case 2:
                            switch (c)
                            {
                                case 0:
                                    Implies(destRb, IsDeterministic(Sr)); //stcsr
                                    break;
                                case 1:
                                    Implies(destRb, IsDeterministic(Gbr)); //stcgbr;
                                    break;
                                case 2:
                                    Implies(destRb, IsDeterministic(Vbr)); //stcvbr;
                                    break;
                            }
                            break;
                        case 3:
                            switch (c)
                            {
                                case 0:
                                    Implies(Pr, true); //bsrf;
                                    goto case 2;
                                case 2:
                                    if ((Deterministic & Constant) != 0 && !IsDeterministic(destRb)) return false;
                                    return DelayCheck(memory, unchecked(pc + 2)); //braf;
                            }
                            break;
                        case 4: //movbs0;
                        case 5: //movws0;
                        case 6:
                            return false;  //movls0;
                        case 7:
                            Implies(Macl, IsDeterministic(destRc) && IsDeterministic(destRb));  // mull
                            break;
                        case 8:
                            switch (c)
                            {
                                case 0: //clrt;
                                case 1:
                                    Implies(SrT, true);  //sett;
                                    break;
                                case 2:
                                    Implies(Mac, true);  //clrmac;
                                    break;
                            }
                            break;
                        case 9:
                            switch (c)
                            {
                                case 0: //nop;
                                    break;
                                case 1:
                                    Implies(SrM | SrQ | SrT, true);  //div0u;
                                    break;
                                case 2:
                                    Implies(destRb, IsDeterministic(SrT));   //movt;
                                    break;
                            }
                            break;
                        case 10:
                            switch (c)
                            {
                                case 0:
                                    Implies(destRb, IsDeterministic(Mach));   //stsmach;
                                    break;
                                case 1:
                                    Implies(destRb, IsDeterministic(Macl));   //stsmacl;
                                    break;
                                case 2:
                                    Implies(destRb, IsDeterministic(Pr));   //stspr;
                                    break;
                            }
                            break;
                        case 11:
                            switch (c)
                            {
                                case 1: //sleep;
                                    break;
                                case 0:
                                    if ((Deterministic & Constant) != 0 && !IsDeterministic(Pr)) return false; //rts;
                                    return DelayCheck(memory, unchecked(pc + 2));
                                case 2:
                                    if ((Deterministic & Constant) != 0 && !IsDeterministic(R15)) return false;
                                    Implies(Sr, IsDeterministic(R15));
                                    return DelayCheck(memory, unchecked(pc + 2)); //rte;
                            }
                            break;
                        case 12: //movbl0;
                        case 13: //movwl0;
                        case 14:
                            Implies(destRb, IsDeterministic(destRc) && IsDeterministic(R0));  //movll0;
                            break;
                        case 15:
                            Implies(Mac, IsDeterministic(destRc) && IsDeterministic(destRb) && IsDeterministic(SrS) && IsDeterministic(Macl) && IsDeterministic(Mach)); //macl
                            break;
                    }
                    break;
                case 1:
                    return false; //movls4;
                case 2:
                    switch (d)
                    {
                        case 0: //movbs;
                        case 1: //movws;
                        case 2:
                            return false; //movls;
                        case 4: //movbm;
                        case 5: //movwm;
                        case 6:
                            return false;//movlm;
                        case 7:
                            Implies(Sr, IsDeterministic(destRc) && IsDeterministic(destRb)); //div0s
                            break;
                        case 8:
                            Implies(SrT, IsDeterministic(destRc) && IsDeterministic(destRb)); //tst
                            break;
                        case 9: //y_and;
                        case 10: //y_xor;
                        case 11:
                            Implies(destRb, IsDeterministic(destRb) && IsDeterministic(destRc)); //y_or
                            break;
                        case 12:
                            Implies(SrT, IsDeterministic(destRc) && IsDeterministic(destRb)); //cmpstr;
                            break;
                        case 13:
                            Implies(destRb, IsDeterministic(destRb) && IsDeterministic(destRc)); //xtrct
                            break;
                        case 14:
                            Implies(Mac, IsDeterministic(destRb) && IsDeterministic(destRc)); //mulu
                            break;
                        case 15:
                            Implies(Macl, IsDeterministic(destRb) && IsDeterministic(destRc)); //muls
                            break;
                    }
                    break;
                case 3:
                    switch (d)
                    {
                        case 0: //cmpeq;
                        case 2: //cmphs;
                        case 6: //cmphi;
                        case 7: //cmpgt;
                        case 3:
                            Implies(SrT, IsDeterministic(destRb) && IsDeterministic(destRc));
                            break;
                        case 4:
                            Implies(SrQ | SrT | destRb, IsDeterministic(destRb) && IsDeterministic(destRc) && IsDeterministic(SrQ) && IsDeterministic(SrM));  //div1; /* CHECK ME */
                            break;
                        case 13: //dmuls;
                        case 5:
                            Implies(Mac, IsDeterministic(destRb) && IsDeterministic(destRc)); // dmulu
                            break;
                        case 8:
                            Implies(destRb, IsDeterministic(destRb) && IsDeterministic(destRc)); //sub
                            break;
                        case 15: //addv;
                        case 11:
                            Implies(SrT, IsDeterministic(destRb) && IsDeterministic(destRc)); //subv
                            break;
                        case 12:
                            Implies(destRb, IsDeterministic(destRb) && IsDeterministic(destRc)); //add
                            break;
                        case 10: //subc;
                        case 14:
                            Implies(destRb | SrT, IsDeterministic(destRb) && IsDeterministic(destRc) && IsDeterministic(SrT)); //addc
                            break;
                    }
                    break;
                case 4:
                    switch (d)
                    {
                        case 0:
                            switch (c)
                            {
                                case 1: //dt;
                                case 0: //shll;
                                case 2:
                                    Implies(SrT | destRb, IsDeterministic(destRb)); //shal
                                    break;
                            }
                            break;
                        case 1:
                            switch (c)
                            {
                                case 2:
                                case 0:
                                    Implies(SrT | destRb, IsDeterministic(destRb)); //shlr;
                                    break;
                                case 1:
                                    Implies(SrT, IsDeterministic(destRb)); //cmppz
                                    break;
                            }
                            break;
                        case 2:
                            switch (c)
                            {
                                case 0: //stsmmach;
                                case 1: //stsmmacl;
                                case 2:
                                    return false;//stsmpr;
                            }
                            break;
                        case 3:
                            switch (c)
                            {
                                case 0: //stcmsr;
                                case 1: //stcmgbr;
                                case 2:
                                    return false; //stcmvbr;
                            }
                            break;
                        case 4:
                            switch (c)
                            {
                                case 0: //rotl;
                                case 2:
                                    Implies(destRb | SrT, IsDeterministic(destRb)); //rotcl
                                    break;
                            }
                            break;
                        case 5:
                            switch (c)
                            {
                                case 0:
                                    Implies(destRb | SrT, IsDeterministic(destRb)); //rotr;
                                    break;
                                case 1:
                                    Implies(SrT, IsDeterministic(destRb)); //cmppl;
                                    break;
                                case 2:
                                    Implies(SrT | destRb, IsDeterministic(SrT) && IsDeterministic(destRb)); //rotcr
                                    break;
                            }
                            break;
                        case 6:
                            switch (c)
                            {
                                case 0:
                                    Implies(Mach, IsDeterministic(destRb)); //ldsmmach
                                    break;
                                case 1:
                                    Implies(Macl, IsDeterministic(destRb)); //lsdmmacl
                                    break;
                                case 2:
                                    Implies(Pr, IsDeterministic(destRb)); //ldsmpr
                                    break;
                            }
                            break;
                        case 7:
                            switch (c)
                            {
                                case 0:
                                    Implies(Sr, IsDeterministic(destRb)); //ldcmsr
                                    break;
                                case 1:
                                    Implies(Gbr, IsDeterministic(destRb)); //ldcmgbr
                                    break;
                                case 2:
                                    Implies(Vbr, IsDeterministic(destRb)); //lscmvbr
                                    break;
                            }
                            break;
                        case 8:
                            switch (c)
                            {
                                case 0: //shll2;
                                case 1: //shll8;
                                case 2:
                                    Implies(destRb, IsDeterministic(destRb)); //shll16
                                    break;
                            }
                            break;
                        case 9:
                            switch (c)
                            {
                                case 0: //shlr2;
                                case 1: //shlr8;
                                case 2:
                                    Implies(destRb, IsDeterministic(destRb)); //shlr16
                                    break;
                            }
                            break;
                        case 10:
                            switch (c)
                            {
                                case 0:
                                    Implies(Mach, IsDeterministic(destRb)); //ldsmach
                                    break;
                                case 1:
                                    Implies(Macl, IsDeterministic(destRb)); //ldsmacl
                                    break;
                                case 2:
                                    Implies(Pr, IsDeterministic(destRb)); //ldspr
                                    break;
                            }
                            break;
                        case 11:
                            switch (c)
                            {
                                case 0:
                                    if ((Deterministic & Constant) != 0 && !IsDeterministic(destRb)) return false;
                                    Implies(Pr, true);
                                    break; //jsr
                                case 1:
                                    return false; //tas;
                                case 2:
                                    if ((Deterministic & Constant) != 0 && !IsDeterministic(destRb)) return false;
                                    return DelayCheck(memory, unchecked(pc + 2)); //jmp
                            }
                            break;
                        case 14:
                            switch (c)
                            {
                                case 0:
                                    Implies(Sr, IsDeterministic(destRb)); //ldcsr
                                    break;
                                case 1:
                                    Implies(Gbr, IsDeterministic(destRb)); //ldcgbr
                                    break;
                                case 2:
                                    Implies(Vbr, IsDeterministic(destRb)); //ldcvrb
                                    break;
                            }
                            break;
                        case 15:
                            Implies(Mac, IsDeterministic(destRb) && IsDeterministic(destRc) && IsDeterministic(Mac)); //macw
                            break;
                    }
                    goto case 5;
                case 5:
                    Implies(destRb, IsDeterministic(destRc)); //movll4
                    break;
                case 6:
                    switch (d)
                    {
                        case 6: //movlp;
                        case 5: //movwp;
                        case 4:
                            return false;  //movbp;
                        case 0: //movbl;
                        case 1: //movwl;
                        case 2: //movll;
                        case 3: //mov;
                        case 7: //y_not;
                        case 8: //swapb;
                        case 11: //neg;
                        case 12: //extub;
                        case 13: //extuw;
                        case 14: //extsb;
                        case 15: //extsw;
                        case 9:
                            Implies(destRb, IsDeterministic(destRc)); //swapw;
                            break;
                        case 10:
                            Implies(destRb | SrT, IsDeterministic(destRb) && IsDeterministic(SrT)); //negc
                            break;
                    }
                    break;
                case 7:
                    Implies(destRb, IsDeterministic(destRb)); //addi;
                    break;
                case 8:
                    switch (b)
                    {
                        case 0: //movbs4;
                        case 1:
                            return false;  //movws4;
                        case 4: //movbl4;
                        case 5:
                            Implies(R0, IsDeterministic(destRc));   //movwl4;
                            break;
                        case 8:
                            Implies(SrT, IsDeterministic(R0)); //cmpim;
                            break;
                        case 9: //bt;
                        case 11: //bf;
                        case 13: //bts;
                        case 15:
                            return false; //bfs;
                    }
                    break;
                case 9:
                    Implies(destRb, true);  //movwi;
                    break;
                case 10:
                    return DelayCheck(memory, unchecked(pc + 2)); //bra;
                case 11:
                    Implies(Pr, true);
                    return DelayCheck(memory, unchecked(pc + 2)); //bsr;
                case 12:
                    switch (b)
                    {
                        case 0: //movbsg;
                        case 1: //movwsg;
                        case 2: //movlsg;
                        case 3:
                            return false;  //trapa;
                        case 4: //movblg;
                        case 5: //movwlg;
                        case 6:
                            Implies(R0, IsDeterministic(Gbr)); //movllg
                            break;
                        case 7:
                            Implies(R0, true);   //mova;
                            break;
                        case 8:
                            Implies(SrT, IsDeterministic(R0));  //tsti;
                            break;
                        case 9: //andi;
                        case 10: //xori;
                        case 11:
                            Implies(R0, IsDeterministic(R0)); //ori;
                            break;
                        case 12:
                            Implies(SrT, IsDeterministic(Gbr) && IsDeterministic(R0));   //tstm;
                            break;
                        case 13: //andm;
                        case 14: //xorm;
                        case 15:
                            return false;//orm;
                    }
                    break;
                case 13: //movli;
                case 14:
                    Implies(destRb, true);  //movi;
                    break;
            }
            return true;
        }
    }

    private static void CheckIdle(SaturnSh2State state, MemoryMap memory, uint cycles)
    {
        bool delayed = false;
        uint loopEnd;
        uint loopBegin;
        while (true)
        {
            state.Instruction = Sh2FetchList[(state.PC >> 20) & 0xFF](memory, state.PC);
            if (state.Instruction >> 12 == 8)
            {
                switch ((state.Instruction >> 8) & 0xF)
                {
                    case 13:
                        delayed = true;
                        goto case 9;
                    case 9:
                        if (state.SR.T != 1)
                        {
                            state.PC += 2;
                            state.Cycles++;
                            return;
                        }
                        loopEnd = state.PC;
                        loopBegin = state.PC = unchecked(state.PC + (uint)(unchecked((sbyte)state.Instruction) << 1) + 4);
                        state.Cycles += 3;
                        goto BranchingReached;

                    case 15:
                        delayed = true;
                        goto case 11;
                    case 11:
                        if (state.SR.T == 1)
                        {
                            state.PC += 2;
                            state.Cycles++;
                            return;
                        }
                        loopEnd = state.PC;
                        loopBegin = state.PC = unchecked(state.PC + (uint)(unchecked((sbyte)state.Instruction) << 1) + 4);
                        state.Cycles += 3;
                        goto BranchingReached;

                    default:
                        Sh2Opcodes[state.Instruction](state, memory);
                        break;
                }
            }
            else
                Sh2Opcodes[state.Instruction](state, memory);

            if (state.Cycles >= cycles)
                return;
        }

    BranchingReached:
        IdleRegisterMarkers markers = default;
        uint cyclesCheckEnd = unchecked(state.Cycles + MaxIdleCheckCycles);
        if (delayed)
        {
            state.Instruction = Sh2FetchList[(unchecked(loopEnd + 2) >> 20) & 0xFF](memory, unchecked(loopEnd + 2));
            Sh2Opcodes[state.Instruction](state, memory);
            state.PC = unchecked(state.PC - 2);
            if (!markers.CheckInstruction(memory, state.Instruction, 0))
                return;
        }

        while (state.PC != loopEnd)
        {
            uint pc = state.PC;
            state.Instruction = Sh2FetchList[(pc >> 20) & 0xFF](memory, pc);
            if (!markers.CheckInstruction(memory, state.Instruction, pc))
                return;
            Sh2Opcodes[state.Instruction](state, memory);
            if (state.Cycles >= cyclesCheckEnd)
                return;
        }

        uint branchPc = state.PC;
        state.Instruction = Sh2FetchList[(branchPc >> 20) & 0xFF](memory, branchPc);
        Sh2Opcodes[state.Instruction](state, memory);
        if (state.PC != loopBegin)
            return;

        markers.MarkUnchangedRegisters();
        if (delayed && !markers.CheckInstruction(memory, Sh2FetchList[(unchecked(loopEnd + 2) >> 20) & 0xFF](memory, unchecked(loopEnd + 2)), 0))
            return;

        while (state.PC != loopEnd)
        {
            uint pc = state.PC;
            state.Instruction = Sh2FetchList[(pc >> 20) & 0xFF](memory, pc);
            if (!markers.CheckInstruction(memory, state.Instruction, pc))
                return;
            Sh2Opcodes[state.Instruction](state, memory);
        }

        state.Instruction = Sh2FetchList[(branchPc >> 20) & 0xFF](memory, branchPc);
        Sh2Opcodes[state.Instruction](state, memory);
        if (state.PC != loopBegin)
            return;

        if (~markers.Deterministic == 0)
        {
            state.Cycles = cycles;
            state.IsIdle = true;
        }
    }

    private static void ParseIdle(SaturnSh2State state, MemoryMap memory, uint cycles)
    {
        while (true)
        {
            uint pc = state.PC;
            state.Instruction = Sh2FetchList[(pc >> 20) & 0xFF](memory, pc);
            if (state.Instruction >> 12 == 8)
            {
                switch ((state.Instruction >> 8) & 0xF)
                {
                    case 13:
                    case 9:
                        if (state.SR.T == 0)
                            state.IsIdle = false;
                        else
                            state.Cycles = cycles;
                        Sh2Opcodes[state.Instruction](state, memory);
                        return;

                    case 15:
                    case 11:
                        if (state.SR.T != 0)
                            state.IsIdle = false;
                        else
                            state.Cycles = cycles;
                        Sh2Opcodes[state.Instruction](state, memory);
                        return;
                }
            }
            Sh2Opcodes[state.Instruction](state, memory);
        }
    }
}

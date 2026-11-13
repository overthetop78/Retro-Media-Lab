namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cpu;

internal sealed partial class M68k<TBus> where TBus : class, IM68kBus
{
    private uint m68ki_get_ea_pcdi()
    {
        unchecked
        {
            uint old_pc = REG_PC;
            return (old_pc + unchecked((uint)(MAKE_INT_16(m68ki_read_imm_16()))));
        }
    }
    private uint m68ki_get_ea_pcix()
    {
        unchecked
        {
            return m68ki_get_ea_ix(REG_PC);
        }
    }
    private uint m68ki_get_ea_ix(uint An)
    {
        unchecked
        {
            uint extension = m68ki_read_imm_16();
            uint Xn = unchecked((uint)(0));
            Xn = _registers[(int)((extension >> 12))];
            if ((!(BIT_B(extension) != 0))) Xn = unchecked((uint)(MAKE_INT_16(Xn)));
            return ((An + Xn) + unchecked((uint)(MAKE_INT_8(extension))));
        }
    }
    private uint OPER_AY_AI_8()
    {
        unchecked
        {
            uint ea = EA_AY_AI_8();
            return m68ki_read_8(ea);
        }
    }
    private uint OPER_AY_AI_16()
    {
        unchecked
        {
            uint ea = EA_AY_AI_16();
            return m68ki_read_16(ea);
        }
    }
    private uint OPER_AY_AI_32()
    {
        unchecked
        {
            uint ea = EA_AY_AI_32();
            return m68ki_read_32(ea);
        }
    }
    private uint OPER_AY_PI_8()
    {
        unchecked
        {
            uint ea = EA_AY_PI_8();
            return m68ki_read_8(ea);
        }
    }
    private uint OPER_AY_PI_16()
    {
        unchecked
        {
            uint ea = EA_AY_PI_16();
            return m68ki_read_16(ea);
        }
    }
    private uint OPER_AY_PI_32()
    {
        unchecked
        {
            uint ea = EA_AY_PI_32();
            return m68ki_read_32(ea);
        }
    }
    private uint OPER_AY_PD_8()
    {
        unchecked
        {
            uint ea = EA_AY_PD_8();
            return m68ki_read_8(ea);
        }
    }
    private uint OPER_AY_PD_16()
    {
        unchecked
        {
            uint ea = EA_AY_PD_16();
            return m68ki_read_16(ea);
        }
    }
    private uint OPER_AY_PD_32()
    {
        unchecked
        {
            uint ea = EA_AY_PD_32();
            return m68ki_read_32(ea);
        }
    }
    private uint OPER_AY_DI_8()
    {
        unchecked
        {
            uint ea = EA_AY_DI_8();
            return m68ki_read_8(ea);
        }
    }
    private uint OPER_AY_DI_16()
    {
        unchecked
        {
            uint ea = EA_AY_DI_16();
            return m68ki_read_16(ea);
        }
    }
    private uint OPER_AY_DI_32()
    {
        unchecked
        {
            uint ea = EA_AY_DI_32();
            return m68ki_read_32(ea);
        }
    }
    private uint OPER_AY_IX_8()
    {
        unchecked
        {
            uint ea = EA_AY_IX_8();
            return m68ki_read_8(ea);
        }
    }
    private uint OPER_AY_IX_16()
    {
        unchecked
        {
            uint ea = EA_AY_IX_16();
            return m68ki_read_16(ea);
        }
    }
    private uint OPER_AY_IX_32()
    {
        unchecked
        {
            uint ea = EA_AY_IX_32();
            return m68ki_read_32(ea);
        }
    }
    private uint OPER_AX_AI_8()
    {
        unchecked
        {
            uint ea = EA_AX_AI_8();
            return m68ki_read_8(ea);
        }
    }
    private uint OPER_AX_AI_16()
    {
        unchecked
        {
            uint ea = EA_AX_AI_16();
            return m68ki_read_16(ea);
        }
    }
    private uint OPER_AX_AI_32()
    {
        unchecked
        {
            uint ea = EA_AX_AI_32();
            return m68ki_read_32(ea);
        }
    }
    private uint OPER_AX_PI_8()
    {
        unchecked
        {
            uint ea = EA_AX_PI_8();
            return m68ki_read_8(ea);
        }
    }
    private uint OPER_AX_PI_16()
    {
        unchecked
        {
            uint ea = EA_AX_PI_16();
            return m68ki_read_16(ea);
        }
    }
    private uint OPER_AX_PI_32()
    {
        unchecked
        {
            uint ea = EA_AX_PI_32();
            return m68ki_read_32(ea);
        }
    }
    private uint OPER_AX_PD_8()
    {
        unchecked
        {
            uint ea = EA_AX_PD_8();
            return m68ki_read_8(ea);
        }
    }
    private uint OPER_AX_PD_16()
    {
        unchecked
        {
            uint ea = EA_AX_PD_16();
            return m68ki_read_16(ea);
        }
    }
    private uint OPER_AX_PD_32()
    {
        unchecked
        {
            uint ea = EA_AX_PD_32();
            return m68ki_read_32(ea);
        }
    }
    private uint OPER_AX_DI_8()
    {
        unchecked
        {
            uint ea = EA_AX_DI_8();
            return m68ki_read_8(ea);
        }
    }
    private uint OPER_AX_DI_16()
    {
        unchecked
        {
            uint ea = EA_AX_DI_16();
            return m68ki_read_16(ea);
        }
    }
    private uint OPER_AX_DI_32()
    {
        unchecked
        {
            uint ea = EA_AX_DI_32();
            return m68ki_read_32(ea);
        }
    }
    private uint OPER_AX_IX_8()
    {
        unchecked
        {
            uint ea = EA_AX_IX_8();
            return m68ki_read_8(ea);
        }
    }
    private uint OPER_AX_IX_16()
    {
        unchecked
        {
            uint ea = EA_AX_IX_16();
            return m68ki_read_16(ea);
        }
    }
    private uint OPER_AX_IX_32()
    {
        unchecked
        {
            uint ea = EA_AX_IX_32();
            return m68ki_read_32(ea);
        }
    }
    private uint OPER_A7_PI_8()
    {
        unchecked
        {
            uint ea = EA_A7_PI_8();
            return m68ki_read_8(ea);
        }
    }
    private uint OPER_A7_PD_8()
    {
        unchecked
        {
            uint ea = EA_A7_PD_8();
            return m68ki_read_8(ea);
        }
    }
    private uint OPER_AW_8()
    {
        unchecked
        {
            uint ea = EA_AW_8();
            return m68ki_read_8(ea);
        }
    }
    private uint OPER_AW_16()
    {
        unchecked
        {
            uint ea = EA_AW_16();
            return m68ki_read_16(ea);
        }
    }
    private uint OPER_AW_32()
    {
        unchecked
        {
            uint ea = EA_AW_32();
            return m68ki_read_32(ea);
        }
    }
    private uint OPER_AL_8()
    {
        unchecked
        {
            uint ea = EA_AL_8();
            return m68ki_read_8(ea);
        }
    }
    private uint OPER_AL_16()
    {
        unchecked
        {
            uint ea = EA_AL_16();
            return m68ki_read_16(ea);
        }
    }
    private uint OPER_AL_32()
    {
        unchecked
        {
            uint ea = EA_AL_32();
            return m68ki_read_32(ea);
        }
    }
    private uint OPER_PCDI_8()
    {
        unchecked
        {
            uint ea = EA_PCDI_8();
            return m68ki_read_pcrel_8(ea);
        }
    }
    private uint OPER_PCDI_16()
    {
        unchecked
        {
            uint ea = EA_PCDI_16();
            return m68ki_read_pcrel_16(ea);
        }
    }
    private uint OPER_PCDI_32()
    {
        unchecked
        {
            uint ea = EA_PCDI_32();
            return m68ki_read_pcrel_32(ea);
        }
    }
    private uint OPER_PCIX_8()
    {
        unchecked
        {
            uint ea = EA_PCIX_8();
            return m68ki_read_pcrel_8(ea);
        }
    }
    private uint OPER_PCIX_16()
    {
        unchecked
        {
            uint ea = EA_PCIX_16();
            return m68ki_read_pcrel_16(ea);
        }
    }
    private uint OPER_PCIX_32()
    {
        unchecked
        {
            uint ea = EA_PCIX_32();
            return m68ki_read_pcrel_32(ea);
        }
    }
    private void m68ki_push_16(uint value)
    {
        unchecked
        {
            REG_SP = MASK_OUT_ABOVE_32((REG_SP - unchecked((uint)(2))));
            m68ki_write_16(REG_SP, value);
        }
    }
    private void m68ki_push_32(uint value)
    {
        unchecked
        {
            REG_SP = MASK_OUT_ABOVE_32((REG_SP - unchecked((uint)(4))));
            m68ki_write_32(REG_SP, value);
        }
    }
    private uint m68ki_pull_16()
    {
        unchecked
        {
            uint sp = REG_SP;
            REG_SP = MASK_OUT_ABOVE_32((REG_SP + unchecked((uint)(2))));
            return m68ki_read_16(sp);
        }
    }
    private uint m68ki_pull_32()
    {
        unchecked
        {
            uint sp = REG_SP;
            REG_SP = MASK_OUT_ABOVE_32((REG_SP + unchecked((uint)(4))));
            return m68ki_read_32(sp);
        }
    }
    private void m68ki_jump(uint new_pc)
    {
        unchecked
        {
            REG_PC = new_pc;
        }
    }
    private void m68ki_jump_vector(uint vector)
    {
        unchecked
        {
            REG_PC = m68ki_read_32((vector << 2));
        }
    }
    private void m68ki_branch_8(uint offset)
    {
        unchecked
        {
            REG_PC += unchecked((uint)(MAKE_INT_8(offset)));
        }
    }
    private void m68ki_branch_16(uint offset)
    {
        unchecked
        {
            REG_PC += unchecked((uint)(MAKE_INT_16(offset)));
        }
    }
    private void m68ki_branch_32(uint offset)
    {
        unchecked
        {
            REG_PC += offset;
        }
    }
    private void m68ki_set_s_flag(uint value)
    {
        unchecked
        {
            REG_SP_BASE[(int)(FLAG_S)] = REG_SP;
            FLAG_S = value;
            REG_SP = REG_SP_BASE[(int)(FLAG_S)];
        }
    }
    private void m68ki_set_ccr(uint value)
    {
        unchecked
        {
            FLAG_X = (BIT_4(value) << 4);
            FLAG_N = (BIT_3(value) << 4);
            FLAG_Z = ((!(BIT_2(value) != 0)) ? (uint)1 : (uint)0);
            FLAG_V = (BIT_1(value) << 6);
            FLAG_C = (BIT_0(value) << 8);
        }
    }
    private void m68ki_set_sr(uint value)
    {
        unchecked
        {
            FLAG_T1 = BIT_F(value);
            FLAG_INT_MASK = (value & unchecked((uint)(1792)));
            m68ki_set_ccr(value);
            m68ki_set_s_flag(((value >> 11) & unchecked((uint)(4))));
            m68ki_check_interrupts();
        }
    }
    private uint m68ki_init_exception()
    {
        unchecked
        {
            uint sr = m68ki_get_sr();
            FLAG_T1 = unchecked((uint)(0));
            m68ki_set_s_flag(SFLAG_SET);
            return sr;
        }
    }
    private void m68ki_stack_frame_3word(uint pc, uint sr)
    {
        unchecked
        {
            m68ki_push_32(pc);
            m68ki_push_16(sr);
        }
    }
    private void m68ki_stack_frame_buserr(uint sr)
    {
        unchecked
        {
            m68ki_push_32(REG_PC);
            m68ki_push_16(sr);
            m68ki_push_16(REG_IR);
            m68ki_push_32(_state.aerr_address);
            m68ki_push_16(((_state.aerr_write_mode | CPU_INSTR_MODE) | _state.aerr_fc));
        }
    }
    private void m68ki_exception_trap(uint vector)
    {
        unchecked
        {
            uint sr = m68ki_init_exception();
            m68ki_stack_frame_3word(REG_PC, sr);
            m68ki_jump_vector(vector);
            USE_CYCLES(unchecked((long)(CYC_EXCEPTION[(int)(vector)])));
        }
    }
    private void m68ki_exception_trapN(uint vector)
    {
        unchecked
        {
            uint sr = m68ki_init_exception();
            m68ki_stack_frame_3word(REG_PC, sr);
            m68ki_jump_vector(vector);
            USE_CYCLES(unchecked((long)(CYC_EXCEPTION[(int)(vector)])));
        }
    }
    private void m68ki_exception_privilege_violation()
    {
        unchecked
        {
            uint sr = m68ki_init_exception();
            if (_addressErrors) CPU_INSTR_MODE = INSTRUCTION_NO;
            m68ki_stack_frame_3word((REG_PC - unchecked((uint)(2))), sr);
            m68ki_jump_vector(EXCEPTION_PRIVILEGE_VIOLATION);
            USE_CYCLES(unchecked((long)((unchecked((int)(CYC_EXCEPTION[(int)(EXCEPTION_PRIVILEGE_VIOLATION)])) - unchecked((int)(CYC_INSTRUCTION[(int)(REG_IR)]))))));
        }
    }
    private void m68ki_exception_1010()
    {
        unchecked
        {
            uint sr = m68ki_init_exception();
            m68ki_stack_frame_3word((REG_PC - unchecked((uint)(2))), sr);
            m68ki_jump_vector(EXCEPTION_1010);
            USE_CYCLES(unchecked((long)((unchecked((int)(CYC_EXCEPTION[(int)(EXCEPTION_1010)])) - unchecked((int)(CYC_INSTRUCTION[(int)(REG_IR)]))))));
        }
    }
    private void m68ki_exception_1111()
    {
        unchecked
        {
            uint sr = m68ki_init_exception();
            m68ki_stack_frame_3word((REG_PC - unchecked((uint)(2))), sr);
            m68ki_jump_vector(EXCEPTION_1111);
            USE_CYCLES(unchecked((long)((unchecked((int)(CYC_EXCEPTION[(int)(EXCEPTION_1111)])) - unchecked((int)(CYC_INSTRUCTION[(int)(REG_IR)]))))));
        }
    }
    private void m68ki_exception_illegal()
    {
        unchecked
        {
            uint sr = m68ki_init_exception();
            if (_addressErrors) CPU_INSTR_MODE = INSTRUCTION_NO;
            m68ki_stack_frame_3word((REG_PC - unchecked((uint)(2))), sr);
            m68ki_jump_vector(EXCEPTION_ILLEGAL_INSTRUCTION);
            USE_CYCLES(unchecked((long)((unchecked((int)(CYC_EXCEPTION[(int)(EXCEPTION_ILLEGAL_INSTRUCTION)])) - unchecked((int)(CYC_INSTRUCTION[(int)(REG_IR)]))))));
        }
    }
    private void m68ki_exception_address_error()
    {
        unchecked
        {
            uint sr = m68ki_init_exception();
            if ((CPU_RUN_MODE == RUN_MODE_BERR_AERR_RESET)) {
            CPU_STOPPED = STOP_LEVEL_HALT;
            SET_CYCLES((_state.cycle_end - unchecked((uint)(CYC_INSTRUCTION[(int)(REG_IR)]))));
            return;
            }
            CPU_RUN_MODE = RUN_MODE_BERR_AERR_RESET;
            m68ki_stack_frame_buserr(sr);
            m68ki_jump_vector(EXCEPTION_ADDRESS_ERROR);
            USE_CYCLES(unchecked((long)((unchecked((int)(CYC_EXCEPTION[(int)(EXCEPTION_ADDRESS_ERROR)])) - unchecked((int)(CYC_INSTRUCTION[(int)(REG_IR)]))))));
        }
    }
    private void m68ki_exception_interrupt(uint int_level)
    {
        unchecked
        {
            uint vector = default;
            uint sr = default;
            uint new_pc = default;
            if (_addressErrors) CPU_INSTR_MODE = INSTRUCTION_NO;
            CPU_STOPPED &= (STOP_LEVEL_HALT | STOP_LEVEL_WAIT);
            if ((CPU_STOPPED != 0)) return;
            vector = (EXCEPTION_INTERRUPT_AUTOVECTOR + int_level);
            sr = m68ki_init_exception();
            FLAG_INT_MASK = (int_level << 8);
            m68ki_int_ack(int_level);
            new_pc = m68ki_read_32((vector << 2));
            if ((new_pc == unchecked((uint)(0)))) new_pc = m68ki_read_32((EXCEPTION_UNINITIALIZED_INTERRUPT << 2));
            m68ki_stack_frame_3word(REG_PC, sr);
            m68ki_jump(new_pc);
            USE_CYCLES(unchecked((long)(m68ki_cycle_interrupts[(int)(((_state.cycles / MUL) % 10))])));
        }
    }
    private void m68ki_check_interrupts()
    {
        unchecked
        {
            if ((CPU_INT_LEVEL > FLAG_INT_MASK)) m68ki_exception_interrupt((CPU_INT_LEVEL >> 8));
        }
    }
    private void UseDivuCycles(uint dst, uint src)
    {
        unchecked
        {
            int i = default;
            uint mcycles = unchecked((uint)((76 * MUL)));
            src <<= 16;
            for (i = 0; (i < 15); i++) {
            if ((unchecked((int)(dst)) < 0)) {
            dst <<= 1;
            dst -= src;
            } else {
            dst <<= 1;
            mcycles += unchecked((uint)((4 * MUL)));
            if ((dst >= src)) {
            dst -= src;
            mcycles -= unchecked((uint)((2 * MUL)));
            }
            }
            }
            USE_CYCLES(unchecked((long)(mcycles)));
            SKIP_BUS_REFRESH();
        }
    }
    private void UseDivsCycles(int dst, short src)
    {
        unchecked
        {
            uint mcycles = unchecked((uint)((12 * MUL)));
            if ((dst < 0)) mcycles += unchecked((uint)((2 * MUL)));
            if (((abs(dst) >> 16) < abs(unchecked((int)(src))))) {
            int i = default;
            uint quotient = unchecked((uint)((abs(dst) / abs(unchecked((int)(src))))));
            mcycles += unchecked((uint)((110 * MUL)));
            if ((unchecked((int)(src)) >= 0)) {
            if ((dst >= 0)) mcycles -= unchecked((uint)((2 * MUL))); else mcycles += unchecked((uint)((2 * MUL)));
            }
            for (i = 0; (i < 15); i++) {
            quotient >>= 1;
            if ((!((quotient & unchecked((uint)(1))) != 0))) mcycles += unchecked((uint)((2 * MUL)));
            }
            } else {
            mcycles += unchecked((uint)((4 * MUL)));
            }
            USE_CYCLES(unchecked((long)(mcycles)));
            SKIP_BUS_REFRESH();
        }
    }
    private void UseMuluCycles(ushort src)
    {
        unchecked
        {
            uint mcycles = unchecked((uint)((38 * MUL)));
            while ((src != 0)) {
            if (((unchecked((int)(src)) & 1) != 0)) mcycles += unchecked((uint)((2 * MUL)));
            src >>= 1;
            }
            USE_CYCLES(unchecked((long)(mcycles)));
        }
    }
    private void UseMulsCycles(short src)
    {
        unchecked
        {
            uint mcycles = unchecked((uint)((38 * MUL)));
            int tmp = (unchecked((int)(src)) << 1);
            tmp = ((tmp ^ unchecked((int)(src))) & 65535);
            while ((tmp != 0)) {
            if (((tmp & 1) != 0)) mcycles += unchecked((uint)((2 * MUL)));
            tmp >>= 1;
            }
            USE_CYCLES(unchecked((long)(mcycles)));
        }
    }
    private void m68k_op_1010()
    {
        unchecked
        {
            m68ki_exception_1010();
        }
    }
    private void m68k_op_1111()
    {
        unchecked
        {
            m68ki_exception_1111();
        }
    }
    private void m68k_op_abcd_8_rr()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = DY;
            uint dst = r_dst;
            uint res = ((LOW_NIBBLE(src) + LOW_NIBBLE(dst)) + XFLAG_AS_1());
            uint corf = unchecked((uint)(0));
            if ((res > unchecked((uint)(9)))) corf = unchecked((uint)(6));
            res += (HIGH_NIBBLE(src) + HIGH_NIBBLE(dst));
            FLAG_V = (~res);
            res += corf;
            FLAG_X = (FLAG_C = unchecked((uint)((((res > unchecked((uint)(159))) ? (int)1 : (int)0) << 8))));
            if ((FLAG_C != 0)) res -= unchecked((uint)(160));
            FLAG_V &= res;
            FLAG_N = NFLAG_8(res);
            res = MASK_OUT_ABOVE_8(res);
            FLAG_Z |= res;
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
        }
    }
    private void m68k_op_abcd_8_mm_ax7()
    {
        unchecked
        {
            uint src = OPER_AY_PD_8();
            uint ea = EA_A7_PD_8();
            uint dst = m68ki_read_8(ea);
            uint res = ((LOW_NIBBLE(src) + LOW_NIBBLE(dst)) + XFLAG_AS_1());
            uint corf = unchecked((uint)(0));
            if ((res > unchecked((uint)(9)))) corf = unchecked((uint)(6));
            res += (HIGH_NIBBLE(src) + HIGH_NIBBLE(dst));
            FLAG_V = (~res);
            res += corf;
            FLAG_X = (FLAG_C = unchecked((uint)((((res > unchecked((uint)(159))) ? (int)1 : (int)0) << 8))));
            if ((FLAG_C != 0)) res -= unchecked((uint)(160));
            FLAG_V &= res;
            FLAG_N = NFLAG_8(res);
            res = MASK_OUT_ABOVE_8(res);
            FLAG_Z |= res;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_abcd_8_mm_ay7()
    {
        unchecked
        {
            uint src = OPER_A7_PD_8();
            uint ea = EA_AX_PD_8();
            uint dst = m68ki_read_8(ea);
            uint res = ((LOW_NIBBLE(src) + LOW_NIBBLE(dst)) + XFLAG_AS_1());
            uint corf = unchecked((uint)(0));
            if ((res > unchecked((uint)(9)))) corf = unchecked((uint)(6));
            res += (HIGH_NIBBLE(src) + HIGH_NIBBLE(dst));
            FLAG_V = (~res);
            res += corf;
            FLAG_X = (FLAG_C = unchecked((uint)((((res > unchecked((uint)(159))) ? (int)1 : (int)0) << 8))));
            if ((FLAG_C != 0)) res -= unchecked((uint)(160));
            FLAG_V &= res;
            FLAG_N = NFLAG_8(res);
            res = MASK_OUT_ABOVE_8(res);
            FLAG_Z |= res;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_abcd_8_mm_axy7()
    {
        unchecked
        {
            uint src = OPER_A7_PD_8();
            uint ea = EA_A7_PD_8();
            uint dst = m68ki_read_8(ea);
            uint res = ((LOW_NIBBLE(src) + LOW_NIBBLE(dst)) + XFLAG_AS_1());
            uint corf = unchecked((uint)(0));
            if ((res > unchecked((uint)(9)))) corf = unchecked((uint)(6));
            res += (HIGH_NIBBLE(src) + HIGH_NIBBLE(dst));
            FLAG_V = (~res);
            res += corf;
            FLAG_X = (FLAG_C = unchecked((uint)((((res > unchecked((uint)(159))) ? (int)1 : (int)0) << 8))));
            if ((FLAG_C != 0)) res -= unchecked((uint)(160));
            FLAG_V &= res;
            FLAG_N = NFLAG_8(res);
            res = MASK_OUT_ABOVE_8(res);
            FLAG_Z |= res;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_abcd_8_mm()
    {
        unchecked
        {
            uint src = OPER_AY_PD_8();
            uint ea = EA_AX_PD_8();
            uint dst = m68ki_read_8(ea);
            uint res = ((LOW_NIBBLE(src) + LOW_NIBBLE(dst)) + XFLAG_AS_1());
            uint corf = unchecked((uint)(0));
            if ((res > unchecked((uint)(9)))) corf = unchecked((uint)(6));
            res += (HIGH_NIBBLE(src) + HIGH_NIBBLE(dst));
            FLAG_V = (~res);
            res += corf;
            FLAG_X = (FLAG_C = unchecked((uint)((((res > unchecked((uint)(159))) ? (int)1 : (int)0) << 8))));
            if ((FLAG_C != 0)) res -= unchecked((uint)(160));
            FLAG_V &= res;
            FLAG_N = NFLAG_8(res);
            res = MASK_OUT_ABOVE_8(res);
            FLAG_Z |= res;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_add_8_er_d()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = MASK_OUT_ABOVE_8(DY);
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_add_8_er_ai()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_AI_8();
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_add_8_er_pi()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_PI_8();
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_add_8_er_pi7()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_A7_PI_8();
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_add_8_er_pd()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_PD_8();
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_add_8_er_pd7()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_A7_PD_8();
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_add_8_er_di()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_DI_8();
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_add_8_er_ix()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_IX_8();
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_add_8_er_aw()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AW_8();
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_add_8_er_al()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AL_8();
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_add_8_er_pcdi()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_PCDI_8();
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_add_8_er_pcix()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_PCIX_8();
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_add_8_er_i()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_I_8();
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_add_16_er_d()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = MASK_OUT_ABOVE_16(DY);
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_add_16_er_a()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = MASK_OUT_ABOVE_16(AY);
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_add_16_er_ai()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_AI_16();
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_add_16_er_pi()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_PI_16();
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_add_16_er_pd()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_PD_16();
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_add_16_er_di()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_DI_16();
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_add_16_er_ix()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_IX_16();
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_add_16_er_aw()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AW_16();
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_add_16_er_al()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AL_16();
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_add_16_er_pcdi()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_PCDI_16();
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_add_16_er_pcix()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_PCIX_16();
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_add_16_er_i()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_I_16();
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_add_32_er_d()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = DY;
            uint dst = r_dst;
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            r_dst = FLAG_Z;
        }
    }
    private void m68k_op_add_32_er_a()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = AY;
            uint dst = r_dst;
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            r_dst = FLAG_Z;
        }
    }
    private void m68k_op_add_32_er_ai()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_AI_32();
            uint dst = r_dst;
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            r_dst = FLAG_Z;
        }
    }
    private void m68k_op_add_32_er_pi()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_PI_32();
            uint dst = r_dst;
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            r_dst = FLAG_Z;
        }
    }
    private void m68k_op_add_32_er_pd()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_PD_32();
            uint dst = r_dst;
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            r_dst = FLAG_Z;
        }
    }
    private void m68k_op_add_32_er_di()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_DI_32();
            uint dst = r_dst;
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            r_dst = FLAG_Z;
        }
    }
    private void m68k_op_add_32_er_ix()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_IX_32();
            uint dst = r_dst;
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            r_dst = FLAG_Z;
        }
    }
    private void m68k_op_add_32_er_aw()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AW_32();
            uint dst = r_dst;
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            r_dst = FLAG_Z;
        }
    }
    private void m68k_op_add_32_er_al()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AL_32();
            uint dst = r_dst;
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            r_dst = FLAG_Z;
        }
    }
    private void m68k_op_add_32_er_pcdi()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_PCDI_32();
            uint dst = r_dst;
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            r_dst = FLAG_Z;
        }
    }
    private void m68k_op_add_32_er_pcix()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_PCIX_32();
            uint dst = r_dst;
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            r_dst = FLAG_Z;
        }
    }
    private void m68k_op_add_32_er_i()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_I_32();
            uint dst = r_dst;
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            r_dst = FLAG_Z;
        }
    }
    private void m68k_op_add_8_re_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_8();
            uint src = MASK_OUT_ABOVE_8(DX);
            uint dst = m68ki_read_8(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_add_8_re_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_8();
            uint src = MASK_OUT_ABOVE_8(DX);
            uint dst = m68ki_read_8(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_add_8_re_pi7()
    {
        unchecked
        {
            uint ea = EA_A7_PI_8();
            uint src = MASK_OUT_ABOVE_8(DX);
            uint dst = m68ki_read_8(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_add_8_re_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_8();
            uint src = MASK_OUT_ABOVE_8(DX);
            uint dst = m68ki_read_8(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_add_8_re_pd7()
    {
        unchecked
        {
            uint ea = EA_A7_PD_8();
            uint src = MASK_OUT_ABOVE_8(DX);
            uint dst = m68ki_read_8(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_add_8_re_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_8();
            uint src = MASK_OUT_ABOVE_8(DX);
            uint dst = m68ki_read_8(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_add_8_re_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_8();
            uint src = MASK_OUT_ABOVE_8(DX);
            uint dst = m68ki_read_8(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_add_8_re_aw()
    {
        unchecked
        {
            uint ea = EA_AW_8();
            uint src = MASK_OUT_ABOVE_8(DX);
            uint dst = m68ki_read_8(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_add_8_re_al()
    {
        unchecked
        {
            uint ea = EA_AL_8();
            uint src = MASK_OUT_ABOVE_8(DX);
            uint dst = m68ki_read_8(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_add_16_re_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_16();
            uint src = MASK_OUT_ABOVE_16(DX);
            uint dst = m68ki_read_16(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_add_16_re_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_16();
            uint src = MASK_OUT_ABOVE_16(DX);
            uint dst = m68ki_read_16(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_add_16_re_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_16();
            uint src = MASK_OUT_ABOVE_16(DX);
            uint dst = m68ki_read_16(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_add_16_re_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_16();
            uint src = MASK_OUT_ABOVE_16(DX);
            uint dst = m68ki_read_16(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_add_16_re_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_16();
            uint src = MASK_OUT_ABOVE_16(DX);
            uint dst = m68ki_read_16(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_add_16_re_aw()
    {
        unchecked
        {
            uint ea = EA_AW_16();
            uint src = MASK_OUT_ABOVE_16(DX);
            uint dst = m68ki_read_16(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_add_16_re_al()
    {
        unchecked
        {
            uint ea = EA_AL_16();
            uint src = MASK_OUT_ABOVE_16(DX);
            uint dst = m68ki_read_16(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_add_32_re_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_32();
            uint src = DX;
            uint dst = m68ki_read_32(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_add_32_re_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_32();
            uint src = DX;
            uint dst = m68ki_read_32(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_add_32_re_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_32();
            uint src = DX;
            uint dst = m68ki_read_32(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_add_32_re_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_32();
            uint src = DX;
            uint dst = m68ki_read_32(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_add_32_re_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_32();
            uint src = DX;
            uint dst = m68ki_read_32(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_add_32_re_aw()
    {
        unchecked
        {
            uint ea = EA_AW_32();
            uint src = DX;
            uint dst = m68ki_read_32(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_add_32_re_al()
    {
        unchecked
        {
            uint ea = EA_AL_32();
            uint src = DX;
            uint dst = m68ki_read_32(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_adda_16_d()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            r_dst = MASK_OUT_ABOVE_32((r_dst + unchecked((uint)(MAKE_INT_16(DY)))));
        }
    }
    private void m68k_op_adda_16_a()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            r_dst = MASK_OUT_ABOVE_32((r_dst + unchecked((uint)(MAKE_INT_16(AY)))));
        }
    }
    private void m68k_op_adda_16_ai()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = unchecked((uint)(MAKE_INT_16(OPER_AY_AI_16())));
            r_dst = MASK_OUT_ABOVE_32((r_dst + src));
        }
    }
    private void m68k_op_adda_16_pi()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = unchecked((uint)(MAKE_INT_16(OPER_AY_PI_16())));
            r_dst = MASK_OUT_ABOVE_32((r_dst + src));
        }
    }
    private void m68k_op_adda_16_pd()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = unchecked((uint)(MAKE_INT_16(OPER_AY_PD_16())));
            r_dst = MASK_OUT_ABOVE_32((r_dst + src));
        }
    }
    private void m68k_op_adda_16_di()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = unchecked((uint)(MAKE_INT_16(OPER_AY_DI_16())));
            r_dst = MASK_OUT_ABOVE_32((r_dst + src));
        }
    }
    private void m68k_op_adda_16_ix()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = unchecked((uint)(MAKE_INT_16(OPER_AY_IX_16())));
            r_dst = MASK_OUT_ABOVE_32((r_dst + src));
        }
    }
    private void m68k_op_adda_16_aw()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = unchecked((uint)(MAKE_INT_16(OPER_AW_16())));
            r_dst = MASK_OUT_ABOVE_32((r_dst + src));
        }
    }
    private void m68k_op_adda_16_al()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = unchecked((uint)(MAKE_INT_16(OPER_AL_16())));
            r_dst = MASK_OUT_ABOVE_32((r_dst + src));
        }
    }
    private void m68k_op_adda_16_pcdi()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = unchecked((uint)(MAKE_INT_16(OPER_PCDI_16())));
            r_dst = MASK_OUT_ABOVE_32((r_dst + src));
        }
    }
    private void m68k_op_adda_16_pcix()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = unchecked((uint)(MAKE_INT_16(OPER_PCIX_16())));
            r_dst = MASK_OUT_ABOVE_32((r_dst + src));
        }
    }
    private void m68k_op_adda_16_i()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = unchecked((uint)(MAKE_INT_16(OPER_I_16())));
            r_dst = MASK_OUT_ABOVE_32((r_dst + src));
        }
    }
    private void m68k_op_adda_32_d()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            r_dst = MASK_OUT_ABOVE_32((r_dst + DY));
        }
    }
    private void m68k_op_adda_32_a()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            r_dst = MASK_OUT_ABOVE_32((r_dst + AY));
        }
    }
    private void m68k_op_adda_32_ai()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            r_dst = MASK_OUT_ABOVE_32((OPER_AY_AI_32() + r_dst));
        }
    }
    private void m68k_op_adda_32_pi()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            r_dst = MASK_OUT_ABOVE_32((OPER_AY_PI_32() + r_dst));
        }
    }
    private void m68k_op_adda_32_pd()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            r_dst = MASK_OUT_ABOVE_32((OPER_AY_PD_32() + r_dst));
        }
    }
    private void m68k_op_adda_32_di()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            r_dst = MASK_OUT_ABOVE_32((OPER_AY_DI_32() + r_dst));
        }
    }
    private void m68k_op_adda_32_ix()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            r_dst = MASK_OUT_ABOVE_32((OPER_AY_IX_32() + r_dst));
        }
    }
    private void m68k_op_adda_32_aw()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            r_dst = MASK_OUT_ABOVE_32((OPER_AW_32() + r_dst));
        }
    }
    private void m68k_op_adda_32_al()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            r_dst = MASK_OUT_ABOVE_32((OPER_AL_32() + r_dst));
        }
    }
    private void m68k_op_adda_32_pcdi()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            r_dst = MASK_OUT_ABOVE_32((OPER_PCDI_32() + r_dst));
        }
    }
    private void m68k_op_adda_32_pcix()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            r_dst = MASK_OUT_ABOVE_32((OPER_PCIX_32() + r_dst));
        }
    }
    private void m68k_op_adda_32_i()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            r_dst = MASK_OUT_ABOVE_32((OPER_I_32() + r_dst));
        }
    }
    private void m68k_op_addi_8_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint src = OPER_I_8();
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_addi_8_ai()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AY_AI_8();
            uint dst = m68ki_read_8(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_addi_8_pi()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AY_PI_8();
            uint dst = m68ki_read_8(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_addi_8_pi7()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_A7_PI_8();
            uint dst = m68ki_read_8(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_addi_8_pd()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AY_PD_8();
            uint dst = m68ki_read_8(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_addi_8_pd7()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_A7_PD_8();
            uint dst = m68ki_read_8(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_addi_8_di()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AY_DI_8();
            uint dst = m68ki_read_8(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_addi_8_ix()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AY_IX_8();
            uint dst = m68ki_read_8(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_addi_8_aw()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AW_8();
            uint dst = m68ki_read_8(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_addi_8_al()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AL_8();
            uint dst = m68ki_read_8(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_addi_16_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint src = OPER_I_16();
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_addi_16_ai()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AY_AI_16();
            uint dst = m68ki_read_16(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_addi_16_pi()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AY_PI_16();
            uint dst = m68ki_read_16(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_addi_16_pd()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AY_PD_16();
            uint dst = m68ki_read_16(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_addi_16_di()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AY_DI_16();
            uint dst = m68ki_read_16(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_addi_16_ix()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AY_IX_16();
            uint dst = m68ki_read_16(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_addi_16_aw()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AW_16();
            uint dst = m68ki_read_16(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_addi_16_al()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AL_16();
            uint dst = m68ki_read_16(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_addi_32_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint src = OPER_I_32();
            uint dst = r_dst;
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            r_dst = FLAG_Z;
        }
    }
    private void m68k_op_addi_32_ai()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AY_AI_32();
            uint dst = m68ki_read_32(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_addi_32_pi()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AY_PI_32();
            uint dst = m68ki_read_32(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_addi_32_pd()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AY_PD_32();
            uint dst = m68ki_read_32(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_addi_32_di()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AY_DI_32();
            uint dst = m68ki_read_32(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_addi_32_ix()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AY_IX_32();
            uint dst = m68ki_read_32(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_addi_32_aw()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AW_32();
            uint dst = m68ki_read_32(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_addi_32_al()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AL_32();
            uint dst = m68ki_read_32(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_addq_8_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
            _state.poll_detected = unchecked((uint)(0));
        }
    }
    private void m68k_op_addq_8_ai()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_AI_8();
            uint dst = m68ki_read_8(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_addq_8_pi()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_PI_8();
            uint dst = m68ki_read_8(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_addq_8_pi7()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_A7_PI_8();
            uint dst = m68ki_read_8(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_addq_8_pd()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_PD_8();
            uint dst = m68ki_read_8(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_addq_8_pd7()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_A7_PD_8();
            uint dst = m68ki_read_8(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_addq_8_di()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_DI_8();
            uint dst = m68ki_read_8(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_addq_8_ix()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_IX_8();
            uint dst = m68ki_read_8(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_addq_8_aw()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AW_8();
            uint dst = m68ki_read_8(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_addq_8_al()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AL_8();
            uint dst = m68ki_read_8(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_addq_16_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
            _state.poll_detected = unchecked((uint)(0));
        }
    }
    private void m68k_op_addq_16_a()
    {
        unchecked
        {
            ref uint r_dst = ref AY;
            r_dst = MASK_OUT_ABOVE_32(((r_dst + (((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7)))) + unchecked((uint)(1))));
        }
    }
    private void m68k_op_addq_16_ai()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_AI_16();
            uint dst = m68ki_read_16(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_addq_16_pi()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_PI_16();
            uint dst = m68ki_read_16(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_addq_16_pd()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_PD_16();
            uint dst = m68ki_read_16(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_addq_16_di()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_DI_16();
            uint dst = m68ki_read_16(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_addq_16_ix()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_IX_16();
            uint dst = m68ki_read_16(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_addq_16_aw()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AW_16();
            uint dst = m68ki_read_16(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_addq_16_al()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AL_16();
            uint dst = m68ki_read_16(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_addq_32_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint dst = r_dst;
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            r_dst = FLAG_Z;
            _state.poll_detected = unchecked((uint)(0));
        }
    }
    private void m68k_op_addq_32_a()
    {
        unchecked
        {
            ref uint r_dst = ref AY;
            r_dst = MASK_OUT_ABOVE_32(((r_dst + (((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7)))) + unchecked((uint)(1))));
        }
    }
    private void m68k_op_addq_32_ai()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_AI_32();
            uint dst = m68ki_read_32(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_addq_32_pi()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_PI_32();
            uint dst = m68ki_read_32(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_addq_32_pd()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_PD_32();
            uint dst = m68ki_read_32(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_addq_32_di()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_DI_32();
            uint dst = m68ki_read_32(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_addq_32_ix()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_IX_32();
            uint dst = m68ki_read_32(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_addq_32_aw()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AW_32();
            uint dst = m68ki_read_32(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_addq_32_al()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AL_32();
            uint dst = m68ki_read_32(ea);
            uint res = (src + dst);
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_addx_8_rr()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = MASK_OUT_ABOVE_8(DY);
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = ((src + dst) + XFLAG_AS_1());
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            res = MASK_OUT_ABOVE_8(res);
            FLAG_Z |= res;
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
        }
    }
    private void m68k_op_addx_16_rr()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = MASK_OUT_ABOVE_16(DY);
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = ((src + dst) + XFLAG_AS_1());
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            res = MASK_OUT_ABOVE_16(res);
            FLAG_Z |= res;
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
        }
    }
    private void m68k_op_addx_32_rr()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = DY;
            uint dst = r_dst;
            uint res = ((src + dst) + XFLAG_AS_1());
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            res = MASK_OUT_ABOVE_32(res);
            FLAG_Z |= res;
            r_dst = res;
        }
    }
    private void m68k_op_addx_8_mm_ax7()
    {
        unchecked
        {
            uint src = OPER_AY_PD_8();
            uint ea = EA_A7_PD_8();
            uint dst = m68ki_read_8(ea);
            uint res = ((src + dst) + XFLAG_AS_1());
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            res = MASK_OUT_ABOVE_8(res);
            FLAG_Z |= res;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_addx_8_mm_ay7()
    {
        unchecked
        {
            uint src = OPER_A7_PD_8();
            uint ea = EA_AX_PD_8();
            uint dst = m68ki_read_8(ea);
            uint res = ((src + dst) + XFLAG_AS_1());
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            res = MASK_OUT_ABOVE_8(res);
            FLAG_Z |= res;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_addx_8_mm_axy7()
    {
        unchecked
        {
            uint src = OPER_A7_PD_8();
            uint ea = EA_A7_PD_8();
            uint dst = m68ki_read_8(ea);
            uint res = ((src + dst) + XFLAG_AS_1());
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            res = MASK_OUT_ABOVE_8(res);
            FLAG_Z |= res;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_addx_8_mm()
    {
        unchecked
        {
            uint src = OPER_AY_PD_8();
            uint ea = EA_AX_PD_8();
            uint dst = m68ki_read_8(ea);
            uint res = ((src + dst) + XFLAG_AS_1());
            FLAG_N = NFLAG_8(res);
            FLAG_V = VFLAG_ADD_8(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            res = MASK_OUT_ABOVE_8(res);
            FLAG_Z |= res;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_addx_16_mm()
    {
        unchecked
        {
            uint src = OPER_AY_PD_16();
            uint ea = EA_AX_PD_16();
            uint dst = m68ki_read_16(ea);
            uint res = ((src + dst) + XFLAG_AS_1());
            FLAG_N = NFLAG_16(res);
            FLAG_V = VFLAG_ADD_16(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            res = MASK_OUT_ABOVE_16(res);
            FLAG_Z |= res;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_addx_32_mm()
    {
        unchecked
        {
            uint src = OPER_AY_PD_32();
            uint ea = EA_AX_PD_32();
            uint dst = m68ki_read_32(ea);
            uint res = ((src + dst) + XFLAG_AS_1());
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_ADD_32(src, dst, res);
            FLAG_X = (FLAG_C = CFLAG_ADD_32(src, dst, res));
            res = MASK_OUT_ABOVE_32(res);
            FLAG_Z |= res;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_and_8_er_d()
    {
        unchecked
        {
            FLAG_Z = MASK_OUT_ABOVE_8((DX &= (DY | 4294967040u)));
            FLAG_N = NFLAG_8(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_8_er_ai()
    {
        unchecked
        {
            FLAG_Z = MASK_OUT_ABOVE_8((DX &= (OPER_AY_AI_8() | 4294967040u)));
            FLAG_N = NFLAG_8(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_8_er_pi()
    {
        unchecked
        {
            FLAG_Z = MASK_OUT_ABOVE_8((DX &= (OPER_AY_PI_8() | 4294967040u)));
            FLAG_N = NFLAG_8(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_8_er_pi7()
    {
        unchecked
        {
            FLAG_Z = MASK_OUT_ABOVE_8((DX &= (OPER_A7_PI_8() | 4294967040u)));
            FLAG_N = NFLAG_8(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_8_er_pd()
    {
        unchecked
        {
            FLAG_Z = MASK_OUT_ABOVE_8((DX &= (OPER_AY_PD_8() | 4294967040u)));
            FLAG_N = NFLAG_8(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_8_er_pd7()
    {
        unchecked
        {
            FLAG_Z = MASK_OUT_ABOVE_8((DX &= (OPER_A7_PD_8() | 4294967040u)));
            FLAG_N = NFLAG_8(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_8_er_di()
    {
        unchecked
        {
            FLAG_Z = MASK_OUT_ABOVE_8((DX &= (OPER_AY_DI_8() | 4294967040u)));
            FLAG_N = NFLAG_8(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_8_er_ix()
    {
        unchecked
        {
            FLAG_Z = MASK_OUT_ABOVE_8((DX &= (OPER_AY_IX_8() | 4294967040u)));
            FLAG_N = NFLAG_8(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_8_er_aw()
    {
        unchecked
        {
            FLAG_Z = MASK_OUT_ABOVE_8((DX &= (OPER_AW_8() | 4294967040u)));
            FLAG_N = NFLAG_8(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_8_er_al()
    {
        unchecked
        {
            FLAG_Z = MASK_OUT_ABOVE_8((DX &= (OPER_AL_8() | 4294967040u)));
            FLAG_N = NFLAG_8(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_8_er_pcdi()
    {
        unchecked
        {
            FLAG_Z = MASK_OUT_ABOVE_8((DX &= (OPER_PCDI_8() | 4294967040u)));
            FLAG_N = NFLAG_8(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_8_er_pcix()
    {
        unchecked
        {
            FLAG_Z = MASK_OUT_ABOVE_8((DX &= (OPER_PCIX_8() | 4294967040u)));
            FLAG_N = NFLAG_8(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_8_er_i()
    {
        unchecked
        {
            FLAG_Z = MASK_OUT_ABOVE_8((DX &= (OPER_I_8() | 4294967040u)));
            FLAG_N = NFLAG_8(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_16_er_d()
    {
        unchecked
        {
            FLAG_Z = MASK_OUT_ABOVE_16((DX &= (DY | 4294901760u)));
            FLAG_N = NFLAG_16(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_16_er_ai()
    {
        unchecked
        {
            FLAG_Z = MASK_OUT_ABOVE_16((DX &= (OPER_AY_AI_16() | 4294901760u)));
            FLAG_N = NFLAG_16(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_16_er_pi()
    {
        unchecked
        {
            FLAG_Z = MASK_OUT_ABOVE_16((DX &= (OPER_AY_PI_16() | 4294901760u)));
            FLAG_N = NFLAG_16(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_16_er_pd()
    {
        unchecked
        {
            FLAG_Z = MASK_OUT_ABOVE_16((DX &= (OPER_AY_PD_16() | 4294901760u)));
            FLAG_N = NFLAG_16(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_16_er_di()
    {
        unchecked
        {
            FLAG_Z = MASK_OUT_ABOVE_16((DX &= (OPER_AY_DI_16() | 4294901760u)));
            FLAG_N = NFLAG_16(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_16_er_ix()
    {
        unchecked
        {
            FLAG_Z = MASK_OUT_ABOVE_16((DX &= (OPER_AY_IX_16() | 4294901760u)));
            FLAG_N = NFLAG_16(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_16_er_aw()
    {
        unchecked
        {
            FLAG_Z = MASK_OUT_ABOVE_16((DX &= (OPER_AW_16() | 4294901760u)));
            FLAG_N = NFLAG_16(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_16_er_al()
    {
        unchecked
        {
            FLAG_Z = MASK_OUT_ABOVE_16((DX &= (OPER_AL_16() | 4294901760u)));
            FLAG_N = NFLAG_16(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_16_er_pcdi()
    {
        unchecked
        {
            FLAG_Z = MASK_OUT_ABOVE_16((DX &= (OPER_PCDI_16() | 4294901760u)));
            FLAG_N = NFLAG_16(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_16_er_pcix()
    {
        unchecked
        {
            FLAG_Z = MASK_OUT_ABOVE_16((DX &= (OPER_PCIX_16() | 4294901760u)));
            FLAG_N = NFLAG_16(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_16_er_i()
    {
        unchecked
        {
            FLAG_Z = MASK_OUT_ABOVE_16((DX &= (OPER_I_16() | 4294901760u)));
            FLAG_N = NFLAG_16(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_32_er_d()
    {
        unchecked
        {
            FLAG_Z = (DX &= DY);
            FLAG_N = NFLAG_32(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_32_er_ai()
    {
        unchecked
        {
            FLAG_Z = (DX &= OPER_AY_AI_32());
            FLAG_N = NFLAG_32(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_32_er_pi()
    {
        unchecked
        {
            FLAG_Z = (DX &= OPER_AY_PI_32());
            FLAG_N = NFLAG_32(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_32_er_pd()
    {
        unchecked
        {
            FLAG_Z = (DX &= OPER_AY_PD_32());
            FLAG_N = NFLAG_32(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_32_er_di()
    {
        unchecked
        {
            FLAG_Z = (DX &= OPER_AY_DI_32());
            FLAG_N = NFLAG_32(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_32_er_ix()
    {
        unchecked
        {
            FLAG_Z = (DX &= OPER_AY_IX_32());
            FLAG_N = NFLAG_32(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_32_er_aw()
    {
        unchecked
        {
            FLAG_Z = (DX &= OPER_AW_32());
            FLAG_N = NFLAG_32(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_32_er_al()
    {
        unchecked
        {
            FLAG_Z = (DX &= OPER_AL_32());
            FLAG_N = NFLAG_32(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_32_er_pcdi()
    {
        unchecked
        {
            FLAG_Z = (DX &= OPER_PCDI_32());
            FLAG_N = NFLAG_32(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_32_er_pcix()
    {
        unchecked
        {
            FLAG_Z = (DX &= OPER_PCIX_32());
            FLAG_N = NFLAG_32(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_32_er_i()
    {
        unchecked
        {
            FLAG_Z = (DX &= OPER_I_32());
            FLAG_N = NFLAG_32(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_and_8_re_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_8();
            uint res = (DX & m68ki_read_8(ea));
            FLAG_N = NFLAG_8(res);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_and_8_re_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_8();
            uint res = (DX & m68ki_read_8(ea));
            FLAG_N = NFLAG_8(res);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_and_8_re_pi7()
    {
        unchecked
        {
            uint ea = EA_A7_PI_8();
            uint res = (DX & m68ki_read_8(ea));
            FLAG_N = NFLAG_8(res);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_and_8_re_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_8();
            uint res = (DX & m68ki_read_8(ea));
            FLAG_N = NFLAG_8(res);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_and_8_re_pd7()
    {
        unchecked
        {
            uint ea = EA_A7_PD_8();
            uint res = (DX & m68ki_read_8(ea));
            FLAG_N = NFLAG_8(res);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_and_8_re_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_8();
            uint res = (DX & m68ki_read_8(ea));
            FLAG_N = NFLAG_8(res);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_and_8_re_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_8();
            uint res = (DX & m68ki_read_8(ea));
            FLAG_N = NFLAG_8(res);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_and_8_re_aw()
    {
        unchecked
        {
            uint ea = EA_AW_8();
            uint res = (DX & m68ki_read_8(ea));
            FLAG_N = NFLAG_8(res);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_and_8_re_al()
    {
        unchecked
        {
            uint ea = EA_AL_8();
            uint res = (DX & m68ki_read_8(ea));
            FLAG_N = NFLAG_8(res);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_and_16_re_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_16();
            uint res = (DX & m68ki_read_16(ea));
            FLAG_N = NFLAG_16(res);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_and_16_re_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_16();
            uint res = (DX & m68ki_read_16(ea));
            FLAG_N = NFLAG_16(res);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_and_16_re_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_16();
            uint res = (DX & m68ki_read_16(ea));
            FLAG_N = NFLAG_16(res);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_and_16_re_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_16();
            uint res = (DX & m68ki_read_16(ea));
            FLAG_N = NFLAG_16(res);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_and_16_re_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_16();
            uint res = (DX & m68ki_read_16(ea));
            FLAG_N = NFLAG_16(res);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_and_16_re_aw()
    {
        unchecked
        {
            uint ea = EA_AW_16();
            uint res = (DX & m68ki_read_16(ea));
            FLAG_N = NFLAG_16(res);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_and_16_re_al()
    {
        unchecked
        {
            uint ea = EA_AL_16();
            uint res = (DX & m68ki_read_16(ea));
            FLAG_N = NFLAG_16(res);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_and_32_re_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_32();
            uint res = (DX & m68ki_read_32(ea));
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_and_32_re_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_32();
            uint res = (DX & m68ki_read_32(ea));
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_and_32_re_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_32();
            uint res = (DX & m68ki_read_32(ea));
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_and_32_re_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_32();
            uint res = (DX & m68ki_read_32(ea));
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_and_32_re_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_32();
            uint res = (DX & m68ki_read_32(ea));
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_and_32_re_aw()
    {
        unchecked
        {
            uint ea = EA_AW_32();
            uint res = (DX & m68ki_read_32(ea));
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_and_32_re_al()
    {
        unchecked
        {
            uint ea = EA_AL_32();
            uint res = (DX & m68ki_read_32(ea));
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_andi_8_d()
    {
        unchecked
        {
            FLAG_Z = MASK_OUT_ABOVE_8((DY &= (OPER_I_8() | 4294967040u)));
            FLAG_N = NFLAG_8(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_andi_8_ai()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AY_AI_8();
            uint res = (src & m68ki_read_8(ea));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_andi_8_pi()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AY_PI_8();
            uint res = (src & m68ki_read_8(ea));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_andi_8_pi7()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_A7_PI_8();
            uint res = (src & m68ki_read_8(ea));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_andi_8_pd()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AY_PD_8();
            uint res = (src & m68ki_read_8(ea));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_andi_8_pd7()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_A7_PD_8();
            uint res = (src & m68ki_read_8(ea));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_andi_8_di()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AY_DI_8();
            uint res = (src & m68ki_read_8(ea));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_andi_8_ix()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AY_IX_8();
            uint res = (src & m68ki_read_8(ea));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_andi_8_aw()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AW_8();
            uint res = (src & m68ki_read_8(ea));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_andi_8_al()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AL_8();
            uint res = (src & m68ki_read_8(ea));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_andi_16_d()
    {
        unchecked
        {
            FLAG_Z = MASK_OUT_ABOVE_16((DY &= (OPER_I_16() | 4294901760u)));
            FLAG_N = NFLAG_16(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_andi_16_ai()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AY_AI_16();
            uint res = (src & m68ki_read_16(ea));
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_andi_16_pi()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AY_PI_16();
            uint res = (src & m68ki_read_16(ea));
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_andi_16_pd()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AY_PD_16();
            uint res = (src & m68ki_read_16(ea));
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_andi_16_di()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AY_DI_16();
            uint res = (src & m68ki_read_16(ea));
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_andi_16_ix()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AY_IX_16();
            uint res = (src & m68ki_read_16(ea));
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_andi_16_aw()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AW_16();
            uint res = (src & m68ki_read_16(ea));
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_andi_16_al()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AL_16();
            uint res = (src & m68ki_read_16(ea));
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_andi_32_d()
    {
        unchecked
        {
            FLAG_Z = (DY &= OPER_I_32());
            FLAG_N = NFLAG_32(FLAG_Z);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_andi_32_ai()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AY_AI_32();
            uint res = (src & m68ki_read_32(ea));
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_andi_32_pi()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AY_PI_32();
            uint res = (src & m68ki_read_32(ea));
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_andi_32_pd()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AY_PD_32();
            uint res = (src & m68ki_read_32(ea));
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_andi_32_di()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AY_DI_32();
            uint res = (src & m68ki_read_32(ea));
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_andi_32_ix()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AY_IX_32();
            uint res = (src & m68ki_read_32(ea));
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_andi_32_aw()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AW_32();
            uint res = (src & m68ki_read_32(ea));
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_andi_32_al()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AL_32();
            uint res = (src & m68ki_read_32(ea));
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_andi_16_toc()
    {
        unchecked
        {
            m68ki_set_ccr((m68ki_get_ccr() & OPER_I_16()));
        }
    }
    private void m68k_op_andi_16_tos()
    {
        unchecked
        {
            if ((FLAG_S != 0)) {
            uint src = OPER_I_16();
            m68ki_set_sr((m68ki_get_sr() & src));
            return;
            }
            m68ki_exception_privilege_violation();
        }
    }
    private void m68k_op_asr_8_s()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint src = MASK_OUT_ABOVE_8(r_dst);
            uint res = (src >> unchecked((int)(shift)));
            if ((shift != unchecked((uint)(0)))) USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            if ((GET_MSB_8(src) != 0)) res |= unchecked((uint)(m68ki_shift_8_table[(int)(shift)]));
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_X = (FLAG_C = (src << unchecked((int)((unchecked((uint)(9)) - shift)))));
        }
    }
    private void m68k_op_asr_16_s()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint src = MASK_OUT_ABOVE_16(r_dst);
            uint res = (src >> unchecked((int)(shift)));
            if ((shift != unchecked((uint)(0)))) USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            if ((GET_MSB_16(src) != 0)) res |= unchecked((uint)(m68ki_shift_16_table[(int)(shift)]));
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_X = (FLAG_C = (src << unchecked((int)((unchecked((uint)(9)) - shift)))));
        }
    }
    private void m68k_op_asr_32_s()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint src = r_dst;
            uint res = (src >> unchecked((int)(shift)));
            if ((shift != unchecked((uint)(0)))) USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            if ((GET_MSB_32(src) != 0)) res |= m68ki_shift_32_table[(int)(shift)];
            r_dst = res;
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_X = (FLAG_C = (src << unchecked((int)((unchecked((uint)(9)) - shift)))));
        }
    }
    private void m68k_op_asr_8_r()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = (DX & unchecked((uint)(63)));
            uint src = MASK_OUT_ABOVE_8(r_dst);
            uint res = (src >> unchecked((int)(shift)));
            if ((shift != unchecked((uint)(0)))) {
            USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            SKIP_BUS_REFRESH();
            if ((shift < unchecked((uint)(8)))) {
            if ((GET_MSB_8(src) != 0)) res |= unchecked((uint)(m68ki_shift_8_table[(int)(shift)]));
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_X = (FLAG_C = (src << unchecked((int)((unchecked((uint)(9)) - shift)))));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            if ((GET_MSB_8(src) != 0)) {
            r_dst |= unchecked((uint)(255));
            FLAG_C = CFLAG_SET;
            FLAG_X = XFLAG_SET;
            FLAG_N = NFLAG_SET;
            FLAG_Z = ZFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            r_dst &= 4294967040u;
            FLAG_C = CFLAG_CLEAR;
            FLAG_X = XFLAG_CLEAR;
            FLAG_N = NFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            FLAG_N = NFLAG_8(src);
            FLAG_Z = src;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_asr_16_r()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = (DX & unchecked((uint)(63)));
            uint src = MASK_OUT_ABOVE_16(r_dst);
            uint res = (src >> unchecked((int)(shift)));
            if ((shift != unchecked((uint)(0)))) {
            USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            SKIP_BUS_REFRESH();
            if ((shift < unchecked((uint)(16)))) {
            if ((GET_MSB_16(src) != 0)) res |= unchecked((uint)(m68ki_shift_16_table[(int)(shift)]));
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_C = (FLAG_X = ((src >> unchecked((int)((shift - unchecked((uint)(1)))))) << 8));
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            if ((GET_MSB_16(src) != 0)) {
            r_dst |= unchecked((uint)(65535));
            FLAG_C = CFLAG_SET;
            FLAG_X = XFLAG_SET;
            FLAG_N = NFLAG_SET;
            FLAG_Z = ZFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            r_dst &= 4294901760u;
            FLAG_C = CFLAG_CLEAR;
            FLAG_X = XFLAG_CLEAR;
            FLAG_N = NFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            FLAG_N = NFLAG_16(src);
            FLAG_Z = src;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_asr_32_r()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = (DX & unchecked((uint)(63)));
            uint src = r_dst;
            uint res = (src >> unchecked((int)(shift)));
            if ((shift != unchecked((uint)(0)))) {
            USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            SKIP_BUS_REFRESH();
            if ((shift < unchecked((uint)(32)))) {
            if ((GET_MSB_32(src) != 0)) res |= m68ki_shift_32_table[(int)(shift)];
            r_dst = res;
            FLAG_C = (FLAG_X = ((src >> unchecked((int)((shift - unchecked((uint)(1)))))) << 8));
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            if ((GET_MSB_32(src) != 0)) {
            r_dst = 4294967295u;
            FLAG_C = CFLAG_SET;
            FLAG_X = XFLAG_SET;
            FLAG_N = NFLAG_SET;
            FLAG_Z = ZFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            r_dst = unchecked((uint)(0));
            FLAG_C = CFLAG_CLEAR;
            FLAG_X = XFLAG_CLEAR;
            FLAG_N = NFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            FLAG_N = NFLAG_32(src);
            FLAG_Z = src;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_asr_16_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_16();
            uint src = m68ki_read_16(ea);
            uint res = (src >> 1);
            if ((GET_MSB_16(src) != 0)) res |= unchecked((uint)(32768));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = (FLAG_X = (src << 8));
        }
    }
    private void m68k_op_asr_16_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_16();
            uint src = m68ki_read_16(ea);
            uint res = (src >> 1);
            if ((GET_MSB_16(src) != 0)) res |= unchecked((uint)(32768));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = (FLAG_X = (src << 8));
        }
    }
    private void m68k_op_asr_16_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_16();
            uint src = m68ki_read_16(ea);
            uint res = (src >> 1);
            if ((GET_MSB_16(src) != 0)) res |= unchecked((uint)(32768));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = (FLAG_X = (src << 8));
        }
    }
    private void m68k_op_asr_16_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_16();
            uint src = m68ki_read_16(ea);
            uint res = (src >> 1);
            if ((GET_MSB_16(src) != 0)) res |= unchecked((uint)(32768));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = (FLAG_X = (src << 8));
        }
    }
    private void m68k_op_asr_16_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_16();
            uint src = m68ki_read_16(ea);
            uint res = (src >> 1);
            if ((GET_MSB_16(src) != 0)) res |= unchecked((uint)(32768));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = (FLAG_X = (src << 8));
        }
    }
    private void m68k_op_asr_16_aw()
    {
        unchecked
        {
            uint ea = EA_AW_16();
            uint src = m68ki_read_16(ea);
            uint res = (src >> 1);
            if ((GET_MSB_16(src) != 0)) res |= unchecked((uint)(32768));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = (FLAG_X = (src << 8));
        }
    }
    private void m68k_op_asr_16_al()
    {
        unchecked
        {
            uint ea = EA_AL_16();
            uint src = m68ki_read_16(ea);
            uint res = (src >> 1);
            if ((GET_MSB_16(src) != 0)) res |= unchecked((uint)(32768));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = (FLAG_X = (src << 8));
        }
    }
    private void m68k_op_asl_8_s()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint src = MASK_OUT_ABOVE_8(r_dst);
            uint res = MASK_OUT_ABOVE_8((src << unchecked((int)(shift))));
            if ((shift != unchecked((uint)(0)))) USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_X = (FLAG_C = (src << unchecked((int)(shift))));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            src &= unchecked((uint)(m68ki_shift_8_table[(int)((shift + unchecked((uint)(1))))]));
            FLAG_V = unchecked((uint)((((!((src == unchecked((uint)(0))) || ((src == unchecked((uint)(m68ki_shift_8_table[(int)((shift + unchecked((uint)(1))))]))) && (shift < unchecked((uint)(8)))))) ? (int)1 : (int)0) << 7)));
        }
    }
    private void m68k_op_asl_16_s()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint src = MASK_OUT_ABOVE_16(r_dst);
            uint res = MASK_OUT_ABOVE_16((src << unchecked((int)(shift))));
            if ((shift != unchecked((uint)(0)))) USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_X = (FLAG_C = (src >> unchecked((int)((unchecked((uint)(8)) - shift)))));
            src &= unchecked((uint)(m68ki_shift_16_table[(int)((shift + unchecked((uint)(1))))]));
            FLAG_V = unchecked((uint)((((!((src == unchecked((uint)(0))) || (src == unchecked((uint)(m68ki_shift_16_table[(int)((shift + unchecked((uint)(1))))]))))) ? (int)1 : (int)0) << 7)));
        }
    }
    private void m68k_op_asl_32_s()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint src = r_dst;
            uint res = MASK_OUT_ABOVE_32((src << unchecked((int)(shift))));
            if ((shift != unchecked((uint)(0)))) USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            r_dst = res;
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_X = (FLAG_C = (src >> unchecked((int)((unchecked((uint)(24)) - shift)))));
            src &= m68ki_shift_32_table[(int)((shift + unchecked((uint)(1))))];
            FLAG_V = unchecked((uint)((((!((src == unchecked((uint)(0))) || (src == m68ki_shift_32_table[(int)((shift + unchecked((uint)(1))))]))) ? (int)1 : (int)0) << 7)));
        }
    }
    private void m68k_op_asl_8_r()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = (DX & unchecked((uint)(63)));
            uint src = MASK_OUT_ABOVE_8(r_dst);
            uint res = MASK_OUT_ABOVE_8((src << unchecked((int)(shift))));
            if ((shift != unchecked((uint)(0)))) {
            USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            SKIP_BUS_REFRESH();
            if ((shift < unchecked((uint)(8)))) {
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_X = (FLAG_C = (src << unchecked((int)(shift))));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            src &= unchecked((uint)(m68ki_shift_8_table[(int)((shift + unchecked((uint)(1))))]));
            FLAG_V = unchecked((uint)((((!((src == unchecked((uint)(0))) || (src == unchecked((uint)(m68ki_shift_8_table[(int)((shift + unchecked((uint)(1))))]))))) ? (int)1 : (int)0) << 7)));
            return;
            }
            r_dst &= 4294967040u;
            FLAG_X = (FLAG_C = (((shift == unchecked((uint)(8))) ? (src & unchecked((uint)(1))) : unchecked((uint)(0))) << 8));
            FLAG_N = NFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
            FLAG_V = unchecked((uint)((((!(src == unchecked((uint)(0)))) ? (int)1 : (int)0) << 7)));
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            FLAG_N = NFLAG_8(src);
            FLAG_Z = src;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_asl_16_r()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = (DX & unchecked((uint)(63)));
            uint src = MASK_OUT_ABOVE_16(r_dst);
            uint res = MASK_OUT_ABOVE_16((src << unchecked((int)(shift))));
            if ((shift != unchecked((uint)(0)))) {
            USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            SKIP_BUS_REFRESH();
            if ((shift < unchecked((uint)(16)))) {
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_X = (FLAG_C = ((src << unchecked((int)(shift))) >> 8));
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            src &= unchecked((uint)(m68ki_shift_16_table[(int)((shift + unchecked((uint)(1))))]));
            FLAG_V = unchecked((uint)((((!((src == unchecked((uint)(0))) || (src == unchecked((uint)(m68ki_shift_16_table[(int)((shift + unchecked((uint)(1))))]))))) ? (int)1 : (int)0) << 7)));
            return;
            }
            r_dst &= 4294901760u;
            FLAG_X = (FLAG_C = (((shift == unchecked((uint)(16))) ? (src & unchecked((uint)(1))) : unchecked((uint)(0))) << 8));
            FLAG_N = NFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
            FLAG_V = unchecked((uint)((((!(src == unchecked((uint)(0)))) ? (int)1 : (int)0) << 7)));
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            FLAG_N = NFLAG_16(src);
            FLAG_Z = src;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_asl_32_r()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = (DX & unchecked((uint)(63)));
            uint src = r_dst;
            uint res = MASK_OUT_ABOVE_32((src << unchecked((int)(shift))));
            if ((shift != unchecked((uint)(0)))) {
            USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            SKIP_BUS_REFRESH();
            if ((shift < unchecked((uint)(32)))) {
            r_dst = res;
            FLAG_X = (FLAG_C = ((src >> unchecked((int)((unchecked((uint)(32)) - shift)))) << 8));
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            src &= m68ki_shift_32_table[(int)((shift + unchecked((uint)(1))))];
            FLAG_V = unchecked((uint)((((!((src == unchecked((uint)(0))) || (src == m68ki_shift_32_table[(int)((shift + unchecked((uint)(1))))]))) ? (int)1 : (int)0) << 7)));
            return;
            }
            r_dst = unchecked((uint)(0));
            FLAG_X = (FLAG_C = (((shift == unchecked((uint)(32))) ? (src & unchecked((uint)(1))) : unchecked((uint)(0))) << 8));
            FLAG_N = NFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
            FLAG_V = unchecked((uint)((((!(src == unchecked((uint)(0)))) ? (int)1 : (int)0) << 7)));
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            FLAG_N = NFLAG_32(src);
            FLAG_Z = src;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_asl_16_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_16();
            uint src = m68ki_read_16(ea);
            uint res = MASK_OUT_ABOVE_16((src << 1));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_X = (FLAG_C = (src >> 7));
            src &= unchecked((uint)(49152));
            FLAG_V = unchecked((uint)((((!((src == unchecked((uint)(0))) || (src == unchecked((uint)(49152))))) ? (int)1 : (int)0) << 7)));
        }
    }
    private void m68k_op_asl_16_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_16();
            uint src = m68ki_read_16(ea);
            uint res = MASK_OUT_ABOVE_16((src << 1));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_X = (FLAG_C = (src >> 7));
            src &= unchecked((uint)(49152));
            FLAG_V = unchecked((uint)((((!((src == unchecked((uint)(0))) || (src == unchecked((uint)(49152))))) ? (int)1 : (int)0) << 7)));
        }
    }
    private void m68k_op_asl_16_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_16();
            uint src = m68ki_read_16(ea);
            uint res = MASK_OUT_ABOVE_16((src << 1));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_X = (FLAG_C = (src >> 7));
            src &= unchecked((uint)(49152));
            FLAG_V = unchecked((uint)((((!((src == unchecked((uint)(0))) || (src == unchecked((uint)(49152))))) ? (int)1 : (int)0) << 7)));
        }
    }
    private void m68k_op_asl_16_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_16();
            uint src = m68ki_read_16(ea);
            uint res = MASK_OUT_ABOVE_16((src << 1));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_X = (FLAG_C = (src >> 7));
            src &= unchecked((uint)(49152));
            FLAG_V = unchecked((uint)((((!((src == unchecked((uint)(0))) || (src == unchecked((uint)(49152))))) ? (int)1 : (int)0) << 7)));
        }
    }
    private void m68k_op_asl_16_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_16();
            uint src = m68ki_read_16(ea);
            uint res = MASK_OUT_ABOVE_16((src << 1));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_X = (FLAG_C = (src >> 7));
            src &= unchecked((uint)(49152));
            FLAG_V = unchecked((uint)((((!((src == unchecked((uint)(0))) || (src == unchecked((uint)(49152))))) ? (int)1 : (int)0) << 7)));
        }
    }
    private void m68k_op_asl_16_aw()
    {
        unchecked
        {
            uint ea = EA_AW_16();
            uint src = m68ki_read_16(ea);
            uint res = MASK_OUT_ABOVE_16((src << 1));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_X = (FLAG_C = (src >> 7));
            src &= unchecked((uint)(49152));
            FLAG_V = unchecked((uint)((((!((src == unchecked((uint)(0))) || (src == unchecked((uint)(49152))))) ? (int)1 : (int)0) << 7)));
        }
    }
    private void m68k_op_asl_16_al()
    {
        unchecked
        {
            uint ea = EA_AL_16();
            uint src = m68ki_read_16(ea);
            uint res = MASK_OUT_ABOVE_16((src << 1));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_X = (FLAG_C = (src >> 7));
            src &= unchecked((uint)(49152));
            FLAG_V = unchecked((uint)((((!((src == unchecked((uint)(0))) || (src == unchecked((uint)(49152))))) ? (int)1 : (int)0) << 7)));
        }
    }
    private void m68k_op_bhi_8()
    {
        unchecked
        {
            if ((COND_HI() != 0)) {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
            return;
            }
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_B)));
        }
    }
    private void m68k_op_bls_8()
    {
        unchecked
        {
            if ((COND_LS() != 0)) {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
            return;
            }
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_B)));
        }
    }
    private void m68k_op_bcc_8()
    {
        unchecked
        {
            if ((COND_CC() != 0)) {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
            return;
            }
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_B)));
        }
    }
    private void m68k_op_bcs_8()
    {
        unchecked
        {
            if ((COND_CS() != 0)) {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
            return;
            }
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_B)));
        }
    }
    private void m68k_op_bne_8()
    {
        unchecked
        {
            if ((COND_NE() != 0)) {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
            return;
            }
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_B)));
        }
    }
    private void m68k_op_beq_8()
    {
        unchecked
        {
            if ((COND_EQ() != 0)) {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
            return;
            }
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_B)));
        }
    }
    private void m68k_op_bvc_8()
    {
        unchecked
        {
            if ((COND_VC() != 0)) {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
            return;
            }
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_B)));
        }
    }
    private void m68k_op_bvs_8()
    {
        unchecked
        {
            if ((COND_VS() != 0)) {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
            return;
            }
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_B)));
        }
    }
    private void m68k_op_bpl_8()
    {
        unchecked
        {
            if ((COND_PL() != 0)) {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
            return;
            }
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_B)));
        }
    }
    private void m68k_op_bmi_8()
    {
        unchecked
        {
            if ((COND_MI() != 0)) {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
            return;
            }
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_B)));
        }
    }
    private void m68k_op_bge_8()
    {
        unchecked
        {
            if ((COND_GE() != 0)) {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
            return;
            }
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_B)));
        }
    }
    private void m68k_op_blt_8()
    {
        unchecked
        {
            if ((COND_LT() != 0)) {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
            return;
            }
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_B)));
        }
    }
    private void m68k_op_bgt_8()
    {
        unchecked
        {
            if ((COND_GT() != 0)) {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
            return;
            }
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_B)));
        }
    }
    private void m68k_op_ble_8()
    {
        unchecked
        {
            if ((COND_LE() != 0)) {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
            return;
            }
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_B)));
        }
    }
    private void m68k_op_bhi_16()
    {
        unchecked
        {
            if ((COND_HI() != 0)) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_W)));
        }
    }
    private void m68k_op_bls_16()
    {
        unchecked
        {
            if ((COND_LS() != 0)) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_W)));
        }
    }
    private void m68k_op_bcc_16()
    {
        unchecked
        {
            if ((COND_CC() != 0)) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_W)));
        }
    }
    private void m68k_op_bcs_16()
    {
        unchecked
        {
            if ((COND_CS() != 0)) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_W)));
        }
    }
    private void m68k_op_bne_16()
    {
        unchecked
        {
            if ((COND_NE() != 0)) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_W)));
        }
    }
    private void m68k_op_beq_16()
    {
        unchecked
        {
            if ((COND_EQ() != 0)) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_W)));
        }
    }
    private void m68k_op_bvc_16()
    {
        unchecked
        {
            if ((COND_VC() != 0)) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_W)));
        }
    }
    private void m68k_op_bvs_16()
    {
        unchecked
        {
            if ((COND_VS() != 0)) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_W)));
        }
    }
    private void m68k_op_bpl_16()
    {
        unchecked
        {
            if ((COND_PL() != 0)) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_W)));
        }
    }
    private void m68k_op_bmi_16()
    {
        unchecked
        {
            if ((COND_MI() != 0)) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_W)));
        }
    }
    private void m68k_op_bge_16()
    {
        unchecked
        {
            if ((COND_GE() != 0)) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_W)));
        }
    }
    private void m68k_op_blt_16()
    {
        unchecked
        {
            if ((COND_LT() != 0)) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_W)));
        }
    }
    private void m68k_op_bgt_16()
    {
        unchecked
        {
            if ((COND_GT() != 0)) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_W)));
        }
    }
    private void m68k_op_ble_16()
    {
        unchecked
        {
            if ((COND_LE() != 0)) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_W)));
        }
    }
    private void m68k_op_bhi_32()
    {
        unchecked
        {
            if ((COND_HI() != 0)) {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
            return;
            }
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_B)));
        }
    }
    private void m68k_op_bls_32()
    {
        unchecked
        {
            if ((COND_LS() != 0)) {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
            return;
            }
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_B)));
        }
    }
    private void m68k_op_bcc_32()
    {
        unchecked
        {
            if ((COND_CC() != 0)) {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
            return;
            }
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_B)));
        }
    }
    private void m68k_op_bcs_32()
    {
        unchecked
        {
            if ((COND_CS() != 0)) {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
            return;
            }
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_B)));
        }
    }
    private void m68k_op_bne_32()
    {
        unchecked
        {
            if ((COND_NE() != 0)) {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
            return;
            }
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_B)));
        }
    }
    private void m68k_op_beq_32()
    {
        unchecked
        {
            if ((COND_EQ() != 0)) {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
            return;
            }
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_B)));
        }
    }
    private void m68k_op_bvc_32()
    {
        unchecked
        {
            if ((COND_VC() != 0)) {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
            return;
            }
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_B)));
        }
    }
    private void m68k_op_bvs_32()
    {
        unchecked
        {
            if ((COND_VS() != 0)) {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
            return;
            }
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_B)));
        }
    }
    private void m68k_op_bpl_32()
    {
        unchecked
        {
            if ((COND_PL() != 0)) {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
            return;
            }
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_B)));
        }
    }
    private void m68k_op_bmi_32()
    {
        unchecked
        {
            if ((COND_MI() != 0)) {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
            return;
            }
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_B)));
        }
    }
    private void m68k_op_bge_32()
    {
        unchecked
        {
            if ((COND_GE() != 0)) {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
            return;
            }
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_B)));
        }
    }
    private void m68k_op_blt_32()
    {
        unchecked
        {
            if ((COND_LT() != 0)) {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
            return;
            }
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_B)));
        }
    }
    private void m68k_op_bgt_32()
    {
        unchecked
        {
            if ((COND_GT() != 0)) {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
            return;
            }
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_B)));
        }
    }
    private void m68k_op_ble_32()
    {
        unchecked
        {
            if ((COND_LE() != 0)) {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
            return;
            }
            USE_CYCLES(unchecked((long)(CYC_BCC_NOTAKE_B)));
        }
    }
    private void m68k_op_bchg_32_r_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(31))))))));
            if (((mask & 4294901760u) != 0)) {
            USE_CYCLES(unchecked((long)((2 * MUL))));
            }
            FLAG_Z = (r_dst & mask);
            r_dst ^= mask;
        }
    }
    private void m68k_op_bchg_8_r_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_8();
            uint src = m68ki_read_8(ea);
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7))))))));
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src ^ mask));
        }
    }
    private void m68k_op_bchg_8_r_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_8();
            uint src = m68ki_read_8(ea);
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7))))))));
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src ^ mask));
        }
    }
    private void m68k_op_bchg_8_r_pi7()
    {
        unchecked
        {
            uint ea = EA_A7_PI_8();
            uint src = m68ki_read_8(ea);
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7))))))));
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src ^ mask));
        }
    }
    private void m68k_op_bchg_8_r_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_8();
            uint src = m68ki_read_8(ea);
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7))))))));
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src ^ mask));
        }
    }
    private void m68k_op_bchg_8_r_pd7()
    {
        unchecked
        {
            uint ea = EA_A7_PD_8();
            uint src = m68ki_read_8(ea);
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7))))))));
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src ^ mask));
        }
    }
    private void m68k_op_bchg_8_r_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_8();
            uint src = m68ki_read_8(ea);
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7))))))));
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src ^ mask));
        }
    }
    private void m68k_op_bchg_8_r_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_8();
            uint src = m68ki_read_8(ea);
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7))))))));
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src ^ mask));
        }
    }
    private void m68k_op_bchg_8_r_aw()
    {
        unchecked
        {
            uint ea = EA_AW_8();
            uint src = m68ki_read_8(ea);
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7))))))));
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src ^ mask));
        }
    }
    private void m68k_op_bchg_8_r_al()
    {
        unchecked
        {
            uint ea = EA_AL_8();
            uint src = m68ki_read_8(ea);
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7))))))));
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src ^ mask));
        }
    }
    private void m68k_op_bchg_32_s_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(31))))))));
            if (((mask & 4294901760u) != 0)) {
            USE_CYCLES(unchecked((long)((2 * MUL))));
            }
            FLAG_Z = (r_dst & mask);
            r_dst ^= mask;
        }
    }
    private void m68k_op_bchg_8_s_ai()
    {
        unchecked
        {
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(7))))))));
            uint ea = EA_AY_AI_8();
            uint src = m68ki_read_8(ea);
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src ^ mask));
        }
    }
    private void m68k_op_bchg_8_s_pi()
    {
        unchecked
        {
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(7))))))));
            uint ea = EA_AY_PI_8();
            uint src = m68ki_read_8(ea);
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src ^ mask));
        }
    }
    private void m68k_op_bchg_8_s_pi7()
    {
        unchecked
        {
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(7))))))));
            uint ea = EA_A7_PI_8();
            uint src = m68ki_read_8(ea);
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src ^ mask));
        }
    }
    private void m68k_op_bchg_8_s_pd()
    {
        unchecked
        {
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(7))))))));
            uint ea = EA_AY_PD_8();
            uint src = m68ki_read_8(ea);
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src ^ mask));
        }
    }
    private void m68k_op_bchg_8_s_pd7()
    {
        unchecked
        {
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(7))))))));
            uint ea = EA_A7_PD_8();
            uint src = m68ki_read_8(ea);
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src ^ mask));
        }
    }
    private void m68k_op_bchg_8_s_di()
    {
        unchecked
        {
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(7))))))));
            uint ea = EA_AY_DI_8();
            uint src = m68ki_read_8(ea);
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src ^ mask));
        }
    }
    private void m68k_op_bchg_8_s_ix()
    {
        unchecked
        {
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(7))))))));
            uint ea = EA_AY_IX_8();
            uint src = m68ki_read_8(ea);
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src ^ mask));
        }
    }
    private void m68k_op_bchg_8_s_aw()
    {
        unchecked
        {
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(7))))))));
            uint ea = EA_AW_8();
            uint src = m68ki_read_8(ea);
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src ^ mask));
        }
    }
    private void m68k_op_bchg_8_s_al()
    {
        unchecked
        {
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(7))))))));
            uint ea = EA_AL_8();
            uint src = m68ki_read_8(ea);
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src ^ mask));
        }
    }
    private void m68k_op_bclr_32_r_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(31))))))));
            if (((mask & 4294901760u) != 0)) {
            USE_CYCLES(unchecked((long)((2 * MUL))));
            }
            FLAG_Z = (r_dst & mask);
            r_dst &= (~mask);
        }
    }
    private void m68k_op_bclr_8_r_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_8();
            uint src = m68ki_read_8(ea);
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7))))))));
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src & (~mask)));
        }
    }
    private void m68k_op_bclr_8_r_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_8();
            uint src = m68ki_read_8(ea);
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7))))))));
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src & (~mask)));
        }
    }
    private void m68k_op_bclr_8_r_pi7()
    {
        unchecked
        {
            uint ea = EA_A7_PI_8();
            uint src = m68ki_read_8(ea);
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7))))))));
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src & (~mask)));
        }
    }
    private void m68k_op_bclr_8_r_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_8();
            uint src = m68ki_read_8(ea);
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7))))))));
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src & (~mask)));
        }
    }
    private void m68k_op_bclr_8_r_pd7()
    {
        unchecked
        {
            uint ea = EA_A7_PD_8();
            uint src = m68ki_read_8(ea);
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7))))))));
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src & (~mask)));
        }
    }
    private void m68k_op_bclr_8_r_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_8();
            uint src = m68ki_read_8(ea);
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7))))))));
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src & (~mask)));
        }
    }
    private void m68k_op_bclr_8_r_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_8();
            uint src = m68ki_read_8(ea);
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7))))))));
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src & (~mask)));
        }
    }
    private void m68k_op_bclr_8_r_aw()
    {
        unchecked
        {
            uint ea = EA_AW_8();
            uint src = m68ki_read_8(ea);
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7))))))));
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src & (~mask)));
        }
    }
    private void m68k_op_bclr_8_r_al()
    {
        unchecked
        {
            uint ea = EA_AL_8();
            uint src = m68ki_read_8(ea);
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7))))))));
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src & (~mask)));
        }
    }
    private void m68k_op_bclr_32_s_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(31))))))));
            if (((mask & 4294901760u) != 0)) {
            USE_CYCLES(unchecked((long)((2 * MUL))));
            }
            FLAG_Z = (r_dst & mask);
            r_dst &= (~mask);
        }
    }
    private void m68k_op_bclr_8_s_ai()
    {
        unchecked
        {
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(7))))))));
            uint ea = EA_AY_AI_8();
            uint src = m68ki_read_8(ea);
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src & (~mask)));
        }
    }
    private void m68k_op_bclr_8_s_pi()
    {
        unchecked
        {
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(7))))))));
            uint ea = EA_AY_PI_8();
            uint src = m68ki_read_8(ea);
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src & (~mask)));
        }
    }
    private void m68k_op_bclr_8_s_pi7()
    {
        unchecked
        {
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(7))))))));
            uint ea = EA_A7_PI_8();
            uint src = m68ki_read_8(ea);
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src & (~mask)));
        }
    }
    private void m68k_op_bclr_8_s_pd()
    {
        unchecked
        {
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(7))))))));
            uint ea = EA_AY_PD_8();
            uint src = m68ki_read_8(ea);
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src & (~mask)));
        }
    }
    private void m68k_op_bclr_8_s_pd7()
    {
        unchecked
        {
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(7))))))));
            uint ea = EA_A7_PD_8();
            uint src = m68ki_read_8(ea);
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src & (~mask)));
        }
    }
    private void m68k_op_bclr_8_s_di()
    {
        unchecked
        {
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(7))))))));
            uint ea = EA_AY_DI_8();
            uint src = m68ki_read_8(ea);
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src & (~mask)));
        }
    }
    private void m68k_op_bclr_8_s_ix()
    {
        unchecked
        {
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(7))))))));
            uint ea = EA_AY_IX_8();
            uint src = m68ki_read_8(ea);
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src & (~mask)));
        }
    }
    private void m68k_op_bclr_8_s_aw()
    {
        unchecked
        {
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(7))))))));
            uint ea = EA_AW_8();
            uint src = m68ki_read_8(ea);
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src & (~mask)));
        }
    }
    private void m68k_op_bclr_8_s_al()
    {
        unchecked
        {
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(7))))))));
            uint ea = EA_AL_8();
            uint src = m68ki_read_8(ea);
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src & (~mask)));
        }
    }
    private void m68k_op_bra_8()
    {
        unchecked
        {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
        }
    }
    private void m68k_op_bra_16()
    {
        unchecked
        {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
        }
    }
    private void m68k_op_bra_32()
    {
        unchecked
        {
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
        }
    }
    private void m68k_op_bset_32_r_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(31))))))));
            if (((mask & 4294901760u) != 0)) {
            USE_CYCLES(unchecked((long)((2 * MUL))));
            }
            FLAG_Z = (r_dst & mask);
            r_dst |= mask;
        }
    }
    private void m68k_op_bset_8_r_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_8();
            uint src = m68ki_read_8(ea);
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7))))))));
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src | mask));
        }
    }
    private void m68k_op_bset_8_r_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_8();
            uint src = m68ki_read_8(ea);
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7))))))));
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src | mask));
        }
    }
    private void m68k_op_bset_8_r_pi7()
    {
        unchecked
        {
            uint ea = EA_A7_PI_8();
            uint src = m68ki_read_8(ea);
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7))))))));
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src | mask));
        }
    }
    private void m68k_op_bset_8_r_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_8();
            uint src = m68ki_read_8(ea);
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7))))))));
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src | mask));
        }
    }
    private void m68k_op_bset_8_r_pd7()
    {
        unchecked
        {
            uint ea = EA_A7_PD_8();
            uint src = m68ki_read_8(ea);
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7))))))));
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src | mask));
        }
    }
    private void m68k_op_bset_8_r_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_8();
            uint src = m68ki_read_8(ea);
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7))))))));
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src | mask));
        }
    }
    private void m68k_op_bset_8_r_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_8();
            uint src = m68ki_read_8(ea);
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7))))))));
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src | mask));
        }
    }
    private void m68k_op_bset_8_r_aw()
    {
        unchecked
        {
            uint ea = EA_AW_8();
            uint src = m68ki_read_8(ea);
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7))))))));
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src | mask));
        }
    }
    private void m68k_op_bset_8_r_al()
    {
        unchecked
        {
            uint ea = EA_AL_8();
            uint src = m68ki_read_8(ea);
            uint mask = unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7))))))));
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src | mask));
        }
    }
    private void m68k_op_bset_32_s_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(31))))))));
            if (((mask & 4294901760u) != 0)) {
            USE_CYCLES(unchecked((long)((2 * MUL))));
            }
            FLAG_Z = (r_dst & mask);
            r_dst |= mask;
        }
    }
    private void m68k_op_bset_8_s_ai()
    {
        unchecked
        {
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(7))))))));
            uint ea = EA_AY_AI_8();
            uint src = m68ki_read_8(ea);
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src | mask));
        }
    }
    private void m68k_op_bset_8_s_pi()
    {
        unchecked
        {
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(7))))))));
            uint ea = EA_AY_PI_8();
            uint src = m68ki_read_8(ea);
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src | mask));
        }
    }
    private void m68k_op_bset_8_s_pi7()
    {
        unchecked
        {
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(7))))))));
            uint ea = EA_A7_PI_8();
            uint src = m68ki_read_8(ea);
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src | mask));
        }
    }
    private void m68k_op_bset_8_s_pd()
    {
        unchecked
        {
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(7))))))));
            uint ea = EA_AY_PD_8();
            uint src = m68ki_read_8(ea);
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src | mask));
        }
    }
    private void m68k_op_bset_8_s_pd7()
    {
        unchecked
        {
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(7))))))));
            uint ea = EA_A7_PD_8();
            uint src = m68ki_read_8(ea);
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src | mask));
        }
    }
    private void m68k_op_bset_8_s_di()
    {
        unchecked
        {
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(7))))))));
            uint ea = EA_AY_DI_8();
            uint src = m68ki_read_8(ea);
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src | mask));
        }
    }
    private void m68k_op_bset_8_s_ix()
    {
        unchecked
        {
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(7))))))));
            uint ea = EA_AY_IX_8();
            uint src = m68ki_read_8(ea);
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src | mask));
        }
    }
    private void m68k_op_bset_8_s_aw()
    {
        unchecked
        {
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(7))))))));
            uint ea = EA_AW_8();
            uint src = m68ki_read_8(ea);
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src | mask));
        }
    }
    private void m68k_op_bset_8_s_al()
    {
        unchecked
        {
            uint mask = unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(7))))))));
            uint ea = EA_AL_8();
            uint src = m68ki_read_8(ea);
            FLAG_Z = (src & mask);
            m68ki_write_8(ea, (src | mask));
        }
    }
    private void m68k_op_bsr_8()
    {
        unchecked
        {
            m68ki_push_32(REG_PC);
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
        }
    }
    private void m68k_op_bsr_16()
    {
        unchecked
        {
            uint offset = OPER_I_16();
            m68ki_push_32(REG_PC);
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
        }
    }
    private void m68k_op_bsr_32()
    {
        unchecked
        {
            m68ki_push_32(REG_PC);
            m68ki_branch_8(MASK_OUT_ABOVE_8(REG_IR));
        }
    }
    private void m68k_op_btst_32_r_d()
    {
        unchecked
        {
            FLAG_Z = (DY & unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(31)))))))));
        }
    }
    private void m68k_op_btst_8_r_ai()
    {
        unchecked
        {
            FLAG_Z = (OPER_AY_AI_8() & unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7)))))))));
        }
    }
    private void m68k_op_btst_8_r_pi()
    {
        unchecked
        {
            FLAG_Z = (OPER_AY_PI_8() & unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7)))))))));
        }
    }
    private void m68k_op_btst_8_r_pi7()
    {
        unchecked
        {
            FLAG_Z = (OPER_A7_PI_8() & unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7)))))))));
        }
    }
    private void m68k_op_btst_8_r_pd()
    {
        unchecked
        {
            FLAG_Z = (OPER_AY_PD_8() & unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7)))))))));
        }
    }
    private void m68k_op_btst_8_r_pd7()
    {
        unchecked
        {
            FLAG_Z = (OPER_A7_PD_8() & unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7)))))))));
        }
    }
    private void m68k_op_btst_8_r_di()
    {
        unchecked
        {
            FLAG_Z = (OPER_AY_DI_8() & unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7)))))))));
        }
    }
    private void m68k_op_btst_8_r_ix()
    {
        unchecked
        {
            FLAG_Z = (OPER_AY_IX_8() & unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7)))))))));
        }
    }
    private void m68k_op_btst_8_r_aw()
    {
        unchecked
        {
            FLAG_Z = (OPER_AW_8() & unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7)))))))));
        }
    }
    private void m68k_op_btst_8_r_al()
    {
        unchecked
        {
            FLAG_Z = (OPER_AL_8() & unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7)))))))));
        }
    }
    private void m68k_op_btst_8_r_pcdi()
    {
        unchecked
        {
            FLAG_Z = (OPER_PCDI_8() & unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7)))))))));
        }
    }
    private void m68k_op_btst_8_r_pcix()
    {
        unchecked
        {
            FLAG_Z = (OPER_PCIX_8() & unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7)))))))));
        }
    }
    private void m68k_op_btst_8_r_i()
    {
        unchecked
        {
            FLAG_Z = (OPER_I_8() & unchecked((uint)((1 << unchecked((int)((DX & unchecked((uint)(7)))))))));
        }
    }
    private void m68k_op_btst_32_s_d()
    {
        unchecked
        {
            FLAG_Z = (DY & unchecked((uint)((1 << unchecked((int)((OPER_I_8() & unchecked((uint)(31)))))))));
        }
    }
    private void m68k_op_btst_8_s_ai()
    {
        unchecked
        {
            uint bit = (OPER_I_8() & unchecked((uint)(7)));
            FLAG_Z = (OPER_AY_AI_8() & unchecked((uint)((1 << unchecked((int)(bit))))));
        }
    }
    private void m68k_op_btst_8_s_pi()
    {
        unchecked
        {
            uint bit = (OPER_I_8() & unchecked((uint)(7)));
            FLAG_Z = (OPER_AY_PI_8() & unchecked((uint)((1 << unchecked((int)(bit))))));
        }
    }
    private void m68k_op_btst_8_s_pi7()
    {
        unchecked
        {
            uint bit = (OPER_I_8() & unchecked((uint)(7)));
            FLAG_Z = (OPER_A7_PI_8() & unchecked((uint)((1 << unchecked((int)(bit))))));
        }
    }
    private void m68k_op_btst_8_s_pd()
    {
        unchecked
        {
            uint bit = (OPER_I_8() & unchecked((uint)(7)));
            FLAG_Z = (OPER_AY_PD_8() & unchecked((uint)((1 << unchecked((int)(bit))))));
        }
    }
    private void m68k_op_btst_8_s_pd7()
    {
        unchecked
        {
            uint bit = (OPER_I_8() & unchecked((uint)(7)));
            FLAG_Z = (OPER_A7_PD_8() & unchecked((uint)((1 << unchecked((int)(bit))))));
        }
    }
    private void m68k_op_btst_8_s_di()
    {
        unchecked
        {
            uint bit = (OPER_I_8() & unchecked((uint)(7)));
            FLAG_Z = (OPER_AY_DI_8() & unchecked((uint)((1 << unchecked((int)(bit))))));
        }
    }
    private void m68k_op_btst_8_s_ix()
    {
        unchecked
        {
            uint bit = (OPER_I_8() & unchecked((uint)(7)));
            FLAG_Z = (OPER_AY_IX_8() & unchecked((uint)((1 << unchecked((int)(bit))))));
        }
    }
    private void m68k_op_btst_8_s_aw()
    {
        unchecked
        {
            uint bit = (OPER_I_8() & unchecked((uint)(7)));
            FLAG_Z = (OPER_AW_8() & unchecked((uint)((1 << unchecked((int)(bit))))));
        }
    }
    private void m68k_op_btst_8_s_al()
    {
        unchecked
        {
            uint bit = (OPER_I_8() & unchecked((uint)(7)));
            FLAG_Z = (OPER_AL_8() & unchecked((uint)((1 << unchecked((int)(bit))))));
        }
    }
    private void m68k_op_btst_8_s_pcdi()
    {
        unchecked
        {
            uint bit = (OPER_I_8() & unchecked((uint)(7)));
            FLAG_Z = (OPER_PCDI_8() & unchecked((uint)((1 << unchecked((int)(bit))))));
        }
    }
    private void m68k_op_btst_8_s_pcix()
    {
        unchecked
        {
            uint bit = (OPER_I_8() & unchecked((uint)(7)));
            FLAG_Z = (OPER_PCIX_8() & unchecked((uint)((1 << unchecked((int)(bit))))));
        }
    }
    private void m68k_op_chk_16_d()
    {
        unchecked
        {
            int src = MAKE_INT_16(DX);
            int bound = MAKE_INT_16(DY);
            FLAG_Z = ZFLAG_16(unchecked((uint)(src)));
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            if (((src >= 0) && (src <= bound))) {
            USE_CYCLES(unchecked((long)((10 * MUL))));
            return;
            }
            if ((src < 0)) {
            FLAG_N = unchecked((uint)((1 << 7)));
            USE_CYCLES(unchecked((long)((2 * MUL))));
            }
            m68ki_exception_trap(EXCEPTION_CHK);
        }
    }
    private void m68k_op_chk_16_ai()
    {
        unchecked
        {
            int src = MAKE_INT_16(DX);
            int bound = MAKE_INT_16(OPER_AY_AI_16());
            FLAG_Z = ZFLAG_16(unchecked((uint)(src)));
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            if (((src >= 0) && (src <= bound))) {
            USE_CYCLES(unchecked((long)((10 * MUL))));
            return;
            }
            if ((src < 0)) {
            FLAG_N = unchecked((uint)((1 << 7)));
            USE_CYCLES(unchecked((long)((2 * MUL))));
            }
            m68ki_exception_trap(EXCEPTION_CHK);
        }
    }
    private void m68k_op_chk_16_pi()
    {
        unchecked
        {
            int src = MAKE_INT_16(DX);
            int bound = MAKE_INT_16(OPER_AY_PI_16());
            FLAG_Z = ZFLAG_16(unchecked((uint)(src)));
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            if (((src >= 0) && (src <= bound))) {
            USE_CYCLES(unchecked((long)((10 * MUL))));
            return;
            }
            if ((src < 0)) {
            FLAG_N = unchecked((uint)((1 << 7)));
            USE_CYCLES(unchecked((long)((2 * MUL))));
            }
            m68ki_exception_trap(EXCEPTION_CHK);
        }
    }
    private void m68k_op_chk_16_pd()
    {
        unchecked
        {
            int src = MAKE_INT_16(DX);
            int bound = MAKE_INT_16(OPER_AY_PD_16());
            FLAG_Z = ZFLAG_16(unchecked((uint)(src)));
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            if (((src >= 0) && (src <= bound))) {
            USE_CYCLES(unchecked((long)((10 * MUL))));
            return;
            }
            if ((src < 0)) {
            FLAG_N = unchecked((uint)((1 << 7)));
            USE_CYCLES(unchecked((long)((2 * MUL))));
            }
            m68ki_exception_trap(EXCEPTION_CHK);
        }
    }
    private void m68k_op_chk_16_di()
    {
        unchecked
        {
            int src = MAKE_INT_16(DX);
            int bound = MAKE_INT_16(OPER_AY_DI_16());
            FLAG_Z = ZFLAG_16(unchecked((uint)(src)));
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            if (((src >= 0) && (src <= bound))) {
            USE_CYCLES(unchecked((long)((10 * MUL))));
            return;
            }
            if ((src < 0)) {
            FLAG_N = unchecked((uint)((1 << 7)));
            USE_CYCLES(unchecked((long)((2 * MUL))));
            }
            m68ki_exception_trap(EXCEPTION_CHK);
        }
    }
    private void m68k_op_chk_16_ix()
    {
        unchecked
        {
            int src = MAKE_INT_16(DX);
            int bound = MAKE_INT_16(OPER_AY_IX_16());
            FLAG_Z = ZFLAG_16(unchecked((uint)(src)));
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            if (((src >= 0) && (src <= bound))) {
            USE_CYCLES(unchecked((long)((10 * MUL))));
            return;
            }
            if ((src < 0)) {
            FLAG_N = unchecked((uint)((1 << 7)));
            USE_CYCLES(unchecked((long)((2 * MUL))));
            }
            m68ki_exception_trap(EXCEPTION_CHK);
        }
    }
    private void m68k_op_chk_16_aw()
    {
        unchecked
        {
            int src = MAKE_INT_16(DX);
            int bound = MAKE_INT_16(OPER_AW_16());
            FLAG_Z = ZFLAG_16(unchecked((uint)(src)));
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            if (((src >= 0) && (src <= bound))) {
            USE_CYCLES(unchecked((long)((10 * MUL))));
            return;
            }
            if ((src < 0)) {
            FLAG_N = unchecked((uint)((1 << 7)));
            USE_CYCLES(unchecked((long)((2 * MUL))));
            }
            m68ki_exception_trap(EXCEPTION_CHK);
        }
    }
    private void m68k_op_chk_16_al()
    {
        unchecked
        {
            int src = MAKE_INT_16(DX);
            int bound = MAKE_INT_16(OPER_AL_16());
            FLAG_Z = ZFLAG_16(unchecked((uint)(src)));
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            if (((src >= 0) && (src <= bound))) {
            USE_CYCLES(unchecked((long)((10 * MUL))));
            return;
            }
            if ((src < 0)) {
            FLAG_N = unchecked((uint)((1 << 7)));
            USE_CYCLES(unchecked((long)((2 * MUL))));
            }
            m68ki_exception_trap(EXCEPTION_CHK);
        }
    }
    private void m68k_op_chk_16_pcdi()
    {
        unchecked
        {
            int src = MAKE_INT_16(DX);
            int bound = MAKE_INT_16(OPER_PCDI_16());
            FLAG_Z = ZFLAG_16(unchecked((uint)(src)));
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            if (((src >= 0) && (src <= bound))) {
            USE_CYCLES(unchecked((long)((10 * MUL))));
            return;
            }
            if ((src < 0)) {
            FLAG_N = unchecked((uint)((1 << 7)));
            USE_CYCLES(unchecked((long)((2 * MUL))));
            }
            m68ki_exception_trap(EXCEPTION_CHK);
        }
    }
    private void m68k_op_chk_16_pcix()
    {
        unchecked
        {
            int src = MAKE_INT_16(DX);
            int bound = MAKE_INT_16(OPER_PCIX_16());
            FLAG_Z = ZFLAG_16(unchecked((uint)(src)));
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            if (((src >= 0) && (src <= bound))) {
            USE_CYCLES(unchecked((long)((10 * MUL))));
            return;
            }
            if ((src < 0)) {
            FLAG_N = unchecked((uint)((1 << 7)));
            USE_CYCLES(unchecked((long)((2 * MUL))));
            }
            m68ki_exception_trap(EXCEPTION_CHK);
        }
    }
    private void m68k_op_chk_16_i()
    {
        unchecked
        {
            int src = MAKE_INT_16(DX);
            int bound = MAKE_INT_16(OPER_I_16());
            FLAG_Z = ZFLAG_16(unchecked((uint)(src)));
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            if (((src >= 0) && (src <= bound))) {
            USE_CYCLES(unchecked((long)((10 * MUL))));
            return;
            }
            if ((src < 0)) {
            FLAG_N = unchecked((uint)((1 << 7)));
            USE_CYCLES(unchecked((long)((2 * MUL))));
            }
            m68ki_exception_trap(EXCEPTION_CHK);
        }
    }
    private void m68k_op_clr_8_d()
    {
        unchecked
        {
            DY &= 4294967040u;
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
        }
    }
    private void m68k_op_clr_8_ai()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_AI_8(), unchecked((uint)(0)));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
        }
    }
    private void m68k_op_clr_8_pi()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PI_8(), unchecked((uint)(0)));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
        }
    }
    private void m68k_op_clr_8_pi7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PI_8(), unchecked((uint)(0)));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
        }
    }
    private void m68k_op_clr_8_pd()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PD_8(), unchecked((uint)(0)));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
        }
    }
    private void m68k_op_clr_8_pd7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PD_8(), unchecked((uint)(0)));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
        }
    }
    private void m68k_op_clr_8_di()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_DI_8(), unchecked((uint)(0)));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
        }
    }
    private void m68k_op_clr_8_ix()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_IX_8(), unchecked((uint)(0)));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
        }
    }
    private void m68k_op_clr_8_aw()
    {
        unchecked
        {
            m68ki_write_8(EA_AW_8(), unchecked((uint)(0)));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
        }
    }
    private void m68k_op_clr_8_al()
    {
        unchecked
        {
            m68ki_write_8(EA_AL_8(), unchecked((uint)(0)));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
        }
    }
    private void m68k_op_clr_16_d()
    {
        unchecked
        {
            DY &= 4294901760u;
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
        }
    }
    private void m68k_op_clr_16_ai()
    {
        unchecked
        {
            m68ki_write_16(EA_AY_AI_16(), unchecked((uint)(0)));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
        }
    }
    private void m68k_op_clr_16_pi()
    {
        unchecked
        {
            m68ki_write_16(EA_AY_PI_16(), unchecked((uint)(0)));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
        }
    }
    private void m68k_op_clr_16_pd()
    {
        unchecked
        {
            m68ki_write_16(EA_AY_PD_16(), unchecked((uint)(0)));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
        }
    }
    private void m68k_op_clr_16_di()
    {
        unchecked
        {
            m68ki_write_16(EA_AY_DI_16(), unchecked((uint)(0)));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
        }
    }
    private void m68k_op_clr_16_ix()
    {
        unchecked
        {
            m68ki_write_16(EA_AY_IX_16(), unchecked((uint)(0)));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
        }
    }
    private void m68k_op_clr_16_aw()
    {
        unchecked
        {
            m68ki_write_16(EA_AW_16(), unchecked((uint)(0)));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
        }
    }
    private void m68k_op_clr_16_al()
    {
        unchecked
        {
            m68ki_write_16(EA_AL_16(), unchecked((uint)(0)));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
        }
    }
    private void m68k_op_clr_32_d()
    {
        unchecked
        {
            DY = unchecked((uint)(0));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
        }
    }
    private void m68k_op_clr_32_ai()
    {
        unchecked
        {
            m68ki_write_32(EA_AY_AI_32(), unchecked((uint)(0)));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
        }
    }
    private void m68k_op_clr_32_pi()
    {
        unchecked
        {
            m68ki_write_32(EA_AY_PI_32(), unchecked((uint)(0)));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
        }
    }
    private void m68k_op_clr_32_pd()
    {
        unchecked
        {
            m68ki_write_32(EA_AY_PD_32(), unchecked((uint)(0)));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
        }
    }
    private void m68k_op_clr_32_di()
    {
        unchecked
        {
            m68ki_write_32(EA_AY_DI_32(), unchecked((uint)(0)));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
        }
    }
    private void m68k_op_clr_32_ix()
    {
        unchecked
        {
            m68ki_write_32(EA_AY_IX_32(), unchecked((uint)(0)));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
        }
    }
    private void m68k_op_clr_32_aw()
    {
        unchecked
        {
            m68ki_write_32(EA_AW_32(), unchecked((uint)(0)));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
        }
    }
    private void m68k_op_clr_32_al()
    {
        unchecked
        {
            m68ki_write_32(EA_AL_32(), unchecked((uint)(0)));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
        }
    }
    private void m68k_op_cmp_8_d()
    {
        unchecked
        {
            uint src = MASK_OUT_ABOVE_8(DY);
            uint dst = MASK_OUT_ABOVE_8(DX);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_C = CFLAG_8(res);
        }
    }
    private void m68k_op_cmp_8_ai()
    {
        unchecked
        {
            uint src = OPER_AY_AI_8();
            uint dst = MASK_OUT_ABOVE_8(DX);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_C = CFLAG_8(res);
        }
    }
    private void m68k_op_cmp_8_pi()
    {
        unchecked
        {
            uint src = OPER_AY_PI_8();
            uint dst = MASK_OUT_ABOVE_8(DX);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_C = CFLAG_8(res);
        }
    }
    private void m68k_op_cmp_8_pi7()
    {
        unchecked
        {
            uint src = OPER_A7_PI_8();
            uint dst = MASK_OUT_ABOVE_8(DX);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_C = CFLAG_8(res);
        }
    }
    private void m68k_op_cmp_8_pd()
    {
        unchecked
        {
            uint src = OPER_AY_PD_8();
            uint dst = MASK_OUT_ABOVE_8(DX);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_C = CFLAG_8(res);
        }
    }
    private void m68k_op_cmp_8_pd7()
    {
        unchecked
        {
            uint src = OPER_A7_PD_8();
            uint dst = MASK_OUT_ABOVE_8(DX);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_C = CFLAG_8(res);
        }
    }
    private void m68k_op_cmp_8_di()
    {
        unchecked
        {
            uint src = OPER_AY_DI_8();
            uint dst = MASK_OUT_ABOVE_8(DX);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_C = CFLAG_8(res);
        }
    }
    private void m68k_op_cmp_8_ix()
    {
        unchecked
        {
            uint src = OPER_AY_IX_8();
            uint dst = MASK_OUT_ABOVE_8(DX);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_C = CFLAG_8(res);
        }
    }
    private void m68k_op_cmp_8_aw()
    {
        unchecked
        {
            uint src = OPER_AW_8();
            uint dst = MASK_OUT_ABOVE_8(DX);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_C = CFLAG_8(res);
        }
    }
    private void m68k_op_cmp_8_al()
    {
        unchecked
        {
            uint src = OPER_AL_8();
            uint dst = MASK_OUT_ABOVE_8(DX);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_C = CFLAG_8(res);
        }
    }
    private void m68k_op_cmp_8_pcdi()
    {
        unchecked
        {
            uint src = OPER_PCDI_8();
            uint dst = MASK_OUT_ABOVE_8(DX);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_C = CFLAG_8(res);
        }
    }
    private void m68k_op_cmp_8_pcix()
    {
        unchecked
        {
            uint src = OPER_PCIX_8();
            uint dst = MASK_OUT_ABOVE_8(DX);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_C = CFLAG_8(res);
        }
    }
    private void m68k_op_cmp_8_i()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint dst = MASK_OUT_ABOVE_8(DX);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_C = CFLAG_8(res);
        }
    }
    private void m68k_op_cmp_16_d()
    {
        unchecked
        {
            uint src = MASK_OUT_ABOVE_16(DY);
            uint dst = MASK_OUT_ABOVE_16(DX);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_C = CFLAG_16(res);
        }
    }
    private void m68k_op_cmp_16_a()
    {
        unchecked
        {
            uint src = MASK_OUT_ABOVE_16(AY);
            uint dst = MASK_OUT_ABOVE_16(DX);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_C = CFLAG_16(res);
        }
    }
    private void m68k_op_cmp_16_ai()
    {
        unchecked
        {
            uint src = OPER_AY_AI_16();
            uint dst = MASK_OUT_ABOVE_16(DX);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_C = CFLAG_16(res);
        }
    }
    private void m68k_op_cmp_16_pi()
    {
        unchecked
        {
            uint src = OPER_AY_PI_16();
            uint dst = MASK_OUT_ABOVE_16(DX);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_C = CFLAG_16(res);
        }
    }
    private void m68k_op_cmp_16_pd()
    {
        unchecked
        {
            uint src = OPER_AY_PD_16();
            uint dst = MASK_OUT_ABOVE_16(DX);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_C = CFLAG_16(res);
        }
    }
    private void m68k_op_cmp_16_di()
    {
        unchecked
        {
            uint src = OPER_AY_DI_16();
            uint dst = MASK_OUT_ABOVE_16(DX);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_C = CFLAG_16(res);
        }
    }
    private void m68k_op_cmp_16_ix()
    {
        unchecked
        {
            uint src = OPER_AY_IX_16();
            uint dst = MASK_OUT_ABOVE_16(DX);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_C = CFLAG_16(res);
        }
    }
    private void m68k_op_cmp_16_aw()
    {
        unchecked
        {
            uint src = OPER_AW_16();
            uint dst = MASK_OUT_ABOVE_16(DX);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_C = CFLAG_16(res);
        }
    }
    private void m68k_op_cmp_16_al()
    {
        unchecked
        {
            uint src = OPER_AL_16();
            uint dst = MASK_OUT_ABOVE_16(DX);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_C = CFLAG_16(res);
        }
    }
    private void m68k_op_cmp_16_pcdi()
    {
        unchecked
        {
            uint src = OPER_PCDI_16();
            uint dst = MASK_OUT_ABOVE_16(DX);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_C = CFLAG_16(res);
        }
    }
    private void m68k_op_cmp_16_pcix()
    {
        unchecked
        {
            uint src = OPER_PCIX_16();
            uint dst = MASK_OUT_ABOVE_16(DX);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_C = CFLAG_16(res);
        }
    }
    private void m68k_op_cmp_16_i()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint dst = MASK_OUT_ABOVE_16(DX);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_C = CFLAG_16(res);
        }
    }
    private void m68k_op_cmp_32_d()
    {
        unchecked
        {
            uint src = DY;
            uint dst = DX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmp_32_a()
    {
        unchecked
        {
            uint src = AY;
            uint dst = DX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmp_32_ai()
    {
        unchecked
        {
            uint src = OPER_AY_AI_32();
            uint dst = DX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmp_32_pi()
    {
        unchecked
        {
            uint src = OPER_AY_PI_32();
            uint dst = DX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmp_32_pd()
    {
        unchecked
        {
            uint src = OPER_AY_PD_32();
            uint dst = DX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmp_32_di()
    {
        unchecked
        {
            uint src = OPER_AY_DI_32();
            uint dst = DX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmp_32_ix()
    {
        unchecked
        {
            uint src = OPER_AY_IX_32();
            uint dst = DX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmp_32_aw()
    {
        unchecked
        {
            uint src = OPER_AW_32();
            uint dst = DX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmp_32_al()
    {
        unchecked
        {
            uint src = OPER_AL_32();
            uint dst = DX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmp_32_pcdi()
    {
        unchecked
        {
            uint src = OPER_PCDI_32();
            uint dst = DX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmp_32_pcix()
    {
        unchecked
        {
            uint src = OPER_PCIX_32();
            uint dst = DX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmp_32_i()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint dst = DX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpa_16_d()
    {
        unchecked
        {
            uint src = unchecked((uint)(MAKE_INT_16(DY)));
            uint dst = AX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpa_16_a()
    {
        unchecked
        {
            uint src = unchecked((uint)(MAKE_INT_16(AY)));
            uint dst = AX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpa_16_ai()
    {
        unchecked
        {
            uint src = unchecked((uint)(MAKE_INT_16(OPER_AY_AI_16())));
            uint dst = AX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpa_16_pi()
    {
        unchecked
        {
            uint src = unchecked((uint)(MAKE_INT_16(OPER_AY_PI_16())));
            uint dst = AX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpa_16_pd()
    {
        unchecked
        {
            uint src = unchecked((uint)(MAKE_INT_16(OPER_AY_PD_16())));
            uint dst = AX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpa_16_di()
    {
        unchecked
        {
            uint src = unchecked((uint)(MAKE_INT_16(OPER_AY_DI_16())));
            uint dst = AX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpa_16_ix()
    {
        unchecked
        {
            uint src = unchecked((uint)(MAKE_INT_16(OPER_AY_IX_16())));
            uint dst = AX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpa_16_aw()
    {
        unchecked
        {
            uint src = unchecked((uint)(MAKE_INT_16(OPER_AW_16())));
            uint dst = AX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpa_16_al()
    {
        unchecked
        {
            uint src = unchecked((uint)(MAKE_INT_16(OPER_AL_16())));
            uint dst = AX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpa_16_pcdi()
    {
        unchecked
        {
            uint src = unchecked((uint)(MAKE_INT_16(OPER_PCDI_16())));
            uint dst = AX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpa_16_pcix()
    {
        unchecked
        {
            uint src = unchecked((uint)(MAKE_INT_16(OPER_PCIX_16())));
            uint dst = AX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpa_16_i()
    {
        unchecked
        {
            uint src = unchecked((uint)(MAKE_INT_16(OPER_I_16())));
            uint dst = AX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpa_32_d()
    {
        unchecked
        {
            uint src = DY;
            uint dst = AX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpa_32_a()
    {
        unchecked
        {
            uint src = AY;
            uint dst = AX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpa_32_ai()
    {
        unchecked
        {
            uint src = OPER_AY_AI_32();
            uint dst = AX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpa_32_pi()
    {
        unchecked
        {
            uint src = OPER_AY_PI_32();
            uint dst = AX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpa_32_pd()
    {
        unchecked
        {
            uint src = OPER_AY_PD_32();
            uint dst = AX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpa_32_di()
    {
        unchecked
        {
            uint src = OPER_AY_DI_32();
            uint dst = AX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpa_32_ix()
    {
        unchecked
        {
            uint src = OPER_AY_IX_32();
            uint dst = AX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpa_32_aw()
    {
        unchecked
        {
            uint src = OPER_AW_32();
            uint dst = AX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpa_32_al()
    {
        unchecked
        {
            uint src = OPER_AL_32();
            uint dst = AX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpa_32_pcdi()
    {
        unchecked
        {
            uint src = OPER_PCDI_32();
            uint dst = AX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpa_32_pcix()
    {
        unchecked
        {
            uint src = OPER_PCIX_32();
            uint dst = AX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpa_32_i()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint dst = AX;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpi_8_d()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint dst = MASK_OUT_ABOVE_8(DY);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_C = CFLAG_8(res);
        }
    }
    private void m68k_op_cmpi_8_ai()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint dst = OPER_AY_AI_8();
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_C = CFLAG_8(res);
        }
    }
    private void m68k_op_cmpi_8_pi()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint dst = OPER_AY_PI_8();
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_C = CFLAG_8(res);
        }
    }
    private void m68k_op_cmpi_8_pi7()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint dst = OPER_A7_PI_8();
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_C = CFLAG_8(res);
        }
    }
    private void m68k_op_cmpi_8_pd()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint dst = OPER_AY_PD_8();
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_C = CFLAG_8(res);
        }
    }
    private void m68k_op_cmpi_8_pd7()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint dst = OPER_A7_PD_8();
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_C = CFLAG_8(res);
        }
    }
    private void m68k_op_cmpi_8_di()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint dst = OPER_AY_DI_8();
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_C = CFLAG_8(res);
        }
    }
    private void m68k_op_cmpi_8_ix()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint dst = OPER_AY_IX_8();
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_C = CFLAG_8(res);
        }
    }
    private void m68k_op_cmpi_8_aw()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint dst = OPER_AW_8();
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_C = CFLAG_8(res);
        }
    }
    private void m68k_op_cmpi_8_al()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint dst = OPER_AL_8();
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_C = CFLAG_8(res);
        }
    }
    private void m68k_op_cmpi_16_d()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint dst = MASK_OUT_ABOVE_16(DY);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_C = CFLAG_16(res);
        }
    }
    private void m68k_op_cmpi_16_ai()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint dst = OPER_AY_AI_16();
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_C = CFLAG_16(res);
        }
    }
    private void m68k_op_cmpi_16_pi()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint dst = OPER_AY_PI_16();
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_C = CFLAG_16(res);
        }
    }
    private void m68k_op_cmpi_16_pd()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint dst = OPER_AY_PD_16();
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_C = CFLAG_16(res);
        }
    }
    private void m68k_op_cmpi_16_di()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint dst = OPER_AY_DI_16();
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_C = CFLAG_16(res);
        }
    }
    private void m68k_op_cmpi_16_ix()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint dst = OPER_AY_IX_16();
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_C = CFLAG_16(res);
        }
    }
    private void m68k_op_cmpi_16_aw()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint dst = OPER_AW_16();
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_C = CFLAG_16(res);
        }
    }
    private void m68k_op_cmpi_16_al()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint dst = OPER_AL_16();
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_C = CFLAG_16(res);
        }
    }
    private void m68k_op_cmpi_32_d()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint dst = DY;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpi_32_ai()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint dst = OPER_AY_AI_32();
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpi_32_pi()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint dst = OPER_AY_PI_32();
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpi_32_pd()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint dst = OPER_AY_PD_32();
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpi_32_di()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint dst = OPER_AY_DI_32();
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpi_32_ix()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint dst = OPER_AY_IX_32();
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpi_32_aw()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint dst = OPER_AW_32();
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpi_32_al()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint dst = OPER_AL_32();
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_cmpm_8_ax7()
    {
        unchecked
        {
            uint src = OPER_AY_PI_8();
            uint dst = OPER_A7_PI_8();
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_C = CFLAG_8(res);
        }
    }
    private void m68k_op_cmpm_8_ay7()
    {
        unchecked
        {
            uint src = OPER_A7_PI_8();
            uint dst = OPER_AX_PI_8();
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_C = CFLAG_8(res);
        }
    }
    private void m68k_op_cmpm_8_axy7()
    {
        unchecked
        {
            uint src = OPER_A7_PI_8();
            uint dst = OPER_A7_PI_8();
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_C = CFLAG_8(res);
        }
    }
    private void m68k_op_cmpm_8()
    {
        unchecked
        {
            uint src = OPER_AY_PI_8();
            uint dst = OPER_AX_PI_8();
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_C = CFLAG_8(res);
        }
    }
    private void m68k_op_cmpm_16()
    {
        unchecked
        {
            uint src = OPER_AY_PI_16();
            uint dst = OPER_AX_PI_16();
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_C = CFLAG_16(res);
        }
    }
    private void m68k_op_cmpm_32()
    {
        unchecked
        {
            uint src = OPER_AY_PI_32();
            uint dst = OPER_AX_PI_32();
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_C = CFLAG_SUB_32(src, dst, res);
        }
    }
    private void m68k_op_dbt_16()
    {
        unchecked
        {
            REG_PC += unchecked((uint)(2));
        }
    }
    private void m68k_op_dbf_16()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint res = MASK_OUT_ABOVE_16((r_dst - unchecked((uint)(1))));
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            if ((res != unchecked((uint)(65535)))) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_NOEXP)));
            _state.poll_detected = unchecked((uint)(0));
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_EXP)));
        }
    }
    private void m68k_op_dbhi_16()
    {
        unchecked
        {
            if ((COND_NOT_HI() != 0)) {
            ref uint r_dst = ref DY;
            uint res = MASK_OUT_ABOVE_16((r_dst - unchecked((uint)(1))));
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            if ((res != unchecked((uint)(65535)))) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_NOEXP)));
            _state.poll_detected = unchecked((uint)(0));
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_EXP)));
            return;
            }
            REG_PC += unchecked((uint)(2));
        }
    }
    private void m68k_op_dbls_16()
    {
        unchecked
        {
            if ((COND_NOT_LS() != 0)) {
            ref uint r_dst = ref DY;
            uint res = MASK_OUT_ABOVE_16((r_dst - unchecked((uint)(1))));
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            if ((res != unchecked((uint)(65535)))) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_NOEXP)));
            _state.poll_detected = unchecked((uint)(0));
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_EXP)));
            return;
            }
            REG_PC += unchecked((uint)(2));
        }
    }
    private void m68k_op_dbcc_16()
    {
        unchecked
        {
            if ((COND_NOT_CC() != 0)) {
            ref uint r_dst = ref DY;
            uint res = MASK_OUT_ABOVE_16((r_dst - unchecked((uint)(1))));
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            if ((res != unchecked((uint)(65535)))) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_NOEXP)));
            _state.poll_detected = unchecked((uint)(0));
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_EXP)));
            return;
            }
            REG_PC += unchecked((uint)(2));
        }
    }
    private void m68k_op_dbcs_16()
    {
        unchecked
        {
            if ((COND_NOT_CS() != 0)) {
            ref uint r_dst = ref DY;
            uint res = MASK_OUT_ABOVE_16((r_dst - unchecked((uint)(1))));
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            if ((res != unchecked((uint)(65535)))) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_NOEXP)));
            _state.poll_detected = unchecked((uint)(0));
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_EXP)));
            return;
            }
            REG_PC += unchecked((uint)(2));
        }
    }
    private void m68k_op_dbne_16()
    {
        unchecked
        {
            if ((COND_NOT_NE() != 0)) {
            ref uint r_dst = ref DY;
            uint res = MASK_OUT_ABOVE_16((r_dst - unchecked((uint)(1))));
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            if ((res != unchecked((uint)(65535)))) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_NOEXP)));
            _state.poll_detected = unchecked((uint)(0));
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_EXP)));
            return;
            }
            REG_PC += unchecked((uint)(2));
        }
    }
    private void m68k_op_dbeq_16()
    {
        unchecked
        {
            if ((COND_NOT_EQ() != 0)) {
            ref uint r_dst = ref DY;
            uint res = MASK_OUT_ABOVE_16((r_dst - unchecked((uint)(1))));
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            if ((res != unchecked((uint)(65535)))) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_NOEXP)));
            _state.poll_detected = unchecked((uint)(0));
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_EXP)));
            return;
            }
            REG_PC += unchecked((uint)(2));
        }
    }
    private void m68k_op_dbvc_16()
    {
        unchecked
        {
            if ((COND_NOT_VC() != 0)) {
            ref uint r_dst = ref DY;
            uint res = MASK_OUT_ABOVE_16((r_dst - unchecked((uint)(1))));
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            if ((res != unchecked((uint)(65535)))) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_NOEXP)));
            _state.poll_detected = unchecked((uint)(0));
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_EXP)));
            return;
            }
            REG_PC += unchecked((uint)(2));
        }
    }
    private void m68k_op_dbvs_16()
    {
        unchecked
        {
            if ((COND_NOT_VS() != 0)) {
            ref uint r_dst = ref DY;
            uint res = MASK_OUT_ABOVE_16((r_dst - unchecked((uint)(1))));
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            if ((res != unchecked((uint)(65535)))) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_NOEXP)));
            _state.poll_detected = unchecked((uint)(0));
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_EXP)));
            return;
            }
            REG_PC += unchecked((uint)(2));
        }
    }
    private void m68k_op_dbpl_16()
    {
        unchecked
        {
            if ((COND_NOT_PL() != 0)) {
            ref uint r_dst = ref DY;
            uint res = MASK_OUT_ABOVE_16((r_dst - unchecked((uint)(1))));
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            if ((res != unchecked((uint)(65535)))) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_NOEXP)));
            _state.poll_detected = unchecked((uint)(0));
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_EXP)));
            return;
            }
            REG_PC += unchecked((uint)(2));
        }
    }
    private void m68k_op_dbmi_16()
    {
        unchecked
        {
            if ((COND_NOT_MI() != 0)) {
            ref uint r_dst = ref DY;
            uint res = MASK_OUT_ABOVE_16((r_dst - unchecked((uint)(1))));
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            if ((res != unchecked((uint)(65535)))) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_NOEXP)));
            _state.poll_detected = unchecked((uint)(0));
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_EXP)));
            return;
            }
            REG_PC += unchecked((uint)(2));
        }
    }
    private void m68k_op_dbge_16()
    {
        unchecked
        {
            if ((COND_NOT_GE() != 0)) {
            ref uint r_dst = ref DY;
            uint res = MASK_OUT_ABOVE_16((r_dst - unchecked((uint)(1))));
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            if ((res != unchecked((uint)(65535)))) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_NOEXP)));
            _state.poll_detected = unchecked((uint)(0));
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_EXP)));
            return;
            }
            REG_PC += unchecked((uint)(2));
        }
    }
    private void m68k_op_dblt_16()
    {
        unchecked
        {
            if ((COND_NOT_LT() != 0)) {
            ref uint r_dst = ref DY;
            uint res = MASK_OUT_ABOVE_16((r_dst - unchecked((uint)(1))));
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            if ((res != unchecked((uint)(65535)))) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_NOEXP)));
            _state.poll_detected = unchecked((uint)(0));
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_EXP)));
            return;
            }
            REG_PC += unchecked((uint)(2));
        }
    }
    private void m68k_op_dbgt_16()
    {
        unchecked
        {
            if ((COND_NOT_GT() != 0)) {
            ref uint r_dst = ref DY;
            uint res = MASK_OUT_ABOVE_16((r_dst - unchecked((uint)(1))));
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            if ((res != unchecked((uint)(65535)))) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_NOEXP)));
            _state.poll_detected = unchecked((uint)(0));
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_EXP)));
            return;
            }
            REG_PC += unchecked((uint)(2));
        }
    }
    private void m68k_op_dble_16()
    {
        unchecked
        {
            if ((COND_NOT_LE() != 0)) {
            ref uint r_dst = ref DY;
            uint res = MASK_OUT_ABOVE_16((r_dst - unchecked((uint)(1))));
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            if ((res != unchecked((uint)(65535)))) {
            uint offset = OPER_I_16();
            REG_PC -= unchecked((uint)(2));
            m68ki_branch_16(offset);
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_NOEXP)));
            _state.poll_detected = unchecked((uint)(0));
            return;
            }
            REG_PC += unchecked((uint)(2));
            USE_CYCLES(unchecked((long)(CYC_DBCC_F_EXP)));
            return;
            }
            REG_PC += unchecked((uint)(2));
        }
    }
    private void m68k_op_divs_16_d()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            int src = MAKE_INT_16(DY);
            int quotient = default;
            int remainder = default;
            if ((src != 0)) {
            UseDivsCycles(unchecked((int)(r_dst)), unchecked((short)(src)));
            if (((r_dst == 2147483648u) && (src == (-1)))) {
            FLAG_Z = unchecked((uint)(0));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = unchecked((uint)(0));
            return;
            }
            quotient = (MAKE_INT_32(r_dst) / src);
            remainder = (MAKE_INT_32(r_dst) % src);
            if ((quotient == MAKE_INT_16(unchecked((uint)(quotient))))) {
            FLAG_Z = unchecked((uint)(quotient));
            FLAG_N = NFLAG_16(unchecked((uint)(quotient)));
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = MASK_OUT_ABOVE_32((MASK_OUT_ABOVE_16(unchecked((uint)(quotient))) | unchecked((uint)((remainder << 16)))));
            return;
            }
            FLAG_V = VFLAG_SET;
            FLAG_N = NFLAG_SET;
            FLAG_C = CFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            m68ki_exception_trap(EXCEPTION_ZERO_DIVIDE);
        }
    }
    private void m68k_op_divs_16_ai()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            int src = MAKE_INT_16(OPER_AY_AI_16());
            int quotient = default;
            int remainder = default;
            if ((src != 0)) {
            UseDivsCycles(unchecked((int)(r_dst)), unchecked((short)(src)));
            if (((r_dst == 2147483648u) && (src == (-1)))) {
            FLAG_Z = unchecked((uint)(0));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = unchecked((uint)(0));
            return;
            }
            quotient = (MAKE_INT_32(r_dst) / src);
            remainder = (MAKE_INT_32(r_dst) % src);
            if ((quotient == MAKE_INT_16(unchecked((uint)(quotient))))) {
            FLAG_Z = unchecked((uint)(quotient));
            FLAG_N = NFLAG_16(unchecked((uint)(quotient)));
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = MASK_OUT_ABOVE_32((MASK_OUT_ABOVE_16(unchecked((uint)(quotient))) | unchecked((uint)((remainder << 16)))));
            return;
            }
            FLAG_V = VFLAG_SET;
            FLAG_N = NFLAG_SET;
            FLAG_C = CFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            m68ki_exception_trap(EXCEPTION_ZERO_DIVIDE);
        }
    }
    private void m68k_op_divs_16_pi()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            int src = MAKE_INT_16(OPER_AY_PI_16());
            int quotient = default;
            int remainder = default;
            if ((src != 0)) {
            UseDivsCycles(unchecked((int)(r_dst)), unchecked((short)(src)));
            if (((r_dst == 2147483648u) && (src == (-1)))) {
            FLAG_Z = unchecked((uint)(0));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = unchecked((uint)(0));
            return;
            }
            quotient = (MAKE_INT_32(r_dst) / src);
            remainder = (MAKE_INT_32(r_dst) % src);
            if ((quotient == MAKE_INT_16(unchecked((uint)(quotient))))) {
            FLAG_Z = unchecked((uint)(quotient));
            FLAG_N = NFLAG_16(unchecked((uint)(quotient)));
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = MASK_OUT_ABOVE_32((MASK_OUT_ABOVE_16(unchecked((uint)(quotient))) | unchecked((uint)((remainder << 16)))));
            return;
            }
            FLAG_V = VFLAG_SET;
            FLAG_N = NFLAG_SET;
            FLAG_C = CFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            m68ki_exception_trap(EXCEPTION_ZERO_DIVIDE);
        }
    }
    private void m68k_op_divs_16_pd()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            int src = MAKE_INT_16(OPER_AY_PD_16());
            int quotient = default;
            int remainder = default;
            if ((src != 0)) {
            UseDivsCycles(unchecked((int)(r_dst)), unchecked((short)(src)));
            if (((r_dst == 2147483648u) && (src == (-1)))) {
            FLAG_Z = unchecked((uint)(0));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = unchecked((uint)(0));
            return;
            }
            quotient = (MAKE_INT_32(r_dst) / src);
            remainder = (MAKE_INT_32(r_dst) % src);
            if ((quotient == MAKE_INT_16(unchecked((uint)(quotient))))) {
            FLAG_Z = unchecked((uint)(quotient));
            FLAG_N = NFLAG_16(unchecked((uint)(quotient)));
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = MASK_OUT_ABOVE_32((MASK_OUT_ABOVE_16(unchecked((uint)(quotient))) | unchecked((uint)((remainder << 16)))));
            return;
            }
            FLAG_V = VFLAG_SET;
            FLAG_N = NFLAG_SET;
            FLAG_C = CFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            m68ki_exception_trap(EXCEPTION_ZERO_DIVIDE);
        }
    }
    private void m68k_op_divs_16_di()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            int src = MAKE_INT_16(OPER_AY_DI_16());
            int quotient = default;
            int remainder = default;
            if ((src != 0)) {
            UseDivsCycles(unchecked((int)(r_dst)), unchecked((short)(src)));
            if (((r_dst == 2147483648u) && (src == (-1)))) {
            FLAG_Z = unchecked((uint)(0));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = unchecked((uint)(0));
            return;
            }
            quotient = (MAKE_INT_32(r_dst) / src);
            remainder = (MAKE_INT_32(r_dst) % src);
            if ((quotient == MAKE_INT_16(unchecked((uint)(quotient))))) {
            FLAG_Z = unchecked((uint)(quotient));
            FLAG_N = NFLAG_16(unchecked((uint)(quotient)));
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = MASK_OUT_ABOVE_32((MASK_OUT_ABOVE_16(unchecked((uint)(quotient))) | unchecked((uint)((remainder << 16)))));
            return;
            }
            FLAG_V = VFLAG_SET;
            FLAG_N = NFLAG_SET;
            FLAG_C = CFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            m68ki_exception_trap(EXCEPTION_ZERO_DIVIDE);
        }
    }
    private void m68k_op_divs_16_ix()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            int src = MAKE_INT_16(OPER_AY_IX_16());
            int quotient = default;
            int remainder = default;
            if ((src != 0)) {
            UseDivsCycles(unchecked((int)(r_dst)), unchecked((short)(src)));
            if (((r_dst == 2147483648u) && (src == (-1)))) {
            FLAG_Z = unchecked((uint)(0));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = unchecked((uint)(0));
            return;
            }
            quotient = (MAKE_INT_32(r_dst) / src);
            remainder = (MAKE_INT_32(r_dst) % src);
            if ((quotient == MAKE_INT_16(unchecked((uint)(quotient))))) {
            FLAG_Z = unchecked((uint)(quotient));
            FLAG_N = NFLAG_16(unchecked((uint)(quotient)));
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = MASK_OUT_ABOVE_32((MASK_OUT_ABOVE_16(unchecked((uint)(quotient))) | unchecked((uint)((remainder << 16)))));
            return;
            }
            FLAG_V = VFLAG_SET;
            FLAG_N = NFLAG_SET;
            FLAG_C = CFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            m68ki_exception_trap(EXCEPTION_ZERO_DIVIDE);
        }
    }
    private void m68k_op_divs_16_aw()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            int src = MAKE_INT_16(OPER_AW_16());
            int quotient = default;
            int remainder = default;
            if ((src != 0)) {
            UseDivsCycles(unchecked((int)(r_dst)), unchecked((short)(src)));
            if (((r_dst == 2147483648u) && (src == (-1)))) {
            FLAG_Z = unchecked((uint)(0));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = unchecked((uint)(0));
            return;
            }
            quotient = (MAKE_INT_32(r_dst) / src);
            remainder = (MAKE_INT_32(r_dst) % src);
            if ((quotient == MAKE_INT_16(unchecked((uint)(quotient))))) {
            FLAG_Z = unchecked((uint)(quotient));
            FLAG_N = NFLAG_16(unchecked((uint)(quotient)));
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = MASK_OUT_ABOVE_32((MASK_OUT_ABOVE_16(unchecked((uint)(quotient))) | unchecked((uint)((remainder << 16)))));
            return;
            }
            FLAG_V = VFLAG_SET;
            FLAG_N = NFLAG_SET;
            FLAG_C = CFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            m68ki_exception_trap(EXCEPTION_ZERO_DIVIDE);
        }
    }
    private void m68k_op_divs_16_al()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            int src = MAKE_INT_16(OPER_AL_16());
            int quotient = default;
            int remainder = default;
            if ((src != 0)) {
            UseDivsCycles(unchecked((int)(r_dst)), unchecked((short)(src)));
            if (((r_dst == 2147483648u) && (src == (-1)))) {
            FLAG_Z = unchecked((uint)(0));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = unchecked((uint)(0));
            return;
            }
            quotient = (MAKE_INT_32(r_dst) / src);
            remainder = (MAKE_INT_32(r_dst) % src);
            if ((quotient == MAKE_INT_16(unchecked((uint)(quotient))))) {
            FLAG_Z = unchecked((uint)(quotient));
            FLAG_N = NFLAG_16(unchecked((uint)(quotient)));
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = MASK_OUT_ABOVE_32((MASK_OUT_ABOVE_16(unchecked((uint)(quotient))) | unchecked((uint)((remainder << 16)))));
            return;
            }
            FLAG_V = VFLAG_SET;
            FLAG_N = NFLAG_SET;
            FLAG_C = CFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            m68ki_exception_trap(EXCEPTION_ZERO_DIVIDE);
        }
    }
    private void m68k_op_divs_16_pcdi()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            int src = MAKE_INT_16(OPER_PCDI_16());
            int quotient = default;
            int remainder = default;
            if ((src != 0)) {
            UseDivsCycles(unchecked((int)(r_dst)), unchecked((short)(src)));
            if (((r_dst == 2147483648u) && (src == (-1)))) {
            FLAG_Z = unchecked((uint)(0));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = unchecked((uint)(0));
            return;
            }
            quotient = (MAKE_INT_32(r_dst) / src);
            remainder = (MAKE_INT_32(r_dst) % src);
            if ((quotient == MAKE_INT_16(unchecked((uint)(quotient))))) {
            FLAG_Z = unchecked((uint)(quotient));
            FLAG_N = NFLAG_16(unchecked((uint)(quotient)));
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = MASK_OUT_ABOVE_32((MASK_OUT_ABOVE_16(unchecked((uint)(quotient))) | unchecked((uint)((remainder << 16)))));
            return;
            }
            FLAG_V = VFLAG_SET;
            FLAG_N = NFLAG_SET;
            FLAG_C = CFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            m68ki_exception_trap(EXCEPTION_ZERO_DIVIDE);
        }
    }
    private void m68k_op_divs_16_pcix()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            int src = MAKE_INT_16(OPER_PCIX_16());
            int quotient = default;
            int remainder = default;
            if ((src != 0)) {
            UseDivsCycles(unchecked((int)(r_dst)), unchecked((short)(src)));
            if (((r_dst == 2147483648u) && (src == (-1)))) {
            FLAG_Z = unchecked((uint)(0));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = unchecked((uint)(0));
            return;
            }
            quotient = (MAKE_INT_32(r_dst) / src);
            remainder = (MAKE_INT_32(r_dst) % src);
            if ((quotient == MAKE_INT_16(unchecked((uint)(quotient))))) {
            FLAG_Z = unchecked((uint)(quotient));
            FLAG_N = NFLAG_16(unchecked((uint)(quotient)));
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = MASK_OUT_ABOVE_32((MASK_OUT_ABOVE_16(unchecked((uint)(quotient))) | unchecked((uint)((remainder << 16)))));
            return;
            }
            FLAG_V = VFLAG_SET;
            FLAG_N = NFLAG_SET;
            FLAG_C = CFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            m68ki_exception_trap(EXCEPTION_ZERO_DIVIDE);
        }
    }
    private void m68k_op_divs_16_i()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            int src = MAKE_INT_16(OPER_I_16());
            int quotient = default;
            int remainder = default;
            if ((src != 0)) {
            UseDivsCycles(unchecked((int)(r_dst)), unchecked((short)(src)));
            if (((r_dst == 2147483648u) && (src == (-1)))) {
            FLAG_Z = unchecked((uint)(0));
            FLAG_N = NFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = unchecked((uint)(0));
            return;
            }
            quotient = (MAKE_INT_32(r_dst) / src);
            remainder = (MAKE_INT_32(r_dst) % src);
            if ((quotient == MAKE_INT_16(unchecked((uint)(quotient))))) {
            FLAG_Z = unchecked((uint)(quotient));
            FLAG_N = NFLAG_16(unchecked((uint)(quotient)));
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = MASK_OUT_ABOVE_32((MASK_OUT_ABOVE_16(unchecked((uint)(quotient))) | unchecked((uint)((remainder << 16)))));
            return;
            }
            FLAG_V = VFLAG_SET;
            FLAG_N = NFLAG_SET;
            FLAG_C = CFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            m68ki_exception_trap(EXCEPTION_ZERO_DIVIDE);
        }
    }
    private void m68k_op_divu_16_d()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = MASK_OUT_ABOVE_16(DY);
            if ((src != unchecked((uint)(0)))) {
            uint quotient = (r_dst / src);
            uint remainder = (r_dst % src);
            if ((quotient < unchecked((uint)(65536)))) {
            UseDivuCycles(r_dst, src);
            FLAG_Z = quotient;
            FLAG_N = NFLAG_16(quotient);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = MASK_OUT_ABOVE_32((MASK_OUT_ABOVE_16(quotient) | (remainder << 16)));
            return;
            }
            USE_CYCLES(unchecked((long)((MUL * 10))));
            FLAG_V = VFLAG_SET;
            FLAG_N = NFLAG_SET;
            FLAG_C = CFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            m68ki_exception_trap(EXCEPTION_ZERO_DIVIDE);
        }
    }
    private void m68k_op_divu_16_ai()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_AI_16();
            if ((src != unchecked((uint)(0)))) {
            uint quotient = (r_dst / src);
            uint remainder = (r_dst % src);
            if ((quotient < unchecked((uint)(65536)))) {
            UseDivuCycles(r_dst, src);
            FLAG_Z = quotient;
            FLAG_N = NFLAG_16(quotient);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = MASK_OUT_ABOVE_32((MASK_OUT_ABOVE_16(quotient) | (remainder << 16)));
            return;
            }
            USE_CYCLES(unchecked((long)((MUL * 10))));
            FLAG_V = VFLAG_SET;
            FLAG_N = NFLAG_SET;
            FLAG_C = CFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            m68ki_exception_trap(EXCEPTION_ZERO_DIVIDE);
        }
    }
    private void m68k_op_divu_16_pi()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_PI_16();
            if ((src != unchecked((uint)(0)))) {
            uint quotient = (r_dst / src);
            uint remainder = (r_dst % src);
            if ((quotient < unchecked((uint)(65536)))) {
            UseDivuCycles(r_dst, src);
            FLAG_Z = quotient;
            FLAG_N = NFLAG_16(quotient);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = MASK_OUT_ABOVE_32((MASK_OUT_ABOVE_16(quotient) | (remainder << 16)));
            return;
            }
            USE_CYCLES(unchecked((long)((MUL * 10))));
            FLAG_V = VFLAG_SET;
            FLAG_N = NFLAG_SET;
            FLAG_C = CFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            m68ki_exception_trap(EXCEPTION_ZERO_DIVIDE);
        }
    }
    private void m68k_op_divu_16_pd()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_PD_16();
            if ((src != unchecked((uint)(0)))) {
            uint quotient = (r_dst / src);
            uint remainder = (r_dst % src);
            if ((quotient < unchecked((uint)(65536)))) {
            UseDivuCycles(r_dst, src);
            FLAG_Z = quotient;
            FLAG_N = NFLAG_16(quotient);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = MASK_OUT_ABOVE_32((MASK_OUT_ABOVE_16(quotient) | (remainder << 16)));
            return;
            }
            USE_CYCLES(unchecked((long)((MUL * 10))));
            FLAG_V = VFLAG_SET;
            FLAG_N = NFLAG_SET;
            FLAG_C = CFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            m68ki_exception_trap(EXCEPTION_ZERO_DIVIDE);
        }
    }
    private void m68k_op_divu_16_di()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_DI_16();
            if ((src != unchecked((uint)(0)))) {
            uint quotient = (r_dst / src);
            uint remainder = (r_dst % src);
            if ((quotient < unchecked((uint)(65536)))) {
            UseDivuCycles(r_dst, src);
            FLAG_Z = quotient;
            FLAG_N = NFLAG_16(quotient);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = MASK_OUT_ABOVE_32((MASK_OUT_ABOVE_16(quotient) | (remainder << 16)));
            return;
            }
            USE_CYCLES(unchecked((long)((MUL * 10))));
            FLAG_V = VFLAG_SET;
            FLAG_N = NFLAG_SET;
            FLAG_C = CFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            m68ki_exception_trap(EXCEPTION_ZERO_DIVIDE);
        }
    }
    private void m68k_op_divu_16_ix()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_IX_16();
            if ((src != unchecked((uint)(0)))) {
            uint quotient = (r_dst / src);
            uint remainder = (r_dst % src);
            if ((quotient < unchecked((uint)(65536)))) {
            UseDivuCycles(r_dst, src);
            FLAG_Z = quotient;
            FLAG_N = NFLAG_16(quotient);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = MASK_OUT_ABOVE_32((MASK_OUT_ABOVE_16(quotient) | (remainder << 16)));
            return;
            }
            USE_CYCLES(unchecked((long)((MUL * 10))));
            FLAG_V = VFLAG_SET;
            FLAG_N = NFLAG_SET;
            FLAG_C = CFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            m68ki_exception_trap(EXCEPTION_ZERO_DIVIDE);
        }
    }
    private void m68k_op_divu_16_aw()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AW_16();
            if ((src != unchecked((uint)(0)))) {
            uint quotient = (r_dst / src);
            uint remainder = (r_dst % src);
            if ((quotient < unchecked((uint)(65536)))) {
            UseDivuCycles(r_dst, src);
            FLAG_Z = quotient;
            FLAG_N = NFLAG_16(quotient);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = MASK_OUT_ABOVE_32((MASK_OUT_ABOVE_16(quotient) | (remainder << 16)));
            return;
            }
            USE_CYCLES(unchecked((long)((MUL * 10))));
            FLAG_V = VFLAG_SET;
            FLAG_N = NFLAG_SET;
            FLAG_C = CFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            m68ki_exception_trap(EXCEPTION_ZERO_DIVIDE);
        }
    }
    private void m68k_op_divu_16_al()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AL_16();
            if ((src != unchecked((uint)(0)))) {
            uint quotient = (r_dst / src);
            uint remainder = (r_dst % src);
            if ((quotient < unchecked((uint)(65536)))) {
            UseDivuCycles(r_dst, src);
            FLAG_Z = quotient;
            FLAG_N = NFLAG_16(quotient);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = MASK_OUT_ABOVE_32((MASK_OUT_ABOVE_16(quotient) | (remainder << 16)));
            return;
            }
            USE_CYCLES(unchecked((long)((MUL * 10))));
            FLAG_V = VFLAG_SET;
            FLAG_N = NFLAG_SET;
            FLAG_C = CFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            m68ki_exception_trap(EXCEPTION_ZERO_DIVIDE);
        }
    }
    private void m68k_op_divu_16_pcdi()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_PCDI_16();
            if ((src != unchecked((uint)(0)))) {
            uint quotient = (r_dst / src);
            uint remainder = (r_dst % src);
            if ((quotient < unchecked((uint)(65536)))) {
            UseDivuCycles(r_dst, src);
            FLAG_Z = quotient;
            FLAG_N = NFLAG_16(quotient);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = MASK_OUT_ABOVE_32((MASK_OUT_ABOVE_16(quotient) | (remainder << 16)));
            return;
            }
            USE_CYCLES(unchecked((long)((MUL * 10))));
            FLAG_V = VFLAG_SET;
            FLAG_N = NFLAG_SET;
            FLAG_C = CFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            m68ki_exception_trap(EXCEPTION_ZERO_DIVIDE);
        }
    }
    private void m68k_op_divu_16_pcix()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_PCIX_16();
            if ((src != unchecked((uint)(0)))) {
            uint quotient = (r_dst / src);
            uint remainder = (r_dst % src);
            if ((quotient < unchecked((uint)(65536)))) {
            UseDivuCycles(r_dst, src);
            FLAG_Z = quotient;
            FLAG_N = NFLAG_16(quotient);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = MASK_OUT_ABOVE_32((MASK_OUT_ABOVE_16(quotient) | (remainder << 16)));
            return;
            }
            USE_CYCLES(unchecked((long)((MUL * 10))));
            FLAG_V = VFLAG_SET;
            FLAG_N = NFLAG_SET;
            FLAG_C = CFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            m68ki_exception_trap(EXCEPTION_ZERO_DIVIDE);
        }
    }
    private void m68k_op_divu_16_i()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_I_16();
            if ((src != unchecked((uint)(0)))) {
            uint quotient = (r_dst / src);
            uint remainder = (r_dst % src);
            if ((quotient < unchecked((uint)(65536)))) {
            UseDivuCycles(r_dst, src);
            FLAG_Z = quotient;
            FLAG_N = NFLAG_16(quotient);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst = MASK_OUT_ABOVE_32((MASK_OUT_ABOVE_16(quotient) | (remainder << 16)));
            return;
            }
            USE_CYCLES(unchecked((long)((MUL * 10))));
            FLAG_V = VFLAG_SET;
            FLAG_N = NFLAG_SET;
            FLAG_C = CFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            m68ki_exception_trap(EXCEPTION_ZERO_DIVIDE);
        }
    }
    private void m68k_op_eor_8_d()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_8((DY ^= MASK_OUT_ABOVE_8(DX)));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eor_8_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_8();
            uint res = MASK_OUT_ABOVE_8((DX ^ m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eor_8_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_8();
            uint res = MASK_OUT_ABOVE_8((DX ^ m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eor_8_pi7()
    {
        unchecked
        {
            uint ea = EA_A7_PI_8();
            uint res = MASK_OUT_ABOVE_8((DX ^ m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eor_8_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_8();
            uint res = MASK_OUT_ABOVE_8((DX ^ m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eor_8_pd7()
    {
        unchecked
        {
            uint ea = EA_A7_PD_8();
            uint res = MASK_OUT_ABOVE_8((DX ^ m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eor_8_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_8();
            uint res = MASK_OUT_ABOVE_8((DX ^ m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eor_8_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_8();
            uint res = MASK_OUT_ABOVE_8((DX ^ m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eor_8_aw()
    {
        unchecked
        {
            uint ea = EA_AW_8();
            uint res = MASK_OUT_ABOVE_8((DX ^ m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eor_8_al()
    {
        unchecked
        {
            uint ea = EA_AL_8();
            uint res = MASK_OUT_ABOVE_8((DX ^ m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eor_16_d()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16((DY ^= MASK_OUT_ABOVE_16(DX)));
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eor_16_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_16();
            uint res = MASK_OUT_ABOVE_16((DX ^ m68ki_read_16(ea)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eor_16_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_16();
            uint res = MASK_OUT_ABOVE_16((DX ^ m68ki_read_16(ea)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eor_16_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_16();
            uint res = MASK_OUT_ABOVE_16((DX ^ m68ki_read_16(ea)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eor_16_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_16();
            uint res = MASK_OUT_ABOVE_16((DX ^ m68ki_read_16(ea)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eor_16_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_16();
            uint res = MASK_OUT_ABOVE_16((DX ^ m68ki_read_16(ea)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eor_16_aw()
    {
        unchecked
        {
            uint ea = EA_AW_16();
            uint res = MASK_OUT_ABOVE_16((DX ^ m68ki_read_16(ea)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eor_16_al()
    {
        unchecked
        {
            uint ea = EA_AL_16();
            uint res = MASK_OUT_ABOVE_16((DX ^ m68ki_read_16(ea)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eor_32_d()
    {
        unchecked
        {
            uint res = (DY ^= DX);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eor_32_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_32();
            uint res = (DX ^ m68ki_read_32(ea));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eor_32_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_32();
            uint res = (DX ^ m68ki_read_32(ea));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eor_32_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_32();
            uint res = (DX ^ m68ki_read_32(ea));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eor_32_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_32();
            uint res = (DX ^ m68ki_read_32(ea));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eor_32_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_32();
            uint res = (DX ^ m68ki_read_32(ea));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eor_32_aw()
    {
        unchecked
        {
            uint ea = EA_AW_32();
            uint res = (DX ^ m68ki_read_32(ea));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eor_32_al()
    {
        unchecked
        {
            uint ea = EA_AL_32();
            uint res = (DX ^ m68ki_read_32(ea));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eori_8_d()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_8((DY ^= OPER_I_8()));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eori_8_ai()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AY_AI_8();
            uint res = (src ^ m68ki_read_8(ea));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eori_8_pi()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AY_PI_8();
            uint res = (src ^ m68ki_read_8(ea));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eori_8_pi7()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_A7_PI_8();
            uint res = (src ^ m68ki_read_8(ea));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eori_8_pd()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AY_PD_8();
            uint res = (src ^ m68ki_read_8(ea));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eori_8_pd7()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_A7_PD_8();
            uint res = (src ^ m68ki_read_8(ea));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eori_8_di()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AY_DI_8();
            uint res = (src ^ m68ki_read_8(ea));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eori_8_ix()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AY_IX_8();
            uint res = (src ^ m68ki_read_8(ea));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eori_8_aw()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AW_8();
            uint res = (src ^ m68ki_read_8(ea));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eori_8_al()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AL_8();
            uint res = (src ^ m68ki_read_8(ea));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eori_16_d()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16((DY ^= OPER_I_16()));
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eori_16_ai()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AY_AI_16();
            uint res = (src ^ m68ki_read_16(ea));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eori_16_pi()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AY_PI_16();
            uint res = (src ^ m68ki_read_16(ea));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eori_16_pd()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AY_PD_16();
            uint res = (src ^ m68ki_read_16(ea));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eori_16_di()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AY_DI_16();
            uint res = (src ^ m68ki_read_16(ea));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eori_16_ix()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AY_IX_16();
            uint res = (src ^ m68ki_read_16(ea));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eori_16_aw()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AW_16();
            uint res = (src ^ m68ki_read_16(ea));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eori_16_al()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AL_16();
            uint res = (src ^ m68ki_read_16(ea));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eori_32_d()
    {
        unchecked
        {
            uint res = (DY ^= OPER_I_32());
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eori_32_ai()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AY_AI_32();
            uint res = (src ^ m68ki_read_32(ea));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eori_32_pi()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AY_PI_32();
            uint res = (src ^ m68ki_read_32(ea));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eori_32_pd()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AY_PD_32();
            uint res = (src ^ m68ki_read_32(ea));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eori_32_di()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AY_DI_32();
            uint res = (src ^ m68ki_read_32(ea));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eori_32_ix()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AY_IX_32();
            uint res = (src ^ m68ki_read_32(ea));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eori_32_aw()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AW_32();
            uint res = (src ^ m68ki_read_32(ea));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eori_32_al()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AL_32();
            uint res = (src ^ m68ki_read_32(ea));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_eori_16_toc()
    {
        unchecked
        {
            m68ki_set_ccr((m68ki_get_ccr() ^ OPER_I_16()));
        }
    }
    private void m68k_op_eori_16_tos()
    {
        unchecked
        {
            if ((FLAG_S != 0)) {
            uint src = OPER_I_16();
            m68ki_set_sr((m68ki_get_sr() ^ src));
            return;
            }
            m68ki_exception_privilege_violation();
        }
    }
    private void m68k_op_exg_32_dd()
    {
        unchecked
        {
            ref uint reg_a = ref DX;
            ref uint reg_b = ref DY;
            uint tmp = reg_a;
            reg_a = reg_b;
            reg_b = tmp;
        }
    }
    private void m68k_op_exg_32_aa()
    {
        unchecked
        {
            ref uint reg_a = ref AX;
            ref uint reg_b = ref AY;
            uint tmp = reg_a;
            reg_a = reg_b;
            reg_b = tmp;
        }
    }
    private void m68k_op_exg_32_da()
    {
        unchecked
        {
            ref uint reg_a = ref DX;
            ref uint reg_b = ref AY;
            uint tmp = reg_a;
            reg_a = reg_b;
            reg_b = tmp;
        }
    }
    private void m68k_op_ext_16()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            r_dst = ((MASK_OUT_BELOW_16(r_dst) | MASK_OUT_ABOVE_8(r_dst)) | unchecked((uint)(((GET_MSB_8(r_dst) != 0) ? 65280 : 0))));
            FLAG_N = NFLAG_16(r_dst);
            FLAG_Z = MASK_OUT_ABOVE_16(r_dst);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_ext_32()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            r_dst = (MASK_OUT_ABOVE_16(r_dst) | ((GET_MSB_16(r_dst) != 0) ? 4294901760u : unchecked((uint)(0))));
            FLAG_N = NFLAG_32(r_dst);
            FLAG_Z = r_dst;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_illegal()
    {
        unchecked
        {
            m68ki_exception_illegal();
        }
    }
    private void m68k_op_jmp_32_ai()
    {
        unchecked
        {
            m68ki_jump(EA_AY_AI_32());
        }
    }
    private void m68k_op_jmp_32_di()
    {
        unchecked
        {
            m68ki_jump(EA_AY_DI_32());
        }
    }
    private void m68k_op_jmp_32_ix()
    {
        unchecked
        {
            m68ki_jump(EA_AY_IX_32());
        }
    }
    private void m68k_op_jmp_32_aw()
    {
        unchecked
        {
            m68ki_jump(EA_AW_32());
        }
    }
    private void m68k_op_jmp_32_al()
    {
        unchecked
        {
            m68ki_jump(EA_AL_32());
        }
    }
    private void m68k_op_jmp_32_pcdi()
    {
        unchecked
        {
            m68ki_jump(EA_PCDI_32());
        }
    }
    private void m68k_op_jmp_32_pcix()
    {
        unchecked
        {
            m68ki_jump(EA_PCIX_32());
        }
    }
    private void m68k_op_jsr_32_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_32();
            m68ki_push_32(REG_PC);
            m68ki_jump(ea);
        }
    }
    private void m68k_op_jsr_32_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_32();
            m68ki_push_32(REG_PC);
            m68ki_jump(ea);
        }
    }
    private void m68k_op_jsr_32_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_32();
            m68ki_push_32(REG_PC);
            m68ki_jump(ea);
        }
    }
    private void m68k_op_jsr_32_aw()
    {
        unchecked
        {
            uint ea = EA_AW_32();
            m68ki_push_32(REG_PC);
            m68ki_jump(ea);
        }
    }
    private void m68k_op_jsr_32_al()
    {
        unchecked
        {
            uint ea = EA_AL_32();
            m68ki_push_32(REG_PC);
            m68ki_jump(ea);
        }
    }
    private void m68k_op_jsr_32_pcdi()
    {
        unchecked
        {
            uint ea = EA_PCDI_32();
            m68ki_push_32(REG_PC);
            m68ki_jump(ea);
        }
    }
    private void m68k_op_jsr_32_pcix()
    {
        unchecked
        {
            uint ea = EA_PCIX_32();
            m68ki_push_32(REG_PC);
            m68ki_jump(ea);
        }
    }
    private void m68k_op_lea_32_ai()
    {
        unchecked
        {
            AX = EA_AY_AI_32();
        }
    }
    private void m68k_op_lea_32_di()
    {
        unchecked
        {
            AX = EA_AY_DI_32();
        }
    }
    private void m68k_op_lea_32_ix()
    {
        unchecked
        {
            AX = EA_AY_IX_32();
        }
    }
    private void m68k_op_lea_32_aw()
    {
        unchecked
        {
            AX = EA_AW_32();
        }
    }
    private void m68k_op_lea_32_al()
    {
        unchecked
        {
            AX = EA_AL_32();
        }
    }
    private void m68k_op_lea_32_pcdi()
    {
        unchecked
        {
            AX = EA_PCDI_32();
        }
    }
    private void m68k_op_lea_32_pcix()
    {
        unchecked
        {
            AX = EA_PCIX_32();
        }
    }
    private void m68k_op_link_16_a7()
    {
        unchecked
        {
            _registers[8 + (int)(7)] -= unchecked((uint)(4));
            m68ki_write_32(_registers[8 + (int)(7)], _registers[8 + (int)(7)]);
            _registers[8 + (int)(7)] = MASK_OUT_ABOVE_32((_registers[8 + (int)(7)] + unchecked((uint)(MAKE_INT_16(OPER_I_16())))));
        }
    }
    private void m68k_op_link_16()
    {
        unchecked
        {
            ref uint r_dst = ref AY;
            m68ki_push_32(r_dst);
            r_dst = _registers[8 + (int)(7)];
            _registers[8 + (int)(7)] = MASK_OUT_ABOVE_32((_registers[8 + (int)(7)] + unchecked((uint)(MAKE_INT_16(OPER_I_16())))));
        }
    }
    private void m68k_op_lsr_8_s()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint src = MASK_OUT_ABOVE_8(r_dst);
            uint res = (src >> unchecked((int)(shift)));
            if ((shift != unchecked((uint)(0)))) USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_N = NFLAG_CLEAR;
            FLAG_Z = res;
            FLAG_X = (FLAG_C = (src << unchecked((int)((unchecked((uint)(9)) - shift)))));
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_lsr_16_s()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint src = MASK_OUT_ABOVE_16(r_dst);
            uint res = (src >> unchecked((int)(shift)));
            if ((shift != unchecked((uint)(0)))) USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_N = NFLAG_CLEAR;
            FLAG_Z = res;
            FLAG_X = (FLAG_C = (src << unchecked((int)((unchecked((uint)(9)) - shift)))));
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_lsr_32_s()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint src = r_dst;
            uint res = (src >> unchecked((int)(shift)));
            if ((shift != unchecked((uint)(0)))) USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            r_dst = res;
            FLAG_N = NFLAG_CLEAR;
            FLAG_Z = res;
            FLAG_X = (FLAG_C = (src << unchecked((int)((unchecked((uint)(9)) - shift)))));
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_lsr_8_r()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = (DX & unchecked((uint)(63)));
            uint src = MASK_OUT_ABOVE_8(r_dst);
            uint res = (src >> unchecked((int)(shift)));
            if ((shift != unchecked((uint)(0)))) {
            USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            SKIP_BUS_REFRESH();
            if ((shift <= unchecked((uint)(8)))) {
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_X = (FLAG_C = (src << unchecked((int)((unchecked((uint)(9)) - shift)))));
            FLAG_N = NFLAG_CLEAR;
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            r_dst &= 4294967040u;
            FLAG_X = XFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_N = NFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            FLAG_N = NFLAG_8(src);
            FLAG_Z = src;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_lsr_16_r()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = (DX & unchecked((uint)(63)));
            uint src = MASK_OUT_ABOVE_16(r_dst);
            uint res = (src >> unchecked((int)(shift)));
            if ((shift != unchecked((uint)(0)))) {
            USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            SKIP_BUS_REFRESH();
            if ((shift <= unchecked((uint)(16)))) {
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_C = (FLAG_X = ((src >> unchecked((int)((shift - unchecked((uint)(1)))))) << 8));
            FLAG_N = NFLAG_CLEAR;
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            r_dst &= 4294901760u;
            FLAG_X = XFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_N = NFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            FLAG_N = NFLAG_16(src);
            FLAG_Z = src;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_lsr_32_r()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = (DX & unchecked((uint)(63)));
            uint src = r_dst;
            uint res = (src >> unchecked((int)(shift)));
            if ((shift != unchecked((uint)(0)))) {
            USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            SKIP_BUS_REFRESH();
            if ((shift < unchecked((uint)(32)))) {
            r_dst = res;
            FLAG_C = (FLAG_X = ((src >> unchecked((int)((shift - unchecked((uint)(1)))))) << 8));
            FLAG_N = NFLAG_CLEAR;
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            r_dst = unchecked((uint)(0));
            FLAG_X = (FLAG_C = ((shift == unchecked((uint)(32))) ? (GET_MSB_32(src) >> 23) : unchecked((uint)(0))));
            FLAG_N = NFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            FLAG_N = NFLAG_32(src);
            FLAG_Z = src;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_lsr_16_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_16();
            uint src = m68ki_read_16(ea);
            uint res = (src >> 1);
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_CLEAR;
            FLAG_Z = res;
            FLAG_C = (FLAG_X = (src << 8));
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_lsr_16_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_16();
            uint src = m68ki_read_16(ea);
            uint res = (src >> 1);
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_CLEAR;
            FLAG_Z = res;
            FLAG_C = (FLAG_X = (src << 8));
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_lsr_16_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_16();
            uint src = m68ki_read_16(ea);
            uint res = (src >> 1);
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_CLEAR;
            FLAG_Z = res;
            FLAG_C = (FLAG_X = (src << 8));
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_lsr_16_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_16();
            uint src = m68ki_read_16(ea);
            uint res = (src >> 1);
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_CLEAR;
            FLAG_Z = res;
            FLAG_C = (FLAG_X = (src << 8));
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_lsr_16_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_16();
            uint src = m68ki_read_16(ea);
            uint res = (src >> 1);
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_CLEAR;
            FLAG_Z = res;
            FLAG_C = (FLAG_X = (src << 8));
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_lsr_16_aw()
    {
        unchecked
        {
            uint ea = EA_AW_16();
            uint src = m68ki_read_16(ea);
            uint res = (src >> 1);
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_CLEAR;
            FLAG_Z = res;
            FLAG_C = (FLAG_X = (src << 8));
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_lsr_16_al()
    {
        unchecked
        {
            uint ea = EA_AL_16();
            uint src = m68ki_read_16(ea);
            uint res = (src >> 1);
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_CLEAR;
            FLAG_Z = res;
            FLAG_C = (FLAG_X = (src << 8));
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_lsl_8_s()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint src = MASK_OUT_ABOVE_8(r_dst);
            uint res = MASK_OUT_ABOVE_8((src << unchecked((int)(shift))));
            if ((shift != unchecked((uint)(0)))) USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_X = (FLAG_C = (src << unchecked((int)(shift))));
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_lsl_16_s()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint src = MASK_OUT_ABOVE_16(r_dst);
            uint res = MASK_OUT_ABOVE_16((src << unchecked((int)(shift))));
            if ((shift != unchecked((uint)(0)))) USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_X = (FLAG_C = (src >> unchecked((int)((unchecked((uint)(8)) - shift)))));
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_lsl_32_s()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint src = r_dst;
            uint res = MASK_OUT_ABOVE_32((src << unchecked((int)(shift))));
            if ((shift != unchecked((uint)(0)))) USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            r_dst = res;
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_X = (FLAG_C = (src >> unchecked((int)((unchecked((uint)(24)) - shift)))));
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_lsl_8_r()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = (DX & unchecked((uint)(63)));
            uint src = MASK_OUT_ABOVE_8(r_dst);
            uint res = MASK_OUT_ABOVE_8((src << unchecked((int)(shift))));
            if ((shift != unchecked((uint)(0)))) {
            USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            SKIP_BUS_REFRESH();
            if ((shift <= unchecked((uint)(8)))) {
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_X = (FLAG_C = (src << unchecked((int)(shift))));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            r_dst &= 4294967040u;
            FLAG_X = XFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_N = NFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            FLAG_N = NFLAG_8(src);
            FLAG_Z = src;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_lsl_16_r()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = (DX & unchecked((uint)(63)));
            uint src = MASK_OUT_ABOVE_16(r_dst);
            uint res = MASK_OUT_ABOVE_16((src << unchecked((int)(shift))));
            if ((shift != unchecked((uint)(0)))) {
            USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            SKIP_BUS_REFRESH();
            if ((shift <= unchecked((uint)(16)))) {
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_X = (FLAG_C = ((src << unchecked((int)(shift))) >> 8));
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            r_dst &= 4294901760u;
            FLAG_X = XFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_N = NFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            FLAG_N = NFLAG_16(src);
            FLAG_Z = src;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_lsl_32_r()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = (DX & unchecked((uint)(63)));
            uint src = r_dst;
            uint res = MASK_OUT_ABOVE_32((src << unchecked((int)(shift))));
            if ((shift != unchecked((uint)(0)))) {
            USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            SKIP_BUS_REFRESH();
            if ((shift < unchecked((uint)(32)))) {
            r_dst = res;
            FLAG_X = (FLAG_C = ((src >> unchecked((int)((unchecked((uint)(32)) - shift)))) << 8));
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            r_dst = unchecked((uint)(0));
            FLAG_X = (FLAG_C = (((shift == unchecked((uint)(32))) ? (src & unchecked((uint)(1))) : unchecked((uint)(0))) << 8));
            FLAG_N = NFLAG_CLEAR;
            FLAG_Z = ZFLAG_SET;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            FLAG_N = NFLAG_32(src);
            FLAG_Z = src;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_lsl_16_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_16();
            uint src = m68ki_read_16(ea);
            uint res = MASK_OUT_ABOVE_16((src << 1));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_X = (FLAG_C = (src >> 7));
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_lsl_16_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_16();
            uint src = m68ki_read_16(ea);
            uint res = MASK_OUT_ABOVE_16((src << 1));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_X = (FLAG_C = (src >> 7));
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_lsl_16_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_16();
            uint src = m68ki_read_16(ea);
            uint res = MASK_OUT_ABOVE_16((src << 1));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_X = (FLAG_C = (src >> 7));
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_lsl_16_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_16();
            uint src = m68ki_read_16(ea);
            uint res = MASK_OUT_ABOVE_16((src << 1));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_X = (FLAG_C = (src >> 7));
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_lsl_16_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_16();
            uint src = m68ki_read_16(ea);
            uint res = MASK_OUT_ABOVE_16((src << 1));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_X = (FLAG_C = (src >> 7));
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_lsl_16_aw()
    {
        unchecked
        {
            uint ea = EA_AW_16();
            uint src = m68ki_read_16(ea);
            uint res = MASK_OUT_ABOVE_16((src << 1));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_X = (FLAG_C = (src >> 7));
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_lsl_16_al()
    {
        unchecked
        {
            uint ea = EA_AL_16();
            uint src = m68ki_read_16(ea);
            uint res = MASK_OUT_ABOVE_16((src << 1));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_X = (FLAG_C = (src >> 7));
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_move_8_d_d()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_8(DY);
            ref uint r_dst = ref DX;
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_8_d_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_8();
            ref uint r_dst = ref DX;
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_8_d_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_8();
            ref uint r_dst = ref DX;
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_8_d_pi7()
    {
        unchecked
        {
            uint res = OPER_A7_PI_8();
            ref uint r_dst = ref DX;
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_8_d_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_8();
            ref uint r_dst = ref DX;
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_8_d_pd7()
    {
        unchecked
        {
            uint res = OPER_A7_PD_8();
            ref uint r_dst = ref DX;
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_8_d_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_8();
            ref uint r_dst = ref DX;
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_8_d_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_8();
            ref uint r_dst = ref DX;
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_8_d_aw()
    {
        unchecked
        {
            uint res = OPER_AW_8();
            ref uint r_dst = ref DX;
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_8_d_al()
    {
        unchecked
        {
            uint res = OPER_AL_8();
            ref uint r_dst = ref DX;
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_8_d_pcdi()
    {
        unchecked
        {
            uint res = OPER_PCDI_8();
            ref uint r_dst = ref DX;
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_8_d_pcix()
    {
        unchecked
        {
            uint res = OPER_PCIX_8();
            ref uint r_dst = ref DX;
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_8_d_i()
    {
        unchecked
        {
            uint res = OPER_I_8();
            ref uint r_dst = ref DX;
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_8_ai_d()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_8(DY);
            uint ea = EA_AX_AI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_ai_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_8();
            uint ea = EA_AX_AI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_ai_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_8();
            uint ea = EA_AX_AI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_ai_pi7()
    {
        unchecked
        {
            uint res = OPER_A7_PI_8();
            uint ea = EA_AX_AI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_ai_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_8();
            uint ea = EA_AX_AI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_ai_pd7()
    {
        unchecked
        {
            uint res = OPER_A7_PD_8();
            uint ea = EA_AX_AI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_ai_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_8();
            uint ea = EA_AX_AI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_ai_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_8();
            uint ea = EA_AX_AI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_ai_aw()
    {
        unchecked
        {
            uint res = OPER_AW_8();
            uint ea = EA_AX_AI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_ai_al()
    {
        unchecked
        {
            uint res = OPER_AL_8();
            uint ea = EA_AX_AI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_ai_pcdi()
    {
        unchecked
        {
            uint res = OPER_PCDI_8();
            uint ea = EA_AX_AI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_ai_pcix()
    {
        unchecked
        {
            uint res = OPER_PCIX_8();
            uint ea = EA_AX_AI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_ai_i()
    {
        unchecked
        {
            uint res = OPER_I_8();
            uint ea = EA_AX_AI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pi7_d()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_8(DY);
            uint ea = EA_A7_PI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pi_d()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_8(DY);
            uint ea = EA_AX_PI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pi7_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_8();
            uint ea = EA_A7_PI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pi7_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_8();
            uint ea = EA_A7_PI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pi7_pi7()
    {
        unchecked
        {
            uint res = OPER_A7_PI_8();
            uint ea = EA_A7_PI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pi7_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_8();
            uint ea = EA_A7_PI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pi7_pd7()
    {
        unchecked
        {
            uint res = OPER_A7_PD_8();
            uint ea = EA_A7_PI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pi7_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_8();
            uint ea = EA_A7_PI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pi7_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_8();
            uint ea = EA_A7_PI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pi7_aw()
    {
        unchecked
        {
            uint res = OPER_AW_8();
            uint ea = EA_A7_PI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pi7_al()
    {
        unchecked
        {
            uint res = OPER_AL_8();
            uint ea = EA_A7_PI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pi7_pcdi()
    {
        unchecked
        {
            uint res = OPER_PCDI_8();
            uint ea = EA_A7_PI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pi7_pcix()
    {
        unchecked
        {
            uint res = OPER_PCIX_8();
            uint ea = EA_A7_PI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pi7_i()
    {
        unchecked
        {
            uint res = OPER_I_8();
            uint ea = EA_A7_PI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pi_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_8();
            uint ea = EA_AX_PI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pi_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_8();
            uint ea = EA_AX_PI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pi_pi7()
    {
        unchecked
        {
            uint res = OPER_A7_PI_8();
            uint ea = EA_AX_PI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pi_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_8();
            uint ea = EA_AX_PI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pi_pd7()
    {
        unchecked
        {
            uint res = OPER_A7_PD_8();
            uint ea = EA_AX_PI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pi_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_8();
            uint ea = EA_AX_PI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pi_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_8();
            uint ea = EA_AX_PI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pi_aw()
    {
        unchecked
        {
            uint res = OPER_AW_8();
            uint ea = EA_AX_PI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pi_al()
    {
        unchecked
        {
            uint res = OPER_AL_8();
            uint ea = EA_AX_PI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pi_pcdi()
    {
        unchecked
        {
            uint res = OPER_PCDI_8();
            uint ea = EA_AX_PI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pi_pcix()
    {
        unchecked
        {
            uint res = OPER_PCIX_8();
            uint ea = EA_AX_PI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pi_i()
    {
        unchecked
        {
            uint res = OPER_I_8();
            uint ea = EA_AX_PI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pd7_d()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_8(DY);
            uint ea = EA_A7_PD_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pd_d()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_8(DY);
            uint ea = EA_AX_PD_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pd7_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_8();
            uint ea = EA_A7_PD_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pd7_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_8();
            uint ea = EA_A7_PD_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pd7_pi7()
    {
        unchecked
        {
            uint res = OPER_A7_PI_8();
            uint ea = EA_A7_PD_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pd7_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_8();
            uint ea = EA_A7_PD_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pd7_pd7()
    {
        unchecked
        {
            uint res = OPER_A7_PD_8();
            uint ea = EA_A7_PD_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pd7_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_8();
            uint ea = EA_A7_PD_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pd7_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_8();
            uint ea = EA_A7_PD_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pd7_aw()
    {
        unchecked
        {
            uint res = OPER_AW_8();
            uint ea = EA_A7_PD_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pd7_al()
    {
        unchecked
        {
            uint res = OPER_AL_8();
            uint ea = EA_A7_PD_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pd7_pcdi()
    {
        unchecked
        {
            uint res = OPER_PCDI_8();
            uint ea = EA_A7_PD_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pd7_pcix()
    {
        unchecked
        {
            uint res = OPER_PCIX_8();
            uint ea = EA_A7_PD_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pd7_i()
    {
        unchecked
        {
            uint res = OPER_I_8();
            uint ea = EA_A7_PD_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pd_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_8();
            uint ea = EA_AX_PD_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pd_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_8();
            uint ea = EA_AX_PD_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pd_pi7()
    {
        unchecked
        {
            uint res = OPER_A7_PI_8();
            uint ea = EA_AX_PD_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pd_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_8();
            uint ea = EA_AX_PD_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pd_pd7()
    {
        unchecked
        {
            uint res = OPER_A7_PD_8();
            uint ea = EA_AX_PD_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pd_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_8();
            uint ea = EA_AX_PD_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pd_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_8();
            uint ea = EA_AX_PD_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pd_aw()
    {
        unchecked
        {
            uint res = OPER_AW_8();
            uint ea = EA_AX_PD_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pd_al()
    {
        unchecked
        {
            uint res = OPER_AL_8();
            uint ea = EA_AX_PD_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pd_pcdi()
    {
        unchecked
        {
            uint res = OPER_PCDI_8();
            uint ea = EA_AX_PD_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pd_pcix()
    {
        unchecked
        {
            uint res = OPER_PCIX_8();
            uint ea = EA_AX_PD_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_pd_i()
    {
        unchecked
        {
            uint res = OPER_I_8();
            uint ea = EA_AX_PD_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_di_d()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_8(DY);
            uint ea = EA_AX_DI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_di_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_8();
            uint ea = EA_AX_DI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_di_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_8();
            uint ea = EA_AX_DI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_di_pi7()
    {
        unchecked
        {
            uint res = OPER_A7_PI_8();
            uint ea = EA_AX_DI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_di_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_8();
            uint ea = EA_AX_DI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_di_pd7()
    {
        unchecked
        {
            uint res = OPER_A7_PD_8();
            uint ea = EA_AX_DI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_di_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_8();
            uint ea = EA_AX_DI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_di_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_8();
            uint ea = EA_AX_DI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_di_aw()
    {
        unchecked
        {
            uint res = OPER_AW_8();
            uint ea = EA_AX_DI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_di_al()
    {
        unchecked
        {
            uint res = OPER_AL_8();
            uint ea = EA_AX_DI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_di_pcdi()
    {
        unchecked
        {
            uint res = OPER_PCDI_8();
            uint ea = EA_AX_DI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_di_pcix()
    {
        unchecked
        {
            uint res = OPER_PCIX_8();
            uint ea = EA_AX_DI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_di_i()
    {
        unchecked
        {
            uint res = OPER_I_8();
            uint ea = EA_AX_DI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_ix_d()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_8(DY);
            uint ea = EA_AX_IX_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_ix_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_8();
            uint ea = EA_AX_IX_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_ix_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_8();
            uint ea = EA_AX_IX_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_ix_pi7()
    {
        unchecked
        {
            uint res = OPER_A7_PI_8();
            uint ea = EA_AX_IX_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_ix_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_8();
            uint ea = EA_AX_IX_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_ix_pd7()
    {
        unchecked
        {
            uint res = OPER_A7_PD_8();
            uint ea = EA_AX_IX_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_ix_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_8();
            uint ea = EA_AX_IX_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_ix_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_8();
            uint ea = EA_AX_IX_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_ix_aw()
    {
        unchecked
        {
            uint res = OPER_AW_8();
            uint ea = EA_AX_IX_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_ix_al()
    {
        unchecked
        {
            uint res = OPER_AL_8();
            uint ea = EA_AX_IX_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_ix_pcdi()
    {
        unchecked
        {
            uint res = OPER_PCDI_8();
            uint ea = EA_AX_IX_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_ix_pcix()
    {
        unchecked
        {
            uint res = OPER_PCIX_8();
            uint ea = EA_AX_IX_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_ix_i()
    {
        unchecked
        {
            uint res = OPER_I_8();
            uint ea = EA_AX_IX_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_aw_d()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_8(DY);
            uint ea = EA_AW_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_aw_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_8();
            uint ea = EA_AW_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_aw_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_8();
            uint ea = EA_AW_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_aw_pi7()
    {
        unchecked
        {
            uint res = OPER_A7_PI_8();
            uint ea = EA_AW_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_aw_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_8();
            uint ea = EA_AW_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_aw_pd7()
    {
        unchecked
        {
            uint res = OPER_A7_PD_8();
            uint ea = EA_AW_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_aw_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_8();
            uint ea = EA_AW_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_aw_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_8();
            uint ea = EA_AW_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_aw_aw()
    {
        unchecked
        {
            uint res = OPER_AW_8();
            uint ea = EA_AW_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_aw_al()
    {
        unchecked
        {
            uint res = OPER_AL_8();
            uint ea = EA_AW_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_aw_pcdi()
    {
        unchecked
        {
            uint res = OPER_PCDI_8();
            uint ea = EA_AW_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_aw_pcix()
    {
        unchecked
        {
            uint res = OPER_PCIX_8();
            uint ea = EA_AW_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_aw_i()
    {
        unchecked
        {
            uint res = OPER_I_8();
            uint ea = EA_AW_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_al_d()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_8(DY);
            uint ea = EA_AL_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_al_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_8();
            uint ea = EA_AL_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_al_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_8();
            uint ea = EA_AL_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_al_pi7()
    {
        unchecked
        {
            uint res = OPER_A7_PI_8();
            uint ea = EA_AL_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_al_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_8();
            uint ea = EA_AL_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_al_pd7()
    {
        unchecked
        {
            uint res = OPER_A7_PD_8();
            uint ea = EA_AL_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_al_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_8();
            uint ea = EA_AL_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_al_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_8();
            uint ea = EA_AL_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_al_aw()
    {
        unchecked
        {
            uint res = OPER_AW_8();
            uint ea = EA_AL_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_al_al()
    {
        unchecked
        {
            uint res = OPER_AL_8();
            uint ea = EA_AL_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_al_pcdi()
    {
        unchecked
        {
            uint res = OPER_PCDI_8();
            uint ea = EA_AL_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_al_pcix()
    {
        unchecked
        {
            uint res = OPER_PCIX_8();
            uint ea = EA_AL_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_8_al_i()
    {
        unchecked
        {
            uint res = OPER_I_8();
            uint ea = EA_AL_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_move_16_d_d()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16(DY);
            ref uint r_dst = ref DX;
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_16_d_a()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16(AY);
            ref uint r_dst = ref DX;
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_16_d_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_16();
            ref uint r_dst = ref DX;
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_16_d_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_16();
            ref uint r_dst = ref DX;
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_16_d_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_16();
            ref uint r_dst = ref DX;
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_16_d_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_16();
            ref uint r_dst = ref DX;
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_16_d_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_16();
            ref uint r_dst = ref DX;
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_16_d_aw()
    {
        unchecked
        {
            uint res = OPER_AW_16();
            ref uint r_dst = ref DX;
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_16_d_al()
    {
        unchecked
        {
            uint res = OPER_AL_16();
            ref uint r_dst = ref DX;
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_16_d_pcdi()
    {
        unchecked
        {
            uint res = OPER_PCDI_16();
            ref uint r_dst = ref DX;
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_16_d_pcix()
    {
        unchecked
        {
            uint res = OPER_PCIX_16();
            ref uint r_dst = ref DX;
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_16_d_i()
    {
        unchecked
        {
            uint res = OPER_I_16();
            ref uint r_dst = ref DX;
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_16_ai_d()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16(DY);
            uint ea = EA_AX_AI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_ai_a()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16(AY);
            uint ea = EA_AX_AI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_ai_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_16();
            uint ea = EA_AX_AI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_ai_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_16();
            uint ea = EA_AX_AI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_ai_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_16();
            uint ea = EA_AX_AI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_ai_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_16();
            uint ea = EA_AX_AI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_ai_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_16();
            uint ea = EA_AX_AI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_ai_aw()
    {
        unchecked
        {
            uint res = OPER_AW_16();
            uint ea = EA_AX_AI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_ai_al()
    {
        unchecked
        {
            uint res = OPER_AL_16();
            uint ea = EA_AX_AI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_ai_pcdi()
    {
        unchecked
        {
            uint res = OPER_PCDI_16();
            uint ea = EA_AX_AI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_ai_pcix()
    {
        unchecked
        {
            uint res = OPER_PCIX_16();
            uint ea = EA_AX_AI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_ai_i()
    {
        unchecked
        {
            uint res = OPER_I_16();
            uint ea = EA_AX_AI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_pi_d()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16(DY);
            uint ea = EA_AX_PI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_pi_a()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16(AY);
            uint ea = EA_AX_PI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_pi_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_16();
            uint ea = EA_AX_PI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_pi_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_16();
            uint ea = EA_AX_PI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_pi_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_16();
            uint ea = EA_AX_PI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_pi_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_16();
            uint ea = EA_AX_PI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_pi_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_16();
            uint ea = EA_AX_PI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_pi_aw()
    {
        unchecked
        {
            uint res = OPER_AW_16();
            uint ea = EA_AX_PI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_pi_al()
    {
        unchecked
        {
            uint res = OPER_AL_16();
            uint ea = EA_AX_PI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_pi_pcdi()
    {
        unchecked
        {
            uint res = OPER_PCDI_16();
            uint ea = EA_AX_PI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_pi_pcix()
    {
        unchecked
        {
            uint res = OPER_PCIX_16();
            uint ea = EA_AX_PI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_pi_i()
    {
        unchecked
        {
            uint res = OPER_I_16();
            uint ea = EA_AX_PI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_pd_d()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16(DY);
            uint ea = EA_AX_PD_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_pd_a()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16(AY);
            uint ea = EA_AX_PD_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_pd_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_16();
            uint ea = EA_AX_PD_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_pd_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_16();
            uint ea = EA_AX_PD_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_pd_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_16();
            uint ea = EA_AX_PD_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_pd_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_16();
            uint ea = EA_AX_PD_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_pd_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_16();
            uint ea = EA_AX_PD_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_pd_aw()
    {
        unchecked
        {
            uint res = OPER_AW_16();
            uint ea = EA_AX_PD_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_pd_al()
    {
        unchecked
        {
            uint res = OPER_AL_16();
            uint ea = EA_AX_PD_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_pd_pcdi()
    {
        unchecked
        {
            uint res = OPER_PCDI_16();
            uint ea = EA_AX_PD_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_pd_pcix()
    {
        unchecked
        {
            uint res = OPER_PCIX_16();
            uint ea = EA_AX_PD_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_pd_i()
    {
        unchecked
        {
            uint res = OPER_I_16();
            uint ea = EA_AX_PD_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_di_d()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16(DY);
            uint ea = EA_AX_DI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_di_a()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16(AY);
            uint ea = EA_AX_DI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_di_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_16();
            uint ea = EA_AX_DI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_di_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_16();
            uint ea = EA_AX_DI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_di_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_16();
            uint ea = EA_AX_DI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_di_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_16();
            uint ea = EA_AX_DI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_di_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_16();
            uint ea = EA_AX_DI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_di_aw()
    {
        unchecked
        {
            uint res = OPER_AW_16();
            uint ea = EA_AX_DI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_di_al()
    {
        unchecked
        {
            uint res = OPER_AL_16();
            uint ea = EA_AX_DI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_di_pcdi()
    {
        unchecked
        {
            uint res = OPER_PCDI_16();
            uint ea = EA_AX_DI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_di_pcix()
    {
        unchecked
        {
            uint res = OPER_PCIX_16();
            uint ea = EA_AX_DI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_di_i()
    {
        unchecked
        {
            uint res = OPER_I_16();
            uint ea = EA_AX_DI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_ix_d()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16(DY);
            uint ea = EA_AX_IX_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_ix_a()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16(AY);
            uint ea = EA_AX_IX_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_ix_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_16();
            uint ea = EA_AX_IX_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_ix_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_16();
            uint ea = EA_AX_IX_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_ix_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_16();
            uint ea = EA_AX_IX_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_ix_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_16();
            uint ea = EA_AX_IX_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_ix_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_16();
            uint ea = EA_AX_IX_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_ix_aw()
    {
        unchecked
        {
            uint res = OPER_AW_16();
            uint ea = EA_AX_IX_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_ix_al()
    {
        unchecked
        {
            uint res = OPER_AL_16();
            uint ea = EA_AX_IX_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_ix_pcdi()
    {
        unchecked
        {
            uint res = OPER_PCDI_16();
            uint ea = EA_AX_IX_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_ix_pcix()
    {
        unchecked
        {
            uint res = OPER_PCIX_16();
            uint ea = EA_AX_IX_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_ix_i()
    {
        unchecked
        {
            uint res = OPER_I_16();
            uint ea = EA_AX_IX_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_aw_d()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16(DY);
            uint ea = EA_AW_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_aw_a()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16(AY);
            uint ea = EA_AW_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_aw_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_16();
            uint ea = EA_AW_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_aw_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_16();
            uint ea = EA_AW_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_aw_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_16();
            uint ea = EA_AW_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_aw_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_16();
            uint ea = EA_AW_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_aw_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_16();
            uint ea = EA_AW_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_aw_aw()
    {
        unchecked
        {
            uint res = OPER_AW_16();
            uint ea = EA_AW_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_aw_al()
    {
        unchecked
        {
            uint res = OPER_AL_16();
            uint ea = EA_AW_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_aw_pcdi()
    {
        unchecked
        {
            uint res = OPER_PCDI_16();
            uint ea = EA_AW_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_aw_pcix()
    {
        unchecked
        {
            uint res = OPER_PCIX_16();
            uint ea = EA_AW_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_aw_i()
    {
        unchecked
        {
            uint res = OPER_I_16();
            uint ea = EA_AW_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_al_d()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16(DY);
            uint ea = EA_AL_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_al_a()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16(AY);
            uint ea = EA_AL_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_al_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_16();
            uint ea = EA_AL_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_al_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_16();
            uint ea = EA_AL_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_al_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_16();
            uint ea = EA_AL_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_al_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_16();
            uint ea = EA_AL_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_al_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_16();
            uint ea = EA_AL_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_al_aw()
    {
        unchecked
        {
            uint res = OPER_AW_16();
            uint ea = EA_AL_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_al_al()
    {
        unchecked
        {
            uint res = OPER_AL_16();
            uint ea = EA_AL_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_al_pcdi()
    {
        unchecked
        {
            uint res = OPER_PCDI_16();
            uint ea = EA_AL_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_al_pcix()
    {
        unchecked
        {
            uint res = OPER_PCIX_16();
            uint ea = EA_AL_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_16_al_i()
    {
        unchecked
        {
            uint res = OPER_I_16();
            uint ea = EA_AL_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_move_32_d_d()
    {
        unchecked
        {
            uint res = DY;
            ref uint r_dst = ref DX;
            r_dst = res;
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_32_d_a()
    {
        unchecked
        {
            uint res = AY;
            ref uint r_dst = ref DX;
            r_dst = res;
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_32_d_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_32();
            ref uint r_dst = ref DX;
            r_dst = res;
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_32_d_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_32();
            ref uint r_dst = ref DX;
            r_dst = res;
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_32_d_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_32();
            ref uint r_dst = ref DX;
            r_dst = res;
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_32_d_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_32();
            ref uint r_dst = ref DX;
            r_dst = res;
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_32_d_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_32();
            ref uint r_dst = ref DX;
            r_dst = res;
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_32_d_aw()
    {
        unchecked
        {
            uint res = OPER_AW_32();
            ref uint r_dst = ref DX;
            r_dst = res;
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_32_d_al()
    {
        unchecked
        {
            uint res = OPER_AL_32();
            ref uint r_dst = ref DX;
            r_dst = res;
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_32_d_pcdi()
    {
        unchecked
        {
            uint res = OPER_PCDI_32();
            ref uint r_dst = ref DX;
            r_dst = res;
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_32_d_pcix()
    {
        unchecked
        {
            uint res = OPER_PCIX_32();
            ref uint r_dst = ref DX;
            r_dst = res;
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_32_d_i()
    {
        unchecked
        {
            uint res = OPER_I_32();
            ref uint r_dst = ref DX;
            r_dst = res;
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_move_32_ai_d()
    {
        unchecked
        {
            uint res = DY;
            uint ea = EA_AX_AI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_ai_a()
    {
        unchecked
        {
            uint res = AY;
            uint ea = EA_AX_AI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_ai_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_32();
            uint ea = EA_AX_AI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_ai_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_32();
            uint ea = EA_AX_AI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_ai_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_32();
            uint ea = EA_AX_AI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_ai_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_32();
            uint ea = EA_AX_AI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_ai_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_32();
            uint ea = EA_AX_AI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_ai_aw()
    {
        unchecked
        {
            uint res = OPER_AW_32();
            uint ea = EA_AX_AI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_ai_al()
    {
        unchecked
        {
            uint res = OPER_AL_32();
            uint ea = EA_AX_AI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_ai_pcdi()
    {
        unchecked
        {
            uint res = OPER_PCDI_32();
            uint ea = EA_AX_AI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_ai_pcix()
    {
        unchecked
        {
            uint res = OPER_PCIX_32();
            uint ea = EA_AX_AI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_ai_i()
    {
        unchecked
        {
            uint res = OPER_I_32();
            uint ea = EA_AX_AI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_pi_d()
    {
        unchecked
        {
            uint res = DY;
            uint ea = EA_AX_PI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_pi_a()
    {
        unchecked
        {
            uint res = AY;
            uint ea = EA_AX_PI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_pi_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_32();
            uint ea = EA_AX_PI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_pi_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_32();
            uint ea = EA_AX_PI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_pi_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_32();
            uint ea = EA_AX_PI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_pi_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_32();
            uint ea = EA_AX_PI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_pi_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_32();
            uint ea = EA_AX_PI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_pi_aw()
    {
        unchecked
        {
            uint res = OPER_AW_32();
            uint ea = EA_AX_PI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_pi_al()
    {
        unchecked
        {
            uint res = OPER_AL_32();
            uint ea = EA_AX_PI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_pi_pcdi()
    {
        unchecked
        {
            uint res = OPER_PCDI_32();
            uint ea = EA_AX_PI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_pi_pcix()
    {
        unchecked
        {
            uint res = OPER_PCIX_32();
            uint ea = EA_AX_PI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_pi_i()
    {
        unchecked
        {
            uint res = OPER_I_32();
            uint ea = EA_AX_PI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_pd_d()
    {
        unchecked
        {
            uint res = DY;
            uint ea = EA_AX_PD_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16((ea + unchecked((uint)(2))), (res & unchecked((uint)(65535))));
            m68ki_write_16(ea, ((res >> 16) & unchecked((uint)(65535))));
        }
    }
    private void m68k_op_move_32_pd_a()
    {
        unchecked
        {
            uint res = AY;
            uint ea = EA_AX_PD_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16((ea + unchecked((uint)(2))), (res & unchecked((uint)(65535))));
            m68ki_write_16(ea, ((res >> 16) & unchecked((uint)(65535))));
        }
    }
    private void m68k_op_move_32_pd_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_32();
            uint ea = EA_AX_PD_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16((ea + unchecked((uint)(2))), (res & unchecked((uint)(65535))));
            m68ki_write_16(ea, ((res >> 16) & unchecked((uint)(65535))));
        }
    }
    private void m68k_op_move_32_pd_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_32();
            uint ea = EA_AX_PD_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16((ea + unchecked((uint)(2))), (res & unchecked((uint)(65535))));
            m68ki_write_16(ea, ((res >> 16) & unchecked((uint)(65535))));
        }
    }
    private void m68k_op_move_32_pd_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_32();
            uint ea = EA_AX_PD_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16((ea + unchecked((uint)(2))), (res & unchecked((uint)(65535))));
            m68ki_write_16(ea, ((res >> 16) & unchecked((uint)(65535))));
        }
    }
    private void m68k_op_move_32_pd_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_32();
            uint ea = EA_AX_PD_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16((ea + unchecked((uint)(2))), (res & unchecked((uint)(65535))));
            m68ki_write_16(ea, ((res >> 16) & unchecked((uint)(65535))));
        }
    }
    private void m68k_op_move_32_pd_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_32();
            uint ea = EA_AX_PD_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16((ea + unchecked((uint)(2))), (res & unchecked((uint)(65535))));
            m68ki_write_16(ea, ((res >> 16) & unchecked((uint)(65535))));
        }
    }
    private void m68k_op_move_32_pd_aw()
    {
        unchecked
        {
            uint res = OPER_AW_32();
            uint ea = EA_AX_PD_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16((ea + unchecked((uint)(2))), (res & unchecked((uint)(65535))));
            m68ki_write_16(ea, ((res >> 16) & unchecked((uint)(65535))));
        }
    }
    private void m68k_op_move_32_pd_al()
    {
        unchecked
        {
            uint res = OPER_AL_32();
            uint ea = EA_AX_PD_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16((ea + unchecked((uint)(2))), (res & unchecked((uint)(65535))));
            m68ki_write_16(ea, ((res >> 16) & unchecked((uint)(65535))));
        }
    }
    private void m68k_op_move_32_pd_pcdi()
    {
        unchecked
        {
            uint res = OPER_PCDI_32();
            uint ea = EA_AX_PD_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16((ea + unchecked((uint)(2))), (res & unchecked((uint)(65535))));
            m68ki_write_16(ea, ((res >> 16) & unchecked((uint)(65535))));
        }
    }
    private void m68k_op_move_32_pd_pcix()
    {
        unchecked
        {
            uint res = OPER_PCIX_32();
            uint ea = EA_AX_PD_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16((ea + unchecked((uint)(2))), (res & unchecked((uint)(65535))));
            m68ki_write_16(ea, ((res >> 16) & unchecked((uint)(65535))));
        }
    }
    private void m68k_op_move_32_pd_i()
    {
        unchecked
        {
            uint res = OPER_I_32();
            uint ea = EA_AX_PD_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_16((ea + unchecked((uint)(2))), (res & unchecked((uint)(65535))));
            m68ki_write_16(ea, ((res >> 16) & unchecked((uint)(65535))));
        }
    }
    private void m68k_op_move_32_di_d()
    {
        unchecked
        {
            uint res = DY;
            uint ea = EA_AX_DI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_di_a()
    {
        unchecked
        {
            uint res = AY;
            uint ea = EA_AX_DI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_di_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_32();
            uint ea = EA_AX_DI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_di_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_32();
            uint ea = EA_AX_DI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_di_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_32();
            uint ea = EA_AX_DI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_di_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_32();
            uint ea = EA_AX_DI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_di_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_32();
            uint ea = EA_AX_DI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_di_aw()
    {
        unchecked
        {
            uint res = OPER_AW_32();
            uint ea = EA_AX_DI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_di_al()
    {
        unchecked
        {
            uint res = OPER_AL_32();
            uint ea = EA_AX_DI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_di_pcdi()
    {
        unchecked
        {
            uint res = OPER_PCDI_32();
            uint ea = EA_AX_DI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_di_pcix()
    {
        unchecked
        {
            uint res = OPER_PCIX_32();
            uint ea = EA_AX_DI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_di_i()
    {
        unchecked
        {
            uint res = OPER_I_32();
            uint ea = EA_AX_DI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_ix_d()
    {
        unchecked
        {
            uint res = DY;
            uint ea = EA_AX_IX_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_ix_a()
    {
        unchecked
        {
            uint res = AY;
            uint ea = EA_AX_IX_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_ix_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_32();
            uint ea = EA_AX_IX_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_ix_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_32();
            uint ea = EA_AX_IX_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_ix_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_32();
            uint ea = EA_AX_IX_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_ix_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_32();
            uint ea = EA_AX_IX_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_ix_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_32();
            uint ea = EA_AX_IX_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_ix_aw()
    {
        unchecked
        {
            uint res = OPER_AW_32();
            uint ea = EA_AX_IX_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_ix_al()
    {
        unchecked
        {
            uint res = OPER_AL_32();
            uint ea = EA_AX_IX_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_ix_pcdi()
    {
        unchecked
        {
            uint res = OPER_PCDI_32();
            uint ea = EA_AX_IX_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_ix_pcix()
    {
        unchecked
        {
            uint res = OPER_PCIX_32();
            uint ea = EA_AX_IX_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_ix_i()
    {
        unchecked
        {
            uint res = OPER_I_32();
            uint ea = EA_AX_IX_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_aw_d()
    {
        unchecked
        {
            uint res = DY;
            uint ea = EA_AW_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_aw_a()
    {
        unchecked
        {
            uint res = AY;
            uint ea = EA_AW_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_aw_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_32();
            uint ea = EA_AW_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_aw_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_32();
            uint ea = EA_AW_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_aw_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_32();
            uint ea = EA_AW_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_aw_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_32();
            uint ea = EA_AW_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_aw_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_32();
            uint ea = EA_AW_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_aw_aw()
    {
        unchecked
        {
            uint res = OPER_AW_32();
            uint ea = EA_AW_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_aw_al()
    {
        unchecked
        {
            uint res = OPER_AL_32();
            uint ea = EA_AW_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_aw_pcdi()
    {
        unchecked
        {
            uint res = OPER_PCDI_32();
            uint ea = EA_AW_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_aw_pcix()
    {
        unchecked
        {
            uint res = OPER_PCIX_32();
            uint ea = EA_AW_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_aw_i()
    {
        unchecked
        {
            uint res = OPER_I_32();
            uint ea = EA_AW_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_al_d()
    {
        unchecked
        {
            uint res = DY;
            uint ea = EA_AL_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_al_a()
    {
        unchecked
        {
            uint res = AY;
            uint ea = EA_AL_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_al_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_32();
            uint ea = EA_AL_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_al_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_32();
            uint ea = EA_AL_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_al_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_32();
            uint ea = EA_AL_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_al_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_32();
            uint ea = EA_AL_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_al_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_32();
            uint ea = EA_AL_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_al_aw()
    {
        unchecked
        {
            uint res = OPER_AW_32();
            uint ea = EA_AL_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_al_al()
    {
        unchecked
        {
            uint res = OPER_AL_32();
            uint ea = EA_AL_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_al_pcdi()
    {
        unchecked
        {
            uint res = OPER_PCDI_32();
            uint ea = EA_AL_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_al_pcix()
    {
        unchecked
        {
            uint res = OPER_PCIX_32();
            uint ea = EA_AL_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_move_32_al_i()
    {
        unchecked
        {
            uint res = OPER_I_32();
            uint ea = EA_AL_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_movea_16_d()
    {
        unchecked
        {
            AX = unchecked((uint)(MAKE_INT_16(DY)));
        }
    }
    private void m68k_op_movea_16_a()
    {
        unchecked
        {
            AX = unchecked((uint)(MAKE_INT_16(AY)));
        }
    }
    private void m68k_op_movea_16_ai()
    {
        unchecked
        {
            AX = unchecked((uint)(MAKE_INT_16(OPER_AY_AI_16())));
        }
    }
    private void m68k_op_movea_16_pi()
    {
        unchecked
        {
            AX = unchecked((uint)(MAKE_INT_16(OPER_AY_PI_16())));
        }
    }
    private void m68k_op_movea_16_pd()
    {
        unchecked
        {
            AX = unchecked((uint)(MAKE_INT_16(OPER_AY_PD_16())));
        }
    }
    private void m68k_op_movea_16_di()
    {
        unchecked
        {
            AX = unchecked((uint)(MAKE_INT_16(OPER_AY_DI_16())));
        }
    }
    private void m68k_op_movea_16_ix()
    {
        unchecked
        {
            AX = unchecked((uint)(MAKE_INT_16(OPER_AY_IX_16())));
        }
    }
    private void m68k_op_movea_16_aw()
    {
        unchecked
        {
            AX = unchecked((uint)(MAKE_INT_16(OPER_AW_16())));
        }
    }
    private void m68k_op_movea_16_al()
    {
        unchecked
        {
            AX = unchecked((uint)(MAKE_INT_16(OPER_AL_16())));
        }
    }
    private void m68k_op_movea_16_pcdi()
    {
        unchecked
        {
            AX = unchecked((uint)(MAKE_INT_16(OPER_PCDI_16())));
        }
    }
    private void m68k_op_movea_16_pcix()
    {
        unchecked
        {
            AX = unchecked((uint)(MAKE_INT_16(OPER_PCIX_16())));
        }
    }
    private void m68k_op_movea_16_i()
    {
        unchecked
        {
            AX = unchecked((uint)(MAKE_INT_16(OPER_I_16())));
        }
    }
    private void m68k_op_movea_32_d()
    {
        unchecked
        {
            AX = DY;
        }
    }
    private void m68k_op_movea_32_a()
    {
        unchecked
        {
            AX = AY;
        }
    }
    private void m68k_op_movea_32_ai()
    {
        unchecked
        {
            AX = OPER_AY_AI_32();
        }
    }
    private void m68k_op_movea_32_pi()
    {
        unchecked
        {
            AX = OPER_AY_PI_32();
        }
    }
    private void m68k_op_movea_32_pd()
    {
        unchecked
        {
            AX = OPER_AY_PD_32();
        }
    }
    private void m68k_op_movea_32_di()
    {
        unchecked
        {
            AX = OPER_AY_DI_32();
        }
    }
    private void m68k_op_movea_32_ix()
    {
        unchecked
        {
            AX = OPER_AY_IX_32();
        }
    }
    private void m68k_op_movea_32_aw()
    {
        unchecked
        {
            AX = OPER_AW_32();
        }
    }
    private void m68k_op_movea_32_al()
    {
        unchecked
        {
            AX = OPER_AL_32();
        }
    }
    private void m68k_op_movea_32_pcdi()
    {
        unchecked
        {
            AX = OPER_PCDI_32();
        }
    }
    private void m68k_op_movea_32_pcix()
    {
        unchecked
        {
            AX = OPER_PCIX_32();
        }
    }
    private void m68k_op_movea_32_i()
    {
        unchecked
        {
            AX = OPER_I_32();
        }
    }
    private void m68k_op_move_16_toc_d()
    {
        unchecked
        {
            m68ki_set_ccr(DY);
        }
    }
    private void m68k_op_move_16_toc_ai()
    {
        unchecked
        {
            m68ki_set_ccr(OPER_AY_AI_16());
        }
    }
    private void m68k_op_move_16_toc_pi()
    {
        unchecked
        {
            m68ki_set_ccr(OPER_AY_PI_16());
        }
    }
    private void m68k_op_move_16_toc_pd()
    {
        unchecked
        {
            m68ki_set_ccr(OPER_AY_PD_16());
        }
    }
    private void m68k_op_move_16_toc_di()
    {
        unchecked
        {
            m68ki_set_ccr(OPER_AY_DI_16());
        }
    }
    private void m68k_op_move_16_toc_ix()
    {
        unchecked
        {
            m68ki_set_ccr(OPER_AY_IX_16());
        }
    }
    private void m68k_op_move_16_toc_aw()
    {
        unchecked
        {
            m68ki_set_ccr(OPER_AW_16());
        }
    }
    private void m68k_op_move_16_toc_al()
    {
        unchecked
        {
            m68ki_set_ccr(OPER_AL_16());
        }
    }
    private void m68k_op_move_16_toc_pcdi()
    {
        unchecked
        {
            m68ki_set_ccr(OPER_PCDI_16());
        }
    }
    private void m68k_op_move_16_toc_pcix()
    {
        unchecked
        {
            m68ki_set_ccr(OPER_PCIX_16());
        }
    }
    private void m68k_op_move_16_toc_i()
    {
        unchecked
        {
            m68ki_set_ccr(OPER_I_16());
        }
    }
    private void m68k_op_move_16_frs_d()
    {
        unchecked
        {
            DY = (MASK_OUT_BELOW_16(DY) | m68ki_get_sr());
        }
    }
    private void m68k_op_move_16_frs_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_16();
            m68ki_write_16(ea, m68ki_get_sr());
        }
    }
    private void m68k_op_move_16_frs_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_16();
            m68ki_write_16(ea, m68ki_get_sr());
        }
    }
    private void m68k_op_move_16_frs_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_16();
            m68ki_write_16(ea, m68ki_get_sr());
        }
    }
    private void m68k_op_move_16_frs_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_16();
            m68ki_write_16(ea, m68ki_get_sr());
        }
    }
    private void m68k_op_move_16_frs_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_16();
            m68ki_write_16(ea, m68ki_get_sr());
        }
    }
    private void m68k_op_move_16_frs_aw()
    {
        unchecked
        {
            uint ea = EA_AW_16();
            m68ki_write_16(ea, m68ki_get_sr());
        }
    }
    private void m68k_op_move_16_frs_al()
    {
        unchecked
        {
            uint ea = EA_AL_16();
            m68ki_write_16(ea, m68ki_get_sr());
        }
    }
    private void m68k_op_move_16_tos_d()
    {
        unchecked
        {
            if ((FLAG_S != 0)) {
            m68ki_set_sr(DY);
            return;
            }
            m68ki_exception_privilege_violation();
        }
    }
    private void m68k_op_move_16_tos_ai()
    {
        unchecked
        {
            if ((FLAG_S != 0)) {
            uint new_sr = OPER_AY_AI_16();
            m68ki_set_sr(new_sr);
            return;
            }
            m68ki_exception_privilege_violation();
        }
    }
    private void m68k_op_move_16_tos_pi()
    {
        unchecked
        {
            if ((FLAG_S != 0)) {
            uint new_sr = OPER_AY_PI_16();
            m68ki_set_sr(new_sr);
            return;
            }
            m68ki_exception_privilege_violation();
        }
    }
    private void m68k_op_move_16_tos_pd()
    {
        unchecked
        {
            if ((FLAG_S != 0)) {
            uint new_sr = OPER_AY_PD_16();
            m68ki_set_sr(new_sr);
            return;
            }
            m68ki_exception_privilege_violation();
        }
    }
    private void m68k_op_move_16_tos_di()
    {
        unchecked
        {
            if ((FLAG_S != 0)) {
            uint new_sr = OPER_AY_DI_16();
            m68ki_set_sr(new_sr);
            return;
            }
            m68ki_exception_privilege_violation();
        }
    }
    private void m68k_op_move_16_tos_ix()
    {
        unchecked
        {
            if ((FLAG_S != 0)) {
            uint new_sr = OPER_AY_IX_16();
            m68ki_set_sr(new_sr);
            return;
            }
            m68ki_exception_privilege_violation();
        }
    }
    private void m68k_op_move_16_tos_aw()
    {
        unchecked
        {
            if ((FLAG_S != 0)) {
            uint new_sr = OPER_AW_16();
            m68ki_set_sr(new_sr);
            return;
            }
            m68ki_exception_privilege_violation();
        }
    }
    private void m68k_op_move_16_tos_al()
    {
        unchecked
        {
            if ((FLAG_S != 0)) {
            uint new_sr = OPER_AL_16();
            m68ki_set_sr(new_sr);
            return;
            }
            m68ki_exception_privilege_violation();
        }
    }
    private void m68k_op_move_16_tos_pcdi()
    {
        unchecked
        {
            if ((FLAG_S != 0)) {
            uint new_sr = OPER_PCDI_16();
            m68ki_set_sr(new_sr);
            return;
            }
            m68ki_exception_privilege_violation();
        }
    }
    private void m68k_op_move_16_tos_pcix()
    {
        unchecked
        {
            if ((FLAG_S != 0)) {
            uint new_sr = OPER_PCIX_16();
            m68ki_set_sr(new_sr);
            return;
            }
            m68ki_exception_privilege_violation();
        }
    }
    private void m68k_op_move_16_tos_i()
    {
        unchecked
        {
            if ((FLAG_S != 0)) {
            uint new_sr = OPER_I_16();
            m68ki_set_sr(new_sr);
            return;
            }
            m68ki_exception_privilege_violation();
        }
    }
    private void m68k_op_move_32_fru()
    {
        unchecked
        {
            if ((FLAG_S != 0)) {
            AY = REG_USP;
            return;
            }
            m68ki_exception_privilege_violation();
        }
    }
    private void m68k_op_move_32_tou()
    {
        unchecked
        {
            if ((FLAG_S != 0)) {
            REG_USP = AY;
            return;
            }
            m68ki_exception_privilege_violation();
        }
    }
    private void m68k_op_movem_16_re_pd()
    {
        unchecked
        {
            uint i = unchecked((uint)(0));
            uint register_list = OPER_I_16();
            uint ea = AY;
            uint count = unchecked((uint)(0));
            for (; (i < unchecked((uint)(16))); i++) if (((register_list & unchecked((uint)((1 << unchecked((int)(i)))))) != 0)) {
            ea -= unchecked((uint)(2));
            m68ki_write_16(ea, MASK_OUT_ABOVE_16(_registers[(int)((unchecked((uint)(15)) - i))]));
            count++;
            }
            AY = ea;
            USE_CYCLES(unchecked((long)((count * unchecked((uint)(CYC_MOVEM_W))))));
        }
    }
    private void m68k_op_movem_16_re_ai()
    {
        unchecked
        {
            uint i = unchecked((uint)(0));
            uint register_list = OPER_I_16();
            uint ea = EA_AY_AI_16();
            uint count = unchecked((uint)(0));
            for (; (i < unchecked((uint)(16))); i++) if (((register_list & unchecked((uint)((1 << unchecked((int)(i)))))) != 0)) {
            m68ki_write_16(ea, MASK_OUT_ABOVE_16(_registers[(int)(i)]));
            ea += unchecked((uint)(2));
            count++;
            }
            USE_CYCLES(unchecked((long)((count * unchecked((uint)(CYC_MOVEM_W))))));
        }
    }
    private void m68k_op_movem_16_re_di()
    {
        unchecked
        {
            uint i = unchecked((uint)(0));
            uint register_list = OPER_I_16();
            uint ea = EA_AY_DI_16();
            uint count = unchecked((uint)(0));
            for (; (i < unchecked((uint)(16))); i++) if (((register_list & unchecked((uint)((1 << unchecked((int)(i)))))) != 0)) {
            m68ki_write_16(ea, MASK_OUT_ABOVE_16(_registers[(int)(i)]));
            ea += unchecked((uint)(2));
            count++;
            }
            USE_CYCLES(unchecked((long)((count * unchecked((uint)(CYC_MOVEM_W))))));
        }
    }
    private void m68k_op_movem_16_re_ix()
    {
        unchecked
        {
            uint i = unchecked((uint)(0));
            uint register_list = OPER_I_16();
            uint ea = EA_AY_IX_16();
            uint count = unchecked((uint)(0));
            for (; (i < unchecked((uint)(16))); i++) if (((register_list & unchecked((uint)((1 << unchecked((int)(i)))))) != 0)) {
            m68ki_write_16(ea, MASK_OUT_ABOVE_16(_registers[(int)(i)]));
            ea += unchecked((uint)(2));
            count++;
            }
            USE_CYCLES(unchecked((long)((count * unchecked((uint)(CYC_MOVEM_W))))));
        }
    }
    private void m68k_op_movem_16_re_aw()
    {
        unchecked
        {
            uint i = unchecked((uint)(0));
            uint register_list = OPER_I_16();
            uint ea = EA_AW_16();
            uint count = unchecked((uint)(0));
            for (; (i < unchecked((uint)(16))); i++) if (((register_list & unchecked((uint)((1 << unchecked((int)(i)))))) != 0)) {
            m68ki_write_16(ea, MASK_OUT_ABOVE_16(_registers[(int)(i)]));
            ea += unchecked((uint)(2));
            count++;
            }
            USE_CYCLES(unchecked((long)((count * unchecked((uint)(CYC_MOVEM_W))))));
        }
    }
    private void m68k_op_movem_16_re_al()
    {
        unchecked
        {
            uint i = unchecked((uint)(0));
            uint register_list = OPER_I_16();
            uint ea = EA_AL_16();
            uint count = unchecked((uint)(0));
            for (; (i < unchecked((uint)(16))); i++) if (((register_list & unchecked((uint)((1 << unchecked((int)(i)))))) != 0)) {
            m68ki_write_16(ea, MASK_OUT_ABOVE_16(_registers[(int)(i)]));
            ea += unchecked((uint)(2));
            count++;
            }
            USE_CYCLES(unchecked((long)((count * unchecked((uint)(CYC_MOVEM_W))))));
        }
    }
    private void m68k_op_movem_32_re_pd()
    {
        unchecked
        {
            uint i = unchecked((uint)(0));
            uint register_list = OPER_I_16();
            uint ea = AY;
            uint count = unchecked((uint)(0));
            for (; (i < unchecked((uint)(16))); i++) if (((register_list & unchecked((uint)((1 << unchecked((int)(i)))))) != 0)) {
            ea -= unchecked((uint)(4));
            m68ki_write_16((ea + unchecked((uint)(2))), (_registers[(int)((unchecked((uint)(15)) - i))] & unchecked((uint)(65535))));
            m68ki_write_16(ea, ((_registers[(int)((unchecked((uint)(15)) - i))] >> 16) & unchecked((uint)(65535))));
            count++;
            }
            AY = ea;
            USE_CYCLES(unchecked((long)((count * unchecked((uint)(CYC_MOVEM_L))))));
            SKIP_BUS_REFRESH();
        }
    }
    private void m68k_op_movem_32_re_ai()
    {
        unchecked
        {
            uint i = unchecked((uint)(0));
            uint register_list = OPER_I_16();
            uint ea = EA_AY_AI_32();
            uint count = unchecked((uint)(0));
            for (; (i < unchecked((uint)(16))); i++) if (((register_list & unchecked((uint)((1 << unchecked((int)(i)))))) != 0)) {
            m68ki_write_32(ea, _registers[(int)(i)]);
            ea += unchecked((uint)(4));
            count++;
            }
            USE_CYCLES(unchecked((long)((count * unchecked((uint)(CYC_MOVEM_L))))));
            SKIP_BUS_REFRESH();
        }
    }
    private void m68k_op_movem_32_re_di()
    {
        unchecked
        {
            uint i = unchecked((uint)(0));
            uint register_list = OPER_I_16();
            uint ea = EA_AY_DI_32();
            uint count = unchecked((uint)(0));
            for (; (i < unchecked((uint)(16))); i++) if (((register_list & unchecked((uint)((1 << unchecked((int)(i)))))) != 0)) {
            m68ki_write_32(ea, _registers[(int)(i)]);
            ea += unchecked((uint)(4));
            count++;
            }
            USE_CYCLES(unchecked((long)((count * unchecked((uint)(CYC_MOVEM_L))))));
            SKIP_BUS_REFRESH();
        }
    }
    private void m68k_op_movem_32_re_ix()
    {
        unchecked
        {
            uint i = unchecked((uint)(0));
            uint register_list = OPER_I_16();
            uint ea = EA_AY_IX_32();
            uint count = unchecked((uint)(0));
            for (; (i < unchecked((uint)(16))); i++) if (((register_list & unchecked((uint)((1 << unchecked((int)(i)))))) != 0)) {
            m68ki_write_32(ea, _registers[(int)(i)]);
            ea += unchecked((uint)(4));
            count++;
            }
            USE_CYCLES(unchecked((long)((count * unchecked((uint)(CYC_MOVEM_L))))));
            SKIP_BUS_REFRESH();
        }
    }
    private void m68k_op_movem_32_re_aw()
    {
        unchecked
        {
            uint i = unchecked((uint)(0));
            uint register_list = OPER_I_16();
            uint ea = EA_AW_32();
            uint count = unchecked((uint)(0));
            for (; (i < unchecked((uint)(16))); i++) if (((register_list & unchecked((uint)((1 << unchecked((int)(i)))))) != 0)) {
            m68ki_write_32(ea, _registers[(int)(i)]);
            ea += unchecked((uint)(4));
            count++;
            }
            USE_CYCLES(unchecked((long)((count * unchecked((uint)(CYC_MOVEM_L))))));
            SKIP_BUS_REFRESH();
        }
    }
    private void m68k_op_movem_32_re_al()
    {
        unchecked
        {
            uint i = unchecked((uint)(0));
            uint register_list = OPER_I_16();
            uint ea = EA_AL_32();
            uint count = unchecked((uint)(0));
            for (; (i < unchecked((uint)(16))); i++) if (((register_list & unchecked((uint)((1 << unchecked((int)(i)))))) != 0)) {
            m68ki_write_32(ea, _registers[(int)(i)]);
            ea += unchecked((uint)(4));
            count++;
            }
            USE_CYCLES(unchecked((long)((count * unchecked((uint)(CYC_MOVEM_L))))));
            SKIP_BUS_REFRESH();
        }
    }
    private void m68k_op_movem_16_er_pi()
    {
        unchecked
        {
            uint i = unchecked((uint)(0));
            uint register_list = OPER_I_16();
            uint ea = AY;
            uint count = unchecked((uint)(0));
            for (; (i < unchecked((uint)(16))); i++) if (((register_list & unchecked((uint)((1 << unchecked((int)(i)))))) != 0)) {
            _registers[(int)(i)] = unchecked((uint)(MAKE_INT_16(MASK_OUT_ABOVE_16(m68ki_read_16(ea)))));
            ea += unchecked((uint)(2));
            count++;
            }
            AY = ea;
            m68ki_read_16(ea);
            USE_CYCLES(unchecked((long)((count * unchecked((uint)(CYC_MOVEM_W))))));
        }
    }
    private void m68k_op_movem_16_er_pcdi()
    {
        unchecked
        {
            uint i = unchecked((uint)(0));
            uint register_list = OPER_I_16();
            uint ea = EA_PCDI_16();
            uint count = unchecked((uint)(0));
            for (; (i < unchecked((uint)(16))); i++) if (((register_list & unchecked((uint)((1 << unchecked((int)(i)))))) != 0)) {
            _registers[(int)(i)] = unchecked((uint)(MAKE_INT_16(MASK_OUT_ABOVE_16(m68ki_read_pcrel_16(ea)))));
            ea += unchecked((uint)(2));
            count++;
            }
            m68ki_read_16(ea);
            USE_CYCLES(unchecked((long)((count * unchecked((uint)(CYC_MOVEM_W))))));
        }
    }
    private void m68k_op_movem_16_er_pcix()
    {
        unchecked
        {
            uint i = unchecked((uint)(0));
            uint register_list = OPER_I_16();
            uint ea = EA_PCIX_16();
            uint count = unchecked((uint)(0));
            for (; (i < unchecked((uint)(16))); i++) if (((register_list & unchecked((uint)((1 << unchecked((int)(i)))))) != 0)) {
            _registers[(int)(i)] = unchecked((uint)(MAKE_INT_16(MASK_OUT_ABOVE_16(m68ki_read_pcrel_16(ea)))));
            ea += unchecked((uint)(2));
            count++;
            }
            m68ki_read_16(ea);
            USE_CYCLES(unchecked((long)((count * unchecked((uint)(CYC_MOVEM_W))))));
        }
    }
    private void m68k_op_movem_16_er_ai()
    {
        unchecked
        {
            uint i = unchecked((uint)(0));
            uint register_list = OPER_I_16();
            uint ea = EA_AY_AI_16();
            uint count = unchecked((uint)(0));
            for (; (i < unchecked((uint)(16))); i++) if (((register_list & unchecked((uint)((1 << unchecked((int)(i)))))) != 0)) {
            _registers[(int)(i)] = unchecked((uint)(MAKE_INT_16(MASK_OUT_ABOVE_16(m68ki_read_16(ea)))));
            ea += unchecked((uint)(2));
            count++;
            }
            m68ki_read_16(ea);
            USE_CYCLES(unchecked((long)((count * unchecked((uint)(CYC_MOVEM_W))))));
        }
    }
    private void m68k_op_movem_16_er_di()
    {
        unchecked
        {
            uint i = unchecked((uint)(0));
            uint register_list = OPER_I_16();
            uint ea = EA_AY_DI_16();
            uint count = unchecked((uint)(0));
            for (; (i < unchecked((uint)(16))); i++) if (((register_list & unchecked((uint)((1 << unchecked((int)(i)))))) != 0)) {
            _registers[(int)(i)] = unchecked((uint)(MAKE_INT_16(MASK_OUT_ABOVE_16(m68ki_read_16(ea)))));
            ea += unchecked((uint)(2));
            count++;
            }
            m68ki_read_16(ea);
            USE_CYCLES(unchecked((long)((count * unchecked((uint)(CYC_MOVEM_W))))));
        }
    }
    private void m68k_op_movem_16_er_ix()
    {
        unchecked
        {
            uint i = unchecked((uint)(0));
            uint register_list = OPER_I_16();
            uint ea = EA_AY_IX_16();
            uint count = unchecked((uint)(0));
            for (; (i < unchecked((uint)(16))); i++) if (((register_list & unchecked((uint)((1 << unchecked((int)(i)))))) != 0)) {
            _registers[(int)(i)] = unchecked((uint)(MAKE_INT_16(MASK_OUT_ABOVE_16(m68ki_read_16(ea)))));
            ea += unchecked((uint)(2));
            count++;
            }
            m68ki_read_16(ea);
            USE_CYCLES(unchecked((long)((count * unchecked((uint)(CYC_MOVEM_W))))));
        }
    }
    private void m68k_op_movem_16_er_aw()
    {
        unchecked
        {
            uint i = unchecked((uint)(0));
            uint register_list = OPER_I_16();
            uint ea = EA_AW_16();
            uint count = unchecked((uint)(0));
            for (; (i < unchecked((uint)(16))); i++) if (((register_list & unchecked((uint)((1 << unchecked((int)(i)))))) != 0)) {
            _registers[(int)(i)] = unchecked((uint)(MAKE_INT_16(MASK_OUT_ABOVE_16(m68ki_read_16(ea)))));
            ea += unchecked((uint)(2));
            count++;
            }
            m68ki_read_16(ea);
            USE_CYCLES(unchecked((long)((count * unchecked((uint)(CYC_MOVEM_W))))));
        }
    }
    private void m68k_op_movem_16_er_al()
    {
        unchecked
        {
            uint i = unchecked((uint)(0));
            uint register_list = OPER_I_16();
            uint ea = EA_AL_16();
            uint count = unchecked((uint)(0));
            for (; (i < unchecked((uint)(16))); i++) if (((register_list & unchecked((uint)((1 << unchecked((int)(i)))))) != 0)) {
            _registers[(int)(i)] = unchecked((uint)(MAKE_INT_16(MASK_OUT_ABOVE_16(m68ki_read_16(ea)))));
            ea += unchecked((uint)(2));
            count++;
            }
            m68ki_read_16(ea);
            USE_CYCLES(unchecked((long)((count * unchecked((uint)(CYC_MOVEM_W))))));
        }
    }
    private void m68k_op_movem_32_er_pi()
    {
        unchecked
        {
            uint i = unchecked((uint)(0));
            uint register_list = OPER_I_16();
            uint ea = AY;
            uint count = unchecked((uint)(0));
            for (; (i < unchecked((uint)(16))); i++) if (((register_list & unchecked((uint)((1 << unchecked((int)(i)))))) != 0)) {
            _registers[(int)(i)] = m68ki_read_32(ea);
            ea += unchecked((uint)(4));
            count++;
            }
            AY = ea;
            m68ki_read_16(ea);
            USE_CYCLES(unchecked((long)((count * unchecked((uint)(CYC_MOVEM_L))))));
            SKIP_BUS_REFRESH();
        }
    }
    private void m68k_op_movem_32_er_pcdi()
    {
        unchecked
        {
            uint i = unchecked((uint)(0));
            uint register_list = OPER_I_16();
            uint ea = EA_PCDI_32();
            uint count = unchecked((uint)(0));
            for (; (i < unchecked((uint)(16))); i++) if (((register_list & unchecked((uint)((1 << unchecked((int)(i)))))) != 0)) {
            _registers[(int)(i)] = m68ki_read_pcrel_32(ea);
            ea += unchecked((uint)(4));
            count++;
            }
            m68ki_read_16(ea);
            USE_CYCLES(unchecked((long)((count * unchecked((uint)(CYC_MOVEM_L))))));
            SKIP_BUS_REFRESH();
        }
    }
    private void m68k_op_movem_32_er_pcix()
    {
        unchecked
        {
            uint i = unchecked((uint)(0));
            uint register_list = OPER_I_16();
            uint ea = EA_PCIX_32();
            uint count = unchecked((uint)(0));
            for (; (i < unchecked((uint)(16))); i++) if (((register_list & unchecked((uint)((1 << unchecked((int)(i)))))) != 0)) {
            _registers[(int)(i)] = m68ki_read_pcrel_32(ea);
            ea += unchecked((uint)(4));
            count++;
            }
            m68ki_read_16(ea);
            USE_CYCLES(unchecked((long)((count * unchecked((uint)(CYC_MOVEM_L))))));
            SKIP_BUS_REFRESH();
        }
    }
    private void m68k_op_movem_32_er_ai()
    {
        unchecked
        {
            uint i = unchecked((uint)(0));
            uint register_list = OPER_I_16();
            uint ea = EA_AY_AI_32();
            uint count = unchecked((uint)(0));
            for (; (i < unchecked((uint)(16))); i++) if (((register_list & unchecked((uint)((1 << unchecked((int)(i)))))) != 0)) {
            _registers[(int)(i)] = m68ki_read_32(ea);
            ea += unchecked((uint)(4));
            count++;
            }
            m68ki_read_16(ea);
            USE_CYCLES(unchecked((long)((count * unchecked((uint)(CYC_MOVEM_L))))));
            SKIP_BUS_REFRESH();
        }
    }
    private void m68k_op_movem_32_er_di()
    {
        unchecked
        {
            uint i = unchecked((uint)(0));
            uint register_list = OPER_I_16();
            uint ea = EA_AY_DI_32();
            uint count = unchecked((uint)(0));
            for (; (i < unchecked((uint)(16))); i++) if (((register_list & unchecked((uint)((1 << unchecked((int)(i)))))) != 0)) {
            _registers[(int)(i)] = m68ki_read_32(ea);
            ea += unchecked((uint)(4));
            count++;
            }
            m68ki_read_16(ea);
            USE_CYCLES(unchecked((long)((count * unchecked((uint)(CYC_MOVEM_L))))));
            SKIP_BUS_REFRESH();
        }
    }
    private void m68k_op_movem_32_er_ix()
    {
        unchecked
        {
            uint i = unchecked((uint)(0));
            uint register_list = OPER_I_16();
            uint ea = EA_AY_IX_32();
            uint count = unchecked((uint)(0));
            for (; (i < unchecked((uint)(16))); i++) if (((register_list & unchecked((uint)((1 << unchecked((int)(i)))))) != 0)) {
            _registers[(int)(i)] = m68ki_read_32(ea);
            ea += unchecked((uint)(4));
            count++;
            }
            m68ki_read_16(ea);
            USE_CYCLES(unchecked((long)((count * unchecked((uint)(CYC_MOVEM_L))))));
            SKIP_BUS_REFRESH();
        }
    }
    private void m68k_op_movem_32_er_aw()
    {
        unchecked
        {
            uint i = unchecked((uint)(0));
            uint register_list = OPER_I_16();
            uint ea = EA_AW_32();
            uint count = unchecked((uint)(0));
            for (; (i < unchecked((uint)(16))); i++) if (((register_list & unchecked((uint)((1 << unchecked((int)(i)))))) != 0)) {
            _registers[(int)(i)] = m68ki_read_32(ea);
            ea += unchecked((uint)(4));
            count++;
            }
            m68ki_read_16(ea);
            USE_CYCLES(unchecked((long)((count * unchecked((uint)(CYC_MOVEM_L))))));
            SKIP_BUS_REFRESH();
        }
    }
    private void m68k_op_movem_32_er_al()
    {
        unchecked
        {
            uint i = unchecked((uint)(0));
            uint register_list = OPER_I_16();
            uint ea = EA_AL_32();
            uint count = unchecked((uint)(0));
            for (; (i < unchecked((uint)(16))); i++) if (((register_list & unchecked((uint)((1 << unchecked((int)(i)))))) != 0)) {
            _registers[(int)(i)] = m68ki_read_32(ea);
            ea += unchecked((uint)(4));
            count++;
            }
            m68ki_read_16(ea);
            USE_CYCLES(unchecked((long)((count * unchecked((uint)(CYC_MOVEM_L))))));
            SKIP_BUS_REFRESH();
        }
    }
    private void m68k_op_movep_16_re()
    {
        unchecked
        {
            uint ea = EA_AY_DI_16();
            uint src = DX;
            m68ki_write_8(ea, MASK_OUT_ABOVE_8((src >> 8)));
            m68ki_write_8((ea += unchecked((uint)(2))), MASK_OUT_ABOVE_8(src));
        }
    }
    private void m68k_op_movep_32_re()
    {
        unchecked
        {
            uint ea = EA_AY_DI_32();
            uint src = DX;
            m68ki_write_8(ea, MASK_OUT_ABOVE_8((src >> 24)));
            m68ki_write_8((ea += unchecked((uint)(2))), MASK_OUT_ABOVE_8((src >> 16)));
            m68ki_write_8((ea += unchecked((uint)(2))), MASK_OUT_ABOVE_8((src >> 8)));
            m68ki_write_8((ea += unchecked((uint)(2))), MASK_OUT_ABOVE_8(src));
        }
    }
    private void m68k_op_movep_16_er()
    {
        unchecked
        {
            uint ea = EA_AY_DI_16();
            ref uint r_dst = ref DX;
            r_dst = (MASK_OUT_BELOW_16(r_dst) | ((m68ki_read_8(ea) << 8) + m68ki_read_8((ea + unchecked((uint)(2))))));
        }
    }
    private void m68k_op_movep_32_er()
    {
        unchecked
        {
            uint ea = EA_AY_DI_32();
            DX = ((((m68ki_read_8(ea) << 24) + (m68ki_read_8((ea + unchecked((uint)(2)))) << 16)) + (m68ki_read_8((ea + unchecked((uint)(4)))) << 8)) + m68ki_read_8((ea + unchecked((uint)(6)))));
        }
    }
    private void m68k_op_moveq_32()
    {
        unchecked
        {
            uint res = (DX = unchecked((uint)(MAKE_INT_8(MASK_OUT_ABOVE_8(REG_IR)))));
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_muls_16_d()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            int src = MAKE_INT_16(DY);
            uint res = MASK_OUT_ABOVE_32(unchecked((uint)((src * MAKE_INT_16(MASK_OUT_ABOVE_16(r_dst))))));
            UseMulsCycles(unchecked((short)(src)));
            r_dst = res;
            FLAG_Z = res;
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_muls_16_ai()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            int src = MAKE_INT_16(OPER_AY_AI_16());
            uint res = MASK_OUT_ABOVE_32(unchecked((uint)((src * MAKE_INT_16(MASK_OUT_ABOVE_16(r_dst))))));
            UseMulsCycles(unchecked((short)(src)));
            r_dst = res;
            FLAG_Z = res;
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_muls_16_pi()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            int src = MAKE_INT_16(OPER_AY_PI_16());
            uint res = MASK_OUT_ABOVE_32(unchecked((uint)((src * MAKE_INT_16(MASK_OUT_ABOVE_16(r_dst))))));
            UseMulsCycles(unchecked((short)(src)));
            r_dst = res;
            FLAG_Z = res;
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_muls_16_pd()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            int src = MAKE_INT_16(OPER_AY_PD_16());
            uint res = MASK_OUT_ABOVE_32(unchecked((uint)((src * MAKE_INT_16(MASK_OUT_ABOVE_16(r_dst))))));
            UseMulsCycles(unchecked((short)(src)));
            r_dst = res;
            FLAG_Z = res;
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_muls_16_di()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            int src = MAKE_INT_16(OPER_AY_DI_16());
            uint res = MASK_OUT_ABOVE_32(unchecked((uint)((src * MAKE_INT_16(MASK_OUT_ABOVE_16(r_dst))))));
            UseMulsCycles(unchecked((short)(src)));
            r_dst = res;
            FLAG_Z = res;
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_muls_16_ix()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            int src = MAKE_INT_16(OPER_AY_IX_16());
            uint res = MASK_OUT_ABOVE_32(unchecked((uint)((src * MAKE_INT_16(MASK_OUT_ABOVE_16(r_dst))))));
            UseMulsCycles(unchecked((short)(src)));
            r_dst = res;
            FLAG_Z = res;
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_muls_16_aw()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            int src = MAKE_INT_16(OPER_AW_16());
            uint res = MASK_OUT_ABOVE_32(unchecked((uint)((src * MAKE_INT_16(MASK_OUT_ABOVE_16(r_dst))))));
            UseMulsCycles(unchecked((short)(src)));
            r_dst = res;
            FLAG_Z = res;
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_muls_16_al()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            int src = MAKE_INT_16(OPER_AL_16());
            uint res = MASK_OUT_ABOVE_32(unchecked((uint)((src * MAKE_INT_16(MASK_OUT_ABOVE_16(r_dst))))));
            UseMulsCycles(unchecked((short)(src)));
            r_dst = res;
            FLAG_Z = res;
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_muls_16_pcdi()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            int src = MAKE_INT_16(OPER_PCDI_16());
            uint res = MASK_OUT_ABOVE_32(unchecked((uint)((src * MAKE_INT_16(MASK_OUT_ABOVE_16(r_dst))))));
            UseMulsCycles(unchecked((short)(src)));
            r_dst = res;
            FLAG_Z = res;
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_muls_16_pcix()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            int src = MAKE_INT_16(OPER_PCIX_16());
            uint res = MASK_OUT_ABOVE_32(unchecked((uint)((src * MAKE_INT_16(MASK_OUT_ABOVE_16(r_dst))))));
            UseMulsCycles(unchecked((short)(src)));
            r_dst = res;
            FLAG_Z = res;
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_muls_16_i()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            int src = MAKE_INT_16(OPER_I_16());
            uint res = MASK_OUT_ABOVE_32(unchecked((uint)((src * MAKE_INT_16(MASK_OUT_ABOVE_16(r_dst))))));
            UseMulsCycles(unchecked((short)(src)));
            r_dst = res;
            FLAG_Z = res;
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_mulu_16_d()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = MASK_OUT_ABOVE_16(DY);
            uint res = (src * MASK_OUT_ABOVE_16(r_dst));
            UseMuluCycles(unchecked((ushort)(src)));
            r_dst = res;
            FLAG_Z = res;
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_mulu_16_ai()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_AI_16();
            uint res = (src * MASK_OUT_ABOVE_16(r_dst));
            UseMuluCycles(unchecked((ushort)(src)));
            r_dst = res;
            FLAG_Z = res;
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_mulu_16_pi()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_PI_16();
            uint res = (src * MASK_OUT_ABOVE_16(r_dst));
            UseMuluCycles(unchecked((ushort)(src)));
            r_dst = res;
            FLAG_Z = res;
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_mulu_16_pd()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_PD_16();
            uint res = (src * MASK_OUT_ABOVE_16(r_dst));
            UseMuluCycles(unchecked((ushort)(src)));
            r_dst = res;
            FLAG_Z = res;
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_mulu_16_di()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_DI_16();
            uint res = (src * MASK_OUT_ABOVE_16(r_dst));
            UseMuluCycles(unchecked((ushort)(src)));
            r_dst = res;
            FLAG_Z = res;
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_mulu_16_ix()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_IX_16();
            uint res = (src * MASK_OUT_ABOVE_16(r_dst));
            UseMuluCycles(unchecked((ushort)(src)));
            r_dst = res;
            FLAG_Z = res;
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_mulu_16_aw()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AW_16();
            uint res = (src * MASK_OUT_ABOVE_16(r_dst));
            UseMuluCycles(unchecked((ushort)(src)));
            r_dst = res;
            FLAG_Z = res;
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_mulu_16_al()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AL_16();
            uint res = (src * MASK_OUT_ABOVE_16(r_dst));
            UseMuluCycles(unchecked((ushort)(src)));
            r_dst = res;
            FLAG_Z = res;
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_mulu_16_pcdi()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_PCDI_16();
            uint res = (src * MASK_OUT_ABOVE_16(r_dst));
            UseMuluCycles(unchecked((ushort)(src)));
            r_dst = res;
            FLAG_Z = res;
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_mulu_16_pcix()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_PCIX_16();
            uint res = (src * MASK_OUT_ABOVE_16(r_dst));
            UseMuluCycles(unchecked((ushort)(src)));
            r_dst = res;
            FLAG_Z = res;
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_mulu_16_i()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_I_16();
            uint res = (src * MASK_OUT_ABOVE_16(r_dst));
            UseMuluCycles(unchecked((ushort)(src)));
            r_dst = res;
            FLAG_Z = res;
            FLAG_N = NFLAG_32(res);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_nbcd_8_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = ((0u - dst) - XFLAG_AS_1());
            if ((res != 0)) {
            FLAG_V = res;
            if ((((res | dst) & unchecked((uint)(15))) == unchecked((uint)(0)))) res = ((res & unchecked((uint)(240))) + unchecked((uint)(6)));
            res = MASK_OUT_ABOVE_8((res + unchecked((uint)(154))));
            FLAG_V &= (~res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_Z |= res;
            FLAG_C = CFLAG_SET;
            FLAG_X = XFLAG_SET;
            FLAG_N = NFLAG_8(res);
            } else {
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_X = XFLAG_CLEAR;
            FLAG_N = NFLAG_CLEAR;
            }
        }
    }
    private void m68k_op_nbcd_8_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_8();
            uint dst = m68ki_read_8(ea);
            uint res = ((0u - dst) - XFLAG_AS_1());
            if ((res != 0)) {
            FLAG_V = res;
            if ((((res | dst) & unchecked((uint)(15))) == unchecked((uint)(0)))) res = ((res & unchecked((uint)(240))) + unchecked((uint)(6)));
            res = MASK_OUT_ABOVE_8((res + unchecked((uint)(154))));
            FLAG_V &= (~res);
            m68ki_write_8(ea, res);
            FLAG_Z |= res;
            FLAG_C = CFLAG_SET;
            FLAG_X = XFLAG_SET;
            FLAG_N = NFLAG_8(res);
            } else {
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_X = XFLAG_CLEAR;
            FLAG_N = NFLAG_CLEAR;
            }
        }
    }
    private void m68k_op_nbcd_8_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_8();
            uint dst = m68ki_read_8(ea);
            uint res = ((0u - dst) - XFLAG_AS_1());
            if ((res != 0)) {
            FLAG_V = res;
            if ((((res | dst) & unchecked((uint)(15))) == unchecked((uint)(0)))) res = ((res & unchecked((uint)(240))) + unchecked((uint)(6)));
            res = MASK_OUT_ABOVE_8((res + unchecked((uint)(154))));
            FLAG_V &= (~res);
            m68ki_write_8(ea, res);
            FLAG_Z |= res;
            FLAG_C = CFLAG_SET;
            FLAG_X = XFLAG_SET;
            FLAG_N = NFLAG_8(res);
            } else {
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_X = XFLAG_CLEAR;
            FLAG_N = NFLAG_CLEAR;
            }
        }
    }
    private void m68k_op_nbcd_8_pi7()
    {
        unchecked
        {
            uint ea = EA_A7_PI_8();
            uint dst = m68ki_read_8(ea);
            uint res = ((0u - dst) - XFLAG_AS_1());
            if ((res != 0)) {
            FLAG_V = res;
            if ((((res | dst) & unchecked((uint)(15))) == unchecked((uint)(0)))) res = ((res & unchecked((uint)(240))) + unchecked((uint)(6)));
            res = MASK_OUT_ABOVE_8((res + unchecked((uint)(154))));
            FLAG_V &= (~res);
            m68ki_write_8(ea, res);
            FLAG_Z |= res;
            FLAG_C = CFLAG_SET;
            FLAG_X = XFLAG_SET;
            FLAG_N = NFLAG_8(res);
            } else {
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_X = XFLAG_CLEAR;
            FLAG_N = NFLAG_CLEAR;
            }
        }
    }
    private void m68k_op_nbcd_8_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_8();
            uint dst = m68ki_read_8(ea);
            uint res = ((0u - dst) - XFLAG_AS_1());
            if ((res != 0)) {
            FLAG_V = res;
            if ((((res | dst) & unchecked((uint)(15))) == unchecked((uint)(0)))) res = ((res & unchecked((uint)(240))) + unchecked((uint)(6)));
            res = MASK_OUT_ABOVE_8((res + unchecked((uint)(154))));
            FLAG_V &= (~res);
            m68ki_write_8(ea, res);
            FLAG_Z |= res;
            FLAG_C = CFLAG_SET;
            FLAG_X = XFLAG_SET;
            FLAG_N = NFLAG_8(res);
            } else {
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_X = XFLAG_CLEAR;
            FLAG_N = NFLAG_CLEAR;
            }
        }
    }
    private void m68k_op_nbcd_8_pd7()
    {
        unchecked
        {
            uint ea = EA_A7_PD_8();
            uint dst = m68ki_read_8(ea);
            uint res = ((0u - dst) - XFLAG_AS_1());
            if ((res != 0)) {
            FLAG_V = res;
            if ((((res | dst) & unchecked((uint)(15))) == unchecked((uint)(0)))) res = ((res & unchecked((uint)(240))) + unchecked((uint)(6)));
            res = MASK_OUT_ABOVE_8((res + unchecked((uint)(154))));
            FLAG_V &= (~res);
            m68ki_write_8(ea, res);
            FLAG_Z |= res;
            FLAG_C = CFLAG_SET;
            FLAG_X = XFLAG_SET;
            FLAG_N = NFLAG_8(res);
            } else {
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_X = XFLAG_CLEAR;
            FLAG_N = NFLAG_CLEAR;
            }
        }
    }
    private void m68k_op_nbcd_8_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_8();
            uint dst = m68ki_read_8(ea);
            uint res = ((0u - dst) - XFLAG_AS_1());
            if ((res != 0)) {
            FLAG_V = res;
            if ((((res | dst) & unchecked((uint)(15))) == unchecked((uint)(0)))) res = ((res & unchecked((uint)(240))) + unchecked((uint)(6)));
            res = MASK_OUT_ABOVE_8((res + unchecked((uint)(154))));
            FLAG_V &= (~res);
            m68ki_write_8(ea, res);
            FLAG_Z |= res;
            FLAG_C = CFLAG_SET;
            FLAG_X = XFLAG_SET;
            FLAG_N = NFLAG_8(res);
            } else {
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_X = XFLAG_CLEAR;
            FLAG_N = NFLAG_CLEAR;
            }
        }
    }
    private void m68k_op_nbcd_8_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_8();
            uint dst = m68ki_read_8(ea);
            uint res = ((0u - dst) - XFLAG_AS_1());
            if ((res != 0)) {
            FLAG_V = res;
            if ((((res | dst) & unchecked((uint)(15))) == unchecked((uint)(0)))) res = ((res & unchecked((uint)(240))) + unchecked((uint)(6)));
            res = MASK_OUT_ABOVE_8((res + unchecked((uint)(154))));
            FLAG_V &= (~res);
            m68ki_write_8(ea, res);
            FLAG_Z |= res;
            FLAG_C = CFLAG_SET;
            FLAG_X = XFLAG_SET;
            FLAG_N = NFLAG_8(res);
            } else {
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_X = XFLAG_CLEAR;
            FLAG_N = NFLAG_CLEAR;
            }
        }
    }
    private void m68k_op_nbcd_8_aw()
    {
        unchecked
        {
            uint ea = EA_AW_8();
            uint dst = m68ki_read_8(ea);
            uint res = ((0u - dst) - XFLAG_AS_1());
            if ((res != 0)) {
            FLAG_V = res;
            if ((((res | dst) & unchecked((uint)(15))) == unchecked((uint)(0)))) res = ((res & unchecked((uint)(240))) + unchecked((uint)(6)));
            res = MASK_OUT_ABOVE_8((res + unchecked((uint)(154))));
            FLAG_V &= (~res);
            m68ki_write_8(ea, res);
            FLAG_Z |= res;
            FLAG_C = CFLAG_SET;
            FLAG_X = XFLAG_SET;
            FLAG_N = NFLAG_8(res);
            } else {
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_X = XFLAG_CLEAR;
            FLAG_N = NFLAG_CLEAR;
            }
        }
    }
    private void m68k_op_nbcd_8_al()
    {
        unchecked
        {
            uint ea = EA_AL_8();
            uint dst = m68ki_read_8(ea);
            uint res = ((0u - dst) - XFLAG_AS_1());
            if ((res != 0)) {
            FLAG_V = res;
            if ((((res | dst) & unchecked((uint)(15))) == unchecked((uint)(0)))) res = ((res & unchecked((uint)(240))) + unchecked((uint)(6)));
            res = MASK_OUT_ABOVE_8((res + unchecked((uint)(154))));
            FLAG_V &= (~res);
            m68ki_write_8(ea, res);
            FLAG_Z |= res;
            FLAG_C = CFLAG_SET;
            FLAG_X = XFLAG_SET;
            FLAG_N = NFLAG_8(res);
            } else {
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            FLAG_X = XFLAG_CLEAR;
            FLAG_N = NFLAG_CLEAR;
            }
        }
    }
    private void m68k_op_neg_8_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint res = (unchecked((uint)(0)) - MASK_OUT_ABOVE_8(r_dst));
            FLAG_N = NFLAG_8(res);
            FLAG_C = (FLAG_X = CFLAG_8(res));
            FLAG_V = (r_dst & res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_neg_8_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_8();
            uint src = m68ki_read_8(ea);
            uint res = (unchecked((uint)(0)) - src);
            FLAG_N = NFLAG_8(res);
            FLAG_C = (FLAG_X = CFLAG_8(res));
            FLAG_V = (src & res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_neg_8_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_8();
            uint src = m68ki_read_8(ea);
            uint res = (unchecked((uint)(0)) - src);
            FLAG_N = NFLAG_8(res);
            FLAG_C = (FLAG_X = CFLAG_8(res));
            FLAG_V = (src & res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_neg_8_pi7()
    {
        unchecked
        {
            uint ea = EA_A7_PI_8();
            uint src = m68ki_read_8(ea);
            uint res = (unchecked((uint)(0)) - src);
            FLAG_N = NFLAG_8(res);
            FLAG_C = (FLAG_X = CFLAG_8(res));
            FLAG_V = (src & res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_neg_8_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_8();
            uint src = m68ki_read_8(ea);
            uint res = (unchecked((uint)(0)) - src);
            FLAG_N = NFLAG_8(res);
            FLAG_C = (FLAG_X = CFLAG_8(res));
            FLAG_V = (src & res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_neg_8_pd7()
    {
        unchecked
        {
            uint ea = EA_A7_PD_8();
            uint src = m68ki_read_8(ea);
            uint res = (unchecked((uint)(0)) - src);
            FLAG_N = NFLAG_8(res);
            FLAG_C = (FLAG_X = CFLAG_8(res));
            FLAG_V = (src & res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_neg_8_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_8();
            uint src = m68ki_read_8(ea);
            uint res = (unchecked((uint)(0)) - src);
            FLAG_N = NFLAG_8(res);
            FLAG_C = (FLAG_X = CFLAG_8(res));
            FLAG_V = (src & res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_neg_8_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_8();
            uint src = m68ki_read_8(ea);
            uint res = (unchecked((uint)(0)) - src);
            FLAG_N = NFLAG_8(res);
            FLAG_C = (FLAG_X = CFLAG_8(res));
            FLAG_V = (src & res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_neg_8_aw()
    {
        unchecked
        {
            uint ea = EA_AW_8();
            uint src = m68ki_read_8(ea);
            uint res = (unchecked((uint)(0)) - src);
            FLAG_N = NFLAG_8(res);
            FLAG_C = (FLAG_X = CFLAG_8(res));
            FLAG_V = (src & res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_neg_8_al()
    {
        unchecked
        {
            uint ea = EA_AL_8();
            uint src = m68ki_read_8(ea);
            uint res = (unchecked((uint)(0)) - src);
            FLAG_N = NFLAG_8(res);
            FLAG_C = (FLAG_X = CFLAG_8(res));
            FLAG_V = (src & res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_neg_16_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint res = (unchecked((uint)(0)) - MASK_OUT_ABOVE_16(r_dst));
            FLAG_N = NFLAG_16(res);
            FLAG_C = (FLAG_X = CFLAG_16(res));
            FLAG_V = ((r_dst & res) >> 8);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_neg_16_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_16();
            uint src = m68ki_read_16(ea);
            uint res = (unchecked((uint)(0)) - src);
            FLAG_N = NFLAG_16(res);
            FLAG_C = (FLAG_X = CFLAG_16(res));
            FLAG_V = ((src & res) >> 8);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_neg_16_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_16();
            uint src = m68ki_read_16(ea);
            uint res = (unchecked((uint)(0)) - src);
            FLAG_N = NFLAG_16(res);
            FLAG_C = (FLAG_X = CFLAG_16(res));
            FLAG_V = ((src & res) >> 8);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_neg_16_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_16();
            uint src = m68ki_read_16(ea);
            uint res = (unchecked((uint)(0)) - src);
            FLAG_N = NFLAG_16(res);
            FLAG_C = (FLAG_X = CFLAG_16(res));
            FLAG_V = ((src & res) >> 8);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_neg_16_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_16();
            uint src = m68ki_read_16(ea);
            uint res = (unchecked((uint)(0)) - src);
            FLAG_N = NFLAG_16(res);
            FLAG_C = (FLAG_X = CFLAG_16(res));
            FLAG_V = ((src & res) >> 8);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_neg_16_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_16();
            uint src = m68ki_read_16(ea);
            uint res = (unchecked((uint)(0)) - src);
            FLAG_N = NFLAG_16(res);
            FLAG_C = (FLAG_X = CFLAG_16(res));
            FLAG_V = ((src & res) >> 8);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_neg_16_aw()
    {
        unchecked
        {
            uint ea = EA_AW_16();
            uint src = m68ki_read_16(ea);
            uint res = (unchecked((uint)(0)) - src);
            FLAG_N = NFLAG_16(res);
            FLAG_C = (FLAG_X = CFLAG_16(res));
            FLAG_V = ((src & res) >> 8);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_neg_16_al()
    {
        unchecked
        {
            uint ea = EA_AL_16();
            uint src = m68ki_read_16(ea);
            uint res = (unchecked((uint)(0)) - src);
            FLAG_N = NFLAG_16(res);
            FLAG_C = (FLAG_X = CFLAG_16(res));
            FLAG_V = ((src & res) >> 8);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_neg_32_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint res = (unchecked((uint)(0)) - r_dst);
            FLAG_N = NFLAG_32(res);
            FLAG_C = (FLAG_X = CFLAG_SUB_32(r_dst, unchecked((uint)(0)), res));
            FLAG_V = ((r_dst & res) >> 24);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            r_dst = FLAG_Z;
        }
    }
    private void m68k_op_neg_32_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_32();
            uint src = m68ki_read_32(ea);
            uint res = (unchecked((uint)(0)) - src);
            FLAG_N = NFLAG_32(res);
            FLAG_C = (FLAG_X = CFLAG_SUB_32(src, unchecked((uint)(0)), res));
            FLAG_V = ((src & res) >> 24);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_neg_32_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_32();
            uint src = m68ki_read_32(ea);
            uint res = (unchecked((uint)(0)) - src);
            FLAG_N = NFLAG_32(res);
            FLAG_C = (FLAG_X = CFLAG_SUB_32(src, unchecked((uint)(0)), res));
            FLAG_V = ((src & res) >> 24);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_neg_32_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_32();
            uint src = m68ki_read_32(ea);
            uint res = (unchecked((uint)(0)) - src);
            FLAG_N = NFLAG_32(res);
            FLAG_C = (FLAG_X = CFLAG_SUB_32(src, unchecked((uint)(0)), res));
            FLAG_V = ((src & res) >> 24);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_neg_32_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_32();
            uint src = m68ki_read_32(ea);
            uint res = (unchecked((uint)(0)) - src);
            FLAG_N = NFLAG_32(res);
            FLAG_C = (FLAG_X = CFLAG_SUB_32(src, unchecked((uint)(0)), res));
            FLAG_V = ((src & res) >> 24);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_neg_32_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_32();
            uint src = m68ki_read_32(ea);
            uint res = (unchecked((uint)(0)) - src);
            FLAG_N = NFLAG_32(res);
            FLAG_C = (FLAG_X = CFLAG_SUB_32(src, unchecked((uint)(0)), res));
            FLAG_V = ((src & res) >> 24);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_neg_32_aw()
    {
        unchecked
        {
            uint ea = EA_AW_32();
            uint src = m68ki_read_32(ea);
            uint res = (unchecked((uint)(0)) - src);
            FLAG_N = NFLAG_32(res);
            FLAG_C = (FLAG_X = CFLAG_SUB_32(src, unchecked((uint)(0)), res));
            FLAG_V = ((src & res) >> 24);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_neg_32_al()
    {
        unchecked
        {
            uint ea = EA_AL_32();
            uint src = m68ki_read_32(ea);
            uint res = (unchecked((uint)(0)) - src);
            FLAG_N = NFLAG_32(res);
            FLAG_C = (FLAG_X = CFLAG_SUB_32(src, unchecked((uint)(0)), res));
            FLAG_V = ((src & res) >> 24);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_negx_8_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint res = ((unchecked((uint)(0)) - MASK_OUT_ABOVE_8(r_dst)) - XFLAG_AS_1());
            FLAG_N = NFLAG_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = (r_dst & res);
            res = MASK_OUT_ABOVE_8(res);
            FLAG_Z |= res;
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
        }
    }
    private void m68k_op_negx_8_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_8();
            uint src = m68ki_read_8(ea);
            uint res = ((unchecked((uint)(0)) - src) - XFLAG_AS_1());
            FLAG_N = NFLAG_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = (src & res);
            res = MASK_OUT_ABOVE_8(res);
            FLAG_Z |= res;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_negx_8_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_8();
            uint src = m68ki_read_8(ea);
            uint res = ((unchecked((uint)(0)) - src) - XFLAG_AS_1());
            FLAG_N = NFLAG_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = (src & res);
            res = MASK_OUT_ABOVE_8(res);
            FLAG_Z |= res;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_negx_8_pi7()
    {
        unchecked
        {
            uint ea = EA_A7_PI_8();
            uint src = m68ki_read_8(ea);
            uint res = ((unchecked((uint)(0)) - src) - XFLAG_AS_1());
            FLAG_N = NFLAG_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = (src & res);
            res = MASK_OUT_ABOVE_8(res);
            FLAG_Z |= res;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_negx_8_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_8();
            uint src = m68ki_read_8(ea);
            uint res = ((unchecked((uint)(0)) - src) - XFLAG_AS_1());
            FLAG_N = NFLAG_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = (src & res);
            res = MASK_OUT_ABOVE_8(res);
            FLAG_Z |= res;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_negx_8_pd7()
    {
        unchecked
        {
            uint ea = EA_A7_PD_8();
            uint src = m68ki_read_8(ea);
            uint res = ((unchecked((uint)(0)) - src) - XFLAG_AS_1());
            FLAG_N = NFLAG_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = (src & res);
            res = MASK_OUT_ABOVE_8(res);
            FLAG_Z |= res;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_negx_8_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_8();
            uint src = m68ki_read_8(ea);
            uint res = ((unchecked((uint)(0)) - src) - XFLAG_AS_1());
            FLAG_N = NFLAG_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = (src & res);
            res = MASK_OUT_ABOVE_8(res);
            FLAG_Z |= res;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_negx_8_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_8();
            uint src = m68ki_read_8(ea);
            uint res = ((unchecked((uint)(0)) - src) - XFLAG_AS_1());
            FLAG_N = NFLAG_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = (src & res);
            res = MASK_OUT_ABOVE_8(res);
            FLAG_Z |= res;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_negx_8_aw()
    {
        unchecked
        {
            uint ea = EA_AW_8();
            uint src = m68ki_read_8(ea);
            uint res = ((unchecked((uint)(0)) - src) - XFLAG_AS_1());
            FLAG_N = NFLAG_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = (src & res);
            res = MASK_OUT_ABOVE_8(res);
            FLAG_Z |= res;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_negx_8_al()
    {
        unchecked
        {
            uint ea = EA_AL_8();
            uint src = m68ki_read_8(ea);
            uint res = ((unchecked((uint)(0)) - src) - XFLAG_AS_1());
            FLAG_N = NFLAG_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = (src & res);
            res = MASK_OUT_ABOVE_8(res);
            FLAG_Z |= res;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_negx_16_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint res = ((unchecked((uint)(0)) - MASK_OUT_ABOVE_16(r_dst)) - XFLAG_AS_1());
            FLAG_N = NFLAG_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = ((r_dst & res) >> 8);
            res = MASK_OUT_ABOVE_16(res);
            FLAG_Z |= res;
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
        }
    }
    private void m68k_op_negx_16_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_16();
            uint src = m68ki_read_16(ea);
            uint res = ((unchecked((uint)(0)) - MASK_OUT_ABOVE_16(src)) - XFLAG_AS_1());
            FLAG_N = NFLAG_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = ((src & res) >> 8);
            res = MASK_OUT_ABOVE_16(res);
            FLAG_Z |= res;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_negx_16_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_16();
            uint src = m68ki_read_16(ea);
            uint res = ((unchecked((uint)(0)) - MASK_OUT_ABOVE_16(src)) - XFLAG_AS_1());
            FLAG_N = NFLAG_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = ((src & res) >> 8);
            res = MASK_OUT_ABOVE_16(res);
            FLAG_Z |= res;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_negx_16_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_16();
            uint src = m68ki_read_16(ea);
            uint res = ((unchecked((uint)(0)) - MASK_OUT_ABOVE_16(src)) - XFLAG_AS_1());
            FLAG_N = NFLAG_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = ((src & res) >> 8);
            res = MASK_OUT_ABOVE_16(res);
            FLAG_Z |= res;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_negx_16_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_16();
            uint src = m68ki_read_16(ea);
            uint res = ((unchecked((uint)(0)) - MASK_OUT_ABOVE_16(src)) - XFLAG_AS_1());
            FLAG_N = NFLAG_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = ((src & res) >> 8);
            res = MASK_OUT_ABOVE_16(res);
            FLAG_Z |= res;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_negx_16_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_16();
            uint src = m68ki_read_16(ea);
            uint res = ((unchecked((uint)(0)) - MASK_OUT_ABOVE_16(src)) - XFLAG_AS_1());
            FLAG_N = NFLAG_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = ((src & res) >> 8);
            res = MASK_OUT_ABOVE_16(res);
            FLAG_Z |= res;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_negx_16_aw()
    {
        unchecked
        {
            uint ea = EA_AW_16();
            uint src = m68ki_read_16(ea);
            uint res = ((unchecked((uint)(0)) - MASK_OUT_ABOVE_16(src)) - XFLAG_AS_1());
            FLAG_N = NFLAG_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = ((src & res) >> 8);
            res = MASK_OUT_ABOVE_16(res);
            FLAG_Z |= res;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_negx_16_al()
    {
        unchecked
        {
            uint ea = EA_AL_16();
            uint src = m68ki_read_16(ea);
            uint res = ((unchecked((uint)(0)) - MASK_OUT_ABOVE_16(src)) - XFLAG_AS_1());
            FLAG_N = NFLAG_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = ((src & res) >> 8);
            res = MASK_OUT_ABOVE_16(res);
            FLAG_Z |= res;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_negx_32_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint res = ((unchecked((uint)(0)) - MASK_OUT_ABOVE_32(r_dst)) - XFLAG_AS_1());
            FLAG_N = NFLAG_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(r_dst, unchecked((uint)(0)), res));
            FLAG_V = ((r_dst & res) >> 24);
            res = MASK_OUT_ABOVE_32(res);
            FLAG_Z |= res;
            r_dst = res;
        }
    }
    private void m68k_op_negx_32_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_32();
            uint src = m68ki_read_32(ea);
            uint res = ((unchecked((uint)(0)) - MASK_OUT_ABOVE_32(src)) - XFLAG_AS_1());
            FLAG_N = NFLAG_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, unchecked((uint)(0)), res));
            FLAG_V = ((src & res) >> 24);
            res = MASK_OUT_ABOVE_32(res);
            FLAG_Z |= res;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_negx_32_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_32();
            uint src = m68ki_read_32(ea);
            uint res = ((unchecked((uint)(0)) - MASK_OUT_ABOVE_32(src)) - XFLAG_AS_1());
            FLAG_N = NFLAG_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, unchecked((uint)(0)), res));
            FLAG_V = ((src & res) >> 24);
            res = MASK_OUT_ABOVE_32(res);
            FLAG_Z |= res;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_negx_32_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_32();
            uint src = m68ki_read_32(ea);
            uint res = ((unchecked((uint)(0)) - MASK_OUT_ABOVE_32(src)) - XFLAG_AS_1());
            FLAG_N = NFLAG_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, unchecked((uint)(0)), res));
            FLAG_V = ((src & res) >> 24);
            res = MASK_OUT_ABOVE_32(res);
            FLAG_Z |= res;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_negx_32_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_32();
            uint src = m68ki_read_32(ea);
            uint res = ((unchecked((uint)(0)) - MASK_OUT_ABOVE_32(src)) - XFLAG_AS_1());
            FLAG_N = NFLAG_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, unchecked((uint)(0)), res));
            FLAG_V = ((src & res) >> 24);
            res = MASK_OUT_ABOVE_32(res);
            FLAG_Z |= res;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_negx_32_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_32();
            uint src = m68ki_read_32(ea);
            uint res = ((unchecked((uint)(0)) - MASK_OUT_ABOVE_32(src)) - XFLAG_AS_1());
            FLAG_N = NFLAG_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, unchecked((uint)(0)), res));
            FLAG_V = ((src & res) >> 24);
            res = MASK_OUT_ABOVE_32(res);
            FLAG_Z |= res;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_negx_32_aw()
    {
        unchecked
        {
            uint ea = EA_AW_32();
            uint src = m68ki_read_32(ea);
            uint res = ((unchecked((uint)(0)) - MASK_OUT_ABOVE_32(src)) - XFLAG_AS_1());
            FLAG_N = NFLAG_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, unchecked((uint)(0)), res));
            FLAG_V = ((src & res) >> 24);
            res = MASK_OUT_ABOVE_32(res);
            FLAG_Z |= res;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_negx_32_al()
    {
        unchecked
        {
            uint ea = EA_AL_32();
            uint src = m68ki_read_32(ea);
            uint res = ((unchecked((uint)(0)) - MASK_OUT_ABOVE_32(src)) - XFLAG_AS_1());
            FLAG_N = NFLAG_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, unchecked((uint)(0)), res));
            FLAG_V = ((src & res) >> 24);
            res = MASK_OUT_ABOVE_32(res);
            FLAG_Z |= res;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_nop()
    {
        unchecked
        {

        }
    }
    private void m68k_op_not_8_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint res = MASK_OUT_ABOVE_8((~r_dst));
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_not_8_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_8();
            uint res = MASK_OUT_ABOVE_8((~m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_not_8_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_8();
            uint res = MASK_OUT_ABOVE_8((~m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_not_8_pi7()
    {
        unchecked
        {
            uint ea = EA_A7_PI_8();
            uint res = MASK_OUT_ABOVE_8((~m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_not_8_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_8();
            uint res = MASK_OUT_ABOVE_8((~m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_not_8_pd7()
    {
        unchecked
        {
            uint ea = EA_A7_PD_8();
            uint res = MASK_OUT_ABOVE_8((~m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_not_8_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_8();
            uint res = MASK_OUT_ABOVE_8((~m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_not_8_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_8();
            uint res = MASK_OUT_ABOVE_8((~m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_not_8_aw()
    {
        unchecked
        {
            uint ea = EA_AW_8();
            uint res = MASK_OUT_ABOVE_8((~m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_not_8_al()
    {
        unchecked
        {
            uint ea = EA_AL_8();
            uint res = MASK_OUT_ABOVE_8((~m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_not_16_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint res = MASK_OUT_ABOVE_16((~r_dst));
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_not_16_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_16();
            uint res = MASK_OUT_ABOVE_16((~m68ki_read_16(ea)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_not_16_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_16();
            uint res = MASK_OUT_ABOVE_16((~m68ki_read_16(ea)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_not_16_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_16();
            uint res = MASK_OUT_ABOVE_16((~m68ki_read_16(ea)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_not_16_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_16();
            uint res = MASK_OUT_ABOVE_16((~m68ki_read_16(ea)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_not_16_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_16();
            uint res = MASK_OUT_ABOVE_16((~m68ki_read_16(ea)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_not_16_aw()
    {
        unchecked
        {
            uint ea = EA_AW_16();
            uint res = MASK_OUT_ABOVE_16((~m68ki_read_16(ea)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_not_16_al()
    {
        unchecked
        {
            uint ea = EA_AL_16();
            uint res = MASK_OUT_ABOVE_16((~m68ki_read_16(ea)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_not_32_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint res = (r_dst = MASK_OUT_ABOVE_32((~r_dst)));
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_not_32_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_32();
            uint res = MASK_OUT_ABOVE_32((~m68ki_read_32(ea)));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_not_32_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_32();
            uint res = MASK_OUT_ABOVE_32((~m68ki_read_32(ea)));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_not_32_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_32();
            uint res = MASK_OUT_ABOVE_32((~m68ki_read_32(ea)));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_not_32_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_32();
            uint res = MASK_OUT_ABOVE_32((~m68ki_read_32(ea)));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_not_32_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_32();
            uint res = MASK_OUT_ABOVE_32((~m68ki_read_32(ea)));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_not_32_aw()
    {
        unchecked
        {
            uint ea = EA_AW_32();
            uint res = MASK_OUT_ABOVE_32((~m68ki_read_32(ea)));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_not_32_al()
    {
        unchecked
        {
            uint ea = EA_AL_32();
            uint res = MASK_OUT_ABOVE_32((~m68ki_read_32(ea)));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_8_er_d()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_8((DX |= MASK_OUT_ABOVE_8(DY)));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_8_er_ai()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_8((DX |= OPER_AY_AI_8()));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_8_er_pi()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_8((DX |= OPER_AY_PI_8()));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_8_er_pi7()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_8((DX |= OPER_A7_PI_8()));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_8_er_pd()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_8((DX |= OPER_AY_PD_8()));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_8_er_pd7()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_8((DX |= OPER_A7_PD_8()));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_8_er_di()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_8((DX |= OPER_AY_DI_8()));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_8_er_ix()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_8((DX |= OPER_AY_IX_8()));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_8_er_aw()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_8((DX |= OPER_AW_8()));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_8_er_al()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_8((DX |= OPER_AL_8()));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_8_er_pcdi()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_8((DX |= OPER_PCDI_8()));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_8_er_pcix()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_8((DX |= OPER_PCIX_8()));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_8_er_i()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_8((DX |= OPER_I_8()));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_16_er_d()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16((DX |= MASK_OUT_ABOVE_16(DY)));
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_16_er_ai()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16((DX |= OPER_AY_AI_16()));
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_16_er_pi()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16((DX |= OPER_AY_PI_16()));
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_16_er_pd()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16((DX |= OPER_AY_PD_16()));
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_16_er_di()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16((DX |= OPER_AY_DI_16()));
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_16_er_ix()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16((DX |= OPER_AY_IX_16()));
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_16_er_aw()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16((DX |= OPER_AW_16()));
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_16_er_al()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16((DX |= OPER_AL_16()));
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_16_er_pcdi()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16((DX |= OPER_PCDI_16()));
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_16_er_pcix()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16((DX |= OPER_PCIX_16()));
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_16_er_i()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16((DX |= OPER_I_16()));
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_32_er_d()
    {
        unchecked
        {
            uint res = (DX |= DY);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_32_er_ai()
    {
        unchecked
        {
            uint res = (DX |= OPER_AY_AI_32());
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_32_er_pi()
    {
        unchecked
        {
            uint res = (DX |= OPER_AY_PI_32());
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_32_er_pd()
    {
        unchecked
        {
            uint res = (DX |= OPER_AY_PD_32());
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_32_er_di()
    {
        unchecked
        {
            uint res = (DX |= OPER_AY_DI_32());
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_32_er_ix()
    {
        unchecked
        {
            uint res = (DX |= OPER_AY_IX_32());
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_32_er_aw()
    {
        unchecked
        {
            uint res = (DX |= OPER_AW_32());
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_32_er_al()
    {
        unchecked
        {
            uint res = (DX |= OPER_AL_32());
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_32_er_pcdi()
    {
        unchecked
        {
            uint res = (DX |= OPER_PCDI_32());
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_32_er_pcix()
    {
        unchecked
        {
            uint res = (DX |= OPER_PCIX_32());
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_32_er_i()
    {
        unchecked
        {
            uint res = (DX |= OPER_I_32());
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_8_re_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_8();
            uint res = MASK_OUT_ABOVE_8((DX | m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_8_re_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_8();
            uint res = MASK_OUT_ABOVE_8((DX | m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_8_re_pi7()
    {
        unchecked
        {
            uint ea = EA_A7_PI_8();
            uint res = MASK_OUT_ABOVE_8((DX | m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_8_re_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_8();
            uint res = MASK_OUT_ABOVE_8((DX | m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_8_re_pd7()
    {
        unchecked
        {
            uint ea = EA_A7_PD_8();
            uint res = MASK_OUT_ABOVE_8((DX | m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_8_re_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_8();
            uint res = MASK_OUT_ABOVE_8((DX | m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_8_re_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_8();
            uint res = MASK_OUT_ABOVE_8((DX | m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_8_re_aw()
    {
        unchecked
        {
            uint ea = EA_AW_8();
            uint res = MASK_OUT_ABOVE_8((DX | m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_8_re_al()
    {
        unchecked
        {
            uint ea = EA_AL_8();
            uint res = MASK_OUT_ABOVE_8((DX | m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_16_re_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_16();
            uint res = MASK_OUT_ABOVE_16((DX | m68ki_read_16(ea)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_16_re_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_16();
            uint res = MASK_OUT_ABOVE_16((DX | m68ki_read_16(ea)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_16_re_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_16();
            uint res = MASK_OUT_ABOVE_16((DX | m68ki_read_16(ea)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_16_re_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_16();
            uint res = MASK_OUT_ABOVE_16((DX | m68ki_read_16(ea)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_16_re_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_16();
            uint res = MASK_OUT_ABOVE_16((DX | m68ki_read_16(ea)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_16_re_aw()
    {
        unchecked
        {
            uint ea = EA_AW_16();
            uint res = MASK_OUT_ABOVE_16((DX | m68ki_read_16(ea)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_16_re_al()
    {
        unchecked
        {
            uint ea = EA_AL_16();
            uint res = MASK_OUT_ABOVE_16((DX | m68ki_read_16(ea)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_32_re_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_32();
            uint res = (DX | m68ki_read_32(ea));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_32_re_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_32();
            uint res = (DX | m68ki_read_32(ea));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_32_re_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_32();
            uint res = (DX | m68ki_read_32(ea));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_32_re_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_32();
            uint res = (DX | m68ki_read_32(ea));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_32_re_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_32();
            uint res = (DX | m68ki_read_32(ea));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_32_re_aw()
    {
        unchecked
        {
            uint ea = EA_AW_32();
            uint res = (DX | m68ki_read_32(ea));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_or_32_re_al()
    {
        unchecked
        {
            uint ea = EA_AL_32();
            uint res = (DX | m68ki_read_32(ea));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ori_8_d()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_8((DY |= OPER_I_8()));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ori_8_ai()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AY_AI_8();
            uint res = MASK_OUT_ABOVE_8((src | m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ori_8_pi()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AY_PI_8();
            uint res = MASK_OUT_ABOVE_8((src | m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ori_8_pi7()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_A7_PI_8();
            uint res = MASK_OUT_ABOVE_8((src | m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ori_8_pd()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AY_PD_8();
            uint res = MASK_OUT_ABOVE_8((src | m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ori_8_pd7()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_A7_PD_8();
            uint res = MASK_OUT_ABOVE_8((src | m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ori_8_di()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AY_DI_8();
            uint res = MASK_OUT_ABOVE_8((src | m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ori_8_ix()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AY_IX_8();
            uint res = MASK_OUT_ABOVE_8((src | m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ori_8_aw()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AW_8();
            uint res = MASK_OUT_ABOVE_8((src | m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ori_8_al()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AL_8();
            uint res = MASK_OUT_ABOVE_8((src | m68ki_read_8(ea)));
            m68ki_write_8(ea, res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ori_16_d()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16((DY |= OPER_I_16()));
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ori_16_ai()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AY_AI_16();
            uint res = MASK_OUT_ABOVE_16((src | m68ki_read_16(ea)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ori_16_pi()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AY_PI_16();
            uint res = MASK_OUT_ABOVE_16((src | m68ki_read_16(ea)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ori_16_pd()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AY_PD_16();
            uint res = MASK_OUT_ABOVE_16((src | m68ki_read_16(ea)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ori_16_di()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AY_DI_16();
            uint res = MASK_OUT_ABOVE_16((src | m68ki_read_16(ea)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ori_16_ix()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AY_IX_16();
            uint res = MASK_OUT_ABOVE_16((src | m68ki_read_16(ea)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ori_16_aw()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AW_16();
            uint res = MASK_OUT_ABOVE_16((src | m68ki_read_16(ea)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ori_16_al()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AL_16();
            uint res = MASK_OUT_ABOVE_16((src | m68ki_read_16(ea)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ori_32_d()
    {
        unchecked
        {
            uint res = (DY |= OPER_I_32());
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ori_32_ai()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AY_AI_32();
            uint res = (src | m68ki_read_32(ea));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ori_32_pi()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AY_PI_32();
            uint res = (src | m68ki_read_32(ea));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ori_32_pd()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AY_PD_32();
            uint res = (src | m68ki_read_32(ea));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ori_32_di()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AY_DI_32();
            uint res = (src | m68ki_read_32(ea));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ori_32_ix()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AY_IX_32();
            uint res = (src | m68ki_read_32(ea));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ori_32_aw()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AW_32();
            uint res = (src | m68ki_read_32(ea));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ori_32_al()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AL_32();
            uint res = (src | m68ki_read_32(ea));
            m68ki_write_32(ea, res);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ori_16_toc()
    {
        unchecked
        {
            m68ki_set_ccr((m68ki_get_ccr() | OPER_I_16()));
        }
    }
    private void m68k_op_ori_16_tos()
    {
        unchecked
        {
            if ((FLAG_S != 0)) {
            uint src = OPER_I_16();
            m68ki_set_sr((m68ki_get_sr() | src));
            return;
            }
            m68ki_exception_privilege_violation();
        }
    }
    private void m68k_op_pea_32_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_32();
            m68ki_push_32(ea);
        }
    }
    private void m68k_op_pea_32_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_32();
            m68ki_push_32(ea);
        }
    }
    private void m68k_op_pea_32_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_32();
            m68ki_push_32(ea);
        }
    }
    private void m68k_op_pea_32_aw()
    {
        unchecked
        {
            uint ea = EA_AW_32();
            m68ki_push_32(ea);
        }
    }
    private void m68k_op_pea_32_al()
    {
        unchecked
        {
            uint ea = EA_AL_32();
            m68ki_push_32(ea);
        }
    }
    private void m68k_op_pea_32_pcdi()
    {
        unchecked
        {
            uint ea = EA_PCDI_32();
            m68ki_push_32(ea);
        }
    }
    private void m68k_op_pea_32_pcix()
    {
        unchecked
        {
            uint ea = EA_PCIX_32();
            m68ki_push_32(ea);
        }
    }
    private void m68k_op_reset()
    {
        unchecked
        {
            if ((FLAG_S != 0)) {
            USE_CYCLES(unchecked((long)(CYC_RESET)));
            _state.refresh_cycles += (128 * MUL);
            return;
            }
            m68ki_exception_privilege_violation();
        }
    }
    private void m68k_op_ror_8_s()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint orig_shift = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint shift = (orig_shift & unchecked((uint)(7)));
            uint src = MASK_OUT_ABOVE_8(r_dst);
            uint res = ROR_8(src, shift);
            if ((orig_shift != unchecked((uint)(0)))) USE_CYCLES(unchecked((long)((orig_shift * unchecked((uint)(CYC_SHIFT))))));
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = (src << unchecked((int)((unchecked((uint)(9)) - orig_shift))));
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ror_16_s()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint src = MASK_OUT_ABOVE_16(r_dst);
            uint res = ROR_16(src, shift);
            if ((shift != unchecked((uint)(0)))) USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = (src << unchecked((int)((unchecked((uint)(9)) - shift))));
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ror_32_s()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            ulong src = unchecked((ulong)(r_dst));
            uint res = ROR_32(unchecked((uint)(src)), shift);
            if ((shift != unchecked((uint)(0)))) USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            r_dst = res;
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = unchecked((uint)((src << unchecked((int)((unchecked((uint)(9)) - shift))))));
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ror_8_r()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint orig_shift = (DX & unchecked((uint)(63)));
            uint shift = (orig_shift & unchecked((uint)(7)));
            uint src = MASK_OUT_ABOVE_8(r_dst);
            uint res = ROR_8(src, shift);
            if ((orig_shift != unchecked((uint)(0)))) {
            USE_CYCLES(unchecked((long)((orig_shift * unchecked((uint)(CYC_SHIFT))))));
            SKIP_BUS_REFRESH();
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_C = (src << unchecked((int)((unchecked((uint)(8)) - ((shift - unchecked((uint)(1))) & unchecked((uint)(7)))))));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            FLAG_N = NFLAG_8(src);
            FLAG_Z = src;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ror_16_r()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint orig_shift = (DX & unchecked((uint)(63)));
            uint shift = (orig_shift & unchecked((uint)(15)));
            uint src = MASK_OUT_ABOVE_16(r_dst);
            uint res = ROR_16(src, shift);
            if ((orig_shift != unchecked((uint)(0)))) {
            USE_CYCLES(unchecked((long)((orig_shift * unchecked((uint)(CYC_SHIFT))))));
            SKIP_BUS_REFRESH();
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_C = ((src >> unchecked((int)(((shift - unchecked((uint)(1))) & unchecked((uint)(15)))))) << 8);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            FLAG_N = NFLAG_16(src);
            FLAG_Z = src;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ror_32_r()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint orig_shift = (DX & unchecked((uint)(63)));
            uint shift = (orig_shift & unchecked((uint)(31)));
            ulong src = unchecked((ulong)(r_dst));
            uint res = ROR_32(unchecked((uint)(src)), shift);
            if ((orig_shift != unchecked((uint)(0)))) {
            USE_CYCLES(unchecked((long)((orig_shift * unchecked((uint)(CYC_SHIFT))))));
            SKIP_BUS_REFRESH();
            r_dst = res;
            FLAG_C = unchecked((uint)(((src >> unchecked((int)(((shift - unchecked((uint)(1))) & unchecked((uint)(31)))))) << 8)));
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            FLAG_N = NFLAG_32(unchecked((uint)(src)));
            FLAG_Z = unchecked((uint)(src));
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ror_16_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_16();
            uint src = m68ki_read_16(ea);
            uint res = ROR_16(src, unchecked((uint)(1)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = (src << 8);
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ror_16_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_16();
            uint src = m68ki_read_16(ea);
            uint res = ROR_16(src, unchecked((uint)(1)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = (src << 8);
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ror_16_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_16();
            uint src = m68ki_read_16(ea);
            uint res = ROR_16(src, unchecked((uint)(1)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = (src << 8);
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ror_16_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_16();
            uint src = m68ki_read_16(ea);
            uint res = ROR_16(src, unchecked((uint)(1)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = (src << 8);
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ror_16_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_16();
            uint src = m68ki_read_16(ea);
            uint res = ROR_16(src, unchecked((uint)(1)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = (src << 8);
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ror_16_aw()
    {
        unchecked
        {
            uint ea = EA_AW_16();
            uint src = m68ki_read_16(ea);
            uint res = ROR_16(src, unchecked((uint)(1)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = (src << 8);
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_ror_16_al()
    {
        unchecked
        {
            uint ea = EA_AL_16();
            uint src = m68ki_read_16(ea);
            uint res = ROR_16(src, unchecked((uint)(1)));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = (src << 8);
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_rol_8_s()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint orig_shift = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint shift = (orig_shift & unchecked((uint)(7)));
            uint src = MASK_OUT_ABOVE_8(r_dst);
            uint res = ROL_8(src, shift);
            if ((orig_shift != unchecked((uint)(0)))) USE_CYCLES(unchecked((long)((orig_shift * unchecked((uint)(CYC_SHIFT))))));
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_C = (src << unchecked((int)(orig_shift)));
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_rol_16_s()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint src = MASK_OUT_ABOVE_16(r_dst);
            uint res = ROL_16(src, shift);
            if ((shift != unchecked((uint)(0)))) USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = (src >> unchecked((int)((unchecked((uint)(8)) - shift))));
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_rol_32_s()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            ulong src = unchecked((ulong)(r_dst));
            uint res = ROL_32(unchecked((uint)(src)), shift);
            if ((shift != unchecked((uint)(0)))) USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            r_dst = res;
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_C = unchecked((uint)((src >> unchecked((int)((unchecked((uint)(24)) - shift))))));
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_rol_8_r()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint orig_shift = (DX & unchecked((uint)(63)));
            uint shift = (orig_shift & unchecked((uint)(7)));
            uint src = MASK_OUT_ABOVE_8(r_dst);
            uint res = ROL_8(src, shift);
            if ((orig_shift != unchecked((uint)(0)))) {
            USE_CYCLES(unchecked((long)((orig_shift * unchecked((uint)(CYC_SHIFT))))));
            SKIP_BUS_REFRESH();
            if ((shift != unchecked((uint)(0)))) {
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_C = (src << unchecked((int)(shift)));
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            FLAG_C = ((src & unchecked((uint)(1))) << 8);
            FLAG_N = NFLAG_8(src);
            FLAG_Z = src;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            FLAG_N = NFLAG_8(src);
            FLAG_Z = src;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_rol_16_r()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint orig_shift = (DX & unchecked((uint)(63)));
            uint shift = (orig_shift & unchecked((uint)(15)));
            uint src = MASK_OUT_ABOVE_16(r_dst);
            uint res = MASK_OUT_ABOVE_16(ROL_16(src, shift));
            if ((orig_shift != unchecked((uint)(0)))) {
            USE_CYCLES(unchecked((long)((orig_shift * unchecked((uint)(CYC_SHIFT))))));
            SKIP_BUS_REFRESH();
            if ((shift != unchecked((uint)(0)))) {
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_C = ((src << unchecked((int)(shift))) >> 8);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            FLAG_C = ((src & unchecked((uint)(1))) << 8);
            FLAG_N = NFLAG_16(src);
            FLAG_Z = src;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            FLAG_N = NFLAG_16(src);
            FLAG_Z = src;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_rol_32_r()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint orig_shift = (DX & unchecked((uint)(63)));
            uint shift = (orig_shift & unchecked((uint)(31)));
            ulong src = unchecked((ulong)(r_dst));
            uint res = ROL_32(unchecked((uint)(src)), shift);
            if ((orig_shift != unchecked((uint)(0)))) {
            USE_CYCLES(unchecked((long)((orig_shift * unchecked((uint)(CYC_SHIFT))))));
            SKIP_BUS_REFRESH();
            r_dst = res;
            FLAG_C = unchecked((uint)(((src >> unchecked((int)(((unchecked((uint)(32)) - shift) & unchecked((uint)(31)))))) << 8)));
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            FLAG_C = CFLAG_CLEAR;
            FLAG_N = NFLAG_32(unchecked((uint)(src)));
            FLAG_Z = unchecked((uint)(src));
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_rol_16_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_16();
            uint src = m68ki_read_16(ea);
            uint res = MASK_OUT_ABOVE_16(ROL_16(src, unchecked((uint)(1))));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = (src >> 7);
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_rol_16_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_16();
            uint src = m68ki_read_16(ea);
            uint res = MASK_OUT_ABOVE_16(ROL_16(src, unchecked((uint)(1))));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = (src >> 7);
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_rol_16_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_16();
            uint src = m68ki_read_16(ea);
            uint res = MASK_OUT_ABOVE_16(ROL_16(src, unchecked((uint)(1))));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = (src >> 7);
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_rol_16_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_16();
            uint src = m68ki_read_16(ea);
            uint res = MASK_OUT_ABOVE_16(ROL_16(src, unchecked((uint)(1))));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = (src >> 7);
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_rol_16_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_16();
            uint src = m68ki_read_16(ea);
            uint res = MASK_OUT_ABOVE_16(ROL_16(src, unchecked((uint)(1))));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = (src >> 7);
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_rol_16_aw()
    {
        unchecked
        {
            uint ea = EA_AW_16();
            uint src = m68ki_read_16(ea);
            uint res = MASK_OUT_ABOVE_16(ROL_16(src, unchecked((uint)(1))));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = (src >> 7);
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_rol_16_al()
    {
        unchecked
        {
            uint ea = EA_AL_16();
            uint src = m68ki_read_16(ea);
            uint res = MASK_OUT_ABOVE_16(ROL_16(src, unchecked((uint)(1))));
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_C = (src >> 7);
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_roxr_8_s()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint src = MASK_OUT_ABOVE_8(r_dst);
            uint res = ROR_9((src | (XFLAG_AS_1() << 8)), shift);
            if ((shift != unchecked((uint)(0)))) USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            FLAG_C = (FLAG_X = res);
            res = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_roxr_16_s()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint src = MASK_OUT_ABOVE_16(r_dst);
            uint res = ROR_17((src | (XFLAG_AS_1() << 16)), shift);
            if ((shift != unchecked((uint)(0)))) USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            FLAG_C = (FLAG_X = (res >> 8));
            res = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_roxr_32_s()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint src = r_dst;
            uint res = MASK_OUT_ABOVE_32(((ROR_33(src, shift) & unchecked((uint)((~(1 << unchecked((int)((unchecked((uint)(32)) - shift)))))))) | (XFLAG_AS_1() << unchecked((int)((unchecked((uint)(32)) - shift))))));
            uint new_x_flag = (src & unchecked((uint)((1 << unchecked((int)((shift - unchecked((uint)(1)))))))));
            if ((shift != unchecked((uint)(0)))) USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            r_dst = res;
            FLAG_C = (FLAG_X = unchecked((uint)((((new_x_flag != unchecked((uint)(0))) ? (int)1 : (int)0) << 8))));
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_roxr_8_r()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint orig_shift = (DX & unchecked((uint)(63)));
            if ((orig_shift != unchecked((uint)(0)))) {
            uint shift = (orig_shift % unchecked((uint)(9)));
            uint src = MASK_OUT_ABOVE_8(r_dst);
            uint res = ROR_9((src | (XFLAG_AS_1() << 8)), shift);
            USE_CYCLES(unchecked((long)((orig_shift * unchecked((uint)(CYC_SHIFT))))));
            SKIP_BUS_REFRESH();
            FLAG_C = (FLAG_X = res);
            res = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            FLAG_C = FLAG_X;
            FLAG_N = NFLAG_8(r_dst);
            FLAG_Z = MASK_OUT_ABOVE_8(r_dst);
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_roxr_16_r()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint orig_shift = (DX & unchecked((uint)(63)));
            if ((orig_shift != unchecked((uint)(0)))) {
            uint shift = (orig_shift % unchecked((uint)(17)));
            uint src = MASK_OUT_ABOVE_16(r_dst);
            uint res = ROR_17((src | (XFLAG_AS_1() << 16)), shift);
            USE_CYCLES(unchecked((long)((orig_shift * unchecked((uint)(CYC_SHIFT))))));
            SKIP_BUS_REFRESH();
            FLAG_C = (FLAG_X = (res >> 8));
            res = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            FLAG_C = FLAG_X;
            FLAG_N = NFLAG_16(r_dst);
            FLAG_Z = MASK_OUT_ABOVE_16(r_dst);
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_roxr_32_r()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint orig_shift = (DX & unchecked((uint)(63)));
            uint shift = (orig_shift % unchecked((uint)(33)));
            uint src = r_dst;
            uint res = MASK_OUT_ABOVE_32(((ROR_33(src, shift) & unchecked((uint)((~(1 << unchecked((int)((unchecked((uint)(32)) - shift)))))))) | (XFLAG_AS_1() << unchecked((int)((unchecked((uint)(32)) - shift))))));
            uint new_x_flag = (src & unchecked((uint)((1 << unchecked((int)((shift - unchecked((uint)(1)))))))));
            if ((orig_shift != unchecked((uint)(0)))) {
            USE_CYCLES(unchecked((long)((orig_shift * unchecked((uint)(CYC_SHIFT))))));
            SKIP_BUS_REFRESH();
            }
            if ((shift != unchecked((uint)(0)))) {
            r_dst = res;
            FLAG_X = unchecked((uint)((((new_x_flag != unchecked((uint)(0))) ? (int)1 : (int)0) << 8)));
            } else res = src;
            FLAG_C = FLAG_X;
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_roxr_16_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_16();
            uint src = m68ki_read_16(ea);
            uint res = ROR_17((src | (XFLAG_AS_1() << 16)), unchecked((uint)(1)));
            FLAG_C = (FLAG_X = (res >> 8));
            res = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_roxr_16_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_16();
            uint src = m68ki_read_16(ea);
            uint res = ROR_17((src | (XFLAG_AS_1() << 16)), unchecked((uint)(1)));
            FLAG_C = (FLAG_X = (res >> 8));
            res = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_roxr_16_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_16();
            uint src = m68ki_read_16(ea);
            uint res = ROR_17((src | (XFLAG_AS_1() << 16)), unchecked((uint)(1)));
            FLAG_C = (FLAG_X = (res >> 8));
            res = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_roxr_16_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_16();
            uint src = m68ki_read_16(ea);
            uint res = ROR_17((src | (XFLAG_AS_1() << 16)), unchecked((uint)(1)));
            FLAG_C = (FLAG_X = (res >> 8));
            res = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_roxr_16_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_16();
            uint src = m68ki_read_16(ea);
            uint res = ROR_17((src | (XFLAG_AS_1() << 16)), unchecked((uint)(1)));
            FLAG_C = (FLAG_X = (res >> 8));
            res = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_roxr_16_aw()
    {
        unchecked
        {
            uint ea = EA_AW_16();
            uint src = m68ki_read_16(ea);
            uint res = ROR_17((src | (XFLAG_AS_1() << 16)), unchecked((uint)(1)));
            FLAG_C = (FLAG_X = (res >> 8));
            res = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_roxr_16_al()
    {
        unchecked
        {
            uint ea = EA_AL_16();
            uint src = m68ki_read_16(ea);
            uint res = ROR_17((src | (XFLAG_AS_1() << 16)), unchecked((uint)(1)));
            FLAG_C = (FLAG_X = (res >> 8));
            res = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_roxl_8_s()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint src = MASK_OUT_ABOVE_8(r_dst);
            uint res = ROL_9((src | (XFLAG_AS_1() << 8)), shift);
            if ((shift != unchecked((uint)(0)))) USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            FLAG_C = (FLAG_X = res);
            res = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_roxl_16_s()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint src = MASK_OUT_ABOVE_16(r_dst);
            uint res = ROL_17((src | (XFLAG_AS_1() << 16)), shift);
            if ((shift != unchecked((uint)(0)))) USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            FLAG_C = (FLAG_X = (res >> 8));
            res = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_roxl_32_s()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint shift = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint src = r_dst;
            uint res = MASK_OUT_ABOVE_32(((ROL_33(src, shift) & unchecked((uint)((~(1 << unchecked((int)((shift - unchecked((uint)(1)))))))))) | (XFLAG_AS_1() << unchecked((int)((shift - unchecked((uint)(1))))))));
            uint new_x_flag = (src & unchecked((uint)((1 << unchecked((int)((unchecked((uint)(32)) - shift)))))));
            if ((shift != unchecked((uint)(0)))) USE_CYCLES(unchecked((long)((shift * unchecked((uint)(CYC_SHIFT))))));
            r_dst = res;
            FLAG_C = (FLAG_X = unchecked((uint)((((new_x_flag != unchecked((uint)(0))) ? (int)1 : (int)0) << 8))));
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_roxl_8_r()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint orig_shift = (DX & unchecked((uint)(63)));
            if ((orig_shift != unchecked((uint)(0)))) {
            uint shift = (orig_shift % unchecked((uint)(9)));
            uint src = MASK_OUT_ABOVE_8(r_dst);
            uint res = ROL_9((src | (XFLAG_AS_1() << 8)), shift);
            USE_CYCLES(unchecked((long)((orig_shift * unchecked((uint)(CYC_SHIFT))))));
            SKIP_BUS_REFRESH();
            FLAG_C = (FLAG_X = res);
            res = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            FLAG_C = FLAG_X;
            FLAG_N = NFLAG_8(r_dst);
            FLAG_Z = MASK_OUT_ABOVE_8(r_dst);
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_roxl_16_r()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint orig_shift = (DX & unchecked((uint)(63)));
            if ((orig_shift != unchecked((uint)(0)))) {
            uint shift = (orig_shift % unchecked((uint)(17)));
            uint src = MASK_OUT_ABOVE_16(r_dst);
            uint res = ROL_17((src | (XFLAG_AS_1() << 16)), shift);
            USE_CYCLES(unchecked((long)((orig_shift * unchecked((uint)(CYC_SHIFT))))));
            SKIP_BUS_REFRESH();
            FLAG_C = (FLAG_X = (res >> 8));
            res = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            return;
            }
            FLAG_C = FLAG_X;
            FLAG_N = NFLAG_16(r_dst);
            FLAG_Z = MASK_OUT_ABOVE_16(r_dst);
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_roxl_32_r()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint orig_shift = (DX & unchecked((uint)(63)));
            uint shift = (orig_shift % unchecked((uint)(33)));
            uint src = r_dst;
            uint res = MASK_OUT_ABOVE_32(((ROL_33(src, shift) & unchecked((uint)((~(1 << unchecked((int)((shift - unchecked((uint)(1)))))))))) | (XFLAG_AS_1() << unchecked((int)((shift - unchecked((uint)(1))))))));
            uint new_x_flag = (src & unchecked((uint)((1 << unchecked((int)((unchecked((uint)(32)) - shift)))))));
            if ((orig_shift != unchecked((uint)(0)))) {
            USE_CYCLES(unchecked((long)((orig_shift * unchecked((uint)(CYC_SHIFT))))));
            SKIP_BUS_REFRESH();
            }
            if ((shift != unchecked((uint)(0)))) {
            r_dst = res;
            FLAG_X = unchecked((uint)((((new_x_flag != unchecked((uint)(0))) ? (int)1 : (int)0) << 8)));
            } else res = src;
            FLAG_C = FLAG_X;
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_roxl_16_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_16();
            uint src = m68ki_read_16(ea);
            uint res = ROL_17((src | (XFLAG_AS_1() << 16)), unchecked((uint)(1)));
            FLAG_C = (FLAG_X = (res >> 8));
            res = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_roxl_16_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_16();
            uint src = m68ki_read_16(ea);
            uint res = ROL_17((src | (XFLAG_AS_1() << 16)), unchecked((uint)(1)));
            FLAG_C = (FLAG_X = (res >> 8));
            res = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_roxl_16_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_16();
            uint src = m68ki_read_16(ea);
            uint res = ROL_17((src | (XFLAG_AS_1() << 16)), unchecked((uint)(1)));
            FLAG_C = (FLAG_X = (res >> 8));
            res = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_roxl_16_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_16();
            uint src = m68ki_read_16(ea);
            uint res = ROL_17((src | (XFLAG_AS_1() << 16)), unchecked((uint)(1)));
            FLAG_C = (FLAG_X = (res >> 8));
            res = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_roxl_16_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_16();
            uint src = m68ki_read_16(ea);
            uint res = ROL_17((src | (XFLAG_AS_1() << 16)), unchecked((uint)(1)));
            FLAG_C = (FLAG_X = (res >> 8));
            res = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_roxl_16_aw()
    {
        unchecked
        {
            uint ea = EA_AW_16();
            uint src = m68ki_read_16(ea);
            uint res = ROL_17((src | (XFLAG_AS_1() << 16)), unchecked((uint)(1)));
            FLAG_C = (FLAG_X = (res >> 8));
            res = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_roxl_16_al()
    {
        unchecked
        {
            uint ea = EA_AL_16();
            uint src = m68ki_read_16(ea);
            uint res = ROL_17((src | (XFLAG_AS_1() << 16)), unchecked((uint)(1)));
            FLAG_C = (FLAG_X = (res >> 8));
            res = MASK_OUT_ABOVE_16(res);
            m68ki_write_16(ea, res);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_rte_32()
    {
        unchecked
        {
            if ((FLAG_S != 0)) {
            uint new_sr = default;
            uint new_pc = default;
            new_sr = m68ki_pull_16();
            new_pc = m68ki_pull_32();
            m68ki_jump(new_pc);
            m68ki_set_sr(new_sr);
            if (_addressErrors) CPU_INSTR_MODE = INSTRUCTION_YES;
            CPU_RUN_MODE = RUN_MODE_NORMAL;
            return;
            }
            m68ki_exception_privilege_violation();
        }
    }
    private void m68k_op_rtr_32()
    {
        unchecked
        {
            m68ki_set_ccr(m68ki_pull_16());
            m68ki_jump(m68ki_pull_32());
        }
    }
    private void m68k_op_rts_32()
    {
        unchecked
        {
            m68ki_jump(m68ki_pull_32());
        }
    }
    private void m68k_op_sbcd_8_rr()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = DY;
            uint dst = r_dst;
            uint res = ((LOW_NIBBLE(dst) - LOW_NIBBLE(src)) - XFLAG_AS_1());
            uint corf = unchecked((uint)(0));
            if ((res > unchecked((uint)(15)))) corf = unchecked((uint)(6));
            res += (HIGH_NIBBLE(dst) - HIGH_NIBBLE(src));
            FLAG_V = res;
            if ((res > unchecked((uint)(255)))) {
            res += unchecked((uint)(160));
            FLAG_X = (FLAG_C = CFLAG_SET);
            } else if ((res < corf)) FLAG_X = (FLAG_C = CFLAG_SET); else FLAG_X = (FLAG_C = unchecked((uint)(0)));
            res = MASK_OUT_ABOVE_8((res - corf));
            FLAG_V &= (~res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z |= res;
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
        }
    }
    private void m68k_op_sbcd_8_mm_ax7()
    {
        unchecked
        {
            uint src = OPER_AY_PD_8();
            uint ea = EA_A7_PD_8();
            uint dst = m68ki_read_8(ea);
            uint res = ((LOW_NIBBLE(dst) - LOW_NIBBLE(src)) - XFLAG_AS_1());
            uint corf = unchecked((uint)(0));
            if ((res > unchecked((uint)(15)))) corf = unchecked((uint)(6));
            res += (HIGH_NIBBLE(dst) - HIGH_NIBBLE(src));
            FLAG_V = res;
            if ((res > unchecked((uint)(255)))) {
            res += unchecked((uint)(160));
            FLAG_X = (FLAG_C = CFLAG_SET);
            } else if ((res < corf)) FLAG_X = (FLAG_C = CFLAG_SET); else FLAG_X = (FLAG_C = unchecked((uint)(0)));
            res = MASK_OUT_ABOVE_8((res - corf));
            FLAG_V &= (~res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z |= res;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_sbcd_8_mm_ay7()
    {
        unchecked
        {
            uint src = OPER_A7_PD_8();
            uint ea = EA_AX_PD_8();
            uint dst = m68ki_read_8(ea);
            uint res = ((LOW_NIBBLE(dst) - LOW_NIBBLE(src)) - XFLAG_AS_1());
            uint corf = unchecked((uint)(0));
            if ((res > unchecked((uint)(15)))) corf = unchecked((uint)(6));
            res += (HIGH_NIBBLE(dst) - HIGH_NIBBLE(src));
            FLAG_V = res;
            if ((res > unchecked((uint)(255)))) {
            res += unchecked((uint)(160));
            FLAG_X = (FLAG_C = CFLAG_SET);
            } else if ((res < corf)) FLAG_X = (FLAG_C = CFLAG_SET); else FLAG_X = (FLAG_C = unchecked((uint)(0)));
            res = MASK_OUT_ABOVE_8((res - corf));
            FLAG_V &= (~res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z |= res;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_sbcd_8_mm_axy7()
    {
        unchecked
        {
            uint src = OPER_A7_PD_8();
            uint ea = EA_A7_PD_8();
            uint dst = m68ki_read_8(ea);
            uint res = ((LOW_NIBBLE(dst) - LOW_NIBBLE(src)) - XFLAG_AS_1());
            uint corf = unchecked((uint)(0));
            if ((res > unchecked((uint)(15)))) corf = unchecked((uint)(6));
            res += (HIGH_NIBBLE(dst) - HIGH_NIBBLE(src));
            FLAG_V = res;
            if ((res > unchecked((uint)(255)))) {
            res += unchecked((uint)(160));
            FLAG_X = (FLAG_C = CFLAG_SET);
            } else if ((res < corf)) FLAG_X = (FLAG_C = CFLAG_SET); else FLAG_X = (FLAG_C = unchecked((uint)(0)));
            res = MASK_OUT_ABOVE_8((res - corf));
            FLAG_V &= (~res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z |= res;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_sbcd_8_mm()
    {
        unchecked
        {
            uint src = OPER_AY_PD_8();
            uint ea = EA_AX_PD_8();
            uint dst = m68ki_read_8(ea);
            uint res = ((LOW_NIBBLE(dst) - LOW_NIBBLE(src)) - XFLAG_AS_1());
            uint corf = unchecked((uint)(0));
            if ((res > unchecked((uint)(15)))) corf = unchecked((uint)(6));
            res += (HIGH_NIBBLE(dst) - HIGH_NIBBLE(src));
            FLAG_V = res;
            if ((res > unchecked((uint)(255)))) {
            res += unchecked((uint)(160));
            FLAG_X = (FLAG_C = CFLAG_SET);
            } else if ((res < corf)) FLAG_X = (FLAG_C = CFLAG_SET); else FLAG_X = (FLAG_C = unchecked((uint)(0)));
            res = MASK_OUT_ABOVE_8((res - corf));
            FLAG_V &= (~res);
            FLAG_N = NFLAG_8(res);
            FLAG_Z |= res;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_st_8_d()
    {
        unchecked
        {
            DY |= unchecked((uint)(255));
        }
    }
    private void m68k_op_st_8_ai()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_AI_8(), unchecked((uint)(255)));
        }
    }
    private void m68k_op_st_8_pi()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PI_8(), unchecked((uint)(255)));
        }
    }
    private void m68k_op_st_8_pi7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PI_8(), unchecked((uint)(255)));
        }
    }
    private void m68k_op_st_8_pd()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PD_8(), unchecked((uint)(255)));
        }
    }
    private void m68k_op_st_8_pd7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PD_8(), unchecked((uint)(255)));
        }
    }
    private void m68k_op_st_8_di()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_DI_8(), unchecked((uint)(255)));
        }
    }
    private void m68k_op_st_8_ix()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_IX_8(), unchecked((uint)(255)));
        }
    }
    private void m68k_op_st_8_aw()
    {
        unchecked
        {
            m68ki_write_8(EA_AW_8(), unchecked((uint)(255)));
        }
    }
    private void m68k_op_st_8_al()
    {
        unchecked
        {
            m68ki_write_8(EA_AL_8(), unchecked((uint)(255)));
        }
    }
    private void m68k_op_sf_8_d()
    {
        unchecked
        {
            DY &= 4294967040u;
        }
    }
    private void m68k_op_sf_8_ai()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_AI_8(), unchecked((uint)(0)));
        }
    }
    private void m68k_op_sf_8_pi()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PI_8(), unchecked((uint)(0)));
        }
    }
    private void m68k_op_sf_8_pi7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PI_8(), unchecked((uint)(0)));
        }
    }
    private void m68k_op_sf_8_pd()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PD_8(), unchecked((uint)(0)));
        }
    }
    private void m68k_op_sf_8_pd7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PD_8(), unchecked((uint)(0)));
        }
    }
    private void m68k_op_sf_8_di()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_DI_8(), unchecked((uint)(0)));
        }
    }
    private void m68k_op_sf_8_ix()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_IX_8(), unchecked((uint)(0)));
        }
    }
    private void m68k_op_sf_8_aw()
    {
        unchecked
        {
            m68ki_write_8(EA_AW_8(), unchecked((uint)(0)));
        }
    }
    private void m68k_op_sf_8_al()
    {
        unchecked
        {
            m68ki_write_8(EA_AL_8(), unchecked((uint)(0)));
        }
    }
    private void m68k_op_shi_8_d()
    {
        unchecked
        {
            if ((COND_HI() != 0)) {
            DY |= unchecked((uint)(255));
            USE_CYCLES(unchecked((long)(CYC_SCC_R_TRUE)));
            return;
            }
            DY &= 4294967040u;
        }
    }
    private void m68k_op_sls_8_d()
    {
        unchecked
        {
            if ((COND_LS() != 0)) {
            DY |= unchecked((uint)(255));
            USE_CYCLES(unchecked((long)(CYC_SCC_R_TRUE)));
            return;
            }
            DY &= 4294967040u;
        }
    }
    private void m68k_op_scc_8_d()
    {
        unchecked
        {
            if ((COND_CC() != 0)) {
            DY |= unchecked((uint)(255));
            USE_CYCLES(unchecked((long)(CYC_SCC_R_TRUE)));
            return;
            }
            DY &= 4294967040u;
        }
    }
    private void m68k_op_scs_8_d()
    {
        unchecked
        {
            if ((COND_CS() != 0)) {
            DY |= unchecked((uint)(255));
            USE_CYCLES(unchecked((long)(CYC_SCC_R_TRUE)));
            return;
            }
            DY &= 4294967040u;
        }
    }
    private void m68k_op_sne_8_d()
    {
        unchecked
        {
            if ((COND_NE() != 0)) {
            DY |= unchecked((uint)(255));
            USE_CYCLES(unchecked((long)(CYC_SCC_R_TRUE)));
            return;
            }
            DY &= 4294967040u;
        }
    }
    private void m68k_op_seq_8_d()
    {
        unchecked
        {
            if ((COND_EQ() != 0)) {
            DY |= unchecked((uint)(255));
            USE_CYCLES(unchecked((long)(CYC_SCC_R_TRUE)));
            return;
            }
            DY &= 4294967040u;
        }
    }
    private void m68k_op_svc_8_d()
    {
        unchecked
        {
            if ((COND_VC() != 0)) {
            DY |= unchecked((uint)(255));
            USE_CYCLES(unchecked((long)(CYC_SCC_R_TRUE)));
            return;
            }
            DY &= 4294967040u;
        }
    }
    private void m68k_op_svs_8_d()
    {
        unchecked
        {
            if ((COND_VS() != 0)) {
            DY |= unchecked((uint)(255));
            USE_CYCLES(unchecked((long)(CYC_SCC_R_TRUE)));
            return;
            }
            DY &= 4294967040u;
        }
    }
    private void m68k_op_spl_8_d()
    {
        unchecked
        {
            if ((COND_PL() != 0)) {
            DY |= unchecked((uint)(255));
            USE_CYCLES(unchecked((long)(CYC_SCC_R_TRUE)));
            return;
            }
            DY &= 4294967040u;
        }
    }
    private void m68k_op_smi_8_d()
    {
        unchecked
        {
            if ((COND_MI() != 0)) {
            DY |= unchecked((uint)(255));
            USE_CYCLES(unchecked((long)(CYC_SCC_R_TRUE)));
            return;
            }
            DY &= 4294967040u;
        }
    }
    private void m68k_op_sge_8_d()
    {
        unchecked
        {
            if ((COND_GE() != 0)) {
            DY |= unchecked((uint)(255));
            USE_CYCLES(unchecked((long)(CYC_SCC_R_TRUE)));
            return;
            }
            DY &= 4294967040u;
        }
    }
    private void m68k_op_slt_8_d()
    {
        unchecked
        {
            if ((COND_LT() != 0)) {
            DY |= unchecked((uint)(255));
            USE_CYCLES(unchecked((long)(CYC_SCC_R_TRUE)));
            return;
            }
            DY &= 4294967040u;
        }
    }
    private void m68k_op_sgt_8_d()
    {
        unchecked
        {
            if ((COND_GT() != 0)) {
            DY |= unchecked((uint)(255));
            USE_CYCLES(unchecked((long)(CYC_SCC_R_TRUE)));
            return;
            }
            DY &= 4294967040u;
        }
    }
    private void m68k_op_sle_8_d()
    {
        unchecked
        {
            if ((COND_LE() != 0)) {
            DY |= unchecked((uint)(255));
            USE_CYCLES(unchecked((long)(CYC_SCC_R_TRUE)));
            return;
            }
            DY &= 4294967040u;
        }
    }
    private void m68k_op_shi_8_ai()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_AI_8(), unchecked((uint)(((COND_HI() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_shi_8_pi()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PI_8(), unchecked((uint)(((COND_HI() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_shi_8_pi7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PI_8(), unchecked((uint)(((COND_HI() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_shi_8_pd()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PD_8(), unchecked((uint)(((COND_HI() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_shi_8_pd7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PD_8(), unchecked((uint)(((COND_HI() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_shi_8_di()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_DI_8(), unchecked((uint)(((COND_HI() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_shi_8_ix()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_IX_8(), unchecked((uint)(((COND_HI() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_shi_8_aw()
    {
        unchecked
        {
            m68ki_write_8(EA_AW_8(), unchecked((uint)(((COND_HI() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_shi_8_al()
    {
        unchecked
        {
            m68ki_write_8(EA_AL_8(), unchecked((uint)(((COND_HI() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sls_8_ai()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_AI_8(), unchecked((uint)(((COND_LS() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sls_8_pi()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PI_8(), unchecked((uint)(((COND_LS() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sls_8_pi7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PI_8(), unchecked((uint)(((COND_LS() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sls_8_pd()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PD_8(), unchecked((uint)(((COND_LS() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sls_8_pd7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PD_8(), unchecked((uint)(((COND_LS() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sls_8_di()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_DI_8(), unchecked((uint)(((COND_LS() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sls_8_ix()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_IX_8(), unchecked((uint)(((COND_LS() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sls_8_aw()
    {
        unchecked
        {
            m68ki_write_8(EA_AW_8(), unchecked((uint)(((COND_LS() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sls_8_al()
    {
        unchecked
        {
            m68ki_write_8(EA_AL_8(), unchecked((uint)(((COND_LS() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_scc_8_ai()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_AI_8(), unchecked((uint)(((COND_CC() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_scc_8_pi()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PI_8(), unchecked((uint)(((COND_CC() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_scc_8_pi7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PI_8(), unchecked((uint)(((COND_CC() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_scc_8_pd()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PD_8(), unchecked((uint)(((COND_CC() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_scc_8_pd7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PD_8(), unchecked((uint)(((COND_CC() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_scc_8_di()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_DI_8(), unchecked((uint)(((COND_CC() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_scc_8_ix()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_IX_8(), unchecked((uint)(((COND_CC() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_scc_8_aw()
    {
        unchecked
        {
            m68ki_write_8(EA_AW_8(), unchecked((uint)(((COND_CC() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_scc_8_al()
    {
        unchecked
        {
            m68ki_write_8(EA_AL_8(), unchecked((uint)(((COND_CC() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_scs_8_ai()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_AI_8(), unchecked((uint)(((COND_CS() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_scs_8_pi()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PI_8(), unchecked((uint)(((COND_CS() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_scs_8_pi7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PI_8(), unchecked((uint)(((COND_CS() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_scs_8_pd()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PD_8(), unchecked((uint)(((COND_CS() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_scs_8_pd7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PD_8(), unchecked((uint)(((COND_CS() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_scs_8_di()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_DI_8(), unchecked((uint)(((COND_CS() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_scs_8_ix()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_IX_8(), unchecked((uint)(((COND_CS() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_scs_8_aw()
    {
        unchecked
        {
            m68ki_write_8(EA_AW_8(), unchecked((uint)(((COND_CS() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_scs_8_al()
    {
        unchecked
        {
            m68ki_write_8(EA_AL_8(), unchecked((uint)(((COND_CS() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sne_8_ai()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_AI_8(), unchecked((uint)(((COND_NE() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sne_8_pi()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PI_8(), unchecked((uint)(((COND_NE() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sne_8_pi7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PI_8(), unchecked((uint)(((COND_NE() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sne_8_pd()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PD_8(), unchecked((uint)(((COND_NE() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sne_8_pd7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PD_8(), unchecked((uint)(((COND_NE() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sne_8_di()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_DI_8(), unchecked((uint)(((COND_NE() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sne_8_ix()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_IX_8(), unchecked((uint)(((COND_NE() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sne_8_aw()
    {
        unchecked
        {
            m68ki_write_8(EA_AW_8(), unchecked((uint)(((COND_NE() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sne_8_al()
    {
        unchecked
        {
            m68ki_write_8(EA_AL_8(), unchecked((uint)(((COND_NE() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_seq_8_ai()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_AI_8(), unchecked((uint)(((COND_EQ() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_seq_8_pi()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PI_8(), unchecked((uint)(((COND_EQ() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_seq_8_pi7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PI_8(), unchecked((uint)(((COND_EQ() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_seq_8_pd()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PD_8(), unchecked((uint)(((COND_EQ() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_seq_8_pd7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PD_8(), unchecked((uint)(((COND_EQ() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_seq_8_di()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_DI_8(), unchecked((uint)(((COND_EQ() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_seq_8_ix()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_IX_8(), unchecked((uint)(((COND_EQ() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_seq_8_aw()
    {
        unchecked
        {
            m68ki_write_8(EA_AW_8(), unchecked((uint)(((COND_EQ() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_seq_8_al()
    {
        unchecked
        {
            m68ki_write_8(EA_AL_8(), unchecked((uint)(((COND_EQ() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_svc_8_ai()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_AI_8(), unchecked((uint)(((COND_VC() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_svc_8_pi()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PI_8(), unchecked((uint)(((COND_VC() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_svc_8_pi7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PI_8(), unchecked((uint)(((COND_VC() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_svc_8_pd()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PD_8(), unchecked((uint)(((COND_VC() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_svc_8_pd7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PD_8(), unchecked((uint)(((COND_VC() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_svc_8_di()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_DI_8(), unchecked((uint)(((COND_VC() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_svc_8_ix()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_IX_8(), unchecked((uint)(((COND_VC() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_svc_8_aw()
    {
        unchecked
        {
            m68ki_write_8(EA_AW_8(), unchecked((uint)(((COND_VC() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_svc_8_al()
    {
        unchecked
        {
            m68ki_write_8(EA_AL_8(), unchecked((uint)(((COND_VC() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_svs_8_ai()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_AI_8(), unchecked((uint)(((COND_VS() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_svs_8_pi()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PI_8(), unchecked((uint)(((COND_VS() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_svs_8_pi7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PI_8(), unchecked((uint)(((COND_VS() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_svs_8_pd()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PD_8(), unchecked((uint)(((COND_VS() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_svs_8_pd7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PD_8(), unchecked((uint)(((COND_VS() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_svs_8_di()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_DI_8(), unchecked((uint)(((COND_VS() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_svs_8_ix()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_IX_8(), unchecked((uint)(((COND_VS() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_svs_8_aw()
    {
        unchecked
        {
            m68ki_write_8(EA_AW_8(), unchecked((uint)(((COND_VS() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_svs_8_al()
    {
        unchecked
        {
            m68ki_write_8(EA_AL_8(), unchecked((uint)(((COND_VS() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_spl_8_ai()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_AI_8(), unchecked((uint)(((COND_PL() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_spl_8_pi()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PI_8(), unchecked((uint)(((COND_PL() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_spl_8_pi7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PI_8(), unchecked((uint)(((COND_PL() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_spl_8_pd()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PD_8(), unchecked((uint)(((COND_PL() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_spl_8_pd7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PD_8(), unchecked((uint)(((COND_PL() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_spl_8_di()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_DI_8(), unchecked((uint)(((COND_PL() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_spl_8_ix()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_IX_8(), unchecked((uint)(((COND_PL() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_spl_8_aw()
    {
        unchecked
        {
            m68ki_write_8(EA_AW_8(), unchecked((uint)(((COND_PL() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_spl_8_al()
    {
        unchecked
        {
            m68ki_write_8(EA_AL_8(), unchecked((uint)(((COND_PL() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_smi_8_ai()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_AI_8(), unchecked((uint)(((COND_MI() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_smi_8_pi()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PI_8(), unchecked((uint)(((COND_MI() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_smi_8_pi7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PI_8(), unchecked((uint)(((COND_MI() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_smi_8_pd()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PD_8(), unchecked((uint)(((COND_MI() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_smi_8_pd7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PD_8(), unchecked((uint)(((COND_MI() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_smi_8_di()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_DI_8(), unchecked((uint)(((COND_MI() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_smi_8_ix()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_IX_8(), unchecked((uint)(((COND_MI() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_smi_8_aw()
    {
        unchecked
        {
            m68ki_write_8(EA_AW_8(), unchecked((uint)(((COND_MI() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_smi_8_al()
    {
        unchecked
        {
            m68ki_write_8(EA_AL_8(), unchecked((uint)(((COND_MI() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sge_8_ai()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_AI_8(), unchecked((uint)(((COND_GE() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sge_8_pi()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PI_8(), unchecked((uint)(((COND_GE() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sge_8_pi7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PI_8(), unchecked((uint)(((COND_GE() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sge_8_pd()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PD_8(), unchecked((uint)(((COND_GE() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sge_8_pd7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PD_8(), unchecked((uint)(((COND_GE() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sge_8_di()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_DI_8(), unchecked((uint)(((COND_GE() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sge_8_ix()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_IX_8(), unchecked((uint)(((COND_GE() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sge_8_aw()
    {
        unchecked
        {
            m68ki_write_8(EA_AW_8(), unchecked((uint)(((COND_GE() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sge_8_al()
    {
        unchecked
        {
            m68ki_write_8(EA_AL_8(), unchecked((uint)(((COND_GE() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_slt_8_ai()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_AI_8(), unchecked((uint)(((COND_LT() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_slt_8_pi()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PI_8(), unchecked((uint)(((COND_LT() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_slt_8_pi7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PI_8(), unchecked((uint)(((COND_LT() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_slt_8_pd()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PD_8(), unchecked((uint)(((COND_LT() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_slt_8_pd7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PD_8(), unchecked((uint)(((COND_LT() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_slt_8_di()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_DI_8(), unchecked((uint)(((COND_LT() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_slt_8_ix()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_IX_8(), unchecked((uint)(((COND_LT() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_slt_8_aw()
    {
        unchecked
        {
            m68ki_write_8(EA_AW_8(), unchecked((uint)(((COND_LT() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_slt_8_al()
    {
        unchecked
        {
            m68ki_write_8(EA_AL_8(), unchecked((uint)(((COND_LT() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sgt_8_ai()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_AI_8(), unchecked((uint)(((COND_GT() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sgt_8_pi()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PI_8(), unchecked((uint)(((COND_GT() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sgt_8_pi7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PI_8(), unchecked((uint)(((COND_GT() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sgt_8_pd()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PD_8(), unchecked((uint)(((COND_GT() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sgt_8_pd7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PD_8(), unchecked((uint)(((COND_GT() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sgt_8_di()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_DI_8(), unchecked((uint)(((COND_GT() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sgt_8_ix()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_IX_8(), unchecked((uint)(((COND_GT() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sgt_8_aw()
    {
        unchecked
        {
            m68ki_write_8(EA_AW_8(), unchecked((uint)(((COND_GT() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sgt_8_al()
    {
        unchecked
        {
            m68ki_write_8(EA_AL_8(), unchecked((uint)(((COND_GT() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sle_8_ai()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_AI_8(), unchecked((uint)(((COND_LE() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sle_8_pi()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PI_8(), unchecked((uint)(((COND_LE() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sle_8_pi7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PI_8(), unchecked((uint)(((COND_LE() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sle_8_pd()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_PD_8(), unchecked((uint)(((COND_LE() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sle_8_pd7()
    {
        unchecked
        {
            m68ki_write_8(EA_A7_PD_8(), unchecked((uint)(((COND_LE() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sle_8_di()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_DI_8(), unchecked((uint)(((COND_LE() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sle_8_ix()
    {
        unchecked
        {
            m68ki_write_8(EA_AY_IX_8(), unchecked((uint)(((COND_LE() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sle_8_aw()
    {
        unchecked
        {
            m68ki_write_8(EA_AW_8(), unchecked((uint)(((COND_LE() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_sle_8_al()
    {
        unchecked
        {
            m68ki_write_8(EA_AL_8(), unchecked((uint)(((COND_LE() != 0) ? 255 : 0))));
        }
    }
    private void m68k_op_stop()
    {
        unchecked
        {
            if ((FLAG_S != 0)) {
            uint new_sr = OPER_I_16();
            CPU_STOPPED |= STOP_LEVEL_STOP;
            m68ki_set_sr(new_sr);
            if ((CPU_STOPPED != 0)) {
            SET_CYCLES((_state.cycle_end - unchecked((uint)((4 * MUL)))));
            }
            return;
            }
            m68ki_exception_privilege_violation();
        }
    }
    private void m68k_op_sub_8_er_d()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = MASK_OUT_ABOVE_8(DY);
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_sub_8_er_ai()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_AI_8();
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_sub_8_er_pi()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_PI_8();
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_sub_8_er_pi7()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_A7_PI_8();
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_sub_8_er_pd()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_PD_8();
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_sub_8_er_pd7()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_A7_PD_8();
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_sub_8_er_di()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_DI_8();
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_sub_8_er_ix()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_IX_8();
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_sub_8_er_aw()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AW_8();
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_sub_8_er_al()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AL_8();
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_sub_8_er_pcdi()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_PCDI_8();
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_sub_8_er_pcix()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_PCIX_8();
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_sub_8_er_i()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_I_8();
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_sub_16_er_d()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = MASK_OUT_ABOVE_16(DY);
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_sub_16_er_a()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = MASK_OUT_ABOVE_16(AY);
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_sub_16_er_ai()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_AI_16();
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_sub_16_er_pi()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_PI_16();
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_sub_16_er_pd()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_PD_16();
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_sub_16_er_di()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_DI_16();
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_sub_16_er_ix()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_IX_16();
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_sub_16_er_aw()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AW_16();
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_sub_16_er_al()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AL_16();
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_sub_16_er_pcdi()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_PCDI_16();
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_sub_16_er_pcix()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_PCIX_16();
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_sub_16_er_i()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_I_16();
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_sub_32_er_d()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = DY;
            uint dst = r_dst;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            r_dst = FLAG_Z;
        }
    }
    private void m68k_op_sub_32_er_a()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = AY;
            uint dst = r_dst;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            r_dst = FLAG_Z;
        }
    }
    private void m68k_op_sub_32_er_ai()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_AI_32();
            uint dst = r_dst;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            r_dst = FLAG_Z;
        }
    }
    private void m68k_op_sub_32_er_pi()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_PI_32();
            uint dst = r_dst;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            r_dst = FLAG_Z;
        }
    }
    private void m68k_op_sub_32_er_pd()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_PD_32();
            uint dst = r_dst;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            r_dst = FLAG_Z;
        }
    }
    private void m68k_op_sub_32_er_di()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_DI_32();
            uint dst = r_dst;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            r_dst = FLAG_Z;
        }
    }
    private void m68k_op_sub_32_er_ix()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AY_IX_32();
            uint dst = r_dst;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            r_dst = FLAG_Z;
        }
    }
    private void m68k_op_sub_32_er_aw()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AW_32();
            uint dst = r_dst;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            r_dst = FLAG_Z;
        }
    }
    private void m68k_op_sub_32_er_al()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_AL_32();
            uint dst = r_dst;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            r_dst = FLAG_Z;
        }
    }
    private void m68k_op_sub_32_er_pcdi()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_PCDI_32();
            uint dst = r_dst;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            r_dst = FLAG_Z;
        }
    }
    private void m68k_op_sub_32_er_pcix()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_PCIX_32();
            uint dst = r_dst;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            r_dst = FLAG_Z;
        }
    }
    private void m68k_op_sub_32_er_i()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = OPER_I_32();
            uint dst = r_dst;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            r_dst = FLAG_Z;
        }
    }
    private void m68k_op_sub_8_re_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_8();
            uint src = MASK_OUT_ABOVE_8(DX);
            uint dst = m68ki_read_8(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_sub_8_re_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_8();
            uint src = MASK_OUT_ABOVE_8(DX);
            uint dst = m68ki_read_8(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_sub_8_re_pi7()
    {
        unchecked
        {
            uint ea = EA_A7_PI_8();
            uint src = MASK_OUT_ABOVE_8(DX);
            uint dst = m68ki_read_8(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_sub_8_re_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_8();
            uint src = MASK_OUT_ABOVE_8(DX);
            uint dst = m68ki_read_8(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_sub_8_re_pd7()
    {
        unchecked
        {
            uint ea = EA_A7_PD_8();
            uint src = MASK_OUT_ABOVE_8(DX);
            uint dst = m68ki_read_8(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_sub_8_re_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_8();
            uint src = MASK_OUT_ABOVE_8(DX);
            uint dst = m68ki_read_8(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_sub_8_re_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_8();
            uint src = MASK_OUT_ABOVE_8(DX);
            uint dst = m68ki_read_8(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_sub_8_re_aw()
    {
        unchecked
        {
            uint ea = EA_AW_8();
            uint src = MASK_OUT_ABOVE_8(DX);
            uint dst = m68ki_read_8(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_sub_8_re_al()
    {
        unchecked
        {
            uint ea = EA_AL_8();
            uint src = MASK_OUT_ABOVE_8(DX);
            uint dst = m68ki_read_8(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_sub_16_re_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_16();
            uint src = MASK_OUT_ABOVE_16(DX);
            uint dst = m68ki_read_16(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_sub_16_re_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_16();
            uint src = MASK_OUT_ABOVE_16(DX);
            uint dst = m68ki_read_16(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_sub_16_re_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_16();
            uint src = MASK_OUT_ABOVE_16(DX);
            uint dst = m68ki_read_16(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_sub_16_re_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_16();
            uint src = MASK_OUT_ABOVE_16(DX);
            uint dst = m68ki_read_16(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_sub_16_re_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_16();
            uint src = MASK_OUT_ABOVE_16(DX);
            uint dst = m68ki_read_16(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_sub_16_re_aw()
    {
        unchecked
        {
            uint ea = EA_AW_16();
            uint src = MASK_OUT_ABOVE_16(DX);
            uint dst = m68ki_read_16(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_sub_16_re_al()
    {
        unchecked
        {
            uint ea = EA_AL_16();
            uint src = MASK_OUT_ABOVE_16(DX);
            uint dst = m68ki_read_16(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_sub_32_re_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_32();
            uint src = DX;
            uint dst = m68ki_read_32(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_sub_32_re_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_32();
            uint src = DX;
            uint dst = m68ki_read_32(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_sub_32_re_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_32();
            uint src = DX;
            uint dst = m68ki_read_32(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_sub_32_re_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_32();
            uint src = DX;
            uint dst = m68ki_read_32(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_sub_32_re_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_32();
            uint src = DX;
            uint dst = m68ki_read_32(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_sub_32_re_aw()
    {
        unchecked
        {
            uint ea = EA_AW_32();
            uint src = DX;
            uint dst = m68ki_read_32(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_sub_32_re_al()
    {
        unchecked
        {
            uint ea = EA_AL_32();
            uint src = DX;
            uint dst = m68ki_read_32(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_suba_16_d()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            r_dst = MASK_OUT_ABOVE_32((r_dst - unchecked((uint)(MAKE_INT_16(DY)))));
        }
    }
    private void m68k_op_suba_16_a()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            r_dst = MASK_OUT_ABOVE_32((r_dst - unchecked((uint)(MAKE_INT_16(AY)))));
        }
    }
    private void m68k_op_suba_16_ai()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = unchecked((uint)(MAKE_INT_16(OPER_AY_AI_16())));
            r_dst = MASK_OUT_ABOVE_32((r_dst - src));
        }
    }
    private void m68k_op_suba_16_pi()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = unchecked((uint)(MAKE_INT_16(OPER_AY_PI_16())));
            r_dst = MASK_OUT_ABOVE_32((r_dst - src));
        }
    }
    private void m68k_op_suba_16_pd()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = unchecked((uint)(MAKE_INT_16(OPER_AY_PD_16())));
            r_dst = MASK_OUT_ABOVE_32((r_dst - src));
        }
    }
    private void m68k_op_suba_16_di()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = unchecked((uint)(MAKE_INT_16(OPER_AY_DI_16())));
            r_dst = MASK_OUT_ABOVE_32((r_dst - src));
        }
    }
    private void m68k_op_suba_16_ix()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = unchecked((uint)(MAKE_INT_16(OPER_AY_IX_16())));
            r_dst = MASK_OUT_ABOVE_32((r_dst - src));
        }
    }
    private void m68k_op_suba_16_aw()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = unchecked((uint)(MAKE_INT_16(OPER_AW_16())));
            r_dst = MASK_OUT_ABOVE_32((r_dst - src));
        }
    }
    private void m68k_op_suba_16_al()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = unchecked((uint)(MAKE_INT_16(OPER_AL_16())));
            r_dst = MASK_OUT_ABOVE_32((r_dst - src));
        }
    }
    private void m68k_op_suba_16_pcdi()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = unchecked((uint)(MAKE_INT_16(OPER_PCDI_16())));
            r_dst = MASK_OUT_ABOVE_32((r_dst - src));
        }
    }
    private void m68k_op_suba_16_pcix()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = unchecked((uint)(MAKE_INT_16(OPER_PCIX_16())));
            r_dst = MASK_OUT_ABOVE_32((r_dst - src));
        }
    }
    private void m68k_op_suba_16_i()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = unchecked((uint)(MAKE_INT_16(OPER_I_16())));
            r_dst = MASK_OUT_ABOVE_32((r_dst - src));
        }
    }
    private void m68k_op_suba_32_d()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            r_dst = MASK_OUT_ABOVE_32((r_dst - DY));
        }
    }
    private void m68k_op_suba_32_a()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            r_dst = MASK_OUT_ABOVE_32((r_dst - AY));
        }
    }
    private void m68k_op_suba_32_ai()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = OPER_AY_AI_32();
            r_dst = MASK_OUT_ABOVE_32((r_dst - src));
        }
    }
    private void m68k_op_suba_32_pi()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = OPER_AY_PI_32();
            r_dst = MASK_OUT_ABOVE_32((r_dst - src));
        }
    }
    private void m68k_op_suba_32_pd()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = OPER_AY_PD_32();
            r_dst = MASK_OUT_ABOVE_32((r_dst - src));
        }
    }
    private void m68k_op_suba_32_di()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = OPER_AY_DI_32();
            r_dst = MASK_OUT_ABOVE_32((r_dst - src));
        }
    }
    private void m68k_op_suba_32_ix()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = OPER_AY_IX_32();
            r_dst = MASK_OUT_ABOVE_32((r_dst - src));
        }
    }
    private void m68k_op_suba_32_aw()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = OPER_AW_32();
            r_dst = MASK_OUT_ABOVE_32((r_dst - src));
        }
    }
    private void m68k_op_suba_32_al()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = OPER_AL_32();
            r_dst = MASK_OUT_ABOVE_32((r_dst - src));
        }
    }
    private void m68k_op_suba_32_pcdi()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = OPER_PCDI_32();
            r_dst = MASK_OUT_ABOVE_32((r_dst - src));
        }
    }
    private void m68k_op_suba_32_pcix()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = OPER_PCIX_32();
            r_dst = MASK_OUT_ABOVE_32((r_dst - src));
        }
    }
    private void m68k_op_suba_32_i()
    {
        unchecked
        {
            ref uint r_dst = ref AX;
            uint src = OPER_I_32();
            r_dst = MASK_OUT_ABOVE_32((r_dst - src));
        }
    }
    private void m68k_op_subi_8_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint src = OPER_I_8();
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_subi_8_ai()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AY_AI_8();
            uint dst = m68ki_read_8(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_subi_8_pi()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AY_PI_8();
            uint dst = m68ki_read_8(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_subi_8_pi7()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_A7_PI_8();
            uint dst = m68ki_read_8(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_subi_8_pd()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AY_PD_8();
            uint dst = m68ki_read_8(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_subi_8_pd7()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_A7_PD_8();
            uint dst = m68ki_read_8(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_subi_8_di()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AY_DI_8();
            uint dst = m68ki_read_8(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_subi_8_ix()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AY_IX_8();
            uint dst = m68ki_read_8(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_subi_8_aw()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AW_8();
            uint dst = m68ki_read_8(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_subi_8_al()
    {
        unchecked
        {
            uint src = OPER_I_8();
            uint ea = EA_AL_8();
            uint dst = m68ki_read_8(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_subi_16_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint src = OPER_I_16();
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_subi_16_ai()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AY_AI_16();
            uint dst = m68ki_read_16(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_subi_16_pi()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AY_PI_16();
            uint dst = m68ki_read_16(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_subi_16_pd()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AY_PD_16();
            uint dst = m68ki_read_16(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_subi_16_di()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AY_DI_16();
            uint dst = m68ki_read_16(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_subi_16_ix()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AY_IX_16();
            uint dst = m68ki_read_16(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_subi_16_aw()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AW_16();
            uint dst = m68ki_read_16(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_subi_16_al()
    {
        unchecked
        {
            uint src = OPER_I_16();
            uint ea = EA_AL_16();
            uint dst = m68ki_read_16(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_subi_32_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint src = OPER_I_32();
            uint dst = r_dst;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            r_dst = FLAG_Z;
        }
    }
    private void m68k_op_subi_32_ai()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AY_AI_32();
            uint dst = m68ki_read_32(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_subi_32_pi()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AY_PI_32();
            uint dst = m68ki_read_32(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_subi_32_pd()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AY_PD_32();
            uint dst = m68ki_read_32(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_subi_32_di()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AY_DI_32();
            uint dst = m68ki_read_32(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_subi_32_ix()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AY_IX_32();
            uint dst = m68ki_read_32(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_subi_32_aw()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AW_32();
            uint dst = m68ki_read_32(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_subi_32_al()
    {
        unchecked
        {
            uint src = OPER_I_32();
            uint ea = EA_AL_32();
            uint dst = m68ki_read_32(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_subq_8_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            r_dst = (MASK_OUT_BELOW_8(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_subq_8_ai()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_AI_8();
            uint dst = m68ki_read_8(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_subq_8_pi()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_PI_8();
            uint dst = m68ki_read_8(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_subq_8_pi7()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_A7_PI_8();
            uint dst = m68ki_read_8(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_subq_8_pd()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_PD_8();
            uint dst = m68ki_read_8(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_subq_8_pd7()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_A7_PD_8();
            uint dst = m68ki_read_8(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_subq_8_di()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_DI_8();
            uint dst = m68ki_read_8(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_subq_8_ix()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_IX_8();
            uint dst = m68ki_read_8(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_subq_8_aw()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AW_8();
            uint dst = m68ki_read_8(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_subq_8_al()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AL_8();
            uint dst = m68ki_read_8(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = MASK_OUT_ABOVE_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            m68ki_write_8(ea, FLAG_Z);
        }
    }
    private void m68k_op_subq_16_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            r_dst = (MASK_OUT_BELOW_16(r_dst) | FLAG_Z);
        }
    }
    private void m68k_op_subq_16_a()
    {
        unchecked
        {
            ref uint r_dst = ref AY;
            r_dst = MASK_OUT_ABOVE_32((r_dst - ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)))));
        }
    }
    private void m68k_op_subq_16_ai()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_AI_16();
            uint dst = m68ki_read_16(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_subq_16_pi()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_PI_16();
            uint dst = m68ki_read_16(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_subq_16_pd()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_PD_16();
            uint dst = m68ki_read_16(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_subq_16_di()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_DI_16();
            uint dst = m68ki_read_16(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_subq_16_ix()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_IX_16();
            uint dst = m68ki_read_16(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_subq_16_aw()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AW_16();
            uint dst = m68ki_read_16(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_subq_16_al()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AL_16();
            uint dst = m68ki_read_16(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = MASK_OUT_ABOVE_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            m68ki_write_16(ea, FLAG_Z);
        }
    }
    private void m68k_op_subq_32_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint dst = r_dst;
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            r_dst = FLAG_Z;
        }
    }
    private void m68k_op_subq_32_a()
    {
        unchecked
        {
            ref uint r_dst = ref AY;
            r_dst = MASK_OUT_ABOVE_32((r_dst - ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)))));
        }
    }
    private void m68k_op_subq_32_ai()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_AI_32();
            uint dst = m68ki_read_32(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_subq_32_pi()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_PI_32();
            uint dst = m68ki_read_32(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_subq_32_pd()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_PD_32();
            uint dst = m68ki_read_32(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_subq_32_di()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_DI_32();
            uint dst = m68ki_read_32(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_subq_32_ix()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AY_IX_32();
            uint dst = m68ki_read_32(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_subq_32_aw()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AW_32();
            uint dst = m68ki_read_32(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_subq_32_al()
    {
        unchecked
        {
            uint src = ((((REG_IR >> 9) - unchecked((uint)(1))) & unchecked((uint)(7))) + unchecked((uint)(1)));
            uint ea = EA_AL_32();
            uint dst = m68ki_read_32(ea);
            uint res = (dst - src);
            FLAG_N = NFLAG_32(res);
            FLAG_Z = MASK_OUT_ABOVE_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            m68ki_write_32(ea, FLAG_Z);
        }
    }
    private void m68k_op_subx_8_rr()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = MASK_OUT_ABOVE_8(DY);
            uint dst = MASK_OUT_ABOVE_8(r_dst);
            uint res = ((dst - src) - XFLAG_AS_1());
            FLAG_N = NFLAG_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            res = MASK_OUT_ABOVE_8(res);
            FLAG_Z |= res;
            r_dst = (MASK_OUT_BELOW_8(r_dst) | res);
        }
    }
    private void m68k_op_subx_16_rr()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = MASK_OUT_ABOVE_16(DY);
            uint dst = MASK_OUT_ABOVE_16(r_dst);
            uint res = ((dst - src) - XFLAG_AS_1());
            FLAG_N = NFLAG_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            res = MASK_OUT_ABOVE_16(res);
            FLAG_Z |= res;
            r_dst = (MASK_OUT_BELOW_16(r_dst) | res);
        }
    }
    private void m68k_op_subx_32_rr()
    {
        unchecked
        {
            ref uint r_dst = ref DX;
            uint src = DY;
            uint dst = r_dst;
            uint res = ((dst - src) - XFLAG_AS_1());
            FLAG_N = NFLAG_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            res = MASK_OUT_ABOVE_32(res);
            FLAG_Z |= res;
            r_dst = res;
        }
    }
    private void m68k_op_subx_8_mm_ax7()
    {
        unchecked
        {
            uint src = OPER_AY_PD_8();
            uint ea = EA_A7_PD_8();
            uint dst = m68ki_read_8(ea);
            uint res = ((dst - src) - XFLAG_AS_1());
            FLAG_N = NFLAG_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            res = MASK_OUT_ABOVE_8(res);
            FLAG_Z |= res;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_subx_8_mm_ay7()
    {
        unchecked
        {
            uint src = OPER_A7_PD_8();
            uint ea = EA_AX_PD_8();
            uint dst = m68ki_read_8(ea);
            uint res = ((dst - src) - XFLAG_AS_1());
            FLAG_N = NFLAG_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            res = MASK_OUT_ABOVE_8(res);
            FLAG_Z |= res;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_subx_8_mm_axy7()
    {
        unchecked
        {
            uint src = OPER_A7_PD_8();
            uint ea = EA_A7_PD_8();
            uint dst = m68ki_read_8(ea);
            uint res = ((dst - src) - XFLAG_AS_1());
            FLAG_N = NFLAG_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            res = MASK_OUT_ABOVE_8(res);
            FLAG_Z |= res;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_subx_8_mm()
    {
        unchecked
        {
            uint src = OPER_AY_PD_8();
            uint ea = EA_AX_PD_8();
            uint dst = m68ki_read_8(ea);
            uint res = ((dst - src) - XFLAG_AS_1());
            FLAG_N = NFLAG_8(res);
            FLAG_X = (FLAG_C = CFLAG_8(res));
            FLAG_V = VFLAG_SUB_8(src, dst, res);
            res = MASK_OUT_ABOVE_8(res);
            FLAG_Z |= res;
            m68ki_write_8(ea, res);
        }
    }
    private void m68k_op_subx_16_mm()
    {
        unchecked
        {
            uint src = OPER_AY_PD_16();
            uint ea = EA_AX_PD_16();
            uint dst = m68ki_read_16(ea);
            uint res = ((dst - src) - XFLAG_AS_1());
            FLAG_N = NFLAG_16(res);
            FLAG_X = (FLAG_C = CFLAG_16(res));
            FLAG_V = VFLAG_SUB_16(src, dst, res);
            res = MASK_OUT_ABOVE_16(res);
            FLAG_Z |= res;
            m68ki_write_16(ea, res);
        }
    }
    private void m68k_op_subx_32_mm()
    {
        unchecked
        {
            uint src = OPER_AY_PD_32();
            uint ea = EA_AX_PD_32();
            uint dst = m68ki_read_32(ea);
            uint res = ((dst - src) - XFLAG_AS_1());
            FLAG_N = NFLAG_32(res);
            FLAG_X = (FLAG_C = CFLAG_SUB_32(src, dst, res));
            FLAG_V = VFLAG_SUB_32(src, dst, res);
            res = MASK_OUT_ABOVE_32(res);
            FLAG_Z |= res;
            m68ki_write_32(ea, res);
        }
    }
    private void m68k_op_swap_32()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            FLAG_Z = MASK_OUT_ABOVE_32((r_dst << 16));
            r_dst = ((r_dst >> 16) | FLAG_Z);
            FLAG_Z = r_dst;
            FLAG_N = NFLAG_32(r_dst);
            FLAG_C = CFLAG_CLEAR;
            FLAG_V = VFLAG_CLEAR;
        }
    }
    private void m68k_op_tas_8_d()
    {
        unchecked
        {
            ref uint r_dst = ref DY;
            FLAG_Z = MASK_OUT_ABOVE_8(r_dst);
            FLAG_N = NFLAG_8(r_dst);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            r_dst |= unchecked((uint)(128));
        }
    }
    private void m68k_op_tas_8_ai()
    {
        unchecked
        {
            uint ea = EA_AY_AI_8();
            uint dst = m68ki_read_8(ea);
            FLAG_Z = dst;
            FLAG_N = NFLAG_8(dst);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            if ((m68ki_tas_callback() != 0)) m68ki_write_8(ea, (dst | unchecked((uint)(128))));
        }
    }
    private void m68k_op_tas_8_pi()
    {
        unchecked
        {
            uint ea = EA_AY_PI_8();
            uint dst = m68ki_read_8(ea);
            FLAG_Z = dst;
            FLAG_N = NFLAG_8(dst);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            if ((m68ki_tas_callback() != 0)) m68ki_write_8(ea, (dst | unchecked((uint)(128))));
        }
    }
    private void m68k_op_tas_8_pi7()
    {
        unchecked
        {
            uint ea = EA_A7_PI_8();
            uint dst = m68ki_read_8(ea);
            FLAG_Z = dst;
            FLAG_N = NFLAG_8(dst);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            if ((m68ki_tas_callback() != 0)) m68ki_write_8(ea, (dst | unchecked((uint)(128))));
        }
    }
    private void m68k_op_tas_8_pd()
    {
        unchecked
        {
            uint ea = EA_AY_PD_8();
            uint dst = m68ki_read_8(ea);
            FLAG_Z = dst;
            FLAG_N = NFLAG_8(dst);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            if ((m68ki_tas_callback() != 0)) m68ki_write_8(ea, (dst | unchecked((uint)(128))));
        }
    }
    private void m68k_op_tas_8_pd7()
    {
        unchecked
        {
            uint ea = EA_A7_PD_8();
            uint dst = m68ki_read_8(ea);
            FLAG_Z = dst;
            FLAG_N = NFLAG_8(dst);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            if ((m68ki_tas_callback() != 0)) m68ki_write_8(ea, (dst | unchecked((uint)(128))));
        }
    }
    private void m68k_op_tas_8_di()
    {
        unchecked
        {
            uint ea = EA_AY_DI_8();
            uint dst = m68ki_read_8(ea);
            FLAG_Z = dst;
            FLAG_N = NFLAG_8(dst);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            if ((m68ki_tas_callback() != 0)) m68ki_write_8(ea, (dst | unchecked((uint)(128))));
        }
    }
    private void m68k_op_tas_8_ix()
    {
        unchecked
        {
            uint ea = EA_AY_IX_8();
            uint dst = m68ki_read_8(ea);
            FLAG_Z = dst;
            FLAG_N = NFLAG_8(dst);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            if ((m68ki_tas_callback() != 0)) m68ki_write_8(ea, (dst | unchecked((uint)(128))));
        }
    }
    private void m68k_op_tas_8_aw()
    {
        unchecked
        {
            uint ea = EA_AW_8();
            uint dst = m68ki_read_8(ea);
            FLAG_Z = dst;
            FLAG_N = NFLAG_8(dst);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            if ((m68ki_tas_callback() != 0)) m68ki_write_8(ea, (dst | unchecked((uint)(128))));
        }
    }
    private void m68k_op_tas_8_al()
    {
        unchecked
        {
            uint ea = EA_AL_8();
            uint dst = m68ki_read_8(ea);
            FLAG_Z = dst;
            FLAG_N = NFLAG_8(dst);
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
            if ((m68ki_tas_callback() != 0)) m68ki_write_8(ea, (dst | unchecked((uint)(128))));
        }
    }
    private void m68k_op_trap()
    {
        unchecked
        {
            m68ki_exception_trapN((EXCEPTION_TRAP_BASE + (REG_IR & unchecked((uint)(15)))));
        }
    }
    private void m68k_op_trapv()
    {
        unchecked
        {
            if ((COND_VC() != 0)) {
            USE_CYCLES(unchecked((long)((4 * MUL))));
            return;
            }
            m68ki_exception_trap(EXCEPTION_TRAPV);
        }
    }
    private void m68k_op_tst_8_d()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_8(DY);
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_tst_8_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_tst_8_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_tst_8_pi7()
    {
        unchecked
        {
            uint res = OPER_A7_PI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_tst_8_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_tst_8_pd7()
    {
        unchecked
        {
            uint res = OPER_A7_PD_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_tst_8_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_tst_8_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_tst_8_aw()
    {
        unchecked
        {
            uint res = OPER_AW_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_tst_8_al()
    {
        unchecked
        {
            uint res = OPER_AL_8();
            FLAG_N = NFLAG_8(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_tst_16_d()
    {
        unchecked
        {
            uint res = MASK_OUT_ABOVE_16(DY);
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_tst_16_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_tst_16_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_tst_16_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_tst_16_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_tst_16_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_tst_16_aw()
    {
        unchecked
        {
            uint res = OPER_AW_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_tst_16_al()
    {
        unchecked
        {
            uint res = OPER_AL_16();
            FLAG_N = NFLAG_16(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_tst_32_d()
    {
        unchecked
        {
            uint res = DY;
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_tst_32_ai()
    {
        unchecked
        {
            uint res = OPER_AY_AI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_tst_32_pi()
    {
        unchecked
        {
            uint res = OPER_AY_PI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_tst_32_pd()
    {
        unchecked
        {
            uint res = OPER_AY_PD_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_tst_32_di()
    {
        unchecked
        {
            uint res = OPER_AY_DI_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_tst_32_ix()
    {
        unchecked
        {
            uint res = OPER_AY_IX_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_tst_32_aw()
    {
        unchecked
        {
            uint res = OPER_AW_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_tst_32_al()
    {
        unchecked
        {
            uint res = OPER_AL_32();
            FLAG_N = NFLAG_32(res);
            FLAG_Z = res;
            FLAG_V = VFLAG_CLEAR;
            FLAG_C = CFLAG_CLEAR;
        }
    }
    private void m68k_op_unlk_32_a7()
    {
        unchecked
        {
            _registers[8 + (int)(7)] = m68ki_read_32(_registers[8 + (int)(7)]);
        }
    }
    private void m68k_op_unlk_32()
    {
        unchecked
        {
            ref uint r_dst = ref AY;
            _registers[8 + (int)(7)] = r_dst;
            r_dst = m68ki_pull_32();
        }
    }
}

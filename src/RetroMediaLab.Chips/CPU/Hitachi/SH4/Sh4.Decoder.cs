using System.Diagnostics;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

using static Sh4BlockEndType;
using static Sh4DecoderConstants;
using static Sh4Register;
using static Sh4ShilOperation;

#if !SH4_DYNAREC_NONE
internal sealed partial class Sh4Decoder(Sh4RecompilerDriver driver)
{
    private const int BlockOpcodeSoftLimit = 500;
    private const int BlockOpcodeHardLimit = 511;
    private static Sh4RuntimeBlockInfo _block = null!;
    private static readonly Sh4Cycles _cycles = new();
    private static readonly Sh4DecoderState _state = new();

    private static Sh4ShilParameter Imm(uint value) => new(value);

    private static Sh4ShilParameter Reg(Sh4Register register) => new(register);

    private static Sh4ShilParameter Reg(int register) => Reg((Sh4Register)register);

    private void Emit(Sh4ShilOperation operation, Sh4ShilParameter destination = default,
        Sh4ShilParameter source1 = default, Sh4ShilParameter source2 = default,
        uint size = 0, Sh4ShilParameter source3 = default, Sh4ShilParameter destination2 = default)
    {
        _block.Opcodes.Add(new Sh4ShilOpcode
        {
            Operation = operation,
            Size = size,
            Destination = destination,
            Destination2 = destination2,
            Source1 = source1,
            Source2 = source2,
            Source3 = source3,
            GuestOffset = (int)(_state.Cpu.ProgramCounter - _block.VirtualAddress),
            DelaySlot = _state.Cpu.IsDelaySlot
        });
    }

    private void DecodeFallback(uint opcode)
    {
        _block.Opcodes.Add(new Sh4ShilOpcode
        {
            Operation = shop_ifb,
            Source1 = Imm(driver.OpcodeTable[opcode].NeedPc ? 1u : 0u),
            Source2 = Imm(_state.Cpu.ProgramCounter + 2),
            Source3 = Imm(opcode),
            GuestOffset = (int)(_state.Cpu.ProgramCounter - _block.VirtualAddress),
            DelaySlot = _state.Cpu.IsDelaySlot
        });
    }

    private void SetDynamicTarget(Sh4Register register, uint offset = 0)
    {
        if (offset == 0)
            Emit(shop_jdyn, reg_pc_dyn, Reg(register));
        else
            Emit(shop_jdyn, reg_pc_dyn, Reg(register), Imm(offset));
    }

    private void EndBlock(uint destination, Sh4BlockEndType type, bool delaySlot)
    {
        _state.BlockType = type;
        _state.NextOperation = delaySlot ? Sh4NextDecoderOperation.DelaySlot : Sh4NextDecoderOperation.End;
        _state.DelayOperation = Sh4NextDecoderOperation.End;
        _state.JumpAddress = destination;
        if (type is not (BET_StaticCall or BET_StaticJump))
            _state.NextAddress = _state.Cpu.ProgramCounter + (delaySlot ? 4u : 2u);
        else
            Sh4HostRuntime.Verify(_state.JumpAddress != NullAddress);
    }

    private uint JumpSigned8(uint opcode) =>
        unchecked(_state.Cpu.ProgramCounter + (uint)(GetSImm8(opcode) * 2) + 4);

    private uint JumpSigned12(uint opcode) =>
        unchecked(_state.Cpu.ProgramCounter + (uint)(GetSImm12(opcode) * 2) + 4);

    private uint SetProcedureRegister()
    {
        uint returnAddress = _state.Cpu.ProgramCounter + 4;
        Emit(shop_mov32, reg_pr, Imm(returnAddress));
        return returnAddress;
    }

    private void WriteStatusRegister(Sh4ShilParameter source)
    {
        Emit(shop_and, Reg(reg_sr_status), source, Imm(Sh4StatusRegister.Mask));
        Emit(shop_and, Reg(reg_sr_T), source, Imm(1));
    }

    internal partial void Decode_i1000_1011_iiii_iiii(uint opcode)
    {
        EndBlock(JumpSigned8(opcode), BET_Cond_0, false);
    }
    internal partial void Decode_i1000_1111_iiii_iiii(uint opcode)
    {
        _block.HasConditionalJump = true;
        Emit(shop_jcond, reg_pc_dyn, reg_sr_T);
        EndBlock(JumpSigned8(opcode), BET_Cond_0, true);
    }
    internal partial void Decode_i1000_1001_iiii_iiii(uint opcode)
    {
        EndBlock(JumpSigned8(opcode), BET_Cond_1, false);
    }
    internal partial void Decode_i1000_1101_iiii_iiii(uint opcode)
    {
        _block.HasConditionalJump = true;
        Emit(shop_jcond, reg_pc_dyn, reg_sr_T);
        EndBlock(JumpSigned8(opcode), BET_Cond_1, true);
    }
    internal partial void Decode_i1010_iiii_iiii_iiii(uint opcode)
    {
        EndBlock(JumpSigned12(opcode), BET_StaticJump, true);
    }
    internal partial void Decode_i0000_nnnn_0010_0011(uint opcode)
    {
        uint n = GetN(opcode);

        SetDynamicTarget(reg_r0 + (int)n, _state.Cpu.ProgramCounter + 4);
        EndBlock(NullAddress, BET_DynamicJump, true);
    }
    internal partial void Decode_i0100_nnnn_0010_1011(uint opcode)
    {
        uint n = GetN(opcode);

        SetDynamicTarget(reg_r0 + (int)n);
        EndBlock(NullAddress, BET_DynamicJump, true);
    }
    internal partial void Decode_i1011_iiii_iiii_iiii(uint opcode)
    {
        SetProcedureRegister();
        EndBlock(JumpSigned12(opcode), BET_StaticCall, true);
    }
    internal partial void Decode_i0000_nnnn_0000_0011(uint opcode)
    {
        uint n = GetN(opcode);
        uint returnAddress = SetProcedureRegister();
        SetDynamicTarget(reg_r0 + (int)n, returnAddress);
        EndBlock(NullAddress, BET_DynamicCall, true);
    }
    internal partial void Decode_i0100_nnnn_0000_1011(uint opcode)
    {
        uint n = GetN(opcode);

        SetProcedureRegister();
        SetDynamicTarget(reg_r0 + (int)n);
        EndBlock(NullAddress, BET_DynamicCall, true);
    }
    internal partial void Decode_i0000_0000_0000_1011(uint opcode)
    {
        SetDynamicTarget(reg_pr);
        EndBlock(NullAddress, BET_DynamicRet, true);
    }
    internal partial void Decode_i0000_0000_0010_1011(uint opcode)
    {
        WriteStatusRegister(reg_ssr);
        Emit(shop_sync_sr);
        SetDynamicTarget(reg_spc);
        EndBlock(NullAddress, BET_DynamicIntr, true);
    }
    internal partial void Decode_i1100_0011_iiii_iiii(uint opcode)
    {
        DecodeFallback(opcode);
        SetDynamicTarget(reg_nextpc);
        EndBlock(NullAddress, BET_DynamicJump, false);
    }
    internal partial void Decode_i0000_0000_0001_1011(uint opcode)
    {
        DecodeFallback(opcode);
        SetDynamicTarget(reg_nextpc);
        EndBlock(NullAddress, BET_DynamicJump, false);
    }

    internal partial void Decode_illegalOp(uint opcode)
    {
        Trace.TraceInformation($"illegal instuction {opcode:x4} at pc {_state.Cpu.ProgramCounter:x}");
        Emit(shop_illegal, default, Imm(_state.Cpu.ProgramCounter), Imm(_state.Cpu.IsDelaySlot ? 1u : 0u));
        SetDynamicTarget(reg_nextpc);
        EndBlock(NullAddress, BET_DynamicJump, false);
    }

    internal partial void Decode_i0100_nnnn_0000_1110(uint opcode)
    {
        WriteStatusRegister((Sh4Register)(reg_r0 + (int)GetN(opcode)));
        Emit(shop_sync_sr);
        if (!_state.Cpu.IsDelaySlot)
            EndBlock(_state.Cpu.ProgramCounter + 2, BET_StaticIntr, false);
    }

    internal partial void Decode_i0100_nnnn_0000_0111(uint opcode)
    {
        Sh4ShilParameter rn = Reg(reg_r0 + (int)GetN(opcode));
        _state.Info.HasReadMemory = true;
        Emit(shop_readm, reg_temp, rn, default, 4);
        Emit(shop_add, rn, rn, Imm(4));
        WriteStatusRegister(reg_temp);
        Emit(shop_sync_sr);
        if (!_state.Cpu.IsDelaySlot)
            EndBlock(_state.Cpu.ProgramCounter + 2, BET_StaticIntr, false);
    }

    internal partial void Decode_i0100_nnnn_0110_1010(uint opcode)
    {
        Emit(shop_mov32, reg_fpscr, Reg(reg_r0 + (int)GetN(opcode)));
        Emit(shop_sync_fpscr);
        if (!_state.Cpu.IsDelaySlot)
            EndBlock(_state.Cpu.ProgramCounter + 2, BET_StaticJump, false);
    }

    internal partial void Decode_i0100_nnnn_0110_0110(uint opcode)
    {
        Sh4ShilParameter rn = Reg(reg_r0 + (int)GetN(opcode));
        _state.Info.HasReadMemory = true;
        Emit(shop_readm, reg_fpscr, rn, default, 4);
        Emit(shop_add, rn, rn, Imm(4));
        Emit(shop_sync_fpscr);
        if (!_state.Cpu.IsDelaySlot)
            EndBlock(_state.Cpu.ProgramCounter + 2, BET_StaticJump, false);
    }

    internal partial void Decode_i0100_nnnn_0000_0011(uint opcode)
    {
        Emit(shop_mov32, reg_temp, reg_sr_status);
        Emit(shop_or, reg_temp, reg_temp, reg_sr_T);
        Sh4ShilParameter rn = Reg(reg_r0 + (int)GetN(opcode));
        _state.Info.HasWriteMemory = true;
        Emit(shop_writem, default, rn, reg_temp, 4, Imm(unchecked((uint)-4)));
        Emit(shop_add, rn, rn, Imm(unchecked((uint)-4)));
    }

    internal partial void Decode_i0100_nnnn_0001_1011(uint opcode)
    {
        Sh4ShilParameter rn = Reg(reg_r0 + (int)GetN(opcode));
        _state.Info.HasReadMemory = true;
        _state.Info.HasWriteMemory = true;
        Emit(shop_readm, reg_temp, rn, default, 1);
        Emit(shop_seteq, reg_sr_T, reg_temp, Imm(0));
        Emit(shop_or, reg_temp, reg_temp, Imm(0x80));
        Emit(shop_writem, default, rn, reg_temp, 1);
    }

    internal partial void Decode_i0000_0000_0000_1001(uint opcode)
    {
    }

    internal partial void Decode_i1111_0011_1111_1101(uint opcode)
    {
        Emit(shop_xor, reg_fpscr, reg_fpscr, Imm(1<<20));
        _state.Cpu.Fsz64 = !_state.Cpu.Fsz64;
    }

    internal partial void Decode_i1111_1011_1111_1101(uint opcode)
    {
        Emit(shop_xor, reg_fpscr, reg_fpscr, Imm(1<<21));
        Emit(shop_mov32, reg_old_fpscr, reg_fpscr);
        Emit(shop_frswap, regv_xmtrx, regv_fmtrx, regv_xmtrx, 0, default, regv_fmtrx);
    }

    internal partial void Decode_i0100_nnnn_0010_0100(uint opcode)
    {
        uint n = GetN(opcode);
        Sh4Register rn = (Sh4Register)(reg_r0 + (int)n);

        Emit(shop_rocl, rn, rn, reg_sr_T, 0, default, reg_sr_T);
    }

    internal partial void Decode_i0100_nnnn_0010_0101(uint opcode)
    {
        uint n = GetN(opcode);
        Sh4Register rn = (Sh4Register)(reg_r0 + (int)n);

        Emit(shop_rocr, rn, rn, reg_sr_T, 0, default, reg_sr_T);
    }

    private static readonly Sh4Register[] SystemRegisters =
    [
        reg_mach,
        reg_macl,
        reg_pr,
        reg_sgr,
        NoReg,
        reg_fpul,
        reg_fpscr,
        NoReg,

        NoReg,
        NoReg,
        NoReg,
        NoReg,
        NoReg,
        NoReg,
        NoReg,
        reg_dbr,
    ];

    private static readonly Sh4Register[] ControlRegisters =
    [
        reg_sr_status,
        reg_gbr,
        reg_vbr,
        reg_ssr,
        reg_spc,
        NoReg,
        NoReg,
        NoReg,

        reg_r0_Bank,
        reg_r1_Bank,
        reg_r2_Bank,
        reg_r3_Bank,
        reg_r4_Bank,
        reg_r5_Bank,
        reg_r6_Bank,
        reg_r7_Bank,
    ];

    private void DecodeParameter(Sh4DecodeParameter p, ref Sh4ShilParameter r1, ref Sh4ShilParameter r2, uint opcode)
    {
        switch(p)
        {

        case Sh4DecodeParameter.PRM_PC_D8_x2:
            r1 = Imm((_state.Cpu.ProgramCounter+4)+(GetImm8(opcode)<<1));
            break;

        case Sh4DecodeParameter.PRM_PC_D8_x4:
            r1 = Imm(((_state.Cpu.ProgramCounter+4)&0xFFFFFFFC)+(GetImm8(opcode)<<2));
            break;

        case Sh4DecodeParameter.PRM_ZERO:
            r1 = Imm(0);
            break;

        case Sh4DecodeParameter.PRM_ONE:
            r1 = Imm(1);
            break;

        case Sh4DecodeParameter.PRM_TWO:
            r1 = Imm(2);
            break;

        case Sh4DecodeParameter.PRM_TWO_INV:
            r1 = Imm(~2u);
            break;

        case Sh4DecodeParameter.PRM_ONE_F32:
            r1 = Imm(0x3f800000);
            break;


        case Sh4DecodeParameter.PRM_SIMM8:
            r1 = Imm(unchecked((uint)GetSImm8(opcode)));
            break;
        case Sh4DecodeParameter.PRM_UIMM8:
            r1 = Imm(GetImm8(opcode));
            break;


        case Sh4DecodeParameter.PRM_R0:
            r1 = Reg(reg_r0);
            break;

        case Sh4DecodeParameter.PRM_RN:
            r1 = Reg(reg_r0 + (int)(GetN(opcode)));
            break;

        case Sh4DecodeParameter.PRM_RM:
            r1 = Reg(reg_r0 + (int)(GetM(opcode)));
            break;

        case Sh4DecodeParameter.PRM_FRN_SZ:
            if (_state.Cpu.Fsz64)
            {
                int rx = (int)(GetN(opcode)/2);
                if ((GetN(opcode)&1) != 0)
                    rx += (int)regv_xd_0;
                else
                    rx += (int)regv_dr_0;

                r1 = Reg(rx);
                break;
            }
            goto case Sh4DecodeParameter.PRM_FRN;
        case Sh4DecodeParameter.PRM_FRN:
            r1 = Reg(reg_fr_0 + (int)(GetN(opcode)));
            break;

        case Sh4DecodeParameter.PRM_FRM_SZ:
            if (_state.Cpu.Fsz64)
            {
                int rx = (int)(GetM(opcode)/2);
                if ((GetM(opcode)&1) != 0)
                    rx += (int)regv_xd_0;
                else
                    rx += (int)regv_dr_0;

                r1 = Reg(rx);
                break;
            }
            goto case Sh4DecodeParameter.PRM_FRM;
        case Sh4DecodeParameter.PRM_FRM:
            r1 = Reg(reg_fr_0 + (int)(GetM(opcode)));
            break;

        case Sh4DecodeParameter.PRM_FPUL:
            r1 = Reg(reg_fpul);
            break;

        case Sh4DecodeParameter.PRM_FPN:
            r1 = Reg(regv_dr_0 + (int)(GetN(opcode)/2));
            break;

        case Sh4DecodeParameter.PRM_FVN:
            r1 = Reg(regv_fv_0 + (int)(GetN(opcode)/4));
            break;

        case Sh4DecodeParameter.PRM_FVM:
            r1 = Reg(regv_fv_0 + (int)(GetN(opcode) & 3));
            break;

        case Sh4DecodeParameter.PRM_XMTRX:
            r1 = Reg(regv_xmtrx);
            break;

        case Sh4DecodeParameter.PRM_FRM_FR0:
            r1 = Reg(reg_fr_0 + (int)(GetM(opcode)));
            r2 = Reg(reg_fr_0);
            break;

        case Sh4DecodeParameter.PRM_SR_T:
            r1 = Reg(reg_sr_T);
            break;

        case Sh4DecodeParameter.PRM_SR_STATUS:
            r1 = Reg(reg_sr_status);
            break;

        case Sh4DecodeParameter.PRM_SREG:
            r1 = Reg(SystemRegisters[GetM(opcode)]);
            break;
        case Sh4DecodeParameter.PRM_CREG:
            r1 = Reg(ControlRegisters[GetM(opcode)]);
            break;


        case Sh4DecodeParameter.PRM_RN_D4_x1:
        case Sh4DecodeParameter.PRM_RN_D4_x2:
        case Sh4DecodeParameter.PRM_RN_D4_x4:
            {
                int shft = (int)p - (int)Sh4DecodeParameter.PRM_RN_D4_x1;
                r1 = Reg(reg_r0 + (int)(GetN(opcode)));
                r2 = Imm(GetImm4(opcode)<<shft);
                if (r2.ImmediateValue == 0)
                    r2 = default;
            }
            break;

        case Sh4DecodeParameter.PRM_RN_R0:
            r1 = Reg(reg_r0 + (int)(GetN(opcode)));
            r2 = Reg(reg_r0);
            break;

        case Sh4DecodeParameter.PRM_RM_D4_x1:
        case Sh4DecodeParameter.PRM_RM_D4_x2:
        case Sh4DecodeParameter.PRM_RM_D4_x4:
            {
                int shft = (int)p - (int)Sh4DecodeParameter.PRM_RM_D4_x1;
                r1 = Reg(reg_r0 + (int)(GetM(opcode)));
                r2 = Imm(GetImm4(opcode)<<shft);
                if (r2.ImmediateValue == 0)
                    r2 = default;
            }
            break;

        case Sh4DecodeParameter.PRM_RM_R0:
            r1 = Reg(reg_r0 + (int)(GetM(opcode)));
            r2 = Reg(reg_r0);
            break;

        case Sh4DecodeParameter.PRM_GBR_D8_x1:
        case Sh4DecodeParameter.PRM_GBR_D8_x2:
        case Sh4DecodeParameter.PRM_GBR_D8_x4:
            {
                int shft = (int)p - (int)Sh4DecodeParameter.PRM_GBR_D8_x1;
                r1 = Reg(reg_gbr);
                r2 = Imm(GetImm8(opcode)<<shft);
                if (r2.ImmediateValue == 0)
                    r2 = default;
            }
            break;

        default:
            Sh4HostRuntime.Die("Non-supported parameter used");
            break;
        }
    }

    private const uint MASK_N_M = 0xF00F;
    private const uint MASK_N = 0xF0FF;
    private const uint DIV0U_KEY = 0x0019;
    private const uint DIV0S_KEY = 0x2007;
    private const uint DIV1_KEY = 0x3004;
    private const uint ROTCL_KEY = 0x4024;

    private static Sh4Register _divisionRegister1;
    private static Sh4Register _divisionRegister2;
    private static Sh4Register _divisionRegister3;

    private uint MatchDiv32(uint pc , ref Sh4Register reg1, ref Sh4Register reg2 , ref Sh4Register reg3)
    {

        uint address = pc;
        uint match = 1;
        for (int i = 0;i<32;i++)
        {
            ushort opcode = driver.ReadInstruction16(address);
            address+=2;
            if ((opcode&MASK_N)==ROTCL_KEY)
            {
                if (reg1==NoReg)
                    reg1 = (Sh4Register)GetN(opcode);
                else if (reg1!=(Sh4Register)GetN(opcode))
                    break;
                match++;
            }
            else
            {

                break;
            }

            opcode = driver.ReadInstruction16(address);
            address+=2;
            if ((opcode&MASK_N_M)==DIV1_KEY)
            {
                if (reg2==NoReg)
                    reg2 = (Sh4Register)GetM(opcode);
                else if (reg2!=(Sh4Register)GetM(opcode))
                    break;

                if (reg2==reg1)
                    break;

                if (reg3==NoReg)
                    reg3 = (Sh4Register)GetN(opcode);
                else if (reg3!=(Sh4Register)GetN(opcode))
                    break;

                if (reg3==reg1)
                    break;

                match++;
            }
            else
                break;
        }

        return match;
    }

    private bool MatchDiv32u(uint opcode, uint pc)
    {
        _divisionRegister1 = NoReg;
        _divisionRegister2 = NoReg;
        _divisionRegister3 = NoReg;

        uint match = MatchDiv32(pc + 2, ref _divisionRegister1, ref _divisionRegister2, ref _divisionRegister3);

        return match == 65;
    }

    private bool MatchDiv32s(uint opcode, uint pc)
    {
        uint n = GetN(opcode);
        uint m = GetM(opcode);

        _divisionRegister1 = NoReg;
        _divisionRegister2 = (Sh4Register)m;
        _divisionRegister3 = (Sh4Register)n;

        uint match = MatchDiv32(pc + 2, ref _divisionRegister1, ref _divisionRegister2, ref _divisionRegister3);

        return match == 65;
    }

    private bool DecodeGeneric(uint opcode)
    {
        Sh4DecodeMode mode;Sh4DecodeParameter d;Sh4DecodeParameter s;Sh4ShilOperation operation;uint e;
        if (driver.OpcodeTable[opcode].Decode==0)
            return false;

        ulong descriptor = driver.OpcodeTable[opcode].Decode;

        e = (uint)(descriptor>>32);
        mode = (Sh4DecodeMode)((descriptor>>24)&0xFF);
        d = (Sh4DecodeParameter)((descriptor>>16)&0xFF);
        s = (Sh4DecodeParameter)((descriptor>>8)&0xFF);
        operation = (Sh4ShilOperation)((descriptor>>0)&0xFF);

        bool transfer64 = false;
        if (opcode>=0xF000)
        {
            _state.Info.HasFpu = true;
            if (_state.Cpu.Fpr64) {


                if (((opcode & 0xf) < 6 || (opcode & 0xf) > 0xc)
                        && (opcode & 0xef) != 0x0d
                        && (opcode & 0xff) != 0x4d)
                    return false;
            }

            if (_state.Cpu.Fsz64 && (d==Sh4DecodeParameter.PRM_FRN_SZ || d==Sh4DecodeParameter.PRM_FRM_SZ || s==Sh4DecodeParameter.PRM_FRN_SZ || s==Sh4DecodeParameter.PRM_FRM_SZ))
                transfer64 = true;
        }

        Sh4ShilParameter rs1 = default, rs2 = default, rs3 = default;

        DecodeParameter(s, ref rs2, ref rs3, opcode);
        DecodeParameter(d, ref rs1, ref rs3, opcode);

        switch(mode)
        {
        case Sh4DecodeMode.DM_ReadSRF:
            Emit(shop_mov32, rs1, reg_sr_status);
            Emit(shop_or, rs1, rs1, reg_sr_T);
            break;

        case Sh4DecodeMode.DM_WriteTOp:
            Emit(operation, reg_sr_T, rs1, rs2);
            break;

        case Sh4DecodeMode.DM_DT:
            Sh4HostRuntime.Verify(operation == shop_sub);
            Emit(operation, rs1, rs1, rs2);
            Emit(shop_seteq, Reg(reg_sr_T), rs1, Imm(0));
            break;

        case Sh4DecodeMode.DM_Shift:
            if (operation==shop_shl && e==1)
                Emit(shop_shr, Reg(reg_sr_T), rs1, Imm(31));
            else if (e==1)
                Emit(shop_and, Reg(reg_sr_T), rs1, Imm(1));

            Emit(operation, rs1, rs1, Imm(e));
            break;

        case Sh4DecodeMode.DM_Rot:
            if ((((int)e >= 0 ? e : unchecked(0u - e)) & 0x1000) == 0)
            {
                if ((int)e<0)
                {

                    Emit(shop_shr, Reg(reg_sr_T), rs2, Imm(31));
                    e = unchecked(0u - e);
                }
                else
                {

                    Emit(shop_and, Reg(reg_sr_T), rs2, Imm(1));
                }
            }
            e&=31;

            Emit(operation, rs1, rs2, Imm(e));
            break;

        case Sh4DecodeMode.DM_BinaryOp:
            if ((e & 1) != 0)
                Emit(operation, rs1, rs1, rs2, 0, rs3);
            else
                Emit(operation, default, rs1, rs2, 0, rs3);
            break;

        case Sh4DecodeMode.DM_UnaryOp:
            if (transfer64 && operation==shop_mov32)
                operation = shop_mov64;

            if (operation==shop_cvt_i2f_n && _state.Cpu.RoundToZero)
                operation = shop_cvt_i2f_z;

            if ((e & 1) != 0)
                Emit(operation, default, rs1);
            else
                Emit(operation, rs1, rs2);
            break;

        case Sh4DecodeMode.DM_WriteM:
            {

                if (rs3.IsImmediate && rs3.Immediate==0)
                    rs3 = default;

                _state.Info.HasWriteMemory = true;
                if (transfer64) e = unchecked(e * 2);
                bool updateAfter = false;
                if ((int)e<0)
                {
                    if (rs1.Register!=rs2.Register && !driver.Mmu.Enabled)
                    {
                        Emit(shop_sub, rs1, rs1, Imm(unchecked(0u - e)));
                    }
                    else
                    {
                        Sh4HostRuntime.Verify(rs3.IsNull);
                        rs3 = Imm(e);
                        updateAfter = true;
                    }
                }

                Emit(shop_writem, default, rs1, rs2, (int)e<0?unchecked(0u - e):e, rs3);

                if (updateAfter)
                {
                    Emit(shop_sub, rs1, rs1, Imm(unchecked(0u - e)));
                }
            }
            break;

        case Sh4DecodeMode.DM_ReadM:

            if (rs3.IsImmediate && rs3.Immediate==0)
                    rs3 = default;

            _state.Info.HasReadMemory = true;
            if (transfer64) e = unchecked(e * 2);

            Emit(shop_readm, rs1, rs2, default, (int)e<0?unchecked(0u - e):e, rs3);
            if ((int)e<0)
            {
                if (rs1.Register!=rs2.Register)
                    Emit(shop_add, rs2, rs2, Imm(unchecked(0u - e)));
            }
            break;

        case Sh4DecodeMode.DM_fiprOp:
            {
                Sh4ShilParameter rdd = Reg(rs1.Register+3);
                Emit(operation, rdd, rs1, rs2);
            }
            break;

        case Sh4DecodeMode.DM_EXTOP:
            {
                Emit(operation, rs1, rs2, Imm(e == 1 ? 0xFFu : 0xFFFFu));
            }
            break;

        case Sh4DecodeMode.DM_MUL:
            {
                Sh4ShilOperation multiply;
                Sh4ShilParameter destination = Reg(reg_macl);
                Sh4ShilParameter rd2 = default;

                switch((int)e)
                {
                    case 16:  multiply = shop_mul_u16; break;
                    case -16: multiply = shop_mul_s16; break;

                    case -32: multiply = shop_mul_i32; break;

                    case 64:  multiply = shop_mul_u64; rd2 = Reg(reg_mach); break;
                    case -64: multiply = shop_mul_s64; rd2 = Reg(reg_mach); break;

                    default:
                        Sh4HostRuntime.Die("DM_MUL: Failed to classify opcode");
                        return false;
                }

                Emit(multiply, destination, rs1, rs2, 0, default, rd2);
            }
            break;

        case Sh4DecodeMode.DM_DIV0:
            {
                if (e==1)
                {
                    if (MatchDiv32u(opcode, _state.Cpu.ProgramCounter))
                    {
                        Sh4HostRuntime.Verify(!_state.Cpu.IsDelaySlot);

                        Emit(shop_div32u, Reg(_divisionRegister1), Reg(_divisionRegister1), Reg(_divisionRegister2), 0, Reg(_divisionRegister3), Reg(_divisionRegister3));

                        Emit(shop_and, Reg(reg_sr_T), Reg(_divisionRegister1), Imm(1));
                        Emit(shop_shr, Reg(_divisionRegister1), Reg(_divisionRegister1), Imm(1));

                        Emit(shop_div32p2, Reg(_divisionRegister3), Reg(_divisionRegister3), Reg(_divisionRegister2), 0, Reg(reg_sr_T));

                        for (int i = 1; i <= 64; i++)
                        {
                            ushort stepOpcode = driver.ReadInstruction16(_state.Cpu.ProgramCounter + (uint)i * 2);
                            _block.GuestCycles += (uint)_cycles.CountCycles(stepOpcode);
                        }

                        _state.Cpu.ProgramCounter += 128;
                    }
                    else
                    {

                        uint qm = (1<<8)|(1<<9);
                        Emit(shop_and, Reg(reg_sr_status), Reg(reg_sr_status), Imm(~qm));

                        Emit(shop_mov32, Reg(reg_sr_T), Imm(0));
                    }
                }
                else
                {
                    if (MatchDiv32s(opcode, _state.Cpu.ProgramCounter))
                    {
                        Sh4HostRuntime.Verify(!_state.Cpu.IsDelaySlot);

                        Emit(shop_xor, Reg(reg_sr_T), Reg(_divisionRegister3), Reg(_divisionRegister2));
                        Emit(shop_and, Reg(reg_sr_T), Reg(reg_sr_T), Imm(1u << 31));

                        Emit(shop_div32s, Reg(_divisionRegister1), Reg(_divisionRegister1), Reg(_divisionRegister2), 0, Reg(_divisionRegister3), Reg(_divisionRegister3));

                        Emit(shop_and, Reg(reg_temp), Reg(_divisionRegister1), Imm(1));
                        Emit(shop_sar, Reg(_divisionRegister1), Reg(_divisionRegister1), Imm(1));
                        Emit(shop_or, Reg(reg_sr_T), Reg(reg_sr_T), Reg(reg_temp));

                        Emit(shop_div32p2, Reg(_divisionRegister3), Reg(_divisionRegister3), Reg(_divisionRegister2), 0, Reg(reg_sr_T));

                        Emit(shop_and, Reg(reg_sr_T), Reg(reg_sr_T), Imm(1));

                        for (int i = 1; i <= 64; i++)
                        {
                            ushort stepOpcode = driver.ReadInstruction16(_state.Cpu.ProgramCounter + (uint)i * 2);
                            _block.GuestCycles += (uint)_cycles.CountCycles(stepOpcode);
                        }

                        _state.Cpu.ProgramCounter += 128;
                    }
                    else
                    {

                        Emit(shop_and, Reg(reg_sr_status), Reg(reg_sr_status), Imm(~((1u << 8) | (1u << 9))));


                        Emit(shop_sar, Reg(reg_sr_T), rs1, Imm(31));
                        Emit(shop_and, Reg(reg_sr_T), Reg(reg_sr_T), Imm(1 << 8));
                        Emit(shop_or, Reg(reg_sr_status), Reg(reg_sr_status), Reg(reg_sr_T));


                        Emit(shop_sar, Reg(reg_sr_T), rs2, Imm(31));
                        Emit(shop_and, Reg(reg_sr_T), Reg(reg_sr_T), Imm(1 << 9));
                        Emit(shop_or, Reg(reg_sr_status), Reg(reg_sr_status), Reg(reg_sr_T));


                        Emit(shop_xor, Reg(reg_sr_T), rs1, rs2);
                        Emit(shop_shr, Reg(reg_sr_T), Reg(reg_sr_T), Imm(31));
                    }
                }
            }
            break;

        case Sh4DecodeMode.DM_ADC:
            {
                Emit(operation, rs1, rs1, rs2, 0, Reg(reg_sr_T), Reg(reg_sr_T));
            }
            break;

        case Sh4DecodeMode.DM_NEGC:
            Emit(operation, rs1, rs2, Reg(reg_sr_T), 0, default, Reg(reg_sr_T));
            break;

        default:
            Sh4HostRuntime.Verify(false);
            break;
        }

        return true;
    }

    private void SetupState(uint pc, Sh4Fpscr fpuConfiguration)
    {
        _state.Cpu.ProgramCounter = pc;
        _state.Cpu.IsDelaySlot = false;
        _state.Cpu.Fpr64 = fpuConfiguration.Pr;
        _state.Cpu.Fsz64 = fpuConfiguration.Sz;
        _state.Cpu.RoundToZero = fpuConfiguration.Rm == 1;
        _state.NextOperation = Sh4NextDecoderOperation.Next;
        _state.BlockType = BET_SCL_Intr;
        _state.JumpAddress = NullAddress;
        _state.NextAddress = NullAddress;
        _state.Info.HasReadMemory = false;
        _state.Info.HasWriteMemory = false;
        _state.Info.HasFpu = false;
    }

    internal void UpdateBlockCycles(Sh4RuntimeBlockInfo block, ushort opcode) =>
        block.GuestCycles += (uint)_cycles.CountCycles(opcode);

    internal bool DecodeBlock(Sh4RuntimeBlockInfo block, uint maxCycles)
    {
        _block = block;
        SetupState(block.VirtualAddress, block.FpuConfiguration);
        block.GuestOpcodes = 0;
        _cycles.Initialize(driver.Context);
        _cycles.Reset();

        uint maxPc = driver.Mmu.Enabled ? ((_state.Cpu.ProgramCounter >> 12) + 1) << 12 : uint.MaxValue;
        for (;;)
        {
            switch (_state.NextOperation)
            {
                case Sh4NextDecoderOperation.DelaySlot:
                    _state.NextOperation = _state.DelayOperation;
                    _state.Cpu.IsDelaySlot = true;
                    goto case Sh4NextDecoderOperation.Next;

                case Sh4NextDecoderOperation.Next:
                {
                    uint pc = _state.Cpu.ProgramCounter;
                    if ((block.Opcodes.Count >= BlockOpcodeSoftLimit || block.GuestCycles >= maxCycles || pc >= maxPc)
                        && !_state.Cpu.IsDelaySlot)
                    {
                        EndBlock(pc, BET_StaticJump, false);
                    }
                    else
                    {
                        ushort opcode = driver.ReadInstruction16(pc);
                        block.GuestOpcodes++;
                        UpdateBlockCycles(block, opcode);

                        var descriptor = driver.OpcodeTable[opcode];
                        if (!block.HasFpuOperation && descriptor.IsFloatingPoint)
                        {
                            if (driver.Context.StatusRegister.Fd)
                            {
                                driver.RaiseFpuDisabled(driver.Context.ProgramCounter);
                                return false;
                            }
                            block.HasFpuOperation = true;
                        }

                        if (_state.Cpu.IsDelaySlot && descriptor.SetsPc)
                            throw new InvalidOperationException("Fatal: SH4 branch instruction in delay slot");

                        if (descriptor.RecompilerOpcode is not { } decode)
                        {
                            if (!DecodeGeneric(opcode))
                            {
                                DecodeFallback(opcode);
                                if (descriptor.SetsPc)
                                {
                                    SetDynamicTarget(reg_nextpc);
                                    EndBlock(NullAddress, BET_DynamicJump, false);
                                }
                                else if (descriptor.SetsFpscr && !_state.Cpu.IsDelaySlot)
                                {
                                    EndBlock(pc + 2, BET_StaticJump, false);
                                }
                            }
                        }
                        else
                        {
                            decode(this, opcode);
                        }
                        _state.Cpu.ProgramCounter += 2;
                    }
                    break;
                }

                case Sh4NextDecoderOperation.End:
#if false
                    if (_state.BlockType == BET_StaticCall && block.ReadOnly)
                    {
                        if ((_state.JumpAddress >> 12) == (block.VirtualAddress >> 12)
                            || (_state.JumpAddress >> 12) == ((block.VirtualAddress + (block.GuestOpcodes - 1) * 2) >> 12))
                        {
                            ushort opcode = driver.ReadInstruction16(_state.JumpAddress);
                            if (opcode == 0x000b)
                            {
                                ushort delayOpcode = driver.ReadInstruction16(_state.JumpAddress + 2);
                                if (delayOpcode == 0x0000 || delayOpcode == 0x0009)
                                {
                                    _state.NextOperation = Sh4NextDecoderOperation.Next;
                                    _state.Cpu.IsDelaySlot = false;
                                    UpdateBlockCycles(block, opcode);
                                    UpdateBlockCycles(block, delayOpcode);
                                    continue;
                                }
                            }
                        }
                    }
#endif
                    goto End;
            }
        }

    End:
        block.GuestCodeSize = _state.Cpu.ProgramCounter - block.VirtualAddress;
        block.NextAddress = _state.NextAddress;
        block.BranchAddress = _state.JumpAddress;
        block.BlockType = _state.BlockType;

        Sh4HostRuntime.Verify(block.Opcodes.Count <= BlockOpcodeHardLimit);
        block.GuestCycles = (uint)MathF.Round(block.GuestCycles * 200f / Math.Max(1f, driver.Sh4Clock),
            MidpointRounding.AwayFromZero);
        block.GuestCycles = Math.Max(1u, block.GuestCycles);
        _block = null!;
        return true;
    }
}
#endif

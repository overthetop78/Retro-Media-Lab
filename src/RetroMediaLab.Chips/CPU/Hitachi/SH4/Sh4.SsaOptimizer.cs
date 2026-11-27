using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

using static Sh4BlockEndType;
using static Sh4DecoderConstants;
using static Sh4Register;
using static Sh4ShilOperation;
using static Sh4ShilCanonical;

#if !SH4_DYNAREC_NONE
internal sealed partial class Sh4SsaOptimizer
{
    internal void Optimize()
    {
        AddVersionPass();
#if false
        Trace.TraceInformation("BEFORE");
        PrintBlock();
#endif

        PropagateConstants();
        RemoveDeadCode();
        SimplifyExpressions();
        CombineShifts();
        RemoveDeadRegisters();
        RemoveIdentityMoves();
        SingleBranchTargetPass();

#if false
        if (_stats.PropagatedConstants > 0 || _stats.DeadCodeOperations > 0 || _stats.ConstantOperationsReplaced > 0
            || _stats.DeadRegisters > 0 || _stats.DynamicToStaticBlocks > 0 || _stats.WriteAfterWriteBlocks > 0
            || _stats.CombinedShifts > 0)
        {
            Trace.TraceInformation($"STATS: {_block.VirtualAddress:x8} ops {_block.Opcodes.Count} constants {_stats.PropagatedConstants} constops replaced {_stats.ConstantOperationsReplaced} dead code {_stats.DeadCodeOperations} dead regs {_stats.DeadRegisters} dyn2stat blks {_stats.DynamicToStaticBlocks} waw {_stats.WriteAfterWriteBlocks} shifts {_stats.CombinedShifts}");
        }
#endif
    }

    internal void AddVersionPass()
    {
        Array.Clear(_registerVersions);
        foreach (var opcode in _block.Opcodes)
        {
            AddVersion(ref opcode.Source1, define: false);
            AddVersion(ref opcode.Source2, define: false);
            AddVersion(ref opcode.Source3, define: false);
            AddVersion(ref opcode.Destination, define: true);
            AddVersion(ref opcode.Destination2, define: true);
        }
    }

    private void InsertMove(Sh4ShilParameter destination, Sh4ShilParameter source)
    {
        var original = _block.Opcodes[_operationIndex];
        _block.Opcodes.Insert(++_operationIndex, new Sh4ShilOpcode
        {
            Operation = shop_mov32,
            Destination = destination,
            Source1 = source,
            Size = original.Size,
            HostOffset = original.HostOffset,
            GuestOffset = original.GuestOffset,
            DelaySlot = original.DelaySlot
        });
    }

    private unsafe bool ExecuteConstantOperation(Sh4ShilOpcode opcode)
    {
        if (opcode.Source1.IsRegister || opcode.Source2.IsRegister || opcode.Source3.IsRegister
            || !(opcode.Source1.IsImmediate || opcode.Source2.IsImmediate || opcode.Source3.IsImmediate)
            || !opcode.Destination.IsRegister)
            return false;

        uint first = opcode.Source1.IsImmediate ? opcode.Source1.ImmediateValue : 0;
        uint second = opcode.Source2.IsImmediate ? opcode.Source2.ImmediateValue : 0;
        uint third = opcode.Source3.IsImmediate ? opcode.Source3.ImmediateValue : 0;
        uint result;
        switch (opcode.Operation)
        {
            case shop_adc:
            case shop_sbc:
            case shop_negc:
            case shop_rocl:
            case shop_rocr:
            case shop_mul_u64:
            case shop_mul_s64:
                ulong wideResult = opcode.Operation switch
                {
                    shop_adc => Evaluate_adc(first, second, third),
                    shop_sbc => Evaluate_sbc(first, second, third),
                    shop_rocl => Evaluate_rocl(first, second),
                    shop_rocr => Evaluate_rocr(first, second),
                    shop_mul_u64 => Evaluate_mul_u64(first, second),
                    shop_mul_s64 => Evaluate_mul_s64(first, second),
                    _ => Evaluate_negc(first, second)
                };
                result = (uint)wideResult;
                var highDestination = new Sh4ShilParameter(opcode.Destination2.Register);
                highDestination.SetVersion(0, opcode.Destination2.GetVersion(0));
                InsertMove(highDestination, new((uint)(wideResult >> 32)));
                opcode.Destination2 = default;
                break;

            case shop_div32u:
            case shop_div32s:
                ulong division = opcode.Operation == shop_div32u
                    ? Evaluate_div32u(first, second, third)
                    : Evaluate_div32s(first, unchecked((int)second), unchecked((int)third));
                result = (uint)division;
                InsertHighResult(opcode.Destination, (uint)(division >> 32));
                break;

            case shop_jdyn:
                Sh4HostRuntime.Verify(GetBlockClass(_block.BlockType) == BET_CLS_Dynamic);
                uint target = unchecked(first + second);
                switch ((Sh4BlockEndType)(((int)_block.BlockType >> 1) & 3))
                {
                    case BET_SCL_Jump:
                    case BET_SCL_Ret:
                        _block.BlockType = BET_StaticJump;
                        _block.BranchAddress = target;
                        break;
                    case BET_SCL_Call:
                        _block.BlockType = BET_StaticCall;
                        _block.BranchAddress = target;
                        break;
                    case BET_SCL_Intr:
                        _block.BlockType = BET_StaticIntr;
                        _block.NextAddress = target;
                        break;
                    default:
                        Sh4HostRuntime.Die("Unexpected block end type\n");
                        break;
                }
                _block.Opcodes.RemoveAt(_operationIndex--);
                _stats.DynamicToStaticBlocks++;
                return true;

            case shop_jcond:
                if (first != ((uint)_block.BlockType & 1))
                    _block.BranchAddress = _block.NextAddress;
                _block.BlockType = BET_StaticJump;
                _block.NextAddress = NullAddress;
                _block.HasConditionalJump = false;
                _block.Opcodes.RemoveAt(_operationIndex--);
                _stats.DynamicToStaticBlocks++;
                return true;

            case shop_fsca:
                float* pair = stackalloc float[2];
                Evaluate_fsca(pair, first);
                result = BitConverter.SingleToUInt32Bits(pair[0]);
                InsertHighResult(opcode.Destination, BitConverter.SingleToUInt32Bits(pair[1]));
                opcode.Destination = ChangeFormat(opcode.Destination, Sh4ShilParameterType.FMT_F32);
                break;

            default:
                float firstFloat = BitConverter.UInt32BitsToSingle(first);
                float secondFloat = BitConverter.UInt32BitsToSingle(second);
                float thirdFloat = BitConverter.UInt32BitsToSingle(third);
                uint? evaluated = opcode.Operation switch
                {
                    shop_mov32 => first,
                    shop_add => Evaluate_add(first, second),
                    shop_sub => Evaluate_sub(first, second),
                    shop_shl => Evaluate_shl(first, second),
                    shop_shr => Evaluate_shr(first, second),
                    shop_sar => Evaluate_sar(first, second),
                    shop_ror => Evaluate_ror(first, second),
                    shop_shld => Evaluate_shld(first, second),
                    shop_shad => Evaluate_shad(unchecked((int)first), second),
                    shop_or => Evaluate_or(first, second),
                    shop_and => Evaluate_and(first, second),
                    shop_xor => Evaluate_xor(first, second),
                    shop_not => Evaluate_not(first),
                    shop_ext_s16 => Evaluate_ext_s16(first),
                    shop_ext_s8 => Evaluate_ext_s8(first),
                    shop_mul_i32 => Evaluate_mul_i32(first, second),
                    shop_mul_u16 => Evaluate_mul_u16(first, second),
                    shop_mul_s16 => Evaluate_mul_s16(first, second),
                    shop_test => Evaluate_test(first, second),
                    shop_neg => Evaluate_neg(first),
                    shop_swaplb => Evaluate_swaplb(first),
                    shop_seteq => Evaluate_seteq(first, second),
                    shop_setgt => Evaluate_setgt(first, second),
                    shop_setge => Evaluate_setge(first, second),
                    shop_setab => Evaluate_setab(first, second),
                    shop_setae => Evaluate_setae(first, second),
                    shop_setpeq => Evaluate_setpeq(first, second),
                    shop_xtrct => Evaluate_xtrct(first, second),
                    shop_div32p2 => Evaluate_div32p2(unchecked((int)first), unchecked((int)second), third),
                    shop_fneg => BitConverter.SingleToUInt32Bits(Evaluate_fneg(firstFloat)),
                    shop_fadd => BitConverter.SingleToUInt32Bits(Evaluate_fadd(firstFloat, secondFloat)),
                    shop_fsub => BitConverter.SingleToUInt32Bits(Evaluate_fsub(firstFloat, secondFloat)),
                    shop_fmul => BitConverter.SingleToUInt32Bits(Evaluate_fmul(firstFloat, secondFloat)),
                    shop_fdiv => BitConverter.SingleToUInt32Bits(Evaluate_fdiv(firstFloat, secondFloat)),
                    shop_cvt_i2f_n => BitConverter.SingleToUInt32Bits(Evaluate_cvt_i2f_n(first)),
                    shop_cvt_i2f_z => BitConverter.SingleToUInt32Bits(Evaluate_cvt_i2f_z(first)),
                    shop_fsqrt => BitConverter.SingleToUInt32Bits(Evaluate_fsqrt(firstFloat)),
                    shop_cvt_f2i_t => Evaluate_cvt_f2i_t(firstFloat),
                    shop_fsrra => BitConverter.SingleToUInt32Bits(Evaluate_fsrra(firstFloat)),
                    shop_fabs => BitConverter.SingleToUInt32Bits(Evaluate_fabs(firstFloat)),
                    shop_fmac => BitConverter.SingleToUInt32Bits(Evaluate_fmac(firstFloat, secondFloat, thirdFloat)),
                    shop_fseteq => Evaluate_fseteq(firstFloat, secondFloat),
                    shop_fsetgt => Evaluate_fsetgt(firstFloat, secondFloat),
                    _ => null
                };
                if (evaluated.HasValue)
                {
                    result = evaluated.Value;
                }
                else
                {
                    Trace.TraceError($"unhandled constant op {(int)opcode.Operation}");
                    Sh4HostRuntime.Die("unhandled constant op");
                    Unsafe.SkipInit(out result);
                }
                break;
        }

        _constantValues[new RegisterValue(opcode.Destination)] = result;
        if (opcode.Destination2.Is32)
            _constantValues[new RegisterValue(opcode.Destination2)] = 0;
        ReplaceByMove(opcode, result);
        return true;
    }

    private void InsertHighResult(Sh4ShilParameter destination, uint value)
    {
        _constantValues[new RegisterValue(destination, 1)] = value;
        var high = new Sh4ShilParameter(destination.Register + 1);
        high.SetVersion(0, destination.GetVersion(1));
        InsertMove(high, new(value));
    }

    private unsafe void PropagateConstants()
    {
        for (_operationIndex = 0; _operationIndex < _block.Opcodes.Count; _operationIndex++)
        {
            var opcode = _block.Opcodes[_operationIndex];
            var operation = opcode.Operation;
            if (operation is not (shop_setab or shop_setae or shop_setgt or shop_setge or shop_sub or shop_fsetgt
                or shop_fseteq or shop_fdiv or shop_fsub or shop_fmac))
                PropagateOperand(ref opcode.Source1);
            if (operation is not (shop_rocr or shop_rocl or shop_fsetgt or shop_fseteq or shop_fmac))
                PropagateOperand(ref opcode.Source2);
            if (operation is not (shop_fmac or shop_adc))
                PropagateOperand(ref opcode.Source3);

            if (operation == shop_ifb)
            {
                _constantValues.Clear();
            }
            else if (operation is shop_sync_sr or shop_div1 or shop_sync_fpscr)
            {
                foreach (var key in _constantValues.Keys.ToArray())
                {
                    var register = key.Register;
                    bool invalidated = operation switch
                    {
                        shop_sync_sr => register is reg_sr_status or >= reg_r0 and <= reg_r7 or >= reg_r0_Bank and <= reg_r7_Bank,
                        shop_div1 => register == reg_sr_status,
                        _ => register is reg_fpscr or reg_old_fpscr or >= reg_fr_0 and <= reg_xf_15
                    };
                    if (invalidated)
                        _constantValues.Remove(key);
                }
            }
            else if (operation is shop_readm or shop_writem)
            {
                if (opcode.Source1.IsImmediate && !opcode.Source3.IsRegister)
                {
                    if (opcode.Source3.IsImmediate)
                    {
                        opcode.Source1.ReplaceValue(Sh4ShilParameterType.FMT_IMM,
                            unchecked(opcode.Source1.Immediate + opcode.Source3.ImmediateValue));
                        opcode.Source3.ReplaceValue(Sh4ShilParameterType.FMT_NULL, opcode.Source3.Immediate);
                    }
                    uint address = opcode.Source1.Immediate;
                    if (operation == shop_readm && _block.ReadOnly
                        && (address >> 12) >= (_block.VirtualAddress >> 12)
                        && (address >> 12) <= ((_block.VirtualAddress + _block.GuestCodeSize - 1) >> 12)
                        && opcode.Size <= 4
                        && Driver.TryReadMemoryImmediate(address, (int)opcode.Size, out nuint pointer, out bool isRam,
                            out _, _block) && isRam)
                    {
                        uint value;
                        switch (opcode.Size)
                        {
                            case 1:
                                value = unchecked((uint)*(sbyte*)pointer);
                                break;
                            case 2:
                                value = unchecked((uint)*(short*)pointer);
                                break;
                            case 4:
                                value = *(uint*)pointer;
                                break;
                            default:
                                Sh4HostRuntime.Die("invalid size");
                                value = 0;
                                break;
                        }
                        ReplaceByMove(opcode, value);
                        _constantValues[new RegisterValue(opcode.Destination)] = value;
                    }
                }
                else
                {
                    if (opcode.Source1.IsImmediate && opcode.Source3.IsRegister)
                        (opcode.Source1, opcode.Source3) = (opcode.Source3, opcode.Source1);
                    if (opcode.Source3.IsImmediate && opcode.Source3.ImmediateValue == 0)
                        opcode.Source3.ReplaceValue(Sh4ShilParameterType.FMT_NULL, opcode.Source3.Immediate);
                }
            }
            else if (!ExecuteConstantOperation(opcode))
            {
                if (operation is shop_and or shop_or or shop_xor or shop_add or shop_mul_s16 or shop_mul_u16
                    or shop_mul_i32 or shop_test or shop_seteq or shop_fseteq or shop_fadd or shop_fmul
                    or shop_mul_u64 or shop_mul_s64 or shop_adc or shop_setpeq)
                {
                    if (opcode.Source1.IsImmediate && opcode.Source2.IsRegister)
                        (opcode.Source1, opcode.Source2) = (opcode.Source2, opcode.Source1);
                }
                else if (operation is shop_shld or shop_shad && opcode.Source2.IsImmediate)
                {
                    uint shift = opcode.Source2.ImmediateValue;
                    if (shift == 0)
                        continue;
                    if ((shift & 31) == 0 && operation == shop_shld)
                    {
                        ReplaceByMove(opcode, 0);
                        continue;
                    }
                    if ((shift & 31) == 0)
                    {
                        opcode.Operation = shop_sar;
                        opcode.Source2.ReplaceValue(Sh4ShilParameterType.FMT_IMM, 31);
                    }
                    else if ((shift & 0x80000000) == 0)
                    {
                        opcode.Operation = shop_shl;
                        opcode.Source2.ReplaceValue(Sh4ShilParameterType.FMT_IMM, shift & 31);
                    }
                    else
                    {
                        opcode.Operation = operation == shop_shad ? shop_sar : shop_shr;
                        opcode.Source2.ReplaceValue(Sh4ShilParameterType.FMT_IMM, (~shift & 31) + 1);
                    }
                    _stats.ConstantOperationsReplaced++;
                }
            }
        }
    }

    private void RemoveDeadCode()
    {
        var lastVersions = new uint[(int)sh4_reg_count];
        var uses = new HashSet<RegisterValue>();
        Array.Fill(lastVersions, uint.MaxValue);

        for (int index = _block.Opcodes.Count - 1; index >= 0; index--)
        {
            var opcode = _block.Opcodes[index];
            var operation = opcode.Operation;
            bool dead = false;
            if (operation == shop_ifb || (Driver.Mmu.Enabled && operation is shop_readm or shop_writem))
            {
                Array.Fill(lastVersions, uint.MaxValue);
                continue;
            }
            if (operation == shop_pref)
            {
                if (opcode.Source1.IsImmediate && (opcode.Source1.ImmediateValue & 0xFC000000) != 0xE0000000)
                    dead = true;
                else if (Driver.Mmu.Enabled)
                {
                    Array.Fill(lastVersions, uint.MaxValue);
                    continue;
                }
            }
            if (operation == shop_sync_sr)
            {
                lastVersions[(int)reg_sr_T] = lastVersions[(int)reg_sr_status] = uint.MaxValue;
                Array.Fill(lastVersions, uint.MaxValue, (int)reg_r0, 8);
                Array.Fill(lastVersions, uint.MaxValue, (int)reg_r0_Bank, 8);
                continue;
            }
            if (operation == shop_div1)
                lastVersions[(int)reg_sr_status] = uint.MaxValue;
            if (operation == shop_sync_fpscr)
            {
                lastVersions[(int)reg_fpscr] = lastVersions[(int)reg_old_fpscr] = uint.MaxValue;
                Array.Fill(lastVersions, uint.MaxValue, (int)reg_fr_0, (int)(reg_xf_15 - reg_fr_0) + 1);
                continue;
            }

            if (opcode.Destination.IsRegister)
                dead |= IsUnusedDestination(opcode.Destination, lastVersions, uses);
            if (opcode.Destination2.IsRegister)
                dead &= IsUnusedDestination(opcode.Destination2, lastVersions, uses);

            if (dead && operation != shop_readm)
            {
                _block.Opcodes.RemoveAt(index);
                _stats.DeadCodeOperations++;
            }
            else
            {
                RecordUses(opcode.Source1, uses);
                RecordUses(opcode.Source2, uses);
                RecordUses(opcode.Source3, uses);
            }
        }
    }

    private bool IsUnusedDestination(Sh4ShilParameter destination, uint[] lastVersions, HashSet<RegisterValue> uses)
    {
        bool unused = true;
        for (int i = 0; i < destination.Count; i++)
        {
            var value = new RegisterValue(destination, i);
            int register = (int)value.Register;
            if (lastVersions[register] == uint.MaxValue)
            {
                lastVersions[register] = value.Version;
                unused = false;
                _writeBackValues.Add(value);
            }
            else
            {
                Sh4HostRuntime.Verify(value.Version < lastVersions[register]);
                if (uses.Contains(value))
                    unused = false;
            }
        }
        return unused;
    }

    private static void RecordUses(Sh4ShilParameter source, HashSet<RegisterValue> uses)
    {
        if (source.IsRegister)
        {
            for (int i = 0; i < source.Count; i++)
                uses.Add(new RegisterValue(source, i));
        }
    }

    private void SimplifyExpressions()
    {
        foreach (var opcode in _block.Opcodes)
        {
            var operation = opcode.Operation;
            if (opcode.Source2.IsImmediate || opcode.Source2.IsNull)
            {
                if (opcode.Source2.IsNull || opcode.Source2.ImmediateValue == 0)
                {
                    if (operation is shop_and or shop_mul_i32 or shop_mul_s16 or shop_mul_u16)
                        ReplaceByMove(opcode, 0);
                    else if (operation is shop_add or shop_sub or shop_or or shop_xor or shop_shl or shop_shr
                        or shop_sar or shop_shad or shop_shld)
                        ReplaceByMove(opcode);
                }
                else if (opcode.Source2.ImmediateValue == 1 && operation is shop_mul_i32 or shop_mul_s16 or shop_mul_u16)
                {
                    ReplaceByMove(opcode);
                }
            }
            else if (opcode.Source1.IsInteger32 && opcode.Source1.Register == opcode.Source2.Register)
            {
                switch (operation)
                {
                    case shop_add:
                        opcode.Operation = shop_shl;
                        opcode.Source2 = new(1u);
                        break;
                    case shop_xor:
                    case shop_sub:
                        ReplaceByMove(opcode, 0);
                        break;
                    case shop_sbc:
                        opcode.Source1 = opcode.Source2 = new(0u);
                        _stats.PropagatedConstants += 2;
                        break;
                    case shop_and:
                    case shop_or:
                        ReplaceByMove(opcode);
                        break;
                }
            }
        }
    }

    private void RemoveDeadRegisters()
    {
        var aliases = new SortedDictionary<RegisterValue, RegisterValue>();
        foreach (var opcode in _block.Opcodes)
        {
            if (opcode.Operation == shop_mov32 && opcode.Source1.IsRegister
                && opcode.Destination.IsInteger32 == opcode.Source1.IsInteger32)
            {
                var source = new RegisterValue(opcode.Source1);
                aliases[new RegisterValue(opcode.Destination)] = aliases.GetValueOrDefault(source, source);
            }
        }

        foreach (var (value, alias) in aliases)
        {
            if (_writeBackValues.Contains(value))
                continue;

            int definition = -1;
            int lastUse = -1;
            int redefinition = -1;
            for (int i = 0; i < _block.Opcodes.Count; i++)
            {
                var opcode = _block.Opcodes[i];
                if ((opcode.Destination.Is32 && new RegisterValue(opcode.Destination) == value)
                    || (opcode.Destination2.Is32 && new RegisterValue(opcode.Destination2) == value))
                    definition = i;

                if (redefinition == -1 && (DefinesHigherVersion(opcode.Destination, alias)
                    || DefinesHigherVersion(opcode.Destination2, alias) || opcode.Operation == shop_ifb))
                    redefinition = i;

                var operand = UsesRegisterValue(opcode.Source1, value) ? opcode.Source1
                    : UsesRegisterValue(opcode.Source2, value) ? opcode.Source2
                    : UsesRegisterValue(opcode.Source3, value) ? opcode.Source3 : default;
                if (!operand.IsRegister)
                    continue;
                if (operand.Count != 1)
                {
                    lastUse = 0xFFFF;
                    redefinition = 0;
                    break;
                }
                lastUse = i;
            }

            Sh4HostRuntime.Verify(definition != -1);
            if (redefinition != -1 && lastUse != -1 && redefinition < lastUse)
                continue;
            for (int i = definition + 1; i <= lastUse; i++)
            {
                var opcode = _block.Opcodes[i];
                ReplaceByAlias(ref opcode.Source1, value, alias);
                ReplaceByAlias(ref opcode.Source2, value, alias);
                ReplaceByAlias(ref opcode.Source3, value, alias);
            }
            _stats.DeadRegisters++;
            _block.Opcodes.RemoveAt(definition);
        }
    }

    private void RemoveIdentityMoves()
    {
        for (int index = 0; index < _block.Opcodes.Count; index++)
        {
            var opcode = _block.Opcodes[index];
            if (opcode.Operation == shop_mov32 && opcode.Source1.IsRegister
                && opcode.Destination.Register == opcode.Source1.Register)
            {
                _block.Opcodes.RemoveAt(index--);
                _stats.DeadCodeOperations++;
            }
        }
    }

    private void CombineShifts()
    {
        for (int index = 0; index < _block.Opcodes.Count - 1; index++)
        {
            var opcode = _block.Opcodes[index];
            var next = _block.Opcodes[index + 1];
            if (opcode.Operation == next.Operation && opcode.Operation is shop_shl or shop_shr or shop_sar
                && next.Source1.IsInteger32 && opcode.Destination.Register == next.Source1.Register)
            {
                uint shift = unchecked(next.Source2.Immediate + opcode.Source2.Immediate);
                if (shift <= 31)
                {
                    next.Source2.ReplaceValue(next.Source2.Type, shift);
                    ReplaceByMove(opcode);
                    _stats.CombinedShifts++;
                }
            }
        }
    }

    private void WriteAfterWritePass()
    {
        for (int index = 0; index < _block.Opcodes.Count - 1; index++)
        {
            var opcode = _block.Opcodes[index];
            var next = _block.Opcodes[index + 1];
            if (opcode.Operation == shop_writem && next.Operation == shop_writem
                && opcode.Source1.Type == next.Source1.Type
                && opcode.Source1.Immediate == next.Source1.Immediate
                && opcode.Source1.GetVersion(0) == next.Source1.GetVersion(0)
                && opcode.Source3.Type == next.Source3.Type
                && opcode.Source3.Immediate == next.Source3.Immediate
                && opcode.Source3.GetVersion(0) == next.Source3.GetVersion(0)
                && opcode.Source2.Count == next.Source2.Count)
            {
                _block.Opcodes.RemoveAt(index--);
                _stats.DeadCodeOperations++;
                _stats.WriteAfterWriteBlocks++;
            }
        }
    }

    private bool SkipSingleBranchTarget(ref uint address, bool updateCycles)
    {
        if (address == NullAddress)
            return false;
        bool success = false;
        uint startPage = _block.VirtualAddress >> 12;
        uint endPage = (_block.VirtualAddress + (_block.GuestOpcodes - 1) * 2) >> 12;
        for (int i = 0; i < 5; i++)
        {
            if ((address >> 12) < startPage || ((address + 2) >> 12) > endPage)
                break;
            ushort opcode = Driver.ReadInstruction16(address);
            if ((opcode & 0xF000) != 0xA000)
                break;
            ushort delayOpcode = Driver.ReadInstruction16(address + 2);
            if (delayOpcode is not (0x0000 or 0x0009))
                break;
            int displacement = GetSImm12(opcode) * 2 + 4;
            if (displacement == 0)
                break;
            address = unchecked(address + (uint)displacement);
            if (updateCycles)
            {
                Driver.Decoder.UpdateBlockCycles(_block, opcode);
                Driver.Decoder.UpdateBlockCycles(_block, delayOpcode);
            }
            success = true;
        }
        return success;
    }
}
#endif

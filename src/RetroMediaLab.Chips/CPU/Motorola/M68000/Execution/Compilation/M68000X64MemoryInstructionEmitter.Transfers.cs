using RetroMediaLab.Chips.Common.Compilation.X64;
using System.Numerics;
using R = RetroMediaLab.Chips.Common.Compilation.X64.X64Register;
using W = RetroMediaLab.Chips.Common.Compilation.X64.X64Width;
using A = RetroMediaLab.Chips.Common.Compilation.X64.X64Arithmetic;
using O = RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation.M68000NativeOperation;
using M = RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation.M68000OperandMode;

namespace RetroMediaLab.Chips.CPU.Motorola.M68000.Execution.Compilation;

internal sealed partial class M68000X64MemoryInstructionEmitter
{
    // BlastEm m68k_core.c: translate_m68k_{jmp_jsr,link,unlk,movem}.
    // The register list and EA arithmetic are specialized at translation time.
    private static bool CanEmitTransfer(in M68000DecodedInstruction instruction) =>
        instruction.Status == M68000DecodeStatus.Decoded && instruction.Operation is
            O.LoadEffectiveAddress or O.PushEffectiveAddress or O.Jump or O.JumpSubroutine or O.Link or O.Unlink or O.MoveMultiple or O.MovePeripheral;

    private void PreflightTransfer(in M68000DecodedInstruction instruction, bool addressErrors, List<int> denied)
    {
        var operation = instruction.Operation;
        if (operation == O.MovePeripheral)
        {
            bool load = Memory(instruction.Source);
            var operand = load ? instruction.Source : instruction.Destination;
            Address(operand with { Displacement = unchecked((short)operand.Displacement) }, M68000OperandSize.Long, instruction.Address, false);
            code.Move(R.R8, R.R14, W.Dword); Guard(R.R8, M68000OperandSize.Byte, !load, addressErrors, denied);
            code.Move(R.R14, R.R8, W.Dword); code.BinaryImmediate(A.Add, instruction.Size == M68000OperandSize.Word ? 2 : 6, R.R8, W.Dword);
            Guard(R.R8, M68000OperandSize.Byte, !load, addressErrors, denied); return;
        }
        if (operation is O.LoadEffectiveAddress or O.Jump) return;
        if (operation == O.MoveMultiple)
        {
            bool load = Memory(instruction.Source);
            var operand = load ? instruction.Source : instruction.Destination;
            ushort mask = (ushort)(load ? instruction.Destination.Immediate : instruction.Source.Immediate);
            var size = instruction.Size!.Value;
            int step = size == M68000OperandSize.Word ? 2 : 4;
            int count = BitOperations.PopCount(mask);
            bool descending = operand.Mode == M.PreDecrement;
            if (size == M68000OperandSize.Long && _configuration.RefreshSkipAdvance != 0)
                new M68000X64RefreshEmitter(code).Guard(denied);
            if (count == 0 && !load) return;
            Address(operand with { Mode = operand.Mode is M.PreDecrement or M.PostIncrement ? M.Indirect : operand.Mode }, size, instruction.Address, false);
            if (descending) code.BinaryImmediate(A.Subtract, step * count, R.R8, W.Dword);
            code.Move(R.R8, R.R14, W.Dword);
            var guardSize = count == 0 ? M68000OperandSize.Word : size;
            Guard(R.R8, guardSize, !load, addressErrors, denied);
            // At most 64 contiguous bytes plus the final read: two pages at
            // most. Check both endpoints instead of guarding every register.
            if (count > 1 || load)
            {
                code.Move(R.R14, R.R8, W.Dword);
                code.BinaryImmediate(A.Add, step * (load ? count : count - 1), R.R8, W.Dword);
                Guard(R.R8, load ? M68000OperandSize.Word : size, !load, addressErrors, denied);
            }
            return;
        }
        if (operation == O.Unlink)
            _context.LoadRegister(8 + instruction.Destination.Register, R.R8, M68000OperandSize.Long);
        else
        {
            _context.LoadRegister(15, R.R8, M68000OperandSize.Long);
            code.BinaryImmediate(A.Subtract, 4, R.R8, W.Dword);
        }
        Guard(R.R8, M68000OperandSize.Long, operation != O.Unlink, addressErrors, denied);
    }

    private void EmitTransfer(in M68000DecodedInstruction instruction, int nextOffset)
    {
        switch (instruction.Operation)
        {
            case O.MovePeripheral: EmitMovePeripheral(instruction); return;
            case O.MoveMultiple: EmitMoveMultiple(instruction); return;
            case O.Link:
                _context.LoadRegister(15, R.R15, M68000OperandSize.Long);
                code.BinaryImmediate(A.Subtract, 4, R.R15, W.Dword);
                _context.StoreRegister(15, R.R15, M68000OperandSize.Long);
                _context.LoadRegister(8 + instruction.Source.Register, R.R10, M68000OperandSize.Long);
                Write(M68000OperandSize.Long);
                CompleteInstructionFetch(instruction.NextAddress, nextOffset);
                _context.StoreRegister(8 + instruction.Source.Register, R.R15, M68000OperandSize.Long);
                code.BinaryImmediate(A.Add, unchecked((int)instruction.Destination.Immediate), R.R15, W.Dword);
                _context.StoreRegister(15, R.R15, M68000OperandSize.Long); return;
            case O.Unlink:
                _context.LoadRegister(8 + instruction.Destination.Register, R.R15, M68000OperandSize.Long);
                Read(M68000OperandSize.Long);
                if (instruction.Destination.Register != 7)
                {
                    code.BinaryImmediate(A.Add, 4, R.R15, W.Dword);
                    _context.StoreRegister(15, R.R15, M68000OperandSize.Long);
                }
                _context.StoreRegister(8 + instruction.Destination.Register, R.R10, M68000OperandSize.Long); return;
        }
        Address(instruction.Source, M68000OperandSize.Long, instruction.Address, false);
        if (instruction.Operation == O.LoadEffectiveAddress)
        {
            _context.StoreRegister(8 + instruction.Destination.Register, R.R8, M68000OperandSize.Long); return;
        }
        if (instruction.Operation == O.Jump)
        {
            _context.StoreState(M68000StateField.pc, R.R8); return;
        }
        code.Move(R.R8, R.R14, W.Dword);
        _context.LoadRegister(15, R.R15, M68000OperandSize.Long);
        code.BinaryImmediate(A.Subtract, 4, R.R15, W.Dword);
        _context.StoreRegister(15, R.R15, M68000OperandSize.Long);
        if (instruction.Operation == O.PushEffectiveAddress) code.Move(R.R14, R.R10, W.Dword);
        else code.MoveImmediate(instruction.NextAddress, R.R10, W.Dword);
        Write(M68000OperandSize.Long);
        if (instruction.Operation == O.JumpSubroutine) _context.StoreState(M68000StateField.pc, R.R14);
    }

    private void EmitMovePeripheral(in M68000DecodedInstruction instruction)
    {
        bool load = Memory(instruction.Source);
        var operand = load ? instruction.Source : instruction.Destination;
        var register = load ? instruction.Destination : instruction.Source;
        var size = instruction.Size!.Value;
        int bytes = size == M68000OperandSize.Word ? 2 : 4;
        Address(operand with { Displacement = unchecked((short)operand.Displacement) }, M68000OperandSize.Long, instruction.Address, false);
        code.Move(R.R8, R.R15, W.Dword);
        if (load) code.MoveImmediate(0, R.R14, W.Dword);
        else _context.LoadRegister(register.Register, R.R14, size);
        for (int part = 0; part < bytes; part++)
        {
            if (load)
            {
                Read(M68000OperandSize.Byte);
                code.Shift(X64Shift.Left, 8, R.R14, W.Dword); code.Binary(A.Or, R.R10, R.R14, W.Dword);
            }
            else
            {
                code.Move(R.R14, R.R10, W.Dword);
                int shift = (bytes - 1 - part) * 8;
                if (shift != 0) code.Shift(X64Shift.Right, (byte)shift, R.R10, W.Dword);
                Write(M68000OperandSize.Byte);
            }
            if (part + 1 < bytes) code.BinaryImmediate(A.Add, 2, R.R15, W.Dword);
        }
        if (load) _context.StoreRegister(register.Register, R.R14, size);
    }

    private void EmitMoveMultiple(in M68000DecodedInstruction instruction)
    {
        bool load = Memory(instruction.Source);
        var operand = load ? instruction.Source : instruction.Destination;
        ushort mask = (ushort)(load ? instruction.Destination.Immediate : instruction.Source.Immediate);
        var size = instruction.Size!.Value;
        int step = size == M68000OperandSize.Word ? 2 : 4;
        bool descending = operand.Mode == M.PreDecrement;
        Address(operand with { Mode = operand.Mode is M.PreDecrement or M.PostIncrement ? M.Indirect : operand.Mode }, size, instruction.Address, false);
        code.Move(R.R8, R.R15, W.Dword);
        for (int bit = 0; bit < 16; bit++)
        {
            if ((mask & (1 << bit)) == 0) continue;
            int register = descending ? 15 - bit : bit;
            if (descending) code.BinaryImmediate(A.Subtract, step, R.R15, W.Dword);
            if (load)
            {
                Read(size, operand.Mode is M.PcDisplacement or M.PcIndexedDisplacement);
                if (size == M68000OperandSize.Word) code.MoveExtend(R.R10, R.R10, W.Word, W.Dword, true);
                _context.StoreRegister(register, R.R10, M68000OperandSize.Long);
            }
            else
            {
                _context.LoadRegister(register, R.R10, size);
                Write(size, descending && size == M68000OperandSize.Long);
            }
            if (!descending) code.BinaryImmediate(A.Add, step, R.R15, W.Dword);
        }
        if (descending || operand.Mode == M.PostIncrement)
            _context.StoreRegister(8 + operand.Register, R.R15, M68000OperandSize.Long);
        // The final read is observable on handler-backed banks.
        if (load && busCallbacks) Read(M68000OperandSize.Word);
        int clocks = BitOperations.PopCount(mask) * (size == M68000OperandSize.Word ? 4 : 8);
        _context.AddStateImmediate(M68000StateField.cycles, unchecked((int)((long)(clocks * multiplier) * ratio >> shift)));
        if (size == M68000OperandSize.Long)
            new M68000X64RefreshEmitter(code).EmitSkip(_configuration.RefreshSkipAdvance);
    }
}


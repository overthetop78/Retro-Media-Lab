using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;
using F = RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Constants.Z80Flags;

namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Services;

public sealed partial class CpuZ80
{
    private int BlockDirection => (_opcode & 8) != 0 ? -1 : 1;

    private void BeginBlock(byte opcode)
    {
        _operation = (opcode & 3) switch
        {
            0 => CpuZ80Operation.BlockTransfer, 1 => CpuZ80Operation.BlockCompare,
            2 => CpuZ80Operation.BlockInput, _ => CpuZ80Operation.BlockOutput,
        };
        if ((_opcode & 2) != 0) BeginInternal(1, CpuZ80Flow.InternalBlockStart);
        else ReadMemory(_registers.HL, CpuZ80Flow.BlockReadMemory);
    }

    private void BeginBlockIo()
    {
        if (_operation == CpuZ80Operation.BlockInput)
        {
            _registers.WZ = unchecked((ushort)(_registers.BC + BlockDirection));
            _flow = CpuZ80Flow.BlockInput;
            _bus.BeginCycle(Z80BusCycle.IoRead, _registers.BC, 0);
        }
        else ReadMemory(_registers.HL, CpuZ80Flow.BlockReadMemory);
    }

    private void CompleteBlockRead(byte data)
    {
        _low = data;
        switch (_operation)
        {
            case CpuZ80Operation.BlockTransfer:
                WriteMemory(_registers.DE, data, CpuZ80Flow.BlockWriteMemory, holdData: true);
                break;
            case CpuZ80Operation.BlockCompare:
                BeginInternal(5, CpuZ80Flow.InternalBlockFinish);
                break;
            case CpuZ80Operation.BlockOutput:
                _registers.B = unchecked((byte)(_registers.B - 1));
                _registers.WZ = unchecked((ushort)(_registers.BC + BlockDirection));
                _flow = CpuZ80Flow.BlockOutput;
                _bus.BeginCycle(Z80BusCycle.IoWrite, _registers.BC, 0, data);
                break;
            default: throw new InvalidOperationException("Invalid Z80 block read.");
        }
    }

    private void CompleteBlockOperation()
    {
        int direction = BlockDirection;
        _registers.HL = unchecked((ushort)(_registers.HL + direction));
        switch (_operation)
        {
            case CpuZ80Operation.BlockTransfer:
                _bus.ReleaseWriteData();
                _registers.DE = unchecked((ushort)(_registers.DE + direction));
                _registers.BC = unchecked((ushort)(_registers.BC - 1));
                int sum = _registers.A + _low;
                WriteFlags((byte)((_registers.F & (F.Sign | F.Zero | F.Carry))
                    | (sum & 8) | ((sum & 2) << 4) | (_registers.BC != 0 ? F.ParityOverflow : 0)));
                break;
            case CpuZ80Operation.BlockCompare:
                Z80Arithmetic.Subtract8(_registers.A, _low, false, out byte comparedFlags);
                _registers.BC = unchecked((ushort)(_registers.BC - 1));
                _registers.WZ = unchecked((ushort)(_registers.WZ + direction));
                int adjusted = _registers.A - _low - ((comparedFlags & F.HalfCarry) != 0 ? 1 : 0);
                WriteFlags((byte)((comparedFlags & (F.Sign | F.Zero | F.HalfCarry | F.Subtract))
                    | (_registers.F & F.Carry) | (adjusted & 8) | ((adjusted & 2) << 4)
                    | (_registers.BC != 0 ? F.ParityOverflow : 0)));
                break;
            case CpuZ80Operation.BlockInput:
                _registers.B = unchecked((byte)(_registers.B - 1));
                SetBlockIoFlags(_low + unchecked((byte)(_registers.C + direction)));
                break;
            case CpuZ80Operation.BlockOutput:
                SetBlockIoFlags(_low + _registers.L);
                break;
            default: throw new InvalidOperationException("Invalid Z80 block operation.");
        }
        bool again = (_opcode & 0x10) != 0 && (_operation switch
        {
            CpuZ80Operation.BlockTransfer => _registers.BC != 0,
            CpuZ80Operation.BlockCompare => _registers.BC != 0 && (_registers.F & F.Zero) == 0,
            _ => _registers.B != 0,
        });
        if (again) BeginInternal(5, CpuZ80Flow.InternalBlockRepeat);
        else RetireInstruction();
    }

    private void SetBlockIoFlags(int sum)
    {
        WriteFlags((byte)((_registers.B & (F.Sign | F.ResultBits)) | (_registers.B == 0 ? F.Zero : 0)
            | ((_low & 0x80) != 0 ? F.Subtract : 0) | (sum > 255 ? F.HalfCarry | F.Carry : 0)
            | Parity((byte)((sum & 7) ^ _registers.B))));
    }

    private void RepeatBlock()
    {
        _registers.PC = unchecked((ushort)(_registers.PC - 2));
        _registers.WZ = unchecked((ushort)(_registers.PC + 1));
        int flags = (_registers.F & ~F.ResultBits) | ((_registers.PC >> 8) & F.ResultBits);
        if (_operation is CpuZ80Operation.BlockInput or CpuZ80Operation.BlockOutput)
        {
            int b = _registers.B;
            if ((flags & F.Carry) != 0)
            {
                bool negative = (_low & 0x80) != 0;
                flags ^= Parity((byte)((b + (negative ? -1 : 1)) & 7)) ^ F.ParityOverflow;
                flags = (flags & ~F.HalfCarry) | ((b & 15) == (negative ? 0 : 15) ? F.HalfCarry : 0);
            }
            else flags ^= Parity((byte)(b & 7)) ^ F.ParityOverflow;
        }
        WriteFlags((byte)flags);
        // Une nouvelle lecture ED/opcode est réelle. Cette frontière peut accepter
        // une interruption ; les itérations ne sont pas exécutées par une boucle hôte.
        RetireInstruction();
    }
}

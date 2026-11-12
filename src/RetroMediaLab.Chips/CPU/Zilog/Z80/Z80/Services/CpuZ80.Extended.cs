using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;
using F = RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Constants.Z80Flags;

namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Services;

public sealed partial class CpuZ80
{
    // UM008011-0816 pp.94–97. La prise INT applique son effet P/V
    // dans CpuZ80.Interrupts.cs avant la réponse matérielle.
    private void CompleteSpecialTransfer()
    {
        switch (_operation)
        {
            case CpuZ80Operation.LoadIFromAccumulator: _registers.I = _registers.A; return;
            case CpuZ80Operation.LoadRFromAccumulator: _registers.R = _registers.A; return;
            case CpuZ80Operation.LoadAccumulatorFromI: _registers.A = _registers.I; break;
            case CpuZ80Operation.LoadAccumulatorFromR: _registers.A = _registers.R; break;
            default: throw new InvalidOperationException("Invalid Z80 I/R transfer.");
        }
        WriteFlags((byte)((_registers.A & (F.Sign | F.ResultBits))
            | (_registers.A == 0 ? F.Zero : 0) | (_interrupts.Iff2 ? F.ParityOverflow : 0)
            | (_registers.F & F.Carry)));
    }

    private void BeginSpecialTransfer(CpuZ80Operation operation)
    {
        _operation = operation;
        BeginInternal(1, CpuZ80Flow.InternalOperation);
    }

    private void CompleteRegisterInput(byte data)
    {
        Z80Arithmetic.Or8(data, 0, out byte flags);
        WriteFlags((byte)(flags | (data & F.ResultBits) | (_registers.F & F.Carry)));
        if (_registerCode != 6) SetRegister(_registerCode, data);
        RetireInstruction();
    }

    private byte RotateDigits(byte data)
    {
        byte accumulator = _registers.A;
        byte result;
        if (_operation == CpuZ80Operation.RotateDigitsLeft)
        {
            result = unchecked((byte)((data << 4) | (accumulator & 0x0f)));
            _registers.A = (byte)((accumulator & 0xf0) | (data >> 4));
        }
        else
        {
            result = (byte)(((accumulator & 0x0f) << 4) | (data >> 4));
            _registers.A = (byte)((accumulator & 0xf0) | (data & 0x0f));
        }
        Z80Arithmetic.Or8(_registers.A, 0, out byte flags);
        WriteFlags((byte)(flags | (_registers.A & F.ResultBits) | (_registers.F & F.Carry)));
        return result;
    }
}

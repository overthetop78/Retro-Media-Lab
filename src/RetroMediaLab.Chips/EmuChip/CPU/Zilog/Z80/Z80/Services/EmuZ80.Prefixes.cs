using F = RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Constants.Z80Flags;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;

namespace RetroMediaLab.Chips.EmuChip.CPU.Zilog.Z80.Z80.Services;

public sealed partial class EmuZ80
{
    // UM008011-0816, rotations/décalages et SET/RES ; SLL : Young §3.1,
    // confirmé sur le motif 06 -> 0D par le relevé matériel Devic CB36.
    // Le second octet CB est un véritable M1 ; aucun retrait d'instruction
    // et aucune nouvelle acceptation d'interruption entre les deux octets.
    private void DecodeCb(byte opcode)
    {
        byte register = (byte)(opcode & 7);
        int group = opcode >> 6;
        if (group == 1)
        {
            if (register == 6) ReadMemory(_registers.HL, CpuZ80Flow.ReadBit);
            else { TestBit(GetRegister(register), GetRegister(register)); RetireInstruction(); }
            return;
        }
        if (register == 6)
        {
            _address = _registers.HL;
            _operation = CpuZ80Operation.CbMemory;
            ReadMemory(_address, CpuZ80Flow.ReadModifyWrite);
            return;
        }
        SetRegister(register, ModifyCb(GetRegister(register)));
        RetireInstruction();
    }

    private byte ModifyCb(byte value)
    {
        int group = _opcode >> 6, bit = (_opcode >> 3) & 7;
        if (group == 0)
        {
            value = Z80Arithmetic.RotateShift(value, bit, _registers.F, out byte flags);
            WriteFlags((byte)(flags | (value & F.ResultBits)));
        }
        else if (group == 2) value = (byte)(value & ~(1 << bit));
        else value = (byte)(value | (1 << bit));
        return value;
    }
}

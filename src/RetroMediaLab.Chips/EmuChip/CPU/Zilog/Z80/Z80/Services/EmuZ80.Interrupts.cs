using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;
using F = RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Constants.Z80Flags;

namespace RetroMediaLab.Chips.EmuChip.CPU.Zilog.Z80.Z80.Services;

public sealed partial class EmuZ80
{
    private bool TryBeginInterrupt()
    {
        var kind = _interrupts.AcceptAtBoundary(false, _control.SampledInt, _control.SampledNmi);
        if (kind == Z80InterruptKind.None) return false;
        _control.Halted = false;
        _control.PendingHalt = false;
        _control.InterruptSequence = true;
        _index = CpuZ80Index.None;
        // NMOS : l'acceptation d'INT après LD A,I/R efface P/V (UM0080, LD A,I/R).
        if (kind == Z80InterruptKind.Maskable && _profile.IrReadInterruptClearsParity && _operation is
            CpuZ80Operation.LoadAccumulatorFromI or CpuZ80Operation.LoadAccumulatorFromR)
            _registers.F &= unchecked((byte)~F.ParityOverflow);
        _operation = CpuZ80Operation.None;
        _instructionFlagsModified = false;
        _instructionAddress = _registers.PC;
        if (kind == Z80InterruptKind.NonMaskable) BeginIdleFetch(CpuZ80Flow.NmiFetch);
        else
        {
            _flow = CpuZ80Flow.InterruptAcknowledge;
            ushort refresh = (ushort)((_registers.I << 8) | _registers.R);
            _registers.IncrementRefresh();
            _bus.BeginCycle(Z80BusCycle.InterruptAcknowledge, _registers.PC, refresh);
        }
        return true;
    }

    private void CompleteInterruptAcknowledge()
    {
        switch (_interrupts.Mode)
        {
            case Z80InterruptMode.Mode0:
                _control.InterruptSequence = false; // l'opcode injecté est une véritable instruction.
                _opcode = _bus.SampledData;
                Decode(_opcode);
                break;
            case Z80InterruptMode.Mode1:
                _address = _registers.WZ = 0x38;
                BeginStackWrite(CpuZ80Operation.Restart, _registers.PC);
                break;
            case Z80InterruptMode.Mode2:
                _address = (ushort)((_registers.I << 8) | _bus.SampledData);
                BeginStackWrite(CpuZ80Operation.InterruptMode2, _registers.PC);
                break;
        }
    }

    private void CompleteInterruptStack()
    {
        if (_operation == CpuZ80Operation.InterruptMode2)
        {
            ReadMemory(_address, CpuZ80Flow.InterruptVectorLow);
            return;
        }
        _registers.PC = _registers.WZ = _address;
        _control.InterruptSequence = false;
        ContinueAtInstructionBoundary();
    }
}

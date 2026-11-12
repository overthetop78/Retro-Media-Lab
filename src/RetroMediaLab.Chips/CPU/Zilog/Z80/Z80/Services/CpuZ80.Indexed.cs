using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;

namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Services;

public sealed partial class CpuZ80
{
    // Une vue sur la paire réellement sélectionnée, pas une copie de HL/IX/IY.
    private ushort ActivePair
    {
        get => _index switch { CpuZ80Index.IX => _registers.IX, CpuZ80Index.IY => _registers.IY, _ => _registers.HL };
        set
        {
            switch (_index)
            {
                case CpuZ80Index.IX: _registers.IX = value; break;
                case CpuZ80Index.IY: _registers.IY = value; break;
                default: _registers.HL = value; break;
            }
        }
    }

    private void BeginIndexed(CpuZ80Operation operation)
    {
        _operation = operation;
        OperandRead(CpuZ80Flow.IndexDisplacement);
    }

    private void DecodeIndexedCb(byte opcode)
    {
        _registerCode = (byte)(opcode & 7);
        _operation = CpuZ80Operation.CbMemory;
        BeginInternal(2, CpuZ80Flow.InternalIndexCbRead);
    }

    private void CompleteDisplacement(byte displacement)
    {
        _address = unchecked((ushort)(ActivePair + unchecked((sbyte)displacement)));
        _registers.WZ = _address;
        if (_operation == CpuZ80Operation.IndexedImmediate) OperandRead(CpuZ80Flow.IndexImmediate);
        else BeginInternal(5, CpuZ80Flow.InternalIndexedAccess);
    }

    private void CompleteIndexedAccess()
    {
        // Les états internes précèdent l'accès, en conservant sur le bus
        // l'adresse de l'opérande précédent (chronogrammes matériels DD).
        switch (_operation)
        {
            case CpuZ80Operation.IndexedReadRegister:
                ReadMemory(_address, CpuZ80Flow.ReadIndexedRegister);
                break;
            case CpuZ80Operation.IndexedWriteRegister:
                WriteMemory(_address, GetRegister(_registerCode, substituteIndex: false));
                break;
            case CpuZ80Operation.IndexedImmediate:
                WriteMemory(_address, _low);
                break;
            case CpuZ80Operation.IndexedArithmetic:
                ReadMemory(_address, CpuZ80Flow.ReadArithmetic);
                break;
            case CpuZ80Operation.IncrementMemory:
            case CpuZ80Operation.DecrementMemory:
                ReadMemory(_address, CpuZ80Flow.ReadModifyWrite);
                break;
            default: throw new InvalidOperationException("Invalid Z80 indexed access.");
        }
    }
}

using C = RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Constants.Z80PinEncoding;
using L = RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Constants.Z80BusLines;

namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;

/// <summary>Snapshot immuable des sorties ; null signifie haute impédance.
/// Niveaux et pilotage partagent un mot, sans construire six Nullable par notification.</summary>
public readonly record struct Z80BusPins
{
    private readonly ulong _bits;
    internal ulong EncodedLevels => _bits;
    internal Z80BusPins(ulong bits) => _bits = bits;

    public Z80BusPins(ushort? Address, byte? Data, bool? MemoryRequestHigh, bool? IoRequestHigh,
        bool? ReadHigh, bool? WriteHigh, bool MachineCycleOneHigh, bool RefreshHigh,
        bool HaltHigh = true, bool BusAcknowledgeHigh = true)
    {
        this = default;
        this.Address = Address; this.Data = Data;
        this.MemoryRequestHigh = MemoryRequestHigh; this.IoRequestHigh = IoRequestHigh;
        this.ReadHigh = ReadHigh; this.WriteHigh = WriteHigh;
        this.MachineCycleOneHigh = MachineCycleOneHigh; this.RefreshHigh = RefreshHigh;
        this.HaltHigh = HaltHigh; this.BusAcknowledgeHigh = BusAcknowledgeHigh;
    }

    public ushort? Address
    {
        get => (_bits & C.AddressDriven) == 0 ? null : (ushort)_bits;
        init => _bits = WithEncodedOutput(L.Address, value.HasValue ? value.Value : -1)._bits;
    }
    public byte? Data
    {
        get => (_bits & C.DataDriven) == 0 ? null : (byte)(_bits >> C.DataShift);
        init => _bits = WithEncodedOutput(L.Data, value.HasValue ? value.Value : -1)._bits;
    }
    public bool? MemoryRequestHigh
    {
        get => (_bits & C.MemoryDriven) == 0 ? null : (_bits & C.MemoryHigh) != 0;
        init => _bits = WithEncodedOutput(L.Memory, value.HasValue ? value.Value ? 1 : 0 : -1)._bits;
    }
    public bool? IoRequestHigh
    {
        get => (_bits & C.IoDriven) == 0 ? null : (_bits & C.IoHigh) != 0;
        init => _bits = WithEncodedOutput(L.Io, value.HasValue ? value.Value ? 1 : 0 : -1)._bits;
    }
    public bool? ReadHigh
    {
        get => (_bits & C.ReadDriven) == 0 ? null : (_bits & C.ReadHigh) != 0;
        init => _bits = WithEncodedOutput(L.Read, value.HasValue ? value.Value ? 1 : 0 : -1)._bits;
    }
    public bool? WriteHigh
    {
        get => (_bits & C.WriteDriven) == 0 ? null : (_bits & C.WriteHigh) != 0;
        init => _bits = WithEncodedOutput(L.Write, value.HasValue ? value.Value ? 1 : 0 : -1)._bits;
    }
    public bool MachineCycleOneHigh
    {
        get => (_bits & C.M1High) != 0;
        init => _bits = WithEncodedOutput(L.M1, value ? 1 : 0)._bits;
    }
    public bool RefreshHigh
    {
        get => (_bits & C.RefreshHigh) != 0;
        init => _bits = WithEncodedOutput(L.Refresh, value ? 1 : 0)._bits;
    }
    public bool HaltHigh
    {
        get => (_bits & C.HaltHigh) != 0;
        init => _bits = WithEncodedOutput(L.Halt, value ? 1 : 0)._bits;
    }
    public bool BusAcknowledgeHigh
    {
        get => (_bits & C.BusAckHigh) != 0;
        init => _bits = WithEncodedOutput(L.BusAcknowledge, value ? 1 : 0)._bits;
    }

    // Même qualification que les niveaux typés, directement sur les fils reçus.
    public bool IsMemoryRead() => (_bits & (C.MemoryRead | C.MemoryHigh | C.ReadHigh)) == C.MemoryRead;
    public bool IsIoRead() => (_bits & (C.IoRead | C.IoHigh | C.ReadHigh)) == C.IoRead;
    public bool IsMemoryWrite() => (_bits & (C.MemoryWrite | C.MemoryHigh | C.WriteHigh)) == C.MemoryWrite;
    public bool IsIoWrite() => (_bits & (C.IoWrite | C.IoHigh | C.WriteHigh)) == C.IoWrite;
    public ushort GetAddressOrZero() => (ushort)_bits;
    public byte GetDataOrZero() => (byte)(_bits >> C.DataShift);
    public bool Equals(Z80BusPins other) => _bits == other._bits;
    public override int GetHashCode() => _bits.GetHashCode();

    internal int GetEncodedOutput(int line)
    {
        if (line == L.Address) return (_bits & C.AddressDriven) == 0 ? -1 : (ushort)_bits;
        if (line == L.Data) return (_bits & C.DataDriven) == 0 ? -1 : (byte)(_bits >> C.DataShift);
        return line < L.M1 && (_bits & (1UL << (line + C.DrivenShift))) == 0
            ? -1 : (int)((_bits >> (line + C.ControlShift)) & 1);
    }

    internal Z80BusPins WithEncodedOutput(int line, int target)
    {
        ulong mask, value;
        if (line == L.Address)
        {
            mask = C.AddressMask | C.AddressDriven;
            value = target < 0 ? 0 : (ulong)(ushort)target | C.AddressDriven;
        }
        else if (line == L.Data)
        {
            mask = C.DataMask | C.DataDriven;
            value = target < 0 ? 0 : ((ulong)(byte)target << C.DataShift) | C.DataDriven;
        }
        else
        {
            mask = 1UL << (line + C.ControlShift);
            value = target > 0 ? mask : 0;
            if (line < L.M1)
            {
                ulong driven = 1UL << (line + C.DrivenShift);
                mask |= driven;
                if (target >= 0) value |= driven;
            }
        }
        return new((_bits & ~mask) | value);
    }

    public void Deconstruct(out ushort? Address, out byte? Data, out bool? MemoryRequestHigh,
        out bool? IoRequestHigh, out bool? ReadHigh, out bool? WriteHigh, out bool MachineCycleOneHigh,
        out bool RefreshHigh, out bool HaltHigh, out bool BusAcknowledgeHigh)
    {
        Address = this.Address; Data = this.Data; MemoryRequestHigh = this.MemoryRequestHigh;
        IoRequestHigh = this.IoRequestHigh; ReadHigh = this.ReadHigh; WriteHigh = this.WriteHigh;
        MachineCycleOneHigh = this.MachineCycleOneHigh; RefreshHigh = this.RefreshHigh;
        HaltHigh = this.HaltHigh; BusAcknowledgeHigh = this.BusAcknowledgeHigh;
    }
}

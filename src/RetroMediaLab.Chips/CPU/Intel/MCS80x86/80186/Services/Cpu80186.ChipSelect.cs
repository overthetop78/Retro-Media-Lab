using RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Enums;
using S = RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Constants.ChipSelect80186Constants;

namespace RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Services;

public sealed partial class Cpu80186
{
    private ushort _upperChipSelect;
    private ushort _lowerChipSelect;
    private ushort _peripheralChipSelect;
    private ushort _middleChipSelect;
    private ushort _middleChipSelectSize;
    private int _chipSelectWritten;
    public Cpu80186ChipSelect ActiveChipSelect { get; private set; }
    public Action<Cpu80186ChipSelect>? ChipSelectChanged { get; set; }
    public bool LatchedAddress1 { get; private set; }
    public bool LatchedAddress2 { get; private set; }

    private void ResetChipSelects()
    {
        _upperChipSelect = S.ResetUpper;
        _lowerChipSelect = _peripheralChipSelect = _middleChipSelect = _middleChipSelectSize = 0;
        _chipSelectWritten = S.UpperWritten;
        LatchedAddress1 = LatchedAddress2 = false;
        EndExternalBus();
    }

    private ushort ReadChipSelectRegister(int offset) => offset switch
    {
        S.UpperRegister => _upperChipSelect,
        S.LowerRegister => _lowerChipSelect,
        S.PeripheralRegister => _peripheralChipSelect,
        S.MiddleRegister => _middleChipSelect,
        S.MiddleSizeRegister => _middleChipSelectSize,
        _ => 0
    };

    private void WriteChipSelectRegister(int offset, ushort value)
    {
        switch (offset)
        {
            case S.UpperRegister:
                _upperChipSelect = (ushort)(value | S.UpperFixedBits);
                _chipSelectWritten |= S.UpperWritten;
                break;
            case S.LowerRegister:
                _lowerChipSelect = (ushort)((value & S.LowerWritableBits) | S.BoundaryFixedBits);
                _chipSelectWritten |= S.LowerWritten;
                break;
            case S.PeripheralRegister:
                _peripheralChipSelect = (ushort)(value | S.BoundaryFixedBits);
                _chipSelectWritten |= S.PeripheralWritten;
                break;
            case S.MiddleRegister:
                _middleChipSelect = (ushort)(value | S.MiddleFixedBits);
                _chipSelectWritten |= S.MiddleWritten;
                break;
            case S.MiddleSizeRegister:
                _middleChipSelectSize = (ushort)(value | S.MiddleSizeFixedBits);
                _chipSelectWritten |= S.MiddleSizeWritten;
                break;
        }
    }

    public Cpu80186ChipSelect SelectBusTargets(int address, bool memory)
    {
        var selected = Cpu80186ChipSelect.None;
        if (memory)
        {
            var upperStart = (_upperChipSelect & S.BoundaryMask) << S.BoundaryShift;
            if (address >= upperStart) selected |= Cpu80186ChipSelect.Upper;
            if ((_chipSelectWritten & S.LowerWritten) != 0)
            {
                var lowerEnd = ((_lowerChipSelect | S.BoundaryTail) << S.BoundaryShift) | S.AddressLowBits;
                if (address <= lowerEnd) selected |= Cpu80186ChipSelect.Lower;
            }
            if ((_chipSelectWritten & (S.MiddleWritten | S.MiddleSizeWritten)) == (S.MiddleWritten | S.MiddleSizeWritten))
            {
                var size = (_middleChipSelectSize & S.MiddleSizeMask) << S.MiddleSizeShift;
                var start = (_middleChipSelect & S.MiddleBoundaryMask) << S.BoundaryShift;
                var relative = address - start;
                // Les combinaisons non one-hot et les bases non alignées sont indéfinies sur la puce.
                if (size > 0 && (size & (size - 1)) == 0 && (start & (size - 1)) == 0 && relative >= 0 && relative < size)
                    selected |= (Cpu80186ChipSelect)((int)Cpu80186ChipSelect.Middle0 << (relative / (size / S.MiddleCount)));
            }
        }
        if ((_chipSelectWritten & (S.PeripheralWritten | S.MiddleSizeWritten)) == (S.PeripheralWritten | S.MiddleSizeWritten) &&
            ((_middleChipSelectSize & S.PeripheralsInMemory) != 0) == memory)
        {
            var start = (_peripheralChipSelect & S.BoundaryMask) << S.BoundaryShift;
            var relative = address - start;
            var count = (_middleChipSelectSize & S.ExtendedPeripherals) != 0 ? S.PeripheralCount : S.PeripheralBasicCount;
            if (relative >= 0 && relative < count * S.PeripheralBlockBytes)
                selected |= (Cpu80186ChipSelect)((int)Cpu80186ChipSelect.Peripheral0 << (relative / S.PeripheralBlockBytes));
        }
        return selected;
    }

    private int ChipSelectWaits(Cpu80186ChipSelect selected)
    {
        var waits = 0;
        if ((selected & Cpu80186ChipSelect.Upper) != 0) waits = _upperChipSelect & S.WaitMask;
        if ((selected & Cpu80186ChipSelect.Lower) != 0) waits = Math.Max(waits, _lowerChipSelect & S.WaitMask);
        var middle = Cpu80186ChipSelect.Middle0 | Cpu80186ChipSelect.Middle1 | Cpu80186ChipSelect.Middle2 | Cpu80186ChipSelect.Middle3;
        if ((selected & middle) != 0) waits = Math.Max(waits, _middleChipSelect & S.WaitMask);
        var lowPeripherals = Cpu80186ChipSelect.Peripheral0 | Cpu80186ChipSelect.Peripheral1 | Cpu80186ChipSelect.Peripheral2 | Cpu80186ChipSelect.Peripheral3;
        if ((selected & lowPeripherals) != 0) waits = Math.Max(waits, _peripheralChipSelect & S.WaitMask);
        var highPeripherals = Cpu80186ChipSelect.Peripheral4 | Cpu80186ChipSelect.Peripheral5 | Cpu80186ChipSelect.Peripheral6;
        if ((selected & highPeripherals) != 0) waits = Math.Max(waits, _middleChipSelectSize & S.WaitMask);
        return waits;
    }

    private void BeginExternalBus(int address, bool memory, bool waitStates = true)
    {
        PrepareExecutionBus();
        LatchedAddress1 = (address & S.Address1) != 0;
        LatchedAddress2 = (address & S.Address2) != 0;
        ActiveChipSelect = SelectBusTargets(address, memory);
        if (waitStates && !IsInstructionFetch)
        {
            var waits = ChipSelectWaits(ActiveChipSelect);
            _busPenalty += waits;
            _executionWaitClocks += waits;
        }
        ChipSelectChanged?.Invoke(ActiveChipSelect);
    }

    private void EndExternalBus()
    {
        ActiveChipSelect = Cpu80186ChipSelect.None;
        ChipSelectChanged?.Invoke(Cpu80186ChipSelect.None);
    }
}

using RetroMediaLab.Chips.IO.ChipsAndTechnologies.SuperIO.Chip82C711.Enums;
using C = RetroMediaLab.Chips.IO.ChipsAndTechnologies.SuperIO.Chip82C711.Constants.Upc82C711Constants;

namespace RetroMediaLab.Chips.IO.ChipsAndTechnologies.SuperIO.Chip82C711.Services;

/// <summary>Configuration and port decoding of the 82C711, independent of its child controllers.</summary>
/// <remarks>CHIPS 82C711/712 datasheet, sections 6.2–6.3. Only PowerOn clears CR0–CR3;
/// resetting a child controller must not recreate this object.</remarks>
public sealed class Upc82C711Configuration
:RetroMediaLab.Chips.IO.ChipsAndTechnologies.SuperIO.Chip82C711.Interfaces.IUpc82C711Configuration{
    private readonly byte[] _registers = new byte[C.RegisterCount];
    private int _unlockWrites;
    private int _index;

    public Upc82C711Configuration() => PowerOn();

    public bool IsUnlocked => _unlockWrites == C.UnlockWrites;

    public void PowerOn()
    {
        Array.Clear(_registers);
        _registers[C.EnableRegister] = C.InitialEnable;
        _registers[C.AddressRegister] = C.InitialAddress;
        _registers[C.SerialRegister] = C.InitialSerial;
        _unlockWrites = 0;
        _index = 0;
    }

    public byte Read(int address)
    {
        if (!IsUnlocked) _unlockWrites = 0;
        return address == C.ConfigurationDataAddress && IsUnlocked && _index < C.RegisterCount
            && Enabled(C.AddressRegister, C.ConfigurationReadEnable) ? _registers[_index] : C.OpenBus;
    }

    /// <summary>Returns true only for writes consumed by the configuration ports.</summary>
    public bool Write(int address, byte value)
    {
        if (address != C.ConfigurationAddress && !IsUnlocked) _unlockWrites = 0;
        if (address == C.ConfigurationAddress)
        {
            if (IsUnlocked)
            {
                if (value == C.Lock) _unlockWrites = 0;
                else _index = value & C.RegisterMask;
            }
            else _unlockWrites = value == C.Unlock ? _unlockWrites + 1 : 0;
            return true;
        }
        if (address != C.ConfigurationDataAddress) return false;
        if (IsUnlocked && _index < C.RegisterCount) _registers[_index] = value;
        return true;
    }

    public bool TryDecode(int address, out UpcPeripheral peripheral, out int register)
    {
        peripheral = UpcPeripheral.None;
        register = 0;
        if (address == C.ConfigurationAddress || address == C.ConfigurationDataAddress)
        {
            peripheral = UpcPeripheral.Configuration;
            register = address - C.ConfigurationAddress;
        }
        else if (Enabled(C.EnableRegister, C.IdeEnable) && address == C.IdeControl)
        {
            peripheral = UpcPeripheral.IdeControl;
            register = address & C.EightRegisterMask;
        }
        else if (Enabled(C.EnableRegister, C.IdeEnable) && InRange(address, C.IdeBase, C.EightRegisterMask))
        {
            peripheral = UpcPeripheral.Ide;
            register = address & C.EightRegisterMask;
        }
        else if (Enabled(C.EnableRegister, C.FloppyEnable) && InRange(address, C.FloppyBase, C.EightRegisterMask)
            && address != C.IdeControl)
        {
            peripheral = UpcPeripheral.Floppy;
            register = address & C.EightRegisterMask;
        }

        // Serial decoding has priority over LPT and the fixed blocks when their addresses overlap.
        var parallel = C.ParallelAddresses[_registers[C.AddressRegister] & C.ParallelSelectionMask];
        if (parallel != 0 && Enabled(C.AddressRegister, C.ParallelEnable)
            && InRange(address, parallel, C.FourRegisterMask))
        {
            peripheral = UpcPeripheral.Parallel;
            register = address & C.FourRegisterMask;
        }
        if (Enabled(C.SerialRegister, C.Serial1Enable)
            && InRange(address, SerialAddress(false), C.EightRegisterMask))
        {
            peripheral = UpcPeripheral.Serial1;
            register = address & C.EightRegisterMask;
        }
        if (Enabled(C.SerialRegister, C.Serial2Enable)
            && InRange(address, SerialAddress(true), C.EightRegisterMask))
        {
            peripheral = UpcPeripheral.Serial2;
            register = address & C.EightRegisterMask;
        }
        return peripheral != UpcPeripheral.None;
    }

    private bool Enabled(int index, int mask) => (_registers[index] & mask) == mask;

    private static bool InRange(int address, int start, int mask) => (address & ~mask) == start;

    private int SerialAddress(bool second)
    {
        var shift = second ? C.Serial2SelectionShift : 0;
        var selection = (_registers[C.SerialRegister] >> shift) & C.SerialSelectionMask;
        if (selection == 0) return C.SerialCom1;
        if (selection == 1) return C.SerialCom2;
        var pair = (_registers[C.AddressRegister] >> C.AuxiliarySerialShift) & C.SerialSelectionMask;
        return C.AuxiliarySerialAddresses[pair * C.AuxiliarySerialPairSize
            + selection - C.AuxiliarySerialFirstSelection];
    }
}

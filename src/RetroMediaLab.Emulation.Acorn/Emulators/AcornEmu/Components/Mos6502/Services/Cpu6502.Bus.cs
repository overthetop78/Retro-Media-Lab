using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6502.Constants.Cpu6502Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6502.Services;

internal sealed partial class Cpu6502
{
    private byte IndexedZeroPage(byte index)
    {
        var address = Fetch();
        Read(address);
        return (byte)(address + index);
    }

    private ushort IndexedIndirect()
    {
        var pointer = Fetch();
        Read(pointer);
        return ZeroPageWord((byte)(pointer + _x));
    }

    private ushort IndexedAddress(ushort address, byte index, bool forceRead, bool indirect = false)
    {
        var result = (ushort)(address + index);
        if (forceRead || (address & C.PageMask) != (result & C.PageMask))
        {
            if (cmos)
            {
                var crossing = (address & C.PageMask) != (result & C.PageMask);
                // Le 65C12 reprend l'octet d'opérande en cas de retenue.
                // STA (zp),Y reprend également son opérande, sans accéder à l'E/S cible.
                Read(crossing || indirect ? (ushort)(_pc - 1) : result);
            }
            else Read((ushort)((address & C.PageMask) | (byte)result));
        }
        return result;
    }

    private int Branch(bool taken)
    {
        var displacement = (sbyte)Fetch();
        if (!taken) return 2;
        // NMOS : le sondage normal reste celui du cycle de lecture de l'opcode,
        // même si le branchement pris ajoute ensuite un cycle.
        var earlyIrq = _irqPrevious && (_p & InterruptDisable) == 0;
        var earlyNmi = _nmiPrevious;
        var previous = _pc;
        Read(previous);
        _pc = (ushort)(previous + displacement);
        if ((previous & C.PageMask) == (_pc & C.PageMask))
        {
            if (!cmos)
            {
                _branchIrqSample = earlyIrq;
                _branchNmiSample = earlyNmi;
            }
            return 3;
        }
        if (cmos) Read((ushort)((previous & C.PageMask) | (byte)_pc));
        else
        {
            Read((ushort)((previous & C.PageMask) | (byte)_pc));
            // Le franchissement de page fournit un second sondage ; les deux
            // peuvent déclencher l'IRQ, y compris une impulsion déjà retombée.
            _branchIrqSample = earlyIrq || (_irqPrevious && (_p & InterruptDisable) == 0);
            _branchNmiSample = earlyNmi || _nmiPrevious;
        }
        return 4;
    }

    private void PreparePull()
    {
        Read(_pc);
        Read((ushort)(C.StackBase | _s));
    }

    private ushort InterruptVector(ushort requested, bool hardware = false)
    {
        if ((!cmos || hardware) && _nmiPending)
        {
            _nmiPending = _nmiAccepted = false;
            return C.NmiVector;
        }
        return requested;
    }
}

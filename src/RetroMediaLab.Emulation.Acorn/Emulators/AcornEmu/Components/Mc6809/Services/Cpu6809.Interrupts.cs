using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mc6809.Constants.Cpu6809Constants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mc6809.Services;

internal sealed partial class Cpu6809
{
    private bool ServiceInterrupt()
    {
        var fast = Firq?.Invoke() == true;
        if (bus.Irq || fast || _nmiPending) _sync = false;
        if (_nmiPending)
        {
            _nmiPending = false;
            Interrupt(C.NmiVector, false, C.IrqMask | C.FirqMask);
            return true;
        }
        if (fast && !IsSet(C.FirqMask)) { Interrupt(C.FirqVector, true, C.IrqMask | C.FirqMask); return true; }
        if (bus.Irq && !IsSet(C.IrqMask)) { Interrupt(C.IrqVector, false, C.IrqMask); return true; }
        return false;
    }

    private void Interrupt(ushort vector, bool fast, byte masks)
    {
        var start = Cycles;
        var waiting = _cwai;
        if (!waiting)
        {
            Flag(C.EntireState, !fast);
            PushRegisters(fast ? (byte)0x81 : byte.MaxValue, false);
        }
        _cwai = _sync = false;
        CC |= masks;
        PC = ReadWord(vector);
        Pad(start, waiting ? 7 : fast ? 10 : 19);
    }

    private int SoftwareInterrupt(int page)
    {
        Flag(C.EntireState, true);
        PushRegisters(byte.MaxValue, false);
        if (page == 0) CC |= C.IrqMask | C.FirqMask;
        PC = ReadWord(page == 0x10 ? C.SwiTwoVector : page == 0x11 ? C.SwiThreeVector : C.SwiVector);
        return page == 0 ? 19 : 20;
    }

    private int ReturnInterrupt()
    {
        PullRegisters(0x01, false);
        var entire = IsSet(C.EntireState);
        PullRegisters(entire ? (byte)0xfe : (byte)0x80, false);
        return entire ? 15 : 6;
    }
}

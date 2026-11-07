using RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Interfaces;
using C = RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Constants.ElectronUlaConstants;

namespace RetroMediaLab.Chips.Custom.Acorn.Electron.ULA.Services;

/// <summary>Registres et interruptions de l’ULA ; le CPU exécute lui-même le MOS Electron.</summary>
public sealed partial class ElectronUla : IElectronUla
{
    private readonly ElectronVideo _video;
    private readonly IElectronTapePins _tape;
    private byte _status;
    private byte _interrupts;
    private int _comms;
    private readonly ElectronCommsCounter _commsCounter = new();

    public ElectronUla(ElectronVideo video, IElectronTapePins tape)
    {
        _video = video;
        _tape = tape;
        Reset(true);
    }

    public int RomPage { get; private set; }
    public bool Motor { get; private set; }
    public bool CapsLock { get; private set; }
    public bool Irq => (_status & _interrupts & C.EnabledInterruptMask) != 0;
    public short SoundSample => _comms == C.SoundMode ? (_commsCounter.SoundLevel ? (short)C.SpeakerAmplitude : (short)-C.SpeakerAmplitude) : (short)0;

    public void Reset(bool cold)
    {
        // BREAK drives the CPU reset output, not the ULA power-on input.
        if (!cold) return;
        _status = C.PowerOnInterrupt;
        _interrupts = 0;
        RomPage = 0;
        _comms = C.UnusedCommsMode;
        _commsCounter.Reset();
        Motor = CapsLock = false;
        ResetSerial();
        ResetBus(cold);
    }

    public byte Read(int register)
    {
        switch (register & C.RegisterMask)
        {
            case C.InterruptRegister:
                var result = (byte)(_status | C.StatusHigh | (Irq ? C.InterruptSummary : 0));
                _status &= unchecked((byte)~C.PowerOnInterrupt);
                return result;
            case C.TapeDataRegister:
                _status &= unchecked((byte)~C.ReceiveFullInterrupt);
                return _received;
            default:
                return C.UnusedUlaRegister;
        }
    }

    public void Write(int register, byte value)
    {
        register &= C.RegisterMask;
        switch (register)
        {
            case C.InterruptRegister:
                _interrupts = value;
                break;
            case C.ScreenStartLowRegister:
            case C.ScreenStartHighRegister:
                _video.WriteStart(register == C.ScreenStartHighRegister, value);
                break;
            case C.TapeDataRegister:
                LoadTransmitter(value);
                break;
            case C.PageInterruptRegister:
                if ((value & C.ClearNmiPriority) != 0) ClearRamPriority();
                if ((RomPage & C.InternalPageMask) != C.InternalPageSelect || (value & C.InternalPageSelect) != 0)
                    RomPage = value & C.PageMask;
                if ((value & C.ClearDisplayEnd) != 0) _status &= unchecked((byte)~C.DisplayEndInterrupt);
                if ((value & C.ClearClock) != 0) _status &= unchecked((byte)~C.ClockInterrupt);
                if ((value & C.ClearHighTone) != 0) _status &= unchecked((byte)~C.HighToneInterrupt);
                break;
            case C.DividerRegister:
                _commsCounter.WriteDivider(value);
                break;
            case C.ControlRegister:
                _comms = (value >> C.CommsShift) & C.CommsMask;
                Motor = (value & C.TapeMotorMask) != 0;
                CapsLock = (value & C.CapsLockMask) != 0;
                _video.SetMode(value);
                break;
            default:
                if (register >= C.PaletteStartRegister) _video.WritePalette(register - C.PaletteStartRegister, value);
                break;
        }
    }

    public void Tick()
    {
        TickBus();
        TickSerial();
        _tape.Tick(Motor);
        _video.Tick();
        if (_video.ClockInterruptEdge) _status |= C.ClockInterrupt;
        if (_video.DisplayEndInterruptEdge) _status |= C.DisplayEndInterrupt;
    }
}

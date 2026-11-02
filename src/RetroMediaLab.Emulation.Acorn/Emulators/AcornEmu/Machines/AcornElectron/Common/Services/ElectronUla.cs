using RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Services;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornElectron.Common.Constants.ElectronConstants;
using V = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornElectron.Common.Constants.ElectronVideoConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornElectron.Common.Services;

/// <summary>Registres et interruptions de l’ULA ; le CPU exécute lui-même le MOS Electron.</summary>
internal sealed partial class ElectronUla
{
    private readonly ElectronVideo _video;
    private readonly AnalogCassette _cassette;
    private byte _status;
    private byte _interrupts;
    private int _comms;
    private int _divider;
    private int _tonePhase;
    private bool _tone;

    internal ElectronUla(ElectronVideo video, AnalogCassette cassette)
    {
        _video = video;
        _cassette = cassette;
        video.LineStarted = OnLineStarted;
        Reset(true);
    }

    internal int RomPage { get; private set; }
    internal bool Motor { get; private set; }
    internal bool CapsLock { get; private set; }
    internal bool Irq => (_status & _interrupts & C.EnabledInterruptMask) != 0;
    internal short SoundSample => _comms == C.SoundMode ? (_tone ? (short)C.SpeakerAmplitude : (short)-C.SpeakerAmplitude) : (short)0;

    internal void Reset(bool cold)
    {
        _status = cold ? C.PowerOnInterrupt : (byte)0;
        _interrupts = 0;
        RomPage = 0;
        _comms = C.UnusedCommsMode;
        _divider = C.InitialSoundDivider;
        _tonePhase = 0;
        _tone = false;
        Motor = CapsLock = false;
        ResetSerial();
    }

    internal byte Read(int register)
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

    internal void Write(int register, byte value)
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
                _pendingByte = value;
                _transmitPending = true;
                _status &= unchecked((byte)~C.TransmitEmptyInterrupt);
                break;
            case C.PageInterruptRegister:
                if ((RomPage & C.InternalPageMask) != C.InternalPageSelect || (value & C.InternalPageSelect) != 0)
                    RomPage = value & C.PageMask;
                if ((value & C.ClearDisplayEnd) != 0) _status &= unchecked((byte)~C.DisplayEndInterrupt);
                if ((value & C.ClearClock) != 0) _status &= unchecked((byte)~C.ClockInterrupt);
                if ((value & C.ClearHighTone) != 0) _status &= unchecked((byte)~C.HighToneInterrupt);
                break;
            case C.DividerRegister:
                _divider = value;
                _tonePhase = 0;
                _tone = false;
                break;
            case C.ControlRegister:
                var next = (value >> C.CommsShift) & C.CommsMask;
                if (next != _comms)
                {
                    ResetSerial();
                    _tonePhase = 0;
                    _tone = false;
                    if (next == C.TapeOutputMode) _status |= C.ReceiveFullInterrupt;
                }
                _comms = next;
                Motor = (value & C.TapeMotorMask) != 0;
                CapsLock = (value & C.CapsLockMask) != 0;
                _video.SetMode(value);
                break;
            default:
                if (register >= C.PaletteStartRegister) _video.WritePalette(register - C.PaletteStartRegister, value);
                break;
        }
    }

    internal void Tick()
    {
        if (_comms == C.SoundMode && ++_tonePhase >= C.SoundHalfPeriodDivider * (_divider + 1))
        {
            _tonePhase = 0;
            _tone = !_tone;
        }
        TickSerial();
        _cassette.Tick(Motor);
        _video.Tick();
    }

    private void OnLineStarted(int line)
    {
        if (line == V.ClockInterruptLine) _status |= C.ClockInterrupt;
        if (line == V.ModeHeight[_video.Mode]) _status |= C.DisplayEndInterrupt;
    }
}

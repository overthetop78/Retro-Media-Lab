using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Interfaces;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000AicRegisterConstants;
using S = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000SequencerConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

/// <summary>Registres internes AIC-010 ; les ports externes et la RAM de tampon restent au propriétaire.</summary>
/// <remarks>Une lecture non définie ou une pile vide retourne false sans inventer de valeur de bus.
/// Les paramètres d'horloge sont exposés au câblage ; ils ne sont pas des délais de temps hôte.</remarks>
public sealed partial class Acb4000AicRegisters
{
    private byte gpioControl;
    private byte gpioOutput;
    private byte gpioInputs;
    private bool branchInput;
    private byte externalDecode;

    public Acb4000EccRegisters Ecc { get; }
    public Acb4000Sequencer Sequencer { get; }
    public byte WriteAddressMarkControl { get; private set; }
    public byte DetectAddressMarkControl { get; private set; }
    public Acb4000Clocks Clocks { get; } = new();
    public byte ClockControl => Clocks.Control;
    public bool WriteAddressMarkOutput => Sequencer.AddressMarkOutputWindow
        && (WriteAddressMarkControl & (1 << Sequencer.OutputBitRing)) != 0;
    public byte GpioOutputEnable => (byte)(gpioControl & ~(gpioControl >> C.DecodeShift) & C.GpioMask);
    public byte GpioOutputLevels => (byte)(gpioOutput & GpioOutputEnable);
    /// <summary>Broches pilotées par le GPIO ou par le décodeur des ports externes.</summary>
    public byte PinOutputEnable => (byte)(gpioControl & C.GpioMask);
    /// <summary>Les sorties de décodage sont au repos haut et actives à zéro pendant leur cycle.</summary>
    public byte PinOutputLevels => (byte)(GpioOutputLevels | (DecodedPins & ~externalDecode));
    private byte DecodedPins => (byte)(gpioControl & (gpioControl >> C.DecodeShift) & C.GpioMask);
    public int CompareStartBit => ClockControl & C.CompareStartMask;
    public bool ClockOutputsEnabled => (ClockControl & C.ClocksHighImpedance) == 0;
    public int TransferClocksPerByte => (ClockControl & C.FourTransferClocks) == 0
        ? C.TwoCyclesPerByte : C.FourCyclesPerByte;
    public int NonTransferClockSelection => (ClockControl >> C.NonTransferClockShift) & C.NonTransferClockMask;
    public bool HasStableNonTransferClock => NonTransferClockSelection != C.UnstableNonTransferClock;

    public bool CanRead(byte register) => register is C.MiddleSyndrome or C.HighSyndrome
        or C.Branch or C.StartStatus or C.Operation or C.Gpio
        || register == C.ClockStack && Sequencer.StackHasData || IsProgramRegister(register);

    public bool CanWrite(byte register) => register is >= C.EccControl and <= C.ClockStack || IsProgramRegister(register);

    private static bool IsProgramRegister(byte register) => register >= S.RamBase && (register & S.StateMask) < S.StateCount;

    public Acb4000AicRegisters(Acb4000EccRegisters ecc, IAcb4000SequencerBus buffer)
    {
        ArgumentNullException.ThrowIfNull(ecc);
        Ecc = ecc;
        clockedBuffer = buffer as Acb4000BufferController;
        Sequencer = new(ecc, buffer);
    }

    public void SetGpioInputs(byte levels) => gpioInputs = (byte)(levels & C.GpioMask);

    public void SetBranchInput(bool level)
    {
        branchInput = level;
        Sequencer.SetInput(level);
    }

    /// <summary>Reconnaissance du registre NRZ en présence du signal AMD du décodeur externe.</summary>
    public bool TrySynchronizeRead(byte shiftedNrz, bool addressMarkDetected)
    {
        if (!addressMarkDetected || !Sequencer.WaitingForReadSync) return false;
        var mask = (byte)(byte.MaxValue << CompareStartBit);
        if (((shiftedNrz ^ DetectAddressMarkControl) & mask) != 0) return false;
        return Sequencer.AcceptReadAddressMark(shiftedNrz);
    }

    /// <summary>Broches actives à zéro pendant le cycle d'un port externe ; le propriétaire fournit ses données.</summary>
    public byte DecodeExternalCycle(byte register, bool writing)
    {
        var pin = register switch
        {
            C.External6E => writing ? C.Write6EPin : C.Read6EPin,
            C.External6F => writing ? C.Write6FPin : C.Read6FPin,
            _ => -1
        };
        if (pin < 0) return 0;
        var selected = 1 << pin;
        return (byte)((gpioControl & (gpioControl >> C.DecodeShift)) & selected);
    }

    internal void BeginExternalCycle(byte register, bool writing) => externalDecode = DecodeExternalCycle(register, writing);
    internal void EndExternalCycle() => externalDecode = 0;

    public bool TryRead(byte register, out byte value)
    {
        switch (register)
        {
            case C.MiddleSyndrome: value = Ecc.ReadMiddleSyndrome(); return true;
            case C.HighSyndrome: value = Ecc.ReadHighSyndrome(); return true;
            case C.Branch: value = Sequencer.ReadBranch(); return true;
            case C.StartStatus: value = Sequencer.ReadStatus(); return true;
            case C.Operation: value = Sequencer.ReadOperation(); return true;
            case C.Gpio:
                value = (byte)(((gpioInputs & ~PinOutputEnable) | PinOutputLevels)
                    | (branchInput ? C.BranchInputMask : 0)
                    | (Sequencer.Output ? C.SequencerOutputMask : 0));
                return true;
            case C.ClockStack: return Sequencer.TryPop(out value);
            default: return Sequencer.TryReadProgram(register, out value);
        }
    }

    public bool TryWrite(byte register, byte value)
    {
        if (Ecc.ResetAsserted && register is >= C.Branch and <= C.Gpio) return true;
        switch (register)
        {
            case C.EccControl:
                Ecc.WriteControl(value);
                if (Ecc.ResetAsserted) ResetRegisters();
                return true;
            case C.MiddleSyndrome:
            case C.HighSyndrome: return true; // Read-only decoding: no write effect.
            case >= C.FirstPolynomial and <= C.LastPolynomial:
                Ecc.WritePolynomial(register, value);
                return true;
            case C.Branch: Sequencer.WriteBranch(value); return true;
            case C.StartStatus: Sequencer.Start(value); return true;
            case C.Operation: Sequencer.WriteOperation(value); return true;
            case C.WriteAddressMark: WriteAddressMarkControl = value; return true;
            case C.DetectAddressMark: DetectAddressMarkControl = value; return true;
            case C.GpioControl: gpioControl = value; return true;
            case C.Gpio: gpioOutput = (byte)(value & C.GpioMask); return true;
            case C.ClockStack: Clocks.WriteControl(value); return true;
            default: return Sequencer.TryWriteProgram(register, value);
        }
    }

    /// <summary>Entrée RST : conserve les broches empruntées et les registres hors du groupe 71h–7Eh.</summary>
    public void Reset()
    {
        Ecc.Reset();
        ResetRegisters();
    }

    private void ResetRegisters()
    {
        Sequencer.ResetEngine();
        peripheralAddressSampled = false;
        hostAddressSampled = false;
        Clocks.SelectReference(false);
        Clocks.ResetPhase();
        gpioControl = gpioOutput = 0;
        externalDecode = 0;
        WriteAddressMarkControl = DetectAddressMarkControl = 0;
    }
}

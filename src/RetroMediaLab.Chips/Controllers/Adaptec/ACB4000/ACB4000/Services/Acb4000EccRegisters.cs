using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000EccRegisterConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

/// <summary>Registres ECC AIC-010 : horloge du flux et impulsions logicielles distinctes.</summary>
/// <remarks>Les registres de polynôme ne sont pas initialisés par RESET. Le propriétaire
/// les programme comme le firmware. Le mode de longueur 10b ne possède pas de fonction
/// documentée : les méthodes TryClock refusent ce mode sans fabriquer de syndrome.</remarks>
public sealed class Acb4000EccRegisters
{
    private uint _taps;
    private uint _syndrome;
    private byte _control = C.Reset;
    private bool _shiftPending;

    internal bool ShiftPending => _shiftPending;
    public byte Control => _control;
    public bool ResetAsserted => (_control & C.Reset) != 0;
    public bool SectorBranchEnabled => (_control & C.SectorBranch) != 0;
    public bool IsLengthDocumented => (_control & C.LengthMask) != C.UndocumentedLength;
    public uint Polynomial => (_taps << 1) | 1U;

    public bool CheckPassed => !ResetAsserted && IsLengthDocumented && (_syndrome & VisibleMask) == 0;
    internal bool SerialOutput => (_syndrome & C.HighBit) != 0;

    internal void BeginField() => _syndrome = 0;

    internal static Acb4000EccRegisters CreateFirmwareConfiguration()
    {
        var result = new Acb4000EccRegisters();
        result.InitializeFirmware();
        return result;
    }

    internal void InitializeFirmware()
    {
        // Board firmware 1D9E-1DC1 resets the ASIC then reloads table 1FC7.
        Reset();
        var taps = Constants.Acb4000ErrorCheckConstants.Polynomial >> 1;
        for (var register = C.FirstPolynomialRegister; register <= C.LastPolynomialRegister; register++, taps >>= C.BitsPerByte)
            WritePolynomial(register, (byte)taps);
        WriteControl(C.Length32);
    }

    private uint VisibleMask => (_control & C.LengthMask) switch
    {
        C.Length16 => C.Mask16,
        C.Length24 => C.Mask24,
        C.Length32 => C.Mask32,
        _ => 0
    };

    private int LowBit => (_control & C.LengthMask) switch
    {
        C.Length16 => C.LowBit16,
        C.Length24 => C.LowBit24,
        _ => C.LowBit32
    };

    public void WritePolynomial(int register, byte value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(register, C.FirstPolynomialRegister);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(register, C.LastPolynomialRegister);
        var shift = (register - C.FirstPolynomialRegister) * C.BitsPerByte;
        _taps = ((_taps & ~((uint)byte.MaxValue << shift)) | ((uint)value << shift)) & C.PolynomialTapMask;
    }

    public void WriteControl(byte value)
    {
        _control = (byte)(value & ~C.Shift);
        _shiftPending = (value & C.Shift) != 0;
        if ((value & (C.Reset | C.Clear)) != 0) _syndrome = 0;
    }

    public byte ReadMiddleSyndrome()
    {
        var value = _syndrome & VisibleMask;
        return (byte)(((value >> C.MiddleByteShift) & ~1U) | ((value & C.LowSyndromeMask) != 0 ? 1U : 0U));
    }

    public byte ReadHighSyndrome() => (byte)((_syndrome & VisibleMask) >> C.HighByteShift);

    /// <summary>Front RD/REF pour l'impulsion demandée par 71h ; aucune impulsion sans horloge.</summary>
    public bool TryClockReference(bool readGate, bool writeGate)
    {
        if (!_shiftPending) return false;
        _shiftPending = false;
        if (ResetAsserted || readGate || writeGate || !IsLengthDocumented) return false;
        if ((_control & C.Clear) != 0) { _syndrome = 0; return true; }
        if ((_control & C.DisableFeedback) != 0)
            _syndrome = (_syndrome << 1) | ((_control & C.SerialInput) != 0 ? 1U : 0U);
        else Advance(false);
        return true;
    }

    /// <summary>Un bit NRZ appartenant au champ couvert par l'ECC ; hors champ le propriétaire n'appelle pas cette méthode.</summary>
    public bool TryClockDataBit(bool bit)
    {
        if (ResetAsserted || !IsLengthDocumented) return false;
        if ((_control & C.Clear) != 0) { _syndrome = 0; return true; }
        if ((_control & C.DisableFeedback) != 0)
            _syndrome = (_syndrome << 1) | (bit ? 1U : 0U);
        else Advance(bit);
        return true;
    }

    public bool TryClockDataByte(byte value)
    {
        if (ResetAsserted || !IsLengthDocumented) return false;
        if ((_control & (C.LengthMask | C.DisableFeedback | C.Clear)) == C.Length32)
        {
            _syndrome = Acb4000ErrorCheck.AdvanceByte(_syndrome, value, Polynomial);
            return true;
        }
        for (var bit = C.BitsPerByte - 1; bit >= 0; bit--)
            TryClockDataBit((value & (1 << bit)) != 0);
        return true;
    }

    private void Advance(bool bit)
    {
        var feedback = ((_syndrome & C.HighBit) != 0) != bit;
        var polynomial = (Polynomial & VisibleMask) | (1U << LowBit);
        _syndrome = ((_syndrome << 1) ^ (feedback ? polynomial : 0U)) & VisibleMask;
    }

    public void Reset()
    {
        _control = C.Reset;
        _syndrome = 0;
        _shiftPending = false;
    }
}

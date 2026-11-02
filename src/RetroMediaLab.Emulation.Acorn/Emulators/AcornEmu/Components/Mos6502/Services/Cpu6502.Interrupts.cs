namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Mos6502.Services;

/// <summary>Broches d'interruption du NMOS 6502, indépendantes des périphériques raccordés.</summary>
internal sealed partial class Cpu6502
{
    private bool _nmiLevel;
    private bool _nmiPending;
    private bool _nmiPrevious;
    private bool _nmiAccepted;
    private bool _overflowLevel;

    private void SampleOverflow()
    {
        var active = bus.SetOverflow;
        if (active && !_overflowLevel) _p |= Overflow;
        _overflowLevel = active;
    }

    /// <summary>La NMI est mémorisée sur front actif ; un niveau maintenu ne déclenche pas plusieurs entrées.</summary>
    private void SampleNmi()
    {
        var active = bus.Nmi;
        if (active && !_nmiLevel) _nmiPending = true;
        _nmiLevel = active;
    }

    /// <summary>Empile le retour et l'état sans indicateur BRK, puis charge le vecteur choisi en sept cycles.</summary>
    private void EnterInterrupt(long start, ushort vector)
    {
        _irqPending = false;
        if (_publishesSync) bus.SetSync(true);
        try { Read(_pc); }
        finally { if (_publishesSync) bus.SetSync(false); }
        Read(_pc);
        Push((byte)(_pc >> 8)); Push((byte)_pc); Push((byte)((_p | Unused) & ~Break));
        _p |= InterruptDisable;
        if (cmos) _p &= unchecked((byte)~Decimal);
        _pc = ReadWord(InterruptVector(vector, hardware: true));
        Pad(start, 7);
        _nmiAccepted = _nmiPrevious;
    }
}

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
        if (active && !_overflowLevel) P |= Overflow;
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
        bus.SetSync(true);
        try { Read(PC); }
        finally { bus.SetSync(false); }
        Read(PC);
        Push((byte)(PC >> 8)); Push((byte)PC); Push((byte)((P | Unused) & ~Break));
        P |= InterruptDisable;
        if (cmos) P &= unchecked((byte)~Decimal);
        PC = ReadWord(InterruptVector(vector, hardware: true));
        Pad(start, 7);
        _nmiAccepted = _nmiPrevious;
    }
}

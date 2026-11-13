using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;

namespace RetroMediaLab.Chips.EmuChip.CPU.Zilog.Z80.Z80.Services;

/// <summary>Effets fonctionnels documentés des bascules, Zilog UM008011-0816, table 1,
/// pages 18–19, et instructions DI/EI/IM/RETN. Aucun séquencement de bus n'est substitué.</summary>
/// <remarks>Le séquenceur CPU doit appeler InstructionCompleted une fois par instruction entière,
/// jamais par préfixe, puis appeler AcceptAtBoundary à la frontière matérielle autorisée.
/// Cette classe interne ne détermine pas les phases de ces appels et ne constitue pas un CPU.</remarks>
internal sealed class Z80InterruptControl
{
    private bool _iff1, _iff2, _intAsserted, _nmiAsserted, _nmiPending;
    private byte _eiRemaining;
    private Z80InterruptMode _mode;

    internal Z80InterruptControl(Z80InterruptState initialState) => RestoreState(initialState);
    internal bool IntAsserted => _intAsserted;
    internal bool Iff1 => _iff1;
    internal bool Iff2 => _iff2;
    internal bool NmiPending => _nmiPending;
    internal bool HasEligibleRequest => _nmiPending || (_intAsserted && _iff1 && _eiRemaining == 0);
    internal Z80InterruptMode Mode => _mode;

    internal void SetInt(bool asserted) => _intAsserted = asserted;

    /// <summary>L'argument exprime l'assertion logique : false→true est le front électrique
    /// descendant de /NMI. Un verrou unique, aucune file de demandes supplémentaire.</summary>
    internal void SetNmi(bool asserted)
    {
        if (asserted && !_nmiAsserted) _nmiPending = true;
        _nmiAsserted = asserted;
    }

    internal void EnableInterrupts()
    {
        _iff1 = _iff2 = true;
        _eiRemaining = 2; // fin d'EI elle-même, puis fin de l'instruction suivante.
    }

    internal void DisableInterrupts() { _iff1 = _iff2 = false; _eiRemaining = 0; }

    internal void SetMode(Z80InterruptMode mode)
    {
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        _mode = mode;
    }

    internal void ReturnFromNonMaskableInterrupt() => _iff1 = _iff2;

    /// <summary>Ne qualifie pas une impulsion RESET ni ses délais ; applique seulement
    /// les effets définis sur ce bloc une fois le reset normal reconnu par le CPU.</summary>
    internal void ApplyNormalResetEffects()
    {
        DisableInterrupts();
        _mode = Z80InterruptMode.Mode0;
        _nmiPending = false;
        // Les niveaux des fils reçus ne sont pas changés par le reset interne.
    }

    internal void InstructionCompleted()
    {
        if (_eiRemaining != 0) _eiRemaining--;
    }

    /// <summary>Décision fonctionnelle à la frontière fournie par le CPU. BUSRQ empêche
    /// l'acceptation ; NMI reste prioritaire et indépendant du masque IFF1.</summary>
    internal Z80InterruptKind AcceptAtBoundary(bool busRequested, bool? sampledInt = null, bool? sampledNmi = null)
    {
        if (busRequested) return Z80InterruptKind.None;
        if ((sampledNmi ?? _nmiPending) && _nmiPending)
        {
            _nmiPending = false;
            _iff1 = false; // IFF2 est conservé, y compris pour une NMI imbriquée.
            return Z80InterruptKind.NonMaskable;
        }
        if (!(sampledInt ?? _intAsserted) || !_iff1 || _eiRemaining != 0) return Z80InterruptKind.None;
        _iff1 = _iff2 = false;
        return Z80InterruptKind.Maskable;
    }

    internal Z80InterruptState CaptureState() => new(_iff1, _iff2, _mode, _eiRemaining,
        _intAsserted, _nmiAsserted, _nmiPending);

    internal void RestoreState(Z80InterruptState state)
    {
        if (!Enum.IsDefined(state.Mode) || state.EiInhibitRemaining > 2)
            throw new ArgumentException("Invalid Z80 interrupt state.", nameof(state));
        _iff1 = state.Iff1; _iff2 = state.Iff2; _mode = state.Mode;
        _eiRemaining = state.EiInhibitRemaining;
        _intAsserted = state.IntAsserted; _nmiAsserted = state.NmiAsserted; _nmiPending = state.NmiPending;
    }
}

using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000ClockConstants;
using R = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000AicRegisterConstants;
using M = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000MfmConstants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

/// <summary>Horloges divisées et demande périphérique ; le propriétaire fournit les demi-cycles sources.</summary>
/// <remarks>Les périodes sont celles des sources fournies, jamais des temps d'attente hôte.
/// Les sorties non définies sont distinctes d'un niveau logique bas.</remarks>
public sealed class Acb4000Clocks
{
    private int systemPhase;
    private int referencePhase;
    private bool pending;
    private bool requestActive;
    private int systemDivider = C.SystemQuarterDivider;
    private int referenceDivider = M.BitsPerByte / R.TwoCyclesPerByte;
    private int selectedDivider = C.SystemQuarterDivider;
    private bool outputsDriven = true;
    private bool bufferClockRequired;
    private bool peripheralStrobeRequired;
    /// <summary>Front descendant CLKA utile au tampon demandeur ; les phases avancent aussi sans demande.</summary>
    internal event Action? BufferClockFell;
    /// <summary>La demande CLKB est engagée ou complètement relâchée.</summary>
    internal event Action<bool>? TransferPendingChanged;
    public byte Control { get; private set; }
    public bool ReferenceSelected { get; private set; }
    public bool OutputsEnabled => (Control & R.ClocksHighImpedance) == 0;
    public bool SourceIsDefined => ReferenceSelected || SystemSelection != R.UnstableNonTransferClock;
    public bool? ClockA => OutputsEnabled && SourceIsDefined ? Phase < Divider : null;
    /// <summary>CLKB est actif à zéro ; son impulsion encadre le front descendant CLKA.</summary>
    public bool? ClockB => OutputsEnabled && SourceIsDefined ? !requestActive : null;
    public bool TransferPending => pending || requestActive;
    private int SystemSelection => (Control >> R.NonTransferClockShift) & R.NonTransferClockMask;
    private int Divider => selectedDivider;
    private int Phase => ReferenceSelected ? referencePhase : systemPhase;
    internal void SetBufferClockRequired(bool required, bool peripheralStrobe)
    {
        bufferClockRequired = required;
        peripheralStrobeRequired = peripheralStrobe;
    }

    public void WriteControl(byte value)
    {
        Control = value;
        systemDivider = SystemSelection switch
        {
            C.SystemQuarterSelection => C.SystemQuarterDivider,
            C.SystemHalfSelection => C.SystemHalfDivider,
            _ => C.SystemDirectDivider
        };
        referenceDivider = M.BitsPerByte / ((Control & R.FourTransferClocks) == 0
            ? R.TwoCyclesPerByte : R.FourCyclesPerByte);
        selectedDivider = ReferenceSelected ? referenceDivider : systemDivider;
        systemPhase &= systemDivider * C.EdgesPerCycle - 1;
        referencePhase &= referenceDivider * C.EdgesPerCycle - 1;
        outputsDriven = OutputsEnabled && SourceIsDefined;
    }

    /// <summary>Le mux de source est indépendant des horloges fournies aux deux entrées.</summary>
    public void SelectReference(bool selected)
    {
        if (ReferenceSelected == selected) return;
        var wasPending = TransferPending;
        ReferenceSelected = selected;
        selectedDivider = selected ? referenceDivider : systemDivider;
        outputsDriven = OutputsEnabled && SourceIsDefined;
        requestActive = false;
        if (wasPending && !TransferPending) TransferPendingChanged?.Invoke(false);
    }

    public bool TryRequestTransfer()
    {
        if (TransferPending) return false;
        pending = true;
        TransferPendingChanged?.Invoke(true);
        return true;
    }

    public void AdvanceSystemHalfCycles(long count) => Advance(ref systemPhase, systemDivider, reference: false, count);
    public void AdvanceReferenceHalfCycles(long count) => Advance(ref referencePhase, referenceDivider, reference: true, count);

    private void Advance(ref int phase, int divider, bool reference, long count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var period = divider * C.EdgesPerCycle;
        var mask = period - 1;
        // Only useful edges are visited. RAM completion callbacks may change the requests
        // before the next edge in the same span; idle phases still advance arithmetically.
        while (reference == ReferenceSelected && count > 0)
        {
            if (outputsDriven && peripheralStrobeRequired && !TransferPending) TryRequestTransfer();
            var falling = bufferClockRequired && outputsDriven;
            if (!TransferPending && !falling) break;
            var distance = int.MaxValue;
            var pulseEdge = false;
            var fallingEdge = false;
            if (TransferPending)
            {
                var target = requestActive ? (divider + C.HoldHalfCycles) & mask : divider - C.SetupHalfCycles;
                distance = (target - phase) & mask;
                if (requestActive && distance == 0) distance = period;
                pulseEdge = true;
            }
            if (falling)
            {
                var nextFall = phase < divider ? divider - phase : period + divider - phase;
                if (nextFall <= distance)
                {
                    if (nextFall < distance) pulseEdge = false;
                    distance = nextFall;
                    fallingEdge = true;
                }
            }
            if (distance > count) break;
            phase = (phase + distance) & mask;
            count -= distance;
            if (pulseEdge && requestActive)
            {
                requestActive = false;
                TransferPendingChanged?.Invoke(false);
            }
            else if (pulseEdge) { pending = false; requestActive = true; }
            if (fallingEdge && bufferClockRequired && outputsDriven) BufferClockFell?.Invoke();
            divider = reference ? referenceDivider : systemDivider;
            period = divider * C.EdgesPerCycle;
            mask = period - 1;
        }
        phase = (int)(((long)phase + count) & mask);
    }

    /// <summary>Réalignement du bit ring ; le contrôle 7Fh est conservé.</summary>
    public void ResetPhase()
    {
        var wasPending = TransferPending;
        systemPhase = referencePhase = 0;
        pending = requestActive = false;
        if (wasPending) TransferPendingChanged?.Invoke(false);
    }
}

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
    internal int HalfCyclesToNextBufferEvent(out bool fallingEdge)
    {
        var divider = selectedDivider;
        var phase = Phase;
        var period = divider * C.EdgesPerCycle;
        var falling = phase < divider ? divider - phase : period + divider - phase;
        if (requestActive)
        {
            var release = (divider + C.HoldHalfCycles - phase) & (period - 1);
            if (release == 0) release = period;
            if (release < falling)
            {
                fallingEdge = false;
                return release;
            }
        }
        fallingEdge = true;
        return falling;
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
    }

    /// <summary>Le mux de source est indépendant des horloges fournies aux deux entrées.</summary>
    public void SelectReference(bool selected)
    {
        if (ReferenceSelected == selected) return;
        ReferenceSelected = selected;
        selectedDivider = selected ? referenceDivider : systemDivider;
        requestActive = false;
    }

    public bool TryRequestTransfer()
    {
        if (TransferPending) return false;
        pending = true;
        return true;
    }

    public void AdvanceSystemHalfCycles(long count) => Advance(ref systemPhase, systemDivider, !ReferenceSelected, count);
    public void AdvanceReferenceHalfCycles(long count) => Advance(ref referencePhase, referenceDivider, ReferenceSelected, count);

    private void Advance(ref int phase, int divider, bool selected, long count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var period = divider * C.EdgesPerCycle;
        var mask = period - 1;
        // At most a pulse start and a pulse end need inspection; idle spans advance arithmetically.
        while (selected && count > 0 && TransferPending)
        {
            var target = requestActive ? (divider + C.HoldHalfCycles) & mask : divider - C.SetupHalfCycles;
            var distance = (target - phase) & mask;
            if (requestActive && distance == 0) distance = period;
            if (distance > count) break;
            phase = target;
            count -= distance;
            if (requestActive) requestActive = false;
            else { pending = false; requestActive = true; }
        }
        phase = (int)(((long)phase + count) & mask);
    }

    /// <summary>Réalignement du bit ring ; le contrôle 7Fh est conservé.</summary>
    public void ResetPhase()
    {
        systemPhase = referencePhase = 0;
        pending = requestActive = false;
    }
}

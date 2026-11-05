using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using B = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000BusConstants;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000PositioningConstants;
using P = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000ParameterConstants;
using S = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000Constants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

/// <summary>Séquenceur de fronts STEP ; le lecteur emprunté fournit READY, SEEK COMPLETE et TRACK ZERO.</summary>
/// <remarks>StepIssued représente un front, pas la largeur électrique de l'impulsion.
/// Les échéances de panne sont fournies par le propriétaire, pas inventées ici.</remarks>
public sealed class Acb4000Positioning
{
    private readonly int clockRate;
    private long period;
    private long delay;
    private int remaining;
    private bool calibrating;

    public Acb4000PositioningPhase Phase { get; private set; }
    public bool Busy => Phase is not (Acb4000PositioningPhase.Idle or Acb4000PositioningPhase.Complete);
    public bool Complete => Phase == Acb4000PositioningPhase.Complete;
    public bool StepIssued { get; private set; }
    public bool DirectionIn { get; private set; }
    public byte Error { get; private set; }
    public int IssuedSteps { get; private set; }

    public Acb4000Positioning(int ticksPerSecond)
    {
        if ((long)ticksPerSecond * P.FastBufferedStepNanoseconds < B.NanosecondsPerSecond)
            throw new ArgumentOutOfRangeException(nameof(ticksPerSecond));
        clockRate = ticksPerSecond;
    }

    /// <summary>Le signe de distance donne la direction ; un SEEK ne vérifie pas de champ ID.</summary>
    public void BeginSeek(int distance, byte stepCode)
    {
        if (distance == int.MinValue) throw new ArgumentOutOfRangeException(nameof(distance));
        Begin(stepCode, calibration: false);
        DirectionIn = distance >= 0;
        remaining = Math.Abs(distance);
    }

    public void BeginRezero(byte stepCode) => Begin(stepCode, calibration: true);

    private void Begin(byte stepCode, bool calibration)
    {
        if (Busy) throw new InvalidOperationException();
        var nanoseconds = stepCode switch
        {
            P.NonBufferedStepCode => P.NonBufferedStepNanoseconds,
            P.BufferedStepCode => P.BufferedStepNanoseconds,
            P.FastBufferedStepCode => P.FastBufferedStepNanoseconds,
            _ => throw new ArgumentOutOfRangeException(nameof(stepCode))
        };
        Reset();
        period = (long)nanoseconds * clockRate;
        calibrating = calibration;
        Phase = Acb4000PositioningPhase.InitialCheck;
    }

    public void Tick(bool ready, bool seekComplete, bool trackZero)
    {
        StepIssued = false;
        if (!Busy) return;
        if (delay > 0) delay -= B.NanosecondsPerSecond;
        if (!ready) { Finish(S.NotReady); return; }
        if (Phase == Acb4000PositioningPhase.InitialCheck)
        {
            if (!seekComplete) return;
            if (!calibrating) Phase = Acb4000PositioningPhase.SeekSteps;
            else if (trackZero)
            {
                DirectionIn = true;
                remaining = C.CalibrationExcursion;
                Phase = Acb4000PositioningPhase.LeaveTrackZero;
            }
            else StartZeroSearch();
        }
        if (Phase == Acb4000PositioningPhase.WaitForExcursion)
        {
            if (!seekComplete) return;
            if (trackZero) { Finish(C.NoTrackZero); return; }
            StartZeroSearch();
        }
        else if (Phase == Acb4000PositioningPhase.WaitForTrackZeroStep)
        {
            if (!seekComplete) return;
            if (trackZero) { Finish(); return; }
            if (remaining == 0) { Finish(C.NoTrackZero); return; }
            Phase = Acb4000PositioningPhase.SearchTrackZero;
        }
        if (Phase is not (Acb4000PositioningPhase.SeekSteps or Acb4000PositioningPhase.LeaveTrackZero
            or Acb4000PositioningPhase.SearchTrackZero)) return;
        if (remaining == 0) { Finish(); return; }
        if (delay > 0) return;
        StepIssued = true;
        IssuedSteps++;
        remaining--;
        delay += period;
        if (Phase == Acb4000PositioningPhase.SearchTrackZero)
            Phase = Acb4000PositioningPhase.WaitForTrackZeroStep;
        else if (remaining == 0)
        {
            if (Phase == Acb4000PositioningPhase.LeaveTrackZero)
                Phase = Acb4000PositioningPhase.WaitForExcursion;
            else Finish();
        }
    }

    private void StartZeroSearch()
    {
        DirectionIn = false;
        remaining = C.MaximumCalibrationSteps;
        Phase = Acb4000PositioningPhase.SearchTrackZero;
    }

    /// <summary>Signale l'expiration de l'attente SEEK COMPLETE établie par le contrôleur propriétaire.</summary>
    public void ExpireSeekComplete()
    {
        if (Busy)
        {
            StepIssued = false;
            Finish(C.NoSeekComplete);
        }
    }

    private void Finish(byte error = S.NoSense)
    {
        Error = error;
        Phase = Acb4000PositioningPhase.Complete;
    }

    public void Reset()
    {
        Phase = Acb4000PositioningPhase.Idle;
        period = delay = remaining = IssuedSteps = 0;
        calibrating = StepIssued = DirectionIn = false;
        Error = S.NoSense;
    }
}

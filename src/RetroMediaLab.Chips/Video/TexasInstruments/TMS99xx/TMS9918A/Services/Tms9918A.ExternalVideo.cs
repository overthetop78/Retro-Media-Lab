using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using T = RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Models.Tms9918Timing;
using E = RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Constants.Tms9918ExternalVideoRequirements;

namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Services;

public sealed partial class Tms9918A
{
    private Tms9918ExternalVideoState _externalVideo = new(0, false, 5, true, false, false, 0, false);
    private double? ExternalOutputVoltage => _externalVideo.VideoKnown
        ? Math.Max(0, _externalVideo.VideoVoltage - E.NominalMosDropVolts) : null;

    public void SetExternalVideoVoltage(long timeNanoseconds, double voltage, bool known)
    {
        ValidateVoltage(voltage, 6);
        AdvanceTo(timeNanoseconds);
        if (_externalVideo.VideoVoltage == voltage && _externalVideo.VideoKnown == known) return;
        _externalVideo = _externalVideo with { VideoVoltage = voltage, VideoKnown = known };
        if (!VideoSignal.ExternalVideo) return;
        VideoSignal = VideoSignal with { TimeNanoseconds = TimeNanoseconds, ExternalOutputVoltage = ExternalOutputVoltage };
        bool wasPublishing = _publishing;
        _publishing = true;
        try { VideoSignalProduced?.Invoke(VideoSignal); }
        finally { _publishing = wasPublishing; }
    }

    public void SetResetSyncVoltage(long timeNanoseconds, double voltage, bool known)
    {
        ValidateVoltage(voltage, 15);
        AdvanceTo(timeNanoseconds);
        bool wasSync = _externalVideo.SyncActive;
        _externalVideo = _externalVideo with { ResetSyncVoltage = voltage, ResetSyncKnown = known };
        if (!known) { _qualified = false; return; }
        ApplyResetLevel(timeNanoseconds, voltage < E.ResetLowVolts);
        bool sync = voltage > E.SyncHighVolts;
        if (sync && !wasSync)
            _externalVideo = _externalVideo with { SyncActive = true, SyncPending = true,
                SyncStarted = timeNanoseconds, VerticalSyncRecognized = false };
        else if (!sync)
            _externalVideo = _externalVideo with { SyncActive = false, SyncPending = false,
                VerticalSyncRecognized = false };
        InvalidateClockPredictions();
        PublishCpuBoundary();
    }

    private void ApplyExternalSyncAtPixel()
    {
        if (!_externalVideo.ResetSyncKnown || !_externalVideo.SyncActive) return;
        InvalidateClockPredictions();
        if (_externalVideo.SyncPending)
        {
            _externalVideo = _externalVideo with { SyncPending = false };
            _raster = _raster with { Dot = T.HorizontalSyncStart };
            // Terminer la fraction de cycle en cours, sans effacer les
            // registres CPU ou consommer la demande mémoire interrompue.
            _vramDue = _vramWriteEnd = -1;
            RefreshNextDeadline();
            _vramActive = _ramLoadActive = _cpuVramInService = false;
            _vramBus.ApplyOutputs(TimeNanoseconds, VramPins with
                { RasHigh = true, CasHigh = true, ReadWriteHigh = true, AddressDataKnown = false });
            PublishDeadline();
        }
        if (!_externalVideo.VerticalSyncRecognized
            && TimeNanoseconds - _externalVideo.SyncStarted > E.VerticalSyncMinimumNanoseconds)
        {
            _externalVideo = _externalVideo with { VerticalSyncRecognized = true };
            _raster = _raster with { Line = T.VerticalSyncStart };
        }
    }

    private static void ValidateVoltage(double voltage, double maximum)
    {
        if (!double.IsFinite(voltage) || voltage < 0 || voltage > maximum)
            throw new ArgumentOutOfRangeException(nameof(voltage));
    }
}

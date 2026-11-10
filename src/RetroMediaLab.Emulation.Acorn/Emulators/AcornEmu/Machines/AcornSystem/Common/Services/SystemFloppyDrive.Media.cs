using System.IO;
using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Constants.Intel8271DriveConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.AcornSystem.Common.Services;

internal sealed partial class SystemFloppyDrive
{
    private static readonly IReadOnlyDictionary<string, int> TrackAttributes =
        new Dictionary<string, int> { [C.PhysicalLayoutAttribute] = C.PhysicalLayoutEnabled };

    private void LoadTrack(Drive drive)
    {
        drive.Pulses = []; drive.Pulse = 0;
        if (drive.Image is not { } image) return;
        try
        {
            var revolution = image.ReadTrack(drive.Track, drive.Head, C.EncodingId, CellNanoseconds,
                C.RevolutionNanoseconds, TrackAttributes).Revolutions[0];
            uint position = 0;
            drive.Pulses = revolution.FluxIntervalsNanoseconds.Select(interval => position = checked(position + interval)).ToArray();
            while (drive.Pulse < drive.Pulses.Length && drive.Pulses[drive.Pulse] <= drive.Rotation) drive.Pulse++;
        }
        catch (Exception error) when (error is IOException or ArgumentException or NotSupportedException)
        { drive.Pulses = []; }
    }

    private void PublishWrite(Drive drive, bool continueWriting)
    {
        if (!drive.Writing || drive.Image is not { } image) return;
        drive.Writing = false;
        var duration = checked((uint)(_time - drive.WriteStart));
        var endingPulse = drive.Written.Count != 0 && drive.Written[^1] == duration;
        try
        {
            if (duration != 0) image.WriteTrackWindow(drive.Track, drive.Head, C.EncodingId, CellNanoseconds,
                C.RevolutionNanoseconds, drive.WritePhase, duration,
                drive.Written.Where(offset => !continueWriting || offset < duration).ToArray(),
                TrackAttributes, includeEndingInstant: !continueWriting);
            drive.Writing = continueWriting;
            if (continueWriting) { drive.WriteStart = _time; drive.WritePhase = drive.Rotation; }
        }
        finally
        {
            drive.Written.Clear();
            if (drive.Writing && endingPulse) drive.Written.Add(0);
            LoadTrack(drive);
        }
    }
}

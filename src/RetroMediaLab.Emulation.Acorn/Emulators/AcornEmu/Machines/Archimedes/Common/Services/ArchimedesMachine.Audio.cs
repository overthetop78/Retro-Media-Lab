using C = RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Constants.ArchimedesBoardConstants;

namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Machines.Archimedes.Common.Services;

internal sealed partial class ArchimedesMachine
{
    private readonly List<short> _audio = [];
    private readonly double _speakerFilter = 1 - Math.Exp(-2 * Math.PI * C.SpeakerFilterHz / C.AudioSampleRate);
    private int _audioPhase;
    private double _speakerLeft;
    private double _speakerRight;

    /// <summary>Sortie casque stéréo entrelacée ; la coupure du haut-parleur interne ne coupe pas le casque.</summary>
    public short[] DrainAudio()
    {
        var samples = _audio.ToArray();
        _audio.Clear();
        return samples;
    }

    private void AdvanceAudio(int masterTicks)
    {
        _audioPhase += masterTicks * C.AudioSampleRate;
        if (_audioPhase < C.MasterClockHz) return;
        _audioPhase -= C.MasterClockHz;
        var (left, right) = _vidc.TakeAudioSample();
        _speakerLeft += _speakerFilter * (left - _speakerLeft);
        _speakerRight += _speakerFilter * (right - _speakerRight);
        _audio.Add((short)Math.Clamp(_speakerLeft, short.MinValue, short.MaxValue));
        _audio.Add((short)Math.Clamp(_speakerRight, short.MinValue, short.MaxValue));
    }
}

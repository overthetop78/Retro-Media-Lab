using RetroMediaLab.MediaEngine.Functions;
using C = RetroMediaLab.MediaEngine.Constants.MechanicalSoundConstants;

namespace RetroMediaLab.MediaEngine.Services;

public sealed partial class MediaDriveMechanics
{
    /// <inheritdoc/>
    public short[] RenderAudio(int sampleRate, int sampleCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);
        ArgumentOutOfRangeException.ThrowIfNegative(sampleCount);
        lock (_sync)
        {
            if (_device == C.Cassette)
            {
                var tape = new short[sampleCount];
                if (!_soundEnabled) return tape;
                var position = 0;
                while (_events.TryPeek(out var action) && position < sampleCount)
                {
                    var frames = (long)Math.Ceiling(sampleRate * MechanicalSoundFunctions.Duration(_device, _profile, action.Operation));
                    var count = (int)Math.Min(sampleCount - position, Math.Max(0, frames - _eventOffset));
                    MechanicalSoundFunctions.Render(_device, action.Operation, sampleRate, count, _eventOffset, _profile, _volumePercent)
                        .CopyTo(tape, position);
                    position += count;
                    _eventOffset += count;
                    if (_eventOffset >= frames) { _events.Dequeue(); _eventOffset = 0; }
                }
                if (_tapeOperation != C.None && position < sampleCount)
                {
                    MechanicalSoundFunctions.Render(_device, _tapeOperation, sampleRate, sampleCount - position, _tapeOffset, _profile, _volumePercent)
                        .CopyTo(tape, position);
                    _tapeOffset += sampleCount - position;
                }
                return tape;
            }
            var mixed = new double[sampleCount];
            if (!_soundEnabled) return new short[sampleCount];
            var destination = 0;
            while (destination < sampleCount)
            {
                var count = sampleCount - destination;
                var playMotor = _audibleMotor;
                if (_events.TryPeek(out var sound))
                {
                    if (_eventOffset == 0 && sound.Operation == C.Stop)
                    {
                        _audibleMotor = false;
                        playMotor = false;
                    }
                    var duration = MechanicalSoundFunctions.Duration(_device, _profile, sound.Operation, sound.Distance, sound.Direction, sound.Cylinder);
                    var frames = checked((long)Math.Ceiling(duration * sampleRate));
                    count = (int)Math.Min(count, Math.Max(0, frames - _eventOffset));
                    if (count > 0)
                    {
                        Mix(MechanicalSoundFunctions.Render(_device, sound.Operation, sampleRate, count, _eventOffset,
                            _profile, _volumePercent, sound.Distance, sound.Direction, sound.Cylinder));
                        _eventOffset += count;
                    }
                    if (_eventOffset >= frames)
                    {
                        _events.Dequeue();
                        _eventOffset = 0;
                    }
                }
                if (count == 0) continue;
                if (playMotor)
                {
                    var operation = _state.Writing ? C.Write : _state.Reading ? C.Read : C.Motor;
                    Mix(MechanicalSoundFunctions.Render(_device, operation, sampleRate, count, _motorOffset, _profile, _volumePercent));
                    _motorOffset += count;
                }
                // Une nouvelle commande peut commencer pendant la résonance du mouvement précédent.
                // Les sons ne doivent pas attendre dans une file après la fin des accès matériels.
                for (var voice = _seekVoices.Count - 1; voice >= 0; voice--)
                {
                    var (seek, offset) = _seekVoices[voice];
                    var frames = checked((long)Math.Ceiling(MechanicalSoundFunctions.Duration(_device, _profile,
                        seek.Operation, seek.Distance, seek.Direction, seek.Cylinder) * sampleRate));
                    var remaining = (int)Math.Min(count, Math.Max(0, frames - offset));
                    if (remaining > 0)
                        Mix(MechanicalSoundFunctions.Render(_device, seek.Operation, sampleRate, remaining, offset,
                            _profile, _volumePercent, seek.Distance, seek.Direction, seek.Cylinder));
                    offset += count;
                    if (offset >= frames) _seekVoices.RemoveAt(voice);
                    else _seekVoices[voice] = (seek, offset);
                }
                destination += count;
            }
            return MechanicalSoundFunctions.ProtectMix(mixed);

            void Mix(short[] samples)
            {
                for (var index = 0; index < samples.Length; index++) mixed[destination + index] += samples[index];
            }
        }
    }
}

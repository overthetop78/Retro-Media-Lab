using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Contracts;
using C = RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Constants.Sg1000Constants;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Services;

internal sealed partial class Sg1000Board
{
    public Sg1000BoardState CaptureState()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new(3, (byte[])_cartridgeHash.Clone(), TimeNanoseconds, _nextMasterIndex, _masterHigh,
            _vdpReleased, _cpuReleased, _pressed, _pause, _cpu.CaptureState(), _vdp.CaptureState(),
            _psg.CaptureState(), _decoder.CaptureState(), _gates.CaptureState(),
            _joy.Select(chip => chip.CaptureState()).ToArray(), _ram.CaptureState(),
            _dram.Select(chip => chip.CaptureState()).ToArray(), (byte[])_pixels.Clone(),
            (bool[])_pixelKnown.Clone(), FramesCompleted, _samples.ToArray(), _audioTime,
            _sampleIndex, _sampleStart, _sampleArea, _previousSample, _dcOutput, _audioLevel,
            _romConfiguration,(byte[])_internalGameHash.Clone(),_cartridgeOrigin,
            _cartridgeLow?.CaptureState(),_cartridgeHigh?.CaptureState(),_internalRom?.CaptureState(),_cartridgeRam?.CaptureState());
    }

    public void RestoreState(Sg1000BoardState state)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(state);
        ValidateEnvelope(state);
        // Validate every component in an isolated managed board before touching this instance.
        Sg1000Board? staged = null;
        try
        {
            staged = new(_rom, false,_romConfiguration,_internalGame);
            staged.ApplyState(state);
            ApplyState(state);
        }
        finally { staged?.Dispose(); }
    }

    private void ValidateEnvelope(Sg1000BoardState state)
    {
        if (state.Version != 3 || state.CartridgeHash is null || !state.CartridgeHash.SequenceEqual(_cartridgeHash)
            || state.RomConfiguration != _romConfiguration || state.InternalGameHash is null
            || !state.InternalGameHash.SequenceEqual(_internalGameHash)
            || state.CartridgeOrigin < 0 || state.CartridgeOrigin > state.Time
            || (_cartridgeLow is null) != (state.CartridgeLow is null)
            || (_cartridgeHigh is null) != (state.CartridgeHigh is null)
            || (_internalRom is null) != (state.InternalRom is null)
            || (_cartridgeRam is null) != (state.CartridgeRam is null)
            || (state.CartridgeRam is { } cartridgeRam && cartridgeRam.Time != state.Time - state.CartridgeOrigin)
            || (state.CartridgeLow is { } low && low.Time != state.Time - state.CartridgeOrigin)
            || (state.CartridgeHigh is { } high && high.Time != state.Time - state.CartridgeOrigin)
            || (state.InternalRom is { } internalRom && internalRom.Time != state.Time)
            || state.Time < 0 || state.NextMasterIndex < 1 || state.Pressed > 4095 || state.Frames < 0
            || state.Joy?.Length != 2 || state.Dram?.Length != 8 || state.Pixels?.Length != _pixels.Length
            || state.PixelKnown?.Length != _pixels.Length || state.Samples is null || state.Pixels.Any(value => value > 15)
            || state.AudioTime != state.Time || state.SampleIndex < 1 || state.SampleStart < 0
            || state.SampleStart > state.AudioTime || !double.IsFinite(state.SampleArea) || state.SampleArea < 0
            || !double.IsFinite(state.PreviousSample) || !double.IsFinite(state.DcOutput)
            || state.AudioLevel != state.Psg.Output || state.VdpReleased != (state.Time >= C.VdpReleaseNanoseconds)
            || state.CpuReleased != (state.Time >= C.CpuReleaseNanoseconds)
            || state.MasterHigh != (((state.NextMasterIndex - 1) & 1) != 0))
            throw new ArgumentException(nameof(state));
        long nextEdge = checked((long)((Int128)state.NextMasterIndex * C.NanosecondsPerSecond / (C.CrystalHertz * 2)));
        long previousEdge = checked((long)((Int128)(state.NextMasterIndex - 1) * C.NanosecondsPerSecond / (C.CrystalHertz * 2)));
        long sampleStart = checked((long)((Int128)(state.SampleIndex - 1) * C.NanosecondsPerSecond / C.SampleRate));
        long nextSample = checked((long)((Int128)state.SampleIndex * C.NanosecondsPerSecond / C.SampleRate));
        if (nextEdge <= state.Time || previousEdge > state.Time || sampleStart != state.SampleStart || nextSample <= state.AudioTime)
            throw new ArgumentException(nameof(state));
    }

    private void ApplyState(Sg1000BoardState state)
    {
        _cpu.RestoreState(state.Cpu); _vdp.RestoreState(state.Vdp); _psg.RestoreState(state.Psg);
        _decoder.RestoreState(state.Decoder); _gates.RestoreState(state.Gates);
        for (int chip = 0; chip < 2; chip++) _joy[chip].RestoreState(state.Joy[chip]);
        _ram.RestoreState(state.Ram);
        if (_cartridgeLow is { } low) low.RestoreState(state.CartridgeLow!);
        if (_cartridgeHigh is { } high) high.RestoreState(state.CartridgeHigh!);
        if (_internalRom is { } internalRom) internalRom.RestoreState(state.InternalRom!);
        if (_cartridgeRam is { } cartridgeRam) cartridgeRam.RestoreState(state.CartridgeRam!);
        _cartridgeOrigin = state.CartridgeOrigin;
        for (int chip = 0; chip < 8; chip++) _dram[chip].RestoreState(state.Dram[chip]);
        TimeNanoseconds = state.Time; _nextMasterIndex = state.NextMasterIndex; _masterHigh = state.MasterHigh;
        _vdpReleased = state.VdpReleased; _cpuReleased = state.CpuReleased;
        _pressed = state.Pressed; _pause = state.Pause; FramesCompleted = state.Frames;
        RefreshControllerContacts();
        state.Pixels.CopyTo(_pixels, 0); state.PixelKnown.CopyTo(_pixelKnown, 0);
        _samples.Clear(); _samples.AddRange(state.Samples);
        _audioTime = state.AudioTime; _sampleIndex = state.SampleIndex; _sampleStart = state.SampleStart;
        _sampleArea = state.SampleArea; _previousSample = state.PreviousSample;
        _dcOutput = state.DcOutput; _audioLevel = state.AudioLevel;
        _vramWirePins = _vdp.VramPins; _vramWiresInitialized = true;
        for (int bit = 0; bit < 8; bit++) CacheDramOutput(bit, _dram[bit].Output);
        RefreshVideoMemoryBoundaries();
        ConnectRangeBus();
        ConnectClockDomains();
        ResolveRangeCpuData();
    }
}

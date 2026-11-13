using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums;
using K = RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums.Tms9918VramSlotKind;

namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Services;

public sealed partial class Tms9918A
{
    public Tms9918AState CaptureState()
    {
        EnsureStateBoundary();
        if (_cpuInputTime > _time)
            throw new InvalidOperationException("Synchronize the video domain before observing its complete state.");
        ResolveVramReadInputs(_time);
        return new(5, _time, _registers.Capture(), _status.Value, _protocol.Capture(),
            _memory.Capture(), new(_cpuPins, _readMode, _readQualified, _reset, _resetStarted,
                _qualified, _dataDue < 0 ? null : _dataDue, _dataTargetDriven), _writeBus.Capture(), _statusReadBus.Capture(),
            CpuOutputs, _clock, _raster, VideoSignal, _vramBus.Capture(),
            new(_vramDue < 0 ? null : _vramDue, _vramWriteEnd < 0 ? null : _vramWriteEnd,
                _vramStarted, _vramStartedHalfEdge, _cpuReadyHalfEdge,
                _vramStep, _vramActive, _vramWrite, _vramAddressKnown, _vramRow, _vramColumn,
                _vramWriteData, _refreshRow, (int)_vramSlot.Kind, _vramSlot.Index,
                (int)_ramSlot.Kind, _ramSlot.Index, _ramLoadActive, _ramDataKnown,
                _ramCpuWrite, _ramData, _ramLoadHalfEdge, _cpuVramInService),
            _background.Capture(), CaptureSpriteState(), _externalVideo,
            _cpuTiming.AccessNanoseconds, _cpuTiming.ReleaseNanoseconds, _status.SpriteCounter);
    }

    public void RestoreState(Tms9918AState state)
    {
        EnsureStateBoundary();
        ArgumentNullException.ThrowIfNull(state);
        ValidateState(state);
        if (state.CpuAccessNanoseconds != _cpuTiming.AccessNanoseconds
            || state.CpuReleaseNanoseconds != _cpuTiming.ReleaseNanoseconds)
            throw new ArgumentException("State CPU timing profile does not match the VDP instance.");
        // Préparer et valider toutes les parties avant la première mutation.
        var registers = new Tms9918Registers(state.Registers);
        var status = new Tms9918Status(state.Status, state.SpriteCounter);
        var protocol = new Tms9918CpuProtocol(state.Control);
        var memory = new Tms9918CpuMemoryPort(state.Memory);
        var write = new Tms9918CpuWriteBus(state.CpuWriteBus);
        var read = new Tms9918StatusReadBus(state.StatusReadBus, status, protocol);
        var vram = new Tms9918VramBus(state.VramBus);
        var background = new Tms9918BackgroundPipeline(state.Background);
        var selected = (int[])state.Sprites.SelectedNumbers.Clone();
        var preparing = (Tms9918SpriteChannelState[])state.Sprites.Preparing.Clone();
        var active = (Tms9918SpriteChannelState[])state.Sprites.Active.Clone();

        WithdrawVramReadProgram();
        DetachMasterClockSource();
        _clockInputBoundary = long.MaxValue;

        _writeBus.WriteCaptured = null;
        _statusReadBus.StatusAcknowledged = null;
        _vramBus.PinsChanged = null;
        _vramBus.ReadCaptured = null;
        _registers = registers; _status = status; _protocol = protocol; _memory = memory;
        _vramReadWaveRouted = false;
        _writeBus = write; _statusReadBus = read; _vramBus = vram; _background = background;
        _time = state.TimeNanoseconds;
        _cpuInputTime = Math.Max(state.CpuWriteBus.TimeNanoseconds, state.StatusReadBus.TimeNanoseconds);
        CpuOutputs = state.CpuOutputs; _clock = state.Clock; _raster = state.Raster;
        VideoSignal = state.VideoSignal;
        _externalVideo = state.ExternalVideo;
        var cpu = state.CpuInterface;
        _cpuPins = cpu.Pins; _readMode = cpu.ReadMode; _readQualified = cpu.ReadQualified;
        _reset = cpu.Reset; _resetStarted = cpu.ResetStarted; _qualified = cpu.Qualified;
        _dataDue = cpu.DataDue ?? -1; _dataTargetDriven = cpu.DataTargetDriven;
        var bus = state.VramSequencer;
        _vramDue = bus.Due ?? -1; _vramWriteEnd = bus.WriteEnd ?? -1; _vramStarted = bus.Started;
        _vramStartedHalfEdge = bus.StartedHalfEdge; _cpuReadyHalfEdge = bus.CpuReadyHalfEdge;
        _vramStep = bus.Step; _vramActive = bus.Active; _vramWrite = bus.Write;
        _vramAddressKnown = bus.AddressKnown; _vramRow = bus.Row; _vramColumn = bus.Column;
        _vramWriteData = bus.WriteData; _refreshRow = bus.RefreshRow;
        _vramSlot = new((K)bus.SlotKind, bus.SlotIndex); _ramSlot = new((K)bus.RamSlotKind, bus.RamSlotIndex);
        _ramLoadActive = bus.RamLoadActive; _ramDataKnown = bus.RamDataKnown;
        _ramCpuWrite = bus.RamCpuWrite; _ramData = bus.RamData;
        _ramLoadHalfEdge = bus.RamLoadHalfEdge; _cpuVramInService = bus.CpuInService;
        var sprites = state.Sprites;
        _spriteScanLine = sprites.ScanLine; _spriteSelectedCount = sprites.SelectedCount;
        _spriteScanTerminated = sprites.ScanTerminated; _spriteScanKnown = sprites.ScanKnown;
        Array.Copy(selected, _selectedSprites, 5); Array.Copy(preparing, _preparingSprites, 4);
        Array.Copy(active, _activeSprites, 4);
        _preparingSpriteLine = sprites.PreparingLine; _preparingSpritesKnown = sprites.PreparingKnown;
        _activeSpriteLine = sprites.ActiveLine; _activeSpritesKnown = sprites.ActiveKnown;
        _writeBus.WriteCaptured = AcceptCpuWrite;
        _statusReadBus.StatusAcknowledged = _ => PublishCpuOutputs();
        _vramBus.ReadCaptured = CaptureVramRead;
        UpdateVramPinObservation();
        // Aucun faux front de restauration. La carte rétablit ses abonnements
        // et échéances depuis l'état restauré de l'ensemble de ses composants.
        RefreshNextDeadline();
        _announcedDeadline = _nextDue;
    }

    private void EnsureStateBoundary()
    {
        if (_publishing || _deliveringClock)
            throw new InvalidOperationException("VDP state must be transferred between input transactions.");
    }

    private static void ValidateState(Tms9918AState state)
    {
        if (state.Version != 5 || state.TimeNanoseconds < 0 || state.SpriteCounter > 31)
            throw new ArgumentException("Invalid TMS9918 state version/time.");
        long time = state.TimeNanoseconds;
        ValidateVoltage(state.ExternalVideo.VideoVoltage, 6);
        ValidateVoltage(state.ExternalVideo.ResetSyncVoltage, 15);
        if (state.ExternalVideo.SyncStarted < 0 || state.ExternalVideo.SyncStarted > time)
            throw new ArgumentException("Invalid TMS9918 external sync state.");
        if (state.Clock.HalfEdgePhase is < 0 or >= 12 || state.Clock.HalfEdges < 0
            || state.Clock.GromHalfEdgePhase is < 0 or >= 48
            || state.Raster.Dot is < 0 or >= 342 || state.Raster.Line is < 0 or >= 262 || state.Raster.Frame < 0
            || state.CpuOutputs.TimeNanoseconds < 0 || state.CpuOutputs.TimeNanoseconds > time
            || state.VideoSignal.TimeNanoseconds < 0 || state.VideoSignal.TimeNanoseconds > time
            || state.CpuWriteBus.TimeNanoseconds > time || state.StatusReadBus.TimeNanoseconds > time
            || state.VramBus.TimeNanoseconds > time || state.CpuInterface.ResetStarted < 0
            || state.CpuInterface.ResetStarted > time)
            throw new ArgumentException("Invalid TMS9918 clock/interface state.");
        if (state.CpuOutputs.IntHigh != !((state.Status & 0x80) != 0 && (state.Registers.R1 & 0x20) != 0))
            throw new ArgumentException("Inconsistent TMS9918 interrupt output.");
        ValidateDue(state.CpuInterface.DataDue, time);
        var bus = state.VramSequencer;
        ValidateDue(bus.Due, time); ValidateDue(bus.WriteEnd, time);
        ValidateSlot(bus.SlotKind, bus.SlotIndex); ValidateSlot(bus.RamSlotKind, bus.RamSlotIndex);
        if (bus.Started < 0 || bus.Started > time || bus.StartedHalfEdge < 0
            || bus.StartedHalfEdge > state.Clock.HalfEdges || bus.CpuReadyHalfEdge < 0
            || bus.Step is < 0 or > 8 || bus.RefreshRow > 127 || bus.Row > 127 || bus.Column > 127
            || bus.RamLoadHalfEdge < 0 || (bus.Active && (bus.Due is null || bus.Step is < 1 or > 7))
            || (!bus.Active && bus.Due is not null)
            || (bus.RamLoadActive && bus.RamLoadHalfEdge < state.Clock.HalfEdges)
            || (bus.CpuInService && state.Memory.PendingOperation == Tms9918CpuVramOperation.None))
            throw new ArgumentException("Invalid TMS9918 VRAM sequencer state.");
        var sprites = state.Sprites;
        ArgumentNullException.ThrowIfNull(sprites);
        if (sprites.SelectedNumbers is not { Length: 5 } || sprites.Preparing is not { Length: 4 }
            || sprites.Active is not { Length: 4 } || sprites.SelectedCount is < 0 or > 5
            || (sprites.SelectedCount == 5 && !sprites.ScanTerminated)
            || sprites.ScanLine is < -1 or >= 262 || sprites.PreparingLine is < -1 or >= 262
            || sprites.ActiveLine is < -1 or >= 262)
            throw new ArgumentException("Invalid TMS9918 sprite pipeline.");
        for (int index = 0; index < 5; index++)
        {
            int number = sprites.SelectedNumbers[index];
            if (number is < 0 or > 31 || (index > 0 && index < sprites.SelectedCount
                && number <= sprites.SelectedNumbers[index - 1]))
                throw new ArgumentException("Invalid TMS9918 sprite selection FIFO.");
        }
        foreach (var sprite in sprites.Preparing.Concat(sprites.Active))
            if (sprite.KnownMask > 63 || sprite.PatternRow is < 0 or > 15)
                throw new ArgumentException("Invalid TMS9918 sprite channel.");
    }

    private static void ValidateDue(long? due, long time)
    {
        if (due < time) throw new ArgumentException("VDP deadline precedes restored time.");
    }

    private static void ValidateSlot(int kind, int index)
    {
        if (kind is < 0 or > byte.MaxValue || !Enum.IsDefined((K)kind))
            throw new ArgumentException("Invalid VDP RAM destination.");
        int maximum = (K)kind switch
        {
            K.Cpu => 0, K.Refresh => 63, K.BackgroundName or K.BackgroundColour or K.BackgroundPattern => 39,
            K.SpriteVisibility => 31, _ => 3,
        };
        if (index < 0 || index > maximum) throw new ArgumentException("Invalid VDP RAM destination index.");
    }
}

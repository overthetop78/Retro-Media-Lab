using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums;
using V = RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Models.Tms9918VramTiming;
using K = RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Enums.Tms9918VramSlotKind;

namespace RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Services;

public sealed partial class Tms9918A
{
    private Tms9918VramBus _vramBus;
    private Tms9918BackgroundPipeline _background = new(default);
    private long? _vramDue, _vramWriteEnd;
    private long _vramStarted, _vramStartedHalfEdge, _cpuReadyHalfEdge;
    private int _vramStep;
    private bool _vramActive, _vramWrite, _vramAddressKnown;
    private byte _vramRow, _vramColumn, _vramWriteData, _refreshRow;
    private Tms9918VramSlot _vramSlot, _ramSlot;
    private bool _ramLoadActive, _ramDataKnown, _ramCpuWrite;
    private byte _ramData;
    private long _ramLoadHalfEdge;
    private bool _cpuVramInService;

    public Tms9918VramPins VramPins => _vramBus.Pins;
    public Action<Tms9918VramSignal>? VramPinsChanged { get; set; }

    public void SetVramReadData(long timeNanoseconds, byte data, bool known)
    {
        AdvanceTo(timeNanoseconds);
        _vramBus.SetInputData(timeNanoseconds, data, known);
    }

    private void ArmCpuVramRequest() => _cpuReadyHalfEdge = checked(_clock.HalfEdges + V.CpuPreparationHalfEdges);

    private void SelectVramSlot(int dot, int line)
    {
        int nextLine = line == 261 ? 0 : line + 1;
        var slot = Tms9918VramSchedule.At(dot, _registers.Mode, false);
        // Préparer aussi les sprites des premières lignes : leur scan commence
        // deux lignes avant la sortie, puis les quatre canaux sont chargés.
        // Garder le calendrier existant ; la phase verticale est nominale,
        // pas une nouvelle caractérisation des broches pendant la bordure.
        bool spritePreparation = line == 261 && slot.Kind is >= K.SpriteVisibility and <= K.SpritePatternRight
            || line == 260 && dot >= 316 && slot.Kind == K.SpriteVisibility;
        bool refresh = !_registers.DisplayEnabled || (line >= 192
            && !spritePreparation && !(nextLine == 0 && dot >= 332));
        if (refresh) slot = Tms9918VramSchedule.At(dot, _registers.Mode, true);
        Tms9918FetchAddress address;
        bool write = false;
        byte data = 0;
        int targetLine = dot >= (_registers.UsesTextTiming ? 338 : 332) ? nextLine : line;
        switch (slot.Kind)
        {
            case K.Cpu:
                if (!_memory.HasPendingRequest || _cpuVramInService || _clock.HalfEdges < _cpuReadyHalfEdge) return;
                var request = _memory.CurrentRequest;
                address = new(request.Address ?? 0, request.Address.HasValue && request.Qualified);
                write = request.Operation == Tms9918CpuVramOperation.Write;
                data = request.Data;
                break;
            case K.Refresh:
                address = new(_refreshRow, true);
                _refreshRow = (byte)((_refreshRow + 1) & 127);
                break;
            case K.BackgroundName:
                if (targetLine >= 192) return;
                address = Tms9918BackgroundAddresses.Name(_registers, targetLine, slot.Index);
                break;
            case K.BackgroundColour:
                if (targetLine >= 192) return;
                address = Tms9918BackgroundAddresses.Colour(_registers, targetLine, _background.Name, _background.NameKnown);
                break;
            case K.BackgroundPattern:
                if (targetLine >= 192) return;
                address = Tms9918BackgroundAddresses.Pattern(_registers, targetLine, _background.Name, _background.NameKnown);
                break;
            default:
                if (!SelectSpriteAddress(slot, line, out address)) return;
                break;
        }
        BeginVramCycle(slot, address, write, data);
    }

    private void BeginVramCycle(Tms9918VramSlot slot, Tms9918FetchAddress address, bool write, byte data)
    {
        if (_vramActive) { _qualified = false; return; } // source hors cadence du profil en cours
        ushort value = address.Address;
        if (_registers.SixteenKilobyteVram)
        { _vramRow = (byte)(value & 127); _vramColumn = (byte)(value >> 7); }
        else
        { _vramRow = (byte)((value & 63) | ((value >> 6) & 64)); _vramColumn = (byte)(((value >> 6) & 63) | ((value >> 7) & 64)); }
        _vramSlot = slot;
        if (slot.Kind == K.Cpu) _cpuVramInService = true;
        _vramAddressKnown = address.Qualified;
        _vramWrite = write;
        _vramWriteData = data;
        _vramActive = true;
        _vramStarted = TimeNanoseconds;
        _vramStartedHalfEdge = _clock.HalfEdges;
        _vramStep = 1;
        _vramDue = checked(TimeNanoseconds + V.RasAssert);
        _vramBus.ApplyOutputs(TimeNanoseconds, VramPins with
            { AddressData = _vramRow, AddressDataKnown = address.Qualified });
        PublishDeadline();
    }

    private void AdvanceVramSequence()
    {
        var pins = VramPins;
        int nextOffset;
        switch (_vramStep++)
        {
            case 1:
                pins = pins with { RasHigh = false };
                nextOffset = V.CasAssert;
                break;
            case 2:
                pins = pins with { CasHigh = false, AddressData = _vramColumn, AddressDataKnown = _vramAddressKnown };
                nextOffset = V.Column;
                break;
            case 3:
                pins = pins with { AddressData = _vramColumn, AddressDataKnown = _vramAddressKnown };
                nextOffset = _vramWrite ? V.WriteData : V.RasRelease;
                if (!_vramWrite) _vramStep = 6;
                break;
            case 4:
                // Pas de second bufferCPU capturé à la sélection d'adresse :
                // présenter le registre partagé à la vraie phase de données.
                if (_vramSlot.Kind == K.Cpu)
                {
                    _vramWriteData = _memory.Data;
                    _vramAddressKnown &= _memory.CurrentRequest.Qualified && _memory.DataKnown;
                }
                pins = pins with { AddressData = _vramWriteData, AddressDataKnown = _vramAddressKnown };
                nextOffset = V.WriteAssert;
                break;
            case 5:
                pins = pins with { ReadWriteHigh = false };
                nextOffset = V.RasRelease;
                break;
            case 6:
                pins = pins with { RasHigh = true };
                nextOffset = V.CasRelease;
                break;
            case 7:
                _vramDue = null;
                if (_vramWrite)
                {
                    QueueRamLoad(0, _vramAddressKnown, true);
                    _vramWriteEnd = checked(_vramStarted + V.WriteRelease);
                }
                _vramBus.ApplyOutputs(TimeNanoseconds, pins with { CasHigh = true });
                _vramActive = false;
                return;
            default: throw new InvalidOperationException("Invalid VDP VRAM sequencer phase.");
        }
        _vramDue = checked(_vramStarted + nextOffset);
        _vramBus.ApplyOutputs(TimeNanoseconds, pins);
    }

    private void CaptureVramRead(Tms9918VramRead read)
    {
        if (!_vramActive || _vramWrite || _vramSlot.Kind == K.Refresh) return;
        QueueRamLoad(read.Data, read.DataKnown && _vramAddressKnown, false);
    }

    private void QueueRamLoad(byte data, bool known, bool cpuWrite)
    {
        if (_ramLoadActive) throw new InvalidOperationException("VDP RAM latch was not consumed at its phase.");
        _ramSlot = _vramSlot; _ramData = data; _ramDataKnown = known; _ramCpuWrite = cpuWrite;
        _ramLoadHalfEdge = checked(_vramStartedHalfEdge + V.RamLatchHalfEdges);
        _ramLoadActive = true;
    }

    private void ApplyRamLoad()
    {
        _ramLoadActive = false;
        switch (_ramSlot.Kind)
        {
            case K.Cpu:
                _cpuVramInService = false;
                if (_ramCpuWrite) _memory.CompleteWrite();
                else _memory.CompleteRead(_ramData, _ramDataKnown);
                break;
            case K.BackgroundName: _background.LoadName(_ramData, _ramDataKnown); break;
            case K.BackgroundColour: _background.LoadColour(_ramData, _ramDataKnown); break;
            case K.BackgroundPattern: _background.LoadPattern(_ramData, _ramDataKnown); break;
            default: LoadSpriteByte(_ramSlot, _ramData, _ramDataKnown); break;
        }
    }

    private void CancelVramOnReset()
    {
        _vramDue = _vramWriteEnd = null;
        _vramActive = _ramLoadActive = _cpuVramInService = false;
        var state = _background.Capture();
        _background = new(state with { NameKnown = false, PatternKnown = false, ColourKnown = false,
            ActivePatternKnown = false, ActiveColourKnown = false });
        _vramBus.ApplyOutputs(TimeNanoseconds, VramPins with
            { RasHigh = true, CasHigh = true, ReadWriteHigh = true, AddressDataKnown = false });
    }
}

using RetroMediaLab.Chips.Logic.Common.Enums;
using RetroMediaLab.Chips.Memory.Fujitsu.MB81xx.MB8118.Contracts;
using RetroMediaLab.Chips.Video.TexasInstruments.TMS99xx.TMS9918A.Contracts;
using RetroMediaLab.Chips.EmuChip.Video.TexasInstruments.TMS99xx.TMS9918A.Services;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.SG1000.Services;

internal sealed partial class Sg1000Board
{
    private bool _vramWiresInitialized;
    private Tms9918VramPins _vramWirePins;
    private byte _vramReadLevels, _vramReadKnown;
    private bool _collectingDramData, _hasCoincidentVramWord, _coincidentVramTransition;
    private long _coincidentVramTime;
    private ushort _coincidentVramWord;
    private bool _vramOutputsRepresented;
    private static bool? Level(LogicPinState pin) => pin == LogicPinState.High ? true : pin == LogicPinState.Low ? false : null;

    private void OnVramPins(Tms9918VramSignal signal)
    {
        if (_vramOutputsRepresented) WithdrawVramOutputPrograms();
        var pins = signal.Pins;
        var previous = _vramWirePins;
        byte addressData = pins.AddressData;
        bool known = pins.AddressDataKnown, rasHigh = pins.RasHigh,
            casHigh = pins.CasHigh, writeHigh = pins.ReadWriteHigh;
        int changes = addressData ^ previous.AddressData;
        bool all = !_vramWiresInitialized || (changes & 127) != 0
            || known != previous.AddressDataKnown || rasHigh != previous.RasHigh
            || casHigh != previous.CasHigh || writeHigh != previous.ReadWriteHigh;
        _vramWirePins = pins; _vramWiresInitialized = true;
        byte address = (byte)(addressData & 127);
        for (int index = 0; index < 8; index++)
        {
            if (!all && (changes & (1 << index)) == 0) continue;
            _dramExplicitPinDeliveries++;
            _dram[index].ReceiveMultiplexedPins(signal.TimeNanoseconds, address, known,
                rasHigh, casHigh, writeHigh, known ? (addressData & (1 << index)) != 0 : null);
        }
        PublishVideoInputBoundary();
    }

    private bool ReceiveVramReadCycle(in Tms9918VramReadCycle cycle)
    {
        // The next explicit fallback must deliver all pins: the old per-phase
        // wire cache is intentionally not walked during a represented cycle.
        _vramWiresInitialized = false;
        _vramReadWaveDeliveries++;
        var common = new Mb8118ReadCycle(cycle.RowTime, cycle.RasFall, cycle.CasFall,
            cycle.RasRise, cycle.CasRise, cycle.Row, cycle.Column, cycle.AddressKnown, cycle.AddressKnown, null, null);
        var prepared = _dram[0].PrepareReadProgram(in common);
        bool addressKnown = cycle.AddressKnown;
        byte row = cycle.Row, column = cycle.Column;
        Span<Mb8118ReadOutputProgram> programs = stackalloc Mb8118ReadOutputProgram[8];
        byte dataKnown = addressKnown ? (byte)255 : (byte)0;
        bool accepted = _dramReadPort!.ReceiveReadProgram(in prepared, row, dataKnown, column, dataKnown, programs);
        if (accepted)
        {
            DramReadOutputPrograms += 8;
            RepresentVramOutputs(programs);
        }
        else
        {
            DramReadOutputFallbacks++;
            WithdrawVramOutputPrograms();
        }
        PublishVideoInputBoundary();
        return true;
    }

    private void CancelVramReadCycle(long time)
    {
        _vramOutputsRepresented = false;
        _vdp.WithdrawVramReadProgram();
        foreach (var memory in _dram) memory.CancelReadCycle(time);
        for (int bit = 0; bit < 8; bit++) CacheDramOutput(bit, _dram[bit].Output);
        PublishVideoInputBoundary();
    }

    private void WithdrawVramOutputPrograms()
    {
        _vramOutputsRepresented = false;
        long time = _vdp.TimeNanoseconds;
        _vdp.WithdrawVramReadProgram();
        foreach (var memory in _dram) memory.WithdrawReadOutputs(time);
        for (int bit = 0; bit < 8; bit++) CacheDramOutput(bit, _dram[bit].Output);
        PublishVideoInputBoundary();
    }

    private void RepresentVramOutputs(ReadOnlySpan<Mb8118ReadOutputProgram> programs)
    {
        for (int bit = 0; bit < 8; bit++)
            CacheDramOutput(bit, programs[bit].Initial);
        _vdp.WithdrawVramReadProgram();

        Span<int> positions = stackalloc int[8];
        positions.Clear();
        Span<Tms9918VramReadInput> words = stackalloc Tms9918VramReadInput[48];
        int count = 0;
        byte levels = _vramReadLevels, known = _vramReadKnown;
        if (!ComposeReleasedVramOutputs(programs, words, ref count)
            && !ComposeAlignedVramOutputs(programs, words, ref count, ref levels, ref known))
        while (true)
        {
            long date = long.MaxValue;
            for (int bit = 0; bit < 8; bit++)
            {
                while (positions[bit] < 3 && programs[bit].TimeAt(positions[bit]) < 0) positions[bit]++;
                if (positions[bit] < 3) date = Math.Min(date, programs[bit].TimeAt(positions[bit]));
            }
            if (date == long.MaxValue) break;
            bool first = true, transition = false;
            ushort previousWord = (ushort)(levels | (known == 255 ? 256 : 0));
            for (int bit = 0; bit < 8; bit++)
            {
                while (positions[bit] < 3 && programs[bit].TimeAt(positions[bit]) == date)
                {
                    var value = programs[bit].ValueAt(positions[bit]++);
                    byte mask = (byte)(1 << bit);
                    levels = (byte)((levels & ~mask) | (value == LogicPinState.High ? mask : 0));
                    known = (byte)((known & ~mask) | (value is LogicPinState.Low or LogicPinState.High ? mask : 0));
                    ushort word = (ushort)(levels | (known == 255 ? 256 : 0));
                    if (first)
                    {
                        words[count++] = new(date, levels, known == 255);
                        first = false;
                    }
                    else transition |= previousWord != word;
                    previousWord = word;
                }
            }
            if (transition) words[count++] = new(date, levels, known == 255, true);
        }
        // Preparing the shared wire word never advances or merges the eight
        // cell owners. Only accepted, already represented callbacks disappear.
        _vdp.ReceiveVramReadProgram(words[..count]);
        VramReadProgramWords += count;
        _vramOutputsRepresented = true;
    }

    private static bool ComposeReleasedVramOutputs(ReadOnlySpan<Mb8118ReadOutputProgram> programs,
        Span<Tms9918VramReadInput> words, ref int count)
    {
        long read = programs[0].ReadTime, release = programs[0].ReleaseTime;
        if (read < 0 || release < 0) return false;
        byte levels = 0;
        bool known = true;
        for (int bit = 0; bit < 8; bit++)
        {
            ref readonly var program = ref programs[bit];
            if (program.Initial != LogicPinState.HighImpedance || program.PreviousTime >= 0
                || program.ReadTime != read || program.ReleaseTime != release) return false;
            if (program.Read == LogicPinState.High) levels |= (byte)(1 << bit);
            known &= program.Read is LogicPinState.Low or LogicPinState.High;
        }
        // From eight released drivers each data bit changes at most once.
        // Knowledge can only become complete at the final driver. During
        // release the data bits only clear and knowledge cannot become true.
        // Thus the first and final words capture the exact same-date excursion
        // summary without rebuilding eight intermediate words per phase.
        byte first = (byte)(levels & 1);
        words[count++] = new(read, first, false);
        if (levels != first || known) words[count++] = new(read, levels, known, true);
        first = (byte)(levels & 254);
        words[count++] = new(release, first, false);
        if (first != 0) words[count++] = new(release, 0, false, true);
        return true;
    }

    private static bool ComposeAlignedVramOutputs(ReadOnlySpan<Mb8118ReadOutputProgram> programs,
        Span<Tms9918VramReadInput> words, ref int count, ref byte levels, ref byte known)
    {
        // The connected packages normally share their electrical timing. Form
        // that grid once instead of repeatedly searching eight next dates.
        // Absent transitions do not create an input event. Distinct timings
        // retain the general ordered merge above.
        Span<long> dates = stackalloc long[3];
        dates.Fill(-1);
        for (int stage = 0; stage < 3; stage++)
        for (int bit = 0; bit < 8; bit++)
        {
            long date = programs[bit].TimeAt(stage);
            if (date < 0) continue;
            if (dates[stage] >= 0 && dates[stage] != date) return false;
            dates[stage] = date;
        }
        for (int stage = 0; stage < 3; stage++)
        {
            long date = dates[stage];
            if (date < 0) continue;
            bool first = true, transition = false;
            ushort previous = (ushort)(levels | (known == 255 ? 256 : 0));
            for (int bit = 0; bit < 8; bit++)
            {
                if (programs[bit].TimeAt(stage) < 0) continue;
                var value = programs[bit].ValueAt(stage);
                byte mask = (byte)(1 << bit);
                levels = (byte)((levels & ~mask) | (value == LogicPinState.High ? mask : 0));
                known = (byte)((known & ~mask) | (value is LogicPinState.Low or LogicPinState.High ? mask : 0));
                ushort word = (ushort)(levels | (known == 255 ? 256 : 0));
                if (first) { words[count++] = new(date, levels, known == 255); first = false; }
                else transition |= previous != word;
                previous = word;
            }
            if (transition) words[count++] = new(date, levels, known == 255, true);
        }
        return true;
    }
    private void CacheDramOutput(int bit, LogicPinState value)
    {
        byte mask = (byte)(1 << bit);
        _vramReadLevels = (byte)((_vramReadLevels & ~mask) | (value == LogicPinState.High ? mask : 0));
        _vramReadKnown = (byte)((_vramReadKnown & ~mask) | (value is LogicPinState.High or LogicPinState.Low ? mask : 0));
    }
    private void UpdateVramReadData(long time, int bit, LogicPinState value)
    {
        _dramOutputNotifications++;
        CacheDramOutput(bit, value);
        ushort word = (ushort)(_vramReadLevels | (_vramReadKnown == 255 ? 256 : 0));
        if (_collectingDramData && time == _coincidentVramTime)
        {
            if (_hasCoincidentVramWord)
            {
                _coincidentVramTransition |= word != _coincidentVramWord;
                _coincidentVramWord = word;
                return;
            }
            // The first delivery drains all VDP responses at this date before
            // its input changes, exactly as explicit pin delivery does. Capture
            // the argument before entering the VDP, since nested callbacks may
            // change the board's wire cache during that advance.
            _vramDataDeliveries++;
            _vdp.SetVramReadData(time, (byte)word, (word & 256) != 0);
            _coincidentVramWord = word;
            _coincidentVramTransition = false;
            _hasCoincidentVramWord = true;
            _vramDataGroups++;
            return;
        }
        FlushCoincidentVramData();
        _vramDataDeliveries++;
        _vdp.SetVramReadData(time, _vramReadLevels, _vramReadKnown == 255);
    }

    private void FlushCoincidentVramData()
    {
        if (!_hasCoincidentVramWord) return;
        bool transitioned = _coincidentVramTransition;
        ushort word = _coincidentVramWord;
        _hasCoincidentVramWord = false;
        _coincidentVramTransition = false;
        if (!transitioned) return;
        _vramDataDeliveries++;
        _vdp.SetVramReadData(_coincidentVramTime, (byte)word, (word & 256) != 0, true);
    }
}

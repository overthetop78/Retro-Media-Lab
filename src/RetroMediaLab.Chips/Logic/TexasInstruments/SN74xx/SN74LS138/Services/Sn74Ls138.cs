using System.Numerics;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS138.Enums;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS138.Interfaces;
using RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS138.Models;
using C = RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS138.Constants.Sn74Ls138Constants;

namespace RetroMediaLab.Chips.Logic.TexasInstruments.SN74xx.SN74LS138.Services;

/// <summary>Clockless 3-to-8 decoder with six locally retained inputs and physical path delays.</summary>
public sealed partial class Sn74Ls138 : ISn74Ls138
{
    private readonly long[] _due = new long[C.Outputs];
    // Rows: unknown, high, low. Columns: select-2, select-3, enable-2, enable-3.
    private readonly long[][] _delays;
    private byte _inputs, _inputKnown, _levels, _known, _targets, _targetKnown;
    private byte _pending, _observed = byte.MaxValue;
    private long _time, _next, _publishedNext;
    private bool _resolving;
    private Action<long?>? _nextChanged;
    private Action<int, bool?>? _outputChanged;

    public Sn74Ls138(Sn74Ls138Timing timing)
    {
        ArgumentNullException.ThrowIfNull(timing);
        Timing = timing;
        _delays =
        [
            [Math.Max(timing.SelectTwoRise, timing.SelectTwoFall), Math.Max(timing.SelectThreeRise, timing.SelectThreeFall),
                Math.Max(timing.EnableTwoRise, timing.EnableTwoFall), Math.Max(timing.EnableThreeRise, timing.EnableThreeFall)],
            [timing.SelectTwoRise, timing.SelectThreeRise, timing.EnableTwoRise, timing.EnableThreeRise],
            [timing.SelectTwoFall, timing.SelectThreeFall, timing.EnableTwoFall, timing.EnableThreeFall]
        ];
    }

    public Sn74Ls138Timing Timing { get; }
    public long TimeNanoseconds => _time;
    public long? NextEventNanoseconds => _next == 0 ? null : _next;
    public Action<long?>? NextEventNanosecondsChanged { get => _nextChanged; set => _nextChanged = value; }
    public Action<int, bool?>? OutputChanged { get => _outputChanged; set => _outputChanged = value; }
    public byte OutputLevels => _levels;
    public byte OutputKnown => _known;

    public bool? GetOutput(int output)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)output, (uint)C.Outputs);
        var mask = 1 << output;
        return (_known & mask) == 0 ? null : (_levels & mask) != 0;
    }
    public void SetObservedOutputs(byte mask)
    {
        if (_observed == mask) return;
        _observed = mask;
        PublishDeadline();
    }

    public void SetInput(Sn74Ls138Input pin, bool? high)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((byte)pin, (byte)Sn74Ls138Input.G2B);
        var mask = 1 << (int)pin;
        var known = high.HasValue ? _inputKnown | mask : _inputKnown & ~mask;
        var levels = high == true ? _inputs | mask : _inputs & ~mask;
        ApplyInputs((byte)levels, (byte)known);
    }

    public void SetInputs(byte levels, byte known)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(levels, C.InputMask);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(known, C.InputMask);
        ApplyInputs((byte)(levels & known), known);
    }

    private void ApplyInputs(byte levels, byte known)
    {
        var causes = (byte)((_inputs ^ levels) | (_inputKnown ^ known));
        if (causes == 0) return;
        Resolve(levels, known, out var targets, out var targetKnown);
        var changes = (byte)((_targets ^ targets) | (_targetKnown ^ targetKnown));
        var inputState = levels | (known << 6);
        // A one-source front already determines its path class. Both active-low
        // enables also share one physical path class; retain the general resolver
        // for mixtures of select and enable paths.
        var fixedPath = causes == 8 ? 3 : (causes & 15) == 0 ? 2 : -1;
        var singleSelect = (causes & C.SelectMask) != 0 && (causes & (causes - 1)) == 0;
        // Validate a complete input front before any mutation at the time representation limit.
        if (changes != 0 && _time > long.MaxValue - Timing.Maximum)
        {
            var check = changes;
            while (check != 0)
            {
                var output = BitOperations.TrailingZeroCount((uint)check);
                check &= (byte)(check - 1);
                var mask = 1 << output;
                if ((((_levels ^ targets) | (_known ^ targetKnown)) & mask) != 0)
                    _ = checked(_time + Delay(output, causes, targets, targetKnown, inputState));
            }
        }
        _inputs = levels; _inputKnown = known;
        if (changes == 0) return;
        _targets = targets; _targetKnown = targetKnown;
        if (singleSelect && _pending == 0 && _known == byte.MaxValue && targetKnown == byte.MaxValue)
        {
            var rising = (byte)(changes & targets);
            var falling = (byte)(changes & ~targets);
            if (rising != 0 && falling != 0)
            {
                // A stable, enabled decoder releases the previous selection and
                // asserts the new one. Keep both slots and their distinct path
                // delays; only the generic per-pin programming loop is avoided.
                var release = BitOperations.TrailingZeroCount((uint)rising);
                var select = BitOperations.TrailingZeroCount((uint)falling);
                var releaseDue = _time + _delays[1][(release & causes) == 0 ? 0 : 1];
                var selectDue = _time + _delays[2][(select & causes) == 0 ? 0 : 1];
                _due[release] = releaseDue;
                _due[select] = selectDue;
                _pending = changes;
                _next = Math.Min(releaseDue, selectDue);
                PublishDeadline();
                return;
            }
        }
        var oldNext = _next;
        var recompute = false;
        while (changes != 0)
        {
            var output = BitOperations.TrailingZeroCount((uint)changes);
            changes &= (byte)(changes - 1);
            var mask = (byte)(1 << output);
            var oldDue = _due[output];
            var matches = (((_levels ^ targets) | (_known ^ targetKnown)) & mask) == 0;
            long due = 0;
            if (!matches)
            {
                var path = fixedPath >= 0 ? fixedPath : singleSelect ? (output & causes) == 0 ? 0 : 1 : -1;
                var delay = path < 0 ? Delay(output, causes, targets, targetKnown, inputState)
                    : _delays[(targetKnown & mask) == 0 ? 0 : (targets & mask) != 0 ? 1 : 2][path];
                due = _time + delay;
            }
            _due[output] = due;
            if (due == 0) _pending &= (byte)~mask;
            else
            {
                _pending |= mask;
                if (_next == 0 || due < _next) _next = due;
            }
            recompute |= oldDue != 0 && oldDue == oldNext;
        }
        if (recompute) _next = FindDeadline(_pending);
        PublishDeadline();
    }

    private static void Resolve(byte inputs, byte known, out byte levels, out byte outputKnown)
    {
        if (((inputs ^ C.EnabledLevels) & known & C.EnableMask) != 0)
        {
            levels = byte.MaxValue; outputKnown = byte.MaxValue;
            return;
        }
        var selectKnown = known & C.SelectMask;
        byte candidates;
        if (selectKnown == C.SelectMask) candidates = (byte)(1 << (inputs & C.SelectMask));
        else
        {
            candidates = byte.MaxValue;
            if ((selectKnown & 1) != 0) candidates &= (byte)((inputs & 1) != 0 ? 0xaa : 0x55);
            if ((selectKnown & 2) != 0) candidates &= (byte)((inputs & 2) != 0 ? 0xcc : 0x33);
            if ((selectKnown & 4) != 0) candidates &= (byte)((inputs & 4) != 0 ? 0xf0 : 0x0f);
        }
        levels = (byte)~candidates;
        outputKnown = selectKnown == C.SelectMask && (known & C.EnableMask) == C.EnableMask
            ? byte.MaxValue : levels;
    }

    private long Delay(int output, byte causes, byte levels, byte known, int inputState)
    {
        var mask = 1 << output;
        var rise = (known & levels & mask) != 0;
        var delays = _delays[(known & mask) == 0 ? 0 : rise ? 1 : 2];
        if (rise)
        {
            // Y is the NAND of its six matching terms. The first known blocker
            // releases Y; a changed input matching its required level cannot cause that rise.
            causes &= (byte)((inputState ^ (output | C.EnabledLevels)) & (inputState >> 6));
            var select = causes & C.SelectMask;
            var first = long.MaxValue;
            if ((select & ~output) != 0) first = delays[0];
            if ((select & output) != 0) first = Math.Min(first, delays[1]);
            if ((causes & 0x30) != 0) first = Math.Min(first, delays[2]);
            if ((causes & 8) != 0) first = Math.Min(first, delays[3]);
            return first;
        }
        // Selection needs all required terms; preserve the last changed path.
        // An indeterminate target uses the conservative delay of both directions.
        var selects = causes & C.SelectMask;
        long last = 0;
        if ((selects & ~output) != 0) last = delays[0];
        if ((selects & output) != 0) last = Math.Max(last, delays[1]);
        if ((causes & 0x30) != 0) last = Math.Max(last, delays[2]);
        if ((causes & 8) != 0) last = Math.Max(last, delays[3]);
        return last;
    }

    public void AdvanceTo(long nanoseconds)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(nanoseconds, _time);
        if (_resolving)
        {
            if (nanoseconds != _time) throw new InvalidOperationException("Cannot advance time from an output callback.");
            return;
        }
        if (_next == 0 || _next > nanoseconds) { _time = nanoseconds; return; }
        _resolving = true;
        try
        {
            while (_next != 0 && _next <= nanoseconds)
            {
                _time = _next;
                byte changed = 0;
                var pending = _pending;
                while (pending != 0)
                {
                    var output = BitOperations.TrailingZeroCount((uint)pending);
                    pending &= (byte)(pending - 1);
                    if (_due[output] != _time) continue;
                    _due[output] = 0;
                    var mask = (byte)(1 << output);
                    _pending &= (byte)~mask;
                    if ((((_levels ^ _targets) | (_known ^ _targetKnown)) & mask) == 0) continue;
                    _levels = (byte)((_levels & ~mask) | (_targets & mask));
                    _known = (byte)((_known & ~mask) | (_targetKnown & mask));
                    changed |= mask;
                }
                _next = FindDeadline(_pending);
                while (changed != 0)
                {
                    var output = BitOperations.TrailingZeroCount((uint)changed);
                    changed &= (byte)(changed - 1);
                    var mask = 1 << output;
                    if ((_observed & mask) == 0) continue;
                    bool? value = (_known & mask) == 0 ? null : (_levels & mask) != 0;
                    _outputChanged?.Invoke(output, value);
                }
            }
            _time = nanoseconds;
        }
        finally { _resolving = false; PublishDeadline(); }
    }

    private long FindDeadline(byte pending)
    {
        if (pending == 0) return 0;
        if ((pending & (pending - 1)) == 0) return _due[BitOperations.TrailingZeroCount((uint)pending)];
        long next = 0;
        while (pending != 0)
        {
            var output = BitOperations.TrailingZeroCount((uint)pending);
            pending &= (byte)(pending - 1);
            var due = _due[output];
            if (next == 0 || due < next) next = due;
        }
        return next;
    }

    private void PublishDeadline()
    {
        if (_resolving) return;
        if (_publishedNext != _next)
        {
            _publishedNext = _next;
            _nextChanged?.Invoke(_next == 0 ? null : _next);
        }
        // The all-output callback may have changed inputs or the observed mask.
        // Compute this view afterwards from the resulting canonical slots.
        PublishOutputDeadline();
    }
}

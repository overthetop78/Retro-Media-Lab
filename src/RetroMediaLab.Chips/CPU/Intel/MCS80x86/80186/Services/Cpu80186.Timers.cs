using R = RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Constants.Timer80186Constants;
using D = RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Constants.Dma80186Constants;

namespace RetroMediaLab.Chips.CPU.Intel.MCS80x86.Chip80186.Services;

public sealed partial class Cpu80186
{
    private sealed class TimerChannel
    {
        public ushort Count;
        public ushort MaximumA;
        public ushort MaximumB;
        public ushort Control;
        public bool Input;
        public bool Triggered;
        public bool Output = true;
        public int OutputDelay;
        public bool PendingOutput;
        public bool ReturnPulse;
    }

    private readonly TimerChannel[] _timers = [new(), new(), new()];
    private int _timerClockPhase;
    private int _timerActiveMask;
    private bool _timerDmaRequest;
    public Action<int, bool>? TimerOutputChanged { get; set; }
    public bool TimerOutput(int timer) => _timers[timer].Output;

    private void ResetTimers()
    {
        _timerClockPhase = _timerActiveMask = 0;
        _timerDmaRequest = false;
        for (var timer = 0; timer < R.Count; timer++)
        {
            var channel = _timers[timer];
            channel.Count = channel.MaximumA = channel.MaximumB = 0;
            channel.Control = timer < R.ExternalCount ? R.Alternate : (ushort)0;
            channel.Triggered = false;
            channel.OutputDelay = 0;
            SetTimerOutput(timer, true);
        }
    }

    public void SetTimerInput(int timer, bool asserted)
    {
        var channel = _timers[timer];
        var rising = asserted && !channel.Input;
        channel.Input = asserted;
        if (!rising || (channel.Control & R.Enabled) == 0) return;
        if ((channel.Control & R.ExternalClock) != 0) IncrementTimer(timer);
        else if ((channel.Control & R.Retrigger) != 0)
        {
            channel.Count = 0;
            channel.Triggered = true;
        }
    }

    private ushort ReadTimerRegister(int offset)
    {
        var timer = (offset - R.RegisterBase) / R.RegisterStride;
        var channel = _timers[timer];
        return ((offset - R.RegisterBase) % R.RegisterStride) switch
        {
            R.CounterRegister => channel.Count,
            R.MaximumARegister => channel.MaximumA,
            R.MaximumBRegister => timer < R.ExternalCount ? channel.MaximumB : (ushort)0,
            R.ControlRegister => channel.Control,
            _ => 0
        };
    }

    private void WriteTimerRegister(int offset, ushort value)
    {
        var timer = (offset - R.RegisterBase) / R.RegisterStride;
        var channel = _timers[timer];
        switch ((offset - R.RegisterBase) % R.RegisterStride)
        {
            case R.CounterRegister: channel.Count = value; break;
            case R.MaximumARegister: channel.MaximumA = value; break;
            case R.MaximumBRegister:
                if (timer < R.ExternalCount) channel.MaximumB = value;
                break;
            case R.ControlRegister:
                var previous = channel.Control;
                var preserved = timer == R.PrescalerTimer ? R.Timer2PreservedBits : R.PreservedBits;
                value = (ushort)((value & ~preserved) | (previous & preserved));
                if ((value & R.ChangeEnable) == 0) value = (ushort)((value & ~R.Enabled) | (previous & R.Enabled));
                value &= unchecked((ushort)~R.ChangeEnable);
                if ((value & R.Alternate) == 0) value &= unchecked((ushort)~R.RegisterInUse);
                channel.Control = value;
                if ((previous & R.Enabled) == 0 && (value & R.Enabled) != 0) channel.Triggered = false;
                if ((value & R.Enabled) != 0) _timerActiveMask |= 1 << timer;
                else _timerActiveMask &= ~(1 << timer);
                if (timer < R.ExternalCount && (value & R.Alternate) != 0)
                {
                    channel.OutputDelay = 0;
                    SetTimerOutput(timer, (value & R.RegisterInUse) == 0);
                }
                break;
        }
    }

    private void SetTimerOutput(int timer, bool level)
    {
        var channel = _timers[timer];
        if (channel.Output == level) return;
        channel.Output = level;
        if (timer < R.ExternalCount) TimerOutputChanged?.Invoke(timer, level);
    }

    private bool TimerGateOpen(int timer)
    {
        if (timer == R.PrescalerTimer) return true;
        var channel = _timers[timer];
        return (channel.Control & R.Retrigger) != 0 ? channel.Triggered : channel.Input;
    }

    private void IncrementTimer(int timer)
    {
        var channel = _timers[timer];
        channel.Count++;
        var maximum = (channel.Control & R.RegisterInUse) != 0 ? channel.MaximumB : channel.MaximumA;
        if (channel.Count != maximum) return;
        channel.Count = 0;
        channel.Control |= R.MaximumReached;
        if ((channel.Control & R.InterruptEnabled) != 0) _interruptStatus |= (ushort)(1 << timer);
        if (timer == R.PrescalerTimer)
        {
            if (((_dma[0].Control | _dma[1].Control) & D.TimerRequest) != 0) _timerDmaRequest = true;
            for (var prescaled = 0; prescaled < R.ExternalCount; prescaled++)
            {
                var control = _timers[prescaled].Control;
                if ((control & (R.Enabled | R.ExternalClock | R.Prescaled)) == (R.Enabled | R.Prescaled) && TimerGateOpen(prescaled))
                    IncrementTimer(prescaled);
            }
        }
        else
        {
            channel.OutputDelay = R.PulseDelay;
            channel.PendingOutput = (channel.Control & (R.Alternate | R.RegisterInUse)) == (R.Alternate | R.RegisterInUse);
            channel.ReturnPulse = (channel.Control & R.Alternate) == 0;
        }
        if ((channel.Control & R.Continuous) != 0 || (channel.Control & (R.Alternate | R.RegisterInUse)) == R.Alternate)
        {
            if ((channel.Control & R.Alternate) != 0) channel.Control ^= R.RegisterInUse;
        }
        else
        {
            channel.Control &= unchecked((ushort)~(R.Enabled | R.RegisterInUse));
            _timerActiveMask &= ~(1 << timer);
            channel.Triggered = false;
        }
    }

    private void AdvanceTimers(long clocks)
    {
        while (clocks > 0)
        {
            var delay = R.ClockDivider - _timerClockPhase;
            var pendingOutput = false;
            for (var timer = 0; timer < R.ExternalCount; timer++)
            {
                var outputDelay = _timers[timer].OutputDelay;
                if (outputDelay == 0) continue;
                pendingOutput = true;
                delay = Math.Min(delay, outputDelay);
            }
            if (_timerActiveMask == 0 && !pendingOutput)
            {
                _timerClockPhase = (int)((_timerClockPhase + clocks) % R.ClockDivider);
                return;
            }
            var elapsed = (int)Math.Min(clocks, delay);
            clocks -= elapsed;
            _timerClockPhase += elapsed;
            for (var timer = 0; timer < R.ExternalCount; timer++)
            {
                var channel = _timers[timer];
                if (channel.OutputDelay == 0) continue;
                channel.OutputDelay -= elapsed;
                if (channel.OutputDelay != 0) continue;
                SetTimerOutput(timer, channel.PendingOutput);
                if (channel.ReturnPulse)
                {
                    channel.ReturnPulse = false;
                    channel.PendingOutput = true;
                    channel.OutputDelay = R.PulseDelay;
                }
            }
            if (_timerClockPhase != R.ClockDivider) continue;
            _timerClockPhase = 0;
            for (var timer = 0; timer < R.Count; timer++)
            {
                var control = _timers[timer].Control;
                if ((control & R.Enabled) == 0 || (control & R.ExternalClock) != 0 ||
                    timer < R.ExternalCount && (control & R.Prescaled) != 0 || !TimerGateOpen(timer)) continue;
                IncrementTimer(timer);
            }
        }
    }
}

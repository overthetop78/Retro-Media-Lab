using RetroMediaLab.Chips.Audio.Common.Resampling;

namespace RetroMediaLab.Chips.Audio.Sega.PWM32X;

/// <summary>32X PWM; timestamps are Mega Drive master clocks. Borrows the output buffer.</summary>
public sealed partial class Pwm32X
{
    private readonly BandLimitedBuffer _output;
    private readonly ushort[] _fifo = new ushort[6];
    private readonly int[] _head = new int[2], _count = new int[2], _sample = new int[2];
    private ushort _control, _cycle;
    private long _nextTick;
    private uint _tick;
    private int _periods, _lastLeft, _lastRight;
    public bool InterruptPending { get; private set; }
    public bool DmaRequest => InterruptPending && (_control & 0x80) != 0;
    public Action? InterruptChanged { get; set; }
    public Pwm32X(BandLimitedBuffer output) { _output = output; Reset(); }
    public void Reset()
    {
        Array.Clear(_fifo); Array.Clear(_head); Array.Clear(_count); Array.Clear(_sample);
        _control = _cycle = 0; _nextTick = 4095 * 7; _tick = 0; _periods = _lastLeft = _lastRight = 0;
        InterruptPending = false; InterruptChanged?.Invoke();
    }
    public void Acknowledge() { InterruptPending = false; InterruptChanged?.Invoke(); }
    public ushort Read(int offset, uint tick)
    {
        Update(tick);
        int Status(int channel) => (_count[channel] == 0 ? 0x4000 : 0) | (_count[channel] == 3 ? 0x8000 : 0);
        return (offset & 14) switch
        {
            0 => _control, 2 => _cycle, 4 => (ushort)Status(0), 6 => (ushort)Status(1),
            8 => (ushort)((_count[0] == 0 && _count[1] == 0 ? 0x4000 : 0) | (_count[0] == 3 || _count[1] == 3 ? 0x8000 : 0)), _ => 0
        };
    }
    public void Write(int offset, ushort value, uint tick)
    {
        Update(tick);
        switch (offset & 14)
        {
            case 0: _control = (ushort)(value & 0xf9f); if ((_control & 0x80) == 0) InterruptChanged?.Invoke(); break;
            case 2: _cycle = (ushort)(value & 4095); _nextTick = (long)tick * 3 + Period * 7; break;
            case 4: Push(0, value); break; case 6: Push(1, value); break; case 8: Push(0, value); Push(1, value); break;
        }
    }
    private int Period => Math.Max(1, (_cycle - 1) & 4095);
    private void Push(int channel, ushort value)
    {
        if (_count[channel] == 3) return;
        _fifo[channel * 3 + (_head[channel] + _count[channel]) % 3] = (ushort)(value & 4095); _count[channel]++;
    }
    public void Update(uint tick)
    {
        if (tick < _tick) return;
        _tick = tick;
        if (_cycle == 1 || (_control & 15) == 0) { _nextTick = (long)tick * 3 + Period * 7; return; }
        while (_nextTick <= (long)tick * 3)
        {
            int period = Period;
            if (_cycle != 1 && (_control & 15) != 0)
            {
                for (int c = 0; c < 2; c++) if (_count[c] > 0) { _sample[c] = _fifo[c * 3 + _head[c]]; _head[c] = (_head[c] + 1) % 3; _count[c]--; }
                int timer = (_control >> 8) & 15;
                if (++_periods >= (timer == 0 ? 16 : timer)) { _periods = 0; InterruptPending = true; InterruptChanged?.Invoke(); }
                int leftMode = _control & 3, rightMode = (_control >> 2) & 3;
                int Convert(int sample) => Math.Clamp((sample * 2 - period) * 16384 / period, -32768, 32767);
                int left = leftMode == 1 ? Convert(_sample[0]) : leftMode == 2 ? Convert(_sample[1]) : 0;
                int right = rightMode == 1 ? Convert(_sample[1]) : rightMode == 2 ? Convert(_sample[0]) : 0;
                _output.AddDeltaFast((uint)(_nextTick / 3), left - _lastLeft, right - _lastRight); _lastLeft = left; _lastRight = right;
            }
            _nextTick += period * 7L;
        }
    }
    public void EndFrame(uint clocks) { Update(clocks); _nextTick -= clocks * 3L; _tick = 0; }
}

using System.Diagnostics;
using System.IO;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

internal sealed partial class Sh4TimerRegisters
{
    private const uint StartAddress = 0xFFD80004;
    private const uint ReloadAddress = 0xFFD80008;
    private const uint CounterAddress = 0xFFD8000C;
    private const uint ControlAddress = 0xFFD80010;
    private const uint ChannelStride = 12;
    private const uint Underflow = 0x100;
    private const uint UnderflowInterruptEnable = 0x20;

    private static readonly Sh4InterruptId[] ChannelInterrupts =
        [Sh4InterruptId.Tmu0Tuni0, Sh4InterruptId.Tmu1Tuni1, Sh4InterruptId.Tmu2Tuni2];
    private readonly uint[] _shift = new uint[3];
    private readonly ulong[] _mask = new ulong[3];
    private readonly uint[] _oldMode = [0xFFFF, 0xFFFF, 0xFFFF];
    private readonly int[] _schedulerIds = [-1, -1, -1];
    internal ReadOnlySpan<int> SchedulerIds => _schedulerIds;
    private readonly long[] _channelBase = new long[3];
    private Sh4Context _context = null!;
    private Sh4Scheduler _scheduler = null!;
    private Sh4Interrupts _interrupts = null!;

    internal void Attach(Sh4Context context, Sh4Scheduler scheduler, Sh4Interrupts interrupts)
    {
        _context = context;
        _scheduler = scheduler;
        _interrupts = interrupts;
    }

    private ulong Now() => unchecked(_scheduler.Now64() + Sh4Scheduler.TimeSlice - (ulong)_context.CycleCounter);

    private long Elapsed(int channel) => unchecked((long)((Now() >> (int)_shift[channel]) & _mask[channel]));

    private long ReadCounter(int channel) => unchecked(_channelBase[channel] - Elapsed(channel));

    private void ScheduleChannelTick(int channel)
    {
        if (_mask[channel] != 0)
        {
            long counter = ReadCounter(channel);
            int cycles = (int)Math.Clamp(counter << (int)_shift[channel], 0L, Sh4Context.MainClock);
            _scheduler.Request(_schedulerIds[channel], cycles);
        }
        else
        {
            _scheduler.Request(_schedulerIds[channel], -1);
        }
    }

    private void WriteCounter(int channel, long value)
    {
        _channelBase[channel] = unchecked(value + Elapsed(channel));
        ScheduleChannelTick(channel);
    }

    private uint ReloadCounter(int channel, long value)
    {
        uint offset = (uint)channel * ChannelStride;
        uint control = ControlAddress + offset;
        SetValue(control, GetValue(control) | Underflow);
        _interrupts.SetPending(ChannelInterrupts[channel], true);

        long reloaded = Math.Max(GetValue(ReloadAddress + offset) + value, 0);
        WriteCounter(channel, reloaded);
        return (uint)reloaded;
    }

    private uint ReadCounterRegister(int channel)
    {
        long value = ReadCounter(channel);
        return value < 0 ? ReloadCounter(channel, value) : (uint)value;
    }

    private void EnableChannel(int channel, bool enabled)
    {
        long value = ReadCounter(channel);
        _mask[channel] = enabled ? ulong.MaxValue : 0;
        WriteCounter(channel, value);
    }

    private void UpdateCounts(int channel)
    {
        uint control = GetValue(ControlAddress + (uint)channel * ChannelStride);
        _interrupts.SetPending(ChannelInterrupts[channel], (control & Underflow) != 0);
        _interrupts.SetMask(ChannelInterrupts[channel], (control & UnderflowInterruptEnable) != 0);

        uint mode = control & 7;
        if (_oldMode[channel] == mode)
            return;
        _oldMode[channel] = mode;

        long counter = ReadCounter(channel);
        switch (mode)
        {
            case 0:
                _shift[channel] = 2;
                break;
            case 1:
                _shift[channel] = 4;
                break;
            case 2:
                _shift[channel] = 6;
                break;
            case 3:
                _shift[channel] = 8;
                break;
            case 4:
                _shift[channel] = 10;
                break;
            case 5:
                Trace.TraceInformation($"TMU ch{channel} - TCR{channel} mode is reserved (5)");
                break;
            case 6:
                Trace.TraceInformation($"TMU ch{channel} - TCR{channel} mode is RTC (6), can't be used on Dreamcast");
                break;
            case 7:
                Trace.TraceInformation($"TMU ch{channel} - TCR{channel} mode is External (7), can't be used on Dreamcast");
                break;
        }
        _shift[channel] += 2;
        WriteCounter(channel, counter);
    }

    private void WriteControl(int channel, ushort value)
    {
        SetValue(ControlAddress + (uint)channel * ChannelStride, (uint)(value & (channel == 2 ? 0x03FF : 0x013F)));
        UpdateCounts(channel);
    }

    private static uint ReadCapture(uint address)
    {
        Trace.TraceInformation("Read from TMU_TCPR2 - this register should be not used on Dreamcast according to docs");
        return 0;
    }

    private static void WriteCapture(uint address, uint value)
    {
        Trace.TraceInformation($"Write to TMU_TCPR2 - this register should be not used on Dreamcast according to docs, data={unchecked((int)value)}");
    }

    private void WriteStart(uint address, byte value)
    {
        SetValue(StartAddress, (uint)(value & 7));
        for (int channel = 0; channel < 3; channel++)
            EnableChannel(channel, (value & (1 << channel)) != 0);
    }

    private int ScheduledCallback(int channel, int cycles, int jitter, nint argument)
    {
        if (_mask[channel] != 0)
        {
            long counter = ReadCounter(channel);
            if (counter < 0)
                ReloadCounter(channel, counter);
            else
                WriteCounter(channel, counter);
        }

        return 0;
    }

    internal override void Initialize()
    {
        base.Initialize();
        SetReadWrite(0xFFD80000, 1, 1);
        SetWriteHandler8(StartAddress, WriteStart);
        SetReadWrite(ReloadAddress, 4);
        SetHandlers32(CounterAddress, _ => ReadCounterRegister(0), (_, value) => WriteCounter(0, value));
        SetWriteHandler16(ControlAddress, (_, value) => WriteControl(0, value));
        SetReadWrite(ReloadAddress + ChannelStride, 4);
        SetHandlers32(CounterAddress + ChannelStride, _ => ReadCounterRegister(1), (_, value) => WriteCounter(1, value));
        SetWriteHandler16(ControlAddress + ChannelStride, (_, value) => WriteControl(1, value));
        SetReadWrite(ReloadAddress + 2 * ChannelStride, 4);
        SetHandlers32(CounterAddress + 2 * ChannelStride, _ => ReadCounterRegister(2), (_, value) => WriteCounter(2, value));
        SetWriteHandler16(ControlAddress + 2 * ChannelStride, (_, value) => WriteControl(2, value));
        SetHandlers32(0xFFD8002C, ReadCapture, WriteCapture);
        for (int channel = 0; channel < _schedulerIds.Length; channel++)
            _schedulerIds[channel] = _scheduler.Register(channel, ScheduledCallback);
        Reset();
    }

    internal override void Reset()
    {
        base.Reset();
        Array.Clear(_shift);
        Array.Clear(_mask);
        Array.Fill(_oldMode, uint.MaxValue);
        Array.Clear(_channelBase);

        SetValue(ReloadAddress + 2 * ChannelStride, uint.MaxValue);
        SetValue(ReloadAddress + ChannelStride, uint.MaxValue);
        SetValue(ReloadAddress, uint.MaxValue);

        UpdateCounts(0);
        UpdateCounts(1);
        UpdateCounts(2);

        WriteStart(0, 0);
        for (int channel = 0; channel < 3; channel++)
            WriteCounter(channel, uint.MaxValue);
    }

    internal override void Terminate()
    {
        try
        {
            base.Terminate();
        }
        finally
        {
            foreach (ref int schedulerId in _schedulerIds.AsSpan())
            {
                _scheduler.Unregister(schedulerId);
                schedulerId = -1;
            }
        }
    }

    internal void Serialize(BinaryWriter writer)
    {
        foreach (uint value in _shift)
            writer.Write(value);
        foreach (ulong value in _mask)
            writer.Write(value);
        foreach (uint value in _oldMode)
            writer.Write(value);
        foreach (long value in _channelBase)
            writer.Write(value);
        foreach (int schedulerId in _schedulerIds)
            _scheduler.Serialize(writer, schedulerId);
    }

    internal void Deserialize(BinaryReader reader, int version)
    {
        for (int channel = 0; channel < 3; channel++)
            _shift[channel] = reader.ReadUInt32();
        if (version < Sh4Scheduler.SaveVersion58)
            for (int channel = 0; channel < 3; channel++)
                reader.ReadUInt32();
        for (int channel = 0; channel < 3; channel++)
            _mask[channel] = reader.ReadUInt64();
        for (int channel = 0; channel < 3; channel++)
            _oldMode[channel] = reader.ReadUInt32();
        if (version < Sh4Scheduler.SaveVersion58)
            for (int channel = 0; channel < 3; channel++)
                reader.ReadUInt32();
        for (int channel = 0; channel < 3; channel++)
            _channelBase[channel] = reader.ReadInt64();
        if (version >= Sh4Scheduler.SaveVersion58)
            foreach (int schedulerId in _schedulerIds)
                _scheduler.Deserialize(reader, schedulerId);
    }
}

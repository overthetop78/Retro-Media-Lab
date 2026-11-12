using RetroMediaLab.Chips.CPU.NEC.UPD780C.UPD780C1.Models;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Contracts;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Enums;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Models;
using RetroMediaLab.Chips.CPU.Zilog.Z80.Z80.Services;
using Xunit;

namespace RetroMediaLab.Chips.Tests.CPU.Common.Z80;

/// <summary>Banc numérique commun aux profils du même moteur, sans ROM ni ressource externe.</summary>
internal sealed class Z80TestCircuit : IDisposable
{
    internal CpuZ80 Cpu { get; }
    internal byte[] Memory { get; }
    internal List<(long Time, Z80BusPins Value)> Pins { get; } = [];
    internal List<(long Time, ushort Address, byte Value)> MemoryWrites { get; } = [];
    internal List<(long Time, ushort Address, byte Value)> PortWrites { get; } = [];
    internal List<(long Time, ushort Address, bool Io)> Reads { get; } = [];
    internal byte PortInputData { get; set; } = 0x7b;
    private Z80BusPins _previous;
    private bool _clockHigh;
    private long _nextClock;
    private readonly long _halfPeriod;

    internal Z80TestCircuit(byte[] program, Z80Registers? registers = null, Z80InterruptState? interrupts = null,
        CpuZ80State? restore = null, Z80SiliconVariant silicon = Z80SiliconVariant.ZilogNmos, Z80BusTiming? timing = null)
    {
        var initial = registers ?? InitialRegisters();
        Memory = restore is null ? new byte[65536] : (byte[])program.Clone();
        if (restore is null) program.CopyTo(Memory, initial.PC);
        var selectedTiming = timing ?? restore?.Bus.Timing ?? Z80BusTiming.ZilogZ80AMaximum;
        var hardware = silicon == Z80SiliconVariant.NecNmos ? Upd780C1Profile.Hardware : Z80HardwareProfile.ZilogNmos;
        _halfPeriod = selectedTiming.MinimumPeriod / 2;
        Cpu = new CpuZ80(hardware, selectedTiming, initial, interrupts ?? default, 0xff);
        if (restore is not null) Cpu.RestoreState(restore);
        _previous = Cpu.Pins;
        _clockHigh = restore?.Bus.ClockHigh ?? false;
        _nextClock = restore is null ? _halfPeriod : restore.Bus.LastClockEdge + _halfPeriod;
        Cpu.PinsChanged = OnPins;
        if (restore is null) { Cpu.SetClock(true); _clockHigh = true; }
    }

    private void OnPins(Z80BusPins pins)
    {
        long time = Cpu.TimeNanoseconds;
        Pins.Add((time, pins));
        if (WriteActive(_previous, io: false) && !WriteActive(pins, io: false))
        {
            ushort address = _previous.Address!.Value; byte data = _previous.Data!.Value;
            Memory[address] = data; MemoryWrites.Add((time, address, data));
        }
        if (WriteActive(_previous, io: true) && !WriteActive(pins, io: true))
            PortWrites.Add((time, _previous.Address!.Value, _previous.Data!.Value));
        if (pins.ReadHigh == false && pins.Address is { } readAddress)
        {
            bool memory = pins.MemoryRequestHigh == false, io = pins.IoRequestHigh == false;
            if (memory || io)
            {
                if (_previous.ReadHigh != false || _previous.Address != readAddress)
                    Reads.Add((time, readAddress, io));
                Cpu.SetData(memory ? Memory[readAddress] : PortInputData);
            }
        }
        if (!pins.MachineCycleOneHigh && pins.IoRequestHigh == false) Cpu.SetData(PortInputData);
        _previous = pins;
    }

    private static bool WriteActive(Z80BusPins pins, bool io) => pins.WriteHigh == false
        && (io ? pins.IoRequestHigh : pins.MemoryRequestHigh) == false
        && pins.Address.HasValue && pins.Data.HasValue;

    internal void AdvanceTo(long time)
    {
        while (_nextClock <= time) Edge();
        Cpu.AdvanceTo(time);
    }

    internal void RunInstructions(long count)
    {
        int limit = 100_000;
        while (Cpu.InstructionsCompleted < count && limit-- != 0) Edge();
        Assert.Equal(count, Cpu.InstructionsCompleted);
    }

    internal void Edge()
    {
        Cpu.AdvanceTo(_nextClock); _clockHigh = !_clockHigh; Cpu.SetClock(_clockHigh); _nextClock += _halfPeriod;
    }

    internal void ClearTraces() { Pins.Clear(); MemoryWrites.Clear(); PortWrites.Clear(); Reads.Clear(); }
    public void Dispose() { Cpu.PinsChanged = null; Cpu.NextEventNanosecondsChanged = null; }
    internal static Z80Registers InitialRegisters() => new() { AF = 0x1245, SP = 0xa000, R = 0x80 };

    internal static void VerifyNormalReset(Z80SiliconVariant silicon, bool fromHalt)
    {
        // NEC 1982 Microcomputer Catalog, printed p.393: normal RESET clears
        // PC/I/R/IFF, selects IM0, floats address/data and inactivates controls.
        // This checks the documented >=3-clock reset, not short pulses or RD in HALT.
        var initial = InitialRegisters(); initial.I = 0x7a; initial.R = 0xa1;
        var interrupts = new Z80InterruptState(true, true, Z80InterruptMode.Mode2, 0, false, false, false);
        using var circuit = new Z80TestCircuit(fromHalt ? [0x76] : [0x21, 0x34, 0x12], initial,
            interrupts, silicon: silicon, timing: silicon == Z80SiliconVariant.NecNmos
                ? Upd780C1Profile.Timing : Z80BusTiming.ZilogZ80AMaximum);
        circuit.Cpu.SetWait(!fromHalt); circuit.AdvanceTo(1500);
        Assert.Equal(fromHalt, circuit.Cpu.IsHalted);
        circuit.Cpu.SetReset(true); circuit.AdvanceTo(2500);
        Assert.Equal((ushort)0, circuit.Cpu.Registers.PC);
        Assert.Equal((byte)0, circuit.Cpu.Registers.I); Assert.Equal((byte)0, circuit.Cpu.Registers.R);
        Assert.Equal(initial.AF, circuit.Cpu.Registers.AF);
        Assert.False(circuit.Cpu.InterruptState.Iff1);
        Assert.False(circuit.Cpu.InterruptState.Iff2);
        Assert.Equal(Z80InterruptMode.Mode0, circuit.Cpu.InterruptState.Mode);
        Assert.False(circuit.Cpu.IsHalted);
        var pins = circuit.Cpu.Pins;
        Assert.Null(pins.Address); Assert.Null(pins.Data);
        Assert.True(pins.MemoryRequestHigh); Assert.True(pins.IoRequestHigh);
        Assert.True(pins.ReadHigh); Assert.True(pins.WriteHigh);
        Assert.True(pins.MachineCycleOneHigh); Assert.True(pins.RefreshHigh);
        Assert.True(pins.HaltHigh); Assert.True(pins.BusAcknowledgeHigh);
        circuit.Memory[0] = 0x21; circuit.Memory[1] = 0x34; circuit.Memory[2] = 0x12;
        circuit.Cpu.SetWait(false); circuit.Cpu.SetReset(false);
        circuit.RunInstructions(circuit.Cpu.InstructionsCompleted + 1);
        Assert.Equal((ushort)0x1234, circuit.Cpu.Registers.HL);
    }

}

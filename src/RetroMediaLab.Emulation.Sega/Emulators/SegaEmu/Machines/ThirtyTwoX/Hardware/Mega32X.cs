using System.Buffers.Binary;
using RetroMediaLab.Chips.Controllers.Hitachi.SH7095;
using RetroMediaLab.Chips.Video.Sega.VDP32X;
using RetroMediaLab.Chips.Audio.Sega.PWM32X;
using RetroMediaLab.Chips.Audio.Common.Resampling;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Machines.ThirtyTwoX.Hardware;

/// <summary>Machine-owned 32X adapter. Borrows the Mega Drive ROM, clock and audio sink.</summary>
internal sealed partial class Mega32X
{
    private readonly Func<uint, byte> _rom;
    private readonly Func<uint, ushort> _romWord;
    private ReadOnlyMemory<byte> _cartridgeView;
    private readonly Func<uint> _mainCycles;
    private readonly Sh7095?[] _cpus = new Sh7095?[2];
    private readonly byte[] _sdram = new byte[0x40000], _bios68k = new byte[256], _masterRom = new byte[2048], _slaveRom = new byte[1024];
    private readonly ushort[] _communication = new ushort[8], _fifo = new ushort[8];
    private readonly byte[] _mask = new byte[2], _pending = new byte[2];
    private readonly bool[] _running = new bool[2];
    private readonly uint[] _sdramVersions = new uint[16384];
    private readonly uint[] _sdramCodeWords = new uint[4096]; // one bit per SH-2 word (16 KiB)
    // Derived write barrier for translated code; deliberately absent from save states.
    private ulong _sdramEpoch;
    private readonly bool _cartridgePresent;
    private bool _realMaster, _realSlave, _booted, _syncing;
    private ushort _control = 0x82, _dreqControl, _dmaLength = 0xffff;
    private uint _source, _destination;
    private int _romBank, _fifoHead, _fifoCount, _hintPeriod, _hintCounter, _lastPhysicalLine = -1;
    private ulong _epoch, _lastMaster;
    public Vdp32X Video { get; } = new();
    public Pwm32X Pwm { get; }
    public Sh7095 Master => _cpus[0]!;
    public Sh7095 Slave => _cpus[1]!;
    public bool Enabled => (_control & 1) != 0;
    public bool Released => (_control & 2) != 0;
    public void SetCartridgeView(ReadOnlyMemory<byte> view)
    {
        if (!view.IsEmpty && view.Length != 0x400000) throw new ArgumentException(nameof(view));
        _cartridgeView = view;
        Master.Cpu.ClearCodeCache(); Slave.Cpu.ClearCodeCache();
    }
    public Mega32X(Func<uint, byte> rom, Func<uint, ushort> romWord, Func<uint> mainCycles, BandLimitedBuffer output, Func<int, Memory<byte>, int> firmware, bool cartridgePresent, bool pal)
    {
        _rom = rom; _romWord = romWord; _mainCycles = mainCycles; _cartridgePresent = cartridgePresent; Video.Pal = pal;
        Pwm = new(output); PrepareBootRoms(firmware);
        _cpus[0] = new Sh7095(new Sh2Bus(this, 0)); _cpus[1] = new Sh7095(new Sh2Bus(this, 1));
        Master.SerialTransmitted = Slave.ReceiveSerial; Slave.SerialTransmitted = Master.ReceiveSerial;
        Pwm.InterruptChanged = UpdateInterrupts;
    }
    public void Reset()
    {
        _control = 0x82; _dreqControl = 0; _dmaLength = 0xffff; _source = _destination = 0;
        _romBank = _fifoHead = _fifoCount = _hintPeriod = _hintCounter = 0; _lastPhysicalLine = -1;
        _epoch = _lastMaster = 0; _booted = _syncing = false; Array.Clear(_running); Array.Clear(_mask); Array.Clear(_pending);
        ResetPolling(); _sdramEpoch++; Array.Clear(_sdramVersions); Array.Clear(_sdram); Array.Clear(_communication); Array.Clear(_fifo); Video.Reset(); Pwm.Reset(); Master.Reset(); Slave.Reset();
        Array.Clear(_sdramCodeWords);
    }
    private uint Rom32(uint address) => (uint)_rom(address) << 24 | (uint)_rom(address + 1) << 16 | (uint)_rom(address + 2) << 8 | _rom(address + 3);
    private void StartProcessors()
    {
        ResetPolling();
        _cpuOriginMaster = _lastMaster;
        Master.Reset(); Slave.Reset();
        Array.Clear(_sdramCodeWords);
        if (!_realMaster) Master.Cpu.GlobalBase = 0x20004000;
        if (_cartridgePresent && !_realMaster)
        {
            uint source = Rom32(0x3d4) & 0x0fffffff, destination = Rom32(0x3d8) & 0x0fffffff, count = Math.Min(Rom32(0x3dc), 0x40000);
            for (uint i = 0; i < count; i++) _sdram[(destination + i) & 0x3ffff] = _rom(source + i);
            _sdramEpoch++;
            Master.Cpu.VectorBase = Rom32(0x3e8);
            _communication[4] = (ushort)((_rom(0x18e) << 8) | _rom(0x18f));
        }
        if (!_realSlave) { Slave.Cpu.GlobalBase = 0x20004000; if (_cartridgePresent) Slave.Cpu.VectorBase = Rom32(0x3ec); }
        _booted = true;
    }
    private void UpdateInterrupts()
    {
        for (int c = 0; c < 2; c++)
        {
            if (_cpus[c] is not { } cpu) continue;
            if (Pwm.InterruptPending) _pending[c] |= 1;
            cpu.SetDreq(0, _fifoCount != 0); cpu.SetDreq(1, Pwm.DmaRequest);
            int bits = (_pending[c] & _mask[c] & 15) | (_pending[c] & 16), level = 0;
            for (int i = 0; i < 5; i++) if ((bits & (1 << i)) != 0) level = 6 + i * 2;
            cpu.SetExternalInterrupt(level, 64 + level / 2);
        }
    }
}

